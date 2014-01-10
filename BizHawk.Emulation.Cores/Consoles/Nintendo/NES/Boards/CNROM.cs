using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//generally mapper 3

	//Solomon's Key
	//Arkanoid
	//Arkista's Ring
	//Bump 'n' Jump
	//Cybernoid

	/*
	 * What's going on here?
	 * 
	 * 1. Security Diode should include all Mapper185 carts, but they aren't included?
	 * 2. Good luck ever getting bus conflicts right, considering all of the third party crap overloaded onto this mapper.
	 * 3. (Minor) Configuration shouldn't be serialized.
	 * 4. What is prg_mask?  It's obviously a mask for swapping 16K prg banks... which this board never does.
	 * 5. Like on real CNROM, the reg writes are masked so only the lowest 2 bits show.  But then:
	 * 5a. AssertChr(64); could never be handled correctly by this class.
	 * 5b. The copy protection check compares on bits of the chr reg that are always 0.  Nonsensical.
	 * 5c. The real cart's security diodes, which are connected to higher data bits, are never actually read.
	 * 6. chr_enabled is forced to true after any PPU read, which is wrong.  Probably a hack for #5; since the real security setting is never used.
	 * 7. Related to 5a; the AVE-74*161 implementation is busted.
	 */

	[NES.INESBoardImplPriority]
	public sealed class CNROM : NES.NESBoardBase
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
				case "MAPPER185":
				case "MAPPER003":
					//we assume no bus conflicts for generic unknown cases.
					//this was done originally to support Colorful Dragon (Unl) (Sachen) which bugs out if bus conflicts are emulated
					//Games which behave otherwise will force us to start entering these in the game DB
					//Licensed titles below are more likely to have used the same original discrete logic design and so suffer from the conflicts
					bus_conflict = false;
					break;
				
				case "NES-CNROM": //adventure island
				case "UNIF_NES-CNROM": // some of these should be bus_conflict = false because UNIF is bad
				case "HVC-CNROM":
					bus_conflict = true;
					AssertPrg(16, 32); AssertChr(8,16,32,64);
					break;
				case "KONAMI-CNROM": //gradius (J)
					bus_conflict = true;
					AssertPrg(32); AssertChr(32);
					break;
				case "AVE-74*161":
					bus_conflict = true;
					AssertPrg(32); AssertChr(64);
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

		public override byte ReadPRG(int addr)
		{
			if (prg_mask > 0)
			{
				return ROM[addr];
			}
			else
			{
				return ROM[addr & 0x3FFF]; //adelikat: Keeps Bird Week from crashing (only 16k PRG), but it doesn't work
			}
			
		}

	}
}