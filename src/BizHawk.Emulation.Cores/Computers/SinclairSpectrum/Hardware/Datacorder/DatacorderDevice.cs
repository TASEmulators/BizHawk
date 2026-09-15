using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.Z80AOpt;

using System.Collections.Generic;
using BizHawk.Emulation.Cores.Sound;
using BizHawk.Emulation.Cores.Tapes;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Represents the tape device (or built-in datacorder as it was called +2 and above).
	///
	/// The generic tape mechanism (block model, transport, EAR signal generation) lives in the shared
	/// TapeDeck; this class owns a deck and supplies the Spectrum-specific pieces via
	/// ITapeHost: the Z80 cycle counter, the tape beeper, on-screen notifications, tape-format
	/// loading, the ROM auto-detect (tape-trap) monitor, the flash loader and the tape input/output ports.
	/// </summary>
	public sealed class DatacorderDevice : IPortIODevice, ITapeHost
	{
		private SpectrumBase _machine { get; set; }
		private Z80AOpt<ZXSpectrum.CpuLink> _cpu { get; set; }
		private OneBitBeeper _buzzer { get; set; }

		/// <summary>
		/// The shared tape player that holds the loaded blocks and generates the signal.
		/// </summary>
		private TapeDeck _deck;

		/// <summary>
		/// Default constructor
		/// </summary>
		public DatacorderDevice(bool autoplay)
		{
			_autoPlay = autoplay;
		}

		/// <summary>
		/// Initializes the datacorder device
		/// </summary>
		public void Init(SpectrumBase machine)
		{
			_machine = machine;
			_cpu = _machine.CPU;
			_buzzer = machine.TapeBuzzer;

			// Tape formats (TAP/TZX/PZX and CPC CDT) store pulse timings against a 3.5MHz reference. Scale them
			// into this model's CPU clock so the tape plays at the correct rate regardless of model: the 48K is
			// 3.5MHz exactly (ratio 1.0, unchanged), the 128K family is 3.5469MHz (ratio ~1.0134). Without this
			// the 128K plays the same tape ~1.3% faster than the 48K (its higher clock burns the unscaled
			// periods quicker), which does not happen on real hardware where the tape is an external source.
			_deck = new TapeDeck(this, _machine.ULADevice.ClockSpeed / 3_500_000.0);
		}

		// -- shared deck proxies (external callers still talk to DatacorderDevice) --

		public List<TapeDataBlock> DataBlocks
		{
			get => _deck.DataBlocks;
			set => _deck.DataBlocks = value;
		}

		/// <summary>
		/// The loading / copy-protection scheme detected from the currently loaded tape (report-only).
		/// </summary>
		public TapeProtectionScheme DetectedLoader => TapeProtection.Detect(_deck.DataBlocks);

		/// <summary>
		/// Human-readable name of DetectedLoader for the OSD.
		/// </summary>
		public string DetectedLoaderName => TapeProtection.DisplayName(DetectedLoader);

		public int CurrentDataBlockIndex
		{
			get => _deck.CurrentDataBlockIndex;
			set => _deck.CurrentDataBlockIndex = value;
		}

		public int Position => _deck.Position;

		public bool TapeIsPlaying => _deck.TapeIsPlaying;

		public void Play() => _deck.Play();

		public void Stop() => _deck.Stop();

		public void RTZ() => _deck.RTZ();

		public void SkipBlock(bool skipForward) => _deck.SkipBlock(skipForward);

		public void TapeCycle() => _deck.TapeCycle();

		public void Reset() => _deck.Reset();

		public bool GetEarBit(long cpuCycle) => _deck.GetEarBit(cpuCycle);

		/// <summary>
		/// Signs whether the device should autodetect when the Z80 has entered into
		/// 'load' mode and auto-play the tape if neccesary
		/// </summary>
		private bool _autoPlay;

		/// <summary>
		/// Should be fired at the end of every frame
		/// Primary purpose is to detect tape traps and manage auto play
		/// </summary>
		public void EndFrame()
		{
			MonitorFrame();
		}

		/// <summary>
		/// Inserts a new tape and sets up the tape device accordingly
		/// </summary>
		public void LoadTape(byte[] tapeData)
		{
			// instantiate converters
			TzxConverter tzxSer = new TzxConverter(this);
			TapConverter tapSer = new TapConverter(this);
			PzxConverter pzxSer = new PzxConverter(this);
			CswConverter cswSer = new CswConverter(this);
			WavConverter wavSer = new WavConverter(this);

			// TZX
			if (tzxSer.CheckType(tapeData))
			{
				// this file has a tzx header - attempt serialization
				try
				{
					tzxSer.Read(tapeData);
					// reset block index
					CurrentDataBlockIndex = 0;
					return;
				}
				catch (Exception ex)
				{
					// exception during operation
					throw new Exception($"{nameof(DatacorderDevice)}\n\nTape image file has a valid TZX header, but threw an exception whilst data was being parsed.\n\n{ex}", ex);
				}
			}

			// PZX
			else if (pzxSer.CheckType(tapeData))
			{
				// this file has a pzx header - attempt serialization
				try
				{
					pzxSer.Read(tapeData);
					// reset block index
					CurrentDataBlockIndex = 0;
					return;
				}
				catch (Exception ex)
				{
					// exception during operation
					throw new Exception($"{nameof(DatacorderDevice)}\n\nTape image file has a valid PZX header, but threw an exception whilst data was being parsed.\n\n{ex}", ex);
				}
			}

			// CSW
			else if (cswSer.CheckType(tapeData))
			{
				// this file has a csw header - attempt serialization
				try
				{
					cswSer.Read(tapeData);
					// reset block index
					CurrentDataBlockIndex = 0;
					return;
				}
				catch (Exception ex)
				{
					// exception during operation
					throw new Exception($"{nameof(DatacorderDevice)}\n\nTape image file has a valid CSW header, but threw an exception whilst data was being parsed.\n\n{ex}", ex);
				}
			}

			// WAV
			else if (wavSer.CheckType(tapeData))
			{
				// this file has a csw header - attempt serialization
				try
				{
					wavSer.Read(tapeData);
					// reset block index
					CurrentDataBlockIndex = 0;
					return;
				}
				catch (Exception ex)
				{
					// exception during operation
					throw new Exception($"{nameof(DatacorderDevice)}\n\nTape image file has a valid WAV header, but threw an exception whilst data was being parsed.\n\n{ex}", ex);
				}
			}

			// Assume TAP
			else
			{
				try
				{
					tapSer.Read(tapeData);
					// reset block index
					CurrentDataBlockIndex = 0;
					return;
				}
				catch (Exception ex)
				{
					// exception during operation
					throw new Exception($"{nameof(DatacorderDevice)}\n\nAn exception was thrown whilst data from this tape image was being parsed as TAP.\n\n{ex}", ex);
				}
			}
		}

		/// <summary>
		/// Flash loading implementation
		/// (Deterministic Emulation must be FALSE)
		/// CURRENTLY NOT ENABLED/WORKING
		/// </summary>
		private bool FlashLoad()
		{
			// deterministic emulation must = false
			//if (_machine.Spectrum.SyncSettings.DeterministicEmulation)
			//return;

			var util = _machine.Spectrum;

			if (CurrentDataBlockIndex < 0)
				CurrentDataBlockIndex = 0;

			if (CurrentDataBlockIndex >= DataBlocks.Count)
				return false;

			//var val = GetEarBit(_cpu.TotalExecutedCycles);
			//_buzzer.ProcessPulseValue(true, val);

			ushort addr = _cpu.RegPC;

			var tb = DataBlocks[CurrentDataBlockIndex];
			var tData = tb.BlockData;

			if (tData == null || tData.Length < 2)
			{
				// skip this
				return false;
			}

			var toRead = tData.Length - 1;

			if (toRead < _cpu.Regs[_cpu.E] + (_cpu.Regs[_cpu.D] << 8))
			{
				// no-op
			}
			else
			{
				toRead = _cpu.Regs[_cpu.E] + (_cpu.Regs[_cpu.D] << 8);
			}

			if (toRead <= 0)
				return false;

			var parity = tData[0];

			if (parity != _cpu.Regs[_cpu.F_s] + (_cpu.Regs[_cpu.A_s] << 8) >> 8)
				return false;

			util.SetCpuRegister("Shadow AF", 0x0145);

			for (var i = 0; i < toRead; i++)
			{
				var v = tData[i + 1];
				_cpu.Regs[_cpu.L] = v;
				parity ^= v;
				var d = (ushort)(_cpu.Regs[_cpu.Ixl] + (_cpu.Regs[_cpu.Ixh] << 8) + 1);
				_machine.WriteBus(d, v);
			}
			var pc = (ushort)0x05DF;

			if (_cpu.Regs[_cpu.E] + (_cpu.Regs[_cpu.D] << 8) == toRead
				&& toRead + 1 < tData.Length)
			{
				var v = tData[toRead + 1];
				_cpu.Regs[_cpu.L] = v;
				parity ^= v;
				_cpu.Regs[_cpu.B] = 0xB0;
			}
			else
			{
				_cpu.Regs[_cpu.L] = 1;
				_cpu.Regs[_cpu.B] = 0;
				_cpu.Regs[_cpu.F] = 0x50;
				_cpu.Regs[_cpu.A] = parity;
				pc = 0x05EE;
			}

			_cpu.Regs[_cpu.H] = parity;
			var de = _cpu.Regs[_cpu.E] + (_cpu.Regs[_cpu.D] << 8);
			util.SetCpuRegister("DE", de - toRead);
			var ix = _cpu.Regs[_cpu.Ixl] + (_cpu.Regs[_cpu.Ixh] << 8);
			util.SetCpuRegister("IX", ix + toRead);

			util.SetCpuRegister("PC", pc);

			CurrentDataBlockIndex++;

			return true;
		}

		private long _lastINCycle = 0;
		private int _monitorCount;
		private int _monitorTimeOut;
		private ushort _monitorLastPC;
		private ushort[] _monitorLastRegs = new ushort[7];

		/// <summary>
		/// Resets the TapeMonitor
		/// </summary>
		private void MonitorReset()
		{
			_lastINCycle = 0;
			_monitorCount = 0;
			_monitorLastPC = 0;
			_monitorLastRegs = null;
		}

		/// <summary>
		/// An iteration of the monitor process
		/// </summary>
		public void MonitorRead()
		{
			long cpuCycle = _cpu.TotalExecutedCycles;
			int delta = (int)(cpuCycle - _lastINCycle);
			_lastINCycle = cpuCycle;

			var nRegs = new ushort[]
			{
				_cpu.Regs[_cpu.A],
				_cpu.Regs[_cpu.B],
				_cpu.Regs[_cpu.C],
				_cpu.Regs[_cpu.D],
				_cpu.Regs[_cpu.E],
				_cpu.Regs[_cpu.H],
				_cpu.Regs[_cpu.L]
			};

			if (delta is > 0 and < 96 && _cpu.RegPC == _monitorLastPC && _monitorLastRegs is not null)
			{
				int dCnt = 0;
				int dVal = 0;

				for (int i = 0; i < nRegs.Length; i++)
				{
					if (_monitorLastRegs[i] != nRegs[i])
					{
						dVal = _monitorLastRegs[i] - nRegs[i];
						dCnt++;
					}
				}

				if (dCnt is 1 && dVal is 1 or -1)
				{
					_monitorCount++;

					if (_monitorCount >= 16 && _autoPlay)
					{
						if (!TapeIsPlaying)
						{
							Play();
							_machine.Spectrum.OSD_TapePlayingAuto();
						}

						_monitorTimeOut = 50;
					}
				}
				else
				{
					_monitorCount = 0;
				}
			}

			_monitorLastRegs = nRegs;
			_monitorLastPC = _cpu.RegPC;
		}

		public void AutoStopTape()
		{
			if (!TapeIsPlaying)
				return;

			if (!_autoPlay)
				return;

			Stop();
			_machine.Spectrum.OSD_TapeStoppedAuto();
		}

		public void AutoStartTape()
		{
			if (TapeIsPlaying)
				return;

			if (!_autoPlay)
				return;

			Play();
			_machine.Spectrum.OSD_TapePlayingAuto();
		}

		public int MaskableInterruptCount = 0;

		private void MonitorFrame()
		{
			if (TapeIsPlaying && _autoPlay)
			{
				if (DataBlocks.Count > 1
					|| DataBlocks[CurrentDataBlockIndex].BlockDescription is not (BlockType.CSW_Recording or BlockType.WAV_Recording))
				{
					// we should only stop the tape when there are multiple blocks
					// if we just have one big block (maybe a CSW or WAV) then auto stopping will cock things up
					_monitorTimeOut--;
				}

				if (_monitorTimeOut < 0)
				{
					if (DataBlocks[CurrentDataBlockIndex].BlockDescription is not (BlockType.PAUSE_BLOCK or BlockType.PAUS))
					{
						AutoStopTape();
					}

					return;
				}

				// fallback in case usual monitor detection methods do not work

				// number of t-states since last IN operation
				long diff = _machine.CPU.TotalExecutedCycles - _lastINCycle;

				// get current datablock
				var block = DataBlocks[CurrentDataBlockIndex];

				// is this a pause block?
				if (block.BlockDescription is BlockType.PAUS or BlockType.PAUSE_BLOCK)
				{
					// don't autostop the tape here
					return;
				}

				// pause in ms at the end of the current block
				int blockPause = block.PauseInMS;

				// timeout in t-states (equiv. to blockpause)
				int timeout = ((_machine.ULADevice.FrameLength * 50) / 1000) * blockPause;

				// don't use autostop detection if block has no pause at the end
				if (timeout == 0)
					return;

				// don't autostop if there is only 1 block
				if (DataBlocks.Count is 1
					|| DataBlocks[CurrentDataBlockIndex].BlockDescription is BlockType.CSW_Recording or BlockType.WAV_Recording)
				{
					return;
				}

				if (diff >= timeout * 2)
				{
					// There have been no attempted tape reads by the CPU within the double timeout period
					// Autostop the tape
					AutoStopTape();
					_deck.ResetLastCycleToNow();
				}
			}
		}

		/// <summary>
		/// Mask constants
		/// </summary>
		private const int TAPE_BIT = 0x40;
		private const int EAR_BIT = 0x10;
		private const int MIC_BIT = 0x08;

		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public bool ReadPort(ushort port, ref int result)
		{
			if (TapeIsPlaying)
			{
				_deck.GetEarBit(_cpu.TotalExecutedCycles);
			}
			if (_deck.CurrentEarLevel)
			{
				result |= TAPE_BIT;
			}
			else
			{
				result &= ~TAPE_BIT;
			}

			if (!TapeIsPlaying)
			{
				if (_machine.UPDDiskDevice == null || !_machine.UPDDiskDevice.FDD_IsDiskLoaded)
					MonitorRead();
			}
			if (_machine.UPDDiskDevice == null || !_machine.UPDDiskDevice.FDD_IsDiskLoaded)
				MonitorRead();

			return true;
		}

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		public bool WritePort(ushort port, int result)
		{
			if (!TapeIsPlaying)
			{
				_deck.CurrentEarLevel = ((byte)result & 0x10) != 0;
			}

			return true;
		}

		/// <summary>
		/// Bizhawk state serialization
		/// </summary>
		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(DatacorderDevice));
			// transport/signal state (field names + order preserve the pre-extraction savestate layout)
			_deck.SyncState(ser);
			// auto-detect (tape-trap) monitor state
			ser.Sync(nameof(_lastINCycle), ref _lastINCycle);
			ser.Sync(nameof(_monitorCount), ref _monitorCount);
			ser.Sync(nameof(_monitorTimeOut), ref _monitorTimeOut);
			ser.Sync(nameof(_monitorLastPC), ref _monitorLastPC);
			ser.Sync(nameof(_monitorLastRegs), ref _monitorLastRegs, false);
			ser.EndSection();
		}

		// -- ITapeHost: the services the shared deck needs from the Spectrum --

		long ITapeHost.TotalExecutedCycles => _cpu.TotalExecutedCycles;

		bool ITapeHost.IsIn48kMode => _machine.IsIn48kMode();

		bool ITapeHost.FastLoadAllowed => !_machine.Spectrum.DeterministicEmulation;

		void ITapeHost.FeedBeeper(bool earLevel)
		{
			// event-driven clock: position the beeper at the current frame cycle before the pulse
			_buzzer.SetClock((int)_machine.CurrentFrameCycle);
			_buzzer.ProcessPulseValue(earLevel);
		}

		void ITapeHost.NotifyPlay() => _machine.Spectrum.OSD_TapePlaying();

		void ITapeHost.NotifyStop() => _machine.Spectrum.OSD_TapeStopped();

		void ITapeHost.NotifyRewind() => _machine.Spectrum.OSD_TapeRTZ();

		void ITapeHost.NotifyNextBlock(string blockInfo) => _machine.Spectrum.OSD_TapeNextBlock(blockInfo);

		void ITapeHost.NotifyPrevBlock(string blockInfo) => _machine.Spectrum.OSD_TapePrevBlock(blockInfo);

		void ITapeHost.NotifyPlayingBlock(string blockInfo) => _machine.Spectrum.OSD_TapePlayingBlockInfo(blockInfo);

		void ITapeHost.NotifySkipBlock(string blockInfo) => _machine.Spectrum.OSD_TapePlayingSkipBlockInfo(blockInfo);

		void ITapeHost.NotifyStoppedAuto() => _machine.Spectrum.OSD_TapeStoppedAuto();

		void ITapeHost.NotifyStopCommand() => _monitorTimeOut = 2000;
	}
}
