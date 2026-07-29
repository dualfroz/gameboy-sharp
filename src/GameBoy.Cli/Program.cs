using System;
using System.IO;
using GameBoy;
using GameBoy.Util;

namespace GameBoy.Cli
{
    internal static class Program
    {
        private const int MaxSerialFrames = 4000;

        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: GameBoy.Cli <rom.gb> [--frames N] [--serial] [--png path] [--ascii] [--ascii-out path]");
                return 1;
            }

            string romPath = args[0];
            int frames = 60;
            bool waitSerial = false;
            bool ascii = false;
            string? pngPath = null;
            string? asciiPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--frames":
                        frames = int.Parse(args[++i]);
                        break;
                    case "--serial":
                        waitSerial = true;
                        break;
                    case "--png":
                        pngPath = args[++i];
                        break;
                    case "--ascii":
                        ascii = true;
                        break;
                    case "--ascii-out":
                        asciiPath = args[++i];
                        break;
                }
            }

            if (!File.Exists(romPath))
            {
                Console.Error.WriteLine($"rom not found: {romPath}");
                return 1;
            }

            var emulator = new Emulator(File.ReadAllBytes(romPath));
            Console.WriteLine($"loaded {emulator.Cartridge.Title} ({emulator.Cartridge.MapperName}, type 0x{emulator.Cartridge.CartridgeType:X2})");

            if (waitSerial)
                RunUntilSerial(emulator);
            else
                for (int f = 0; f < frames; f++)
                    emulator.RunFrame();

            Console.WriteLine($"ran {emulator.TotalCycles} cycles");

            if (waitSerial || emulator.SerialOutput.Length > 0)
                Console.WriteLine($"serial: {emulator.SerialOutput.Trim()}");

            if (pngPath != null)
            {
                PngImage.WriteGrayscale(pngPath, Emulator.ScreenWidth, Emulator.ScreenHeight, emulator.Framebuffer);
                Console.WriteLine($"framebuffer written to {pngPath}");
            }

            if (ascii || asciiPath != null)
            {
                string art = AsciiRender(emulator.Framebuffer);
                if (asciiPath != null)
                    File.WriteAllText(asciiPath, art);
                if (ascii)
                    Console.WriteLine(art);
            }

            return 0;
        }

        private static void RunUntilSerial(Emulator emulator)
        {
            for (int f = 0; f < MaxSerialFrames; f++)
            {
                emulator.RunFrame();
                string output = emulator.SerialOutput;
                if (output.Contains("Passed") || output.Contains("Failed"))
                    return;
            }
        }

        private static string AsciiRender(byte[] framebuffer)
        {
            char[] ramp = { ' ', '.', '+', '#' };
            var builder = new System.Text.StringBuilder();
            for (int y = 0; y < Emulator.ScreenHeight; y += 2)
            {
                for (int x = 0; x < Emulator.ScreenWidth; x += 2)
                {
                    byte shade = framebuffer[y * Emulator.ScreenWidth + x];
                    builder.Append(ramp[shade & 0x03]);
                }
                builder.Append('\n');
            }
            return builder.ToString();
        }
    }
}
