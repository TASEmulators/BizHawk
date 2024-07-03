using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed class Cartridge : ICart
	{
		private readonly ushort[] Data = new ushort[56320];

		private ushort[] Cart_Ram = new ushort[0x800];

		// There are several mappers Intellivision games use (not counting intellicart which is handled seperately)
		// we will pick the mapper from the game DB and default to 0
		private int _mapper = 0;

		public string BoardName => $"Mapper {_mapper}";

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Cart");

			ser.Sync("mapper", ref _mapper);
			ser.Sync(nameof(Cart_Ram), ref Cart_Ram, false);

			ser.EndSection();
		}

		public int Parse(byte[] rom)
		{
			// Combine every two bytes into a word.
			int index = 0;

			while (index + 1 < rom.Length)
			{
				Data[(index / 2)] = (ushort)((rom[index++] << 8) | rom[index++]);
			}

			// look up hash in gamedb to see what mapper to use
			string s_mapper = null;

			var gi = Database.CheckDatabase(SHA1Checksum.ComputePrefixedHex(rom));
			if (gi != null && !gi.GetOptions().TryGetValue("board", out s_mapper)) { throw new Exception("INTV gamedb entries must have a board identifier!"); }
			if (gi == null && (rom.Length % 1024) != 0) { throw new Exception("Game is of unusual size but no header entry present and hash not in game db."); }
			_mapper = 0;
			int.TryParse(s_mapper, out _mapper);

			return rom.Length;
		}

		public ushort? ReadCart(ushort addr, bool peek)
		{
			switch (_mapper)
			{
				case 0:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0xB000];
					}

					if (addr >= 0xF000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xC000];
					}
					break;

				case 1:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0xD000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xB000];
					}
					break;

				case 2:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0x9000 && addr <= 0xBFFF)
					{
						return Data[addr - 0x7000];
					}

					if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0x8000];
					}
					break;

				case 3:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0x9000 && addr <= 0xAFFF)
					{
						return Data[addr - 0x7000];
					}

					if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0x9000];
					}

					if (addr >= 0xF000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xA000];
					}
					break;

				case 4:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0xD000 && addr <= 0xD3FF)
					{
						return Cart_Ram[addr - 0xD000];
					}
					break;

				case 5:
					if (addr >= 0x5000 && addr <= 0x7FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0x9000 && addr <= 0xBFFF)
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

					if (addr >= 0x9000 && addr <= 0xAFFF)
					{
						return Data[addr - 0x7000];
					}

					if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0x9000];
					}

					if (addr >= 0xF000 && addr <= 0xFFFF)
					{
						return Data[addr - 0xA000];
					}

					if (addr >= 0x8800 && addr <= 0x8FFF)
					{
						return Cart_Ram[addr - 0x8800];
					}
					break;

				case 10:
					if (addr >= 0x5000 && addr <= 0x6FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0x8800 && addr <= 0xB7FF)
					{
						return Data[addr - 0x6800];
					}

					if (addr >= 0xD000 && addr <= 0xFFFF)
					{
						return Data[addr - 0x8000];
					}
					break;
				case 11:
					if (addr >= 0x5000 && addr <= 0x5FFF)
					{
						return Data[addr - 0x5000];
					}

					if (addr >= 0xD000 && addr <= 0xDFFF)
					{
						return Data[addr - 0xC000];
					}
					break;
			}

			return null;
		}

		public bool WriteCart(ushort addr, ushort value, bool poke)
		{
			switch (_mapper)
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
