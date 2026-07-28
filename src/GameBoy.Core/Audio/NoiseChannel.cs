using System.IO;

namespace GameBoy.Audio
{
    internal sealed class NoiseChannel
    {
        private static readonly int[] Divisors = { 8, 16, 32, 48, 64, 80, 96, 112 };

        private byte nr41, nr42, nr43, nr44;

        private int frequencyTimer;
        private int lfsr = 0x7FFF;

        private bool enabled;
        private bool dacEnabled;

        private int lengthCounter;
        private bool lengthEnabled;

        private int volume;
        private int envelopePeriod;
        private bool envelopeAdd;
        private int envelopeTimer;

        public bool Enabled => enabled;
        public bool DacEnabled => dacEnabled;

        private int Period => Divisors[nr43 & 0x07] << (nr43 >> 4);
        private bool WidthMode => (nr43 & 0x08) != 0;

        public int Output() => enabled ? ((~lfsr & 1) * volume) : 0;

        public float Analog() => dacEnabled ? (Output() / 15f * 2f - 1f) : 0f;

        public byte Read(int reg)
        {
            return reg switch
            {
                1 => nr41,
                2 => nr42,
                3 => nr43,
                4 => nr44,
                _ => 0xFF
            };
        }

        public void Write(int reg, byte value)
        {
            switch (reg)
            {
                case 1:
                    nr41 = value;
                    lengthCounter = 64 - (value & 0x3F);
                    break;
                case 2:
                    nr42 = value;
                    dacEnabled = (value & 0xF8) != 0;
                    if (!dacEnabled)
                        enabled = false;
                    envelopePeriod = value & 0x07;
                    envelopeAdd = (value & 0x08) != 0;
                    break;
                case 3:
                    nr43 = value;
                    break;
                case 4:
                    nr44 = value;
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
                frequencyTimer += Period;
                ClockLfsr();
            }
        }

        private void ClockLfsr()
        {
            int xor = (lfsr & 1) ^ ((lfsr >> 1) & 1);
            lfsr >>= 1;
            lfsr |= xor << 14;
            if (WidthMode)
            {
                lfsr &= ~(1 << 6);
                lfsr |= xor << 6;
            }
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

        public void ClockEnvelope()
        {
            if (envelopePeriod == 0)
                return;
            if (envelopeTimer > 0)
                envelopeTimer--;
            if (envelopeTimer == 0)
            {
                envelopeTimer = envelopePeriod;
                int next = volume + (envelopeAdd ? 1 : -1);
                if (next >= 0 && next <= 15)
                    volume = next;
            }
        }

        private void Trigger()
        {
            enabled = dacEnabled;
            if (lengthCounter == 0)
                lengthCounter = 64;
            frequencyTimer = Period;
            envelopeTimer = envelopePeriod;
            volume = nr42 >> 4;
            lfsr = 0x7FFF;
        }

        public void PowerOff()
        {
            nr41 = nr42 = nr43 = nr44 = 0;
            frequencyTimer = 0;
            lfsr = 0x7FFF;
            enabled = false;
            dacEnabled = false;
            lengthCounter = 0;
            lengthEnabled = false;
            volume = 0;
            envelopePeriod = 0;
            envelopeAdd = false;
            envelopeTimer = 0;
        }

        public void SaveState(BinaryWriter w)
        {
            w.Write(nr41); w.Write(nr42); w.Write(nr43); w.Write(nr44);
            w.Write(frequencyTimer); w.Write(lfsr);
            w.Write(enabled); w.Write(dacEnabled);
            w.Write(lengthCounter); w.Write(lengthEnabled);
            w.Write(volume); w.Write(envelopePeriod); w.Write(envelopeAdd); w.Write(envelopeTimer);
        }

        public void LoadState(BinaryReader r)
        {
            nr41 = r.ReadByte(); nr42 = r.ReadByte(); nr43 = r.ReadByte(); nr44 = r.ReadByte();
            frequencyTimer = r.ReadInt32(); lfsr = r.ReadInt32();
            enabled = r.ReadBoolean(); dacEnabled = r.ReadBoolean();
            lengthCounter = r.ReadInt32(); lengthEnabled = r.ReadBoolean();
            volume = r.ReadInt32(); envelopePeriod = r.ReadInt32(); envelopeAdd = r.ReadBoolean(); envelopeTimer = r.ReadInt32();
        }
    }
}
