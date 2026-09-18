using System;
using System.Globalization;

namespace GameBoy.Desktop
{
    public sealed class LcdPalette
    {
        public static readonly LcdPalette Green = new LcdPalette("green", 0xE0F8D0, 0x88C070, 0x346856, 0x081820);
        public static readonly LcdPalette Gray = new LcdPalette("gray", 0xFFFFFF, 0xAAAAAA, 0x555555, 0x000000);

        private readonly byte[] rgba = new byte[16];

        public LcdPalette(string name, params int[] shadeColors)
        {
            Name = name;
            for (int shade = 0; shade < 4; shade++)
            {
                rgba[shade * 4] = (byte)(shadeColors[shade] >> 16);
                rgba[shade * 4 + 1] = (byte)(shadeColors[shade] >> 8);
                rgba[shade * 4 + 2] = (byte)shadeColors[shade];
                rgba[shade * 4 + 3] = 0xFF;
            }
        }

        public string Name { get; }

        public void Fill(byte[] shades, byte[] pixels)
        {
            for (int i = 0; i < shades.Length; i++)
            {
                int source = (shades[i] & 0x03) * 4;
                int target = i * 4;
                pixels[target] = rgba[source];
                pixels[target + 1] = rgba[source + 1];
                pixels[target + 2] = rgba[source + 2];
                pixels[target + 3] = rgba[source + 3];
            }
        }

        public static LcdPalette Parse(string text)
        {
            string trimmed = text.Trim();
            if (trimmed.Equals(Green.Name, StringComparison.OrdinalIgnoreCase))
                return Green;
            if (trimmed.Equals(Gray.Name, StringComparison.OrdinalIgnoreCase))
                return Gray;

            string[] parts = trimmed.Split(',');
            var colors = new int[4];
            bool valid = parts.Length == 4;
            for (int shade = 0; valid && shade < 4; shade++)
            {
                string hex = parts[shade].Trim().TrimStart('#');
                valid = hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out colors[shade]);
            }

            if (!valid)
                throw new OptionsException($"unknown palette '{text}' (use green, gray or four colors like #E0F8D0,#88C070,#346856,#081820, lightest first)");

            return new LcdPalette("custom", colors);
        }
    }
}
