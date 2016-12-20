using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class MapperPocahontas : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(3);

		public ByteBuffer prg_regs_8k = new ByteBuffer(4);

		private int prg_mask_8k, chr_mask_1k;

		private byte[] regs_sec = { 0, 2, 6, 1, 7, 3, 4, 5 }; 

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "Pocahontas":
					break;
				default:
					return false;
			}

			BaseSetup();
			exRegs[0] = 0;
			exRegs[1] = 0;
			exRegs[2] = 0;

			prg_mask_8k = Cart.prg_size / 8 - 1;
			chr_mask_1k = Cart.chr_size - 1;

			prg_regs_8k[0] = 0;
			prg_regs_8k[1] = 1;
			prg_regs_8k[2] = (byte)(0xFE & prg_mask_8k);
			prg_regs_8k[3] = (byte)(0xFF & prg_mask_8k);

			
			return true;
		}

		public override void Dispose()
		{
			exRegs.Dispose();
			prg_regs_8k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
			ser.Sync("ptg_regs_8k", ref prg_regs_8k);
			ser.Sync("prg_mask", ref prg_mask_8k);
			ser.Sync("chr_mask", ref chr_mask_1k);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x1000) { exRegs[0] = value; }
			if (addr == 0x1001) { exRegs[1] = value; }

			base.WriteEXP(addr, value);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;

			if (addr < 0xA000)
			{
				if (((value >> 7) | value)==0)
				{
					SetMirrorType(EMirrorType.Vertical);
				}
				else
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
			}
			else if (addr < 0xC000)
			{
				value = (byte)((value & 0xC0) | regs_sec[value & 0x07]);
				exRegs[2] = 1;

				base.WritePRG(0x0000, value);
			}
			else if (addr < 0xE000)
			{
				if (exRegs[2] >0)
				{
					exRegs[2] = 0;
					base.WritePRG(0x0001, value);
				}
			}
			else if (addr < 0xF000)
			{
				// nothing
				base.WritePRG(0x6000, value);
			}
			else
			{
				base.WritePRG(0x6001, value);
				base.WritePRG(0x4000, value);
				base.WritePRG(0x4001, value);
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = base.Get_CHRBank_1K(addr);

				bank_1k |= (exRegs[1] << 6 & 0x100);
				bank_1k &= chr_mask_1k;
				addr = (bank_1k << 10) | (addr & 0x3FF);
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank = exRegs[0] & 0xF;
			if ((exRegs[0] & 0x80)>0)
			{
				if ((exRegs[0] & 0x20)>0)
				{
					return ROM[((bank >> 1) << 15) + (addr & 0x7FFF)];
				}
				else
				{
					return ROM[((bank) << 14) + (addr & 0x3FFF)];
				}
			}

			else
			{
				bank = mmc3.Get_PRGBank_8K(addr);
				bank &= prg_mask_8k;
				return ROM[(bank << 13) + (addr & 0x1FFF)];
			}
			
		}
	}
}
