namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper227 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_16k;

		//state
		int prg;
		bool vram_protected;
		ByteBuffer prg_banks_16k = new ByteBuffer(2);

		//1200-in-1
		//[NJXXX] Xiang Shuai Chuan Qi

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER227":
					//AssertVram(16);
					Cart.vram_size = 16;
					break;
				default:
					return false;
			}
			prg_bank_mask_16k = (Cart.prg_size / 16) - 1;

			SetMirrorType(EMirrorType.Vertical);
			vram_protected = false;
			prg_banks_16k[0] = prg_banks_16k[1] = 0;
			return true;
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

		public override void WritePRG(int addr, byte value)
		{
			bool S = addr.Bit(0);
			bool M_horz = addr.Bit(1);
			int p = (addr >> 2) & 0x1F;
			p += addr.Bit(8) ? 0x20 : 0;
			bool o = addr.Bit(7);
			bool L = addr.Bit(9);

			//virtuaNES doesnt do this.
			//fceux does it...
			//if we do it, [NJXXX] Xiang Shuai Chuan Qi will not be able to set any patterns
			//maybe only the multicarts do it, to keep the game from clobbering vram on accident
			//vram_protected = o;

			if (o == true && S == false)	
			{
				prg_banks_16k[0] = (byte)(p);
				prg_banks_16k[1] = (byte)(p);
			}
			if (o == true && S == true)
			{
				prg_banks_16k[0] = (byte)((p & ~1));
				prg_banks_16k[1] = (byte)((p & ~1) + 1);
			}
			if (o == false && S == false && L == false)
			{
				prg_banks_16k[0] = (byte)p;
				prg_banks_16k[1] = (byte)(p & 0x38);
			}
			if (o == false && S == true && L == false)
			{
				prg_banks_16k[0] = (byte)(p & 0x3E);
				prg_banks_16k[1] = (byte)(p & 0x38);
			}
			if (o == false && S == false && L == true)
			{
				prg_banks_16k[0] = (byte)p;
				prg_banks_16k[1] = (byte)(p | 0x07);
			}
			if (o == false && S == true && L == true)
			{
				prg_banks_16k[0] = (byte)(p & 0x3E);
				prg_banks_16k[1] = (byte)(p | 0x07);
			}

			prg_banks_16k[0] = (byte)(prg_banks_16k[0]&prg_bank_mask_16k);
			prg_banks_16k[1] = (byte)(prg_banks_16k[1]&prg_bank_mask_16k);

			if (M_horz) SetMirrorType(EMirrorType.Horizontal);
			else SetMirrorType(EMirrorType.Vertical);
		}


		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (vram_protected) 
					return;
				else base.WritePPU(addr, value);
			}
			else base.WritePPU(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
		}
	}
}
