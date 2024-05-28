using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper226 : NesBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_226
		public int prg_page;
		public bool prg_mode;

		private int prg_mask_32k;
		private int prg_mask_16k;

		private bool resetFlag = false;
		private bool resetSwitchMode = false;

		public override bool Configure(EDetectionOrigin origin)
		{
			int prg_mask_hack = 1;
			switch (Cart.BoardType)
			{
				case "MAPPER226":
					break;
				case "UNIF_BMC-42in1ResetSwitch":
					resetSwitchMode = true;
					prg_mask_hack = 2;
					break;
				default:
					return false;
			}
			prg_page = 0;
			prg_mode = false;

			prg_mask_32k = (Cart.PrgSize / prg_mask_hack) / 32 - 1;
			prg_mask_16k = (Cart.PrgSize / prg_mask_hack) / 16 - 1;

			return true;
		}

		public override void NesSoftReset()
		{
			resetFlag ^= true;
			prg_page = 0;
			prg_mode = false;
			base.NesSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_page), ref prg_page);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync(nameof(resetFlag), ref resetFlag);
			ser.Sync(nameof(resetSwitchMode), ref resetSwitchMode);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr &= 1;
			if (addr == 0)
			{
				prg_page &= ~0x3F;
				prg_page |= ((value & 0x1F) + ((value & 0x80) >> 2));
				prg_mode = value.Bit(5);

				if (value.Bit(6))
				{
					SetMirrorType(EMirrorType.Vertical);
				}
				else
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
			}
			else if (addr == 1)
			{
				prg_page &= ~0x40;
				prg_page |= ((value & 0x1) << 6);
			}
		}
		
		public override byte ReadPrg(int addr)
		{
			int baseAddr = resetSwitchMode && resetFlag ? 0x80000 : 0;

			if (!prg_mode)
			{
				return Rom[baseAddr + (( ((prg_page >> 1) & prg_mask_32k) << 15) + (addr & 0x7FFF))];
			}

			return Rom[baseAddr + (((prg_page & prg_mask_16k) << 14) + (addr & 0x3FFF))];
		}
	}
}
