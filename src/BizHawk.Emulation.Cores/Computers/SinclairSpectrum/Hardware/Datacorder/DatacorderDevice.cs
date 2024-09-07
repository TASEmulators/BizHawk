using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Cores.Sound;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Represents the tape device (or build-in datacorder as it was called +2 and above)
	/// </summary>
	public sealed class DatacorderDevice : IPortIODevice
	{
		private SpectrumBase _machine { get; set; }
		private Z80A<ZXSpectrum.CpuLink> _cpu { get; set; }
		private OneBitBeeper _buzzer { get; set; }

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
		}

		/// <summary>
		/// Internal counter used to trigger tape buzzer output
		/// </summary>
		private int counter = 0;

		/// <summary>
		/// The index of the current tape data block that is loaded
		/// </summary>
		private int _currentDataBlockIndex = 0;
		public int CurrentDataBlockIndex
		{
			get
			{
				if (_dataBlocks.Count > 0) { return _currentDataBlockIndex; }
				else { return -1; }
			}
			set
			{
				if (value == _currentDataBlockIndex) { return; }
				if (value < _dataBlocks.Count && value >= 0)
				{
					_currentDataBlockIndex = value;
					_position = 0;
				}
			}
		}

		/// <summary>
		/// The current position within the current data block
		/// </summary>
		private int _position = 0;
		public int Position
		{
			get
			{
				if (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count) { return 0; }

				return _position;
			}
		}

		/// <summary>
		/// Signs whether the tape is currently playing or not
		/// </summary>
		private bool _tapeIsPlaying = false;
		public bool TapeIsPlaying => _tapeIsPlaying;

		/// <summary>
		/// A list of the currently loaded data blocks
		/// </summary>
		private List<TapeDataBlock> _dataBlocks = new List<TapeDataBlock>();
		public List<TapeDataBlock> DataBlocks
		{
			get => _dataBlocks;
			set => _dataBlocks = value;
		}

		/// <summary>
		/// Stores the last CPU t-state value
		/// </summary>
		private long _lastCycle = 0;

		/// <summary>
		/// Edge
		/// </summary>
		private int _waitEdge = 0;

		/// <summary>
		/// Current tapebit state
		/// </summary>
		private bool currentState = false;

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
		/// Starts the tape playing from the beginning of the current block
		/// </summary>
		public void Play()
		{
			if (_tapeIsPlaying)
				return;

			_machine.Spectrum.OSD_TapePlaying();

			// update the lastCycle
			_lastCycle = _cpu.TotalExecutedCycles;

			// reset waitEdge and position
			_waitEdge = 0;
			_position = 0;

			if (_dataBlocks.Count > 0 && _currentDataBlockIndex >= 0) //TODO removed a comment that said "index is 1 or greater", but code is clearly "0 or greater"--which is correct? --yoshi
			{
				while (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count)
				{
					// we are at the end of a data block - move to the next
					_position = 0;
					_currentDataBlockIndex++;

					// are we at the end of the tape?
					if (_currentDataBlockIndex >= _dataBlocks.Count)
					{
						break;
					}
				}

				// check for end of tape
				if (_currentDataBlockIndex >= _dataBlocks.Count)
				{
					// end of tape reached. Rewind to beginning
					AutoStopTape();
					RTZ();
					return;
				}

				// update waitEdge with the current position in the current block
				_waitEdge = _dataBlocks[_currentDataBlockIndex].DataPeriods[_position];

				// sign that the tape is now playing
				_tapeIsPlaying = true;
			}
		}

		/// <summary>
		/// Stops the tape
		/// (should move to the beginning of the next block)
		/// </summary>
		public void Stop()
		{
			if (!_tapeIsPlaying)
				return;

			_machine.Spectrum.OSD_TapeStopped();

			// sign that the tape is no longer playing
			_tapeIsPlaying = false;

			if (_currentDataBlockIndex >= 0 // we are at datablock 1 or above //TODO 1-indexed then? --yoshi
				&& _position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count - 1) // the block is still playing back
			{
				// move to the next block
				_currentDataBlockIndex++;

				if (_currentDataBlockIndex >= _dataBlocks.Count)
				{
					_currentDataBlockIndex = -1;
				}

				// reset waitEdge and position
				_waitEdge = 0;
				_position = 0;

				if (_currentDataBlockIndex < 0 && _dataBlocks.Count > 0) //TODO deleted a comment that said "block index is -1", but code is clearly "is negative"--are lower values not reachable? --yoshi
				{
					// move the index on to 0
					_currentDataBlockIndex = 0;
				}
			}

			// update the lastCycle
			_lastCycle = _cpu.TotalExecutedCycles;
		}

		/// <summary>
		/// Rewinds the tape to it's beginning (return to zero)
		/// </summary>
		public void RTZ()
		{
			Stop();
			_machine.Spectrum.OSD_TapeRTZ();
			_currentDataBlockIndex = 0;
		}

		/// <summary>
		/// Performs a block skip operation on the current tape
		/// TRUE:   skip forward
		/// FALSE:  skip backward
		/// </summary>
		public void SkipBlock(bool skipForward)
		{
			int blockCount = _dataBlocks.Count;
			int targetBlockId = _currentDataBlockIndex;

			if (skipForward)
			{
				if (_currentDataBlockIndex == blockCount - 1)
				{
					// last block, go back to beginning
					targetBlockId = 0;
				}
				else
				{
					targetBlockId++;
				}
			}
			else
			{
				if (_currentDataBlockIndex == 0)
				{
					// already first block, goto last block
					targetBlockId = blockCount - 1;
				}
				else
				{
					targetBlockId--;
				}
			}

			var bl = _dataBlocks[targetBlockId];

			StringBuilder sbd = new StringBuilder();
			sbd.Append('(');
			sbd.Append((targetBlockId + 1) + " of " + _dataBlocks.Count);
			sbd.Append(") : ");
			sbd.Append(bl.BlockDescription);
			if (bl.MetaData.Count > 0)
			{
				sbd.Append(" - ");
				sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
			}

			if (skipForward)
				_machine.Spectrum.OSD_TapeNextBlock(sbd.ToString());
			else
				_machine.Spectrum.OSD_TapePrevBlock(sbd.ToString());

			CurrentDataBlockIndex = targetBlockId;
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
		/// Resets the tape
		/// </summary>
		public void Reset()
		{
			RTZ();
		}

		/// <summary>
		/// Is called every cpu cycle but runs every 50 t-states
		/// This enables the tape devices to play out even if the spectrum itself is not
		/// requesting tape data
		/// </summary>
		public void TapeCycle()
		{
			if (TapeIsPlaying)
			{
				counter++;

				if (counter > 20)
				{
					counter = 0;
					bool state = GetEarBit(_machine.CPU.TotalExecutedCycles);
					_buzzer.ProcessPulseValue(state);
				}
			}
		}

		/// <summary>
		/// Simulates the spectrum 'EAR' input reading data from the tape
		/// </summary>
		public bool GetEarBit(long cpuCycle)
		{
			// decide how many cycles worth of data we are capturing
			long cycles = cpuCycle - _lastCycle;

			bool is48k = _machine.IsIn48kMode();

			// check whether tape is actually playing
			if (!_tapeIsPlaying)
			{
				// it's not playing. Update lastCycle and return
				_lastCycle = cpuCycle;
				return false;
			}

			// check for end of tape
			if (_currentDataBlockIndex < 0)
			{
				// end of tape reached - RTZ (and stop)
				RTZ();
				return currentState;
			}

			// process the cycles based on the waitEdge
			while (cycles >= _waitEdge)
			{
				// decrement cycles
				cycles -= _waitEdge;

				if (_position == 0 && _tapeIsPlaying)
				{				
					// notify about the current block
					var bl = _dataBlocks[_currentDataBlockIndex];

					StringBuilder sbd = new StringBuilder();
					sbd.Append('(');
					sbd.Append((_currentDataBlockIndex + 1) + " of " + _dataBlocks.Count);
					sbd.Append(") : ");
					sbd.Append(bl.BlockDescription);
					if (bl.MetaData.Count > 0)
					{
						sbd.Append(" - ");
						sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
					}
					_machine.Spectrum.OSD_TapePlayingBlockInfo(sbd.ToString());
				}

				// increment the current period position
				_position++;

				if (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count)
				{
					// we have reached the end of the current block
					if (_dataBlocks[_currentDataBlockIndex].DataPeriods.Count == 0)
					{
						// notify about the current block (we are skipping it because its empty)
						var bl = _dataBlocks[_currentDataBlockIndex];
						StringBuilder sbd = new StringBuilder();
						sbd.Append('(');
						sbd.Append((_currentDataBlockIndex + 1) + " of " + _dataBlocks.Count);
						sbd.Append(") : ");
						sbd.Append(bl.BlockDescription);
						if (bl.MetaData.Count > 0)
						{
							sbd.Append(" - ");
							sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
						}
						_machine.Spectrum.OSD_TapePlayingSkipBlockInfo(sbd.ToString());

					}

					// skip any empty blocks (and process any command blocks)
					while (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count)
					{
						// check for any commands
						var command = _dataBlocks[_currentDataBlockIndex].Command;
						var block = _dataBlocks[_currentDataBlockIndex];
						bool shouldStop = false;
						switch (command)
						{
							// Stop the tape command found - if this is the end of the tape RTZ
							// otherwise just STOP and move to the next block
							case TapeCommand.STOP_THE_TAPE:

								_machine.Spectrum.OSD_TapeStoppedAuto();
								shouldStop = true;

								if (_currentDataBlockIndex >= _dataBlocks.Count)
									RTZ();
								else
								{
									Stop();
								}

								_monitorTimeOut = 2000;
								break;
							case TapeCommand.STOP_THE_TAPE_48K:
								if (is48k)
								{
									_machine.Spectrum.OSD_TapeStoppedAuto();
									shouldStop = true;

									if (_currentDataBlockIndex >= _dataBlocks.Count)
										RTZ();
									else
									{
										Stop();
									}

									_monitorTimeOut = 2000;
								}
								break;
						}

						if (shouldStop)
							break;

						_position = 0;
						_currentDataBlockIndex++;

						if (_currentDataBlockIndex >= _dataBlocks.Count)
						{
							break;
						}
					}

					// check for end of tape
					if (_currentDataBlockIndex >= _dataBlocks.Count)
					{
						_currentDataBlockIndex = -1;
						RTZ();
						return currentState;
					}
				}

				// update waitEdge with current position within the current block
				_waitEdge = _dataBlocks[_currentDataBlockIndex].DataPeriods.Count > 0 ? _dataBlocks[_currentDataBlockIndex].DataPeriods[_position] : 0;

				// flip the current state
				//FlipTapeState();
				currentState = _dataBlocks[_currentDataBlockIndex].DataLevels[_position];
			}

			// update lastCycle and return currentstate
			_lastCycle = cpuCycle - cycles;

			return currentState;
		}

		private void FlipTapeState()
		{
			currentState = !currentState;
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

			if (_currentDataBlockIndex < 0)
				_currentDataBlockIndex = 0;

			if (_currentDataBlockIndex >= DataBlocks.Count)
				return false;

			//var val = GetEarBit(_cpu.TotalExecutedCycles);
			//_buzzer.ProcessPulseValue(true, val);

			ushort addr = _cpu.RegPC;

			if (_machine.Spectrum.SyncSettings.DeterministicEmulation)
			{

			}

			var tb = DataBlocks[_currentDataBlockIndex];
			var tData = tb.BlockData;

			if (tData == null || tData.Length < 2)
			{
				// skip this
				return false;
			}

			var toRead = tData.Length - 1;

			if (toRead < _cpu.Regs[_cpu.E] + (_cpu.Regs[_cpu.D] << 8))
			{

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

			_currentDataBlockIndex++;

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
						if (!_tapeIsPlaying)
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
			if (!_tapeIsPlaying)
				return;

			if (!_autoPlay)
				return;

			Stop();
			_machine.Spectrum.OSD_TapeStoppedAuto();
		}

		public void AutoStartTape()
		{
			if (_tapeIsPlaying)
				return;

			if (!_autoPlay)
				return;

			Play();
			_machine.Spectrum.OSD_TapePlayingAuto();
		}

		public int MaskableInterruptCount = 0;

		private void MonitorFrame()
		{
			if (_tapeIsPlaying && _autoPlay)
			{
				if (DataBlocks.Count > 1
					|| _dataBlocks[_currentDataBlockIndex].BlockDescription is not (BlockType.CSW_Recording or BlockType.WAV_Recording))
				{
					// we should only stop the tape when there are multiple blocks
					// if we just have one big block (maybe a CSW or WAV) then auto stopping will cock things up
					_monitorTimeOut--;
				}

				if (_monitorTimeOut < 0)
				{
					if (_dataBlocks[_currentDataBlockIndex].BlockDescription is not (BlockType.PAUSE_BLOCK or BlockType.PAUS))
					{
						AutoStopTape();
					}

					return;
				}

				// fallback in case usual monitor detection methods do not work

				// number of t-states since last IN operation
				long diff = _machine.CPU.TotalExecutedCycles - _lastINCycle;

				// get current datablock
				var block = DataBlocks[_currentDataBlockIndex];

				// is this a pause block?
				if (block.BlockDescription == BlockType.PAUS || block.BlockDescription == BlockType.PAUSE_BLOCK)
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
					|| _dataBlocks[_currentDataBlockIndex].BlockDescription is BlockType.CSW_Recording or BlockType.WAV_Recording)
				{
					return;
				}

				if (diff >= timeout * 2)
				{
					// There have been no attempted tape reads by the CPU within the double timeout period
					// Autostop the tape
					AutoStopTape();
					_lastCycle = _cpu.TotalExecutedCycles;
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
				GetEarBit(_cpu.TotalExecutedCycles);
			}
			if (currentState)
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
				currentState = ((byte)result & 0x10) != 0;
			}

			return true;
		}

		/// <summary>
		/// Bizhawk state serialization
		/// </summary>
		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(DatacorderDevice));
			ser.Sync(nameof(counter), ref counter);
			ser.Sync(nameof(_currentDataBlockIndex), ref _currentDataBlockIndex);
			ser.Sync(nameof(_position), ref _position);
			ser.Sync(nameof(_tapeIsPlaying), ref _tapeIsPlaying);
			ser.Sync(nameof(_lastCycle), ref _lastCycle);
			ser.Sync(nameof(_waitEdge), ref _waitEdge);
			ser.Sync(nameof(currentState), ref currentState);
			ser.Sync(nameof(_lastINCycle), ref _lastINCycle);
			ser.Sync(nameof(_monitorCount), ref _monitorCount);
			ser.Sync(nameof(_monitorTimeOut), ref _monitorTimeOut);
			ser.Sync(nameof(_monitorLastPC), ref _monitorLastPC);
			ser.Sync(nameof(_monitorLastRegs), ref _monitorLastRegs, false);
			ser.EndSection();
		}
	}
}
