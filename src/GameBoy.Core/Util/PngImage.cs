using System;
using System.IO;
using System.IO.Compression;

namespace GameBoy.Util
{
    public sealed class DecodedImage
    {
        public int Width;
        public int Height;
        public byte[] Samples = Array.Empty<byte>();
    }

    public static class PngImage
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private static readonly byte[] GrayLevels = { 0xFF, 0xAA, 0x55, 0x00 };

        public static void WriteGrayscale(string path, int width, int height, byte[] shades)
        {
            using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
            WriteGrayscale(file, width, height, shades);
        }

        public static void WriteGrayscale(Stream output, int width, int height, byte[] shades)
        {
            output.Write(Signature, 0, Signature.Length);

            var ihdr = new byte[13];
            WriteBigEndian(ihdr, 0, (uint)width);
            WriteBigEndian(ihdr, 4, (uint)height);
            ihdr[8] = 8;
            ihdr[9] = 0;
            ihdr[10] = 0;
            ihdr[11] = 0;
            ihdr[12] = 0;
            WriteChunk(output, "IHDR", ihdr);

            var raw = new byte[height * (width + 1)];
            int pos = 0;
            for (int y = 0; y < height; y++)
            {
                raw[pos++] = 0;
                for (int x = 0; x < width; x++)
                {
                    byte shade = shades[y * width + x];
                    raw[pos++] = GrayLevels[shade & 0x03];
                }
            }

            WriteChunk(output, "IDAT", ZlibCompress(raw));
            WriteChunk(output, "IEND", Array.Empty<byte>());
        }

        public static DecodedImage Decode(string path)
        {
            return Decode(File.ReadAllBytes(path));
        }

        public static DecodedImage Decode(byte[] bytes)
        {
            for (int i = 0; i < Signature.Length; i++)
                if (bytes[i] != Signature[i])
                    throw new InvalidDataException("Not a PNG file");

            int offset = 8;
            int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
            using var idat = new MemoryStream();

            while (offset < bytes.Length)
            {
                int length = (int)ReadBigEndian(bytes, offset);
                string type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
                int dataStart = offset + 8;

                if (type == "IHDR")
                {
                    width = (int)ReadBigEndian(bytes, dataStart);
                    height = (int)ReadBigEndian(bytes, dataStart + 4);
                    bitDepth = bytes[dataStart + 8];
                    colorType = bytes[dataStart + 9];
                    interlace = bytes[dataStart + 12];
                }
                else if (type == "IDAT")
                {
                    idat.Write(bytes, dataStart, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                offset = dataStart + length + 4;
            }

            if (colorType != 0)
                throw new NotSupportedException("Only grayscale PNG is supported");
            if (interlace != 0)
                throw new NotSupportedException("Interlaced PNG is not supported");

            byte[] compressed = idat.ToArray();
            byte[] inflated = ZlibDecompress(compressed);

            int bitsPerRow = width * bitDepth;
            int bytesPerRow = (bitsPerRow + 7) / 8;
            var samples = new byte[width * height];
            var previous = new byte[bytesPerRow];
            int inPos = 0;

            for (int y = 0; y < height; y++)
            {
                byte filter = inflated[inPos++];
                var current = new byte[bytesPerRow];
                Array.Copy(inflated, inPos, current, 0, bytesPerRow);
                inPos += bytesPerRow;
                Unfilter(filter, current, previous, 1);

                for (int x = 0; x < width; x++)
                    samples[y * width + x] = ExtractSample(current, x, bitDepth);

                previous = current;
            }

            return new DecodedImage { Width = width, Height = height, Samples = samples };
        }

        private static byte ExtractSample(byte[] row, int x, int bitDepth)
        {
            switch (bitDepth)
            {
                case 8:
                    return row[x];
                case 4:
                {
                    int b = row[x / 2];
                    return (byte)((x & 1) == 0 ? b >> 4 : b & 0x0F);
                }
                case 2:
                {
                    int b = row[x / 4];
                    int shift = 6 - (x & 3) * 2;
                    return (byte)((b >> shift) & 0x03);
                }
                case 1:
                {
                    int b = row[x / 8];
                    int shift = 7 - (x & 7);
                    return (byte)((b >> shift) & 0x01);
                }
                default:
                    throw new NotSupportedException("Unsupported bit depth");
            }
        }

        private static void Unfilter(byte filter, byte[] current, byte[] previous, int bpp)
        {
            for (int i = 0; i < current.Length; i++)
            {
                int a = i >= bpp ? current[i - bpp] : 0;
                int b = previous[i];
                int c = i >= bpp ? previous[i - bpp] : 0;
                int value = current[i];
                switch (filter)
                {
                    case 1: value += a; break;
                    case 2: value += b; break;
                    case 3: value += (a + b) / 2; break;
                    case 4: value += Paeth(a, b, c); break;
                }
                current[i] = (byte)value;
            }
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        private static byte[] ZlibCompress(byte[] raw)
        {
            using var output = new MemoryStream();
            output.WriteByte(0x78);
            output.WriteByte(0x01);

            int pos = 0;
            while (pos < raw.Length)
            {
                int chunk = Math.Min(65535, raw.Length - pos);
                bool last = pos + chunk >= raw.Length;
                output.WriteByte((byte)(last ? 1 : 0));
                output.WriteByte((byte)(chunk & 0xFF));
                output.WriteByte((byte)((chunk >> 8) & 0xFF));
                output.WriteByte((byte)(~chunk & 0xFF));
                output.WriteByte((byte)((~chunk >> 8) & 0xFF));
                output.Write(raw, pos, chunk);
                pos += chunk;
            }

            uint adler = Adler32(raw);
            output.WriteByte((byte)(adler >> 24));
            output.WriteByte((byte)(adler >> 16));
            output.WriteByte((byte)(adler >> 8));
            output.WriteByte((byte)adler);
            return output.ToArray();
        }

        private static byte[] ZlibDecompress(byte[] compressed)
        {
            using var input = new MemoryStream(compressed, 2, compressed.Length - 2);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }

        private static uint Adler32(byte[] data)
        {
            const uint mod = 65521;
            uint a = 1, b = 0;
            foreach (byte value in data)
            {
                a = (a + value) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            var length = new byte[4];
            WriteBigEndian(length, 0, (uint)data.Length);
            output.Write(length, 0, 4);

            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            output.Write(typeBytes, 0, 4);
            output.Write(data, 0, data.Length);

            uint crc = Crc32(typeBytes, data);
            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, crc);
            output.Write(crcBytes, 0, 4);
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        private static uint Crc32(byte[] type, byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte value in type)
                crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
            foreach (byte value in data)
                crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFF;
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static uint ReadBigEndian(byte[] buffer, int offset)
        {
            return (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);
        }
    }
}
