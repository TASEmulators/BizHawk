using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//generally mapper 3

	//Solomon's Key
	//Arkanoid
	//Arkista's Ring
	//Bump 'n' Jump
	//Cybernoid

	public class CNROM : NES.NESBoardBase
	{
		//configuration
		int prg_mask,chr_mask;
		bool copyprotection;
		bool chr_enabled = true;
		bool bus_conflict;

		//state
		int chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "NES-CNROM": //adventure island
				case "HVC-CNROM":
					AssertPrg(16, 32); AssertChr(8,16,32);
					break;
				case "KONAMI-CNROM": //gradius (J)
					AssertPrg(32); AssertChr(32);
					break;

				default:
					return false;

			}
			if (Cart.pcb == "HVC-CNROM-256K-01")
				copyprotection = true;
			else
				copyprotection = false;
			prg_mask = (Cart.prg_size / 16) - 1;
			chr_mask = (Cart.chr_size / 8) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			bus_conflict = true;

			return true;
		}
		
		public override void WritePRG(int addr, byte value)
		{
			if (bus_conflict) value = HandleNormalPRGConflict(addr,value);
			value &= 3;
			chr = value&chr_mask;

			if (copyprotection)
			{
				if ((chr & 0x0F) > 0 && (chr != 0x13))
				{
					chr_enabled = true;
					Console.WriteLine("chr enabled");
				}
				else
				{
					chr_enabled = false;
					Console.WriteLine("chr disabled");
				}
			}
			//Console.WriteLine("at {0}, set chr={1}", NES.ppu.ppur.status.sl, chr);
		}

		public override byte ReadPPU(int addr)
		{
			if (chr_enabled == false)
			{
				chr_enabled = true;
				return 0x12;
			}
			if (addr < 0x2000)
			{
				return VROM[addr + (chr<<13)];
			}
			else return base.ReadPPU(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr",ref chr);
			ser.Sync("copyprotection", ref copyprotection);
			ser.Sync("prg_mask", ref prg_mask);
			ser.Sync("chr_mask", ref chr_mask);
			ser.Sync("bus_conflict", ref bus_conflict);
		}

	}
}