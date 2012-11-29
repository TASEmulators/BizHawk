using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridges
{
	public class Mapper0000 : Cartridge
	{
		private byte[] romA;
		private byte[] romB;

		// standard cartridge mapper (Commodore)
		// note that this format also covers Ultimax carts

		public Mapper0000(List<uint> newAddresses, List<uint> newBanks, List<byte[]> newData, bool game, bool exrom)
		{
			pinGame = game;
			pinExRom = exrom;

			romA = new byte[0x2000];
			romB = new byte[0x2000];
			validCartridge = true;

			for (int i = 0; i < newAddresses.Count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					if (newData[i].Length < 0x2000)
					{
						Array.Copy(newData[i], 0x0000, romA, 0x0000, 0x1000);
						Array.Copy(newData[i], 0x0000, romA, 0x1000, 0x1000);
					}
					else if (newData[i].Length < 0x4000)
					{
						romA = newData[i];
					}
					else
					{
						Array.Copy(newData[i], 0x0000, romA, 0x0000, 0x2000);
						Array.Copy(newData[i], 0x2000, romB, 0x0000, 0x2000);
					}
				}
				else if (newAddresses[i] == 0xA000 || newAddresses[i] == 0xE000)
				{
					if (newData[i].Length < 0x2000)
					{
						Array.Copy(newData[i], 0x0000, romB, 0x0000, 0x1000);
						Array.Copy(newData[i], 0x0000, romB, 0x1000, 0x1000);
					}
					else
					{
						romB = newData[i];
					}
				}
			}

			HardReset();
		}

		public override byte Peek8000(int addr)
		{
			return romA[addr];
		}

		public override byte PeekA000(int addr)
		{
			return romB[addr];
		}

		public override byte Read8000(ushort addr)
		{
			return romA[addr];
		}

		public override byte ReadA000(ushort addr)
		{
			return romB[addr];
		}
	}
}
