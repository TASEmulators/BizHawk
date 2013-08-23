using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridge
{
	public class Mapper0000 : Cart
	{
		private byte[] romA;
		private int romAMask;
		private byte[] romB;
		private int romBMask;

		// standard cartridge mapper (Commodore)
		// note that this format also covers Ultimax carts

		public Mapper0000(List<int> newAddresses, List<int> newBanks, List<byte[]> newData, bool game, bool exrom)
		{
			pinGame = game;
			pinExRom = exrom;

			validCartridge = true;
			
			// default to empty banks
			romA = new byte[1];
			romB = new byte[1];
			romA[0] = 0xFF;
			romB[0] = 0xFF;

			for (int i = 0; i < newAddresses.Count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					switch (newData[i].Length)
					{
						case 0x1000: 
							romAMask = 0x0FFF; 
							romA = newData[i];
							break;
						case 0x2000: 
							romAMask = 0x1FFF; 
							romA = newData[i];
							break;
						case 0x4000: 
							romAMask = 0x1FFF;
							romBMask = 0x1FFF;
							// split the rom into two banks
							romA = new byte[0x2000];
							romB = new byte[0x2000];
							Array.Copy(newData[i], 0x0000, romA, 0x0000, 0x2000);
							Array.Copy(newData[i], 0x2000, romB, 0x0000, 0x2000);
							break;
						default:
							validCartridge = false;
							return;
					}
				}
				else if (newAddresses[i] == 0xA000 || newAddresses[i] == 0xE000)
				{
					switch (newData[i].Length)
					{
						case 0x1000:
							romBMask = 0x0FFF;
							break;
						case 0x2000:
							romBMask = 0x1FFF;
							break;
						default:
							validCartridge = false;
							return;
					}
					romB = newData[i];
				}
			}
		}

		public override byte Peek8000(int addr)
		{
			return romA[addr & romAMask];
		}

		public override byte PeekA000(int addr)
		{
			return romB[addr & romBMask];
		}

		public override byte Read8000(int addr)
		{
			return romA[addr & romAMask];
		}

		public override byte ReadA000(int addr)
		{
			return romB[addr & romBMask];
		}
	}
}
