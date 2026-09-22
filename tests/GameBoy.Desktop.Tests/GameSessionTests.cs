using System.IO;
using GameBoy.Util;
using Raylib_cs;
using Xunit;

namespace GameBoy.Desktop.Tests
{
    public sealed class GameSessionTests
    {
        private const double DisplayTick = 1.0 / 60.0;
        private const long FixedUnixTime = 1_700_000_000;

        private static GameSession NewSession(byte[] rom, TempDirectory directory)
        {
            return new GameSession(rom, directory.PathOf("game.sav"), directory.PathOf("game.state"), 4, () => FixedUnixTime);
        }

        private static byte ReadActionLines(Emulator emulator)
        {
            emulator.WriteMemory(0xFF00, 0x10);
            return (byte)(emulator.ReadMemory(0xFF00) & 0x0F);
        }

        [Fact]
        public void PacedFrameLoopRendersDmgAcid2()
        {
            using var directory = new TempDirectory();
            var session = NewSession(TestRoms.Load("dmg-acid2.gb"), directory);
            var input = new InputController(new KeyBindings());
            var keyboard = new FakeKeyboard();

            int frames = 0;
            int ticks = 0;
            while (frames < 200)
            {
                input.Apply(keyboard, session);
                frames += session.Advance(DisplayTick);
                keyboard.EndFrame();
                ticks++;
            }

            Assert.InRange(ticks, 200, 202);

            DecodedImage reference = PngImage.Decode(TestRoms.Path("dmg-acid2-ref.png"));
            int mismatches = 0;
            for (int i = 0; i < reference.Samples.Length; i++)
                if (session.Emulator.Framebuffer[i] != 3 - reference.Samples[i])
                    mismatches++;
            Assert.Equal(0, mismatches);

            var pixels = new byte[Emulator.ScreenWidth * Emulator.ScreenHeight * 4];
            LcdPalette.Gray.Fill(session.Emulator.Framebuffer, pixels);
            for (int i = 0; i < reference.Samples.Length; i++)
                Assert.Equal(reference.Samples[i] * 85, pixels[i * 4]);
        }

        [Fact]
        public void HeldKeysReachTheJoypadRegister()
        {
            using var directory = new TempDirectory();
            var session = NewSession(TestRoms.Load("dmg-acid2.gb"), directory);
            var input = new InputController(new KeyBindings());
            var keyboard = new FakeKeyboard();

            input.Apply(keyboard, session);
            Assert.Equal(0x0F, ReadActionLines(session.Emulator));

            keyboard.Hold(KeyboardKey.KEY_X);
            keyboard.Hold(KeyboardKey.KEY_ENTER);
            input.Apply(keyboard, session);
            Assert.Equal(0x0F & ~0x01 & ~0x08, ReadActionLines(session.Emulator));

            keyboard.Release(KeyboardKey.KEY_X);
            keyboard.Release(KeyboardKey.KEY_ENTER);
            keyboard.Hold(KeyboardKey.KEY_Z);
            input.Apply(keyboard, session);
            Assert.Equal(0x0F & ~0x02, ReadActionLines(session.Emulator));
        }

        [Fact]
        public void PauseAndFastForwardHotkeysControlThePace()
        {
            using var directory = new TempDirectory();
            var session = NewSession(TestRoms.Load("dmg-acid2.gb"), directory);
            var input = new InputController(new KeyBindings());
            var keyboard = new FakeKeyboard();

            keyboard.Tap(KeyboardKey.KEY_P);
            input.Apply(keyboard, session);
            keyboard.EndFrame();
            Assert.True(session.Paused);
            Assert.Equal(0, session.Advance(1.0));

            keyboard.Tap(KeyboardKey.KEY_P);
            input.Apply(keyboard, session);
            keyboard.EndFrame();
            Assert.False(session.Paused);

            keyboard.Hold(KeyboardKey.KEY_TAB);
            input.Apply(keyboard, session);
            Assert.Equal(PaceMode.FastForward, session.Mode);
            Assert.Equal(4, session.Advance(FramePacer.FrameSeconds * 1.01));
            Assert.Equal(0, session.Emulator.Apu.AvailableSamples);

            keyboard.Release(KeyboardKey.KEY_TAB);
            input.Apply(keyboard, session);
            Assert.Equal(PaceMode.Normal, session.Mode);
            session.Advance(FramePacer.FrameSeconds * 2.01);
            Assert.True(new AudioPump(0.5f).TryFill(session.Emulator.Apu));
        }

