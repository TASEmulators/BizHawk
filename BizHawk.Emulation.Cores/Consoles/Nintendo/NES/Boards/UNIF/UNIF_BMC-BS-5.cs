using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_BMC_BS_5 : NES.NESBoardBase
	{
		[MapperProp]
		public int BMC_BS_5_DipSwitch;

		private IntBuffer reg_prg = new IntBuffer(4);
		private IntBuffer reg_chr = new IntBuffer(4);

		private int _prgMask8k;
		private int _chrMask2k;

		private const int DipSwitchMask = 3;

		public override bool Configure(NES.EDetectionOrigin origin)
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

		public override void NESSoftReset()
		{
			reg_prg[0] = 0xFF;
			reg_prg[1] = 0xFF;
			reg_prg[2] = 0xFF;
			reg_prg[3] = 0xFF;

			base.NESSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg_prg", ref reg_prg);
			ser.Sync("reg_chr", ref reg_chr);
			ser.Sync("BMC_BS_5_DipSwitch", ref BMC_BS_5_DipSwitch);
		}

		public override void WritePRG(int addr, byte value)
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

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x2000)
			{
				return ROM[((reg_prg[0] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0x4000)
			{
				return ROM[((reg_prg[1] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0x6000)
			{
				return ROM[((reg_prg[2] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}

			return ROM[((reg_prg[3] & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (addr < 0x800)
				{
					return VROM[((reg_chr[0] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
				}

				if (addr < 0x1000)
				{
					return VROM[((reg_chr[1] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
				}

				if (addr < 0x1800)
				{
					return VROM[((reg_chr[2] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
				}

				return VROM[((reg_chr[3] & _chrMask2k) * 0x800) + (addr & 0x7FF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
