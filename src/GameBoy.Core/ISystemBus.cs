using GameBoy.Cpu;

namespace GameBoy
{
    public interface ISystemBus
    {
        byte Read(ushort address);
        void Write(ushort address, byte value);
        void Tick4();
        InterruptController Interrupts { get; }
    }
}
