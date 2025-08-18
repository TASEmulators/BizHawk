﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_033
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_048

	//AKA mapper 033

	//Akira
	//Bakushou!! Jinsei Gekijou
	//Don Doko Don
	//Insector X

	//also mapper 048 (same as 33 but with an extra chip)

	internal sealed class TAITO_TC0190FMC : NesBoardBase
	{
		//configuration
		private int prg_bank_mask, chr_bank_mask;
		private bool pal16;

		private class MMC3Variant : MMC3
		{
			public MMC3Variant(NesBoardBase board)
			: base(board,0)
			{
			}

			private bool pending;
			private int delay;

			public override void SyncState(Serializer ser)
			{
				base.SyncState(ser);
				ser.BeginSection("mmc3variant");
				ser.Sync(nameof(pending), ref pending);
				ser.Sync(nameof(delay), ref delay);
				ser.EndSection();
			}

			public override void SyncIRQ()
			{
				if (irq_pending && !pending)
					delay = 15; // 5 cpu cycle delay fixes gltiches in Jetsons
				if (!irq_pending)
				{
					delay = 0;
					board.IrqSignal = false;
				}
				pending = irq_pending;
			}

			public override void ClockPPU()
			{
				base.ClockPPU();

				if (delay > 0)
				{

					delay--;
					if(delay==0 && irq_pending)
						board.IrqSignal = true;
				}
			}
		}


		//state
		private byte[] prg_regs_8k = new byte[4];
		private byte[] chr_regs_1k = new byte[8];
		private int mirror_mode;
		private MMC3Variant mmc3;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			mmc3?.SyncState(ser);
			ser.Sync(nameof(prg_regs_8k), ref prg_regs_8k, false);
			ser.Sync(nameof(chr_regs_1k), ref chr_regs_1k, false);
			ser.Sync(nameof(mirror_mode), ref mirror_mode);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER033":
					break;
				case "TAITO-TC0190FMC":
				case "TAITO-TC0350FMR":
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0); AssertVram(0);
					pal16 = false;
					break;
				case "MAPPER048": // TODO: Disch docs say that mapper 48 is a TC0690 which is a superset of TC0190FMC
					pal16 = true;
					mmc3 = new MMC3Variant(this);
					break;
				case "TAITO-TC0190FMC+PAL16R4":
					//this is the same as the base TAITO-TC0190FMC, with an added PAL16R4ACN which is a "programmable TTL device", presumably just the IRQ and mirroring
					AssertPrg(128, 256); AssertChr(256); AssertWram(0); AssertVram(0);
					pal16 = true;
					mmc3 = new MMC3Variant(this);
					break;
				default:
					return false;
			}

			prg_bank_mask = Cart.PrgSize / 8 - 1;
			chr_bank_mask = Cart.ChrSize - 1;

			prg_regs_8k[0] = 0x00;
			prg_regs_8k[1] = 0x00;
			prg_regs_8k[2] = 0xFE; //constant
			prg_regs_8k[3] = 0xFF; //constant

			SyncMirror();

			return true;
		}

		private void SyncMirror()
		{
			SetMirrorType(mirror_mode == 0 ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (pal16)
				addr &= 0xE003;
			else
				addr &= 0xA003;
			switch (addr)
			{
				//$8000 [.MPP PPPP]
				//M = Mirroring (0=Vert, 1=Horz)
				//P = PRG Reg 0 (8k @ $8000)
				case 0x0000:
					prg_regs_8k[0] = (byte)(value & 0x3F);
					if(!pal16) mirror_mode = (value >> 6) & 1;
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

				case 0x4000:
					if (!pal16) break;
					mmc3.WritePRG(0x4000, (byte)(value ^ 0xFF));
					break;
				case 0x4001:
					if (!pal16) break;
					mmc3.WritePRG(0x4001, value);
					break;
				case 0x4002:
					if (!pal16) break;
					mmc3.WritePRG(0x6001, value);
					break;
				case 0x4003:
					if (!pal16) break;
					mmc3.WritePRG(0x6000, value);
					break;

				case 0x6000:
					if(pal16) mirror_mode = (value >> 6) & 1;
					SyncMirror();
					break;
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_regs_1k[bank_1k];
				bank_1k &= chr_bank_mask;
				addr = (bank_1k << 10) | ofs;
				return Vrom[addr];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask;
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		public override void ClockPpu()
		{
			if(pal16)
				mmc3.ClockPPU();
		}

		public override void AddressPpu(int addr)
		{
			if (pal16)
				mmc3.AddressPPU(addr);
		}
	}
}