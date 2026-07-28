using System.IO;

namespace GameBoy.Cartridge
{
    public sealed class Mbc3Rtc
    {
        private const long CyclesPerSecond = 4194304;

        private int seconds;
        private int minutes;
        private int hours;
        private int days;
        private bool halt;
        private bool carry;
        private long cycleAccumulator;

        private int latchedSeconds;
        private int latchedMinutes;
        private int latchedHours;
        private int latchedDays;
        private bool latchedHalt;
        private bool latchedCarry;

        private byte previousLatch = 0xFF;

        public void Step(int cycles)
        {
            if (halt)
                return;
            cycleAccumulator += cycles;
            while (cycleAccumulator >= CyclesPerSecond)
            {
                cycleAccumulator -= CyclesPerSecond;
                AddSecond();
            }
        }

        public void AddSeconds(long amount)
        {
            if (halt)
                return;
            for (long i = 0; i < amount; i++)
                AddSecond();
        }

        private void AddSecond()
        {
            seconds++;
            if (seconds < 60)
                return;
            seconds = 0;
            minutes++;
            if (minutes < 60)
                return;
            minutes = 0;
            hours++;
            if (hours < 24)
                return;
            hours = 0;
            days++;
            if (days < 512)
                return;
            days = 0;
            carry = true;
        }

        public void WriteLatch(byte value)
        {
            if (previousLatch == 0x00 && value == 0x01)
                Latch();
            previousLatch = value;
        }

        private void Latch()
        {
            latchedSeconds = seconds;
            latchedMinutes = minutes;
            latchedHours = hours;
            latchedDays = days;
            latchedHalt = halt;
            latchedCarry = carry;
        }

        public byte ReadRegister(int register)
        {
            return register switch
            {
                0x08 => (byte)latchedSeconds,
                0x09 => (byte)latchedMinutes,
                0x0A => (byte)latchedHours,
                0x0B => (byte)(latchedDays & 0xFF),
                0x0C => (byte)(((latchedDays >> 8) & 0x01) | (latchedHalt ? 0x40 : 0x00) | (latchedCarry ? 0x80 : 0x00)),
                _ => 0xFF
            };
        }

        public void WriteRegister(int register, byte value)
        {
            switch (register)
            {
                case 0x08:
                    seconds = value;
                    cycleAccumulator = 0;
                    break;
                case 0x09:
                    minutes = value;
                    break;
                case 0x0A:
                    hours = value;
                    break;
                case 0x0B:
                    days = (days & 0x100) | value;
                    break;
                case 0x0C:
                    days = (days & 0xFF) | ((value & 0x01) << 8);
                    halt = (value & 0x40) != 0;
                    carry = (value & 0x80) != 0;
                    break;
            }
        }

        public void SaveState(BinaryWriter w)
        {
            w.Write(seconds); w.Write(minutes); w.Write(hours); w.Write(days);
            w.Write(halt); w.Write(carry); w.Write(cycleAccumulator);
            w.Write(latchedSeconds); w.Write(latchedMinutes); w.Write(latchedHours); w.Write(latchedDays);
            w.Write(latchedHalt); w.Write(latchedCarry);
            w.Write(previousLatch);
        }

        public void LoadState(BinaryReader r)
        {
            seconds = r.ReadInt32(); minutes = r.ReadInt32(); hours = r.ReadInt32(); days = r.ReadInt32();
            halt = r.ReadBoolean(); carry = r.ReadBoolean(); cycleAccumulator = r.ReadInt64();
            latchedSeconds = r.ReadInt32(); latchedMinutes = r.ReadInt32(); latchedHours = r.ReadInt32(); latchedDays = r.ReadInt32();
            latchedHalt = r.ReadBoolean(); latchedCarry = r.ReadBoolean();
            previousLatch = r.ReadByte();
        }
    }
}
