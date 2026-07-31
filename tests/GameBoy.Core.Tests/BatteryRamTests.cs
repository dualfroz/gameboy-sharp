using Xunit;

namespace GameBoy.Tests
{
    public sealed class BatteryRamTests
    {
        private static byte PatternByte(int bank, int offset)
        {
            return (byte)((bank * 31 + (offset & 0xFF)) ^ 0x5A);
        }

        private static void FillRam(Emulator emulator, int banks)
        {
            emulator.WriteMemory(0x0000, 0x0A);
            for (int bank = 0; bank < banks; bank++)
            {
                emulator.WriteMemory(0x4000, (byte)bank);
                for (int offset = 0; offset < 0x2000; offset++)
                    emulator.WriteMemory((ushort)(0xA000 + offset), PatternByte(bank, offset));
            }
        }

        private static void AssertRamMatches(Emulator emulator, int banks)
        {
            emulator.WriteMemory(0x0000, 0x0A);
            for (int bank = 0; bank < banks; bank++)
            {
                emulator.WriteMemory(0x4000, (byte)bank);
                for (int offset = 0; offset < 0x2000; offset++)
                    Assert.Equal(PatternByte(bank, offset), emulator.ReadMemory((ushort)(0xA000 + offset)));
            }
        }

        private static void RoundTrip(byte cartridgeType, byte ramSizeCode, int banks)
        {
            var source = new Emulator(TestRoms.BuildCartridge(cartridgeType, ramSizeCode));
            Assert.True(source.HasBattery);

            FillRam(source, banks);
            byte[] saved = source.SaveRam();
            Assert.Equal(banks * 0x2000, saved.Length);

            var loaded = new Emulator(TestRoms.BuildCartridge(cartridgeType, ramSizeCode));
            loaded.LoadRam(saved);
            AssertRamMatches(loaded, banks);
        }

        [Fact]
        public void Mbc1RamRoundTrips()
        {
            RoundTrip(0x03, 0x02, 1);
        }

        [Fact]
        public void Mbc3RamRoundTrips()
        {
            RoundTrip(0x13, 0x03, 4);
        }

        [Fact]
        public void Mbc5RamRoundTrips()
        {
            RoundTrip(0x1B, 0x03, 4);
        }

        [Fact]
        public void PlainRomReportsNoBattery()
        {
            var emulator = new Emulator(TestRoms.BuildProgram());
            Assert.False(emulator.HasBattery);
        }
    }
}
