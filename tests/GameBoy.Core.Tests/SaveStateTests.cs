using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests
{
    public sealed class SaveStateTests
    {
        private readonly ITestOutputHelper output;

        public SaveStateTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RestoreContinuesIdenticallyToUninterruptedRun()
        {
            byte[] rom = TestRoms.Load("cpu_instrs.gb");

            var reference = new Emulator(rom);
            for (int frame = 0; frame < 300; frame++)
                reference.RunFrame();

            byte[] snapshot = reference.SaveState();

            for (int frame = 0; frame < 150; frame++)
                reference.RunFrame();

            var restored = new Emulator(rom);
            restored.LoadState(snapshot);
            for (int frame = 0; frame < 150; frame++)
                restored.RunFrame();

            output.WriteLine($"snapshot size: {snapshot.Length} bytes");
            output.WriteLine($"reference cycles: {reference.TotalCycles}, restored cycles: {restored.TotalCycles}");

            Assert.Equal(reference.TotalCycles, restored.TotalCycles);
            Assert.Equal(reference.SerialOutput, restored.SerialOutput);
            Assert.Equal(reference.Framebuffer, restored.Framebuffer);

            Assert.Equal(reference.Cpu.AF, restored.Cpu.AF);
            Assert.Equal(reference.Cpu.BC, restored.Cpu.BC);
            Assert.Equal(reference.Cpu.DE, restored.Cpu.DE);
            Assert.Equal(reference.Cpu.HL, restored.Cpu.HL);
            Assert.Equal(reference.Cpu.SP, restored.Cpu.SP);
            Assert.Equal(reference.Cpu.PC, restored.Cpu.PC);
        }

        [Fact]
        public void SnapshotTakenMidTransferSurvivesRoundTrip()
        {
            byte[] rom = TestRoms.Load("dmg-acid2.gb");

            var reference = new Emulator(rom);
            for (int frame = 0; frame < 120; frame++)
                reference.RunFrame();

            byte[] snapshot = reference.SaveState();
            var restored = new Emulator(rom);
            restored.LoadState(snapshot);

            for (int frame = 0; frame < 90; frame++)
            {
                reference.RunFrame();
                restored.RunFrame();
            }

            Assert.Equal(reference.Framebuffer, restored.Framebuffer);
            Assert.Equal(reference.Cpu.PC, restored.Cpu.PC);
        }
    }
}
