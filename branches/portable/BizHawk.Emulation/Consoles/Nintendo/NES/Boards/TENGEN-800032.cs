namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 64
	public class TENGEN_800032 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k;
		int chr_bank_mask_1k;

		//regenerable state
		IntBuffer prg_banks_8k = new IntBuffer(4);
		IntBuffer chr_banks_1k = new IntBuffer(8);
		//state
		IntBuffer regs = new IntBuffer(16);
		int address;
		bool chr_1k, chr_mode, prg_mode;
		//irq
		int irq_countdown;
		int a12_old;
		int irq_reload, irq_counter;
		bool irq_pending, irq_enable;
		bool irq_mode;
		bool irq_reload_pending;
		int separator_counter;


		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
			regs.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("regs", ref regs);
			ser.Sync("address", ref address);
			ser.Sync("chr_1k", ref chr_1k);
			ser.Sync("chr_mode", ref chr_mode);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("irq_countdown", ref irq_countdown);
			ser.Sync("a12_old", ref a12_old);
			ser.Sync("irq_reload", ref irq_reload);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("irq_mode", ref irq_mode);
			ser.Sync("irq_reload_pending", ref irq_reload_pending);
			ser.Sync("separator_counter", ref separator_counter);

			Sync();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER064":
					break;
				case "TENGEN-800032":
					AssertPrg(64, 128); AssertChr(64, 128); AssertVram(0); AssertWram(00);
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size / 1 - 1;

			SetMirrorType(EMirrorType.Vertical);

			Sync();

			return true;
		}

		void Sync()
		{
			SyncIRQ();
	
			if (prg_mode)
			{
				prg_banks_8k[0] = regs[0xF] & prg_bank_mask_8k;
				prg_banks_8k[1] = regs[0x6] & prg_bank_mask_8k;
				prg_banks_8k[2] = regs[0x7] & prg_bank_mask_8k;
				prg_banks_8k[3] = 0xFF & prg_bank_mask_8k;
			}
			else
			{
				prg_banks_8k[0] = regs[0x6] & prg_bank_mask_8k;
				prg_banks_8k[1] = regs[0x7] & prg_bank_mask_8k;
				prg_banks_8k[2] = regs[0xF] & prg_bank_mask_8k;
				prg_banks_8k[3] = 0xFF & prg_bank_mask_8k;
			}

			if (chr_mode)
			{
				chr_banks_1k[0] = regs[0x2] & chr_bank_mask_1k;
				chr_banks_1k[1] = regs[0x3] & chr_bank_mask_1k;
				chr_banks_1k[2] = regs[0x4] & chr_bank_mask_1k;
				chr_banks_1k[3] = regs[0x5] & chr_bank_mask_1k;
				if (chr_1k)
				{
					chr_banks_1k[4] = regs[0x0] & chr_bank_mask_1k;
					chr_banks_1k[5] = regs[0x8] & chr_bank_mask_1k;
					chr_banks_1k[6] = regs[0x1] & chr_bank_mask_1k;
					chr_banks_1k[7] = regs[0x9] & chr_bank_mask_1k;
				}
				else
				{
					chr_banks_1k[4] = ((regs[0x0] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[5] = ((regs[0x0] & 0xFE) + 1) & chr_bank_mask_1k;
					chr_banks_1k[6] = ((regs[0x1] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[7] = ((regs[0x1] & 0xFE) + 1) & chr_bank_mask_1k;
				}
			}
			else
			{
				chr_banks_1k[4] = regs[0x2] & chr_bank_mask_1k;
				chr_banks_1k[5] = regs[0x3] & chr_bank_mask_1k;
				chr_banks_1k[6] = regs[0x4] & chr_bank_mask_1k;
				chr_banks_1k[7] = regs[0x5] & chr_bank_mask_1k;
				if (chr_1k)
				{
					chr_banks_1k[0] = regs[0x0] & chr_bank_mask_1k;
					chr_banks_1k[1] = regs[0x8] & chr_bank_mask_1k;
					chr_banks_1k[2] = regs[0x1] & chr_bank_mask_1k;
					chr_banks_1k[3] = regs[0x9] & chr_bank_mask_1k;
				}
				else
				{
					chr_banks_1k[0] = ((regs[0x0] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[1] = ((regs[0x0] & 0xFE) + 1) & chr_bank_mask_1k;
					chr_banks_1k[2] = ((regs[0x1] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[3] = ((regs[0x1] & 0xFE) + 1) & chr_bank_mask_1k;
				}
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("mapping {0:X4} = {1:X2}", addr, value);
			addr &= 0xE001;
			switch (addr)
			{
				case 0x0000:
					address = value & 0xF;
					chr_1k = value.Bit(5);
					prg_mode = value.Bit(6);
					chr_mode = value.Bit(7);
					Sync();
					break;

				case 0x0001: //data port
					regs[address] = value;
					Sync();
					break;

				case 0x2000:
					if (value.Bit(0)) SetMirrorType(EMirrorType.Horizontal);
					else SetMirrorType(EMirrorType.Vertical);
					break;

				case 0x4000:
					irq_reload = value;
					break;

				case 0x4001:
					irq_mode = value.Bit(0);
					if (irq_mode) irq_countdown = 12;
					irq_reload_pending = true;
					break;

				case 0x6000:
					irq_enable = false;
					irq_pending = false;
					SyncIRQ();
					break;
				case 0x6001:
					irq_enable = true;
					SyncIRQ();
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


		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_banks_1k[bank_1k];
				addr = (bank_1k << 10) | ofs;
				return VROM[addr];
			}
			else
				return base.ReadPPU(addr);
		}

		void SyncIRQ()
		{
			IRQSignal = irq_pending;
		}

		void ClockIRQ()
		{
			if (irq_reload_pending)
			{
				irq_counter = irq_reload + 1;
				irq_reload_pending = false;
			}
			else if (irq_counter == 0)
			{
				irq_counter = irq_reload;
			}
			else
			{
				irq_counter--;
				if (irq_counter == 0 && irq_enable)
				{
					irq_pending = true;
					SyncIRQ();
				}
			}
		}

		public override void ClockPPU()
		{
			if (separator_counter > 0)
				separator_counter--;

			if (irq_countdown > 0)
			{
				irq_countdown--;
				if (irq_countdown == 0)
				{
					ClockIRQ();
				}
			}
		}

		public override void AddressPPU(int addr)
		{
			int a12 = (addr >> 12) & 1;
			bool rising_edge = (a12 == 1 && a12_old == 0);
			if (rising_edge)
			{
				if (separator_counter > 0)
				{
					separator_counter = 15;
				}
				else
				{
					separator_counter = 15;
					irq_countdown = 11;
				}
			}

			a12_old = a12;
		}

	}
}