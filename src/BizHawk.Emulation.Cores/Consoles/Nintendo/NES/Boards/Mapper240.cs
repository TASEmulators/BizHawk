using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper240 : NesBoardBase
	{
		//MHROM (mapper60) -like but wider regs (4 prg, 4 chr instead of 2 prg, 2 chr) and on EXP bus

		//configuration
		private int prg_bank_mask_32k, chr_bank_mask_8k;

		//state
		private int prg_bank_32k, chr_bank_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER240":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);

			prg_bank_32k = 0;
			chr_bank_8k = 0;

			return true;
		}


		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_bank_32k), ref prg_bank_32k);
			ser.Sync(nameof(chr_bank_8k), ref chr_bank_8k);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr + (chr_bank_8k * 0x2000)];
			}
			else return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr + (prg_bank_32k * 0x8000)];
		}

		public override void WriteExp(int addr, byte value)
		{
			//if (ROM != null && bus_conflict) value = HandleNormalPRGConflict(addr, value);
			Console.WriteLine("{0:x4} = {1:x2}", addr + 0x4000, value);
			prg_bank_32k = (value >> 4) & 0xF;
			chr_bank_8k = (value) & 0xF;
			prg_bank_32k &= prg_bank_mask_32k;
			chr_bank_mask_8k &= chr_bank_mask_8k;
		}

	}
}
