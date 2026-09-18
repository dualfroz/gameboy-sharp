using System.Diagnostics;
using System.Numerics;
using GameBoy.Audio;
using Raylib_cs;

namespace GameBoy.Desktop
{
    internal static class RaylibHost
    {
        private static readonly Color Backdrop = new Color(0, 0, 0, 255);
        private static readonly Color Ink = new Color(255, 255, 255, 255);
        private static readonly Color Shadow = new Color(0, 0, 0, 200);

        public static void Run(GameSession session, DesktopOptions options)
        {
            int scale = options.Scale;
            int width = Emulator.ScreenWidth * scale;
            int height = Emulator.ScreenHeight * scale;

            Raylib.SetTraceLogLevel(TraceLogLevel.LOG_WARNING);
            Raylib.InitWindow(width, height, WindowTitle(session));
            Raylib.InitAudioDevice();
            Raylib.SetAudioStreamBufferSizeDefault(AudioPump.BufferFrames);

            AudioStream stream = Raylib.LoadAudioStream(Apu.SampleRate, 32, 2);
            Raylib.PlayAudioStream(stream);

            Image blank = Raylib.GenImageColor(Emulator.ScreenWidth, Emulator.ScreenHeight, Backdrop);
            Texture2D lcd = Raylib.LoadTextureFromImage(blank);
            Raylib.UnloadImage(blank);
            Raylib.SetTextureFilter(lcd, TextureFilter.TEXTURE_FILTER_POINT);

            var keyboard = new RaylibKeyboard();
            var input = new InputController(options.Bindings);
            var pump = new AudioPump(options.Volume);
            var pixels = new byte[Emulator.ScreenWidth * Emulator.ScreenHeight * 4];
            var source = new Rectangle(0, 0, Emulator.ScreenWidth, Emulator.ScreenHeight);
            var target = new Rectangle(0, 0, width, height);

            var clock = Stopwatch.StartNew();
            double previous = 0;

            try
            {
                while (!Raylib.WindowShouldClose())
                {
                    double now = clock.Elapsed.TotalSeconds;
                    input.Apply(keyboard, session);
                    session.Advance(now - previous);
                    previous = now;

                    FeedAudio(stream, session, pump);

                    options.Palette.Fill(session.Emulator.Framebuffer, pixels);
                    UploadPixels(lcd, pixels);

                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Backdrop);
                    Raylib.DrawTexturePro(lcd, source, target, Vector2.Zero, 0f, Ink);
                    DrawOverlay(session, width, height, scale);
                    Raylib.EndDrawing();

                    double wait = session.Pacer.SecondsUntilNextFrame(session.Mode) - (clock.Elapsed.TotalSeconds - now);
                    if (wait > 0)
                        Raylib.WaitTime(wait);
                }
            }
            finally
            {
                Raylib.UnloadTexture(lcd);
                Raylib.UnloadAudioStream(stream);
                Raylib.CloseAudioDevice();
                Raylib.CloseWindow();
            }
        }

        private static string WindowTitle(GameSession session)
        {
            string title = session.Emulator.Cartridge.Title;
            return title.Length > 0 ? $"gameboy-sharp - {title}" : "gameboy-sharp";
        }

        private static void FeedAudio(AudioStream stream, GameSession session, AudioPump pump)
        {
            while (Raylib.IsAudioStreamProcessed(stream))
            {
                if (session.Mode != PaceMode.Normal)
                    pump.FillSilence();
                else if (!pump.TryFill(session.Emulator.Apu))
                    return;

                unsafe
                {
                    fixed (float* samples = pump.Buffer)
                    {
                        Raylib.UpdateAudioStream(stream, samples, AudioPump.BufferFrames);
                    }
                }
            }
        }

        private static unsafe void UploadPixels(Texture2D texture, byte[] pixels)
        {
            fixed (byte* data = pixels)
            {
                Raylib.UpdateTexture(texture, data);
            }
        }

        private static void DrawOverlay(GameSession session, int width, int height, int scale)
        {
            int fontSize = 6 * scale;
            int margin = 2 * scale;

            if (session.Paused)
            {
                const string label = "PAUSED";
                int labelWidth = Raylib.MeasureText(label, fontSize * 2);
                DrawShadowedText(label, (width - labelWidth) / 2, (height - fontSize * 2) / 2, fontSize * 2, scale);
            }
            else if (session.FastForward)
            {
                string label = $">> x{session.Pacer.FastForwardFactor}";
                DrawShadowedText(label, width - Raylib.MeasureText(label, fontSize) - margin, margin, fontSize, scale);
            }

            if (session.Notice != null)
                DrawShadowedText(session.Notice, margin, height - fontSize - margin, fontSize, scale);
        }

        private static void DrawShadowedText(string text, int x, int y, int fontSize, int scale)
        {
            int offset = scale < 2 ? 1 : scale / 2;
            Raylib.DrawText(text, x + offset, y + offset, fontSize, Shadow);
            Raylib.DrawText(text, x, y, fontSize, Ink);
        }

        private sealed class RaylibKeyboard : IKeyboard
        {
            public bool IsDown(KeyboardKey key) => Raylib.IsKeyDown(key);

            public bool IsPressed(KeyboardKey key) => Raylib.IsKeyPressed(key);
        }
    }
}
