using System;

namespace GameBoy.Desktop
{
    public sealed class OptionsException : Exception
    {
        public OptionsException(string message)
            : base(message)
        {
        }
    }
}
