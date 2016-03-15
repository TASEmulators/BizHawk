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
        [SaveState.SaveWithName("Disk")]
        private Disk _disk;
        [SaveState.SaveWithName("BitHistory")]
        private int _bitHistory;
        [SaveState.SaveWithName("BitsRemainingInLatchedByte")]
        private int _bitsRemainingInLatchedByte;
        [SaveState.SaveWithName("Sync")]
        private bool _sync;
        [SaveState.SaveWithName("ByteReady")]
        private bool _byteReady;
        [SaveState.SaveWithName("DriveCpuClockNumerator")]
        private readonly int _driveCpuClockNum;
        [SaveState.SaveWithName("TrackNumber")]
        private int _trackNumber;
        [SaveState.SaveWithName("MotorEnabled")]
        private bool _motorEnabled;
        [SaveState.SaveWithName("LedEnabled")]
        private bool _ledEnabled;
        [SaveState.SaveWithName("MotorStep")]
        private int _motorStep;
        [SaveState.SaveWithName("CPU")]
        private readonly MOS6502X _cpu;
        [SaveState.SaveWithName("RAM")]
        private readonly int[] _ram;
        [SaveState.SaveWithName("VIA0")]
        public readonly Via Via0;
        [SaveState.SaveWithName("VIA1")]
        public readonly Via Via1;
        [SaveState.SaveWithName("SystemCpuClockNumerator")]
        private readonly int _cpuClockNum;
        [SaveState.SaveWithName("SystemDriveCpuRatioDifference")]
        private int _ratioDifference;
        [SaveState.SaveWithName("DriveLightOffTime")]
        private int _driveLightOffTime;
        [SaveState.DoNotSave]
        private int[] _trackImageData = new int[1];
        [SaveState.DoNotSave]
        public Func<int> ReadIec = () => 0xFF;
        [SaveState.DoNotSave]
        public Action DebuggerStep;
        [SaveState.DoNotSave]
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
            Via0 = Chip6522.Create(ViaReadClock, ViaReadData, ViaReadAtn, 8);
            Via1 = Chip6522.Create(ReadVia1PrA, ReadVia1PrB);

            _cpuClockNum = clockNum;
            _driveCpuClockNum = clockDen*16000000; // 16mhz
        }

        public override void ExecutePhase()
        {
            _ratioDifference += _driveCpuClockNum;
            while (_ratioDifference > _cpuClockNum)
            {
                _ratioDifference -= _cpuClockNum;
                _clocks++;
            }
            ExecutePhaseInternal();
        }

        private void ExecutePhaseInternal()
        {
            // clock output from 325572-01 drives CPU clock (phi0)
            ExecuteMotor();
            ExecuteFlux();
        }

        private void ExecuteSystem()
        {
            Via0.Ca1 = ViaReadAtn();
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

            if (_ledEnabled)
            {
                _driveLightOffTime = 25000;
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
                _diskBits = _trackImageData[_diskByteOffset] >> (Disk.FluxBitsPerEntry - _diskBitsLeft);
            }
        }

        public void RemoveMedia()
        {
            _disk = null;
            _trackImageData = null;
            _diskBits = 0;
        }
    }
}
