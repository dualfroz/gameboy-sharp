using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;

namespace GameBoy.Desktop.Tests
{
    internal sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "gameboy-sharp-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string PathOf(string name) => Path.Combine(Root, name);

        public void Dispose() => Directory.Delete(Root, true);
    }

    internal static class TestRoms
    {
        public static string Path(string name) => System.IO.Path.Combine(AppContext.BaseDirectory, "roms", name);

        public static byte[] Load(string name) => File.ReadAllBytes(Path(name));

        public static byte[] BuildCartridge(byte cartridgeType, byte ramSizeCode)
        {
            var rom = new byte[0x8000];
            rom[0x0147] = cartridgeType;
            rom[0x0149] = ramSizeCode;
            return rom;
        }
    }

    internal sealed class FakeKeyboard : IKeyboard
    {
        private readonly HashSet<KeyboardKey> held = new HashSet<KeyboardKey>();
        private readonly HashSet<KeyboardKey> pressedThisFrame = new HashSet<KeyboardKey>();

        public void Hold(KeyboardKey key)
        {
            if (held.Add(key))
                pressedThisFrame.Add(key);
        }

        public void Release(KeyboardKey key) => held.Remove(key);

        public void Tap(KeyboardKey key)
        {
            Hold(key);
            Release(key);
        }

        public void EndFrame() => pressedThisFrame.Clear();

        public bool IsDown(KeyboardKey key) => held.Contains(key);

        public bool IsPressed(KeyboardKey key) => pressedThisFrame.Contains(key);
    }
}
