using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper112 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_8k, chr_bank_mask_1k, chr_outer_reg;

		//state
		private int reg_addr;
		private byte[] regs = new byte[8];

		private int[] _chrRegs1K = new int[8];
		private byte[] _prgRegs8K = new byte[4];

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER112":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = (Cart.PrgSize / 8) - 1;
			int num_chr_banks = (Cart.ChrSize);
			chr_bank_mask_1k = num_chr_banks - 1;
			SetMirrorType(EMirrorType.Vertical);
			Sync();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(reg_addr), ref reg_addr);
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(chr_outer_reg), ref chr_outer_reg);
			ser.Sync(nameof(_chrRegs1K), ref _chrRegs1K, false);
			ser.Sync(nameof(_prgRegs8K), ref _prgRegs8K, false);

			Sync();
		}

		public override void WritePrg(int addr, byte value)
		{
			
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					reg_addr = (value & 7);
					break;
				case 0x2000: //$A000
					regs[reg_addr] = value;
					Sync();
					break;
				case 0x4000:
					Console.WriteLine("{0:X4} = {1:X2}", addr, value);
					chr_outer_reg = value;
					Sync();
					break;
				case 0x6000:
					if ((value & 1) == 0)
					{
						SetMirrorType(EMirrorType.Vertical);
					}
					else
					{
						SetMirrorType(EMirrorType.Horizontal);
					}
					break;
			}
		}

		private void Sync()
		{
			_prgRegs8K[0] = regs[0];
			_prgRegs8K[1] = regs[1];
			_prgRegs8K[2] = 0xFE;
			_prgRegs8K[3] = 0xFF;

			byte r0_0 = (byte)(regs[2] & ~1);
			byte r0_1 = (byte)(regs[2] | 1);
			byte r1_0 = (byte)(regs[3] & ~1);
			byte r1_1 = (byte)(regs[3] | 1);

			int temp4 = (chr_outer_reg & 0x10) << 4;
			int temp5 = (chr_outer_reg & 0x20) << 3;
			int temp6 = (chr_outer_reg & 0x40) << 2;
			int temp7 = (chr_outer_reg & 0x80) << 1;

			_chrRegs1K[0] = r0_0;
			_chrRegs1K[1] = r0_1;
			_chrRegs1K[2] = r1_0;
			_chrRegs1K[3] = r1_1;
			_chrRegs1K[4] = temp4 | regs[4];
			_chrRegs1K[5] = temp5 | regs[5];
			_chrRegs1K[6] = temp6 | regs[6];
			_chrRegs1K[7] = temp7 | regs[7];
		}

		public int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = _prgRegs8K[bank_8k];
			return bank_8k;
		}

		public int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = _chrRegs1K[bank_1k];
			return bank_1k;
		}

		private int RewireCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			bank_1k &= chr_bank_mask_1k;
			int ofs = addr & ((1 << 10) - 1);
			addr = (bank_1k << 10) + ofs;
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000) return Vrom[RewireCHR(addr)];
			else return base.ReadPpu(addr);
		}
		public override void WritePpu(int addr, byte value)
		{
			if (addr >= 0x2000) base.WritePpu(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_bank_mask_8k;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}
	}
}
