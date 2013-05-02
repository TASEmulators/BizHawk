using System;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//generally mapper2

	//Mega Man
	//Castlevania
	//Contra
	//Duck Tales
	//Metal Gear

	//TODO - look for a mirror=H UNROM--maybe there are none? this may be fixed to the board type.

	[NES.INESBoardImplPriority]
	public class UxROM : NES.NESBoardBase
	{
		//configuration
		int prg_mask;
		int vram_byte_mask;
		Func<int, int> adjust_prg;

		//state
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			adjust_prg = (x) => x;

			//configure
			switch (Cart.board_type)
			{
				case "MAPPER002":
					break;

				case "NES-UNROM": //mega man
				case "HVC-UNROM": 
				case "KONAMI-UNROM":
					AssertPrg(128); AssertChr(0); AssertVram(8);
					//AssertWram(0); //JJ - Tobidase Daisakusen Part 2 (J) includes WRAM
					break;
	
				case "HVC-UN1ROM":
					AssertPrg(128); AssertChr(0); AssertWram(0); AssertVram(8);
					adjust_prg = (x) => ((x >> 2) & 7);
					break;

				case "NES-UOROM": //paperboy 2
				case "HVC-UOROM":
					AssertPrg(256); AssertChr(0); AssertVram(8); AssertWram(0);
					break;

				default:
					return false;
			}
			//these boards always have 8KB of VRAM
			vram_byte_mask = (Cart.vram_size*1024) - 1;
			prg_mask = (Cart.prg_size / 16) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			int block = addr >> 14;
			int page = block == 1 ? prg_mask : prg;
			int ofs = addr & 0x3FFF;
			return ROM[(page << 14) | ofs];
		}
		public override void WritePRG(int addr, byte value)
		{
			prg = adjust_prg(value) & prg_mask;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VRAM[addr & vram_byte_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[addr & vram_byte_mask] = value;
			}
			else base.WritePPU(addr,value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
		}
	}
}