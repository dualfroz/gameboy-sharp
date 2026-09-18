using System;
using System.IO;

namespace GameBoy.Cartridge
{
    public interface ICartridgeMapper
    {
        byte Read(ushort address);
        void Write(ushort address, byte value);
        void SaveState(BinaryWriter writer);
        void LoadState(BinaryReader reader);
    }

    public sealed class Cartridge
    {
        public byte[] Rom { get; }
        public byte[] Ram { get; }
        public byte CartridgeType { get; }
        public string Title { get; }
        public string MapperName { get; }

        private readonly ICartridgeMapper mapper;
        private readonly Mbc3Rtc? rtc;

        public Cartridge(byte[] rom)
        {
            if (rom.Length < 0x150)
                throw new ArgumentException("ROM too small to contain a header");

            Rom = rom;
            CartridgeType = rom[0x0147];
            Title = ReadTitle(rom);
            Ram = new byte[RamSize(rom[0x0149])];

            mapper = CreateMapper(CartridgeType, this);
            MapperName = mapper.GetType().Name;
            rtc = (mapper as Mbc3)?.Rtc;
        }

        public bool HasRtc => rtc != null;

        public bool HasBattery => CartridgeType switch
        {
            0x03 or 0x06 or 0x09 or 0x0D or 0x0F or 0x10 or 0x13 or 0x1B or 0x1E or 0x22 => true,
            _ => false
        };

        public byte Read(ushort address) => mapper.Read(address);

        public void Write(ushort address, byte value) => mapper.Write(address, value);

        public void Step(int cycles) => rtc?.Step(cycles);

        public void AdvanceRtc(long seconds) => rtc?.AddSeconds(seconds);

        public byte[] SaveBattery(long unixSeconds)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Ram);
            rtc?.WriteBatteryFooter(writer, unixSeconds);
            writer.Flush();
            return stream.ToArray();
        }

        public void LoadBattery(byte[] contents, long unixSeconds)
        {
            Array.Copy(contents, 0, Ram, 0, Math.Min(contents.Length, Ram.Length));

            int footerLength = contents.Length - Ram.Length;
            if (rtc == null || (footerLength != Mbc3Rtc.BatteryFooterLength && footerLength != Mbc3Rtc.BatteryFooterLength - 4))
                return;

            using var reader = new BinaryReader(new MemoryStream(contents, Ram.Length, footerLength));
            rtc.ReadBatteryFooter(reader, unixSeconds);
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(Ram.Length);
            writer.Write(Ram);
            mapper.SaveState(writer);
        }

        public void LoadState(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] stored = reader.ReadBytes(length);
            Array.Copy(stored, 0, Ram, 0, Math.Min(length, Ram.Length));
            mapper.LoadState(reader);
        }

        private static string ReadTitle(byte[] rom)
        {
            var chars = new char[16];
            int length = 0;
            for (int i = 0; i < 16; i++)
            {
                byte value = rom[0x0134 + i];
                if (value == 0)
                    break;
                chars[length++] = (char)value;
            }
            return new string(chars, 0, length).Trim();
        }

        private static int RamSize(byte code)
        {
            return code switch
            {
                0x01 => 0x800,
                0x02 => 0x2000,
                0x03 => 0x8000,
                0x04 => 0x20000,
                0x05 => 0x10000,
                _ => 0
            };
        }

        private static ICartridgeMapper CreateMapper(byte type, Cartridge cart)
        {
            switch (type)
            {
                case 0x00:
                case 0x08:
                case 0x09:
                    return new NoMbc(cart);
                case 0x01:
                case 0x02:
                case 0x03:
                    return new Mbc1(cart);
                case 0x0F:
                case 0x10:
                    return new Mbc3(cart, true);
                case 0x11:
                case 0x12:
                case 0x13:
                    return new Mbc3(cart, false);
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                    return new Mbc5(cart);
                default:
                    return new Mbc1(cart);
            }
        }
    }
}
