using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper186 : NES.NESBoardBase
	{
		private ByteBuffer _SRAM = new ByteBuffer(3072);
		private ByteBuffer regs = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER186":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void Dispose()
		{
			_SRAM.Dispose();
			regs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("SRAM", ref _SRAM);
			ser.Sync("regs", ref regs);
			base.SyncState(ser);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[(regs[1] << 14) + (addr & 0x3FFF)];
			}

			// C000-FFFF is always bank 0
			return ROM[addr & 0x3FFF];
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[(regs[0] >> 6) + addr];
		}

		public override void WriteEXP(int addr, byte value)
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
				base.WriteEXP(addr, value);
			}
		}

		public override byte ReadEXP(int addr)
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

			return base.ReadEXP(addr);
		}
	}
}
