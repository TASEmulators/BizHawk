namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper164 : NES.NESBoardBase 
	{
		/*
		* Here are Disch's original notes:  
		========================
		=  Mapper 164          =
		========================
 
		Example Game:
		--------------------------
		Final Fantasy V
 
 
 
		Registers:
		---------------------------
 
		Range,Mask:   $5000-FFFF, $F300
 
		$5000, $D000:  PRG reg (32k @ $8000)
 
		$6000-7FFF may have SRAM (not sure)
 
 
		On Reset
		---------------------------
		Reg seems to contain $FF on powerup/reset
 
 
		Notes:
		---------------------------
 
		Swapping is really simple -- the thing that is funky is the register range/mask.  $5000 and $D000 will access
		the register, however $5100, $5200, etc will not.
		*/

		int prg_bank;
		int prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER164":
					break;
				default:
					return false;
			}
			prg_bank = 0xFF;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr = (addr + 0x4000) & 0xF300;
			if (addr == 0x5000 || addr == 0xD000)
				prg_bank = value;
		}

		public override void WritePRG(int addr, byte value)
		{
			addr = (addr + 0x8000) & 0xF300;
			if (addr == 0x5000 || addr == 0xD000)
				prg_bank = value;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + ((prg_bank & prg_bank_mask_32k) * 0x8000)];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg_bank);
		}

		public override void NESSoftReset()
		{
			prg_bank = 0xFF;
			base.NESSoftReset();
		}
	}
}
