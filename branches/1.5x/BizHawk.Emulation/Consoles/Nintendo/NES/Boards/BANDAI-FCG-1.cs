namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 16 & 159
	/*
		Example Games:
	--------------------------
	Dragon Ball - Dai Maou Jukkatsu      (016)   Works
	Dragon Ball Z Gaiden                 (016)   Works
	Dragon Ball Z 2                      (016)   Works
	Rokudenashi Blues                    (016)   Works
	Akuma-kun - Makai no Wana            (016)   Works
	Dragon Ball Z - Kyoushuu! Saiya Jin  (159)   Works
	SD Gundam Gaiden                     (159)   Works
	Magical Taruruuto Kun 1, 2           (159)   Works
	
	PRG_ROM: 128KB
	PRG_RAM: None
	CHR-ROM: 128KB
	CHR_RAM: None
	No Batter
	Mapper controlled mirroring
	No CIC present
	*/

	public sealed class BANDAI_FCG_1 : NES.NESBoardBase 
	{
		//configuration
		int prg_bank_mask_16k, chr_bank_mask_1k;

		//regenerable state
		IntBuffer prg_banks_16k = new IntBuffer(2);

		//state
		int prg_reg_16k;
		ByteBuffer regs = new ByteBuffer(8);
		bool irq_enabled;
		ushort irq_counter;
		SEEPROM eprom;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_reg_16k", ref prg_reg_16k);
			ser.Sync("regs", ref regs);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_enabled", ref irq_enabled);
			if (eprom != null)
				eprom.SyncState(ser);
			SyncPRG();
		}

		public override void Dispose()
		{
			base.Dispose();
			regs.Dispose();
			prg_banks_16k.Dispose();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "BANDAI-FCG-1": // no eprom
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertWram(0); AssertVram(0);
					break;
				case "BANDAI-FCG-2": // no eprom
					AssertPrg(128); AssertChr(128); AssertWram(0); AssertVram(0);
					break;
				case "BANDAI-LZ93D50+24C01": // 1kbit eprom
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0); AssertVram(0);
					eprom = new SEEPROM(false);
					break;
				case "BANDAI-LZ93D50+24C02": // 2kbit eprom
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0); AssertVram(0);
					eprom = new SEEPROM(true);
					break;
					/* if implementing NES mappers, a way must be found to reliably determine which
					 * eprom variety is in use
				case "MAPPER016": // TEST TEST
					Cart.wram_size = 0;
					Cart.vram_size = 0;
					eprom = new SEEPROM(false);
					break;
					 */
				default:
					return false;
			}

			prg_bank_mask_16k = (Cart.prg_size / 16) - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;
			
			SetMirrorType(EMirrorType.Vertical);

			prg_reg_16k = 0;
			SyncPRG();

			return true;
		}

		void SyncPRG()
		{
			prg_banks_16k[0] = prg_reg_16k & prg_bank_mask_16k;
			prg_banks_16k[1] = 0xFF & prg_bank_mask_16k;
		}

		void WriteReg(int reg, byte value)
		{
			//Console.WriteLine("reg {0:X2} = {1:X2}", reg, value);
			switch (reg)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
					regs[reg] = value;
					break;
				case 8:
					//NES.LogLine("mapping PRG {0}", value);
					prg_reg_16k = value;
					SyncPRG();
					break;
				case 9:
					switch (value & 3)
					{
						case 0: SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); break;
						case 1: SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal); break;
						case 2: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB); break;
					}
					break;
				case 0xA:
					irq_enabled = value.Bit(0);
					// all write acknolwedge
					IRQSignal = false;
					break;
				case 0xB:
					irq_counter &= 0xFF00;
					irq_counter |= value;
					break;
				case 0xC:
					irq_counter &= 0x00FF;
					irq_counter |= (ushort)(value << 8);
					break;
				case 0xD:
					if (eprom != null)
						eprom.WriteByte(value);
					break;
			}
		}

		public override void WriteWRAM(int addr, byte value)
		{
			//NES.LogLine("writewram {0:X4} = {1:X2}", addr, value);
			addr &= 0xF;
			WriteReg(addr, value);
		}
		public override void WritePRG(int addr, byte value)
		{
			//NES.LogLine("writeprg {0:X4} = {1:X2}", addr, value);
			addr &= 0xF;
			WriteReg(addr, value);
		}

		public override byte ReadWRAM(int addr)
		{
			// reading any addr in 6000:7fff returns a single bit from the eeprom
			// in bit 4.
			byte ret = (byte)(NES.DB & 0xef);
			if (eprom != null && eprom.ReadBit(NES.DB.Bit(4)))
				ret |= 0x10;
			return ret;
		}

		public override void ClockCPU()
		{
			if (irq_enabled)
			{
				irq_counter--;
				if (irq_counter == 0x0000)
				{
					IRQSignal = true;
				}
			}
		}


		public override byte ReadPRG(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			addr = (bank_16k << 14) | ofs;
			return ROM[addr];
		}

		int CalcPPUAddress(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);
			bank_1k = regs[bank_1k];
			bank_1k &= chr_bank_mask_1k;
			return (bank_1k << 10) | ofs;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[CalcPPUAddress(addr)];
			else return base.ReadPPU(addr);
		}

		public override byte[] SaveRam
		{
			get
			{
				if (eprom != null)
					return eprom.GetSaveRAM();
				else
					return null;
			}
		}
	}
}
