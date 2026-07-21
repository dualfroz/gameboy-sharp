using System.IO;
using GameBoy.Cpu;

namespace GameBoy.Timing
{
    public sealed class Timer
    {
        private readonly InterruptController interrupts;

        private ushort counter;
        private byte tima;
        private byte tma;
        private byte tac;
        private bool previousEdge;
        private int reloadCountdown;

        public Timer(InterruptController interrupts)
        {
            this.interrupts = interrupts;
            counter = 0xABCC;
        }

        public void Step()
        {
            if (reloadCountdown > 0)
            {
                reloadCountdown--;
                if (reloadCountdown == 0)
                {
                    tima = tma;
                    interrupts.Request(Interrupt.Timer);
                }
            }

            counter = (ushort)(counter + 1);
            UpdateEdge();
        }

        private void UpdateEdge()
        {
            bool edge = ((counter >> SelectedBit()) & 1) != 0 && (tac & 0x04) != 0;
            if (previousEdge && !edge)
                IncrementTima();
            previousEdge = edge;
        }

        private void IncrementTima()
        {
            if (tima == 0xFF)
            {
                tima = 0;
                reloadCountdown = 4;
            }
            else
            {
                tima++;
            }
        }

        private int SelectedBit()
        {
            return (tac & 0x03) switch
            {
                0 => 9,
                1 => 3,
                2 => 5,
                _ => 7
            };
        }

        public byte Read(ushort address)
        {
            return (address & 0xFF) switch
            {
                0x04 => (byte)(counter >> 8),
                0x05 => tima,
                0x06 => tma,
                0x07 => (byte)(tac | 0xF8),
                _ => 0xFF
            };
        }

        public void Write(ushort address, byte value)
        {
            switch (address & 0xFF)
            {
                case 0x04:
                    counter = 0;
                    UpdateEdge();
                    break;
                case 0x05:
                    if (reloadCountdown > 0)
                        reloadCountdown = 0;
                    tima = value;
                    break;
                case 0x06:
                    tma = value;
                    break;
                case 0x07:
                    tac = (byte)(value & 0x07);
                    UpdateEdge();
                    break;
            }
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(counter);
            writer.Write(tima);
            writer.Write(tma);
            writer.Write(tac);
            writer.Write(previousEdge);
            writer.Write(reloadCountdown);
        }

        public void LoadState(BinaryReader reader)
        {
            counter = reader.ReadUInt16();
            tima = reader.ReadByte();
            tma = reader.ReadByte();
            tac = reader.ReadByte();
            previousEdge = reader.ReadBoolean();
            reloadCountdown = reader.ReadInt32();
        }
    }
}
