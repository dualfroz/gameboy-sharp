using System.IO;

namespace GameBoy.Audio
{
    internal sealed class SquareChannel
    {
        private static readonly byte[][] DutyTable =
        {
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
            new byte[] { 1, 0, 0, 0, 0, 0, 0, 1 },
            new byte[] { 1, 0, 0, 0, 0, 1, 1, 1 },
            new byte[] { 0, 1, 1, 1, 1, 1, 1, 0 }
        };

        private readonly bool hasSweep;

        private byte nrx0, nrx1, nrx2, nrx3, nrx4;

        private int frequencyTimer;
        private int dutyStep;
        private int duty;

        private bool enabled;
        private bool dacEnabled;

        private int lengthCounter;
        private bool lengthEnabled;

        private int volume;
        private int envelopePeriod;
        private bool envelopeAdd;
        private int envelopeTimer;

        private int sweepShadow;
        private int sweepTimer;
        private bool sweepEnabled;
        private int sweepPeriod;
        private bool sweepNegate;
        private int sweepShift;

        public SquareChannel(bool hasSweep)
        {
            this.hasSweep = hasSweep;
        }

        public bool Enabled => enabled;
        public bool DacEnabled => dacEnabled;

        private int Frequency => ((nrx4 & 0x07) << 8) | nrx3;

        public int Output() => enabled ? DutyTable[duty][dutyStep] * volume : 0;

        public float Analog() => dacEnabled ? (Output() / 15f * 2f - 1f) : 0f;

        public byte Read(int reg)
        {
            return reg switch
            {
                0 => nrx0,
                1 => nrx1,
                2 => nrx2,
                3 => nrx3,
                4 => nrx4,
                _ => 0xFF
            };
        }

        public void Write(int reg, byte value)
        {
            switch (reg)
            {
                case 0:
                    nrx0 = value;
                    break;
                case 1:
                    nrx1 = value;
                    duty = value >> 6;
                    lengthCounter = 64 - (value & 0x3F);
                    break;
                case 2:
                    nrx2 = value;
                    dacEnabled = (value & 0xF8) != 0;
                    if (!dacEnabled)
                        enabled = false;
                    envelopePeriod = value & 0x07;
                    envelopeAdd = (value & 0x08) != 0;
                    break;
                case 3:
                    nrx3 = value;
                    break;
                case 4:
                    nrx4 = value;
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
                frequencyTimer += (2048 - Frequency) * 4;
                dutyStep = (dutyStep + 1) & 7;
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

        public void ClockSweep()
        {
            if (!hasSweep)
                return;
            if (sweepTimer > 0)
                sweepTimer--;
            if (sweepTimer != 0)
                return;

            sweepTimer = sweepPeriod > 0 ? sweepPeriod : 8;
            if (!sweepEnabled || sweepPeriod == 0)
                return;

            int candidate = NextSweepFrequency();
            if (candidate <= 2047 && sweepShift > 0)
            {
                sweepShadow = candidate;
                nrx3 = (byte)(candidate & 0xFF);
                nrx4 = (byte)((nrx4 & 0xF8) | ((candidate >> 8) & 0x07));
                NextSweepFrequency();
            }
        }

        private int NextSweepFrequency()
        {
            int delta = sweepShadow >> sweepShift;
            int candidate = sweepNegate ? sweepShadow - delta : sweepShadow + delta;
            if (candidate > 2047)
                enabled = false;
            return candidate;
        }

        private void Trigger()
        {
            enabled = dacEnabled;
            if (lengthCounter == 0)
                lengthCounter = 64;
            frequencyTimer = (2048 - Frequency) * 4;
            envelopeTimer = envelopePeriod;
            volume = nrx2 >> 4;

            if (hasSweep)
            {
                sweepShadow = Frequency;
                sweepPeriod = (nrx0 >> 4) & 0x07;
                sweepNegate = (nrx0 & 0x08) != 0;
                sweepShift = nrx0 & 0x07;
                sweepTimer = sweepPeriod > 0 ? sweepPeriod : 8;
                sweepEnabled = sweepPeriod > 0 || sweepShift > 0;
                if (sweepShift > 0)
                    NextSweepFrequency();
            }
        }

        public void PowerOff()
        {
            nrx0 = nrx1 = nrx2 = nrx3 = nrx4 = 0;
            frequencyTimer = 0;
            dutyStep = 0;
            duty = 0;
            enabled = false;
            dacEnabled = false;
            lengthCounter = 0;
            lengthEnabled = false;
            volume = 0;
            envelopePeriod = 0;
            envelopeAdd = false;
            envelopeTimer = 0;
            sweepShadow = 0;
            sweepTimer = 0;
            sweepEnabled = false;
            sweepPeriod = 0;
            sweepNegate = false;
            sweepShift = 0;
        }

        public void SaveState(BinaryWriter w)
        {
            w.Write(nrx0); w.Write(nrx1); w.Write(nrx2); w.Write(nrx3); w.Write(nrx4);
            w.Write(frequencyTimer); w.Write(dutyStep); w.Write(duty);
            w.Write(enabled); w.Write(dacEnabled);
            w.Write(lengthCounter); w.Write(lengthEnabled);
            w.Write(volume); w.Write(envelopePeriod); w.Write(envelopeAdd); w.Write(envelopeTimer);
            w.Write(sweepShadow); w.Write(sweepTimer); w.Write(sweepEnabled);
            w.Write(sweepPeriod); w.Write(sweepNegate); w.Write(sweepShift);
        }

        public void LoadState(BinaryReader r)
        {
            nrx0 = r.ReadByte(); nrx1 = r.ReadByte(); nrx2 = r.ReadByte(); nrx3 = r.ReadByte(); nrx4 = r.ReadByte();
            frequencyTimer = r.ReadInt32(); dutyStep = r.ReadInt32(); duty = r.ReadInt32();
            enabled = r.ReadBoolean(); dacEnabled = r.ReadBoolean();
            lengthCounter = r.ReadInt32(); lengthEnabled = r.ReadBoolean();
            volume = r.ReadInt32(); envelopePeriod = r.ReadInt32(); envelopeAdd = r.ReadBoolean(); envelopeTimer = r.ReadInt32();
            sweepShadow = r.ReadInt32(); sweepTimer = r.ReadInt32(); sweepEnabled = r.ReadBoolean();
            sweepPeriod = r.ReadInt32(); sweepNegate = r.ReadBoolean(); sweepShift = r.ReadInt32();
        }
    }
}
