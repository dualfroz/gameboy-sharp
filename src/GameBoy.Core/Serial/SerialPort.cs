using System.IO;
using System.Text;
using GameBoy.Cpu;

namespace GameBoy.Serial
{
    public sealed class SerialPort
    {
        private readonly InterruptController interrupts;
        private readonly StringBuilder output = new StringBuilder();
        private byte data;
        private byte control;

        public SerialPort(InterruptController interrupts)
        {
            this.interrupts = interrupts;
        }

        public string Output => output.ToString();

        public byte Read(ushort address)
        {
            return (address & 0xFF) switch
            {
                0x01 => data,
                0x02 => (byte)(control | 0x7E),
                _ => 0xFF
            };
        }

        public void Write(ushort address, byte value)
        {
            switch (address & 0xFF)
            {
                case 0x01:
                    data = value;
                    break;
                case 0x02:
                    control = value;
                    if ((value & 0x81) == 0x81)
                    {
                        output.Append((char)data);
                        data = 0xFF;
                        control &= 0x7F;
                        interrupts.Request(Interrupt.Serial);
                    }
                    break;
            }
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(data);
            writer.Write(control);
            writer.Write(output.ToString());
        }

        public void LoadState(BinaryReader reader)
        {
            data = reader.ReadByte();
            control = reader.ReadByte();
            output.Clear();
            output.Append(reader.ReadString());
        }
    }
}
