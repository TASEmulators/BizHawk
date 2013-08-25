namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 068 (and TENGEN-800042)

	//After Burner & After Burner 2
	//Maharaja

	public sealed class Sunsoft4 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask, chr_bank_mask, nt_bank_mask;

		//state
		ByteBuffer chr_regs_2k = new ByteBuffer(4);
		ByteBuffer nt_regs = new ByteBuffer(2);
		ByteBuffer prg_regs_16k = new ByteBuffer(2);
		bool flag_m, flag_r;

		public override void Dispose()
		{
			base.Dispose();
			chr_regs_2k.Dispose();
			nt_regs.Dispose();
			prg_regs_16k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr_regs_2k", ref chr_regs_2k);
			ser.Sync("nt_regs", ref nt_regs);
			ser.Sync("prg_regs_16k", ref prg_regs_16k);
			ser.Sync("flag_m", ref flag_m);
			ser.Sync("flag_r", ref flag_r);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER068":
					break;
				case "TENGEN-800042":
					AssertPrg(128); AssertChr(256); AssertVram(0); AssertWram(0);
					break;
				case "SUNSOFT-4":
					AssertPrg(128); AssertChr(128,256); AssertVram(0); AssertWram(0,8); 
					break;
				case "UNIF_NES-NTBROM":
					AssertPrg(128 + 16); AssertChr(128); Cart.wram_size = 8; Cart.vram_size = 0;
					/* The actual cart had 128k prg, with a small slot on the top that can load an optional daughterboard.
					 * The UNIF dump has this as an extra 16k prg lump.  I don't know how this lump is actually used,
					 * though.
					 */
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);
			prg_regs_16k[1] = 0xFF;
			prg_bank_mask = Cart.prg_size / 16 - 1;
			if (Cart.prg_size == 128 + 16)
				prg_bank_mask = 7; // ignore extra prg lump
			chr_bank_mask = Cart.chr_size / 2 - 1;
			nt_bank_mask = Cart.chr_size - 1;
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_regs_16k[bank_16k];
			bank_16k &= prg_bank_mask;
			addr = (bank_16k << 14) | ofs;
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				//chr comes from normal chr mapping
				int bank_2k = addr >> 11;
				int ofs = addr & ((1 << 11) - 1);
				bank_2k = chr_regs_2k[bank_2k];
				bank_2k &= chr_bank_mask;
				addr = (bank_2k << 11) | ofs;
				return VROM[addr];
			}
			else
			{
				//nametable may come from "NT-ROM"
				//which means from extra CHR data starting at bank 0x80
				if (flag_r)
				{
					addr = ApplyMirroring(addr);
					int bank_1k = (addr >> 10) & 3;
					int ofs = addr & ((1 << 10) - 1);
					bank_1k = nt_regs[bank_1k] + 0x80;
					bank_1k &= nt_bank_mask;
					addr = (bank_1k << 10) | ofs;
					return VROM[addr];
				}
				else return base.ReadPPU(addr);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("W{0:x4} {1:x2}", addr + 0x8000, value);
			switch (addr & 0xF000)
			{
				case 0x0000: //$8000
					chr_regs_2k[0] = value;
					break;
				case 0x1000: //$9000
					chr_regs_2k[1] = value;
					break;
				case 0x2000: //$A000
					chr_regs_2k[2] = value;
					break;
				case 0x3000: //$B000
					chr_regs_2k[3] = value;
					break;
				case 0x4000: //$C000
					nt_regs[0] = (byte)(value & 0x7F);
					break;
				case 0x5000: //$D000
					nt_regs[1] = (byte)(value & 0x7F);
					break;
				case 0x6000: //$E000
					flag_m = (value & 1) != 0;
					flag_r = ((value >> 4) & 1) != 0;
					if (flag_m) SetMirrorType(EMirrorType.Horizontal);
					else SetMirrorType(EMirrorType.Vertical);
					break;
				case 0x7000: //$F000
					prg_regs_16k[0] = value;
					break;
			}
		}
	}
}