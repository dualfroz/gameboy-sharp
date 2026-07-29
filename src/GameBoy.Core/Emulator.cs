using GameBoy.Audio;
using GameBoy.Cpu;
using GameBoy.Input;
using GameBoy.Memory;
using GameBoy.Serial;
using GameBoy.Timing;
using GameBoy.Video;
using CpuCore = GameBoy.Cpu.Cpu;
using CartridgeInfo = GameBoy.Cartridge.Cartridge;

namespace GameBoy
{
    public sealed partial class Emulator : ISystemBus
    {
        public const int ScreenWidth = Ppu.ScreenWidth;
        public const int ScreenHeight = Ppu.ScreenHeight;

        private const int CyclesPerFrame = 70224;

        private readonly InterruptController interrupts = new InterruptController();
        private readonly Ppu ppu;
        private readonly Timer timer;
        private readonly Joypad joypad;
        private readonly SerialPort serial;
        private readonly Apu apu;
        private readonly Mmu mmu;
        private readonly OamDma dma;
        private readonly CpuCore cpu;

        public CpuCore Cpu => cpu;
        public Ppu Ppu => ppu;
        public Joypad Joypad => joypad;
        public CartridgeInfo Cartridge { get; }
        public byte[] Framebuffer => ppu.Framebuffer;
        public string SerialOutput => serial.Output;
        public InterruptController Interrupts => interrupts;
        public long TotalCycles { get; private set; }

        public Emulator(byte[] rom)
        {
            Cartridge = new CartridgeInfo(rom);
            ppu = new Ppu(interrupts);
            timer = new Timer(interrupts);
            joypad = new Joypad(interrupts);
            serial = new SerialPort(interrupts);
            apu = new Apu();
            mmu = new Mmu(Cartridge, ppu, timer, joypad, serial, apu, interrupts);
            dma = new OamDma(mmu, ppu);
            mmu.Dma = dma;
            cpu = new CpuCore(this);

            cpu.ResetPostBoot();
            InitPostBootIo();
        }

        private void InitPostBootIo()
        {
            ppu.WriteRegister(0xFF47, 0xFC);
            ppu.WriteRegister(0xFF48, 0xFF);
            ppu.WriteRegister(0xFF49, 0xFF);
            ppu.WriteRegister(0xFF40, 0x91);
        }

        byte ISystemBus.Read(ushort address)
        {
            Tick4();
            return mmu.Read(address);
        }

        void ISystemBus.Write(ushort address, byte value)
        {
            Tick4();
            mmu.Write(address, value);
        }

        public void Tick4()
        {
            for (int i = 0; i < 4; i++)
            {
                timer.Step();
                ppu.Step();
                dma.Step();
            }
            apu.Step(4);
            Cartridge.Step(4);
            TotalCycles += 4;
        }

        public void StepInstruction()
        {
            cpu.Step();
        }

        public void RunFrame()
        {
            ppu.ConsumeFrame();
            long deadline = TotalCycles + CyclesPerFrame * 2;
            while (!ppu.FrameReady && TotalCycles < deadline)
                cpu.Step();
        }

        public void RunCycles(long cycles)
        {
            long deadline = TotalCycles + cycles;
            while (TotalCycles < deadline)
                cpu.Step();
        }
    }
}
