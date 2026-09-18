using System;
using System.IO;
using GameBoy.Audio;

namespace GameBoy
{
    public sealed partial class Emulator
    {
        private const uint SnapshotMagic = 0x31545347;

        public Apu Apu => apu;

        public bool HasBattery => Cartridge.HasBattery;
        public bool HasRtc => Cartridge.HasRtc;

        public byte ReadMemory(ushort address) => mmu.Read(address);

        public void WriteMemory(ushort address, byte value) => mmu.Write(address, value);

        public void AdvanceRtcSeconds(long seconds) => Cartridge.AdvanceRtc(seconds);

        public byte[] SaveRam() => (byte[])Cartridge.Ram.Clone();

        public void LoadRam(byte[] contents)
        {
            Array.Copy(contents, 0, Cartridge.Ram, 0, Math.Min(contents.Length, Cartridge.Ram.Length));
        }

        public byte[] SaveBattery(long unixSeconds) => Cartridge.SaveBattery(unixSeconds);

        public void LoadBattery(byte[] contents, long unixSeconds) => Cartridge.LoadBattery(contents, unixSeconds);

        public byte[] SaveState()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(SnapshotMagic);
            writer.Write(TotalCycles);
            cpu.SaveState(writer);
            interrupts.SaveState(writer);
            ppu.SaveState(writer);
            timer.SaveState(writer);
            joypad.SaveState(writer);
            serial.SaveState(writer);
            apu.SaveState(writer);
            mmu.SaveState(writer);
            dma.SaveState(writer);
            Cartridge.SaveState(writer);

            writer.Flush();
            return stream.ToArray();
        }

        public void LoadState(byte[] snapshot)
        {
            using var stream = new MemoryStream(snapshot);
            using var reader = new BinaryReader(stream);

            uint magic = reader.ReadUInt32();
            if (magic != SnapshotMagic)
                throw new InvalidDataException("unrecognized save state header");

            TotalCycles = reader.ReadInt64();
            cpu.LoadState(reader);
            interrupts.LoadState(reader);
            ppu.LoadState(reader);
            timer.LoadState(reader);
            joypad.LoadState(reader);
            serial.LoadState(reader);
            apu.LoadState(reader);
            mmu.LoadState(reader);
            dma.LoadState(reader);
            Cartridge.LoadState(reader);
        }
    }
}
