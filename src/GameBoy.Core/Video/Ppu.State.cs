using System;
using System.IO;

namespace GameBoy.Video
{
    public sealed partial class Ppu
    {
        public void SaveState(BinaryWriter writer)
        {
            writer.Write(lcdc); writer.Write(statEnables);
            writer.Write(scy); writer.Write(scx);
            writer.Write(ly); writer.Write(lyc);
            writer.Write(bgp); writer.Write(obp0); writer.Write(obp1);
            writer.Write(wy); writer.Write(wx);
            writer.Write(dotCounter); writer.Write(mode); writer.Write(windowLine);
            writer.Write(statLine); writer.Write(FrameReady);
            writer.Write(vram);
            writer.Write(oam);
            writer.Write(Framebuffer);
        }

        public void LoadState(BinaryReader reader)
        {
            lcdc = reader.ReadByte(); statEnables = reader.ReadByte();
            scy = reader.ReadByte(); scx = reader.ReadByte();
            ly = reader.ReadByte(); lyc = reader.ReadByte();
            bgp = reader.ReadByte(); obp0 = reader.ReadByte(); obp1 = reader.ReadByte();
            wy = reader.ReadByte(); wx = reader.ReadByte();
            dotCounter = reader.ReadInt32(); mode = reader.ReadInt32(); windowLine = reader.ReadInt32();
            statLine = reader.ReadBoolean(); FrameReady = reader.ReadBoolean();
            Array.Copy(reader.ReadBytes(vram.Length), vram, vram.Length);
            Array.Copy(reader.ReadBytes(oam.Length), oam, oam.Length);
            Array.Copy(reader.ReadBytes(Framebuffer.Length), Framebuffer, Framebuffer.Length);
        }
    }
}
