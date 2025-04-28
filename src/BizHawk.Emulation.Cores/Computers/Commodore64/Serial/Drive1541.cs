using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public sealed partial class Drive1541 : SerialPortDevice
	{
		private byte[][][] _diskDeltas;
		private Disk _disk;
		private int _bitHistory;
		private int _bitsRemainingInLatchedByte;
		private bool _sync;
		private bool _byteReady;
		private int _driveCpuClockNum;
		private int _trackNumber;
		private bool _motorEnabled;
		private bool _ledEnabled;
		private int _motorStep;
		private readonly MOS6502X<CpuLink> _cpu;
		private int[] _ram;
		public readonly Via Via0;
		public readonly Via Via1;
		private int _cpuClockNum;
		private int _ratioDifference;
		private int _driveLightOffTime;
		public Func<int> ReadIec = () => 0xFF;
		public Action DebuggerStep;
		public readonly Chip23128 DriveRom;
		private bool _via1Ca1;

		private struct CpuLink : IMOS6502XLink
		{
			private readonly Drive1541 _drive;

			public CpuLink(Drive1541 drive)
			{
				_drive = drive;
			}

			public byte DummyReadMemory(ushort address) => unchecked((byte)_drive.Read(address));

			public void OnExecFetch(ushort address) { }

			public byte PeekMemory(ushort address) => unchecked((byte)_drive.Peek(address));

			public byte ReadMemory(ushort address) => unchecked((byte)_drive.Read(address));

			public void WriteMemory(ushort address, byte value) => _drive.Write(address, value);
		}

		public Drive1541(int clockNum, int clockDen, Func<int> getCurrentDiskNumber)
		{
			DriveRom = new Chip23128();
			_cpu = new MOS6502X<CpuLink>(new CpuLink(this))
			{
				NMI = false,
				AneConstant = 0xEF,
				LxaConstant = 0xFE
			};

			_ram = new int[0x800];
			Via0 = Chip6522.Create(ViaReadClock, ViaReadData, ViaReadAtn, 8);
			Via0.ReadCa1 = ViaReadAtn;
			Via1 = Chip6522.Create(ReadVia1PrA, ReadVia1PrB);
			Via1.ReadCa1 = ViaReadByteReady;

			_cpuClockNum = clockNum;
			_driveCpuClockNum = clockDen * 16000000; // 16mhz
			_getCurrentDiskNumber = getCurrentDiskNumber;
		}

		public override void SyncState(Serializer ser)
		{
			ser.BeginSection("Disk");
			_disk?.SyncState(ser);
			ser.EndSection();

			ser.Sync("BitHistory", ref _bitHistory);
			ser.Sync("BitsRemainingInLatchedByte", ref _bitsRemainingInLatchedByte);
			ser.Sync("Sync", ref _sync);
			ser.Sync("ByteReady", ref _byteReady);
			ser.Sync("DriveCpuClockNumerator", ref _driveCpuClockNum);
			ser.Sync("TrackNumber", ref _trackNumber);
			ser.Sync("MotorEnabled", ref _motorEnabled);
			ser.Sync("LedEnabled", ref _ledEnabled);
			ser.Sync("MotorStep", ref _motorStep);

			ser.BeginSection("Disk6502");
			_cpu.SyncState(ser);
			ser.EndSection();

			ser.Sync("RAM", ref _ram, useNull: false);

			ser.BeginSection("VIA0");
			Via0.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("VIA1");
			Via1.SyncState(ser);
			ser.EndSection();

			ser.Sync(nameof(_cpuClockNum), ref _cpuClockNum);
			ser.Sync(nameof(_ratioDifference), ref _ratioDifference);
			ser.Sync(nameof(_driveLightOffTime), ref _driveLightOffTime);

			ser.Sync(nameof(_diskDensityCounter), ref _diskDensityCounter);
			ser.Sync(nameof(_diskSupplementaryCounter), ref _diskSupplementaryCounter);
			ser.Sync(nameof(_diskFluxReversalDetected), ref _diskFluxReversalDetected);
			ser.Sync(nameof(_diskBitsLeft), ref _diskBitsLeft);
			ser.Sync(nameof(_diskByteOffset), ref _diskByteOffset);
			ser.Sync(nameof(_diskBits), ref _diskBits);
			ser.Sync(nameof(_diskCycle), ref _diskCycle);
			ser.Sync(nameof(_diskDensity), ref _diskDensity);
			ser.Sync(nameof(_previousCa1), ref _previousCa1);
			ser.Sync(nameof(_countsBeforeRandomTransition), ref _countsBeforeRandomTransition);
			ser.Sync(nameof(_rngCurrent), ref _rngCurrent);
			ser.Sync(nameof(_clocks), ref _clocks);
			ser.Sync(nameof(_cpuClocks), ref _cpuClocks);
			ser.Sync(nameof(_overflowFlagDelaySr), ref _overflowFlagDelaySr);
			ser.Sync(nameof(_diskWriteBitsRemaining), ref _diskWriteBitsRemaining);
			ser.Sync(nameof(_diskWriteEnabled), ref _diskWriteEnabled);
			ser.Sync(nameof(_diskWriteLatch), ref _diskWriteLatch);
			ser.Sync(nameof(_diskOutputBits), ref _diskOutputBits);
			ser.Sync(nameof(_diskWriteProtected), ref _diskWriteProtected);
			ser.Sync(nameof(_via1Ca1), ref _via1Ca1);

			if (ser.IsReader)
			{
				ResetDeltas();
			}
			else
			{
				SaveDeltas();
			}

			for (var diskNumber = 0; diskNumber < _usedDiskTracks.Length; diskNumber++)
			{
				ser.Sync($"{nameof(_usedDiskTracks)}{diskNumber}", ref _usedDiskTracks[diskNumber], useNull: false);
				for (var trackNumber = 0; trackNumber < 84; trackNumber++)
				{
					ser.Sync($"{nameof(_diskDeltas)}{diskNumber},{trackNumber}", ref _diskDeltas[diskNumber][trackNumber], useNull: true);
				}
			}

			if (ser.IsReader)
			{
				LoadDeltas();
			}
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
			Via0.ExecutePhase();
			Via1.ExecutePhase();

			// SO pin pipeline
			if ((_overflowFlagDelaySr & 0x01) != 0)
			{
				_cpu.SetOverflow();
			}

			_overflowFlagDelaySr >>= 1;

			_cpu.IRQ = Via0.Irq || Via1.Irq;
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
			_ram.AsSpan().Fill(0);
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
				var track = _disk.Tracks[_trackNumber];
				_diskBits = track.Bits[_diskByteOffset] >> (DiskTrack.FluxBitsPerEntry - _diskBitsLeft);
				_diskWriteProtected = _disk.WriteProtected;
			}
			else
			{
				_diskWriteProtected = true;
			}
		}

		public void RemoveMedia()
		{
			_disk = null;
			_diskBits = 0;
		}

	}
}