        [Fact]
        public void SaveAndLoadStateHotkeysRestoreTheMachine()
        {
            using var directory = new TempDirectory();
            var session = NewSession(TestRoms.Load("cpu_instrs.gb"), directory);
            var input = new InputController(new KeyBindings());
            var keyboard = new FakeKeyboard();

            keyboard.Tap(KeyboardKey.KEY_F8);
            input.Apply(keyboard, session);
            keyboard.EndFrame();
            Assert.Equal("No save state yet", session.Notice);

            for (int i = 0; i < 120; i++)
                session.Advance(DisplayTick);

            keyboard.Tap(KeyboardKey.KEY_F5);
            input.Apply(keyboard, session);
            keyboard.EndFrame();
            Assert.Equal("State saved", session.Notice);
            Assert.True(File.Exists(session.StatePath));

            long savedCycles = session.Emulator.TotalCycles;
            string savedSerial = session.Emulator.SerialOutput;

            for (int i = 0; i < 150; i++)
                session.Advance(DisplayTick);
            Assert.NotEqual(savedCycles, session.Emulator.TotalCycles);
            Assert.Null(session.Notice);

            keyboard.Tap(KeyboardKey.KEY_F8);
            input.Apply(keyboard, session);
            keyboard.EndFrame();
            Assert.Equal("State loaded", session.Notice);
            Assert.Equal(savedCycles, session.Emulator.TotalCycles);
            Assert.Equal(savedSerial, session.Emulator.SerialOutput);
        }

        [Fact]
        public void CorruptStateFileLeavesTheMachineRunning()
        {
            using var directory = new TempDirectory();
            var session = NewSession(TestRoms.Load("cpu_instrs.gb"), directory);
            for (int i = 0; i < 30; i++)
                session.Advance(DisplayTick);

            File.WriteAllBytes(session.StatePath, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            long cycles = session.Emulator.TotalCycles;
            session.LoadState();

            Assert.StartsWith("Load state failed", session.Notice);
            Assert.Equal(cycles, session.Emulator.TotalCycles);
        }

        [Fact]
        public void BatteryRamIsWrittenOnExitAndReadOnStart()
        {
            using var directory = new TempDirectory();
            byte[] rom = TestRoms.BuildCartridge(0x03, 0x02);

            var first = NewSession(rom, directory);
            first.Emulator.WriteMemory(0x0000, 0x0A);
            first.Emulator.WriteMemory(0xA010, 0xC3);
            first.Emulator.WriteMemory(0xBFFF, 0x5A);
            first.SaveBattery();

            Assert.Equal(0x2000, new FileInfo(first.BatteryPath).Length);
            Assert.False(File.Exists(first.BatteryPath + ".tmp"));

            var second = NewSession(rom, directory);
            second.Emulator.WriteMemory(0x0000, 0x0A);
            Assert.Equal(0xC3, second.Emulator.ReadMemory(0xA010));
            Assert.Equal(0x5A, second.Emulator.ReadMemory(0xBFFF));
        }

        [Fact]
        public void CartridgeWithoutBatteryWritesNoSaveFile()
        {
            using var directory = new TempDirectory();
            var session = NewSession(TestRoms.Load("dmg-acid2.gb"), directory);

            session.SaveBattery();

            Assert.False(File.Exists(session.BatteryPath));
        }
    }
}
