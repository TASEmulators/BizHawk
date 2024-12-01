using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public sealed partial class Drive1541 : SerialPortDevice, ISaveRam
	{
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
		private int[] _trackImageData;
		public Func<int> ReadIec = () => 0xFF;
		public Action DebuggerStep;
		public readonly Chip23128 DriveRom;

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
				NMI = false
			};

			_ram = new int[0x800];
			Via0 = Chip6522.Create(ViaReadClock, ViaReadData, ViaReadAtn, 8);
			Via1 = Chip6522.Create(ReadVia1PrA, ReadVia1PrB);

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

			ser.Sync("SystemCpuClockNumerator", ref _cpuClockNum);
			ser.Sync("SystemDriveCpuRatioDifference", ref _ratioDifference);
			ser.Sync("DriveLightOffTime", ref _driveLightOffTime);

			ser.Sync("DiskDensityCounter", ref _diskDensityCounter);
			ser.Sync("DiskSupplementaryCounter", ref _diskSupplementaryCounter);
			ser.Sync("DiskFluxReversalDetected", ref _diskFluxReversalDetected);
			ser.Sync("DiskBitsRemainingInDataEntry", ref _diskBitsLeft);
			ser.Sync("DiskDataEntryIndex", ref _diskByteOffset);
			ser.Sync("DiskDataEntry", ref _diskBits);
			ser.Sync("DiskCurrentCycle", ref _diskCycle);
			ser.Sync("DiskDensityConfig", ref _diskDensity);
			ser.Sync("PreviousCA1", ref _previousCa1);
			ser.Sync("CountsBeforeRandomTransition", ref _countsBeforeRandomTransition);
			ser.Sync("CurrentRNG", ref _rngCurrent);
			ser.Sync("Clocks", ref _clocks);
			ser.Sync("CpuClocks", ref _cpuClocks);
			ser.Sync("OverflowFlagDelayShiftRegister", ref _overflowFlagDelaySr);
			ser.Sync("DiskWriteBitsRemaining", ref _diskWriteBitsRemaining);
			ser.Sync("DiskWriteEnabled", ref _diskWriteEnabled);
			ser.Sync("DiskWriteLatch", ref _diskWriteLatch);
			ser.Sync("DiskOutputBits", ref _diskOutputBits);
			ser.Sync("DiskWriteProtected", ref _diskWriteProtected);

			if (ser.IsReader)
			{
				ResetDeltas();
			}
			else
			{
				SaveDeltas();
			}

			for (var i = 0; i < _usedDiskTracks.Length; i++)
			{
				ser.Sync($"_usedDiskTracks{i}", ref _usedDiskTracks[i], useNull: false);
				for (var j = 0; j < 84; j++)
				{
					ser.Sync($"DiskDeltas{i},{j}", ref _diskDeltas[i, j], useNull: true);
				}
			}

			_disk?.AttachTracker(_usedDiskTracks[_getCurrentDiskNumber()]);

			if (ser.IsReader)
			{
				LoadDeltas();
			}

			// set _trackImageData back to the correct reference
			_trackImageData = _disk?.GetDataForTrack(_trackNumber);
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
			_disk?.AttachTracker(_usedDiskTracks[_getCurrentDiskNumber()]);
			UpdateMediaData();
		}

		private void UpdateMediaData()
		{
			if (_disk != null)
			{
				_trackImageData = _disk.GetDataForTrack(_trackNumber);
				_diskBits = _trackImageData[_diskByteOffset] >> (Disk.FluxBitsPerEntry - _diskBitsLeft);
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
			_trackImageData = null;
			_diskBits = 0;
		}

		// ISaveRam implementation

		// this is some extra state used to keep savestate size down, as most tracks don't get used
		// we keep it here for all disks as we need to remember it when swapping disks around
		// _usedDiskTracks.Length also doubles as a way to remember the disk count
		private bool[][] _usedDiskTracks;
		private byte[,][] _diskDeltas;
		private readonly Func<int> _getCurrentDiskNumber;

		public void InitSaveRam(int diskCount)
		{
			_usedDiskTracks = new bool[diskCount][];
			_diskDeltas = new byte[diskCount, 84][];
			for (var i = 0; i < diskCount; i++)
			{
				_usedDiskTracks[i] = new bool[84];
			}
		}

		public bool SaveRamModified => true;

		public byte[] CloneSaveRam()
		{
			SaveDeltas(); // update the current deltas

			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			bw.Write(_usedDiskTracks.Length);
			for (var i = 0; i < _usedDiskTracks.Length; i++)
			{
				bw.WriteByteBuffer(_usedDiskTracks[i].ToUByteBuffer());
				for (var j = 0; j < 84; j++)
				{
					bw.WriteByteBuffer(_diskDeltas[i, j]);
				}
			}

			return ms.ToArray();
		}

		public void StoreSaveRam(byte[] data)
		{
			using var ms = new MemoryStream(data, false);
			using var br = new BinaryReader(ms);

			var ndisks = br.ReadInt32();
			if (ndisks != _usedDiskTracks.Length)
			{
				throw new InvalidOperationException("Disk count mismatch!");
			}

			ResetDeltas();

			for (var i = 0; i < _usedDiskTracks.Length; i++)
			{
				_usedDiskTracks[i] = br.ReadByteBuffer(returnNull: false)!.ToBoolBuffer();
				for (var j = 0; j < 84; j++)
				{
					_diskDeltas[i, j] = br.ReadByteBuffer(returnNull: true);
				}
			}

			_disk?.AttachTracker(_usedDiskTracks[_getCurrentDiskNumber()]);
			LoadDeltas(); // load up new deltas
			_usedDiskTracks[_getCurrentDiskNumber()][_trackNumber] = true; // make sure this gets set to true now
		}

		public void SaveDeltas()
		{
			_disk?.DeltaUpdate((tracknum, original, current) =>
			{
				_diskDeltas[_getCurrentDiskNumber(), tracknum] = DeltaSerializer.GetDelta<int>(original, current).ToArray();
			});
		}

		public void LoadDeltas()
		{
			_disk?.DeltaUpdate((tracknum, original, current) =>
			{
				DeltaSerializer.ApplyDelta<int>(original, current, _diskDeltas[_getCurrentDiskNumber(), tracknum]);
			});
		}

		private void ResetDeltas()
		{
			_disk?.DeltaUpdate(static (_, original, current) =>
			{
				original.AsSpan().CopyTo(current);
			});
		}
	}
}
