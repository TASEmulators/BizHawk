namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA MMC2 Mike Tyson's Punch-Out!!
	//AKA MMC4 (similar enough to combine in one fle)
	public sealed class PxROM_FxROM : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_4k;
		bool mmc4;

		//state
		byte prg_reg;
		IntBuffer prg_banks_8k = new IntBuffer(4);
		IntBuffer chr_banks_4k = new IntBuffer(4);
		IntBuffer chr_latches = new IntBuffer(2);

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_banks_4k", ref chr_banks_4k);
			ser.Sync("chr_latches", ref chr_latches);

			if (ser.IsReader)
				SyncPRG();
		}

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_4k.Dispose();
			chr_latches.Dispose();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER009":
					break;
				case "MAPPER010":
					mmc4 = true;
					break;

				case "NES-PNROM": //punch-out!!
				case "HVC-PEEOROM":
					AssertPrg(128); AssertChr(128); AssertWram(0); AssertVram(0);
					break;
				
				case "HVC-FKROM": //fire emblem
					mmc4 = true;
					AssertPrg(256); AssertChr(128); AssertWram(8); AssertVram(0);
					break;
				case "HVC-FJROM": //famicom wars
					mmc4 = true;
					AssertPrg(128); AssertChr(64); AssertWram(8); AssertVram(0);
					break;

				default:
					return false;
			}

			
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_4k = Cart.chr_size / 4 - 1;

			SyncPRG();

			return true;
		}

		void SyncPRG()
		{
			if (mmc4)
			{
				prg_banks_8k[0] = (prg_reg * 2) & prg_bank_mask_8k;
				prg_banks_8k[1] = (prg_reg * 2 + 1) & prg_bank_mask_8k;
				prg_banks_8k[2] = 0xFE & prg_bank_mask_8k;
				prg_banks_8k[3] = 0xFF & prg_bank_mask_8k;
			}
			else
			{
				prg_banks_8k[0] = prg_reg & prg_bank_mask_8k;
				prg_banks_8k[1] = 0xFD & prg_bank_mask_8k;
				prg_banks_8k[2] = 0xFE & prg_bank_mask_8k;
				prg_banks_8k[3] = 0xFF & prg_bank_mask_8k;
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr & 0xF000)
			{
				case 0x2000: //$A000:      PRG Reg
					prg_reg = value;
					SyncPRG();
					break;
				case 0x3000: //$B000:      CHR Reg 0A
					chr_banks_4k[0] = value & chr_bank_mask_4k;
					break;
				case 0x4000: //$C000:      CHR Reg 0B
					chr_banks_4k[1] = value & chr_bank_mask_4k;
					break;
				case 0x5000: //$D000:      CHR Reg 1A
					chr_banks_4k[2] = value & chr_bank_mask_4k;
					break;
				case 0x6000: //$E000:      CHR Reg 1B
					chr_banks_4k[3] = value & chr_bank_mask_4k;
					break;
				case 0x7000: //$F000:  [.... ...M]   Mirroring:
					SetMirrorType(value.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
					break;
			}
		}

		// same as readppu but without processing latches
		public override byte PeekPPU(int addr)
		{
			int side = addr >> 12;
			int tile = (addr >> 4) & 0xFF;
			if (addr < 0x2000)
			{
				int reg = side * 2 + chr_latches[side];
				int ofs = addr & ((1 << 12) - 1);
				int bank_4k = chr_banks_4k[reg];
				addr = (bank_4k << 12) | ofs;
				return VROM[addr];
			}
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPPU(int addr)
		{
			int side = addr>>12;
			int tile = (addr>>4)&0xFF;
			if (addr < 0x2000)
			{
				int reg = side * 2 + chr_latches[side];
				int ofs = addr & ((1 << 12) - 1);
				int bank_4k = chr_banks_4k[reg];
				addr = (bank_4k << 12) | ofs;

				//if we're grabbing the second byte of the tile, then apply the tile switching logic
				//(the next tile will be rendered with the fiddled register settings)
				if ((addr & 0xF) >= 0x8)
					switch (tile)
					{
						case 0xFD: chr_latches[side] = 0; break;
						case 0xFE: chr_latches[side] = 1; break;
					}
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}


	}
}