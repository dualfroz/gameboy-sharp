using Xunit;

namespace GameBoy.Tests
{
    public sealed class Mbc3RtcTests
    {
        private const byte Mbc3TimerRamBattery = 0x10;

        private static Emulator NewTimerCartridge()
        {
            return new Emulator(TestRoms.BuildCartridge(Mbc3TimerRamBattery, 0x03));
        }

        private static void EnableRtc(Emulator emulator)
        {
            emulator.WriteMemory(0x0000, 0x0A);
        }

        private static void SelectRtcRegister(Emulator emulator, byte register)
        {
            emulator.WriteMemory(0x4000, register);
        }

        private static void Latch(Emulator emulator)
        {
            emulator.WriteMemory(0x6000, 0x00);
            emulator.WriteMemory(0x6000, 0x01);
        }

        private static byte ReadLatched(Emulator emulator, byte register)
        {
            SelectRtcRegister(emulator, register);
            return emulator.ReadMemory(0xA000);
        }

        private static void WriteRtc(Emulator emulator, byte register, byte value)
        {
            SelectRtcRegister(emulator, register);
            emulator.WriteMemory(0xA000, value);
        }

        [Fact]
        public void CartridgeReportsRtcAndBattery()
        {
            var emulator = NewTimerCartridge();
            Assert.True(emulator.HasRtc);
            Assert.True(emulator.HasBattery);
        }

        [Fact]
        public void SecondsRollIntoMinutes()
        {
            var emulator = NewTimerCartridge();
            EnableRtc(emulator);

            WriteRtc(emulator, 0x08, 58);
            WriteRtc(emulator, 0x09, 0);

            emulator.AdvanceRtcSeconds(5);
            Latch(emulator);

            Assert.Equal(3, ReadLatched(emulator, 0x08));
            Assert.Equal(1, ReadLatched(emulator, 0x09));
        }

        [Fact]
        public void LatchFreezesTheReadableTime()
        {
            var emulator = NewTimerCartridge();
            EnableRtc(emulator);

            WriteRtc(emulator, 0x08, 10);
            Latch(emulator);
            Assert.Equal(10, ReadLatched(emulator, 0x08));

            emulator.AdvanceRtcSeconds(20);
            Assert.Equal(10, ReadLatched(emulator, 0x08));

            Latch(emulator);
            Assert.Equal(30, ReadLatched(emulator, 0x08));
        }

        [Fact]
        public void HaltStopsTheClock()
        {
            var emulator = NewTimerCartridge();
            EnableRtc(emulator);

            WriteRtc(emulator, 0x08, 0);
            WriteRtc(emulator, 0x0C, 0x40);

            emulator.AdvanceRtcSeconds(30);
            Latch(emulator);
            Assert.Equal(0, ReadLatched(emulator, 0x08));

            WriteRtc(emulator, 0x0C, 0x00);
            emulator.AdvanceRtcSeconds(30);
            Latch(emulator);
            Assert.Equal(30, ReadLatched(emulator, 0x08));
        }

        [Fact]
        public void DayCounterOverflowSetsCarry()
        {
            var emulator = NewTimerCartridge();
            EnableRtc(emulator);

            WriteRtc(emulator, 0x08, 0);
            WriteRtc(emulator, 0x09, 0);
            WriteRtc(emulator, 0x0A, 0);
            WriteRtc(emulator, 0x0B, 0xFF);
            WriteRtc(emulator, 0x0C, 0x01);

            emulator.AdvanceRtcSeconds(86400);
            Latch(emulator);

            Assert.Equal(0x00, ReadLatched(emulator, 0x0B));
            byte dayHigh = ReadLatched(emulator, 0x0C);
            Assert.Equal(0x00, dayHigh & 0x01);
            Assert.Equal(0x80, dayHigh & 0x80);
        }

        [Fact]
        public void ClockAdvancesFromEmulatedCycles()
        {
            var emulator = NewTimerCartridge();
            EnableRtc(emulator);
            WriteRtc(emulator, 0x08, 0);

            for (long i = 0; i < 4194304 / 4 * 3; i++)
                emulator.Tick4();

            Latch(emulator);
            Assert.Equal(3, ReadLatched(emulator, 0x08));
        }

        [Fact]
        public void BatterySaveCarriesTheClockAndWallTimeSinceExit()
        {
            const long savedAt = 1_700_000_000;
            var source = NewTimerCartridge();
            EnableRtc(source);
            WriteRtc(source, 0x08, 50);
            WriteRtc(source, 0x09, 59);
            WriteRtc(source, 0x0A, 23);
            WriteRtc(source, 0x0B, 4);
            SelectRtcRegister(source, 0x00);
            source.WriteMemory(0xA123, 0x77);

            byte[] battery = source.SaveBattery(savedAt);
            Assert.Equal(4 * 0x2000 + 48, battery.Length);

            var loaded = NewTimerCartridge();
            loaded.LoadBattery(battery, savedAt + 15);
            EnableRtc(loaded);
            Latch(loaded);

            Assert.Equal(5, ReadLatched(loaded, 0x08));
            Assert.Equal(0, ReadLatched(loaded, 0x09));
            Assert.Equal(0, ReadLatched(loaded, 0x0A));
            Assert.Equal(5, ReadLatched(loaded, 0x0B));
            SelectRtcRegister(loaded, 0x00);
            Assert.Equal(0x77, loaded.ReadMemory(0xA123));
        }

        [Fact]
        public void BatterySaveWithoutClockFooterLeavesClockUntouched()
        {
            var source = NewTimerCartridge();
            EnableRtc(source);
            WriteRtc(source, 0x08, 42);

            var loaded = NewTimerCartridge();
            loaded.LoadBattery(source.SaveRam(), 1_700_000_000);
            EnableRtc(loaded);
            Latch(loaded);

            Assert.Equal(0, ReadLatched(loaded, 0x08));
        }
    }
}
