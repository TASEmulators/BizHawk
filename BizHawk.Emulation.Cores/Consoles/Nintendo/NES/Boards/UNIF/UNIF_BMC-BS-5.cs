using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_BMC_BS_5 : NesBoardBase
	{
		[MapperProp]
		public int BMC_BS_5_DipSwitch;

		private int[] reg_prg = new int[4];
		private int[] reg_chr = new int[4];

		private int _prgMask8k;
		private int _chrMask2k;

		private const int DipSwitchMask = 3;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-BS-5":
					break;
				default:
					return false;
			}

			reg_prg[0] = 0xFF;
			reg_prg[1] = 0xFF;
			reg_prg[2] = 0xFF;
			reg_prg[3] = 0xFF;

			SetMirrorType(EMirrorType.Vertical);

			_prgMask8k = Cart.prg_size / 8 - 1;
			_chrMask2k = Cart.prg_size / 2 - 1;

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void NesSoftReset()
		{
			reg_prg[0] = 0xFF;
			reg_prg[1] = 0xFF;
			reg_prg[2] = 0xFF;
			reg_prg[3] = 0xFF;

			base.NesSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(reg_prg), ref reg_prg, false);
			ser.Sync(nameof(reg_chr), ref reg_chr, false);
			ser.Sync(nameof(BMC_BS_5_DipSwitch), ref BMC_BS_5_DipSwitch);
		}

		public override void WritePrg(int addr, byte value)
		{
			// TODO: clean this up
			addr += 0x8000;
			int bank_sel = (addr & 0xC00) >> 10;
			switch (addr & 0xF000)
			{
				case 0x8000:
					reg_chr[bank_sel] = addr & 0x1F;
					break;
				case 0xA000:
					if ((addr & (1 << (BMC_BS_5_DipSwitch + 4))) > 0)
					{
						reg_prg[bank_sel] = addr & 0x0F;
					}
					break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x2000)
			{
				return Rom[((reg_prg[0] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0x4000)
			{
				return Rom[((reg_prg[1] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0x6000)
			{
				return Rom[((reg_prg[2] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}

			return Rom[((reg_prg[3] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (addr < 0x800)
				{
					return Vrom[((reg_chr[0] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
				}

				if (addr < 0x1000)
				{
					return Vrom[((reg_chr[1] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
				}

				if (addr < 0x1800)
				{
					return Vrom[((reg_chr[2] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
				}

				return Vrom[((reg_chr[3] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
