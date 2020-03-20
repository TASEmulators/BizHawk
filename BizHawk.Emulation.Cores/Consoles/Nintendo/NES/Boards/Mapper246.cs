using BizHawk.Common;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_246
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper246 : NesBoardBase
	{
		int prg_bank_mask_8k;
		byte[] prg_banks_8k = new byte[4];

		int chr_bank_mask_2k;
		byte[] chr_banks_2k = new byte[4];

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER246":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			chr_bank_mask_2k = (Cart.chr_size / 2) - 1;
			prg_banks_8k[3] = 0xFF;
			SetMirrorType(EMirrorType.Horizontal);
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
			if (addr < 0x0800)
			{
				addr &= 0x0007;
				switch (addr)
				{
					case 0:
						prg_banks_8k[0] = value;
						break;
					case 1:
						prg_banks_8k[1] = value;
						break;
					case 2:
						prg_banks_8k[2] = value;
						break;
					case 3:
						prg_banks_8k[3] = value;
						break;
					case 4:
						chr_banks_2k[0] = value;
						break;
					case 5:
						chr_banks_2k[1] = value;
						break;
					case 6:
						chr_banks_2k[2] = value;
						break;
					case 7:
						chr_banks_2k[3] = value;
						break;
				}
				SyncMap();
			}
			else
			{
				base.WriteWram(addr, value);
			}
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
