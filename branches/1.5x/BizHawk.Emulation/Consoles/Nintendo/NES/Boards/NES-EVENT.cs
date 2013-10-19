namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 105
	public sealed class NES_EVENT : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_16k;

		//regenerable state
		IntBuffer prg_banks_16k = new IntBuffer(2);

		//state
		MMC1.MMC1_SerialController scnt;
		bool slot_mode, prg_mode;
		bool irq_control;
		int prg_a,prg_b;
		int init_sequence;
		bool chip_select;
		bool wram_disable;

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_16k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			
			scnt.SyncState(ser);
			ser.Sync("slot_mode", ref slot_mode);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("irq_control", ref irq_control);
			ser.Sync("prg_a", ref prg_a);
			ser.Sync("prg_b", ref prg_b);
			ser.Sync("init_sequence", ref init_sequence);
			ser.Sync("chip_select", ref chip_select);
			ser.Sync("wram_disable", ref wram_disable);

			if (ser.IsReader) Sync();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER105":
					break;
				case "NES-EVENT":
					AssertPrg(256); AssertChr(0); AssertVram(8); AssertWram(8);
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.prg_size / 16 - 1;

			SetMirrorType(EMirrorType.Vertical);

			scnt = new MMC1.MMC1_SerialController();
			scnt.WriteRegister = SerialWriteRegister;
			scnt.Reset = SerialReset;

			Sync();

			return true;
		}

		void SerialReset()
		{
			prg_mode = true;
			slot_mode = true;
		}

		void Sync()
		{
			if (init_sequence != 2)
			{
				//"use first 128k"
				prg_banks_16k[0] = 0;
				prg_banks_16k[1] = 1;
			}
			else
			{
				if (chip_select == false)
				{
					//"use first 128k"
					prg_banks_16k[0] = prg_a*2;
					prg_banks_16k[1] = prg_a*2 + 1;
				}
				else
				{
					if (prg_mode == false)
					{
						//"use second 128k"
						prg_banks_16k[0] = (prg_b>>1) + 8;
						prg_banks_16k[1] = (prg_b>>1) + 8;
					}
					else
					{
						//((these arent tested, i think...))
						if (slot_mode == false)
						{
							//"use second 128k"
							prg_banks_16k[0] = 8;
							prg_banks_16k[1] = prg_b + 8;
						}
						else
						{
							//"use second 128k"
							prg_banks_16k[0] = prg_b + 8;
							prg_banks_16k[1] = 8 + 7;
						}
					}
				}
			}

			prg_banks_16k[0] &= prg_bank_mask_16k;
			prg_banks_16k[1] &= prg_bank_mask_16k;
		}

		public override void WritePPU(int addr, byte value)
		{
			base.WritePPU(addr, value);
		}

		void SerialWriteRegister(int addr, int value)
		{
			switch (addr)
			{
				case 0: //8000-9FFF
					switch (value & 3)
					{
						case 0: SetMirrorType(EMirrorType.OneScreenA); break;
						case 1: SetMirrorType(EMirrorType.OneScreenB); break;
						case 2: SetMirrorType(EMirrorType.Vertical); break;
						case 3: SetMirrorType(EMirrorType.Horizontal); break;
					}
					slot_mode = value.Bit(2);
					prg_mode = value.Bit(3);
					Sync();
					break;
				case 1: //A000-BFFF
					{
						bool last_irq_control = irq_control;
						irq_control = value.Bit(4);
						if (init_sequence == 0)
							if (irq_control == false) init_sequence = 1; else { }
						else if (init_sequence == 1)
							if (irq_control == true) init_sequence = 2;
						chip_select = value.Bit(3);
						prg_a = (value >> 1) & 3;
						Sync();
						break;
					}
				case 2: //C000-DFFF
					//unused
					break;
				case 3: //E000-FFFF
					prg_b = value & 0xF;
					wram_disable = value.Bit(4);
					Sync();
					break;
			}
			//board.NES.LogLine("mapping.. chr_mode={0}, chr={1},{2}", chr_mode, chr_0, chr_1);
			//board.NES.LogLine("mapping.. prg_mode={0}, prg_slot{1}, prg={2}", prg_mode, prg_slot, prg);
		}

		public override void WritePRG(int addr, byte value)
		{
			scnt.Write(addr, value);
		}


		public override byte ReadPRG(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			addr = (bank_16k << 14) | ofs;
			return ROM[addr];
		}



	}
}