using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
    public sealed partial class Drive1541
    {
        [SaveState.SaveWithName("OverflowFlagDelayShiftRegister")]
        private int _overflowFlagDelaySr;

        private byte CpuPeek(ushort addr)
        {
            return unchecked((byte)Peek(addr));
        }

        private byte CpuRead(ushort addr)
        {
            return unchecked((byte)Read(addr));
        }

        private void CpuWrite(ushort addr, byte val)
        {
            Write(addr, val);
        }

        private bool ViaReadClock()
        {
            var inputClock = ReadMasterClk();
            var outputClock = ReadDeviceClk();
            return !(inputClock && outputClock);
        }

        private bool ViaReadData()
        {
            var inputData = ReadMasterData();
            var outputData = ReadDeviceData();
            return !(inputData && outputData);
        }

        private bool ViaReadAtn()
        {
            var inputAtn = ReadMasterAtn();
            return !inputAtn;
        }

        private int ReadVia1PrA()
        {
            return _bitHistory & 0xFF;
        }

        private int ReadVia1PrB()
        {
            return (_motorStep & 0x03) | (_motorEnabled ? 0x04 : 0x00) | (_sync ? 0x00 : 0x80);
        }

        public int Peek(int addr)
        {
            switch (addr & 0xFC00)
            {
                case 0x1800:
                    return Via0.Peek(addr);
                case 0x1C00:
                    return Via1.Peek(addr);
            }
            if ((addr & 0x8000) != 0)
                return DriveRom.Peek(addr & 0x3FFF);
            if ((addr & 0x1F00) < 0x800)
                return _ram[addr & 0x7FF];
            return (addr >> 8) & 0xFF;
        }

        public int PeekVia0(int addr)
        {
            return Via0.Peek(addr);
        }

        public int PeekVia1(int addr)
        {
            return Via1.Peek(addr);
        }

        public void Poke(int addr, int val)
        {
            switch (addr & 0xFC00)
            {
                case 0x1800:
                    Via0.Poke(addr, val);
                    break;
                case 0x1C00:
                    Via1.Poke(addr, val);
                    break;
                default:
                    if ((addr & 0x8000) == 0 && (addr & 0x1F00) < 0x800)
                        _ram[addr & 0x7FF] = val & 0xFF;
                    break;
            }
        }

        public void PokeVia0(int addr, int val)
        {
            Via0.Poke(addr, val);
        }

        public void PokeVia1(int addr, int val)
        {
            Via1.Poke(addr, val);
        }

        public int Read(int addr)
        {
            switch (addr & 0xFC00)
            {
                case 0x1800:
                    return Via0.Read(addr);
                case 0x1C00:
                    return Via1.Read(addr);
            }
            if ((addr & 0x8000) != 0)
                return DriveRom.Read(addr & 0x3FFF);
            if ((addr & 0x1F00) < 0x800)
                return _ram[addr & 0x7FF];
            return (addr >> 8) & 0xFF;
        }

        public void Write(int addr, int val)
        {
            switch (addr & 0xFC00)
            {
                case 0x1800:
                    Via0.Write(addr, val);
                    break;
                case 0x1C00:
                    Via1.Write(addr, val);
                    break;
                default:
                    if ((addr & 0x8000) == 0 && (addr & 0x1F00) < 0x800)
                        _ram[addr & 0x7FF] = val & 0xFF;
                    break;
            }
        }

        public override bool ReadDeviceClk()
        {
            var viaOutputClock = (Via0.DdrB & 0x08) != 0 && (Via0.PrB & 0x08) != 0;
            return !viaOutputClock;
        }

        public override bool ReadDeviceData()
        {
            var viaOutputData = (Via0.DdrB & 0x02) != 0 && (Via0.PrB & 0x02) != 0;
            var viaInputAtn = ViaReadAtn();
            var viaOutputAtna = (Via0.DdrB & 0x10) != 0 && (Via0.PrB & 0x10) != 0;

            return !(viaOutputAtna ^ viaInputAtn) && !viaOutputData;
        }

        public override bool ReadDeviceLight()
        {
            return _driveLightOffTime > 0;
        }
    }
}
