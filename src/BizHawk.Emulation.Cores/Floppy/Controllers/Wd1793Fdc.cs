using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Western Digital WD1793 (and its Soviet KR1818VG93 clone) floppy disk controller, operating on the
	/// shared flux disk model. This is the FDC at the heart of the Beta 128 disk interface built into the
	/// Pentagon and Scorpion ZX Spectrum clones, running TR-DOS. It mirrors the uPD765 core in this folder:
	/// it drives the same IFloppyDrive array, reads sectors off the drive's MFM flux with
	/// StandardMfmFormat.DecodeSectors (computing CRC/record-not-found as the real device does rather than
	/// replaying image status), advances on a Clock tick against the host CPU clock, and drives its
	/// interrupt (INTRQ) and data-request (DRQ) lines through IFdcHost.
	/// The WD1793 is register-based rather than the uPD765's command/parameter/result phases. Five registers
	/// are exposed (Status/Command share an address, then Track, Sector, Data); the Beta interface maps them
	/// to Z80 ports 0x1F/0x3F/0x5F/0x7F and adds an external system latch (drive select, side, density,
	/// reset) plus a status read of INTRQ/DRQ. Those live on the Beta device wrapper, not here.
	/// Commands are grouped into four types with distinct status-register meanings:
	/// Type I - Restore, Seek, Step, Step-In, Step-Out (head positioning).
	/// Type II - Read Sector, Write Sector.
	/// Type III - Read Address, Read Track, Write Track.
	/// Type IV - Force Interrupt.
	/// </summary>
	public sealed class Wd1793Fdc
	{
		// Status register - Type I (positioning) commands
		private const byte ST_BUSY = 0x01;      // shared bit 0: command in progress
		private const byte ST1_INDEX = 0x02;    // index pulse under the head
		private const byte ST1_TRACK0 = 0x04;   // head positioned at track 0
		private const byte ST1_CRC = 0x08;      // CRC error (in an ID field during verify)
		private const byte ST1_SEEKERR = 0x10;  // target track not verified
		private const byte ST1_HEADLOADED = 0x20;
		private const byte ST_WRITEPROT = 0x40; // shared bit 6 (write protect)
		private const byte ST_NOTREADY = 0x80;  // shared bit 7 (drive not ready)

		// Status register - Type II/III (data-transfer) commands
		private const byte ST2_DRQ = 0x02;      // data register needs service
		private const byte ST2_LOSTDATA = 0x04; // host missed a DRQ
		private const byte ST2_CRC = 0x08;      // CRC error in ID or data field
		private const byte ST2_RNF = 0x10;      // record not found
		private const byte ST2_RECTYPE = 0x20;  // (read) deleted DAM / (write) write fault
		// bit 6 write protect, bit 7 not ready shared with above

		/// <summary>
		/// Internal execution state of the current command.
		/// </summary>
		private enum State
		{
			Idle,
			TypeISeek,      // issuing step pulses toward the target cylinder
			TypeISettle,    // head settling delay after the final step
			TypeIVerify,    // verifying the ID field track matches the track register
			ReadSearch,     // locating the requested sector on the current track
			ReadTransfer,   // streaming the found sector's bytes to the data register
			WriteWait,      // waiting for the host to supply the first data byte
			WriteTransfer,  // accepting sector bytes from the host
			ReadAddress,    // streaming the 6-byte ID field
			RnfWait,        // sector/ID not found - spinning out the search before flagging Record Not Found
		}

		/// <summary>
		/// Up to four drives; the Beta interface can address A-D. Host populates these.
		/// </summary>
		public IFloppyDrive[] Drives { get; } = new IFloppyDrive[4];

		/// <summary>
		/// Optional host callback for the INTRQ/DRQ lines. The Beta reads them from its system port, so a
		/// polling host can leave this null and read IntRequest / DataRequest directly instead.
		/// </summary>
		public IFdcHost Host { get; set; }

		/// <summary>
		/// RNG for weak/fuzzy sectors, shared so repeated reads vary; seedable for determinism.
		/// </summary>
		public WeakBitRng WeakRng { get; set; } = new WeakBitRng(0);

		/// <summary>
		/// Host CPU clock the timing is expressed against; defaults to the Pentagon Z80 clock.
		/// </summary>
		public long CpuClockHz { get; private set; } = 3_546_900;

		/// <summary>
		/// The INTRQ line: set when a command completes, cleared when the status register is read or a new
		/// command is written. The Beta surfaces this on system-port read bit 7.
		/// </summary>
		public bool IntRequest => _intrq;

		/// <summary>
		/// The DRQ line: set when the data register needs servicing during a Type II/III transfer. The Beta
		/// surfaces this on system-port read bit 6.
		/// </summary>
		public bool DataRequest => _drq;

		/// <summary>
		/// True while a command is in progress - drives the disk activity light.
		/// </summary>
		public bool Active => _state != State.Idle;

		// registers
		private byte _status;
		private byte _command;
		private byte _trackReg;
		private byte _sectorReg;
		private byte _dataReg;

		// external latch state, set by the Beta system port
		private int _unit;      // selected drive 0-3
		private int _side;      // selected physical side 0/1
		private bool _dden = true; // double density (MFM); FM is unused by TR-DOS

		private State _state = State.Idle;
		private bool _intrq;
		private bool _drq;
		private bool _typeIStatus = true; // whether _status is composed as Type I (positioning) or Type II/III

		// positioning bookkeeping
		private int _seekTargetCyl;
		private int _stepDir;           // +1 toward higher cylinders, -1 toward track 0
		private bool _verifyAfterSeek;
		private int _cyclesPerStep;     // from the command's r1r0 step-rate field
		private int _settleCycles;      // Type II/III E-flag head settle (15 ms)
		private int _rnfTimeout;        // how long a data-command searches for its ID before Record Not Found
		// note: no per-byte transfer cadence field - reads and writes are handed over on demand (see ReadData)
		private int _verifySettleCycles; // Type I verify head settle (30 ms at the Beta's 1 MHz FDC clock)
		private int _timer;             // counts down the current timed sub-step

		// data-transfer bookkeeping
		private byte[] _xfer = System.Array.Empty<byte>();
		private int _xferPtr;
		private bool _xferCrcError;     // the located sector failed its data CRC
		private bool _multi;            // Type II multiple-record flag
		private bool _deletedData;      // read: located a deleted DAM / write: lay a deleted DAM

		// write / format bookkeeping (the current track decoded to an editable form, modified then rebuilt)
		private bool _formatMode;              // the active write transfer is a Write Track (format), not a sector
		private List<TrackSector> _writeSectors;
		private int _writeIndex;               // index into _writeSectors of the Write Sector target
		private readonly List<byte> _formatBuf = new List<byte>(8192);
		private int _readAddrIndex;            // rotating index so successive Read Address commands step sectors

		private IFloppyDrive ActiveDrive => Drives[_unit & 3];

		private static readonly int[] StepRatesMs = { 6, 12, 20, 30 }; // r1r0 at the Beta's 1 MHz FDC clock

		public Wd1793Fdc() => RecomputeTiming();

		/// <summary>
		/// Point the controller (and its drives) at the host CPU clock so timing lands correctly.
		/// </summary>
		public void ConfigureTiming(long cpuClockHz)
		{
			CpuClockHz = cpuClockHz > 0 ? cpuClockHz : 3_546_900;
			RecomputeTiming();
			for (int i = 0; i < 4; i++) Drives[i]?.ConfigureTiming(CpuClockHz);
		}

		private void RecomputeTiming()
		{
			// Head settle. Per the FD179X datasheet the Type II/III E-flag delay is 15 ms; the Type I verify
			// settle is also 15 ms at a 2 MHz clock but DOUBLES to 30 ms at a 1 MHz clock - and the Beta clocks
			// the WD1793 at 1 MHz for double-density 5.25"/3.5" drives, so the verify settle is 30 ms here.
			_settleCycles = (int)(CpuClockHz * 15 / 1000);
			_verifySettleCycles = (int)(CpuClockHz * 30 / 1000);
			// the datasheet gives Record Not Found after ~5 unsuccessful revolutions; at 300 RPM that is
			// ~1 s (5 x 200 ms) = CpuClockHz cycles. Present sectors are found immediately (we decode the whole
			// track), so this only delays the RNF report for a genuinely absent ID - the "disk error" pause.
			_rnfTimeout = (int)CpuClockHz;
			if (_rnfTimeout < 1) _rnfTimeout = 1;
		}

		/// <summary>
		/// Serialize the controller's operational state (the loaded disk is restored separately).
		/// </summary>
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Wd1793Fdc");
			ser.Sync(nameof(_status), ref _status);
			ser.Sync(nameof(_command), ref _command);
			ser.Sync(nameof(_trackReg), ref _trackReg);
			ser.Sync(nameof(_sectorReg), ref _sectorReg);
			ser.Sync(nameof(_dataReg), ref _dataReg);
			ser.Sync(nameof(_unit), ref _unit);
			ser.Sync(nameof(_side), ref _side);
			ser.Sync(nameof(_dden), ref _dden);
			ser.SyncEnum(nameof(_state), ref _state);
			ser.Sync(nameof(_intrq), ref _intrq);
			ser.Sync(nameof(_drq), ref _drq);
			ser.Sync(nameof(_typeIStatus), ref _typeIStatus);
			ser.Sync(nameof(_seekTargetCyl), ref _seekTargetCyl);
			ser.Sync(nameof(_stepDir), ref _stepDir);
			ser.Sync(nameof(_verifyAfterSeek), ref _verifyAfterSeek);
			ser.Sync(nameof(_cyclesPerStep), ref _cyclesPerStep);
			ser.Sync(nameof(_timer), ref _timer);
			ser.Sync(nameof(_xfer), ref _xfer, useNull: false);
			ser.Sync(nameof(_xferPtr), ref _xferPtr);
			ser.Sync(nameof(_xferCrcError), ref _xferCrcError);
			ser.Sync(nameof(_multi), ref _multi);
			ser.Sync(nameof(_deletedData), ref _deletedData);
			ser.Sync(nameof(_formatMode), ref _formatMode);
			ser.Sync(nameof(_writeIndex), ref _writeIndex);
			ser.Sync(nameof(_readAddrIndex), ref _readAddrIndex);
			ulong weakState = WeakRng.State;
			ser.Sync("_weakRngState", ref weakState);
			WeakRng.State = weakState;
			ser.EndSection();
			RecomputeTiming();
		}

		public void Reset()
		{
			_status = 0;
			_command = 0;
			_trackReg = 0;
			_sectorReg = 0;
			_dataReg = 0;
			_state = State.Idle;
			_drq = false;
			_typeIStatus = true;
			_xfer = System.Array.Empty<byte>();
			_xferPtr = 0;
			RaiseIntrq(false);
			// a hardware reset also issues a Restore (seek to track 0); model it lazily by parking heads
			for (int i = 0; i < 4; i++) Drives[i]?.SeekTo(0);
			_trackReg = 0;
		}

		// ---- Beta system-latch inputs (called by the Beta device on a #FF write) ----

		/// <summary>
		/// Select the active drive (0-3) and physical side (0/1), and the density (true = double / MFM).
		/// The Beta system latch drives these lines externally to the WD1793.
		/// </summary>
		public void SetSystem(int drive, int side, bool doubleDensity)
		{
			_unit = drive & 3;
			_side = side & 1;
			_dden = doubleDensity;
			if (ActiveDrive != null) ActiveDrive.MotorOn = true;
		}

		// ---- register access (mapped by the Beta to ports 0x1F/0x3F/0x5F/0x7F) ----

		/// <summary>
		/// Read the status register (Beta port 0x1F). Reading it clears the INTRQ line.
		/// </summary>
		public byte ReadStatus()
		{
			RaiseIntrq(false);
			return ComposeStatus();
		}

		/// <summary>
		/// Write the command register (Beta port 0x1F). Decodes the command type and starts execution.
		/// </summary>
		public void WriteCommand(byte value)
		{
			_command = value;
			// a Force Interrupt (Type IV) is accepted even while busy; every other command is ignored if busy
			int top = value >> 4;
			if (top == 0xD) { ForceInterrupt(value); return; }
			if ((_status & ST_BUSY) != 0) return;

			RaiseIntrq(false);
			if (value < 0x80) BeginTypeI(value);
			else if (value < 0xC0) BeginTypeII(value);
			else BeginTypeIII(value);
		}

		/// <summary>
		/// Read the track register (Beta port 0x3F).
		/// </summary>
		public byte ReadTrack() => _trackReg;

		/// <summary>
		/// Write the track register (Beta port 0x3F).
		/// </summary>
		public void WriteTrack(byte value) => _trackReg = value;

		/// <summary>
		/// Read the sector register (Beta port 0x5F).
		/// </summary>
		public byte ReadSector() => _sectorReg;

		/// <summary>
		/// Write the sector register (Beta port 0x5F).
		/// </summary>
		public void WriteSector(byte value) => _sectorReg = value;

		/// <summary>
		/// Read the data register (Beta port 0x7F). Bytes are handed over on demand: the current byte is
		/// returned and the next is loaded, DRQ staying asserted until the sector is exhausted. This matches
		/// how the +3's uPD765 controller transfers - the FDC is advanced lazily between the host's polls, so
		/// pacing bytes to a fixed cadence would race the poll loop and spuriously trip Lost Data; TR-DOS
		/// polls and never falls behind, and the flux model still enforces the data-based side of protection.
		/// </summary>
		public byte ReadData()
		{
			byte v = _dataReg;
			if ((_state == State.ReadTransfer || _state == State.ReadAddress) && _drq)
			{
				_xferPtr++;
				if (_xferPtr < _xfer.Length)
				{
					_dataReg = _xfer[_xferPtr];
				}
				else
				{
					_drq = false;
					RaiseDrq(false);
					if (_state == State.ReadAddress) FinishReadAddress();
					else FinishReadSector();
				}
			}
			return v;
		}

		/// <summary>
		/// Write the data register (Beta port 0x7F). Bytes are accepted on demand (same rationale as ReadData):
		/// a Write Sector fills the target sector's data field; a Write Track accumulates the format byte stream
		/// until a track's worth has been supplied. DRQ stays asserted until the transfer is complete.
		/// </summary>
		public void WriteData(byte value)
		{
			_dataReg = value;
			if (_state != State.WriteTransfer || !_drq) return;

			if (_formatMode)
			{
				_formatBuf.Add(value);
				// one DD track is ~6250 bytes at 250 kbit/s x 300 RPM; that is the index-to-index write window
				if (_formatBuf.Count >= 6250)
				{
					_drq = false;
					RaiseDrq(false);
					FinishWriteTrack();
				}
				return;
			}

			_xfer[_xferPtr++] = value;
			if (_xferPtr >= _xfer.Length)
			{
				_drq = false;
				RaiseDrq(false);
				FinishWriteSector();
			}
		}

		// ---- command decode ----

		private void BeginTypeI(byte cmd)
		{
			_typeIStatus = true;
			_status = ST_BUSY;
			RaiseIntrq(false);

			_cyclesPerStep = (int)(CpuClockHz * StepRatesMs[cmd & 0x03] / 1000);
			if (_cyclesPerStep < 1) _cyclesPerStep = 1;
			_verifyAfterSeek = (cmd & 0x04) != 0; // V flag

			int type = cmd >> 4;
			var drive = ActiveDrive;
			int cur = drive?.CurrentCylinder ?? 0;

			switch (type)
			{
				case 0x0: // Restore: seek track 0
					_seekTargetCyl = 0;
					_trackReg = (byte)cur;
					break;
				case 0x1: // Seek: to the cylinder in the data register
					_seekTargetCyl = _dataReg;
					break;
				case 0x2: // Step (repeat last direction) - default toward higher
				case 0x3:
					_seekTargetCyl = cur + (_stepDir >= 0 ? 1 : -1);
					break;
				case 0x4: // Step-In (toward higher cylinders)
				case 0x5:
					_stepDir = +1;
					_seekTargetCyl = cur + 1;
					break;
				case 0x6: // Step-Out (toward track 0)
				case 0x7:
					_stepDir = -1;
					_seekTargetCyl = cur - 1;
					break;
			}

			if (_seekTargetCyl < 0) _seekTargetCyl = 0;
			_stepDir = _seekTargetCyl >= cur ? +1 : -1;
			_timer = _cyclesPerStep;
			_state = State.TypeISeek;
		}

		private void BeginTypeII(byte cmd)
		{
			_typeIStatus = false;
			_status = ST_BUSY;
			_multi = (cmd & 0x10) != 0;
			bool write = (cmd & 0x20) != 0;
			_deletedData = write && (cmd & 0x01) != 0;
			_timer = (cmd & 0x04) != 0 ? _settleCycles : 0; // E flag = 15 ms settle before access

			if (write) StartWriteSector(); // sets WriteTransfer (or errors out)
			else _state = State.ReadSearch;
		}

		private void BeginTypeIII(byte cmd)
		{
			_typeIStatus = false;
			_status = ST_BUSY;
			_multi = false;
			_deletedData = false;
			_timer = (cmd & 0x04) != 0 ? _settleCycles : 0;

			int type = cmd >> 4;
			switch (type)
			{
				case 0xC: StartReadAddress(); break;
				case 0xE: StartReadTrack(); break;   // TODO: raw track read (verbatim gap/mark/data stream)
				case 0xF: StartWriteTrack(); break;  // TODO: track format
			}
		}

		private void ForceInterrupt(byte cmd)
		{
			// abort any command; the low nibble selects the interrupt condition (immediate = bit 3).
			_state = State.Idle;
			_status &= unchecked((byte)~ST_BUSY);
			_drq = false;
			RaiseDrq(false);
			_typeIStatus = true;
			if ((cmd & 0x08) != 0) RaiseIntrq(true); // immediate interrupt
		}

		// ---- clock tick ----

		public void Clock(int cpuCycles)
		{
			for (int i = 0; i < 4; i++) Drives[i]?.Clock(cpuCycles);

			switch (_state)
			{
				case State.Idle:
					break;

				case State.TypeISeek:
					TickSeek(cpuCycles);
					break;

				case State.TypeISettle:
					if ((_timer -= cpuCycles) <= 0)
					{
						if (_verifyAfterSeek) { _state = State.TypeIVerify; }
						else FinishTypeI();
					}
					break;

				case State.TypeIVerify:
					DoVerify();
					break;

				case State.ReadSearch:
					if ((_timer -= cpuCycles) <= 0) DoReadSearch();
					break;

				case State.RnfWait:
					if ((_timer -= cpuCycles) <= 0) FinishTypeIIError(ST2_RNF);
					break;

				case State.ReadTransfer:
				case State.ReadAddress:
					// bytes are handed over on demand as the data register is read, not on a clock cadence
					break;

				case State.WriteWait:
				case State.WriteTransfer:
					// bytes are accepted on demand as the data register is written, not on a clock cadence
					break;
			}
		}

		private void TickSeek(int cpuCycles)
		{
			var drive = ActiveDrive;
			if ((_timer -= cpuCycles) > 0) return;

			int cur = drive?.CurrentCylinder ?? 0;
			if (cur == _seekTargetCyl)
			{
				// arrived: on a verify, take the 30 ms head-settle (1 MHz clock) before checking the ID
				if (_verifyAfterSeek && _verifySettleCycles > 0) { _timer = _verifySettleCycles; _state = State.TypeISettle; }
				else if (_verifyAfterSeek) { _state = State.TypeIVerify; }
				else FinishTypeI();
				return;
			}

			bool up = _seekTargetCyl > cur;
			drive?.Step(up);
			_trackReg = (byte)(drive?.CurrentCylinder ?? 0);
			_timer = _cyclesPerStep;
		}

		private void DoVerify()
		{
			var drive = ActiveDrive;
			var track = drive?.CurrentTrack(_side);
			bool ok = false;
			if (track != null)
			{
				foreach (var s in StandardMfmFormat.DecodeSectors(track, WeakRng))
				{
					if (!s.IdCrcOk) continue;
					if (s.C == _trackReg) { ok = true; break; }
				}
			}
			if (!ok) _status |= ST1_SEEKERR;
			FinishTypeI();
		}

		private void FinishTypeI()
		{
			_status &= unchecked((byte)~ST_BUSY);
			_state = State.Idle;
			RaiseIntrq(true);
		}

		// ---- Type II: Read Sector ----

		private void DoReadSearch()
		{
			var drive = ActiveDrive;
			if (drive == null || !drive.Ready)
			{
				FinishTypeIIError(ST_NOTREADY);
				return;
			}

			var track = drive.CurrentTrack(_side);
			var sectors = track == null ? null : StandardMfmFormat.DecodeSectors(track, WeakRng);
			DecodedSector found = null;
			if (sectors != null)
			{
				foreach (var s in sectors)
				{
					// match the sector-register R (and the track-register C); side compare is left to the
					// Beta side latch, which already selected the physical side above.
					if (s.R == _sectorReg && s.C == _trackReg) { found = s; break; }
				}
			}

			if (found == null)
			{
				BeginRnfWait();
				return;
			}

			_xfer = found.Data ?? System.Array.Empty<byte>();
			_xferPtr = 0;
			_xferCrcError = !found.DataCrcOk;
			_deletedData = found.Deleted;
			if (_xfer.Length == 0) { FinishReadSector(); return; }
			// on-demand transfer: first byte ready now, DRQ held until the sector is drained by ReadData
			_dataReg = _xfer[0];
			_drq = true;
			RaiseDrq(true);
			_state = State.ReadTransfer;
		}

		private void FinishReadSector()
		{
			if (_xferCrcError) _status |= ST2_CRC;
			if (_deletedData) _status |= ST2_RECTYPE;
			if (_multi)
			{
				// multiple-record: advance to the next sector and search again on the next clock
				_sectorReg++;
				_state = State.ReadSearch;
				_timer = 0;
				return;
			}
			_status &= unchecked((byte)~ST_BUSY);
			_state = State.Idle;
			RaiseIntrq(true);
		}

		private void FinishReadAddress()
		{
			if (_xferCrcError) _status |= ST2_CRC;
			_status &= unchecked((byte)~ST_BUSY);
			_state = State.Idle;
			RaiseIntrq(true);
		}

		private void FinishTypeIIError(byte statusBit)
		{
			_status = statusBit;
			_status &= unchecked((byte)~ST_BUSY);
			_state = State.Idle;
			RaiseIntrq(true);
		}

		// The requested ID was not found on the (ready) drive. The real device keeps searching for ~5
		// revolutions before flagging Record Not Found; hold BUSY for that long so software sees the
		// characteristic delay rather than an instant error.
		private void BeginRnfWait()
		{
			_timer = _rnfTimeout;
			_state = State.RnfWait;
		}

		// ---- Type III: Read Address (returns the 6-byte ID field of the next sector) ----

		private void StartReadAddress()
		{
			var drive = ActiveDrive;
			if (drive == null || !drive.Ready) { FinishTypeIIError(ST_NOTREADY); return; }

			var track = drive.CurrentTrack(_side);
			var sectors = track == null ? null : StandardMfmFormat.DecodeSectors(track, WeakRng);
			if (sectors == null || sectors.Count == 0) { BeginRnfWait(); return; }

			// rotate through the track's IDs on successive calls, approximating the sector that happens to be
			// under the head as the disk turns (TR-DOS reads addresses repeatedly to map a track)
			var s = sectors[_readAddrIndex % sectors.Count];
			_readAddrIndex++;
			// the 6 ID bytes: track, side, sector, length, then the ID-field CRC (big-endian, as on disk)
			ushort idCrc = StandardMfmFormat.IdFieldCrc(s.C, s.H, s.R, s.N);
			_xfer = new byte[] { s.C, s.H, s.R, s.N, (byte)(idCrc >> 8), (byte)(idCrc & 0xFF) };
			_xferPtr = 0;
			_xferCrcError = !s.IdCrcOk;
			// Read Address copies the track byte of the ID into the sector register (per the datasheet)
			_sectorReg = s.C;
			_dataReg = _xfer[0];
			_drq = true;
			RaiseDrq(true);
			_state = State.ReadAddress;
		}

		// ---- Type II Write Sector / Type III Read Track, Write Track: next increment ----

		private void StartWriteSector()
		{
			var drive = ActiveDrive;
			if (drive == null || !drive.Ready) { FinishTypeIIError(ST_NOTREADY); return; }
			if (drive.WriteProtected) { FinishTypeIIError(ST_WRITEPROT); return; }

			// decode the current track to an editable form and locate the target sector
			_writeSectors = StandardMfmFormat.ToTrackSectors(drive.CurrentTrack(_side), WeakRng);
			_writeIndex = -1;
			for (int i = 0; i < _writeSectors.Count; i++)
				if (_writeSectors[i].R == _sectorReg && _writeSectors[i].C == _trackReg) { _writeIndex = i; break; }
			if (_writeIndex < 0) { BeginRnfWait(); return; }

			// accept the sector's data bytes on demand, then rebuild the flux track (see WriteData/FinishWriteSector)
			_xfer = new byte[_writeSectors[_writeIndex].SizeBytes];
			_xferPtr = 0;
			_formatMode = false;
			_drq = true;
			RaiseDrq(true);
			_state = State.WriteTransfer;
		}

		private void FinishWriteSector()
		{
			var ts = _writeSectors[_writeIndex];
			ts.Data = _xfer;
			ts.Deleted = _deletedData;
			ts.DataCrcError = false; // a fresh write lays down a correct CRC
			_writeSectors[_writeIndex] = ts;
			ActiveDrive?.WriteTrack(_side, StandardMfmFormat.BuildStandardTrack(_writeSectors));

			if (_multi)
			{
				_sectorReg++;
				StartWriteSector(); // next record (re-decodes the just-written track)
				return;
			}
			_status &= unchecked((byte)~ST_BUSY);
			_state = State.Idle;
			RaiseIntrq(true);
		}

		private void StartReadTrack()
		{
			var drive = ActiveDrive;
			if (drive == null || !drive.Ready) { FinishTypeIIError(ST_NOTREADY); return; }
			var sectors = StandardMfmFormat.DecodeSectors(drive.CurrentTrack(_side), WeakRng);
			if (sectors == null || sectors.Count == 0) { BeginRnfWait(); return; }

			// synthesize a representative IBM System-34 byte stream for the whole track (gaps, sync, address
			// marks, ID + data fields). This is a clean reconstruction from the decoded sectors rather than a
			// verbatim cell-level dump; enough for TR-DOS/copiers that scan the track, CRC bytes left as 0.
			var buf = new List<byte>(sectors.Count * 400);
			foreach (var s in sectors)
			{
				for (int g = 0; g < 12; g++) buf.Add(0x4E);
				buf.Add(0x00); buf.Add(0x00); buf.Add(0x00);
				buf.Add(0xA1); buf.Add(0xA1); buf.Add(0xA1);
				buf.Add(0xFE); buf.Add(s.C); buf.Add(s.H); buf.Add(s.R); buf.Add(s.N);
				buf.Add(0x00); buf.Add(0x00); // ID CRC placeholder
				for (int g = 0; g < 22; g++) buf.Add(0x4E);
				buf.Add(0x00); buf.Add(0x00); buf.Add(0x00);
				buf.Add(0xA1); buf.Add(0xA1); buf.Add(0xA1);
				buf.Add(s.Deleted ? (byte)0xF8 : (byte)0xFB);
				if (s.Data != null) buf.AddRange(s.Data);
				buf.Add(0x00); buf.Add(0x00); // data CRC placeholder
			}

			_xfer = buf.ToArray();
			_xferPtr = 0;
			_xferCrcError = false;
			_deletedData = false;
			_multi = false;
			_dataReg = _xfer[0];
			_drq = true;
			RaiseDrq(true);
			_state = State.ReadTransfer;
		}

		private void StartWriteTrack()
		{
			var drive = ActiveDrive;
			if (drive == null || !drive.Ready) { FinishTypeIIError(ST_NOTREADY); return; }
			if (drive.WriteProtected) { FinishTypeIIError(ST_WRITEPROT); return; }

			// accept the host's format byte stream on demand (see WriteData); FinishWriteTrack parses it
			_formatBuf.Clear();
			_formatMode = true;
			_drq = true;
			RaiseDrq(true);
			_state = State.WriteTransfer;
		}

		private void FinishWriteTrack()
		{
			var sectors = ParseFormatStream(_formatBuf);
			if (sectors.Count > 0)
				ActiveDrive?.WriteTrack(_side, StandardMfmFormat.BuildStandardTrack(sectors));
			_status &= unchecked((byte)~ST_BUSY);
			_state = State.Idle;
			RaiseIntrq(true);
		}

		// Parse a WD179X Write Track (format) byte stream into sectors. In the stream the host writes gap
		// filler (0x4E), sync (0x00), then 0xF5 bytes that the FDC turns into A1 sync marks, an 0xFE ID
		// address mark followed by C H R N and 0xF7 (which writes the ID CRC), more gap/sync, an 0xFB (or
		// 0xF8 for a deleted) data address mark, the data bytes, and 0xF7 (data CRC). We scan for the address
		// marks and read the fixed-size data field after each.
		private static List<TrackSector> ParseFormatStream(List<byte> buf)
		{
			var list = new List<TrackSector>();
			int i = 0, n = buf.Count;
			while (i < n)
			{
				if (buf[i++] != 0xFE) continue; // seek the next ID address mark
				if (i + 4 > n) break;
				byte c = buf[i++], h = buf[i++], r = buf[i++], nn = buf[i++];
				var ts = new TrackSector { C = c, H = h, R = r, N = nn };
				// advance to the data address mark (skip the ID CRC and the gap between ID and data)
				while (i < n && buf[i] != 0xFB && buf[i] != 0xF8 && buf[i] != 0xFE) i++;
				int size = 128 << (nn & 7);
				var data = new byte[size];
				if (i < n && (buf[i] == 0xFB || buf[i] == 0xF8))
				{
					ts.Deleted = buf[i] == 0xF8;
					i++;
					for (int k = 0; k < size && i < n; k++) data[k] = buf[i++];
				}
				ts.Data = data;
				list.Add(ts);
			}
			return list;
		}

		// ---- status composition + line drivers ----

		private byte ComposeStatus()
		{
			var drive = ActiveDrive;
			byte s = _status;

			// bit 7 not ready is meaningful for every command
			if (drive == null || !drive.Ready) s |= ST_NOTREADY; else s &= unchecked((byte)~ST_NOTREADY);

			if (_typeIStatus)
			{
				if (drive != null && drive.Track0) s |= ST1_TRACK0; else s &= unchecked((byte)~ST1_TRACK0);
				if (drive != null && drive.Index) s |= ST1_INDEX; else s &= unchecked((byte)~ST1_INDEX);
				if (drive != null && drive.MotorOn) s |= ST1_HEADLOADED;
				if (drive != null && drive.WriteProtected) s |= ST_WRITEPROT;
			}
			else
			{
				if (_drq) s |= ST2_DRQ; else s &= unchecked((byte)~ST2_DRQ);
			}
			return s;
		}

		private void RaiseIntrq(bool asserted)
		{
			if (_intrq == asserted) return;
			_intrq = asserted;
			Host?.OnFdcInterrupt(asserted);
		}

		private void RaiseDrq(bool asserted)
		{
			Host?.OnFdcDataRequest(asserted);
		}
	}
}
