using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	Cartridge class used for DPC+.  There are six 4K program banks, a 4K
	display bank, 1K frequency table and the DPC chip.  For complete details on
	the DPC chip see David P. Crane's United States Patent Number 4,644,495.
	*/
	internal sealed class mDPCPlus : MapperBase
	{
		// Table for computing the input bit of the random number generator's
		// shift register (it's the NOT of the EOR of four bits)
		private readonly byte[] _randomInputBits = { 1, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1 };

		private int[] _counters = new int[8];
		private byte[] _tops = new byte[8];
		private byte[] _flags = new byte[8];
		private byte[] _bottoms = new byte[8];
		private bool[] _musicModes = new bool[3];

		private int _bank4K;
		private byte _currentRandomVal;
		private int _elapsedCycles = 85; // 85 compensates for a slight timing issue when ClockCpu is first run, 85 puts BizHawk back on track with Stella on elapsed timing values
		private float _fractionalClocks; // Fractional DPC music OSC clocks unused during the last update

		private byte[] _dspData;

		// TODO: PokeMem, and everything else
		public mDPCPlus(Atari2600 core) : base(core)
		{
			throw new NotImplementedException();
		}

		public byte[] DspData => _dspData ??= Core.Rom.Skip(8192).Take(2048).ToArray();

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);

			ser.Sync("counters", ref _counters, false);
			ser.Sync("tops", ref _tops, false);
			ser.Sync("flags", ref _flags, false);
			ser.Sync("bottoms", ref _bottoms, false);
			ser.Sync("musicMode0", ref _musicModes[0]); // Silly, but I didn't want to support bool[] in Serializer just for this one variable
			ser.Sync("musicMode1", ref _musicModes[1]);
			ser.Sync("musicMode2", ref _musicModes[2]);

			ser.Sync("bank_4k", ref _bank4K);
			ser.Sync("currentRandomVal", ref _currentRandomVal);
			ser.Sync("elapsedCycles", ref _elapsedCycles);
			ser.Sync("fractionalClocks", ref _fractionalClocks);
		}

		public override void HardReset()
		{
			_counters = new int[8];
			_tops = new byte[8];
			_flags = new byte[8];
			_bottoms = new byte[8];
			_musicModes = new bool[3];
			_bank4K = 0;
			_currentRandomVal = 0;
			_elapsedCycles = 85;
			_fractionalClocks = 0;
		}

		public override void ClockCpu()
		{
			_elapsedCycles++;
		}

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
				return;
			}

			Address(addr);
			ClockRandomNumberGenerator();

			if (addr >= 0x1040 && addr < 0x1080)
			{
				var index = addr & 0x07;
				var function = (addr >> 3) & 0x07;

				switch (function)
				{
					// DFx top count
					case 0x00:
						_tops[index] = value;
						_flags[index] = 0x00;
						break;

					// DFx bottom count
					case 0x01:
						_bottoms[index] = value;
						break;

					// DFx counter low
					case 0x02:
						if (index >= 5 && _musicModes[index - 5])
						{
							// Data fetcher is in music mode so its low counter value
							// should be loaded from the top register not the poked value
							_counters[index] = (_counters[index] & 0x0700) |
								_tops[index];
						}
						else
						{
							// Data fetcher is either not a music mode data fetcher or it
							// isn't in music mode so it's low counter value should be loaded
							// with the poked value
							_counters[index] = (_counters[index] & 0x0700) | value;
						}

						break;

					// DFx counter high
					case 0x03:
						_counters[index] = (ushort)(((value & 0x07) << 8)
							| (_counters[index] & 0x00ff));

						// Execute special code for music mode data fetchers
						if (index >= 5)
						{
							_musicModes[index - 5] = (value & 0x10) > 0;

							// NOTE: We are not handling the clock source input for
							// the music mode data fetchers.  We're going to assume
							// they always use the OSC input.
						}

						break;

					// Random Number Generator Reset
					case 0x06:
						_currentRandomVal = 1;
						break;
				}
			}
		}

		private byte ReadMem(ushort addr, bool peek)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			if (!peek)
			{
				Address(addr);
				ClockRandomNumberGenerator();
			}

			if (addr < 0x1040)
			{
				byte result;

				// Get the index of the data fetcher that's being accessed
				var index = addr & 0x07;
				var function = (addr >> 3) & 0x07;

				// Update flag register for selected data fetcher
				if ((_counters[index] & 0x00ff) == _tops[index])
				{
					_flags[index] = 0xff;
				}
				else if ((_counters[index] & 0x00ff) == _bottoms[index])
				{
					_flags[index] = 0x00;
				}

				switch (function)
				{
					case 0x00:
						if (index < 4)
						{
							result = _currentRandomVal;
						}
						else // No, it's a music read
						{
							var musicAmplitudes = new byte[] {
							  0x00, 0x04, 0x05, 0x09, 0x06, 0x0a, 0x0b, 0x0f
							};

							// Update the music data fetchers (counter & flag)
							UpdateMusicModeDataFetchers();

							byte i = 0;
							if (_musicModes[0] && _flags[5] > 0)
							{
								i |= 0x01;
							}

							if (_musicModes[1] && _flags[6] > 0)
							{
								i |= 0x02;
							}

							if (_musicModes[2] && _flags[7] > 0)
							{
								i |= 0x04;
							}

							result = musicAmplitudes[i];
						}

						break;

					// DFx display data read
					case 0x01:
						result = DspData[2047 - _counters[index]];
						break;

					// DFx display data read AND'd w/flag
					case 0x02:
						result = (byte)(DspData[2047 - _counters[index]] & _flags[index]);
						break;

					// DFx flag
					case 0x07:
						result = _flags[index];
						break;

					default:
						result = 0;
						break;
				}

				// Clock the selected data fetcher's counter if needed
				if ((index < 5) || ((index >= 5) && (!_musicModes[index - 5])))
				{
					_counters[index] = (_counters[index] - 1) & 0x07ff;
				}

				return result;
			}

			return Core.Rom[(_bank4K << 12) + (addr & 0xFFF)];
		}

		private void Address(ushort addr)
		{
			if (addr == 0x1FF6)
			{
				_bank4K = 0;
			}
			else if (addr == 0x1FF7)
			{
				_bank4K = 1;
			}
			else if (addr == 0x1FF8)
			{
				_bank4K = 2;
			}
			else if (addr == 0x1FF9)
			{
				_bank4K = 3;
			}
			else if (addr == 0x1FFA)
			{
				_bank4K = 4;
			}
			else if (addr == 0x1FFB)
			{
				_bank4K = 5;
			}
		}

		private void ClockRandomNumberGenerator()
		{
			// Using bits 7, 5, 4, & 3 of the shift register compute the input
			// bit for the shift register
			var bit = _randomInputBits[((_currentRandomVal >> 3) & 0x07) |
				(((_currentRandomVal & 0x80) > 0) ? 0x08 : 0x00)];

			// Update the shift register 
			_currentRandomVal = (byte)((_currentRandomVal << 1) | bit);
		}

		private void UpdateMusicModeDataFetchers()
		{
			// Calculate the number of cycles since the last update
			var cycles = _elapsedCycles;
			_elapsedCycles = 0;

			// Calculate the number of DPC OSC clocks since the last update
			var clocks = ((20000.0 * cycles) / 1193191.66666667) + _fractionalClocks;
			var wholeClocks = (int)clocks;
			_fractionalClocks = (float)(clocks - wholeClocks);

			if (wholeClocks <= 0)
			{
				return;
			}

			// Let's update counters and flags of the music mode data fetchers
			for (var x = 5; x <= 7; ++x)
			{
				// Update only if the data fetcher is in music mode
				if (_musicModes[x - 5])
				{
					var top = _tops[x] + 1;
					var newLow = _counters[x] & 0x00ff;

					if (_tops[x] != 0)
					{
						newLow -= wholeClocks % top;
						if (newLow < 0)
						{
							newLow += top;
						}
					}
					else
					{
						newLow = 0;
					}

					// Update flag register for this data fetcher
					if (newLow <= _bottoms[x])
					{
						_flags[x] = 0x00;
					}
					else if (newLow <= _tops[x])
					{
						_flags[x] = 0xff;
					}

					_counters[x] = (_counters[x] & 0x0700) | (ushort)newLow;
				}
			}
		}
	}
}
