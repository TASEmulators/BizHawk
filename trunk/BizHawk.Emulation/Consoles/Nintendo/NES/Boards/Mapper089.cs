using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA Tenka no Goikenban: Mito Koumon
	class MAPPER89 : NES.NESBoardBase
	{
		int chr;
		int prg_bank_mask_16k;
		byte prg_bank_16k;
		ByteBuffer prg_banks_16k = new ByteBuffer(2);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "SUNSOFT-2":
					break;
				default:
					return false;
			}

			//yes, the board type SUNSOFT-2 has a pcb type of SUNSOFT-3
			//(the pcb SUNSOFT-3R has a different revision of SUNSOFT-2 chip which works differently)
			//and all these are different than the SUNSOFT-3 chip
			if (Cart.pcb != "SUNSOFT-3") return false;  

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			prg_bank_mask_16k = (Cart.prg_size / 16) - 1;
			prg_banks_16k[1] = 0xFF;
			return true;
		}

		public override void Dispose()
		{
			prg_banks_16k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg_bank_mask_16k", ref prg_bank_mask_16k);
			ser.Sync("prg_bank_16k", ref prg_bank_16k);
			ser.Sync("prg_banks_16k", ref prg_banks_16k);
		}

		void SyncPRG()
		{
			prg_banks_16k[0] = prg_bank_16k;
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_bank_16k = (byte)((value >> 4) & 7);
				SyncPRG();
			
			if (value.Bit(3))
				SetMirrorType(EMirrorType.OneScreenA);
			else
				SetMirrorType(EMirrorType.OneScreenB);

			chr = ((value & 0x07) + ((value >> 7) * 0x08));
		}

		public override byte ReadPRG(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			bank_16k &= prg_bank_mask_16k;
			addr = (bank_16k << 14) | ofs;
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPPU(addr);
		}
	}
}
