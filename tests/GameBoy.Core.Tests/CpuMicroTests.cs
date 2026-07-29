using Xunit;

namespace GameBoy.Tests
{
    public sealed class CpuMicroTests
    {
        private static Emulator Run(int instructions, params byte[] program)
        {
            var emulator = new Emulator(TestRoms.BuildProgram(program));
            for (int i = 0; i < instructions; i++)
                emulator.StepInstruction();
            return emulator;
        }

        [Fact]
        public void AddProducesZeroHalfAndCarryFlags()
        {
            var emulator = Run(3, 0x3E, 0x3A, 0x06, 0xC6, 0x80);
            Assert.Equal(0x00, emulator.Cpu.A);
            Assert.Equal(0xB0, emulator.Cpu.F);
        }

        [Fact]
        public void PushThenPopMovesWordThroughStack()
        {
            var emulator = Run(4, 0x31, 0xFE, 0xFF, 0x01, 0x34, 0x12, 0xC5, 0xD1);
            Assert.Equal(0x1234, emulator.Cpu.DE);
        }

        [Fact]
        public void DaaConvertsBinaryAddToPackedBcd()
        {
            var emulator = Run(4, 0x3E, 0x45, 0x06, 0x38, 0x80, 0x27);
            Assert.Equal(0x83, emulator.Cpu.A);
        }

        [Fact]
        public void IncSetsHalfCarryOnNibbleRollover()
        {
            var emulator = Run(2, 0x3E, 0x0F, 0x3C);
            Assert.Equal(0x10, emulator.Cpu.A);
            Assert.True((emulator.Cpu.F & 0x20) != 0);
            Assert.True((emulator.Cpu.F & 0x40) == 0);
        }

        [Fact]
        public void SwapExchangesNibbles()
        {
            var emulator = Run(3, 0x3E, 0x3C, 0xCB, 0x37, 0x00);
            Assert.Equal(0xC3, emulator.Cpu.A);
        }
    }
}
