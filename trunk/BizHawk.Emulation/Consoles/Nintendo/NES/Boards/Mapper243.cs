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

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER243":
					break;
				default:
					return false;
			}

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
			switch (addr & 0x101)
			{
				case 0x0000:
					reg_addr = addr & 0x07;
					break;
				case 0x0001:
					switch (reg_addr)
					{
						case 0:
							regs[0] = 0; regs[1] = 3; //FCEUX does this
							break;
						case 2:
							regs[3] = (byte)((value & 0x01) << 3);
							break;
						case 4:
							regs[1] = (byte)((regs[1] & 0x06) | (value & 0x03));
							break;
						case 5:
							regs[0] = (byte)(value & 0x01);
							break;
						case 6:
							regs[1] = (byte)((regs[1] & 0x01) | (regs[3] | (value & 0x03) << 1));
							break;
						case 7:
							regs[2] = (byte)(value & 0x01);
							break;

					}

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
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int chr_bank = regs[4] | (regs[6] << 1) | (regs[2] << 3);
				return VROM[(chr_bank * 0x2000) + addr];
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(regs[5] * 0x8000) + addr];
		}
	}
}
