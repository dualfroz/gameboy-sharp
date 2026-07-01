namespace GameBoy.Cpu
{
    public sealed partial class Cpu
    {
        private byte GetOperand(int index)
        {
            return index switch
            {
                0 => B,
                1 => C,
                2 => D,
                3 => E,
                4 => H,
                5 => L,
                6 => Read8(HL),
                _ => A
            };
        }

        private void SetOperand(int index, byte value)
        {
            switch (index)
            {
                case 0: B = value; break;
                case 1: C = value; break;
                case 2: D = value; break;
                case 3: E = value; break;
                case 4: H = value; break;
                case 5: L = value; break;
                case 6: Write8(HL, value); break;
                default: A = value; break;
            }
        }

        private void ExecuteCb()
        {
            byte op = Fetch8();
            int index = op & 0x07;
            int y = (op >> 3) & 0x07;
            int block = op >> 6;

            byte value = GetOperand(index);

            switch (block)
            {
                case 0:
                    byte rotated = y switch
                    {
                        0 => Rlc(value),
                        1 => Rrc(value),
                        2 => Rl(value),
                        3 => Rr(value),
                        4 => Sla(value),
                        5 => Sra(value),
                        6 => Swap(value),
                        _ => Srl(value)
                    };
                    SetOperand(index, rotated);
                    break;
                case 1:
                    BitTest(y, value);
                    break;
                case 2:
                    SetOperand(index, ResetBit(y, value));
                    break;
                default:
                    SetOperand(index, SetBit(y, value));
                    break;
            }
        }
    }
}
