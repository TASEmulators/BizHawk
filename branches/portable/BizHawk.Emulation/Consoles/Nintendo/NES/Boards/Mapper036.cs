namespace BizHawk.Emulation.Consoles.Nintendo
{
	// mapper036
	// Strike Wolf (MGC-014) [!].nes
	// like an oversize GxROM
	// information from fceux
	public sealed class Mapper036 : NES.NESBoardBase
	{
		int chr;
		int prg;
		int chr_mask;
		int prg_mask;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER036":
					AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}
			chr_mask = Cart.chr_size / 8 - 1;
			prg_mask = Cart.prg_size / 32 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}

		public override void WritePRG(int addr, byte value)
		{
			// either hack emulation of a weird bus conflict, or crappy pirate safeguard
			if (addr >= 0x400 && addr <= 0x7ffe)
			{
				chr = value & 15 & chr_mask;
				prg = value >> 4 & 15 & prg_mask;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}
	}
}
