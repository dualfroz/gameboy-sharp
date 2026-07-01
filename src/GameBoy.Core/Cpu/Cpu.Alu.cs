namespace GameBoy.Cpu
{
    public sealed partial class Cpu
    {
        private void Add8(byte value)
        {
            int r = A + value;
            SetFlags((byte)r == 0, false, ((A & 0xF) + (value & 0xF)) > 0xF, r > 0xFF);
            A = (byte)r;
        }

        private void Adc8(byte value)
        {
            int carry = FlagSet(FlagC) ? 1 : 0;
            int r = A + value + carry;
            SetFlags((byte)r == 0, false, ((A & 0xF) + (value & 0xF) + carry) > 0xF, r > 0xFF);
            A = (byte)r;
        }

        private void Sub8(byte value)
        {
            int r = A - value;
            SetFlags((byte)r == 0, true, ((A & 0xF) - (value & 0xF)) < 0, r < 0);
            A = (byte)r;
        }

        private void Sbc8(byte value)
        {
            int carry = FlagSet(FlagC) ? 1 : 0;
            int r = A - value - carry;
            SetFlags((byte)r == 0, true, ((A & 0xF) - (value & 0xF) - carry) < 0, r < 0);
            A = (byte)r;
        }

        private void And8(byte value)
        {
            A &= value;
            SetFlags(A == 0, false, true, false);
        }

        private void Or8(byte value)
        {
            A |= value;
            SetFlags(A == 0, false, false, false);
        }

        private void Xor8(byte value)
        {
            A ^= value;
            SetFlags(A == 0, false, false, false);
        }

        private void Cp8(byte value)
        {
            int r = A - value;
            SetFlags((byte)r == 0, true, ((A & 0xF) - (value & 0xF)) < 0, r < 0);
        }

        private byte Inc8(byte value)
        {
            byte r = (byte)(value + 1);
            SetFlags(r == 0, false, (value & 0x0F) == 0x0F, FlagSet(FlagC));
            return r;
        }

        private byte Dec8(byte value)
        {
            byte r = (byte)(value - 1);
            SetFlags(r == 0, true, (value & 0x0F) == 0x00, FlagSet(FlagC));
            return r;
        }

        private void AddHl(ushort value)
        {
            int r = HL + value;
            SetFlag(FlagN, false);
            SetFlag(FlagH, ((HL & 0x0FFF) + (value & 0x0FFF)) > 0x0FFF);
            SetFlag(FlagC, r > 0xFFFF);
            HL = (ushort)r;
        }

        private ushort AddSpOffset(byte value)
        {
            int offset = (sbyte)value;
            int r = SP + offset;
            SetFlags(false, false, ((SP & 0xF) + (value & 0xF)) > 0xF, ((SP & 0xFF) + (value & 0xFF)) > 0xFF);
            return (ushort)r;
        }

        private void Daa()
        {
            int a = A;
            if (!FlagSet(FlagN))
            {
                if (FlagSet(FlagC) || a > 0x99)
                {
                    a += 0x60;
                    SetFlag(FlagC, true);
                }
                if (FlagSet(FlagH) || (a & 0x0F) > 0x09)
                    a += 0x06;
            }
            else
            {
                if (FlagSet(FlagC))
                    a -= 0x60;
                if (FlagSet(FlagH))
                    a -= 0x06;
            }
            A = (byte)a;
            SetFlag(FlagZ, A == 0);
            SetFlag(FlagH, false);
        }

        private byte Rlc(byte v)
        {
            int carry = (v >> 7) & 1;
            byte r = (byte)((v << 1) | carry);
            SetFlags(r == 0, false, false, carry != 0);
            return r;
        }

        private byte Rrc(byte v)
        {
            int carry = v & 1;
            byte r = (byte)((v >> 1) | (carry << 7));
            SetFlags(r == 0, false, false, carry != 0);
            return r;
        }

        private byte Rl(byte v)
        {
            int inCarry = FlagSet(FlagC) ? 1 : 0;
            int outCarry = (v >> 7) & 1;
            byte r = (byte)((v << 1) | inCarry);
            SetFlags(r == 0, false, false, outCarry != 0);
            return r;
        }

        private byte Rr(byte v)
        {
            int inCarry = FlagSet(FlagC) ? 1 : 0;
            int outCarry = v & 1;
            byte r = (byte)((v >> 1) | (inCarry << 7));
            SetFlags(r == 0, false, false, outCarry != 0);
            return r;
        }

        private byte Sla(byte v)
        {
            int carry = (v >> 7) & 1;
            byte r = (byte)(v << 1);
            SetFlags(r == 0, false, false, carry != 0);
            return r;
        }

        private byte Sra(byte v)
        {
            int carry = v & 1;
            byte r = (byte)((v >> 1) | (v & 0x80));
            SetFlags(r == 0, false, false, carry != 0);
            return r;
        }

        private byte Swap(byte v)
        {
            byte r = (byte)((v >> 4) | (v << 4));
            SetFlags(r == 0, false, false, false);
            return r;
        }

        private byte Srl(byte v)
        {
            int carry = v & 1;
            byte r = (byte)(v >> 1);
            SetFlags(r == 0, false, false, carry != 0);
            return r;
        }

        private void BitTest(int n, byte v)
        {
            SetFlag(FlagZ, (v & (1 << n)) == 0);
            SetFlag(FlagN, false);
            SetFlag(FlagH, true);
        }

        private static byte ResetBit(int n, byte v) => (byte)(v & ~(1 << n));

        private static byte SetBit(int n, byte v) => (byte)(v | (1 << n));
    }
}
