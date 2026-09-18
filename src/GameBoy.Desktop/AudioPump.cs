using System;
using GameBoy.Audio;

namespace GameBoy.Desktop
{
    public sealed class AudioPump
    {
        public const int BufferFrames = 1024;
        public const int MaxQueuedFrames = BufferFrames * 4;

        private readonly float volume;

        public AudioPump(float volume)
        {
            this.volume = volume;
        }

        public float[] Buffer { get; } = new float[BufferFrames * 2];

        public bool TryFill(Apu apu)
        {
            while (apu.AvailableSamples > MaxQueuedFrames)
                apu.ReadSamples(Buffer);

            if (apu.AvailableSamples < BufferFrames)
                return false;

            apu.ReadSamples(Buffer);
            for (int i = 0; i < Buffer.Length; i++)
                Buffer[i] *= volume;
            return true;
        }

        public void FillSilence()
        {
            Array.Clear(Buffer);
        }
    }
}
