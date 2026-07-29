using System.IO;
using GameBoy.Video;

namespace GameBoy.Memory
{
    public sealed class OamDma
    {
        private readonly Mmu mmu;
        private readonly Ppu ppu;

        private bool active;
        private int index;
        private int subDot;

        public byte Page { get; private set; }

        public OamDma(Mmu mmu, Ppu ppu)
        {
            this.mmu = mmu;
            this.ppu = ppu;
        }

        public void Start(byte page)
        {
            Page = page;
            active = true;
            index = 0;
            subDot = 0;
        }

        public void Step()
        {
            if (!active)
                return;

            subDot++;
            if (subDot < 4)
                return;

            subDot = 0;
            ushort source = (ushort)((Page << 8) | index);
            ppu.WriteOamDma(index, mmu.Read(source));
            index++;
            if (index == 0xA0)
                active = false;
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(active);
            writer.Write(index);
            writer.Write(subDot);
            writer.Write(Page);
        }

        public void LoadState(BinaryReader reader)
        {
            active = reader.ReadBoolean();
            index = reader.ReadInt32();
            subDot = reader.ReadInt32();
            Page = reader.ReadByte();
        }
    }
}
