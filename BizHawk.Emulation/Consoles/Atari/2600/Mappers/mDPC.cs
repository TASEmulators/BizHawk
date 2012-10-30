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
			IntBuffer Flags = new IntBuffer(8);
			IntBuffer Tops = new IntBuffer(8);
			IntBuffer Bottoms = new IntBuffer(8);
			ByteBuffer DisplayBank_2k = new ByteBuffer(2048);
			byte RandomNumber = 0;

			bool[] MusicMode = new bool[3]; //TOOD: savestates

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
								if (index < 4)
								{
									result = RandomNumber;
								}
								else
								{
									result = 0;//TODO
								

									//byte[8] musicAmplitudes = {
									//    0x00, 0x04, 0x05, 0x09, 0x06, 0x0a, 0x0b, 0x0f
									//};

									//// Update the music data fetchers (counter & flag)
									//UpdateMusicModeDataFetchers();

									//byte i = 0;
									//if(MusicMode[0] && Flags[5])
									//{
									//    i |= 0x01;
									//}
									//if(MusicMode[1] && Flags[6])
									//{
									//    i |= 0x02;
									//}
									//if(MusicMode[2] && Flags[7])
									//{
									//    i |= 0x04;
									//}

									//result = MusicAmplitudes[i];
								}
								break;
							case 0x00:
								result = 0; //TODO
								break;
							case 0x01:
								result = DisplayBank_2k[2047 - Counters[index]];
								break;
							case 0x02:
								result = DisplayBank_2k[2047 - (Counters[index] & Flags[index])];
								break;
							case 0x07:
								break;
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
				Address(addr);
				if (addr < 0x1000)
				{
					base.WriteMemory(addr, value);
				}
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
				base.SyncState(ser);
				ser.Sync("bank_4k", ref bank_4k);
				ser.Sync("DisplayBank_2k", ref DisplayBank_2k);
				ser.Sync("Flags", ref Flags);
				ser.Sync("Counters", ref Counters);
				ser.Sync("RandomNumber", ref RandomNumber);
			}

			private void UpdateMusicModeDataFetchers()
			{
				//TODO
			}
		}
	}
}
