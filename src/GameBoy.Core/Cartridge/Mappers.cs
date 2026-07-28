using System.IO;

namespace GameBoy.Cartridge
{
    public sealed class NoMbc : ICartridgeMapper
    {
        private readonly Cartridge cart;

        public NoMbc(Cartridge cart)
        {
            this.cart = cart;
        }

        public byte Read(ushort address)
        {
            if (address < 0x8000)
                return address < cart.Rom.Length ? cart.Rom[address] : (byte)0xFF;
            if (address >= 0xA000 && address < 0xC000 && cart.Ram.Length > 0)
                return cart.Ram[address - 0xA000];
            return 0xFF;
        }

        public void Write(ushort address, byte value)
        {
            if (address >= 0xA000 && address < 0xC000 && cart.Ram.Length > 0)
                cart.Ram[address - 0xA000] = value;
        }

        public void SaveState(BinaryWriter writer)
        {
        }

        public void LoadState(BinaryReader reader)
        {
        }
    }

    public sealed class Mbc1 : ICartridgeMapper
    {
        private readonly Cartridge cart;
        private readonly int romBankCount;
        private readonly int ramBankCount;

        private int bankLow = 1;
        private int bankHigh;
        private int mode;
        private bool ramEnabled;

        public Mbc1(Cartridge cart)
        {
            this.cart = cart;
            romBankCount = cart.Rom.Length / 0x4000;
            ramBankCount = cart.Ram.Length > 0 ? cart.Ram.Length / 0x2000 : 0;
        }

        public byte Read(ushort address)
        {
            if (address < 0x4000)
            {
                int bank = mode == 1 ? (bankHigh << 5) : 0;
                return cart.Rom[MapRom(bank, address)];
            }
            if (address < 0x8000)
            {
                int low = bankLow == 0 ? 1 : bankLow;
                int bank = (bankHigh << 5) | low;
                return cart.Rom[MapRom(bank, address - 0x4000)];
            }
            if (address >= 0xA000 && address < 0xC000)
            {
                if (!ramEnabled || ramBankCount == 0)
                    return 0xFF;
                int bank = mode == 1 ? bankHigh % ramBankCount : 0;
                return cart.Ram[bank * 0x2000 + (address - 0xA000)];
            }
            return 0xFF;
        }

        public void Write(ushort address, byte value)
        {
            if (address < 0x2000)
                ramEnabled = (value & 0x0F) == 0x0A;
            else if (address < 0x4000)
                bankLow = value & 0x1F;
            else if (address < 0x6000)
                bankHigh = value & 0x03;
            else if (address < 0x8000)
                mode = value & 0x01;
            else if (address >= 0xA000 && address < 0xC000)
            {
                if (!ramEnabled || ramBankCount == 0)
                    return;
                int bank = mode == 1 ? bankHigh % ramBankCount : 0;
                cart.Ram[bank * 0x2000 + (address - 0xA000)] = value;
            }
        }

        private int MapRom(int bank, int offset)
        {
            int wrapped = bank % romBankCount;
            return wrapped * 0x4000 + offset;
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(bankLow);
            writer.Write(bankHigh);
            writer.Write(mode);
            writer.Write(ramEnabled);
        }

        public void LoadState(BinaryReader reader)
        {
            bankLow = reader.ReadInt32();
            bankHigh = reader.ReadInt32();
            mode = reader.ReadInt32();
            ramEnabled = reader.ReadBoolean();
        }
    }

    public sealed class Mbc3 : ICartridgeMapper
    {
        private readonly Cartridge cart;
        private readonly int romBankCount;
        private readonly int ramBankCount;
        private readonly Mbc3Rtc? rtc;

        private int romBank = 1;
        private int ramBank;
        private int rtcSelect = -1;
        private bool ramEnabled;

        public Mbc3(Cartridge cart, bool hasTimer)
        {
            this.cart = cart;
            romBankCount = cart.Rom.Length / 0x4000;
            ramBankCount = cart.Ram.Length > 0 ? cart.Ram.Length / 0x2000 : 0;
            rtc = hasTimer ? new Mbc3Rtc() : null;
        }

        public Mbc3Rtc? Rtc => rtc;

