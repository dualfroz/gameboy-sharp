using System.IO;
using GameBoy.Cpu;

namespace GameBoy.Input
{
    public enum JoypadButton
    {
        Right = 0,
        Left = 1,
        Up = 2,
        Down = 3,
        A = 4,
        B = 5,
        Select = 6,
        Start = 7
    }

    public sealed class Joypad
    {
        private readonly InterruptController interrupts;
        private byte selectLines = 0x30;
        private byte directions;
        private byte actions;

        public Joypad(InterruptController interrupts)
        {
            this.interrupts = interrupts;
        }

        public void SetButton(JoypadButton button, bool pressed)
        {
            int index = (int)button;
            byte mask = (byte)(1 << (index & 3));
            bool isDirection = index < 4;
            byte before = ReadLines();

            if (isDirection)
                directions = pressed ? (byte)(directions | mask) : (byte)(directions & ~mask);
            else
                actions = pressed ? (byte)(actions | mask) : (byte)(actions & ~mask);

            byte after = ReadLines();
            if ((before & ~after & 0x0F) != 0)
                interrupts.Request(Interrupt.Joypad);
        }

        private byte ReadLines()
        {
            byte lines = 0x0F;
            if ((selectLines & 0x10) == 0)
                lines &= (byte)~directions;
            if ((selectLines & 0x20) == 0)
                lines &= (byte)~actions;
            return lines;
        }

        public byte Read()
        {
            return (byte)(0xC0 | selectLines | ReadLines());
        }

        public void Write(byte value)
        {
            selectLines = (byte)(value & 0x30);
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(selectLines);
            writer.Write(directions);
            writer.Write(actions);
        }

        public void LoadState(BinaryReader reader)
        {
            selectLines = reader.ReadByte();
            directions = reader.ReadByte();
            actions = reader.ReadByte();
        }
    }
}
