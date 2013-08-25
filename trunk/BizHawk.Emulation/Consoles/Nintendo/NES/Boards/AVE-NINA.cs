namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA half of mapper 034 (the other half is BxROM which is entirely different..)
	public sealed class AVE_NINA_001 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_32k, chr_bank_mask_4k;

		//state
		IntBuffer chr_banks_4k = new IntBuffer(2);
		int prg_bank_32k;

		public override void Dispose()
		{
			base.Dispose();
			chr_banks_4k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr_banks_4k", ref chr_banks_4k);
			ser.Sync("prg_bank_32k", ref prg_bank_32k);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "AVE-NINA-01": //Impossible Mission 2 (U)
					AssertPrg(64); AssertChr(64); AssertWram(8); AssertVram(0);
					break;

				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_4k = Cart.chr_size / 4 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_4k = addr >> 12;
				int ofs = addr & ((1 << 12) - 1);
				bank_4k = chr_banks_4k[bank_4k];
				addr = (bank_4k << 12) | ofs;
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			addr |= (prg_bank_32k << 15);
			return ROM[addr];	
		}

		public override void WriteWRAM(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1FFD: //$7FFD:   Select 32k PRG @ $8000
					prg_bank_32k = value;
					prg_bank_32k &= prg_bank_mask_32k;
					break;
				case 0x1FFE:
					chr_banks_4k[0] = value;
					chr_banks_4k[0] &= chr_bank_mask_4k;
					break;
				case 0x1FFF:
					chr_banks_4k[1] = value;
					chr_banks_4k[1] &= chr_bank_mask_4k;
					break;
				default:
					//apparently these regs are patched in over the WRAM..
					base.WriteWRAM(addr,value);
					break;
			}
		}

	}

	//AKA mapper 079
	//historically, mapper 113 is confused with this, but I can't find any need for mapper 113. bootgod's db has nothing for mapper 113
	class AVE_NINA_006 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_32k, chr_bank_mask_8k;
		//bool mirror_control_enabled;

		//state
		int chr_bank_8k, prg_bank_32k;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr_bank_8k", ref chr_bank_8k);
			ser.Sync("prg_bank_32k", ref prg_bank_32k);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER079":
					break;
				case "MAPPER113":
					break;
				case "AVE-NINA-06": //Blackjack (U)
				case "AVE-NINA-03": //F-15 City War (U)
				case "AVE-MB-91": //Deathbots (U)
					AssertPrg(32, 64); AssertChr(32, 64); AssertWram(0); AssertVram(0);
					break;

				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			prg_bank_32k = 0;

			return true;
		}

		//FCEUX responds to this for PRG writes as well.. ?
		public override void WriteEXP(int addr, byte value)
		{
			addr &= 0x4100;
			switch (addr)
			{
				case 0x0100: //$4100:  [.CPP PCCC]
					chr_bank_8k = (value & 7) | ((value >> 3) & 0x8);
					chr_bank_8k &= chr_bank_mask_8k;
					prg_bank_32k = ((value >> 3) & 7);
					prg_bank_32k &= prg_bank_mask_32k;
					//if (mirror_control_enabled) SetMirrorType(value.Bit(7) ? EMirrorType.Vertical : EMirrorType.Horizontal);
					//NES.LogLine("chr={0:X2}, prg={1:X2}, with val={2:X2}", chr_reg, prg_reg, value);
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			addr |= (prg_bank_32k << 15);
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr |= (chr_bank_8k << 13);
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}


	}

}
