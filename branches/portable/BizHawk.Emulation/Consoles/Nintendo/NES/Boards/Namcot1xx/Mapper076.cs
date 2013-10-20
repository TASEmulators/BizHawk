namespace BizHawk.Emulation.Consoles.Nintendo
{
	//aka NAMCOT-3446
	public sealed class Mapper076 : Namcot108Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NAMCOT-3446": //Megami Tensei: Digital Devil Story
				case "MAPPER076":
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		int RewireCHR(int addr)
		{
			int mapper_addr = addr >> 1;
			int bank_1k = mapper.Get_CHRBank_1K(mapper_addr + 0x1000);
			int ofs = addr & ((1 << 11) - 1);
			return (bank_1k << 11) + ofs;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000) return VROM[RewireCHR(addr)];
			else return base.ReadPPU(addr);
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) { }
			else base.WritePPU(addr, value);
		}
	}
}