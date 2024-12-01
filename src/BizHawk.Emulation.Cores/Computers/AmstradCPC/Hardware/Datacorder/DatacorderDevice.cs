﻿using BizHawk.Common;

//using BizHawk.Emulation.Cores.Components.Z80A;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// Represents the tape device
	/// </summary>
	public sealed class DatacorderDevice
	{
		private CPCBase _machine;
		//private Z80A<AmstradCPC.CpuLink> CPU => _machine.CPU;
		private LibFz80Wrapper CPU => _machine.CPU;
		private IBeeperDevice Buzzer => _machine.TapeBuzzer;

		/// <summary>
		/// Default constructor
		/// </summary>
		public DatacorderDevice(bool autoTape)
		{
			_autoPlay = autoTape;
		}

		/// <summary>
		/// Initializes the datacorder device
		/// </summary>
		public void Init(CPCBase machine) => _machine = machine;

		/// <summary>
		/// Signs whether the tape motor is running
		/// </summary>
		private bool tapeMotor;
		public bool TapeMotor
		{
			get => tapeMotor;
			set
			{
				if (tapeMotor == value)
					return;

				tapeMotor = value;
				if (tapeMotor)
				{
					_machine.CPC.OSD_TapeMotorActive();

					if (_autoPlay)
					{
						Play();
					}
				}

				else
				{
					_machine.CPC.OSD_TapeMotorInactive();

					if (_autoPlay)
					{
						Stop();
					}
				}

			}
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
				if (DataBlocks.Any())
				{
					return _currentDataBlockIndex;
				}

				return -1;
			}
			set
			{
				if (value == _currentDataBlockIndex) { return; }
				if (value < DataBlocks.Count && value >= 0)
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
		public int Position => _position >= DataBlocks[_currentDataBlockIndex].DataPeriods.Count ? 0 : _position;

		/// <summary>
		/// Signs whether the tape is currently playing or not
		/// </summary>
		private bool _tapeIsPlaying = false;
		public bool TapeIsPlaying => _tapeIsPlaying;

		public List<TapeDataBlock> DataBlocks { get; set; } = new List<TapeDataBlock>();

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
		private readonly bool _autoPlay;

		/// <summary>
		/// Should be fired at the end of every frame
		/// Primary purpose is to detect tape traps and manage auto play (if/when this is ever implemented)
		/// </summary>
		public void EndFrame()
		{
			//MonitorFrame();
		}

		public void StartFrame()
		{
			//_buzzer.ProcessPulseValue(currentState);
		}

		/// <summary>
		/// Starts the tape playing from the beginning of the current block
		/// </summary>
		public void Play()
		{
			if (_tapeIsPlaying)
				return;

			if (!_autoPlay)
				_machine.CPC.OSD_TapePlaying();

			_machine.CPC.OSD_TapeMotorActive();

			// update the lastCycle
			_lastCycle = CPU.TotalExecutedCycles;

			// reset waitEdge and position
			_waitEdge = 0;
			_position = 0;

			if (DataBlocks.Count > 0 && _currentDataBlockIndex >= 0) //TODO removed a comment that said "index is 1 or greater", but code is clearly "0 or greater"--which is correct? --yoshi
			{
				while (_position >= DataBlocks[_currentDataBlockIndex].DataPeriods.Count)
				{
					// we are at the end of a data block - move to the next
					_position = 0;
					_currentDataBlockIndex++;

					// are we at the end of the tape?
					if (_currentDataBlockIndex >= DataBlocks.Count)
					{
						break;
					}
				}

				// check for end of tape
				if (_currentDataBlockIndex >= DataBlocks.Count)
				{
					// end of tape reached. Rewind to beginning
					AutoStopTape();
					RTZ();
					return;
				}

				// update waitEdge with the current position in the current block
				_waitEdge = DataBlocks[_currentDataBlockIndex].DataPeriods[_position];

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

			_machine.CPC.OSD_TapeStopped();

			// sign that the tape is no longer playing
			_tapeIsPlaying = false;

			if (_currentDataBlockIndex >= 0 // we are at datablock 1 or above //TODO 1-indexed then? --yoshi
				&& _position >= DataBlocks[_currentDataBlockIndex].DataPeriods.Count - 1) // the block is still playing back
			{
				// move to the next block
				_currentDataBlockIndex++;

				if (_currentDataBlockIndex >= DataBlocks.Count)
				{
					_currentDataBlockIndex = -1;
				}

				// reset waitEdge and position
				_waitEdge = 0;
				_position = 0;

				if (_currentDataBlockIndex < 0 && DataBlocks.Count > 0) //TODO deleted a comment that said "block index is -1", but code is clearly "is negative"--are lower values not reachable? --yoshi
				{
					// move the index on to 0
					_currentDataBlockIndex = 0;
				}
			}

			// update the lastCycle
			_lastCycle = CPU.TotalExecutedCycles;
		}

		/// <summary>
		/// Rewinds the tape to it's beginning (return to zero)
		/// </summary>
		public void RTZ()
		{
			Stop();
			_machine.CPC.OSD_TapeRTZ();
			_currentDataBlockIndex = 0;
		}

		/// <summary>
		/// Performs a block skip operation on the current tape
		/// TRUE:   skip forward
		/// FALSE:  skip backward
		/// </summary>
		public void SkipBlock(bool skipForward)
		{
			int blockCount = DataBlocks.Count;
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

			var bl = DataBlocks[targetBlockId];

			var sbd = new StringBuilder();
			sbd.Append('(');
			sbd.Append((targetBlockId + 1) + " of " + DataBlocks.Count);
			sbd.Append(") : ");
			//sbd.Append("ID" + bl.BlockID.ToString("X2") + " - ");
			sbd.Append(bl.BlockDescription);
			if (bl.MetaData.Count > 0)
			{
				sbd.Append(" - ");
				sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
				//sbd.AppendLine();
				//sbd.Append(bl.MetaData.Skip(1).First().Key + ": " + bl.MetaData.Skip(1).First().Value);
			}

			if (skipForward)
				_machine.CPC.OSD_TapeNextBlock(sbd.ToString());
			else
				_machine.CPC.OSD_TapePrevBlock(sbd.ToString());

			CurrentDataBlockIndex = targetBlockId;
		}

		/// <summary>
		/// Inserts a new tape and sets up the tape device accordingly
		/// </summary>
		public void LoadTape(byte[] tapeData)
		{
			// instantiate converters
			var cdtSer = new CdtConverter(this);

			// CDT
			if (cdtSer.CheckType(tapeData))
			{
				// this file has a tzx header - attempt serialization
				try
				{
					cdtSer.Read(tapeData);
					// reset block index
					CurrentDataBlockIndex = 0;
					return;
				}
				catch (Exception ex)
				{
					// exception during operation
					throw new Exception($"{nameof(DatacorderDevice)}\n\nTape image file has a valid CDT header, but threw an exception whilst data was being parsed.\n\n{ex}", ex);
				}
			}
		}

		/// <summary>
		/// Resets the tape
		/// </summary>
		public void Reset() => RTZ();

		/// <summary>
		/// Is called every cpu cycle but runs every 50 t-states
		/// This enables the tape devices to play out even if the spectrum itself is not
		/// requesting tape data
		/// </summary>
		public void TapeCycle()
		{
			if (TapeMotor)
			{
				counter++;

				if (counter > 20)
				{
					counter = 0;
					bool state = GetEarBit(_machine.CPU.TotalExecutedCycles);
					Buzzer.ProcessPulseValue(state);
				}
			}
		}

		/// <summary>
		/// Simulates the spectrum 'EAR' input reading data from the tape
		/// </summary>
		public bool GetEarBit(long cpuCycle)
		{
			if (DataBlocks.Count == 0)
			{
				// no tape loaded
				return false;
			}

			// decide how many cycles worth of data we are capturing
			long cycles = cpuCycle - _lastCycle;

			// check whether tape is actually playing
			if (!tapeMotor)
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

				if (_position == 0 && tapeMotor)
				{
					// start of block - take care of initial pulse level for PZX
					switch (DataBlocks[_currentDataBlockIndex].BlockDescription)
					{
						case BlockType.PULS:
							// initial pulse level is always low
							if (currentState)
								FlipTapeState();
							break;
						case BlockType.DATA:
							// initial pulse level is stored in block
							if (currentState != DataBlocks[_currentDataBlockIndex].InitialPulseLevel)
								FlipTapeState();
							break;
						case BlockType.PAUS:
							// initial pulse level is stored in block
							if (currentState != DataBlocks[_currentDataBlockIndex].InitialPulseLevel)
								FlipTapeState();
							break;
					}

					// most of these amstrad tapes appear to have a pause block at the start
					// skip this if it is the first block
					switch (DataBlocks[_currentDataBlockIndex].BlockDescription)
					{
						case BlockType.PAUS:
						case BlockType.PAUSE_BLOCK:
						case BlockType.Pause_or_Stop_the_Tape:
							if (_currentDataBlockIndex == 0)
							{
								// this is the first block on the tape
								SkipBlock(true);
							}
							else
							{
								// there may be non-data blocks before this
								bool okToSkipPause = true;
								for (int i = _currentDataBlockIndex; i >= 0; i--)
								{
									switch (DataBlocks[i].BlockDescription)
									{
										case BlockType.Archive_Info:
										case BlockType.BRWS:
										case BlockType.Custom_Info_Block:
										case BlockType.Emulation_Info:
										case BlockType.Glue_Block:
										case BlockType.Hardware_Type:
										case BlockType.Message_Block:
										case BlockType.PZXT:
										case BlockType.Text_Description:
											break;
										default:
											okToSkipPause = false;
											break;
									}

									if (!okToSkipPause)
										break;
								}

								if (okToSkipPause)
								{
									SkipBlock(true);
								}
							}
							break;
					}

					// notify about the current block
					var bl = DataBlocks[_currentDataBlockIndex];

					var sbd = new StringBuilder();
					sbd.Append('(');
					sbd.Append((_currentDataBlockIndex + 1) + " of " + DataBlocks.Count);
					sbd.Append(") : ");
					//sbd.Append("ID" + bl.BlockID.ToString("X2") + " - ");
					sbd.Append(bl.BlockDescription);
					if (bl.MetaData.Count > 0)
					{
						sbd.Append(" - ");
						sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
					}
					_machine.CPC.OSD_TapePlayingBlockInfo(sbd.ToString());
				}


				// increment the current period position
				_position++;

				if (_position >= DataBlocks[_currentDataBlockIndex].DataPeriods.Count)
				{
					// we have reached the end of the current block

					if (DataBlocks[_currentDataBlockIndex].DataPeriods.Count == 0)
					{
						// notify about the current block (we are skipping it because its empty)
						var bl = DataBlocks[_currentDataBlockIndex];
						var sbd = new StringBuilder();
						sbd.Append('(');
						sbd.Append((_currentDataBlockIndex + 1) + " of " + DataBlocks.Count);
						sbd.Append(") : ");
						//sbd.Append("ID" + bl.BlockID.ToString("X2") + " - ");
						sbd.Append(bl.BlockDescription);
						if (bl.MetaData.Count > 0)
						{
							sbd.Append(" - ");
							sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
						}
						_machine.CPC.OSD_TapePlayingSkipBlockInfo(sbd.ToString());

					}

					// skip any empty blocks (and process any command blocks)
					while (_position >= DataBlocks[_currentDataBlockIndex].DataPeriods.Count)
					{
						// check for any commands
						var command = DataBlocks[_currentDataBlockIndex].Command;
						var block = DataBlocks[_currentDataBlockIndex];
						bool shouldStop = false;
						switch (command)
						{
							case TapeCommand.STOP_THE_TAPE:
							case TapeCommand.STOP_THE_TAPE_48K:
								throw new Exception("spectrum tape command found in CPC tape");

							/*
                            // Stop the tape command found - if this is the end of the tape RTZ
                            // otherwise just STOP and move to the next block
                            case TapeCommand.STOP_THE_TAPE:

                                _machine.CPC.OSD_TapeStoppedAuto();
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
                                    _machine.CPC.OSD_TapeStoppedAuto();
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
                                */
							default:
								break;
						}

						if (shouldStop)
							break;

						_position = 0;
						_currentDataBlockIndex++;

						if (_currentDataBlockIndex >= DataBlocks.Count)
						{
							break;
						}
					}

					// check for end of tape
					if (_currentDataBlockIndex >= DataBlocks.Count)
					{
						_currentDataBlockIndex = -1;
						RTZ();
						return currentState;
					}
				}

				// update waitEdge with current position within the current block
				_waitEdge = DataBlocks[_currentDataBlockIndex].DataPeriods[_position];

				// flip the current state
				FlipTapeState();

			}

			// update lastCycle and return currentstate
			_lastCycle = cpuCycle - cycles;

			// play the buzzer
			//_buzzer.ProcessPulseValue(false, currentState);

			return currentState;
		}

		private void FlipTapeState()
		{
			currentState = !currentState;
		}


		public void AutoStopTape()
		{
			if (!_tapeIsPlaying)
				return;

			if (!_autoPlay)
				return;

			Stop();
			_machine.CPC.OSD_TapeStoppedAuto();
		}

		public void AutoStartTape()
		{
			if (_tapeIsPlaying)
				return;

			if (!_autoPlay)
				return;

			Play();
			_machine.CPC.OSD_TapePlayingAuto();
		}

		/*
        public int MaskableInterruptCount = 0;

        private void MonitorFrame()
        {
            if (_tapeIsPlaying && _autoPlay)
            {
                if (DataBlocks.Count > 1 || 
                    (_dataBlocks[_currentDataBlockIndex].BlockDescription != BlockType.CSW_Recording &&
                    _dataBlocks[_currentDataBlockIndex].BlockDescription != BlockType.WAV_Recording))
                {
                    // we should only stop the tape when there are multiple blocks
                    // if we just have one big block (maybe a CSW or WAV) then auto stopping will cock things up
                    _monitorTimeOut--;
                }

                if (_monitorTimeOut < 0)
                {
                    if (_dataBlocks[_currentDataBlockIndex].BlockDescription != BlockType.PAUSE_BLOCK &&
                        _dataBlocks[_currentDataBlockIndex].BlockDescription != BlockType.PAUS)
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
                int timeout = ((_machine.GateArray.FrameLength * 50) / 1000) * blockPause;

                // don't use autostop detection if block has no pause at the end
                if (timeout == 0)
                    return;

                // don't autostop if there is only 1 block
                if (DataBlocks.Count == 1 || _dataBlocks[_currentDataBlockIndex].BlockDescription == BlockType.CSW_Recording ||
                    _dataBlocks[_currentDataBlockIndex].BlockDescription == BlockType.WAV_Recording
                    )
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
        */

		/// <summary>
		/// Mask constants
		/// </summary>
		private const int TAPE_BIT = 0x40;
		private const int EAR_BIT = 0x10;
		private const int MIC_BIT = 0x08;

		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public bool ReadPort()
		{
			if (TapeIsPlaying)
			{
				GetEarBit(CPU.TotalExecutedCycles);
			}
			/*
            if (currentState)
            {
                result |= TAPE_BIT;
            }
            else
            {
                result &= ~TAPE_BIT;
            }
            */

			if (!TapeIsPlaying)
			{
				//if (_machine.UPDDiskDevice == null || !_machine.UPDDiskDevice.FDD_IsDiskLoaded)
				//MonitorRead();
			}
			//if (_machine.UPDDiskDevice == null || !_machine.UPDDiskDevice.FDD_IsDiskLoaded)
			//MonitorRead();

			return true;
		}

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		public void WritePort(bool state)
		{
			// not implemented

			/*
            if (!TapeIsPlaying)
            {
                currentState = ((byte)result & 0x10) != 0;
            }
            */
		}

		/// <summary>
		/// BizHawk state serialization
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
			ser.Sync(nameof(tapeMotor), ref tapeMotor);
			ser.EndSection();
		}
	}
}
