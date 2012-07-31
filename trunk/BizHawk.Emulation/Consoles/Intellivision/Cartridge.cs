using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class Cartridge : ICart
	{
		private ushort[] Data = new ushort[56320];

		public int Parse(byte[] Rom)
		{
			// TODO: Actually parse the ROM.
			return 1;
		}

		public ushort? Read(ushort addr)
		{
			// TODO: Check if address is RAM / ROM.
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x03FF)
						break;
					if (addr <= 0x04FF)
						// OK on all but Intellivision 2.
						return Data[addr & 0x00FF];
					else if (addr <= 0x06FF)
						return Data[addr & 0x02FF];
					else if (addr <= 0x0CFF)
						// OK if no Intellivoice.
						return Data[addr & 0x08FF];
					else
						return Data[addr & 0x0BFF];
				case 0x2000:
					// OK if no ECS.
					return Data[(addr & 0x0FFF) + 0x0C00];
				case 0x4000:
					if (addr <= 0x47FF)
						// OK if no ECS.
						return Data[(addr & 0x07FF) + 0x1C00];
					else if (addr == 0x4800)
						return Data[0x2400];
					else
						return Data[(addr & 0x0FFF) + 0x1C00];
				case 0x5000:
				case 0x6000:
					if (addr <= 0x5014)
						return Data[(addr & 0x0014) + 0x2C00];
					else
						return Data[(addr & 0x1FFF) + 0x2C00];
				case 0x7000:
					if (addr == 0x7000)
						// OK if no ECS.
						return Data[0x04C00];
					else if (addr <= 0x77FF)
						// OK if no ECS.
						return Data[(addr & 0x07FF) + 0x4C00];
					else
						// OK if no ECS.
						return Data[(addr & 0x0FFF) + 0x4C00];
				case 0x8000:
					// OK. Avoid STIC alias at $8000-$803F.
					return Data[(addr & 0x0FFF) + 0x5C00];
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
						return Data[(addr & 0x27FF) + 0x6C00];
					else
						return Data[(addr & 0x2FFF) + 0x6C00];
				case 0xC000:
					// OK. Avoid STIC alias at $C000-$C03F.
					return Data[(addr & 0x0FFF) + 0x9C00];
				case 0xD000:
					return Data[(addr & 0x0FFF) + 0xAC00];
				case 0xE000:
					// OK if no ECS.
					return Data[(addr & 0x0FFF) + 0xBC00];
				case 0xF000:
					if (addr <= 0xF7FF)
						return Data[(addr & 0x07FF) + 0xCC00];
					else
						return Data[(addr & 0x0FFF) + 0xCC00];
			}
			return null;
		}

		public bool Write(ushort addr, ushort value)
		{
			// TODO: Check if address is RAM / ROM.
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x03FF)
						break;
					if (addr <= 0x04FF)
					{
						// OK on all but Intellivision 2.
						Data[addr & 0x00FF] = value;
						return true;
					}
					else if (addr <= 0x06FF)
					{
						Data[addr & 0x02FF] = value;
						return true;
					}
					else if (addr <= 0x0CFF)
					{
						// OK if no Intellivoice.
						Data[addr & 0x08FF] = value;
						return true;
					}
					else
					{
						Data[addr & 0x0BFF] = value;
						return true;
					}
				case 0x2000:
					// OK if no ECS.
					Data[(addr & 0x0FFF) + 0x0C00] = value;
					return true;
				case 0x4000:
					if (addr <= 0x47FF)
					{
						// OK if no ECS.
						Data[(addr & 0x07FF) + 0x1C00] = value;
						return true;
					}
					else if (addr == 0x4800)
					{
						// OK only if boot ROM at $7000.
						Data[0x2400] = value;
						return true;
					}
					else
					{
						Data[(addr & 0x0FFF) + 0x1C00] = value;
						return true;
					}
				case 0x5000:
				case 0x6000:
					if (addr <= 0x5014)
					{
						// OK only if boot ROM at $4800 or $7000.
						Data[(addr & 0x0014) + 0x2C00] = value;
						return true;
					}
					else
					{
						Data[(addr & 0x1FFF) + 0x2C00] = value;
						return true;
					}
				case 0x7000:
					if (addr == 0x7000)
					{
						// RAM at $7000 confuses EXEC boot sequence.
						Data[0x04C00] = value;
						return true;
					}
					else if (addr <= 0x77FF)
					{
						// OK if no ECS.
						Data[(addr & 0x07FF) + 0x4C00] = value;
						return true;
					}
					else
					{
						// Do not map RAM here due to GRAM alias.
						Data[(addr & 0x0FFF) + 0x4C00] = value;
						return true;
					}
				case 0x8000:
					// OK. Avoid STIC alias at $8000-$803F.
					Data[(addr & 0x0FFF) + 0x5C00] = value;
					return true;
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
					{
						Data[(addr & 0x27FF) + 0x6C00] = value;
						return true;
					}
					else
					{
						// Do not map RAM here due to GRAM alias.
						Data[(addr & 0x2FFF) + 0x6C00] = value;
						return true;
					}
				case 0xC000:
					// OK. Avoid STIC alias at $C000-$C03F.
					Data[(addr & 0x0FFF) + 0x9C00] = value;
					return true;
				case 0xD000:
					Data[(addr & 0x0FFF) + 0xAC00] = value;
					return true;
				case 0xE000:
					// OK if no ECS.
					Data[(addr & 0x0FFF) + 0xBC00] = value;
					return true;
				case 0xF000:
					if (addr <= 0xF7FF)
					{
						Data[(addr & 0x07FF) + 0xCC00] = value;
						return true;
					}
					else
					{
						// Do not map RAM here due to GRAM alias.
						Data[(addr & 0x0FFF) + 0xCC00] = value;
						return true;
					}
			}
			return false;
		}
	}
}
