using System;
using System.IO;

namespace GameBoy.Audio
{
    public sealed class Apu
    {
        public const int SampleRate = 44100;
        private const int CpuClock = 4194304;
        private const int FrameSequencerPeriod = 8192;
        private const int RingFrames = SampleRate * 2;

        private static readonly byte[] ReadMask =
        {
            0x80, 0x3F, 0x00, 0xFF, 0xBF,
            0xFF, 0x3F, 0x00, 0xFF, 0xBF,
            0x7F, 0xFF, 0x9F, 0xFF, 0xBF,
            0xFF, 0xFF, 0x00, 0x00, 0xBF,
            0x00, 0x00, 0x70, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF
        };

        private readonly byte[] waveRam = new byte[0x10];
        private readonly SquareChannel ch1 = new SquareChannel(true);
        private readonly SquareChannel ch2 = new SquareChannel(false);
        private readonly WaveChannel ch3;
        private readonly NoiseChannel ch4 = new NoiseChannel();

        private bool powered = true;
        private int frameStep;
        private int frameCounter;
        private int sampleAccumulator;

        private byte nr50;
        private byte nr51;

        private readonly float[] ring = new float[RingFrames * 2];
        private int writePos;
        private int readPos;
        private int frameCount;

        public Apu()
        {
            ch3 = new WaveChannel(waveRam);
        }

        public int AvailableSamples => frameCount;

        public void Step(int cycles)
        {
            if (powered)
            {
                ch1.Step(cycles);
                ch2.Step(cycles);
                ch3.Step(cycles);
                ch4.Step(cycles);

                frameCounter += cycles;
                while (frameCounter >= FrameSequencerPeriod)
                {
                    frameCounter -= FrameSequencerPeriod;
                    ClockFrameSequencer();
                }
            }

            sampleAccumulator += cycles * SampleRate;
            while (sampleAccumulator >= CpuClock)
            {
                sampleAccumulator -= CpuClock;
                EmitSample();
            }
        }

        private void ClockFrameSequencer()
        {
            switch (frameStep)
            {
                case 0:
                    ClockLengths();
                    break;
                case 2:
                    ClockLengths();
                    ch1.ClockSweep();
                    break;
                case 4:
                    ClockLengths();
                    break;
                case 6:
                    ClockLengths();
                    ch1.ClockSweep();
                    break;
                case 7:
                    ch1.ClockEnvelope();
                    ch2.ClockEnvelope();
                    ch4.ClockEnvelope();
                    break;
            }
            frameStep = (frameStep + 1) & 7;
        }

        private void ClockLengths()
        {
            ch1.ClockLength();
            ch2.ClockLength();
            ch3.ClockLength();
            ch4.ClockLength();
        }

        private void EmitSample()
        {
            float o1 = ch1.Analog();
            float o2 = ch2.Analog();
            float o3 = ch3.Analog();
            float o4 = ch4.Analog();

            float left = 0f;
            float right = 0f;

            if ((nr51 & 0x10) != 0) left += o1;
            if ((nr51 & 0x20) != 0) left += o2;
            if ((nr51 & 0x40) != 0) left += o3;
            if ((nr51 & 0x80) != 0) left += o4;

            if ((nr51 & 0x01) != 0) right += o1;
            if ((nr51 & 0x02) != 0) right += o2;
            if ((nr51 & 0x04) != 0) right += o3;
            if ((nr51 & 0x08) != 0) right += o4;

            int leftVolume = (nr50 >> 4) & 0x07;
            int rightVolume = nr50 & 0x07;

            left = left / 4f * ((leftVolume + 1) / 8f);
            right = right / 4f * ((rightVolume + 1) / 8f);

            PushSample(left, right);
        }

        private void PushSample(float left, float right)
        {
            ring[writePos * 2] = left;
            ring[writePos * 2 + 1] = right;
            writePos = (writePos + 1) % RingFrames;
            if (frameCount == RingFrames)
                readPos = (readPos + 1) % RingFrames;
            else
                frameCount++;
        }

        public int ReadSamples(float[] destination)
        {
            int frames = Math.Min(frameCount, destination.Length / 2);
            for (int i = 0; i < frames; i++)
            {
                destination[i * 2] = ring[readPos * 2];
                destination[i * 2 + 1] = ring[readPos * 2 + 1];
                readPos = (readPos + 1) % RingFrames;
            }
            frameCount -= frames;
            return frames;
        }

        public void ClearSamples()
        {
            readPos = writePos;
            frameCount = 0;
        }

