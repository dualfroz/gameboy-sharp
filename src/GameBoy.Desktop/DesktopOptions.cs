using System;
using System.Globalization;
using System.IO;

namespace GameBoy.Desktop
{
    public sealed class DesktopOptions
    {
        public const string Usage =
            "usage: GameBoy.Desktop <rom.gb> [options]\n" +
            "  --scale N                integer LCD scale, 1-12 (default 3)\n" +
            "  --palette NAME           green, gray or #RRGGBB,#RRGGBB,#RRGGBB,#RRGGBB (default green)\n" +
            "  --volume N               audio volume 0-100 (default 70)\n" +
            "  --fast-forward-speed N   speed while fast-forward is held, 2-16 (default 4)\n" +
            "  --save-dir PATH          directory for .sav and .state files (default: next to the ROM)\n" +
            "  --key ACTION=KEY         rebind an action, e.g. --key a=X --key start=SPACE\n" +
            "  --config PATH            read settings from PATH instead of the default config file\n" +
            "actions: up down left right a b start select save-state load-state pause fast-forward";

        public string RomPath { get; private set; } = "";
        public int Scale { get; private set; } = 3;
        public LcdPalette Palette { get; private set; } = LcdPalette.Green;
        public float Volume { get; private set; } = 0.7f;
        public int FastForwardSpeed { get; private set; } = 4;
        public string? SaveDirectory { get; private set; }
        public KeyBindings Bindings { get; } = new KeyBindings();

        public static string DefaultConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gameboy-sharp", "config.ini");

        public static DesktopOptions Parse(string[] args, string? defaultConfigPath)
        {
            var options = new DesktopOptions();

            string? configPath = FindConfigArgument(args);
            if (configPath != null)
                options.ApplyConfigFile(configPath);
            else if (defaultConfigPath != null && File.Exists(defaultConfigPath))
                options.ApplyConfigFile(defaultConfigPath);

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.StartsWith("--", StringComparison.Ordinal))
                {
                    if (options.RomPath.Length > 0)
                        throw new OptionsException($"unexpected argument '{arg}'");
                    options.RomPath = arg;
                    continue;
                }

                if (i + 1 >= args.Length)
                    throw new OptionsException($"missing value for {arg}");
                string value = args[++i];

                switch (arg)
                {
                    case "--config":
                        break;
                    case "--key":
                        int separator = value.IndexOf('=');
                        if (separator <= 0)
                            throw new OptionsException($"expected ACTION=KEY after --key, got '{value}'");
                        options.Apply("key." + value.Substring(0, separator), value.Substring(separator + 1), Directory.GetCurrentDirectory());
                        break;
                    default:
                        options.Apply(arg.Substring(2), value, Directory.GetCurrentDirectory());
                        break;
                }
            }

            if (options.RomPath.Length == 0)
                throw new OptionsException("no ROM given");

            return options;
        }

        private static string? FindConfigArgument(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--config")
                    return args[i + 1];
            return null;
        }

        private void ApplyConfigFile(string path)
        {
            if (!File.Exists(path))
                throw new OptionsException($"config file not found: {path}");

            string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;
            string[] lines = File.ReadAllLines(path);
            for (int number = 1; number <= lines.Length; number++)
            {
                string line = lines[number - 1].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int separator = line.IndexOf('=');
                if (separator <= 0)
                    throw new OptionsException($"{path}:{number}: expected 'name = value'");

                try
                {
                    Apply(line.Substring(0, separator).Trim(), line.Substring(separator + 1).Trim(), baseDirectory);
                }
                catch (OptionsException error)
                {
                    throw new OptionsException($"{path}:{number}: {error.Message}");
                }
            }
        }

        private void Apply(string name, string value, string baseDirectory)
        {
            if (name.StartsWith("key.", StringComparison.OrdinalIgnoreCase))
            {
                Bindings.Bind(name.Substring(4), KeyBindings.ParseKey(value));
                return;
            }

            switch (name.ToLowerInvariant())
            {
                case "scale":
                    Scale = ParseInt(name, value, 1, 12);
                    break;
                case "palette":
                    Palette = LcdPalette.Parse(value);
                    break;
                case "volume":
                    Volume = ParseInt(name, value, 0, 100) / 100f;
                    break;
                case "fast-forward-speed":
                    FastForwardSpeed = ParseInt(name, value, 2, 16);
                    break;
                case "save-dir":
                    SaveDirectory = Path.GetFullPath(value, baseDirectory);
                    break;
                default:
                    throw new OptionsException($"unknown setting '{name}'");
            }
        }

        private static int ParseInt(string name, string value, int min, int max)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) || parsed < min || parsed > max)
                throw new OptionsException($"{name} must be a whole number from {min} to {max}, got '{value}'");
            return parsed;
        }
    }
}
