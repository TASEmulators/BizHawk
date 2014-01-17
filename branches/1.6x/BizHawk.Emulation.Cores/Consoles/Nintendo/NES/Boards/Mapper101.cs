using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Mapper 101:
	// bad dumps of Urusei - Lum no Wedding Bell (J)
	// good dumps of this rom are on Mapper087; only bad dumps with CHR banks out of order go here
	// nothing else uses this, other than hypothetical homebrews which might prefer it to CxROM
	// because of no bus conflicts
	public sealed class Mapper101 : NES.NESBoardBase
	{
		//configuration
		int chr_bank_mask_8k;

		//state
		int chr_bank_8k;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr_bank_8k", ref chr_bank_8k);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER101":
					AssertPrg(16, 32); AssertVram(0);
					Cart.wram_size = 0;
					Cart.wram_battery = false;
					AssertChr(8, 16, 32, 64, 128, 256, 512, 1024, 2048);
					break;
				default:
					return false;
			}

			chr_bank_mask_8k = (Cart.chr_size / 8) - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int ofs = addr & ((1 << 13) - 1);
				addr = (chr_bank_8k << 13) | ofs;
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			chr_bank_8k = value & chr_bank_mask_8k;
		}
	}
}
