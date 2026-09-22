using System.IO;
using GameBoy.Input;
using Raylib_cs;
using Xunit;

namespace GameBoy.Desktop.Tests
{
    public sealed class DesktopOptionsTests
    {
        [Fact]
        public void DefaultsMatchTheDocumentedControls()
        {
            var options = DesktopOptions.Parse(new[] { "game.gb" }, null);
            KeyBindings bindings = options.Bindings;

            Assert.Equal("game.gb", options.RomPath);
            Assert.Equal(3, options.Scale);
            Assert.Same(LcdPalette.Green, options.Palette);
            Assert.Equal(KeyboardKey.KEY_UP, bindings.Buttons[JoypadButton.Up]);
            Assert.Equal(KeyboardKey.KEY_DOWN, bindings.Buttons[JoypadButton.Down]);
            Assert.Equal(KeyboardKey.KEY_LEFT, bindings.Buttons[JoypadButton.Left]);
            Assert.Equal(KeyboardKey.KEY_RIGHT, bindings.Buttons[JoypadButton.Right]);
            Assert.Equal(KeyboardKey.KEY_Z, bindings.Buttons[JoypadButton.B]);
            Assert.Equal(KeyboardKey.KEY_X, bindings.Buttons[JoypadButton.A]);
            Assert.Equal(KeyboardKey.KEY_ENTER, bindings.Buttons[JoypadButton.Start]);
            Assert.Equal(KeyboardKey.KEY_RIGHT_SHIFT, bindings.Buttons[JoypadButton.Select]);
            Assert.Equal(KeyboardKey.KEY_F5, bindings.SaveState);
            Assert.Equal(KeyboardKey.KEY_F8, bindings.LoadState);
            Assert.Equal(KeyboardKey.KEY_P, bindings.Pause);
            Assert.Equal(KeyboardKey.KEY_TAB, bindings.FastForward);
        }

        [Fact]
        public void CommandLineOverridesScaleKeysAndSpeed()
        {
            var options = DesktopOptions.Parse(
                new[] { "--scale", "5", "game.gb", "--key", "a=space", "--key", "pause=KEY_ESCAPE", "--fast-forward-speed", "8", "--palette", "gray" },
                null);

            Assert.Equal(5, options.Scale);
            Assert.Equal(8, options.FastForwardSpeed);
            Assert.Same(LcdPalette.Gray, options.Palette);
            Assert.Equal(KeyboardKey.KEY_SPACE, options.Bindings.Buttons[JoypadButton.A]);
            Assert.Equal(KeyboardKey.KEY_ESCAPE, options.Bindings.Pause);
        }

        [Fact]
        public void ConfigFileIsReadAndCommandLineWins()
        {
            using var directory = new TempDirectory();
            string config = directory.PathOf("config.ini");
            File.WriteAllLines(config, new[]
            {
                "# controls",
                "scale = 2",
                "volume = 40",
                "save-dir = saves",
                "key.b = A",
                "key.start = SPACE",
                "",
                "palette = #FFFFFF, #C0C0C0, #606060, #000000"
            });

            var options = DesktopOptions.Parse(new[] { "game.gb", "--scale", "4" }, config);

            Assert.Equal(4, options.Scale);
            Assert.Equal(0.4f, options.Volume, 3);
            Assert.Equal(directory.PathOf("saves"), options.SaveDirectory);
            Assert.Equal(KeyboardKey.KEY_A, options.Bindings.Buttons[JoypadButton.B]);
            Assert.Equal(KeyboardKey.KEY_SPACE, options.Bindings.Buttons[JoypadButton.Start]);
            Assert.Equal("custom", options.Palette.Name);
        }

        [Fact]
        public void ExplicitConfigReplacesTheDefaultOne()
        {
            using var directory = new TempDirectory();
            string defaultConfig = directory.PathOf("default.ini");
            string explicitConfig = directory.PathOf("explicit.ini");
            File.WriteAllText(defaultConfig, "scale = 2\n");
            File.WriteAllText(explicitConfig, "scale = 6\n");

            var options = DesktopOptions.Parse(new[] { "game.gb", "--config", explicitConfig }, defaultConfig);

            Assert.Equal(6, options.Scale);
        }

        [Theory]
        [InlineData("--scale", "0")]
        [InlineData("--scale", "big")]
        [InlineData("--volume", "101")]
        [InlineData("--key", "a=NOT_A_KEY")]
        [InlineData("--key", "jump=Z")]
        [InlineData("--palette", "sepia")]
        [InlineData("--frobnicate", "1")]
        public void InvalidSettingsAreRejected(string option, string value)
        {
            Assert.Throws<OptionsException>(() => DesktopOptions.Parse(new[] { "game.gb", option, value }, null));
        }

        [Fact]
        public void MissingRomIsRejected()
        {
            Assert.Throws<OptionsException>(() => DesktopOptions.Parse(new[] { "--scale", "2" }, null));
        }

        [Fact]
        public void ConfigErrorsNameTheLine()
        {
            using var directory = new TempDirectory();
            string config = directory.PathOf("config.ini");
            File.WriteAllText(config, "scale = 2\nkey.a = NOPE\n");

            var error = Assert.Throws<OptionsException>(() => DesktopOptions.Parse(new[] { "game.gb" }, config));
            Assert.Contains("config.ini:2:", error.Message);
        }
    }
}
