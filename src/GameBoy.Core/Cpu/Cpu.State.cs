using System.IO;

namespace GameBoy.Cpu
{
    public sealed partial class Cpu
    {
        public void SaveState(BinaryWriter writer)
        {
            writer.Write(A); writer.Write(B); writer.Write(C); writer.Write(D);
            writer.Write(E); writer.Write(H); writer.Write(L); writer.Write(F);
            writer.Write(SP); writer.Write(PC);
            writer.Write(Ime); writer.Write(Halted); writer.Write(Stopped);
            writer.Write(imeScheduled); writer.Write(haltBug);
        }

        public void LoadState(BinaryReader reader)
        {
            A = reader.ReadByte(); B = reader.ReadByte(); C = reader.ReadByte(); D = reader.ReadByte();
            E = reader.ReadByte(); H = reader.ReadByte(); L = reader.ReadByte(); F = reader.ReadByte();
            SP = reader.ReadUInt16(); PC = reader.ReadUInt16();
            Ime = reader.ReadBoolean(); Halted = reader.ReadBoolean(); Stopped = reader.ReadBoolean();
            imeScheduled = reader.ReadBoolean(); haltBug = reader.ReadBoolean();
        }
    }
}
