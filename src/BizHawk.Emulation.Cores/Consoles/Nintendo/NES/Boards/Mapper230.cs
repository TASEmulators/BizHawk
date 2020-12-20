using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_230
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper230 : NesBoardBase
	{
		//TODO: soft reset back to contra = fails
		public int prg_page;
		public bool prg_mode;
		public bool contra_mode;
		public int chip0_prg_bank_mask_16k = 0x07;
		public int chip1_prg_bank_mask_16k = 0x1F;
		public int chip1_offset = 0x20000;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER230":
					break;
				default:
					return false;
			}
			contra_mode = true;
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(contra_mode), ref contra_mode);
			ser.Sync(nameof(prg_page), ref prg_page);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (contra_mode)
			{
				prg_page = value & 0x07;
			}
			else
			{
				prg_page = value & 0x1F;
				prg_mode = value.Bit(5);

				SetMirrorType(value.Bit(6) ? EMirrorType.Vertical : EMirrorType.Horizontal);
			}
		}

		public override byte ReadPrg(int addr)
		{
			if (contra_mode)
			{
				if (addr < 0x4000)
				{
					return Rom[((prg_page & chip0_prg_bank_mask_16k) * 0x4000) + addr];
				}

				return Rom[(7 * 0x4000) + (addr & 0x3FFF)];
			}

			if (prg_mode == false)
			{
				return Rom[((prg_page >> 1) * 0x8000) + addr + chip1_offset];
			}

			int page = prg_page + 8;
			return Rom[(page * 0x4000) + (addr & 0x03FFF)];
		}

		public override void NesSoftReset()
		{
			contra_mode ^= true;
			prg_page = 0;
			prg_mode = false;
			SetMirrorType(EMirrorType.Vertical);
		}
	}
}
