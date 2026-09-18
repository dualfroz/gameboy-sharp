using System;
using System.IO;

namespace GameBoy.Desktop
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                Console.WriteLine(DesktopOptions.Usage);
                Console.WriteLine($"default config file: {DesktopOptions.DefaultConfigPath}");
                return args.Length == 0 ? 1 : 0;
            }

            DesktopOptions options;
            try
            {
                options = DesktopOptions.Parse(args, DesktopOptions.DefaultConfigPath);
            }
            catch (OptionsException error)
            {
                Console.Error.WriteLine($"error: {error.Message}");
                Console.Error.WriteLine(DesktopOptions.Usage);
                return 1;
            }

            GameSession session;
            try
            {
                session = GameSession.Open(options.RomPath, options.SaveDirectory, options.FastForwardSpeed);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Console.Error.WriteLine($"error: cannot open {options.RomPath}: {error.Message}");
                return 1;
            }

            try
            {
                RaylibHost.Run(session, options);
            }
            finally
            {
                session.SaveBattery();
            }

            return 0;
        }
    }
}
