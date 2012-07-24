using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper112 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_1k;

		//state
		int reg_addr;
		ByteBuffer regs = new ByteBuffer(8);

		//volatile state
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER112":	
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			int num_chr_banks = (Cart.chr_size);
			chr_bank_mask_1k = num_chr_banks - 1;
			SetMirrorType(EMirrorType.Vertical);
			Sync();

			return true;
		}

		public override void Dispose()
		{
			base.Dispose();
			regs.Dispose();
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("regs", ref regs);
			Sync();
		}

		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("{0:X4} = {1:X2}", addr, value);
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					reg_addr = (value & 7);
					break;
				case 0x2000: //$A000
					regs[reg_addr] = value;
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

		void Sync()
		{
			prg_regs_8k[0] = regs[0];
			prg_regs_8k[1] = regs[1];
			prg_regs_8k[2] = 0xFE;
			prg_regs_8k[3] = 0xFF;

			byte r0_0 = (byte)(regs[2] & ~1);
			byte r0_1 = (byte)(regs[2] | 1);
			byte r1_0 = (byte)(regs[3] & ~1);
			byte r1_1 = (byte)(regs[3] | 1);

			chr_regs_1k[0] = r0_0;
			chr_regs_1k[1] = r0_1;
			chr_regs_1k[2] = r1_0;
			chr_regs_1k[3] = r1_1;
			chr_regs_1k[4] = regs[4];
			chr_regs_1k[5] = regs[5];
			chr_regs_1k[6] = regs[6];
			chr_regs_1k[7] = regs[7];
		}

		public int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_regs_8k[bank_8k];
			return bank_8k;
		}

		public int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = chr_regs_1k[bank_1k];
			return bank_1k;
		}

		int RewireCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			bank_1k &= chr_bank_mask_1k;
			int ofs = addr & ((1 << 10) - 1);
			addr = (bank_1k << 10) + ofs;
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000) return VROM[RewireCHR(addr)];
			else return base.ReadPPU(addr);
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) { }
			else base.WritePPU(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_bank_mask_8k;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}
	}
}
