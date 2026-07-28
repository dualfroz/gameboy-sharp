using System.IO;

namespace GameBoy.Audio
{
    internal sealed class WaveChannel
    {
        private readonly byte[] waveRam;

        private byte nr30, nr31, nr32, nr33, nr34;

        private int frequencyTimer;
        private int position;
        private int sampleBuffer;

        private bool enabled;
        private bool dacEnabled;

        private int lengthCounter;
        private bool lengthEnabled;

        public WaveChannel(byte[] waveRam)
        {
            this.waveRam = waveRam;
        }

        public bool Enabled => enabled;
        public bool DacEnabled => dacEnabled;

        private int Frequency => ((nr34 & 0x07) << 8) | nr33;

        private int VolumeShift => ((nr32 >> 5) & 0x03) switch
        {
            0 => 4,
            1 => 0,
            2 => 1,
            _ => 2
        };

        public int Output() => enabled ? (sampleBuffer >> VolumeShift) : 0;

        public float Analog() => dacEnabled ? (Output() / 15f * 2f - 1f) : 0f;

        public byte Read(int reg)
        {
            return reg switch
            {
                0 => nr30,
                1 => nr31,
                2 => nr32,
                3 => nr33,
                4 => nr34,
                _ => 0xFF
            };
        }

        public void Write(int reg, byte value)
        {
            switch (reg)
            {
                case 0:
                    nr30 = value;
                    dacEnabled = (value & 0x80) != 0;
                    if (!dacEnabled)
                        enabled = false;
                    break;
                case 1:
                    nr31 = value;
                    lengthCounter = 256 - value;
                    break;
                case 2:
                    nr32 = value;
                    break;
                case 3:
                    nr33 = value;
                    break;
                case 4:
                    nr34 = value;
                    lengthEnabled = (value & 0x40) != 0;
                    if ((value & 0x80) != 0)
                        Trigger();
                    break;
            }
        }

        public void Step(int cycles)
        {
            frequencyTimer -= cycles;
            while (frequencyTimer <= 0)
            {
                frequencyTimer += (2048 - Frequency) * 2;
                position = (position + 1) & 31;
                sampleBuffer = ReadNibble(position);
            }
        }

        private int ReadNibble(int index)
        {
            byte packed = waveRam[index >> 1];
            return (index & 1) == 0 ? (packed >> 4) : (packed & 0x0F);
        }

        public void ClockLength()
        {
            if (lengthEnabled && lengthCounter > 0)
            {
                lengthCounter--;
                if (lengthCounter == 0)
                    enabled = false;
            }
        }

        private void Trigger()
        {
            enabled = dacEnabled;
            if (lengthCounter == 0)
                lengthCounter = 256;
            frequencyTimer = (2048 - Frequency) * 2;
            position = 0;
            sampleBuffer = ReadNibble(0);
        }

        public void PowerOff()
        {
            nr30 = nr31 = nr32 = nr33 = nr34 = 0;
            frequencyTimer = 0;
            position = 0;
            sampleBuffer = 0;
            enabled = false;
            dacEnabled = false;
            lengthCounter = 0;
            lengthEnabled = false;
        }

        public void SaveState(BinaryWriter w)
        {
            w.Write(nr30); w.Write(nr31); w.Write(nr32); w.Write(nr33); w.Write(nr34);
            w.Write(frequencyTimer); w.Write(position); w.Write(sampleBuffer);
            w.Write(enabled); w.Write(dacEnabled);
            w.Write(lengthCounter); w.Write(lengthEnabled);
        }

        public void LoadState(BinaryReader r)
        {
            nr30 = r.ReadByte(); nr31 = r.ReadByte(); nr32 = r.ReadByte(); nr33 = r.ReadByte(); nr34 = r.ReadByte();
            frequencyTimer = r.ReadInt32(); position = r.ReadInt32(); sampleBuffer = r.ReadInt32();
            enabled = r.ReadBoolean(); dacEnabled = r.ReadBoolean();
            lengthCounter = r.ReadInt32(); lengthEnabled = r.ReadBoolean();
        }
    }
}
