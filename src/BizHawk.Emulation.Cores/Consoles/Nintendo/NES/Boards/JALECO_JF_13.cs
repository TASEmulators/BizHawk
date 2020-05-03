using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//Mapper 86
	
	//Example Games:
	//--------------------------
	//Moero!! Pro Yakyuu (Black)
	//Moero!! Pro Yakyuu (Red)

	internal sealed class JALECO_JF_13 : NesBoardBase
	{
		//configuration
		int prg_bank_mask_32k;
		int chr_bank_mask_8k;

		//state
		int chr;
		int prg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER086":
					break;
				case "JALECO-JF-13":
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
			if (addr < 0x8000)
				return Rom[addr + (prg * 0x8000)];
			else
				return base.ReadPrg(addr);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPpu(addr);
		}

		public override void WriteWram(int addr, byte value)
		{
			switch (addr & 0x1000)
			{
				case 0x0000:
					prg = (value >> 4) & 3;
					prg &= prg_bank_mask_32k;
					chr = (value & 3) + ((value >> 4) & 0x04);
					chr &= chr_bank_mask_8k;
					break;
				case 0x1000:
					//sound regs
					break;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
		}
	}
}
