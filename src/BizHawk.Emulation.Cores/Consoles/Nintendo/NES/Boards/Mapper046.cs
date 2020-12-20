using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper046 : NesBoardBase
	{
		//Rumblestation 15-in-1 (Unl).nes

		/*
			Regs at $6000-7FFF means no PRG-RAM.

			$6000-7FFF:  [CCCC PPPP]   High CHR, PRG bits
			$8000-FFFF:  [.CCC ...P]   Low CHR, PRG bits
 
			'C' selects 8k CHR @ $0000
			'P' select 32k PRG @ $8000
		 */
		
		//configuration
		private int prg_bank_mask_32k, chr_bank_mask_8k;

		//state
		private int prg_bank_32k_H, prg_bank_32k_L,
			chr_bank_8k_H, chr_bank_8k_L;

		public override void WriteWram(int addr, byte value)
		{
			prg_bank_32k_H = value & 0x0F;
			chr_bank_8k_H = value >> 4;
		}

		public override void WritePrg(int addr, byte value)
		{
			prg_bank_32k_L = value & 0x01;
			chr_bank_8k_L = (value >> 4) & 0x07;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr + (((chr_bank_8k_H << 3) + chr_bank_8k_L) * 0x2000)];
			}
			else return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			//TODO: High bits
			int offset = (prg_bank_32k_H << 1) + prg_bank_32k_L;
			return Rom[addr + (offset * 0x8000)];
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER046":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);

			prg_bank_32k_H = 0;
			prg_bank_32k_L = 0;
			chr_bank_8k_H = 0;
			chr_bank_8k_L = 0;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_bank_32k_H), ref prg_bank_32k_H);
			ser.Sync(nameof(prg_bank_32k_L), ref prg_bank_32k_L);
			ser.Sync(nameof(chr_bank_8k_H), ref chr_bank_8k_H);
			ser.Sync(nameof(chr_bank_8k_L), ref chr_bank_8k_L);
		}
	}
}
