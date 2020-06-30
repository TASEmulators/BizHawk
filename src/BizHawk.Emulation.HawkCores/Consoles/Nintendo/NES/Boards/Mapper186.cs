using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal class Mapper186 : NesBoardBase
	{
		private byte[] _SRAM = new byte[3072];
		private byte[] regs = new byte[4];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER186":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("SRAM", ref _SRAM, false);
			ser.Sync(nameof(regs), ref regs, false);
			base.SyncState(ser);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
			{
				return Rom[(regs[1] << 14) + (addr & 0x3FFF)];
			}

			// C000-FFFF is always bank 0
			return Rom[addr & 0x3FFF];
		}

		public override byte ReadWram(int addr)
		{
			return Rom[(regs[0] >> 6) + addr];
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x200 && addr < 0x400)
			{
				if (((addr + 0x4000) & 0x4203) > 0)
				{
					regs[addr & 3] = value;
				}
			}
			else if (addr < 0x1000)
			{
				_SRAM[addr - 0x400] = value;
			}
			else
			{
				base.WriteExp(addr, value);
			}
		}

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x200 && addr < 0x400)
			{
				switch (addr + 0x4000)
				{
					case 0x4200: return 0x00;
					case 0x4201: return 0x00;
					case 0x4202: return 0x40;
					case 0x4203: return 0x00;
				}
			}
			else if (addr < 0x1000)
			{
				return _SRAM[addr - 0x400];
			}

			return base.ReadExp(addr);
		}
	}
}
