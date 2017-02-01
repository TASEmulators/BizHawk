using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class Cartridge : ICart
	{
		private ushort[] Data = new ushort[56320];

		private ushort[] Cart_Ram = new ushort[0x800];

		// There are 10 mappers Intellivision games use (not counting intellicart which is handled seperately)
		// we will pick the mapper from the game DB and default to 0
		private int mapper = 0; 

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Cart");

			ser.Sync("mapper", ref mapper);
			ser.Sync("Cart_Ram", ref Cart_Ram, false);

			ser.EndSection();
		}

		public int Parse(byte[] Rom)
		{
			// Combine every two bytes into a word.
			int index = 0;

			while (index + 1 < Rom.Length)
			{
				Data[(index / 2)] = (ushort)((Rom[index++] << 8) | Rom[index++]);
			}

			// look up hash in gamedb to see what mapper to use
			// if none found default is zero
			string hash_sha1 = null;
			string s_mapper = null;
			hash_sha1 = "sha1:" + Rom.HashSHA1(16, Rom.Length - 16);

			var gi = Database.CheckDatabase(hash_sha1);
			if (gi != null)
			{
				var dict = gi.GetOptionsDict();
				if (!dict.ContainsKey("board"))
					throw new Exception("INTV gamedb entries must have a board identifier!");
				s_mapper = dict["board"];
			}
			else
			{
				s_mapper = "0";
			}

			int.TryParse(s_mapper, out mapper);

			return Rom.Length;
		}

		public ushort? ReadCart(ushort addr)
		{
			switch (mapper)
			{
				case 0:
					if (addr>=0x5000 && addr<=0x6FFF)
					{
						return Data[addr-0x5000];
					}
					else if (addr>=0xD000 && addr<=0xDFFF)
					{
						return Data[addr - 0xB000];
					}
					else if (addr>=0xF000 && addr<=0xFFFF)
					{
						return Data[addr - 0xC000];
					}
					break;

				case 1:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0xD000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xB000];
					}
					break;

				case 2:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr <= 0xBFFF)
					{
						return Data[addr - 0x7000];
					}
					else if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0x8000];
					}
					break;

				case 3:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr <= 0xAFFF)
					{
						return Data[addr - 0x7000];
					}
					else if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0x9000];
					}
					else if (addr >= 0xF000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xA000];
					}
					break;

				case 4:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0xD000 && addr <= 0xD3FF)
					{
						return Cart_Ram[addr - 0xD000];
					}
					break;

				case 5:
					if (addr >= 0x5000 && addr <= 0x7FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr <= 0xBFFF)
					{
						return Data[addr - 0x6000];
					}
					break;

				case 6:
					if (addr >= 0x6000 && addr <= 0x7FFF)
					{
						return Data[addr - 0x6000];
					}
					break;

				case 7:
					if (addr >= 0x4800 && addr <= 0x67FF)
					{
						return Data[addr - 0x4800];
					}
					break;

				case 8:
					if (addr >= 0x5000 && addr <= 0x5FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x7000 && addr <= 0x7FFF)
					{
						return Data[addr - 0x6000];
					}
					break;

				case 9:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr <= 0xAFFF)
					{
						return Data[addr - 0x7000];
					}
					else if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0x9000];
					}
					else if (addr >= 0xF000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xA000];
					}
					else if (addr >= 0x8800 && addr <= 0x8FFF)
					{
						return Cart_Ram[addr - 0x8800];
					}
					break;

				case 10:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x8800 && addr <= 0xB7FF)
					{
						return Data[addr - 0x6800];
					}
					else if (addr >= 0xD000 && addr <= 0xFFFF)
					{
						return Data[addr - 0x8000];
					}
					break;
			}


			return null;
		}

		public bool WriteCart(ushort addr, ushort value)
		{
			switch (mapper)
			{
				case 4:
					if (addr >= 0xD000 && addr <= 0xD3FF)
					{
						Cart_Ram[addr - 0xD000] = value;
						return true;
					}
					break;
				case 9:
					if (addr >= 0x8800 && addr <= 0x8FFF)
					{
						Cart_Ram[addr - 0x8800] = value;
						return true;
					}
					break;
			}
			return false;
		}
	}
}
