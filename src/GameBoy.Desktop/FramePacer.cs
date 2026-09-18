using System;

namespace GameBoy.Desktop
{
    public enum PaceMode
    {
        Normal,
        FastForward,
        Paused
    }

    public sealed class FramePacer
    {
        public const double FrameSeconds = 70224.0 / 4194304.0;
        public const double FrameRate = 1.0 / FrameSeconds;
        public const int MaxCatchUpFrames = 3;

        private double owedSeconds;

        public FramePacer(int fastForwardFactor)
        {
            FastForwardFactor = fastForwardFactor;
        }

        public int FastForwardFactor { get; }

        public int Advance(double elapsedSeconds, PaceMode mode)
        {
            if (mode == PaceMode.Paused)
            {
                owedSeconds = 0;
                return 0;
            }

            int speed = Speed(mode);
            owedSeconds += elapsedSeconds * speed;

            int due = (int)(owedSeconds / FrameSeconds);
            int limit = MaxCatchUpFrames * speed;
            if (due > limit)
            {
                owedSeconds = 0;
                return limit;
            }

            owedSeconds -= due * FrameSeconds;
            return due;
        }

        public double SecondsUntilNextFrame(PaceMode mode)
        {
            if (mode == PaceMode.Paused)
                return FrameSeconds;
            return Math.Max(0, (FrameSeconds - owedSeconds) / Speed(mode));
        }

        private int Speed(PaceMode mode) => mode == PaceMode.FastForward ? FastForwardFactor : 1;
    }
}
