using System.Diagnostics;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//mapper 011

	//Crystal Mines
	//Metal Fighter

	[NesBoardImplPriority]
	internal sealed class IC_74x377 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_32k, chr_bank_mask_8k;
		private bool bus_conflict = true;
		private bool bus_conflict_50282 = false;

		//state
		private int prg_bank_32k, chr_bank_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER011":
					break;
				case "MAPPER011_HACKY":
					bus_conflict = false;
					break;
				case "Discrete_74x377-FLEX":
					break;
				case "COLORDREAMS-74*377":
					AssertPrg(32, 64, 128); AssertChr(16, 32, 64, 128); AssertVram(0); AssertWram(0);
					break;
				case "AGCI-47516":
					break;
				case "AGCI-50282": // death race
				case "MAPPER144":
					bus_conflict_50282 = true;
					bus_conflict = false;
					break;
				default:
					return false;
			}
			AssertPrg(32, 64, 128);
			AssertChr(8, 16, 32, 64, 128);

			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;

			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}
		public override byte ReadPrg(int addr)
		{
			return Rom[addr + (prg_bank_32k << 15)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr + (chr_bank_8k << 13)];
			}
			else return base.ReadPpu(addr);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (bus_conflict_50282)
			{
				// this is what fceux does
				//if (addr == 0)
				//	return;
				// this is what nesdev wiki does. seems to give same results as above?
				value = (byte)((value | 1) & ReadPrg(addr));
			}
			if (bus_conflict)
			{
				byte old_value = value;
				value &= ReadPrg(addr);
				//Bible Adventures (Unl) (V1.3) [o1].nes will exercise this bus conflict, but not really test it. (works without bus conflict emulation
				Debug.Assert(old_value == value, "Found a test case of Discrete_74x377 bus conflict. please report.");
			}

			prg_bank_32k = (value & 3) & prg_bank_mask_32k;
			chr_bank_8k = ((value >> 4) & 0xF) & chr_bank_mask_8k;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_bank_32k), ref prg_bank_32k);
			ser.Sync(nameof(chr_bank_8k), ref chr_bank_8k);
		}
	}
}