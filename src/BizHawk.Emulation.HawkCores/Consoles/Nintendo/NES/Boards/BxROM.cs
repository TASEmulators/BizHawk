using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA half of mapper 034 (the other half is AVE_NINA_001 which is entirely different..)
	internal sealed class BxROM : NesBoardBase
	{
		//configuration
		int prg_bank_mask_32k;
		int chr_bank_mask_8k;

		//state
		int prg_bank_32k;
		int chr_bank_8k;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_bank_32k), ref prg_bank_32k);
			ser.Sync(nameof(chr_bank_8k), ref chr_bank_8k);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "AVE-NINA-07": // wally bear and the gang
					// it's not the NINA_001 but something entirely different; actually a colordreams with VRAM
					// this actually works
					AssertPrg(32,128); AssertChr(0,16); AssertWram(0); AssertVram(0,8);
					break;

				case "IREM-BNROM": //Mashou (J).nes
				case "NES-BNROM": //Deadly Towers (U)
					AssertPrg(128,256); AssertChr(0); AssertWram(0,8); AssertVram(8);
					break;

				default:
					return false;
			}

			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;

			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override byte ReadPrg(int addr)
		{
			addr |= (prg_bank_32k << 15);
			return Rom[addr];
		}

		public override void WritePrg(int addr, byte value)
		{
			value = HandleNormalPRGConflict(addr, value);
			prg_bank_32k = value & prg_bank_mask_32k;
			chr_bank_8k = ((value >> 4) & 0xF) & chr_bank_mask_8k;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr<0x2000)
			{
				if (Vram != null)
				{
					return Vram[addr];
				}
				else
				{
					return Vrom[addr | (chr_bank_8k << 13)];
				}
			}
			else
			{
				return base.ReadPpu(addr);
			}
		}

	}
}
