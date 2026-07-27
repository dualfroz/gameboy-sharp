using GameBoy.Cpu;

namespace GameBoy.Video
{
    public sealed partial class Ppu
    {
        public const int ScreenWidth = 160;
        public const int ScreenHeight = 144;

        private const int OamScanDots = 80;
        private const int DrawDots = 172;
        private const int DotsPerLine = 456;
        private const int VisibleLines = 144;
        private const int TotalLines = 154;

        private readonly InterruptController interrupts;
        private readonly byte[] vram = new byte[0x2000];
        private readonly byte[] oam = new byte[0xA0];

        public readonly byte[] Framebuffer = new byte[ScreenWidth * ScreenHeight];

        private byte lcdc;
        private byte statEnables;
        private byte scy, scx;
        private byte ly, lyc;
        private byte bgp, obp0, obp1;
        private byte wy, wx;

        private int dotCounter;
        private int mode = 2;
        private int windowLine;
        private bool statLine;

        public bool FrameReady { get; private set; }

        public Ppu(InterruptController interrupts)
        {
            this.interrupts = interrupts;
        }

        public void ConsumeFrame() => FrameReady = false;

        private bool LcdEnabled => (lcdc & 0x80) != 0;

        public void Step()
        {
            if (!LcdEnabled)
                return;

            dotCounter++;
            if (dotCounter == DotsPerLine)
            {
                dotCounter = 0;
                ly++;
                if (ly == TotalLines)
                {
                    ly = 0;
                    windowLine = 0;
                }

                if (ly < VisibleLines)
                    mode = 2;
                else if (ly == VisibleLines)
                {
                    mode = 1;
                    FrameReady = true;
                    interrupts.Request(Interrupt.VBlank);
                }
            }
            else if (ly < VisibleLines)
            {
                if (dotCounter == OamScanDots)
                {
                    mode = 3;
                    RenderScanline();
                }
                else if (dotCounter == OamScanDots + DrawDots)
                {
                    mode = 0;
                }
            }

            UpdateStatInterrupt();
        }

        private void UpdateStatInterrupt()
        {
            bool lycEqual = ly == lyc;
            bool line =
                (mode == 0 && (statEnables & 0x08) != 0) ||
                (mode == 2 && (statEnables & 0x20) != 0) ||
                (mode == 1 && (statEnables & 0x10) != 0) ||
                (lycEqual && (statEnables & 0x40) != 0);

            if (line && !statLine)
                interrupts.Request(Interrupt.LcdStat);
            statLine = line;
        }

        public byte ReadVram(ushort address)
        {
            if (LcdEnabled && mode == 3)
                return 0xFF;
            return vram[address - 0x8000];
        }

        public void WriteVram(ushort address, byte value)
        {
            if (LcdEnabled && mode == 3)
                return;
            vram[address - 0x8000] = value;
        }

        public byte ReadOam(ushort address)
        {
            if (LcdEnabled && (mode == 2 || mode == 3))
                return 0xFF;
            return oam[address - 0xFE00];
        }

        public void WriteOam(ushort address, byte value)
        {
            if (LcdEnabled && (mode == 2 || mode == 3))
                return;
            oam[address - 0xFE00] = value;
        }

        public void WriteOamDma(int index, byte value)
        {
            oam[index] = value;
        }

        public byte ReadRegister(ushort address)
        {
            switch (address)
            {
                case 0xFF40: return lcdc;
                case 0xFF41: return (byte)(0x80 | statEnables | (ly == lyc ? 0x04 : 0x00) | mode);
                case 0xFF42: return scy;
                case 0xFF43: return scx;
                case 0xFF44: return ly;
                case 0xFF45: return lyc;
                case 0xFF47: return bgp;
                case 0xFF48: return obp0;
                case 0xFF49: return obp1;
                case 0xFF4A: return wy;
                case 0xFF4B: return wx;
                default: return 0xFF;
            }
        }

        public void WriteRegister(ushort address, byte value)
        {
            switch (address)
            {
                case 0xFF40:
                    bool wasOn = LcdEnabled;
                    lcdc = value;
                    if (wasOn && !LcdEnabled)
                        TurnOff();
                    else if (!wasOn && LcdEnabled)
                        TurnOn();
                    break;
                case 0xFF41: statEnables = (byte)(value & 0x78); break;
                case 0xFF42: scy = value; break;
                case 0xFF43: scx = value; break;
                case 0xFF45: lyc = value; break;
                case 0xFF47: bgp = value; break;
                case 0xFF48: obp0 = value; break;
                case 0xFF49: obp1 = value; break;
                case 0xFF4A: wy = value; break;
                case 0xFF4B: wx = value; break;
            }
        }

        private void TurnOff()
        {
            ly = 0;
            dotCounter = 0;
            mode = 0;
            windowLine = 0;
            statLine = false;
        }

        private void TurnOn()
        {
            ly = 0;
            dotCounter = 0;
            mode = 2;
            windowLine = 0;
        }
    }
}
