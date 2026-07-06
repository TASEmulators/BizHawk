using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Tapes
{
	/// <summary>
	/// <para>
	/// A shared, core-agnostic tape player (datacorder). Holds the loaded tape as a list of
	/// <see cref="TapeDataBlock"/>, drives transport (play / stop / rewind / block skip), and generates the
	/// EAR signal by comparing pulse periods against the host CPU's cycle counter.
	/// </para>
	/// <para>
	/// Tape formats store pulse timings in a 3.5MHz T-state reference. cyclesPerTapeTState scales those into
	/// the host CPU's clock domain: 1.0 for a 3.5MHz machine (a plain move, no change), a larger ratio for a
	/// faster clock (e.g. the Amstrad CPC at 4.0/3.5). At exactly 1.0 no scaling is applied, so behaviour is
	/// bit-for-bit identical to comparing raw periods to cycles.
	/// </para>
	/// <para>
	/// Everything core-specific (auto-detect of tape reads, trap/fast loading, port I/O, a motor/remote line)
	/// lives in the owning core behind <see cref="ITapeHost"/>; this class references no CPU or machine type.
	/// </para>
	/// </summary>
	public sealed class TapeDeck
	{
		private readonly ITapeHost _host;

		/// <summary>
		/// Host CPU cycles per 3.5MHz tape T-state. 1.0 leaves periods untouched (a 3.5MHz host); other values
		/// scale the format's 3.5MHz-referenced timings into the host clock domain.
		/// </summary>
		private readonly double _cyclesPerTapeTState;

		public TapeDeck(ITapeHost host, double cyclesPerTapeTState)
		{
			_host = host;
			_cyclesPerTapeTState = cyclesPerTapeTState;
		}

		/// <summary>
		/// Scales a raw (3.5MHz-referenced) pulse period into host CPU cycles. A ratio of exactly 1.0 returns
		/// the period unchanged, keeping a 3.5MHz host bit-identical to the pre-scaling behaviour.
		/// </summary>
		private int ScalePeriod(int period)
			=> _cyclesPerTapeTState == 1.0 ? period : (int)(period * _cyclesPerTapeTState);

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
		/// The current EAR level presented by the tape (read by the host's tape input port).
		/// </summary>
		public bool CurrentEarLevel
		{
			get => currentState;
			set => currentState = value;
		}

		/// <summary>
		/// Builds the human-readable "(n of m) : description - key: value" string for a block, used in the
		/// tape notifications.
		/// </summary>
		private string BlockInfo(int index)
		{
			var bl = _dataBlocks[index];
			StringBuilder sbd = new StringBuilder();
			sbd.Append('(');
			sbd.Append((index + 1) + " of " + _dataBlocks.Count);
			sbd.Append(") : ");
			sbd.Append(bl.BlockDescription);
			if (bl.MetaData.Count > 0)
			{
				sbd.Append(" - ");
				sbd.Append(bl.MetaData.First().Key + ": " + bl.MetaData.First().Value);
			}
			return sbd.ToString();
		}

		/// <summary>
		/// Starts the tape playing from the beginning of the current block
		/// </summary>
		public void Play()
		{
			if (_tapeIsPlaying)
				return;

			_host.NotifyPlay();

			// update the lastCycle
			_lastCycle = _host.TotalExecutedCycles;

			// reset waitEdge and position
			_waitEdge = 0;
			_position = 0;

			if (_dataBlocks.Count > 0 && _currentDataBlockIndex >= 0)
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
					// end of tape reached. Rewind to beginning. (The original also called AutoStopTape here,
					// but the tape is not yet playing at this point so that was a no-op.)
					RTZ();
					return;
				}

				// update waitEdge with the current position in the current block
				_waitEdge = ScalePeriod(_dataBlocks[_currentDataBlockIndex].DataPeriods[_position]);

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

			_host.NotifyStop();

			// sign that the tape is no longer playing
			_tapeIsPlaying = false;

			if (_currentDataBlockIndex >= 0
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

				if (_currentDataBlockIndex < 0 && _dataBlocks.Count > 0)
				{
					// move the index on to 0
					_currentDataBlockIndex = 0;
				}
			}

			// update the lastCycle
			_lastCycle = _host.TotalExecutedCycles;
		}

		/// <summary>
		/// Rewinds the tape to it's beginning (return to zero)
		/// </summary>
		public void RTZ()
		{
			Stop();
			_host.NotifyRewind();
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

			string info = BlockInfo(targetBlockId);

			if (skipForward)
				_host.NotifyNextBlock(info);
			else
				_host.NotifyPrevBlock(info);

			CurrentDataBlockIndex = targetBlockId;
		}

		/// <summary>
		/// Resets the tape
		/// </summary>
		public void Reset()
		{
			RTZ();
		}

		/// <summary>
		/// Is called every cpu cycle but runs every so many t-states.
		/// This enables the tape device to play out even if the host is not requesting tape data.
		/// </summary>
		public void TapeCycle()
		{
			if (_tapeIsPlaying)
			{
				counter++;

				if (counter > 20)
				{
					counter = 0;
					bool state = GetEarBit(_host.TotalExecutedCycles);
					_host.FeedBeeper(state);
				}
			}
		}

		/// <summary>
		/// Simulates the 'EAR' input reading data from the tape
		/// </summary>
		public bool GetEarBit(long cpuCycle)
		{
			// decide how many cycles worth of data we are capturing
			long cycles = cpuCycle - _lastCycle;

			bool is48k = _host.IsIn48kMode;

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
					_host.NotifyPlayingBlock(BlockInfo(_currentDataBlockIndex));
				}

				// increment the current period position
				_position++;

				if (_position >= _dataBlocks[_currentDataBlockIndex].DataPeriods.Count)
				{
					// we have reached the end of the current block
					if (_dataBlocks[_currentDataBlockIndex].DataPeriods.Count == 0)
					{
						// notify about the current block (we are skipping it because its empty)
						_host.NotifySkipBlock(BlockInfo(_currentDataBlockIndex));
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

								_host.NotifyStoppedAuto();
								shouldStop = true;

								if (_currentDataBlockIndex >= _dataBlocks.Count)
									RTZ();
								else
								{
									Stop();
								}

								_host.NotifyStopCommand();
								break;
							case TapeCommand.STOP_THE_TAPE_48K:
								if (is48k)
								{
									_host.NotifyStoppedAuto();
									shouldStop = true;

									if (_currentDataBlockIndex >= _dataBlocks.Count)
										RTZ();
									else
									{
										Stop();
									}

									_host.NotifyStopCommand();
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
				_waitEdge = _dataBlocks[_currentDataBlockIndex].DataPeriods.Count > 0
					? ScalePeriod(_dataBlocks[_currentDataBlockIndex].DataPeriods[_position]) : 0;

				// set the current state
				currentState = _dataBlocks[_currentDataBlockIndex].DataLevels[_position];
			}

			// update lastCycle and return currentstate
			_lastCycle = cpuCycle - cycles;

			return currentState;
		}

		/// <summary>
		/// Resets the deck's activity timestamp to the host's current cycle (used by a host that auto-stops
		/// the tape when the CPU has not read from it for a while).
		/// </summary>
		public void ResetLastCycleToNow()
		{
			_lastCycle = _host.TotalExecutedCycles;
		}

		/// <summary>
		/// Bizhawk state serialization for the transport/signal state. Field names and order match the pre-
		/// extraction datacorder so existing savestate layout is preserved (the owning device serializes its
		/// own auto-detect fields immediately after, within the same section).
		/// </summary>
		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(counter), ref counter);
			ser.Sync(nameof(_currentDataBlockIndex), ref _currentDataBlockIndex);
			ser.Sync(nameof(_position), ref _position);
			ser.Sync(nameof(_tapeIsPlaying), ref _tapeIsPlaying);
			ser.Sync(nameof(_lastCycle), ref _lastCycle);
			ser.Sync(nameof(_waitEdge), ref _waitEdge);
			ser.Sync(nameof(currentState), ref currentState);
		}
	}
}
