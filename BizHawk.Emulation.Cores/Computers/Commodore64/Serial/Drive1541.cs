using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
    public sealed partial class Drive1541 : SerialPortDevice
    {
        private Disk _disk;
        private int _bitHistory;
        private int _bitsRemainingInLatchedByte;
        private bool _sync;
        private bool _byteReady;
        [SaveState.DoNotSave] private readonly int _driveCpuClockNum;
        private int _trackNumber;
        private bool _motorEnabled;
        private bool _ledEnabled;
        private int _motorStep;
        private int _via0PortBtemp;
        private readonly MOS6502X _cpu;
        private readonly int[] _ram;
        public readonly Via Via0;
        public readonly Via Via1;
        private readonly int _cpuClockNum;
        private int _ratioDifference;
        private int _driveLightOffTime;
        [SaveState.DoNotSave] private int[] _trackImageData = new int[1];

        public Func<int> ReadIec = () => 0xFF;
        public Action DebuggerStep;
        public readonly Chip23128 DriveRom;

        public Drive1541(int clockNum, int clockDen)
        {
            DriveRom = new Chip23128();
            _cpu = new MOS6502X
            {
                ReadMemory = CpuRead,
                WriteMemory = CpuWrite,
                DummyReadMemory = CpuRead,
                PeekMemory = CpuPeek,
                NMI = false
            };

            _ram = new int[0x800];
            Via0 = new Via(ViaReadClock, ViaReadData, ViaReadAtn, 8);
            Via1 = new Via(ReadVia1PrA, ReadVia1PrB);

            _cpuClockNum = clockNum;
            _driveCpuClockNum = clockDen*1000000; // 1mhz
        }

        private byte CpuPeek(ushort addr)
        {
            return unchecked((byte)Peek(addr));
        }

        private byte CpuRead(ushort addr)
        {
            return unchecked((byte) Read(addr));
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

        public override void ExecutePhase()
        {
            if (_cpuClockNum > _driveCpuClockNum)
            {
                _ratioDifference += _cpuClockNum - _driveCpuClockNum;
                if (_ratioDifference > _cpuClockNum)
                {
                    _ratioDifference -= _cpuClockNum;
                    return;
                }
            }
            else if (_cpuClockNum <= _driveCpuClockNum)
            {
                _ratioDifference += _driveCpuClockNum - _cpuClockNum;
                while (_ratioDifference > _driveCpuClockNum)
                {
                    _ratioDifference -= _driveCpuClockNum;
                    ExecutePhaseInternal();
                }
            }
            ExecutePhaseInternal();
        }

        private void ExecutePhaseInternal()
        {
            Via0.Ca1 = ViaReadAtn();

            // clock output from 325572-01 drives CPU clock (phi0)
            ExecuteMotor();
            ExecuteFlux();
            Via0.ExecutePhase();
            Via1.ExecutePhase();

            // SO pin pipeline
            if ((_overflowFlagDelaySr & 0x01) != 0)
            {
                _cpu.SetOverflow();
            }
            _overflowFlagDelaySr >>= 1;

            _cpu.IRQ = !(Via0.Irq && Via1.Irq); // active low IRQ line
            _cpu.ExecuteOne();

            _via0PortBtemp = Via0.EffectivePrB;
            _ledEnabled = (_via0PortBtemp & 0x08) != 0;

            if (_ledEnabled)
            {
                _driveLightOffTime = 1000000;
            }
            else if (_driveLightOffTime > 0)
            {
                _driveLightOffTime--;
            }
        }

        public override void HardReset()
        {
            Via0.HardReset();
            Via1.HardReset();
            _trackNumber = 34;
            for (var i = 0; i < _ram.Length; i++)
            {
                _ram[i] = 0x00;
            }

            _diskDensity = 0;
            _diskFluxReversalDetected = false;
            _diskByteOffset = 0;
            _diskBitsLeft = 0;
            _diskBits = 0;
            _driveLightOffTime = 0;
            _diskDensityCounter = 0;
            _diskSupplementaryCounter = 0;
            _diskCycle = 0;
            _previousCa1 = false;
            _countsBeforeRandomTransition = 0;

            SoftReset();
            UpdateMediaData();
        }

        public void SoftReset()
        {
            _cpu.NESSoftReset();
            _overflowFlagDelaySr = 0;
        }

        public void InsertMedia(Disk disk)
        {
            _disk = disk;
            UpdateMediaData();
        }

        private void UpdateMediaData()
        {
            if (_disk != null)
            {
                _trackImageData = _disk.GetDataForTrack(_trackNumber);
                _diskBits = _trackImageData[_diskByteOffset] >> (Disk.FLUX_BITS_PER_ENTRY - _diskBitsLeft);
            }
        }

        public void RemoveMedia()
        {
            _trackImageData = new int[1];
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
