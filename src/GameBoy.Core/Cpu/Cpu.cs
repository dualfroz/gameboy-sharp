namespace GameBoy.Cpu
{
    public sealed partial class Cpu
    {
        public const byte FlagZ = 0x80;
        public const byte FlagN = 0x40;
        public const byte FlagH = 0x20;
        public const byte FlagC = 0x10;

        public byte A, B, C, D, E, H, L, F;
        public ushort SP;
        public ushort PC;

        public bool Ime;
        public bool Halted;
        public bool Stopped;

        private bool imeScheduled;
        private bool haltBug;

        private readonly ISystemBus bus;
        private readonly InterruptController interrupts;

        public Cpu(ISystemBus bus)
        {
            this.bus = bus;
            this.interrupts = bus.Interrupts;
        }

        public void ResetPostBoot()
        {
            A = 0x01; F = 0xB0;
            B = 0x00; C = 0x13;
            D = 0x00; E = 0xD8;
            H = 0x01; L = 0x4D;
            SP = 0xFFFE;
            PC = 0x0100;
            Ime = false;
            Halted = false;
            Stopped = false;
            imeScheduled = false;
            haltBug = false;
        }

        public ushort AF
        {
            get => (ushort)((A << 8) | F);
            set { A = (byte)(value >> 8); F = (byte)(value & 0xF0); }
        }

        public ushort BC
        {
            get => (ushort)((B << 8) | C);
            set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
        }

        public ushort DE
        {
            get => (ushort)((D << 8) | E);
            set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
        }

        public ushort HL
        {
            get => (ushort)((H << 8) | L);
            set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
        }

        private bool FlagSet(byte flag) => (F & flag) != 0;

        private void SetFlag(byte flag, bool on)
        {
            if (on) F |= flag;
            else F = (byte)(F & ~flag);
        }

        private void SetFlags(bool z, bool n, bool h, bool c)
        {
            F = 0;
            if (z) F |= FlagZ;
            if (n) F |= FlagN;
            if (h) F |= FlagH;
            if (c) F |= FlagC;
        }

        public void Step()
        {
            if (HandleInterrupts())
                return;

            if (Halted)
            {
                bus.Tick4();
                return;
            }

            bool enableImeAfter = imeScheduled;

            byte opcode = Fetch8();
            Execute(opcode);

            if (enableImeAfter)
            {
                Ime = true;
                imeScheduled = false;
            }
        }

        private bool HandleInterrupts()
        {
            byte pending = interrupts.Pending;

            if (pending != 0 && Halted)
                Halted = false;

            if (pending == 0 || !Ime)
                return false;

            Ime = false;
            imeScheduled = false;

            bus.Tick4();
            bus.Tick4();

            SP--;
            bus.Write(SP, (byte)(PC >> 8));
            SP--;
            bus.Write(SP, (byte)(PC & 0xFF));

            byte live = interrupts.Pending;
            ushort vector = 0x0000;
            if (live != 0)
            {
                int bit = LowestSetBit(live);
                vector = (ushort)(0x40 + bit * 8);
                interrupts.Flag = (byte)(interrupts.Flag & ~(1 << bit));
            }

            bus.Tick4();
            PC = vector;
            return true;
        }

        private static int LowestSetBit(byte value)
        {
            for (int bit = 0; bit < 5; bit++)
                if ((value & (1 << bit)) != 0)
                    return bit;
            return 0;
        }

        private byte Fetch8()
        {
            byte value = bus.Read(PC);
            if (haltBug)
                haltBug = false;
            else
                PC++;
            return value;
        }

        private ushort Fetch16()
        {
            byte lo = Fetch8();
            byte hi = Fetch8();
            return (ushort)((hi << 8) | lo);
        }

        private byte Read8(ushort address) => bus.Read(address);

        private void Write8(ushort address, byte value) => bus.Write(address, value);

        private void InternalCycle() => bus.Tick4();

        private void Push16(ushort value)
        {
            SP--;
            Write8(SP, (byte)(value >> 8));
            SP--;
            Write8(SP, (byte)(value & 0xFF));
        }

        private ushort Pop16()
        {
            byte lo = Read8(SP);
            SP++;
            byte hi = Read8(SP);
            SP++;
            return (ushort)((hi << 8) | lo);
        }
    }
}
