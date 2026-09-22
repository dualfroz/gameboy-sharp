using System;
using Xunit;

namespace GameBoy.Desktop.Tests
{
    public sealed class FramePacerTests
    {
        private const double DisplayTick = 1.0 / 60.0;

        private static int RunFor(FramePacer pacer, double seconds, PaceMode mode)
        {
            int frames = 0;
            int ticks = (int)Math.Round(seconds / DisplayTick);
            for (int i = 0; i < ticks; i++)
                frames += pacer.Advance(DisplayTick, mode);
            return frames;
        }

        [Fact]
        public void NormalSpeedRunsAtTheDmgRefreshRate()
        {
            var pacer = new FramePacer(4);
            int frames = RunFor(pacer, 10.0, PaceMode.Normal);

            Assert.InRange(FramePacer.FrameRate, 59.72, 59.73);
            Assert.InRange(frames, 597, 598);
        }

        [Fact]
        public void FastForwardMultipliesTheFrameRate()
        {
            var pacer = new FramePacer(4);
            int frames = RunFor(pacer, 10.0, PaceMode.FastForward);

            Assert.InRange(frames, 4 * 597, 4 * 598);
        }

        [Fact]
        public void PauseRunsNothingAndDoesNotBurstOnResume()
        {
            var pacer = new FramePacer(4);
            pacer.Advance(DisplayTick * 0.9, PaceMode.Normal);

            Assert.Equal(0, RunFor(pacer, 5.0, PaceMode.Paused));
            Assert.Equal(0, pacer.Advance(DisplayTick * 0.5, PaceMode.Normal));
        }

        [Fact]
        public void LongStallIsCappedInsteadOfCatchingUpEveryFrame()
        {
            var pacer = new FramePacer(4);

            Assert.Equal(FramePacer.MaxCatchUpFrames, pacer.Advance(2.0, PaceMode.Normal));
            Assert.Equal(0, pacer.Advance(0.0, PaceMode.Normal));
        }

        [Fact]
        public void WaitTimeShrinksAsTimeIsOwed()
        {
            var pacer = new FramePacer(4);
            Assert.Equal(FramePacer.FrameSeconds, pacer.SecondsUntilNextFrame(PaceMode.Normal), 9);

            pacer.Advance(FramePacer.FrameSeconds / 2, PaceMode.Normal);
            Assert.Equal(FramePacer.FrameSeconds / 2, pacer.SecondsUntilNextFrame(PaceMode.Normal), 9);
            Assert.Equal(FramePacer.FrameSeconds / 8, pacer.SecondsUntilNextFrame(PaceMode.FastForward), 9);
        }
    }
}
