using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class Cartridge : ICart
	{
		private ushort[] Data = new ushort[56320];

		private ushort[] Cart_Ram = new ushort[0x400];

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
			/*
			// TODO: Determine which loading method, if either, is correct.
			int index = 0;
			// Combine every two bytes into a word.
			while (index + 1 < Rom.Length)
			{
				Data[(index / 2) + 0x2C00] = (ushort)((Rom[index++] << 8) | Rom[index++]);
			}
			/*
			for (int index = 0; index < Rom.Length; index++)
				Data[index + 0x2C00] = Rom[index];
			*/

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
				Console.WriteLine(mapper);
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
					if (addr>=0x5000 && addr<0x6FFF)
					{
						return Data[addr-0x5000];
					}
					else if (addr>=0xD000 && addr<0xDFFF)
					{
						return Data[addr - 0xB000];
					}
					else if (addr>=0xF000 && addr<0xFFFF)
					{
						return Data[addr - 0xC000];
					}
					break;

				case 1:
					if (addr >= 0x5000 && addr < 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0xD000 && addr < 0xFFFF)
					{
						return Data[addr - 0xB000];
					}
					break;

				case 2:
					if (addr >= 0x5000 && addr < 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr < 0xBFFF)
					{
						return Data[addr - 0x7000];
					}
					else if (addr >= 0xD000 && addr < 0xDFFF)
					{
						return Data[addr - 0x8000];
					}
					break;

				case 3:
					if (addr >= 0x5000 && addr < 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr < 0xAFFF)
					{
						return Data[addr - 0x7000];
					}
					else if (addr >= 0xD000 && addr < 0xDFFF)
					{
						return Data[addr - 0x9000];
					}
					else if (addr >= 0xF000 && addr < 0xFFFF)
					{
						return Data[addr - 0xA000];
					}
					break;

				case 4:
					if (addr >= 0x5000 && addr < 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0xD000 && addr < 0xD3FF)
					{
						return Cart_Ram[addr - 0xD000];
					}
					break;

				case 5:
					if (addr >= 0x5000 && addr < 0x7FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr < 0xBFFF)
					{
						return Data[addr - 0x6000];
					}
					break;

				case 6:
					if (addr >= 0x6000 && addr < 0x7FFF)
					{
						return Data[addr - 0x6000];
					}
					break;

				case 7:
					if (addr >= 0x4800 && addr < 0x67FF)
					{
						return Data[addr - 0x4800];
					}
					break;

				case 8:
					if (addr >= 0x5000 && addr < 0x5FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x7000 && addr < 0x7FFF)
					{
						return Data[addr - 0x6000];
					}
					break;

				case 9:
					if (addr >= 0x5000 && addr < 0x6FFF)
					{
						return Data[addr - 0x5000];
					}
					else if (addr >= 0x9000 && addr < 0xAFFF)
					{
						return Data[addr - 0x7000];
					}
					else if (addr >= 0xD000 && addr < 0xDFFF)
					{
						return Data[addr - 0x9000];
					}
					else if (addr >= 0xF000 && addr < 0xFFFF)
					{
						return Data[addr - 0xA000];
					}
					else if (addr >= 0x8800 && addr < 0x8FFF)
					{
						return Cart_Ram[addr - 0x8800];
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
					if (addr >= 0xD000 && addr < 0xD3FF)
					{
						Cart_Ram[addr - 0xD000] = value;
						return true;
					}
					break;
				case 9:
					if (addr >= 0x8800 && addr < 0x8FFF)
					{
						Cart_Ram[addr - 0x8800] = value;
						return true;
					}
					break;
			}
			return false;
		}
		/*
		public ushort? ReadCart(ushort addr)
		{
			// TODO: Check if address is RAM / ROM.
			int dest;
			switch (addr & 0xF000)
			{
				case 0x0000:
					dest = addr - 0x0400;
					if (addr <= 0x03FF)
					{
						break;
					}
					else if (addr <= 0x04FF)
					{
						// OK on all but Intellivision 2.
						return Data[dest];
					}
					else if (addr <= 0x06FF)
					{
						return Data[dest];
					}
					else if (addr <= 0x0CFF)
					{
						// OK if no Intellivoice.
						return Data[dest];
					}
					else
					{
						return Data[dest];
					}
				case 0x2000:
					dest = (addr - 0x2000) + 0x0C00;
					// OK if no ECS.
					return Data[dest];
				case 0x4000:
					dest = (addr - 0x4000) + 0x1C00;
					if (addr <= 0x47FF)
					{
						// OK if no ECS.
						return Data[dest];
					}
					else if (addr == 0x4800)
					{
						// return Data[dest];
						// For now, assume unmapped. TODO: Fix.
						return null;
					}
					else
					{
						return Data[dest];
					}
				case 0x5000:
				case 0x6000:
					dest = (addr - 0x5000) + 0x2C00;
					if (addr <= 0x5014)
					{
						return Data[dest];
					}
					else
					{
						return Data[dest];
					}
				case 0x7000:
					dest = (addr - 0x7000) + 0x4C00;
					if (addr == 0x7000)
					{
						// OK if no ECS.
						// return Data[dest];
						// For now, assume unmapped. TODO: Fix.
						return null;
					}
					else if (addr <= 0x77FF)
					{
						// OK if no ECS.
						return Data[dest];
					}
					else
					{
						// OK if no ECS.
						return Data[dest];
					}
				case 0x8000:
					dest = (addr - 0x8000) + 0x5C00;
					// OK. Avoid STIC alias at $8000-$803F.
					return Data[dest];
				case 0x9000:
				case 0xA000:
				case 0xB000:
					dest = (addr - 0x9000) + 0x6C00;
					if (addr <= 0xB7FF)
					{
						return Data[dest];
					}
					else
					{
						return Data[dest];
					}
				case 0xC000:
					dest = (addr - 0xC000) + 0x9C00;
					// OK. Avoid STIC alias at $C000-$C03F.
					return Data[dest];
				case 0xD000:
					dest = (addr - 0xD000) + 0xAC00;
					return Data[dest];
				case 0xE000:
					dest = (addr - 0xE000) + 0xBC00;
					// OK if no ECS.
					return Data[dest];
				case 0xF000:
					dest = (addr - 0xF000) + 0xCC00;
					if (addr <= 0xF7FF)
					{
						return Data[dest];
					}
					else
					{
						return Data[dest];
					}
			}
			return null;
		}


		public bool WriteCart(ushort addr, ushort value)
		{
			int dest;
			// TODO: Check if address is RAM / ROM.
			switch (addr & 0xF000)
			{
				case 0x0000:
					dest = addr - 0x0400;
					if (addr <= 0x03FF)
					{
						break;
					}
					else if (addr <= 0x04FF)
					{
						// OK on all but Intellivision 2.
						Data[dest] = value;
						return true;
					}
					else if (addr <= 0x06FF)
					{
						Data[dest] = value;
						return true;
					}
					else if (addr <= 0x0CFF)
					{
						// OK if no Intellivoice.
						Data[dest] = value;
						return true;
					}
					else
					{
						Data[dest] = value;
						return true;
					}
				case 0x2000:
					dest = (addr - 0x2000) + 0x0C00;
					// OK if no ECS.
					Data[dest] = value;
					return true;
				case 0x4000:
					dest = (addr - 0x4000) + 0x1C00;
					if (addr <= 0x47FF)
					{
						// OK if no ECS.
						Data[dest] = value;
						return true;
					}
					else if (addr == 0x4800)
					{
						// OK only if boot ROM at $7000.
						Data[dest] = value;
						return true;
					}
					else
					{
						Data[dest] = value;
						return true;
					}
				case 0x5000:
				case 0x6000:
					dest = (addr - 0x5000) + 0x2C00;
					if (addr <= 0x5014)
					{
						// OK only if boot ROM at $4800 or $7000.
						Data[dest] = value;
						return true;
					}
					else
					{
						Data[dest] = value;
						return true;
					}
				case 0x7000:
					dest = (addr - 0x7000) + 0x4C00;
					if (addr == 0x7000)
					{
						// RAM at $7000 confuses EXEC boot sequence.
						Data[dest] = value;
						return true;
					}
					else if (addr <= 0x77FF)
					{
						// OK if no ECS.
						Data[dest] = value;
						return true;
					}
					else
					{
						// Do not map RAM here due to GRAM alias.
						Data[dest] = value;
						return true;
					}
				case 0x8000:
					dest = (addr - 0x8000) + 0x5C00;
					// OK. Avoid STIC alias at $8000-$803F.
					Data[dest] = value;
					return true;
				case 0x9000:
				case 0xA000:
				case 0xB000:
					dest = (addr - 0x9000) + 0x6C00;
					if (addr <= 0xB7FF)
					{
						Data[dest] = value;
						return true;
					}
					else
					{
						// Do not map RAM here due to GRAM alias.
						Data[dest] = value;
						return true;
					}
				case 0xC000:
					dest = (addr - 0xC000) + 0x9C00;
					// OK. Avoid STIC alias at $C000-$C03F.
					Data[dest] = value;
					return true;
				case 0xD000:
					Data[(addr & 0x0FFF) + 0xAC00] = value;
					return true;
				case 0xE000:
					dest = (addr - 0xE000) + 0xBC00;
					// OK if no ECS.
					Data[dest] = value;
					return true;
				case 0xF000:
					dest = (addr - 0xF000) + 0xCC00;
					if (addr <= 0xF7FF)
					{
						Data[dest] = value;
						return true;
					}
					else
					{
						// Do not map RAM here due to GRAM alias.
						Data[dest] = value;
						return true;
					}
			}
			return false;
		}
		*/
	}
}
