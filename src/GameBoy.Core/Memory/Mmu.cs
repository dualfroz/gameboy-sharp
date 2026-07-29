using System;
using System.IO;
using GameBoy.Audio;
using GameBoy.Cpu;
using GameBoy.Input;
using GameBoy.Serial;
using GameBoy.Timing;
using GameBoy.Video;

namespace GameBoy.Memory
{
    public sealed class Mmu
    {
        private readonly Cartridge.Cartridge cartridge;
        private readonly Ppu ppu;
        private readonly Timer timer;
        private readonly Joypad joypad;
        private readonly SerialPort serial;
        private readonly Apu apu;
        private readonly InterruptController interrupts;

        private readonly byte[] wram = new byte[0x2000];
        private readonly byte[] hram = new byte[0x7F];

        public OamDma? Dma;

        public Mmu(
            Cartridge.Cartridge cartridge,
            Ppu ppu,
            Timer timer,
            Joypad joypad,
            SerialPort serial,
            Apu apu,
            InterruptController interrupts)
        {
            this.cartridge = cartridge;
            this.ppu = ppu;
            this.timer = timer;
            this.joypad = joypad;
            this.serial = serial;
            this.apu = apu;
            this.interrupts = interrupts;
        }

        public byte Read(ushort address)
        {
            if (address < 0x8000)
                return cartridge.Read(address);
            if (address < 0xA000)
                return ppu.ReadVram(address);
            if (address < 0xC000)
                return cartridge.Read(address);
            if (address < 0xE000)
                return wram[address - 0xC000];
            if (address < 0xFE00)
                return wram[address - 0xE000];
            if (address < 0xFEA0)
                return ppu.ReadOam(address);
            if (address < 0xFF00)
                return 0xFF;
            if (address < 0xFF80)
                return ReadIo(address);
            if (address < 0xFFFF)
                return hram[address - 0xFF80];
            return interrupts.Enable;
        }

        public void Write(ushort address, byte value)
        {
            if (address < 0x8000)
                cartridge.Write(address, value);
            else if (address < 0xA000)
                ppu.WriteVram(address, value);
            else if (address < 0xC000)
                cartridge.Write(address, value);
            else if (address < 0xE000)
                wram[address - 0xC000] = value;
            else if (address < 0xFE00)
                wram[address - 0xE000] = value;
            else if (address < 0xFEA0)
                ppu.WriteOam(address, value);
            else if (address < 0xFF00)
            {
            }
            else if (address < 0xFF80)
                WriteIo(address, value);
            else if (address < 0xFFFF)
                hram[address - 0xFF80] = value;
            else
                interrupts.Enable = value;
        }

        private byte ReadIo(ushort address)
        {
            if (address == 0xFF00)
                return joypad.Read();
            if (address == 0xFF01 || address == 0xFF02)
                return serial.Read(address);
            if (address >= 0xFF04 && address <= 0xFF07)
                return timer.Read(address);
            if (address == 0xFF0F)
                return interrupts.ReadFlag();
            if (address >= 0xFF10 && address <= 0xFF3F)
                return apu.Read(address);
            if (address == 0xFF46)
                return Dma!.Page;
            if (address >= 0xFF40 && address <= 0xFF4B)
                return ppu.ReadRegister(address);
            return 0xFF;
        }

        private void WriteIo(ushort address, byte value)
        {
            if (address == 0xFF00)
                joypad.Write(value);
            else if (address == 0xFF01 || address == 0xFF02)
                serial.Write(address, value);
            else if (address >= 0xFF04 && address <= 0xFF07)
                timer.Write(address, value);
            else if (address == 0xFF0F)
                interrupts.WriteFlag(value);
            else if (address >= 0xFF10 && address <= 0xFF3F)
                apu.Write(address, value);
            else if (address == 0xFF46)
                Dma!.Start(value);
            else if (address >= 0xFF40 && address <= 0xFF4B)
                ppu.WriteRegister(address, value);
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(wram);
            writer.Write(hram);
        }

        public void LoadState(BinaryReader reader)
        {
            Array.Copy(reader.ReadBytes(wram.Length), wram, wram.Length);
            Array.Copy(reader.ReadBytes(hram.Length), hram, hram.Length);
        }
    }
}
