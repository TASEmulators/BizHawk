using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_193
	public sealed class Mapper193 : NesBoardBase 
	{
		private int prg_bank_mask_8k;
		private byte[] prg_banks_8k = new byte[4];

		private int chr_bank_mask_2k;
		private byte[] chr_banks_2k = new byte[4];

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER193":
					break;
				case "NTDEC-TC-112": // untested
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			prg_banks_8k[1] = 0xFD;
			prg_banks_8k[2] = 0xFE;
			prg_banks_8k[3] = 0xFF;

			chr_bank_mask_2k = (Cart.chr_size / 2) - 1;

			SetMirrorType(EMirrorType.Vertical);
			SyncMap();
			return true;
		}

		private void SyncMap()
		{
			ApplyMemoryMapMask(prg_bank_mask_8k, prg_banks_8k);
			ApplyMemoryMapMask(chr_bank_mask_2k, chr_banks_2k);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_8k), ref prg_banks_8k, false);
			ser.Sync(nameof(chr_banks_2k), ref chr_banks_2k, false);
		}

		public override void WriteWram(int addr, byte value)
		{
			addr &= 0x6003;
			switch (addr)
			{
				case 0:
					chr_banks_2k[0] = (byte)((value & ~3) >> 1);
					chr_banks_2k[1] = (byte)(((value & ~3) >> 1) + 1); 
					break;
				case 1:
					chr_banks_2k[2] = (byte)((value & ~1) >> 1);
					break;
				case 2:
					chr_banks_2k[3] = (byte)((value & ~1) >> 1); 
					break;
				case 3:
					prg_banks_8k[0] = value;
					break;
			}
			SyncMap();
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = ApplyMemoryMap(11, chr_banks_2k, addr);
				return base.ReadPPUChr(addr);
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			addr = ApplyMemoryMap(13, prg_banks_8k, addr);
			return Rom[addr];
		}
	}
}
