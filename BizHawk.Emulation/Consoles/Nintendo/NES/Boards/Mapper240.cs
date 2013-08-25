using System;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper240 : NES.NESBoardBase
	{
		//MHROM (mapper60) -like but wider regs (4 prg, 4 chr instead of 2 prg, 2 chr) and on EXP bus

		//configuration
		int prg_bank_mask_32k, chr_bank_mask_8k;

		//state
		int prg_bank_32k, chr_bank_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER240":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			prg_bank_32k = 0;
			chr_bank_8k = 0;

			return true;
		}


		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_bank_32k", ref prg_bank_32k);
			ser.Sync("chr_bank_8k", ref chr_bank_8k);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (chr_bank_8k * 0x2000)];
			}
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg_bank_32k * 0x8000)];
		}

		public override void WriteEXP(int addr, byte value)
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
