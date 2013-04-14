namespace BizHawk.Emulation.Consoles.Nintendo
{
	//Mapper 77
	//Napoleon Senki

	//the 4screen implementation is a bit of a guess, but it seems to work

	class IREM_74_161_161_21_138 : NES.NESBoardBase
	{
		int chr, prg;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER077":
					break;
				case "IREM-74*161/161/21/138":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}

		public override void WritePRG(int addr, byte value)
		{
			chr = (value >> 4) & 0x0F;
			prg = value & 0x0F;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x0800)
				return VROM[addr + (chr * 0x0800)];
			else if (addr < 0x2000)
				return VRAM[addr];
			else if (addr < 0x2800)
				return VRAM[addr & 0x7ff];
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x0800)
				return;
			else if (addr < 0x2000)
				VRAM[addr] = value;
			else if (addr < 0x2800)
				VRAM[addr & 0x7ff] = value;
			else base.WritePPU(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x8000)
				return ROM[addr + (prg * 0x8000)];
			else
				return base.ReadPRG(addr); 
		}
	}
}