        public byte Read(ushort address)
        {
            if (address < 0x4000)
                return cart.Rom[address];
            if (address < 0x8000)
            {
                int bank = (romBank == 0 ? 1 : romBank) % romBankCount;
                return cart.Rom[bank * 0x4000 + (address - 0x4000)];
            }
            if (address >= 0xA000 && address < 0xC000)
            {
                if (!ramEnabled)
                    return 0xFF;
                if (rtcSelect >= 0 && rtc != null)
                    return rtc.ReadRegister(rtcSelect);
                if (ramBank < ramBankCount)
                    return cart.Ram[ramBank * 0x2000 + (address - 0xA000)];
                return 0xFF;
            }
            return 0xFF;
        }

        public void Write(ushort address, byte value)
        {
            if (address < 0x2000)
                ramEnabled = (value & 0x0F) == 0x0A;
            else if (address < 0x4000)
                romBank = value & 0x7F;
            else if (address < 0x6000)
            {
                if (value <= 0x03)
                {
                    ramBank = value;
                    rtcSelect = -1;
                }
                else if (value >= 0x08 && value <= 0x0C)
                    rtcSelect = value;
            }
            else if (address < 0x8000)
                rtc?.WriteLatch(value);
            else if (address >= 0xA000 && address < 0xC000)
            {
                if (!ramEnabled)
                    return;
                if (rtcSelect >= 0 && rtc != null)
                {
                    rtc.WriteRegister(rtcSelect, value);
                    return;
                }
                if (ramBank < ramBankCount)
                    cart.Ram[ramBank * 0x2000 + (address - 0xA000)] = value;
            }
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(romBank);
            writer.Write(ramBank);
            writer.Write(rtcSelect);
            writer.Write(ramEnabled);
            writer.Write(rtc != null);
            rtc?.SaveState(writer);
        }

        public void LoadState(BinaryReader reader)
        {
            romBank = reader.ReadInt32();
            ramBank = reader.ReadInt32();
            rtcSelect = reader.ReadInt32();
            ramEnabled = reader.ReadBoolean();
            bool hasRtc = reader.ReadBoolean();
            if (hasRtc)
                rtc?.LoadState(reader);
        }
    }

    public sealed class Mbc5 : ICartridgeMapper
    {
        private readonly Cartridge cart;
        private readonly int romBankCount;
        private readonly int ramBankCount;

        private int romBankLow = 1;
        private int romBankHigh;
        private int ramBank;
        private bool ramEnabled;

        public Mbc5(Cartridge cart)
        {
            this.cart = cart;
            romBankCount = cart.Rom.Length / 0x4000;
            ramBankCount = cart.Ram.Length > 0 ? cart.Ram.Length / 0x2000 : 0;
        }

        public byte Read(ushort address)
        {
            if (address < 0x4000)
                return cart.Rom[address];
            if (address < 0x8000)
            {
                int bank = ((romBankHigh << 8) | romBankLow) % romBankCount;
                return cart.Rom[bank * 0x4000 + (address - 0x4000)];
            }
            if (address >= 0xA000 && address < 0xC000)
            {
                if (!ramEnabled || ramBank >= ramBankCount)
                    return 0xFF;
                return cart.Ram[ramBank * 0x2000 + (address - 0xA000)];
            }
            return 0xFF;
        }

        public void Write(ushort address, byte value)
        {
            if (address < 0x2000)
                ramEnabled = (value & 0x0F) == 0x0A;
            else if (address < 0x3000)
                romBankLow = value;
            else if (address < 0x4000)
                romBankHigh = value & 0x01;
            else if (address < 0x6000)
                ramBank = value & 0x0F;
            else if (address >= 0xA000 && address < 0xC000)
            {
                if (!ramEnabled || ramBank >= ramBankCount)
                    return;
                cart.Ram[ramBank * 0x2000 + (address - 0xA000)] = value;
            }
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(romBankLow);
            writer.Write(romBankHigh);
            writer.Write(ramBank);
            writer.Write(ramEnabled);
        }

        public void LoadState(BinaryReader reader)
        {
            romBankLow = reader.ReadInt32();
            romBankHigh = reader.ReadInt32();
            ramBank = reader.ReadInt32();
            ramEnabled = reader.ReadBoolean();
        }
    }
}
