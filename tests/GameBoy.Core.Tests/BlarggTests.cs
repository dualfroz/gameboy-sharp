using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests
{
    public sealed class BlarggTests
    {
        private const int MaxFrames = 4000;
        private readonly ITestOutputHelper output;

        public BlarggTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CpuInstrs_ReportsPassedOverSerial()
        {
            string serial = RunUntilSerialResult(TestRoms.Load("cpu_instrs.gb"));
            output.WriteLine(serial);
            Assert.Contains("Passed", serial);
            Assert.DoesNotContain("Failed", serial);
        }

        [Fact]
        public void InstrTiming_ReportsPassedOverSerial()
        {
            string serial = RunUntilSerialResult(TestRoms.Load("instr_timing.gb"));
            output.WriteLine(serial);
            Assert.Contains("Passed", serial);
            Assert.DoesNotContain("Failed", serial);
        }

        private static string RunUntilSerialResult(byte[] rom)
        {
            var emulator = new Emulator(rom);
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                emulator.RunFrame();
                string serial = emulator.SerialOutput;
                if (serial.Contains("Passed") || serial.Contains("Failed"))
                    return serial;
            }
            return emulator.SerialOutput;
        }
    }
}
