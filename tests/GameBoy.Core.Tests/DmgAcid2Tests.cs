using GameBoy.Util;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests
{
    public sealed class DmgAcid2Tests
    {
        private readonly ITestOutputHelper output;

        public DmgAcid2Tests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Acid2_FramebufferMatchesReference()
        {
            var emulator = new Emulator(TestRoms.Load("dmg-acid2.gb"));
            for (int frame = 0; frame < 200; frame++)
                emulator.RunFrame();

            DecodedImage reference = PngImage.Decode(TestRoms.Path("dmg-acid2-ref.png"));
            Assert.Equal(Emulator.ScreenWidth, reference.Width);
            Assert.Equal(Emulator.ScreenHeight, reference.Height);

            byte[] actual = emulator.Framebuffer;
            int mismatches = 0;
            int firstX = -1, firstY = -1;
            for (int y = 0; y < reference.Height; y++)
            {
                for (int x = 0; x < reference.Width; x++)
                {
                    int index = y * reference.Width + x;
                    int expectedShade = 3 - reference.Samples[index];
                    if (actual[index] != expectedShade)
                    {
                        if (mismatches == 0)
                        {
                            firstX = x;
                            firstY = y;
                        }
                        mismatches++;
                    }
                }
            }

            output.WriteLine($"mismatched pixels: {mismatches} / {reference.Width * reference.Height}");
            if (mismatches > 0)
                output.WriteLine($"first mismatch at ({firstX},{firstY})");

            Assert.Equal(0, mismatches);
        }
    }
}
