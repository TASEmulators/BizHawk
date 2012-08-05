using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper91 : NES.NESBoardBase
	{
		/*
		*Note: Street Fighter III (Unl) is actually mapper 197.  However variations such as Street Fighter III (9 Fighter) and Mari Street Fighter III use this mapper
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_091
		*/

		ByteBuffer chr_regs_2k = new ByteBuffer(4);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);
		int chr_bank_mask_2k, prg_bank_mask_8k;
		MMC3 mmc3;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER091":
					break;
				default:
					return false;
			}

			chr_bank_mask_2k = Cart.chr_size / 2 - 1;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;

			prg_regs_8k[3] = 0xFF;
			prg_regs_8k[2] = 0xFE;
			
			mmc3 = new MMC3(this, 0x7FFFFFFF);
			
			return true;
		}

		public override void Dispose()
		{
			prg_regs_8k.Dispose();
			chr_regs_2k.Dispose();
			mmc3.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			mmc3.SyncState(ser);
			ser.Sync("prg_regs", ref prg_regs_8k);
			ser.Sync("chr_regs", ref chr_regs_2k);
			base.SyncState(ser);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			switch (addr & 0x7003)
			{
				case 0x0000:
					chr_regs_2k[0] = value;
					break;
				case 0x0001:
					chr_regs_2k[1] = value;
					break;
				case 0x0002:
					chr_regs_2k[2] = value;
					break;
				case 0x0003:
					chr_regs_2k[3] = value;
					break;
				case 0x1000:
					prg_regs_8k[0] = (byte)(value & 0x0F);
					break;
				case 0x1001:
					prg_regs_8k[1] = (byte)(value & 0x0F);
					break;
				case 0x1002: //$7002
					mmc3.WritePRG(0xE000, value);
					break;
				case 0x1003: //$7003
					mmc3.WritePRG(0xC000, 7);
					mmc3.WritePRG(0xC001, value);
					mmc3.WritePRG(0xE001, value);
					break;
			}
		}

		public override void ClockPPU()
		{
			mmc3.ClockPPU();
		}

		public override void AddressPPU(int addr)
		{
			mmc3.AddressPPU(addr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_2k = (addr >> 11) - 1;
				bank_2k = chr_regs_2k[bank_2k];
				bank_2k &= chr_bank_mask_2k;
				return VROM[(bank_2k * 0x800) + addr];
			}
			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			return ROM[(bank_8k * 0x2000) + (addr & 0x1FFF)];
		}
	}
}
