using System;
using System.IO;

namespace GameBoy.Tests
{
    internal static class TestRoms
    {
        public static string Path(string name)
        {
            string path = System.IO.Path.Combine(AppContext.BaseDirectory, "roms", name);
            if (!File.Exists(path))
                throw new FileNotFoundException($"test rom not found: {path}");
            return path;
        }

        public static byte[] Load(string name) => File.ReadAllBytes(Path(name));

        public static byte[] BuildProgram(params byte[] program)
        {
            var rom = new byte[0x8000];
            rom[0x0147] = 0x00;
            rom[0x0148] = 0x00;
            rom[0x0149] = 0x00;
            Array.Copy(program, 0, rom, 0x0100, program.Length);
            return rom;
        }

        public static byte[] BuildCartridge(byte cartridgeType, byte ramSizeCode, int romBanks = 2)
        {
            var rom = new byte[0x4000 * romBanks];
            rom[0x0147] = cartridgeType;
            rom[0x0148] = 0x00;
            rom[0x0149] = ramSizeCode;
            return rom;
        }
    }
}
