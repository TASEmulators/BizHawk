using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Consoles.Atari;

namespace BizHawk
{
	partial class Atari2600
	{
		class mDPC : MapperBase
		{
			int bank_4k = 0;
			IntBuffer Counters = new IntBuffer(8);
			ByteBuffer Flags = new ByteBuffer(8);
			IntBuffer Tops = new IntBuffer(8);
			IntBuffer Bottoms = new IntBuffer(8);
			ByteBuffer DisplayBank_2k = new ByteBuffer(2048);
			byte RandomNumber = 0;

			bool[] MusicMode = new bool[3]; //TOOD: savestates

			public override byte PeekMemory(ushort addr)
			{
				return base.PeekMemory(addr); //TODO
			}

			public override byte ReadMemory(ushort addr)
			{
				if (addr < 0x1000)
				{
					if (addr < 0x0040)
					{
						byte result = 0;
						int index = addr & 0x07;
						int function = (addr >> 3) & 0x07;

						// Update flag register for selected data fetcher
						if ((Counters[index] & 0x00ff) == Tops[index])
						{
							Flags[index] = 0xff;
						}
						else if ((Counters[index] & 0x00ff) == Bottoms[index])
						{
							Flags[index] = 0x00;
						}

						switch (function)
						{
							default:
								result = 0;
								break;
							case 0x00:
								if (index < 4)
								{
									result = RandomNumber;
								}
								else //it's a music read
								{
									byte[] MusicAmplitudes = {
										0x00, 0x04, 0x05, 0x09, 0x06, 0x0a, 0x0b, 0x0f
									};

									//// Update the music data fetchers (counter & flag)
									UpdateMusicModeDataFetchers();

									byte i = 0;
									if(MusicMode[0] && Flags[5] > 0)
									{
										i |= 0x01;
									}
									if(MusicMode[1] && Flags[6] > 0)
									{
										i |= 0x02;
									}
									if(MusicMode[2] && Flags[7] > 0)
									{
										i |= 0x04;
									}

									result = MusicAmplitudes[i];
								}
								break;
							case 0x01:
								result = DisplayBank_2k[2047 - Counters[index]];
								break;
							case 0x02:
								result = DisplayBank_2k[2047 - (Counters[index] & Flags[index])];
								break;
							case 0x07:
								result = Flags[index];
								break;
						}

						// Clock the selected data fetcher's counter if needed
						if ((index < 5) || ((index >= 5) && (!MusicMode[index - 5])))
						{
							Counters[index] = (Counters[index] - 1) & 0x07ff;
						}

						return result;
					}
					else
					{
						return base.ReadMemory(addr);
					}
				}
				else
				{
					Address(addr);
					return core.rom[(bank_4k << 12)+ (addr & 0xFFF)];
				}
			}

			public override void WriteMemory(ushort addr, byte value)
			{
				if (addr < 0x1000)
				{
					base.WriteMemory(addr, value);
				}
				addr &= 0x0FFF;

				// Clock the random number generator.  This should be done for every
				// cartridge access, however, we're only doing it for the DPC and 
				// hot-spot accesses to save time.
				ClockRandomNumberGenerator();

				if ((addr >= 0x0040) && (addr < 0x0080))
				{
					// Get the index of the data fetcher that's being accessed
					int index = addr & 0x07;
					int function = (addr >> 3) & 0x07;

					switch (function)
					{
						case 0x00: // DFx top count
							Tops[index] = value;
							Flags[index] = 0x00;
							break;
						case 0x01: // DFx bottom count
							Bottoms[index] = value;
							break;
						case 0x02: // DFx counter low
							if ((index >= 5) && MusicMode[index - 5])
							{
								Counters[index] = (Counters[index] & 0x0700) | Tops[index]; // Data fetcher is in music mode so its low counter value should be loaded from the top register not the poked value
							}
							else
							{
								// Data fetcher is either not a music mode data fetcher or it
								// isn't in music mode so it's low counter value should be loaded
								// with the poked value
								Counters[index] = (Counters[index] & 0x0700) | value;
							}
							break;
						case 0x03: // DFx counter high
							Counters[index] = ((value & 0x07) << 8) | (Counters[index] & 0x00ff);

							// Execute special code for music mode data fetchers
							if (index >= 5)
							{
								MusicMode[index - 5] = (value & 0x10) > 0 ? true : false;

								// NOTE: We are not handling the clock source input for
								// the music mode data fetchers.  We're going to assume
								// they always use the OSC input.
							}
							break;
						case 0x06: // Random Number Generator Reset
								RandomNumber = 1;
								break;
						default:
							break;
					}
				}
				else
				{
					Address(addr);
				}
				return;
			}

			private void Address(ushort addr)
			{
				if (addr == 0x1FF8)
				{
					bank_4k = 0;
				}
				else if (addr == 0x1FF9)
				{
					bank_4k = 1;
				}
			}

			public override void Dispose()
			{
				DisplayBank_2k.Dispose();
				Counters.Dispose();
				Flags.Dispose();
				base.Dispose();
			}

			public override void SyncState(Serializer ser)
			{
				//TODO
				base.SyncState(ser);
				ser.Sync("bank_4k", ref bank_4k);
				ser.Sync("DisplayBank_2k", ref DisplayBank_2k);
				ser.Sync("Flags", ref Flags);
				ser.Sync("Counters", ref Counters);
				ser.Sync("RandomNumber", ref RandomNumber);
			}

			private double FractionalClocks;
			private void UpdateMusicModeDataFetchers()
			{
				// Calculate the number of cycles since the last update
				//int cycles = mySystem->cycles() - mySystemCycles;
				//mySystemCycles = mySystem->cycles();
				int cycles = 0; //TODO: need to get cycles!
				// Calculate the number of DPC OSC clocks since the last update
				double clocks = ((20000.0 * cycles) / 1193191.66666667) + FractionalClocks;
				int wholeClocks = (int)clocks;
				FractionalClocks = clocks - (double)wholeClocks;

				if (wholeClocks <= 0)
				{
					return;
				}

				// Let's update counters and flags of the music mode data fetchers
				for (int x = 5; x <= 7; ++x)
				{
					// Update only if the data fetcher is in music mode
					if (MusicMode[x - 5])
					{
						int top = Tops[x] + 1;
						int newLow = Counters[x] & 0x00ff;

						if (Tops[x] != 0)
						{
							newLow -= (wholeClocks % top);
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
						if (newLow <= Bottoms[x])
						{
							Flags[x] = 0x00;
						}
						else if (newLow <= Tops[x])
						{
							Flags[x] = 0xff;
						}

						Counters[x] = (Counters[x] & 0x0700) | newLow;
					}
				}
			}

			private void ClockRandomNumberGenerator()
			{
				// Table for computing the input bit of the random number generator's
				// shift register (it's the NOT of the EOR of four bits)
				byte[] f = {
					1, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1
				};

				// Using bits 7, 5, 4, & 3 of the shift register compute the input
				// bit for the shift register
				byte bit = f[((RandomNumber >> 3) & 0x07) |
					((RandomNumber & 0x80) > 0 ? 0x08 : 0x00)];

				// Update the shift register 
				RandomNumber = (byte)(RandomNumber << 1 | bit);
			}
		}
	}
}
