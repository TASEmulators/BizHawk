using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Magic Kid GooGoo
	public sealed class Mapper190 : NES.NESBoardBase
	{
		//state
		int prg_reg;
		int[] chr_reg = new int[4];

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);
			ser.Sync(nameof(chr_reg), ref chr_reg, false);
			base.SyncState(ser);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER190":
					AssertPrg(256); AssertChr(128); AssertVram(0); AssertWram(8);
					break;
				default:
					return false;
			}

			prg_reg = 0;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[(prg_reg * 0x4000) + addr];
			}
			else
			{
				return ROM[addr - 0x4000];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = addr >> 11;
				int ofs = addr & ((1 << 11) - 1);
				bank = chr_reg[bank];
				addr = (bank << 11) | ofs;
				return VROM[addr];
			}
			else
				return base.ReadPPU(addr);		
		}

		public override void WritePRG(int addr, byte value)
		{
			int addr_temp = addr & 0xF000;
			switch (addr_temp)
			{
				case 0x0000: prg_reg = value & 0x7; break;
				case 0x2000: chr_reg[addr & 3] = value & 0x3F; break;
				case 0x4000: prg_reg = 8 | (value & 0x7); break;
			}
		}


	}
}