        public byte Read(ushort address)
        {
            if (address >= 0xFF30 && address <= 0xFF3F)
                return waveRam[address - 0xFF30];

            if (address == 0xFF26)
                return Nr52();

            byte raw = RawRegister(address);
            int index = address - 0xFF10;
            byte mask = index >= 0 && index < ReadMask.Length ? ReadMask[index] : (byte)0xFF;
            return (byte)(raw | mask);
        }

        public void Write(ushort address, byte value)
        {
            if (address >= 0xFF30 && address <= 0xFF3F)
            {
                waveRam[address - 0xFF30] = value;
                return;
            }

            if (address == 0xFF26)
            {
                WriteNr52(value);
                return;
            }

            if (!powered)
                return;

            switch (address)
            {
                case 0xFF10: ch1.Write(0, value); break;
                case 0xFF11: ch1.Write(1, value); break;
                case 0xFF12: ch1.Write(2, value); break;
                case 0xFF13: ch1.Write(3, value); break;
                case 0xFF14: ch1.Write(4, value); break;
                case 0xFF16: ch2.Write(1, value); break;
                case 0xFF17: ch2.Write(2, value); break;
                case 0xFF18: ch2.Write(3, value); break;
                case 0xFF19: ch2.Write(4, value); break;
                case 0xFF1A: ch3.Write(0, value); break;
                case 0xFF1B: ch3.Write(1, value); break;
                case 0xFF1C: ch3.Write(2, value); break;
                case 0xFF1D: ch3.Write(3, value); break;
                case 0xFF1E: ch3.Write(4, value); break;
                case 0xFF20: ch4.Write(1, value); break;
                case 0xFF21: ch4.Write(2, value); break;
                case 0xFF22: ch4.Write(3, value); break;
                case 0xFF23: ch4.Write(4, value); break;
                case 0xFF24: nr50 = value; break;
                case 0xFF25: nr51 = value; break;
            }
        }

        private byte RawRegister(ushort address)
        {
            switch (address)
            {
                case 0xFF10: return ch1.Read(0);
                case 0xFF11: return ch1.Read(1);
                case 0xFF12: return ch1.Read(2);
                case 0xFF13: return ch1.Read(3);
                case 0xFF14: return ch1.Read(4);
                case 0xFF16: return ch2.Read(1);
                case 0xFF17: return ch2.Read(2);
                case 0xFF18: return ch2.Read(3);
                case 0xFF19: return ch2.Read(4);
                case 0xFF1A: return ch3.Read(0);
                case 0xFF1B: return ch3.Read(1);
                case 0xFF1C: return ch3.Read(2);
                case 0xFF1D: return ch3.Read(3);
                case 0xFF1E: return ch3.Read(4);
                case 0xFF20: return ch4.Read(1);
                case 0xFF21: return ch4.Read(2);
                case 0xFF22: return ch4.Read(3);
                case 0xFF23: return ch4.Read(4);
                case 0xFF24: return nr50;
                case 0xFF25: return nr51;
                default: return 0x00;
            }
        }

        private byte Nr52()
        {
            byte value = (byte)((powered ? 0x80 : 0x00) | 0x70);
            if (ch1.Enabled) value |= 0x01;
            if (ch2.Enabled) value |= 0x02;
            if (ch3.Enabled) value |= 0x04;
            if (ch4.Enabled) value |= 0x08;
            return value;
        }

        private void WriteNr52(byte value)
        {
            bool on = (value & 0x80) != 0;
            if (!on)
            {
                ch1.PowerOff();
                ch2.PowerOff();
                ch3.PowerOff();
                ch4.PowerOff();
                nr50 = 0;
                nr51 = 0;
                frameStep = 0;
                frameCounter = 0;
            }
            else if (!powered)
            {
                frameStep = 0;
            }
            powered = on;
        }

        public void SaveState(BinaryWriter w)
        {
            w.Write(powered);
            w.Write(frameStep);
            w.Write(frameCounter);
            w.Write(sampleAccumulator);
            w.Write(nr50);
            w.Write(nr51);
            w.Write(waveRam);
            ch1.SaveState(w);
            ch2.SaveState(w);
            ch3.SaveState(w);
            ch4.SaveState(w);
        }

        public void LoadState(BinaryReader r)
        {
            powered = r.ReadBoolean();
            frameStep = r.ReadInt32();
            frameCounter = r.ReadInt32();
            sampleAccumulator = r.ReadInt32();
            nr50 = r.ReadByte();
            nr51 = r.ReadByte();
            Array.Copy(r.ReadBytes(waveRam.Length), waveRam, waveRam.Length);
            ch1.LoadState(r);
            ch2.LoadState(r);
            ch3.LoadState(r);
            ch4.LoadState(r);
            ClearSamples();
        }
    }
}
