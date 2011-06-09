using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 033

	//Akira
	//Bakushou!! Jinsei Gekijou
	//Don Doko Don
	//Insector X


	class TAITO_TC0190FMC : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask, chr_bank_mask;

		//state
		ByteBuffer prg_regs_8k = new ByteBuffer(4);
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		int mirror_mode;

		public override void Dispose()
		{
			prg_regs_8k.Dispose();
			chr_regs_1k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_regs_8k", ref prg_regs_8k);
			ser.Sync("chr_regs_1k", ref chr_regs_1k);
			ser.Sync("mirror_mode", ref mirror_mode);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "TAITO-TC0190FMC":
				case "TAITO-TC0350FMR":
					AssertPrg(128); AssertChr(128,256); AssertWram(0); AssertVram(0);
					break;
				default:
					return false;
			}

			prg_bank_mask = Cart.prg_size / 8 - 1;
			chr_bank_mask = Cart.chr_size - 1;

			prg_regs_8k[0] = 0x00;
			prg_regs_8k[1] = 0x00;
			prg_regs_8k[2] = 0xFE; //constant
			prg_regs_8k[3] = 0xFF; //constant

			SyncMirror();

			return true;
		}

		void SyncMirror()
		{
			if (mirror_mode == 0)
				SetMirrorType(EMirrorType.Vertical);
			else SetMirrorType(EMirrorType.Horizontal);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0xA003;
			switch (addr)
			{
				//$8000 [.MPP PPPP]
				//M = Mirroring (0=Vert, 1=Horz)
				//P = PRG Reg 0 (8k @ $8000)
				case 0x0000:
					prg_regs_8k[0] = (byte)(value & 0x3F);
					mirror_mode = (value >> 6) & 1;
					SyncMirror();
					break;

				case 0x0001: //$8001 [..PP PPPP]   PRG Reg 1 (8k @ $A000)
					prg_regs_8k[1] = (byte)(value & 0x3F);
					break;

				case 0x0002: //$8002 [CCCC CCCC]   CHR Reg 0 (2k @ $0000)
					chr_regs_1k[0] = (byte)(value * 2);
					chr_regs_1k[1] = (byte)(value * 2 + 1);
					break;

				case 0x0003: //$8003 [CCCC CCCC]   CHR Reg 1 (2k @ $0800)
					chr_regs_1k[2] = (byte)(value * 2);
					chr_regs_1k[3] = (byte)(value * 2 + 1);
					break;
				
				case 0x2000: //$A000 [CCCC CCCC]   CHR Reg 2 (1k @ $1000)
					chr_regs_1k[4] = value;
					break;
				case 0x2001: //$A001 [CCCC CCCC]   CHR Reg 3 (1k @ $1400)
					chr_regs_1k[5] = value;
					break;
				case 0x2002: //$A002 [CCCC CCCC]   CHR Reg 4 (1k @ $1800)
					chr_regs_1k[6] = value;
					break;
				case 0x2003: //$A003 [CCCC CCCC]   CHR Reg 5 (1k @ $1C00)
					chr_regs_1k[7] = value;
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_regs_1k[bank_1k];
				bank_1k &= chr_bank_mask;
				addr = (bank_1k << 10) | ofs;
				return VROM[addr];
			}
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask;
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}
	}
}