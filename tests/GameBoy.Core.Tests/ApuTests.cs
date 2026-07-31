using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests
{
    public sealed class ApuTests
    {
        private const int SampleRate = 44100;
        private const int CpuClock = 4194304;

        private readonly ITestOutputHelper output;

        public ApuTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static Emulator NewMachine()
        {
            return new Emulator(TestRoms.BuildProgram());
        }

        private static void AdvanceSeconds(Emulator emulator, double seconds)
        {
            long ticks = (long)(seconds * CpuClock) / 4;
            for (long i = 0; i < ticks; i++)
                emulator.Tick4();
        }

        private static float[] DrainLeft(Emulator emulator)
        {
            int available = emulator.Apu.AvailableSamples;
            var interleaved = new float[available * 2];
            int frames = emulator.Apu.ReadSamples(interleaved);
            var left = new float[frames];
            for (int i = 0; i < frames; i++)
                left[i] = interleaved[i * 2];
            return left;
        }

        private static void ConfigureSquare1(Emulator emulator, int frequency, byte envelope)
        {
            emulator.WriteMemory(0xFF26, 0x80);
            emulator.WriteMemory(0xFF24, 0x77);
            emulator.WriteMemory(0xFF25, 0x11);
            emulator.WriteMemory(0xFF11, 0x80);
            emulator.WriteMemory(0xFF12, envelope);
            emulator.WriteMemory(0xFF13, (byte)(frequency & 0xFF));
            emulator.WriteMemory(0xFF14, (byte)(0x80 | ((frequency >> 8) & 0x07)));
        }

        [Fact]
        public void PoweredApuWithNoActiveChannelsIsSilent()
        {
            var emulator = NewMachine();
            emulator.WriteMemory(0xFF26, 0x80);
            emulator.WriteMemory(0xFF24, 0x77);
            emulator.WriteMemory(0xFF25, 0xFF);

            AdvanceSeconds(emulator, 0.2);
            float[] left = DrainLeft(emulator);

            Assert.True(left.Length > 8000, $"expected a full sample stream, got {left.Length}");
            foreach (float sample in left)
                Assert.Equal(0f, sample);
        }

        [Fact]
        public void SquareChannelAmplitudeMatchesMixerMath()
        {
            var emulator = NewMachine();
            ConfigureSquare1(emulator, 1024, 0xF0);

            AdvanceSeconds(emulator, 0.1);
            float[] left = DrainLeft(emulator);

            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (float sample in left)
            {
                if (sample < min) min = sample;
                if (sample > max) max = sample;
            }

            output.WriteLine($"min={min} max={max}");
            Assert.InRange(max, 0.24f, 0.26f);
            Assert.InRange(min, -0.26f, -0.24f);
        }

        [Fact]
        public void SquareChannelPeriodMatchesProgrammedFrequency()
        {
            var emulator = NewMachine();
            int frequency = 1024;
            ConfigureSquare1(emulator, frequency, 0xF0);

            AdvanceSeconds(emulator, 0.5);
            float[] left = DrainLeft(emulator);

            double squareHz = 131072.0 / (2048 - frequency);
            double expectedSamplesPerPeriod = SampleRate / squareHz;

            double mean = 0;
            foreach (float sample in left)
                mean += sample;
            mean /= left.Length;

            var crossings = new List<int>();
            for (int i = 1; i < left.Length; i++)
                if (left[i - 1] <= mean && left[i] > mean)
                    crossings.Add(i);

            Assert.True(crossings.Count > 20, $"too few periods detected: {crossings.Count}");

            double total = 0;
            for (int i = 1; i < crossings.Count; i++)
                total += crossings[i] - crossings[i - 1];
            double measured = total / (crossings.Count - 1);

            output.WriteLine($"expected {expectedSamplesPerPeriod:F2} samples/period, measured {measured:F2}");
            Assert.InRange(measured, expectedSamplesPerPeriod * 0.97, expectedSamplesPerPeriod * 1.03);
        }

        [Fact]
        public void DisablingDacSilencesARunningChannel()
        {
            var emulator = NewMachine();
            ConfigureSquare1(emulator, 1024, 0xF0);

            AdvanceSeconds(emulator, 0.05);
            float[] whilePlaying = DrainLeft(emulator);

            float loudest = 0;
            foreach (float sample in whilePlaying)
                if (System.MathF.Abs(sample) > loudest)
                    loudest = System.MathF.Abs(sample);
            Assert.True(loudest > 0.1f, "channel should be audible before the DAC is disabled");

            emulator.WriteMemory(0xFF12, 0x00);
            emulator.Apu.ClearSamples();

            AdvanceSeconds(emulator, 0.05);
            float[] afterMute = DrainLeft(emulator);

            Assert.True(afterMute.Length > 1000);
            foreach (float sample in afterMute)
                Assert.Equal(0f, sample);
        }

        [Fact]
        public void NoiseChannelProducesTogglingOutput()
        {
            var emulator = NewMachine();
            emulator.WriteMemory(0xFF26, 0x80);
            emulator.WriteMemory(0xFF24, 0x77);
            emulator.WriteMemory(0xFF25, 0x88);
            emulator.WriteMemory(0xFF20, 0x00);
            emulator.WriteMemory(0xFF21, 0xF0);
            emulator.WriteMemory(0xFF22, 0x00);
            emulator.WriteMemory(0xFF23, 0x80);

            AdvanceSeconds(emulator, 0.1);
            float[] left = DrainLeft(emulator);

            float min = float.MaxValue;
            float max = float.MinValue;
            int transitions = 0;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] < min) min = left[i];
                if (left[i] > max) max = left[i];
                if (i > 0 && left[i] != left[i - 1])
                    transitions++;
            }

            output.WriteLine($"min={min} max={max} transitions={transitions}");
            Assert.True(max > 0.1f);
            Assert.True(min < -0.1f);
            Assert.True(transitions > 200, $"noise output barely changed: {transitions} transitions");
        }

        [Fact]
        public void WaveChannelPlaysProgrammedSamples()
        {
            var emulator = NewMachine();
            emulator.WriteMemory(0xFF26, 0x80);
            emulator.WriteMemory(0xFF24, 0x77);
            emulator.WriteMemory(0xFF25, 0x44);

            byte[] pattern = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };
            for (int i = 0; i < pattern.Length; i++)
                emulator.WriteMemory((ushort)(0xFF30 + i), pattern[i]);

            emulator.WriteMemory(0xFF1A, 0x80);
            emulator.WriteMemory(0xFF1B, 0x00);
            emulator.WriteMemory(0xFF1C, 0x20);
            emulator.WriteMemory(0xFF1D, 0x00);
            emulator.WriteMemory(0xFF1E, 0x84);

            AdvanceSeconds(emulator, 0.1);
            float[] left = DrainLeft(emulator);

            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (float sample in left)
            {
                if (sample < min) min = sample;
                if (sample > max) max = sample;
            }

            output.WriteLine($"wave min={min} max={max}");
            Assert.True(max - min > 0.3f, "wave output should swing across the programmed ramp");
        }

        [Fact]
        public void LengthCounterDisablesChannelWhenEnabled()
        {
            var emulator = NewMachine();
            emulator.WriteMemory(0xFF26, 0x80);
            emulator.WriteMemory(0xFF11, (byte)(0x80 | 63));
            emulator.WriteMemory(0xFF12, 0xF0);
            emulator.WriteMemory(0xFF13, 0x00);
            emulator.WriteMemory(0xFF14, 0xC0);

            Assert.True((emulator.ReadMemory(0xFF26) & 0x01) != 0, "channel 1 should be active right after trigger");

            AdvanceSeconds(emulator, 0.02);

            Assert.True((emulator.ReadMemory(0xFF26) & 0x01) == 0, "channel 1 should have expired via the length counter");
        }

        [Fact]
        public void PowerOffClearsChannelStatus()
        {
            var emulator = NewMachine();
            ConfigureSquare1(emulator, 1024, 0xF0);
            Assert.True((emulator.ReadMemory(0xFF26) & 0x01) != 0);

            emulator.WriteMemory(0xFF26, 0x00);
            Assert.Equal(0x70, emulator.ReadMemory(0xFF26));

            emulator.Apu.ClearSamples();
            AdvanceSeconds(emulator, 0.02);
            float[] left = DrainLeft(emulator);
            foreach (float sample in left)
                Assert.Equal(0f, sample);
        }
    }
}
