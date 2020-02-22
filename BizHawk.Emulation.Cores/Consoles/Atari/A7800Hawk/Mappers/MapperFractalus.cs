using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Rescue on Fractalus has unique RAM mapping
	public sealed class MapperFractalus : MapperBase
	{
		private byte[] RAM = new byte[0x800];

		public override byte ReadMemory(ushort addr)
		{
			if (addr >= 0x1000 && addr < 0x1800)
			{
				//could be hsbios RAM here
				if (Core._hsbios != null)
				{
					return Core._hsram[addr - 0x1000];
				}

				return 0xFF;
			}

			if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				if (addr >= 0x3000 && Core._hsbios != null)
				{
					return Core._hsbios[addr - 0x3000];
				}

				return Core.RAM[0x800 + addr & 0x7FF];
			}

			// cartridge and other OPSYS
			if (Core._rom.Length >= 0x10000 - addr
				&& Core.A7800_control_register.Bit(2))
			{
				return Core._rom[Core._rom.Length - (0x10000 - addr)];
			}

			if (addr >= 0x10000-Core._bios.Length && !Core.A7800_control_register.Bit(2))
			{
				return Core._bios[addr - (0x10000 - Core._bios.Length)];
			}

			if (addr >= 0x4000 && addr <0x5000)
			{
				int tempRet1 = ((addr >> 8) & 0xE) >> 1;
				int tempRet2 = addr & 0xFF;

				return RAM[(tempRet1 << 8) + tempRet2];
			}

			return 0x00;
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr >= 0x1000 && addr < 0x1800)
			{
				//could be hsbios RAM here
				if (Core._hsbios != null)
				{
					Core._hsram[addr - 0x1000] = value;
				}
			}
			else if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				if (addr >= 0x3000 && Core._hsbios != null)
				{
				}
				else
				{
					Core.RAM[0x800 + addr & 0x7FF] = value;
				}
			}
			else
			{
				if (addr >= 0x4000 && addr < 0x5000)
				{
					int tempRet1 = ((addr >> 8) & 0xE) >> 1;
					int tempRet2 = addr & 0xFF;

					RAM[(tempRet1 << 8) + tempRet2] = value;
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(RAM), ref RAM, false);
		}
	}
}
