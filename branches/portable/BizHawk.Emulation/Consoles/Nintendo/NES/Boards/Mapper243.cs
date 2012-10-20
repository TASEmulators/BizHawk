using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper243 : NES.NESBoardBase
	{
		/*
			Here are Disch's original notes:  
		========================
		=  Mapper 243          =
		========================


		Example Games:
		--------------------------
		Honey
		Poker III 5-in-1


		Registers:
		---------------------------

		Range,Mask:   $4020-4FFF, $4101

		$4100:  [.... .AAA]   Address for use with $4101

		$4101:   Data port
		R:2 -> [.... ...H]  High bit of CHR reg
		R:4 -> [.... ...L]  Low bit of CHR reg
		R:5 -> [.... .PPP]  PRG reg  (32k @ $8000)
		R:6 -> [.... ..DD]  Middle bits of CHR reg
		R:7 -> [.... .MM.]  Mirroring
			%00 = Horz
			%01 = Vert
			%10 = See below
			%11 = 1ScB


		Mirroring:
		---------------------------

		Mirroing mode %10 is not quite 1ScB:

		[  NTA  ][  NTB  ]
		[  NTB  ][  NTB  ]


		CHR Setup:
		---------------------------

		8k CHR page @ $0000 is selected by the given 4 bit CHR page number ('HDDL')
		*/

		int reg_addr;
		ByteBuffer regs = new ByteBuffer(8);
		int chr_bank_mask_8k, prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER243":
				case "UNIF_UNL-Sachen-74LS374N": // seems to have some problems
					break;
				default:
					return false;
			}
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("regs", ref regs);
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
		{
			switch (addr & 0x01)
			{
				case 0x0000:
					reg_addr = value & 0x07;
					break;
				case 0x0001:
					switch (reg_addr)
					{
						case 2:
							regs[2] = (byte)(value & 0x01);
							break;
						case 4:
							regs[4] = (byte)(value & 0x01);
							break;
						case 5:
							regs[5] = (byte)(value & 0x07);
							break;
						case 6:
							regs[6] = (byte)(value & 0x03);
							break;
						case 7:
							int mirror = (value >> 1) & 0x03;
							switch (mirror)
							{
								case 0:
									SetMirrorType(EMirrorType.Horizontal);
									break;
								case 1:
									SetMirrorType(EMirrorType.Vertical);
									break;
								case 2:
									SetMirroring(0, 1, 1, 1);
									break;
								case 3:
									SetMirrorType(EMirrorType.OneScreenB);
									break;
							}
							break;
					}
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int chr_bank = regs[4] | (regs[6] << 1) | (regs[2] << 3);
				return VROM[((chr_bank & chr_bank_mask_8k) * 0x2000) + addr];
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[((regs[5] & prg_bank_mask_32k) * 0x8000) + addr];
		}
	}
}
