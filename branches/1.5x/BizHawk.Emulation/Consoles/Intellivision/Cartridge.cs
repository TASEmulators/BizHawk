namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class Cartridge : ICart
	{
		private ushort[] Data = new ushort[56320];

		public int Parse(byte[] Rom)
		{
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
			return Rom.Length;
		}

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
	}
}
