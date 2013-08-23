namespace BizHawk.Emulation.Consoles.Nintendo
{
	//http://wiki.nesdev.com/w/index.php/INES_Mapper_044
	public class Mapper049 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER049":
					break;
				default:
					return false;
			}

			BaseSetup();
			block = prg = 0;
			mode = false;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("block", ref block);
			ser.Sync("prg", ref prg);
			ser.Sync("mode", ref mode);
		}

		int block, prg;
		bool mode;

		public override void WriteWRAM(int addr, byte value)
		{
			if (!mmc3.wram_enable || mmc3.wram_write_protect) return;
			mode = value.Bit(0);
			prg = (value >> 4) & 3;
			block = (value >> 6) & 3;
			base.WriteWRAM(addr, value);
		}


		protected override int Get_PRGBank_8K(int addr)
		{
			if (mode) 
			  return (mmc3.Get_PRGBank_8K(addr)&0xF) + block * (128 / 8);
			int block_offset = addr >> 13;
			return prg * 4 + block_offset;
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			return (base.Get_CHRBank_1K(addr)&0x7F) + block * 128;
		}

	}
}