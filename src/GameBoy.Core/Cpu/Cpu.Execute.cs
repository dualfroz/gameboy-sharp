namespace GameBoy.Cpu
{
    public sealed partial class Cpu
    {
        private void Execute(byte opcode)
        {
            if (opcode >= 0x40 && opcode <= 0x7F)
            {
                if (opcode == 0x76)
                {
                    Halt();
                    return;
                }
                int dst = (opcode >> 3) & 0x07;
                int src = opcode & 0x07;
                SetOperand(dst, GetOperand(src));
                return;
            }

            if (opcode >= 0x80 && opcode <= 0xBF)
            {
                byte value = GetOperand(opcode & 0x07);
                switch ((opcode >> 3) & 0x07)
                {
                    case 0: Add8(value); break;
                    case 1: Adc8(value); break;
                    case 2: Sub8(value); break;
                    case 3: Sbc8(value); break;
                    case 4: And8(value); break;
                    case 5: Xor8(value); break;
                    case 6: Or8(value); break;
                    default: Cp8(value); break;
                }
                return;
            }

            switch (opcode)
            {
                case 0x00: break;
                case 0x01: BC = Fetch16(); break;
                case 0x02: Write8(BC, A); break;
                case 0x03: BC++; InternalCycle(); break;
                case 0x04: B = Inc8(B); break;
                case 0x05: B = Dec8(B); break;
                case 0x06: B = Fetch8(); break;
                case 0x07: A = Rlc(A); SetFlag(FlagZ, false); break;
                case 0x08: StoreSp(); break;
                case 0x09: AddHl(BC); InternalCycle(); break;
                case 0x0A: A = Read8(BC); break;
                case 0x0B: BC--; InternalCycle(); break;
                case 0x0C: C = Inc8(C); break;
                case 0x0D: C = Dec8(C); break;
                case 0x0E: C = Fetch8(); break;
                case 0x0F: A = Rrc(A); SetFlag(FlagZ, false); break;

                case 0x10: Stop(); break;
                case 0x11: DE = Fetch16(); break;
                case 0x12: Write8(DE, A); break;
                case 0x13: DE++; InternalCycle(); break;
                case 0x14: D = Inc8(D); break;
                case 0x15: D = Dec8(D); break;
                case 0x16: D = Fetch8(); break;
                case 0x17: A = Rl(A); SetFlag(FlagZ, false); break;
                case 0x18: JrCond(true); break;
                case 0x19: AddHl(DE); InternalCycle(); break;
                case 0x1A: A = Read8(DE); break;
                case 0x1B: DE--; InternalCycle(); break;
                case 0x1C: E = Inc8(E); break;
                case 0x1D: E = Dec8(E); break;
                case 0x1E: E = Fetch8(); break;
                case 0x1F: A = Rr(A); SetFlag(FlagZ, false); break;

                case 0x20: JrCond(!FlagSet(FlagZ)); break;
                case 0x21: HL = Fetch16(); break;
                case 0x22: Write8(HL, A); HL++; break;
                case 0x23: HL++; InternalCycle(); break;
                case 0x24: H = Inc8(H); break;
                case 0x25: H = Dec8(H); break;
                case 0x26: H = Fetch8(); break;
                case 0x27: Daa(); break;
                case 0x28: JrCond(FlagSet(FlagZ)); break;
                case 0x29: AddHl(HL); InternalCycle(); break;
                case 0x2A: A = Read8(HL); HL++; break;
                case 0x2B: HL--; InternalCycle(); break;
                case 0x2C: L = Inc8(L); break;
                case 0x2D: L = Dec8(L); break;
                case 0x2E: L = Fetch8(); break;
                case 0x2F: A = (byte)~A; SetFlag(FlagN, true); SetFlag(FlagH, true); break;

                case 0x30: JrCond(!FlagSet(FlagC)); break;
                case 0x31: SP = Fetch16(); break;
                case 0x32: Write8(HL, A); HL--; break;
                case 0x33: SP++; InternalCycle(); break;
                case 0x34: Write8(HL, Inc8(Read8(HL))); break;
                case 0x35: Write8(HL, Dec8(Read8(HL))); break;
                case 0x36: Write8(HL, Fetch8()); break;
                case 0x37: SetFlag(FlagN, false); SetFlag(FlagH, false); SetFlag(FlagC, true); break;
                case 0x38: JrCond(FlagSet(FlagC)); break;
                case 0x39: AddHl(SP); InternalCycle(); break;
                case 0x3A: A = Read8(HL); HL--; break;
                case 0x3B: SP--; InternalCycle(); break;
                case 0x3C: A = Inc8(A); break;
                case 0x3D: A = Dec8(A); break;
                case 0x3E: A = Fetch8(); break;
                case 0x3F: SetFlag(FlagN, false); SetFlag(FlagH, false); SetFlag(FlagC, !FlagSet(FlagC)); break;

                case 0xC0: RetCond(!FlagSet(FlagZ)); break;
                case 0xC1: BC = Pop16(); break;
                case 0xC2: JpCond(!FlagSet(FlagZ)); break;
                case 0xC3: JpCond(true); break;
                case 0xC4: CallCond(!FlagSet(FlagZ)); break;
                case 0xC5: InternalCycle(); Push16(BC); break;
                case 0xC6: Add8(Fetch8()); break;
                case 0xC7: Rst(0x00); break;
                case 0xC8: RetCond(FlagSet(FlagZ)); break;
                case 0xC9: PC = Pop16(); InternalCycle(); break;
                case 0xCA: JpCond(FlagSet(FlagZ)); break;
                case 0xCB: ExecuteCb(); break;
                case 0xCC: CallCond(FlagSet(FlagZ)); break;
                case 0xCD: CallCond(true); break;
                case 0xCE: Adc8(Fetch8()); break;
                case 0xCF: Rst(0x08); break;

                case 0xD0: RetCond(!FlagSet(FlagC)); break;
                case 0xD1: DE = Pop16(); break;
                case 0xD2: JpCond(!FlagSet(FlagC)); break;
                case 0xD4: CallCond(!FlagSet(FlagC)); break;
                case 0xD5: InternalCycle(); Push16(DE); break;
                case 0xD6: Sub8(Fetch8()); break;
                case 0xD7: Rst(0x10); break;
                case 0xD8: RetCond(FlagSet(FlagC)); break;
                case 0xD9: PC = Pop16(); InternalCycle(); Ime = true; imeScheduled = false; break;
                case 0xDA: JpCond(FlagSet(FlagC)); break;
                case 0xDC: CallCond(FlagSet(FlagC)); break;
                case 0xDE: Sbc8(Fetch8()); break;
                case 0xDF: Rst(0x18); break;

                case 0xE0: Write8((ushort)(0xFF00 + Fetch8()), A); break;
                case 0xE1: HL = Pop16(); break;
                case 0xE2: Write8((ushort)(0xFF00 + C), A); break;
                case 0xE5: InternalCycle(); Push16(HL); break;
                case 0xE6: And8(Fetch8()); break;
                case 0xE7: Rst(0x20); break;
                case 0xE8: SP = AddSpOffset(Fetch8()); InternalCycle(); InternalCycle(); break;
                case 0xE9: PC = HL; break;
                case 0xEA: Write8(Fetch16(), A); break;
                case 0xEE: Xor8(Fetch8()); break;
                case 0xEF: Rst(0x28); break;

                case 0xF0: A = Read8((ushort)(0xFF00 + Fetch8())); break;
                case 0xF1: AF = Pop16(); break;
                case 0xF2: A = Read8((ushort)(0xFF00 + C)); break;
                case 0xF3: Ime = false; imeScheduled = false; break;
                case 0xF5: InternalCycle(); Push16(AF); break;
                case 0xF6: Or8(Fetch8()); break;
                case 0xF7: Rst(0x30); break;
                case 0xF8: HL = AddSpOffset(Fetch8()); InternalCycle(); break;
                case 0xF9: SP = HL; InternalCycle(); break;
                case 0xFA: A = Read8(Fetch16()); break;
                case 0xFB: imeScheduled = true; break;
                case 0xFE: Cp8(Fetch8()); break;
                case 0xFF: Rst(0x38); break;

                default: break;
            }
        }

        private void StoreSp()
        {
            ushort address = Fetch16();
            Write8(address, (byte)(SP & 0xFF));
            Write8((ushort)(address + 1), (byte)(SP >> 8));
        }

        private void Halt()
        {
            if (!Ime && interrupts.AnyPending)
                haltBug = true;
            else
                Halted = true;
        }

        private void Stop()
        {
            Fetch8();
            Stopped = false;
        }

        private void JrCond(bool take)
        {
            sbyte offset = (sbyte)Fetch8();
            if (take)
            {
                InternalCycle();
                PC = (ushort)(PC + offset);
            }
        }

        private void JpCond(bool take)
        {
            ushort address = Fetch16();
            if (take)
            {
                InternalCycle();
                PC = address;
            }
        }

        private void CallCond(bool take)
        {
            ushort address = Fetch16();
            if (take)
            {
                InternalCycle();
                Push16(PC);
                PC = address;
            }
        }

        private void RetCond(bool take)
        {
            InternalCycle();
            if (take)
            {
                PC = Pop16();
                InternalCycle();
            }
        }

        private void Rst(byte vector)
        {
            InternalCycle();
            Push16(PC);
            PC = vector;
        }
    }
}
