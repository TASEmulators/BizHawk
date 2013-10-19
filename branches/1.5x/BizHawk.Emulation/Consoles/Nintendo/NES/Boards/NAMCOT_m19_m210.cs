namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 19 + 210
	//210 lacks the sound and nametable control
	//I'm not sure why bootgod turned all of these into mapper 19.. 
	//some of them (example: family circuit) cannot work on mapper 19 because it clobbers nametable[0]
	//luckily, we work by board
	public sealed class NAMCOT_m19_m210 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k;
		int chr_bank_mask_1k;

		//state
		IntBuffer prg_banks_8k = new IntBuffer(4);
		IntBuffer chr_banks_1k = new IntBuffer(8);
		IntBuffer nt_banks_1k = new IntBuffer(4);
		bool[] vram_enable = new bool[2];

		int irq_counter;
		bool irq_enabled;
		int irq_cycles;
		bool irq_pending;

		Namco163Audio audio;
		int audio_cycles;

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
			nt_banks_1k.Dispose();
			if (audio != null)
			{
				audio.Dispose();
				audio = null;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_1k", ref chr_banks_1k);
			ser.Sync("nt_banks_1k", ref nt_banks_1k);
			for (int i = 0; i < 2; i++)
				ser.Sync("vram_enable_" + i, ref vram_enable[i]);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_cycles", ref irq_cycles);
			ser.Sync("irq_pending", ref irq_pending);
			SyncIRQ();
			if (audio != null)
			{
				ser.Sync("audio_cycles", ref audio_cycles);
				audio.SyncState(ser);
			}
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER019":
					break;
				case "MAPPER210":
					break;

				//mapper 19:
				case "NAMCOT-163":
					//final lap
					//battle fleet
					//dragon ninja
					//famista '90
					//hydelide 3 *this is a good test of more advanced features
					Cart.vram_size = 8; //not many test cases of this, but hydelide 3 needs it.
					AssertPrg(128,256); AssertChr(128,256); AssertVram(8); AssertWram(0,8);
					audio = new Namco163Audio();
					break;

				//mapper 210:
				case "NAMCOT-175":
					//wagyan land 2
					//splatter house
					AssertPrg(128,256); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-340":
					//family circuit '91
					//dream master
					//famista '92
					AssertPrg(128,256,512); AssertChr(128,256); AssertVram(0); AssertWram(0,8);
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size / 1 - 1;

			prg_banks_8k[3] = (byte)(0xFF & prg_bank_mask_8k);
			nt_banks_1k[0] = nt_banks_1k[2] = 0xFF;
			nt_banks_1k[1] = nt_banks_1k[3] = 0xFF;

			return true;
		}

		public override byte ReadEXP(int addr)
		{
			addr &= 0xF800;
			switch (addr)
			{
				case 0x0800:
					if (audio != null)
						return audio.ReadData();
					else
						break;
				case 0x1000:
					return (byte)(irq_counter & 0xFF);
				case 0x1800:
					return (byte)((irq_counter >> 8) | (irq_enabled ? 0x8000 : 0));
			}
			return base.ReadEXP(addr);
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr &= 0xF800;
			switch (addr)
			{
				case 0x0800:
					if (audio != null)
						audio.WriteData(value);
					break;
				case 0x1000:
					irq_counter = (irq_counter & 0xFF00) | value;
					irq_pending = false;
					SyncIRQ();
					break;
				case 0x1800:
					{
						irq_counter = (irq_counter & 0x00FF) | (((value & 0x7F) << 8));
						bool last_enabled = irq_enabled;
						irq_enabled = value.Bit(7);
						irq_pending = false;
						if (irq_enabled && !last_enabled)
						{
							irq_cycles = 3;
						}
						SyncIRQ();
						break;
					}
			}
		}


		public override void WritePRG(int addr, byte value)
		{
			addr &= 0xF800;
			switch (addr)
			{
				case 0x0000: chr_banks_1k[0] = value & chr_bank_mask_1k; break;
				case 0x0800: chr_banks_1k[1] = value & chr_bank_mask_1k; break;
				case 0x1000: chr_banks_1k[2] = value & chr_bank_mask_1k; break;
				case 0x1800: chr_banks_1k[3] = value & chr_bank_mask_1k; break;
				case 0x2000: chr_banks_1k[4] = value & chr_bank_mask_1k; break;
				case 0x2800: chr_banks_1k[5] = value & chr_bank_mask_1k; break;
				case 0x3000: chr_banks_1k[6] = value & chr_bank_mask_1k; break;
				case 0x3800: chr_banks_1k[7] = value & chr_bank_mask_1k; break;

				case 0x4000: //$C000
					nt_banks_1k[0] = value;
					break;
				case 0x4800: //$C800
					nt_banks_1k[1] = value;
					break;
				case 0x5000: //$D000
					nt_banks_1k[2] = value;
					break;
				case 0x5800: //$D800
					nt_banks_1k[3] = value;
					break;

				case 0x6000: //$E000
					prg_banks_8k[0] = (value & 0x3F) & prg_bank_mask_8k; 
					break;
				case 0x6800: //$E800
					prg_banks_8k[1] = (value & 0x3F) & prg_bank_mask_8k;
					vram_enable[0] = !value.Bit(6);
					vram_enable[1] = !value.Bit(7);
					break;
				case 0x7000: //$F000
					prg_banks_8k[2] = (value & 0x3F) & prg_bank_mask_8k; 
					break;
				case 0x7800: //$F800
					if (audio != null)
						audio.WriteAddr(value);
					break;
			}
		}


		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}


		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				//hydelide 3 is the first game i found that tests this
				VRAM[addr] = value;
			}
			else
			{
				addr -= 0x2000;
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = nt_banks_1k[bank_1k];
				if (bank_1k >= 0xE0)
				{
					int which_nt = bank_1k & 1;
					NES.CIRAM[which_nt * 0x400 + ofs] = value;
				}
				else
				{
					//throw new InvalidOperationException("what? the nametable was mapped to rom..");
					base.WritePPU(addr + 0x2000, value);
				}
			}	
		}
		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_banks_1k[bank_1k];
				if (bank_1k >= 0xE0)
				{
					//chr ram handling
					int side = addr >> 12;
					if (vram_enable[side])
					{
						bank_1k -= 0xE0;
						bank_1k &= 7; //??
						return VRAM[bank_1k * 0x400 + ofs];
					}
				}
				addr = (bank_1k << 10) | ofs;
				return VROM[addr];
			}
			else
			{
				addr -= 0x2000;
				int bank_1k = addr >> 10;
				if (bank_1k > 3) return base.ReadPPU(addr); //namco classic 2 tests this at the title screen
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = nt_banks_1k[bank_1k];
				if (bank_1k >= 0xE0)
				{
					int which_nt = bank_1k & 1;
					return NES.CIRAM[which_nt * 0x400 + ofs];
				}
				else
				{
					int chr_bank_1k = bank_1k;
					return VROM[chr_bank_1k * 0x400 + ofs];
				}
			}
		}

		void SyncIRQ()
		{
			IRQSignal = (irq_pending && irq_enabled);
		}

		void TriggerIRQ()
		{
			//NES.LogLine("trigger irq");
			irq_pending = true;
			SyncIRQ();
		}

		void ClockIRQ()
		{
			if (irq_counter == 0x7FFF)
			{
				//irq_counter = 0;
				TriggerIRQ();
			}
			else irq_counter++;
		}

		public override void ClockCPU()
		{
			if (irq_enabled)
			{
				//irq_cycles--;
				//if (irq_cycles == 0)
				//{
					//irq_cycles += 3;
					ClockIRQ();
				//}
			}
			if (audio != null)
			{
				audio_cycles++;
				if (audio_cycles == 15)
				{
					audio_cycles = 0;
					audio.Clock();
				}
			}
		}

		public override void ApplyCustomAudio(short[] samples)
		{
			if (audio != null)
				audio.ApplyCustomAudio(samples);
		}
	}
}