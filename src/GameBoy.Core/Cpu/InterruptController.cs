using System.IO;

namespace GameBoy.Cpu
{
    public enum Interrupt
    {
        VBlank = 0x01,
        LcdStat = 0x02,
        Timer = 0x04,
        Serial = 0x08,
        Joypad = 0x10
    }

    public sealed class InterruptController
    {
        public byte Enable;
        public byte Flag;

        public byte ReadFlag()
        {
            return (byte)(Flag | 0xE0);
        }

        public void WriteFlag(byte value)
        {
            Flag = (byte)(value & 0x1F);
        }

        public void Request(Interrupt which)
        {
            Flag |= (byte)which;
        }

        public byte Pending => (byte)(Enable & Flag & 0x1F);

        public bool AnyPending => Pending != 0;

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(Enable);
            writer.Write(Flag);
        }

        public void LoadState(BinaryReader reader)
        {
            Enable = reader.ReadByte();
            Flag = reader.ReadByte();
        }
    }
}
