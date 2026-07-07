using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// NEC uPD765 floppy disk controller operating on the flux disk model. Implements the
	/// command/execution/result phase machine and the main-status-register handshake, reading sectors off
	/// the drive's MFM flux and computing ST0-ST3 (CRC errors, deleted marks, wrong cylinder, no data) as
	/// the real device does, rather than replaying image status bytes.
	/// This increment adds real timing: the host advances the controller with Clock, seeks step at the
	/// programmed step rate, execution-phase bytes appear at the data-rate cadence (32 us per DD byte) with
	/// overrun detection, and the interrupt line is driven through IFdcHost. Implemented commands: Specify,
	/// Recalibrate, Seek, Sense Interrupt Status, Sense Drive Status, Read ID and Read Data. Write/Format/
	/// Scan are later increments. The command opcode is masked with 0x1F (five bits) so Scan/Version decode
	/// correctly.
	/// </summary>
	public sealed class Upd765Fdc
	{
		// Main status register bits
		private const byte MSR_D0B = 0x01, MSR_CB = 0x10, MSR_EXM = 0x20, MSR_DIO = 0x40, MSR_RQM = 0x80;
		// ST0
		private const byte ST0_HD = 0x04, ST0_NR = 0x08, ST0_EC = 0x10, ST0_SE = 0x20, ST0_IC_ABTERM = 0x40, ST0_IC_INVALID = 0x80;
		// ST1
		private const byte ST1_MA = 0x01, ST1_NW = 0x02, ST1_ND = 0x04, ST1_OR = 0x10, ST1_DE = 0x20, ST1_EN = 0x80;
		// ST2
		private const byte ST2_MD = 0x01, ST2_BC = 0x02, ST2_WC = 0x10, ST2_DD = 0x20, ST2_CM = 0x40;
		// ST3
		private const byte ST3_TS = 0x08, ST3_T0 = 0x10, ST3_RY = 0x20, ST3_WP = 0x40, ST3_FT = 0x80;

		private enum Phase { Idle, Command, Execution, Result }

		// opcodes (low 5 bits of the command byte)
		private const int CmdReadData = 0x06, CmdReadId = 0x0A, CmdSpecify = 0x03, CmdSeek = 0x0F,
			CmdRecalibrate = 0x07, CmdSenseInt = 0x08, CmdSenseDrive = 0x04,
			CmdWriteData = 0x05, CmdWriteDeleted = 0x09, CmdFormat = 0x0D,
			CmdReadDeleted = 0x0C, CmdVersion = 0x10, CmdReadDiagnostic = 0x02,
			CmdScanEqual = 0x11, CmdScanLow = 0x19, CmdScanHigh = 0x1D;

		/// <summary>Up to four drives; the +3/CPC populate index 0. Host sets these.</summary>
		public IFloppyDrive[] Drives { get; } = new IFloppyDrive[4];

		/// <summary>Optional host callback for the INT/DRQ lines. Null is fine (a polling host ignores them).</summary>
		public IFdcHost Host { get; set; }

		/// <summary>RNG for weak/fuzzy sectors, shared so repeated reads vary; seedable for determinism.</summary>
		public WeakBitRng WeakRng { get; set; } = new WeakBitRng(0);

		/// <summary>Host CPU clock the timing is expressed against; defaults to the +3 Z80 clock.</summary>
		public long CpuClockHz { get; private set; } = 3_546_900;

		/// <summary>Current state of the FDC interrupt request line.</summary>
		public bool IntPending => _intActive;

		/// <summary>True while a command is in progress (not idle) - drives the disk activity light.</summary>
		public bool Active => _phase != Phase.Idle;

		private Phase _phase = Phase.Idle;

		private int _opcode;
		private bool _mt, _mf, _sk;
		private byte[] _params = new byte[9];
		private int _paramCount, _paramNeeded;

		private byte[] _result = new byte[7];
		private int _resPtr, _resLen;

		private byte[] _exec = System.Array.Empty<byte>();
		private int _execPtr;
		private bool _execByteReady;   // a transfer byte is available (delivered/accepted on demand)
		private bool _execWrite;       // execution direction: true = host writes into the FDC (Write/Format)
		private bool _formatMode;      // the current write execution is a Format Track (ID bytes, not data)

		// write / format bookkeeping (target sectors decoded from the current track, modified then rebuilt)
		private List<TrackSector> _writeList;
		private readonly List<int> _writeTargets = new List<int>();
		private bool _writeDeleted;
		private byte _formatFiller;

		// Specify parameters (millisecond scale); only the step rate feeds a derived threshold now
		private int _srtMs = 6, _hltMs = 2;
		private bool _nonDma;
		private int _cyclesPerStep;

		// per-drive overlapped-seek bookkeeping
		private readonly bool[] _seekActive = new bool[4];
		private readonly bool[] _seekSettling = new bool[4];
		private readonly int[] _seekTarget = new int[4];
		private readonly int[] _seekStepTimer = new int[4];
		private readonly int[] _seekSettleTimer = new int[4];
		private readonly bool[] _seekIntPending = new bool[4];
		private readonly byte[] _pcn = new byte[4];

		// per-drive rotating Read ID position
		private readonly int[] _readIdIndex = new int[4];

		private int _unit;
		private int _side;
		private bool _intActive;

		private IFloppyDrive ActiveDrive => Drives[_unit & 3];

		public Upd765Fdc() => RecomputeTiming();

		/// <summary>Point the controller (and its drives) at the host CPU clock so timing lands correctly.</summary>
		public void ConfigureTiming(long cpuClockHz)
		{
			CpuClockHz = cpuClockHz > 0 ? cpuClockHz : 3_546_900;
			RecomputeTiming();
			for (int i = 0; i < 4; i++) Drives[i]?.ConfigureTiming(CpuClockHz);
		}

		/// <summary>Serialize the controller's operational state (the loaded disk is restored separately).</summary>
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Upd765Fdc");
			ser.SyncEnum(nameof(_phase), ref _phase);
			ser.Sync(nameof(_opcode), ref _opcode);
			ser.Sync(nameof(_mt), ref _mt); ser.Sync(nameof(_mf), ref _mf); ser.Sync(nameof(_sk), ref _sk);
			ser.Sync(nameof(_params), ref _params, useNull: false);
			ser.Sync(nameof(_paramCount), ref _paramCount);
			ser.Sync(nameof(_paramNeeded), ref _paramNeeded);
			ser.Sync(nameof(_result), ref _result, useNull: false);
			ser.Sync(nameof(_resPtr), ref _resPtr);
			ser.Sync(nameof(_resLen), ref _resLen);
			ser.Sync(nameof(_exec), ref _exec, useNull: false);
			ser.Sync(nameof(_execPtr), ref _execPtr);
			ser.Sync(nameof(_execByteReady), ref _execByteReady);
			ser.Sync(nameof(_execWrite), ref _execWrite);
			ser.Sync(nameof(_formatMode), ref _formatMode);
			ser.Sync(nameof(_srtMs), ref _srtMs);
			ser.Sync(nameof(_hltMs), ref _hltMs);
			ser.Sync(nameof(_nonDma), ref _nonDma);
			for (int i = 0; i < 4; i++)
			{
				ser.Sync("_seekActive" + i, ref _seekActive[i]);
				ser.Sync("_seekSettling" + i, ref _seekSettling[i]);
				ser.Sync("_seekTarget" + i, ref _seekTarget[i]);
				ser.Sync("_seekStepTimer" + i, ref _seekStepTimer[i]);
				ser.Sync("_seekSettleTimer" + i, ref _seekSettleTimer[i]);
				ser.Sync("_seekIntPending" + i, ref _seekIntPending[i]);
				ser.Sync("_pcn" + i, ref _pcn[i]);
				ser.Sync("_readIdIndex" + i, ref _readIdIndex[i]);
			}
			ser.Sync(nameof(_unit), ref _unit);
			ser.Sync(nameof(_side), ref _side);
			ser.Sync(nameof(_intActive), ref _intActive);
			// weak-cell RNG state, so weak-sector reads replay identically across savestate/TAS load
			ulong weakState = WeakRng.State;
			ser.Sync("_weakRngState", ref weakState);
			WeakRng.State = weakState;
			ser.EndSection();
			RecomputeTiming();
		}

		private void RecomputeTiming()
		{
			_cyclesPerStep = (int)(CpuClockHz * _srtMs / 1000); // seek step-rate time (Specify SRT)
			if (_cyclesPerStep < 1) _cyclesPerStep = 1;
		}

		public void Reset()
		{
			_phase = Phase.Idle;
			_paramCount = _resPtr = _resLen = _execPtr = 0;
			_execByteReady = _execWrite = _formatMode = false;
			_writeTargets.Clear();
			_writeList = null;
			for (int i = 0; i < 4; i++)
			{
				_seekActive[i] = _seekSettling[i] = _seekIntPending[i] = false;
				_seekTarget[i] = _seekStepTimer[i] = _seekSettleTimer[i] = _readIdIndex[i] = 0;
				_pcn[i] = 0;
			}
			LowerInt();
		}

		// ---- clock tick: advances drives and overlapped seeks ----
		//
		// Note: execution-phase data transfer is NOT clocked to a byte cadence. The +3 has no DMA and no
		// wired FDC interrupt - it transfers each byte by polling RQM in a tight loop - so the host itself
		// paces the transfer. Emulating a fixed byte cadence on top of lazy clock-on-access races against
		// that poll loop (a byte can appear "overrun" between the status poll and the data read), which
		// breaks real loaders. Bytes are therefore delivered/accepted on demand, with no overrun. The
		// data-based nature of copy protection (weak bits, geometry) is preserved by the flux model itself;
		// only the transfer-rate timing is dropped, exactly as the original hardware-agnostic core did.

		public void Clock(int cpuCycles)
		{
			for (int i = 0; i < 4; i++) Drives[i]?.Clock(cpuCycles);
			ClockSeeks(cpuCycles);
		}

		private void ClockSeeks(int cpuCycles)
		{
			for (int u = 0; u < 4; u++)
			{
				if (!_seekActive[u]) continue;

				if (_seekSettling[u])
				{
					// head is settling after the final step; complete once the settle time elapses
					_seekSettleTimer[u] += cpuCycles;
					if (_seekSettleTimer[u] >= SettleCycles(u)) CompleteSeek(u);
					continue;
				}

				_seekStepTimer[u] += cpuCycles;
				int stepCycles = EffectiveStepCycles(u);
				while (_seekActive[u] && !_seekSettling[u] && _seekStepTimer[u] >= stepCycles)
				{
					_seekStepTimer[u] -= stepCycles;
					var drive = Drives[u];
					int cur = drive?.CurrentCylinder ?? _seekTarget[u];
					if (cur != _seekTarget[u])
					{
						drive?.Step(cur < _seekTarget[u]);
						_pcn[u] = (byte)(drive?.CurrentCylinder ?? _seekTarget[u]);
					}
					if ((drive?.CurrentCylinder ?? _seekTarget[u]) == _seekTarget[u])
					{
						if (SettleCycles(u) <= 0)
						{
							CompleteSeek(u);
						}
						else
						{
							// begin settling, carrying any cycles left over past the final step
							_seekSettling[u] = true;
							_seekSettleTimer[u] = _seekStepTimer[u];
							_seekStepTimer[u] = 0;
							if (_seekSettleTimer[u] >= SettleCycles(u)) CompleteSeek(u);
						}
					}
				}
			}
		}

		private void CompleteSeek(int u)
		{
			_seekActive[u] = false;
			_seekSettling[u] = false;
			_seekIntPending[u] = true;
			RaiseInt();
		}

		// Step interval bounded below by the drive's mechanical track-to-track time (the programmed step
		// rate cannot make the head move faster than the mechanism).
		private int EffectiveStepCycles(int u)
		{
			int cycles = _cyclesPerStep;
			int t2t = Drives[u]?.TrackToTrackMs ?? 0;
			if (t2t > 0)
			{
				int floor = (int)(CpuClockHz * t2t / 1000);
				if (floor > cycles) cycles = floor;
			}
			return cycles < 1 ? 1 : cycles;
		}

		private int SettleCycles(int u)
		{
			int settleMs = Drives[u]?.SettleMs ?? 0;
			return settleMs <= 0 ? 0 : (int)(CpuClockHz * settleMs / 1000);
		}

		// ---- host-facing register interface (the host maps I/O ports to these) ----

		public byte ReadStatus()
		{
			byte s = 0;
			switch (_phase)
			{
				case Phase.Idle: s = MSR_RQM; break;
				case Phase.Command: s = MSR_RQM | MSR_CB; break;
				case Phase.Execution:
					s = MSR_CB | MSR_EXM;
					if (!_execWrite) s |= MSR_DIO; // DIO=1 read (FDC->CPU), 0 write (CPU->FDC)
					if (_execByteReady) s |= MSR_RQM;
					break;
				case Phase.Result: s = MSR_RQM | MSR_CB | MSR_DIO; break;
			}
			for (int i = 0; i < 4; i++) if (_seekActive[i]) s |= (byte)(MSR_D0B << i);
			return s;
		}

		public byte ReadData()
		{
			if (_phase == Phase.Execution)
			{
				if (_execWrite || !_execByteReady) return 0xFF; // wrong direction
				byte b = _exec[_execPtr++];
				if (_execPtr >= _exec.Length) EnterResult(); // last byte consumed (clears the ready flag)
				return b; // on demand: the next byte is immediately available while bytes remain
			}
			if (_phase == Phase.Result)
			{
				if (_resPtr == 0) LowerInt(); // reading the result acknowledges the interrupt
				byte b = _result[_resPtr++];
				if (_resPtr >= _resLen) _phase = Phase.Idle;
				return b;
			}
			return 0xFF;
		}

		public void WriteData(byte data)
		{
			switch (_phase)
			{
				case Phase.Idle:
					BeginCommand(data);
					break;
				case Phase.Command:
					_params[_paramCount++] = data;
					if (_paramCount >= _paramNeeded) Execute();
					break;
				case Phase.Execution:
					if (!_execWrite || !_execByteReady) return; // wrong direction
					_exec[_execPtr++] = data;
					if (_execPtr >= _exec.Length) FinalizeWriteExec(); // last byte accepted (clears the ready flag)
					break;
			}
		}

		// ---- command handling ----

		private void BeginCommand(byte cmd)
		{
			_mt = (cmd & 0x80) != 0;
			_mf = (cmd & 0x40) != 0;
			_sk = (cmd & 0x20) != 0;
			_opcode = cmd & 0x1F; // five-bit opcode (fixes Scan/Version vs the old 0x0F mask)
			_paramCount = 0;
			_resPtr = 0;

			_paramNeeded = _opcode switch
			{
				CmdReadData => 8,
				CmdReadDeleted => 8,
				CmdReadDiagnostic => 8,
				CmdScanEqual => 8,
				CmdScanLow => 8,
				CmdScanHigh => 8,
				CmdWriteData => 8,
				CmdWriteDeleted => 8,
				CmdFormat => 5,
				CmdReadId => 1,
				CmdSpecify => 2,
				CmdSeek => 2,
				CmdRecalibrate => 1,
				CmdSenseInt => 0,
				CmdSenseDrive => 1,
				CmdVersion => 0,
				_ => 0,
			};

			_phase = Phase.Command;
			if (_paramNeeded == 0) Execute();
		}

		private void Execute()
		{
			switch (_opcode)
			{
				case CmdSpecify: DoSpecify(); break;
				case CmdRecalibrate: DoSeek(recalibrate: true); break;
				case CmdSeek: DoSeek(recalibrate: false); break;
				case CmdSenseInt: DoSenseInterrupt(); break;
				case CmdSenseDrive: DoSenseDrive(); break;
				case CmdReadId: DoReadId(); break;
				case CmdReadData: DoReadData(readDeleted: false); break;
				case CmdReadDeleted: DoReadData(readDeleted: true); break;
				case CmdWriteData: DoWriteData(deleted: false); break;
				case CmdWriteDeleted: DoWriteData(deleted: true); break;
				case CmdFormat: DoFormat(); break;
				case CmdVersion: DoVersion(); break;
				default: DoInvalid(); break;
			}
		}

		private void DoSpecify()
		{
			_srtMs = 16 - (_params[0] >> 4);
			_hltMs = _params[1] & 0xFE;
			_nonDma = (_params[1] & 0x01) != 0;
			if (_srtMs < 1) _srtMs = 1;
			RecomputeTiming();
			_phase = Phase.Idle; // no result
		}

		private void DoSeek(bool recalibrate)
		{
			_unit = _params[0] & 3;
			int target = recalibrate ? 0 : _params[1];
			_seekTarget[_unit] = target;
			_seekStepTimer[_unit] = 0;
			_seekSettleTimer[_unit] = 0;
			_seekActive[_unit] = true;
			_seekSettling[_unit] = false;
			_seekIntPending[_unit] = false;
			_phase = Phase.Idle; // command accepted; completion is timed and reported via Sense Interrupt
		}

		private void DoSenseInterrupt()
		{
			int unit = -1;
			for (int i = 0; i < 4; i++) if (_seekIntPending[i]) { unit = i; break; }

			if (unit < 0)
			{
				// no interrupt pending
				_result[0] = ST0_IC_INVALID;
				_resLen = 1;
			}
			else
			{
				_seekIntPending[unit] = false;
				byte st0 = (byte)(ST0_SE | (unit & 3));
				var drive = Drives[unit];
				if (drive == null || !drive.Ready) st0 |= ST0_NR;
				_result[0] = st0;
				_result[1] = _pcn[unit];
				_resLen = 2;
			}
			EnterResultPrepared();
		}

		private void DoSenseDrive()
		{
			_unit = _params[0] & 3;
			_side = (_params[0] >> 2) & 1;
			var drive = ActiveDrive;

			byte st3 = (byte)(_unit & 3);
			if (_side != 0) st3 |= ST0_HD;
			if (drive == null)
			{
				st3 |= ST3_FT; // no drive present
			}
			else
			{
				if (drive.WriteProtected) st3 |= ST3_WP;
				if (drive.Track0) st3 |= ST3_T0;
				if (drive.Ready) st3 |= ST3_RY;
				if (drive.SideCount > 1) st3 |= ST3_TS;
			}
			_result[0] = st3;
			_resLen = 1;
			EnterResultPrepared();
		}

		private void DoReadId()
		{
			_unit = _params[0] & 3;
			_side = (_params[0] >> 2) & 1;
			var drive = ActiveDrive;
			byte st0 = (byte)((_side << 2) | (_unit & 3));

			if (drive == null || !drive.Ready)
			{
				// genuinely not ready (no disk / motor off) -> ST0 Not Ready
				st0 |= ST0_IC_ABTERM | ST0_NR;
				PrepareResultChrn(st0, 0, 0, 0, 0, 0, 0);
				EnterResultPrepared();
				return;
			}
			var track = drive.CurrentTrack(_side);
			var sectors = track == null ? null : StandardMfmFormat.DecodeSectors(track, WeakRng);
			if (sectors == null || sectors.Count == 0)
			{
				// drive IS ready but the track has no readable address mark -> Missing Address Mark, NOT Not Ready
				// (a real uPD765 only sets NR when the drive itself is not ready; the frontend maps NR to
				// "disk not ready", so a track with damaged/absent marks must report MA, not NR).
				st0 |= ST0_IC_ABTERM;
				PrepareResultChrn(st0, ST1_MA, 0, 0, 0, 0, 0);
				EnterResultPrepared();
				return;
			}

			int idx = _readIdIndex[_unit] % sectors.Count;
			_readIdIndex[_unit] = (idx + 1) % sectors.Count;
			var s = sectors[idx];
			byte st1 = s.IdCrcOk ? (byte)0 : ST1_DE;
			PrepareResultChrn(st0, st1, 0, s.C, s.H, s.R, s.N);
			EnterResultPrepared();
		}

		// Read Data (readDeleted=false) and Read Deleted Data (readDeleted=true). They differ only in which
		// data-address-mark type is "expected": a mismatch (a deleted sector under Read Data, or a normal
		// sector under Read Deleted Data) sets the Control Mark (ST2 CM) bit - and is skipped when SK is set,
		// otherwise it is transferred and terminates the command. RoboCop and similar titles store protected
		// data under deleted address marks and read it with Read Deleted Data (0x0C).
		private void DoReadData(bool readDeleted)
		{
			_unit = _params[0] & 3;
			_side = (_params[0] >> 2) & 1;
			byte cyl = _params[1], head = _params[2], sector = _params[3], n = _params[4], eot = _params[5];
			var drive = ActiveDrive;
			byte st0 = (byte)((_side << 2) | (_unit & 3));

			if (drive == null || !drive.Ready)
			{
				st0 |= ST0_IC_ABTERM | ST0_NR;
				PrepareResultChrn(st0, 0, 0, cyl, head, sector, n);
				EnterResultPrepared();
				return;
			}
			var track = drive.CurrentTrack(_side);
			if (track == null)
			{
				// drive ready but the current cylinder has no track (unformatted) -> Missing Address Mark, not NR
				st0 |= ST0_IC_ABTERM;
				PrepareResultChrn(st0, ST1_MA, 0, cyl, head, sector, n);
				EnterResultPrepared();
				return;
			}

			var sectors = StandardMfmFormat.DecodeSectors(track, WeakRng);
			var data = new List<byte>();
			byte st1 = 0, st2 = 0;
			byte curR = sector;

			for (; ; )
			{
				var s = FindById(sectors, cyl, head, curR, n);
				if (s == null)
				{
					// requested sector not found on the track
					st1 |= ST1_ND;
					st0 |= ST0_IC_ABTERM;
					break;
				}

				bool markMismatch = s.Deleted != readDeleted; // wrong address-mark type for this command
				if (markMismatch && _sk)
				{
					// SK set: skip a wrong-mark sector without transferring it
					if (curR == eot) { st1 |= ST1_EN; break; }
					curR++;
					continue;
				}

				if (!s.IdCrcOk) st1 |= ST1_DE;
				if (!s.DataCrcOk) { st1 |= ST1_DE; st2 |= ST2_DD; }
				if (markMismatch) st2 |= ST2_CM;

				data.AddRange(s.Data);

				bool crcError = (st1 & ST1_DE) != 0; // DE is set for both ID- and data-field CRC failures
				if (curR == eot || crcError || markMismatch)
				{
					st1 |= ST1_EN;
					break;
				}
				curR++;
			}

			PrepareResultChrn(st0, st1, st2, cyl, head, curR, n);
			if (data.Count > 0)
			{
				_exec = data.ToArray();
				_execPtr = 0;
				_execByteReady = true; // first byte available on demand
				_phase = Phase.Execution;
			}
			else
			{
				EnterResultPrepared();
			}
		}

		// Version: the +3 uses a uPD765A, which reports 0x80.
		private void DoVersion()
		{
			_result[0] = 0x80;
			_resLen = 1;
			EnterResultPrepared();
		}

		private void DoWriteData(bool deleted)
		{
			_unit = _params[0] & 3;
			_side = (_params[0] >> 2) & 1;
			byte cyl = _params[1], head = _params[2], sector = _params[3], n = _params[4], eot = _params[5];
			var drive = ActiveDrive;
			byte st0 = (byte)((_side << 2) | (_unit & 3));

			if (drive == null || !drive.Ready)
			{
				st0 |= ST0_IC_ABTERM | ST0_NR;
				PrepareResultChrn(st0, 0, 0, cyl, head, sector, n);
				EnterResultPrepared();
				return;
			}
			var track = drive.CurrentTrack(_side);
			if (track == null)
			{
				// drive ready but no track at this cylinder -> no address mark to write into, not NR
				st0 |= ST0_IC_ABTERM;
				PrepareResultChrn(st0, ST1_MA, 0, cyl, head, sector, n);
				EnterResultPrepared();
				return;
			}
			if (drive.WriteProtected)
			{
				st0 |= ST0_IC_ABTERM;
				PrepareResultChrn(st0, ST1_NW, 0, cyl, head, sector, n);
				EnterResultPrepared();
				return;
			}

			// decode the current track, then locate the ID of each sector to be written (R..EOT)
			_writeList = StandardMfmFormat.ToTrackSectors(track, WeakRng);
			_writeTargets.Clear();
			int expected = 0;
			byte curR = sector;
			for (; ; )
			{
				int idx = FindSectorIndex(_writeList, cyl, head, curR, n);
				if (idx < 0)
				{
					st0 |= ST0_IC_ABTERM;
					PrepareResultChrn(st0, ST1_ND, 0, cyl, head, curR, n);
					EnterResultPrepared();
					return;
				}
				_writeTargets.Add(idx);
				expected += _writeList[idx].SizeBytes;
				if (curR == eot) break;
				curR++;
			}

			_writeDeleted = deleted;
			BeginWriteExecution(expected, format: false);
			PrepareResultChrn(st0, ST1_EN, deleted ? ST2_CM : (byte)0, cyl, head, curR, n);
		}

		private void DoFormat()
		{
			_unit = _params[0] & 3;
			_side = (_params[0] >> 2) & 1;
			byte n = _params[1], sc = _params[2];
			_formatFiller = _params[4];
			var drive = ActiveDrive;
			byte st0 = (byte)((_side << 2) | (_unit & 3));

			if (drive == null || !drive.Ready)
			{
				st0 |= ST0_IC_ABTERM | ST0_NR;
				PrepareResultChrn(st0, 0, 0, 0, 0, 0, n);
				EnterResultPrepared();
				return;
			}
			if (drive.WriteProtected)
			{
				st0 |= ST0_IC_ABTERM;
				PrepareResultChrn(st0, ST1_NW, 0, 0, 0, 0, n);
				EnterResultPrepared();
				return;
			}
			if (sc == 0)
			{
				PrepareResultChrn(st0, 0, 0, 0, 0, 0, n);
				EnterResultPrepared();
				return;
			}

			// the host supplies four ID bytes (C H R N) per sector during execution
			BeginWriteExecution(sc * 4, format: true);
			PrepareResultChrn(st0, 0, 0, 0, 0, 0, n);
		}

		private void BeginWriteExecution(int expectedBytes, bool format)
		{
			_exec = new byte[expectedBytes];
			_execPtr = 0;
			_execByteReady = true; // ready to accept the first byte on demand
			_execWrite = true;
			_formatMode = format;
			_phase = Phase.Execution;
		}

		private void FinalizeWriteExec()
		{
			if (_formatMode) FinalizeFormat(); else FinalizeWrite();
		}

		private void FinalizeWrite()
		{
			int offset = 0;
			foreach (int idx in _writeTargets)
			{
				var ts = _writeList[idx];
				int size = ts.SizeBytes;
				var buf = new byte[size];
				System.Array.Copy(_exec, offset, buf, 0, System.Math.Min(size, _exec.Length - offset));
				ts.Data = buf;
				ts.Deleted = _writeDeleted;
				ts.DataCrcError = false; // a fresh write lays down a correct CRC
				ts.WeakCopies = null;    // writing over a weak sector makes it deterministic
				offset += size;
			}
			ActiveDrive?.WriteTrack(_side, StandardMfmFormat.BuildStandardTrack(_writeList));
			EnterResult(); // result prepared with EN at command start
		}

		private void FinalizeFormat()
		{
			int sc = _exec.Length / 4;
			var list = new List<TrackSector>(sc);
			byte lastC = 0, lastH = 0, lastR = 0, lastN = 0;
			for (int i = 0; i < sc; i++)
			{
				byte c = _exec[i * 4], h = _exec[i * 4 + 1], r = _exec[i * 4 + 2], n = _exec[i * 4 + 3];
				int size = 128 << (n & 7);
				var data = new byte[size];
				for (int b = 0; b < size; b++) data[b] = _formatFiller;
				list.Add(new TrackSector { C = c, H = h, R = r, N = n, Data = data });
				lastC = c; lastH = h; lastR = r; lastN = n;
			}
			ActiveDrive?.WriteTrack(_side, StandardMfmFormat.BuildStandardTrack(list));
			_result[3] = lastC; _result[4] = lastH; _result[5] = lastR; _result[6] = lastN;
			EnterResult();
		}

		private void DoInvalid()
		{
			_result[0] = ST0_IC_INVALID;
			_resLen = 1;
			EnterResultPrepared();
		}

		// ---- helpers ----

		private static DecodedSector FindById(List<DecodedSector> sectors, byte c, byte h, byte r, byte n)
		{
			foreach (var s in sectors)
				if (s.R == r && s.C == c && s.H == h && s.N == n)
					return s;
			return null;
		}

		private static int FindSectorIndex(List<TrackSector> sectors, byte c, byte h, byte r, byte n)
		{
			for (int i = 0; i < sectors.Count; i++)
			{
				var s = sectors[i];
				if (s.R == r && s.C == c && s.H == h && s.N == n) return i;
			}
			return -1;
		}

		// Fill the standard 7-byte result (ST0 ST1 ST2 C H R N) without changing phase yet.
		private void PrepareResultChrn(byte st0, byte st1, byte st2, byte c, byte h, byte r, byte n)
		{
			_result[0] = st0; _result[1] = st1; _result[2] = st2;
			_result[3] = c; _result[4] = h; _result[5] = r; _result[6] = n;
			_resLen = 7;
		}

		// Enter result phase after the execution buffer is exhausted (result already prepared).
		private void EnterResult()
		{
			_execByteReady = _execWrite = _formatMode = false;
			_resPtr = 0;
			_phase = Phase.Result;
			RaiseInt();
		}

		// Enter result phase directly for commands with no execution streaming.
		private void EnterResultPrepared()
		{
			_resPtr = 0;
			if (_resLen > 0)
			{
				_phase = Phase.Result;
				RaiseInt();
			}
			else
			{
				_phase = Phase.Idle;
			}
		}

		private void RaiseInt()
		{
			if (_intActive) return;
			_intActive = true;
			Host?.OnFdcInterrupt(true);
		}

		private void LowerInt()
		{
			if (!_intActive) return;
			_intActive = false;
			Host?.OnFdcInterrupt(false);
		}
	}
}
