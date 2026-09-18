using System;
using System.IO;

namespace GameBoy.Desktop
{
    public sealed class GameSession
    {
        public const double NoticeSeconds = 2.0;

        private readonly Func<long> unixClock;
        private double noticeRemaining;

        public GameSession(byte[] rom, string batteryPath, string statePath, int fastForwardSpeed, Func<long> unixClock)
        {
            this.unixClock = unixClock;
            BatteryPath = batteryPath;
            StatePath = statePath;
            Emulator = new Emulator(rom);
            Pacer = new FramePacer(fastForwardSpeed);

            if (Emulator.HasBattery && File.Exists(batteryPath))
                Emulator.LoadBattery(File.ReadAllBytes(batteryPath), unixClock());
        }

        public Emulator Emulator { get; }
        public FramePacer Pacer { get; }
        public string BatteryPath { get; }
        public string StatePath { get; }
        public bool Paused { get; private set; }
        public bool FastForward { get; set; }
        public string? Notice { get; private set; }

        public PaceMode Mode => Paused ? PaceMode.Paused : FastForward ? PaceMode.FastForward : PaceMode.Normal;

        public static GameSession Open(string romPath, string? saveDirectory, int fastForwardSpeed)
        {
            string directory = saveDirectory ?? Path.GetDirectoryName(Path.GetFullPath(romPath))!;
            string baseName = Path.GetFileNameWithoutExtension(romPath);
            return new GameSession(
                File.ReadAllBytes(romPath),
                Path.Combine(directory, baseName + ".sav"),
                Path.Combine(directory, baseName + ".state"),
                fastForwardSpeed,
                () => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public int Advance(double elapsedSeconds)
        {
            if (Notice != null)
            {
                noticeRemaining -= elapsedSeconds;
                if (noticeRemaining <= 0)
                    Notice = null;
            }

            int frames = Pacer.Advance(elapsedSeconds, Mode);
            for (int i = 0; i < frames; i++)
                Emulator.RunFrame();

            if (Mode != PaceMode.Normal)
                Emulator.Apu.ClearSamples();

            return frames;
        }

        public void TogglePause()
        {
            Paused = !Paused;
        }

        public void SaveState()
        {
            try
            {
                WriteAtomically(StatePath, Emulator.SaveState());
                Show("State saved");
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Show($"Save state failed: {error.Message}");
            }
        }

        public void LoadState()
        {
            if (!File.Exists(StatePath))
            {
                Show("No save state yet");
                return;
            }

            byte[] fallback = Emulator.SaveState();
            try
            {
                Emulator.LoadState(File.ReadAllBytes(StatePath));
                Show("State loaded");
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is InvalidDataException)
            {
                Emulator.LoadState(fallback);
                Show($"Load state failed: {error.Message}");
            }
        }

        public void SaveBattery()
        {
            if (Emulator.HasBattery)
                WriteAtomically(BatteryPath, Emulator.SaveBattery(unixClock()));
        }

        private void Show(string message)
        {
            Notice = message;
            noticeRemaining = NoticeSeconds;
        }

        private static void WriteAtomically(string path, byte[] contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            string staging = path + ".tmp";
            File.WriteAllBytes(staging, contents);
            File.Move(staging, path, true);
        }
    }
}
