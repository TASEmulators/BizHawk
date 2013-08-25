namespace BizHawk.Emulation.Consoles.Nintendo
{
	//Mapper 069 is FME7 
	//or, Sunsoft-5, which is FME7 with additional sound hardware

	public sealed class Sunsoft_5 : Sunsoft_FME7
	{
		Sound.Sunsoft5BAudio audio;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "SUNSOFT-5B": //Gimmick! (J)
					AssertPrg(256); AssertChr(128); AssertWram(0); AssertVram(0); AssertBattery(false);
					break;
				default:
					return false;
			}

			BaseConfigure();
			if (NES.apu != null)
				audio = new Sound.Sunsoft5BAudio(NES.apu.ExternalQueue);

			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			int a = addr & 0xe000;
			if (a == 0x4000)
				audio.RegSelect(value);
			else if (a == 0x6000)
				audio.RegWrite(value);
			else
				base.WritePRG(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			audio.SyncState(ser);
		}

		public override void ClockCPU()
		{
			audio.Clock();
			base.ClockCPU();
		}
	}

	public class Sunsoft_FME7 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_1k, wram_bank_mask_8k;

		//state
		int addr_reg;
		ByteBuffer regs = new ByteBuffer(12);
		ByteBuffer prg_banks_8k = new ByteBuffer(4);
		int wram_bank;
		bool wram_ram_selected, wram_ram_enabled;
		ushort irq_counter;
		bool irq_countdown, irq_enabled, irq_asserted;
		int clock_counter;


		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("addr_reg", ref addr_reg);
			ser.Sync("regs", ref regs);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("wram_bank", ref wram_bank);
			ser.Sync("wram_ram_selected", ref wram_ram_selected);
			ser.Sync("wram_ram_enabled", ref wram_ram_enabled);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_countdown", ref irq_countdown);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_asserted", ref irq_asserted);
			ser.Sync("clock_counter", ref clock_counter);
			SyncIrq();
		}

		public override void Dispose()
		{
			base.Dispose();
			regs.Dispose();
			prg_banks_8k.Dispose();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "NES-JLROM": //mr gimmick!
					AssertPrg(256); AssertChr(128); AssertWram(0); AssertVram(0); AssertBattery(false);
					break;
				case "MAPPER069":
					break;
				case "SUNSOFT-5A": //Batman (J)
					AssertPrg(128); AssertChr(128); AssertWram(0); AssertVram(0); AssertBattery(false);
					break;
				case "SUNSOFT-FME-7": //Barcode World (J)
					AssertPrg(128,256); AssertChr(128,256); AssertWram(0,8); AssertVram(0);
					break;
				case "NES-BTR": //Batman - Return of the Joker (U)
					AssertPrg(128); AssertChr(256); AssertWram(8); AssertVram(0); AssertBattery(false);
					break;
				default:
					return false;
			}

			BaseConfigure();

			return true;
		}

		protected void BaseConfigure()
		{
			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			wram_bank_mask_8k = (Cart.wram_size / 8) - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;
			prg_banks_8k[3] = 0xFF;
			SetMirrorType(EMirrorType.Vertical);
		}

		void SyncPRG()
		{
			wram_ram_enabled = (regs[8] & 0x80) != 0;
			wram_ram_selected = (regs[8]&0x40)!=0;
			wram_bank = (byte)(regs[8] & 0x7F);
			for(int i=0;i<3;i++)
			{
				prg_banks_8k[i] = regs[8 + i + 1];
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0xE000;
			switch (addr)
			{
				case 0x0000: //$8000:  [.... AAAA]   Address for use with $A000
					addr_reg = value & 0xF;
					break;
				case 0x2000: //$A000:  [DDDD DDDD]   Data port
					switch(addr_reg)
					{
						case 0: case 1: case 2: case 3:
						case 4: case 5: case 6: case 7:
							regs[addr_reg] = value;
							//NES.LogLine("cr set to {0},{1},{2},{3},{4},{5},{6},{7}", regs[0], regs[1], regs[2], regs[3], regs[4], regs[5], regs[6], regs[7]);
							break;
						case 8: case 9: case 0xA: case 0xB:
							regs[addr_reg] = value;
							//NES.LogLine("pr/wr set to {0},{1},{2},{3},~0xFF~", regs[8], regs[9], regs[10], regs[11]);
							SyncPRG();
							break;
						case 0xC:
							switch (value & 3)
							{
								case 0: SetMirrorType(EMirrorType.Vertical); break;
								case 1: SetMirrorType(EMirrorType.Horizontal); break;
								case 2: SetMirrorType(EMirrorType.OneScreenA); break;
								case 3: SetMirrorType(EMirrorType.OneScreenB); break;
							}
							break;
						case 0xD:
							irq_countdown = value.Bit(7);
							irq_enabled = value.Bit(0);
							//if (value != 0) NES.LogLine("irq set to {0},{1} with value {2:x2}", irq_countdown, irq_enabled, value);
							if (!irq_enabled) irq_asserted = false;
							SyncIrq();
							break;
						case 0xE:
							irq_counter &= 0xFF00;
							irq_counter |= value;
							//NES.LogLine("irq_counter set to {0:x4}", irq_counter);
							break;
						case 0xF:
							irq_counter &= 0x00FF;
							irq_counter |= (ushort)(value << 8);
							//NES.LogLine("irq_counter set to {0:x4}", irq_counter);
							break;
						}
					break;
			}
		}

		void SyncIrq()
		{
			IRQSignal = irq_asserted;
		}

		public override void ClockCPU()
		{
			if (!irq_countdown) return;
			irq_counter--;
			if (irq_counter == 0xFFFF)
			{
				irq_asserted = true;
				SyncIrq();
			}
		}

		/*
		public override void ClockPPU()
		{
			clock_counter++;
			if (clock_counter == 3)
			{
				ClockCPU();
				clock_counter = 0;
			}
		}*/

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1<<13)-1);
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}

		int CalcWRAMAddress(int addr, int bank_mask_8k)
		{
			int ofs = addr & ((1 << 13) - 1);
			int bank_8k = wram_bank;
			bank_8k &= bank_mask_8k;
			addr = (bank_8k << 13) | ofs;
			return addr;
		}

		int CalcPPUAddress(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);
			bank_1k = regs[bank_1k];
			bank_1k &= chr_bank_mask_1k;
			return (bank_1k<<10) | ofs;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[CalcPPUAddress(addr)];
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{ }
			else base.WritePPU(addr, value);
		}

		public override byte ReadWRAM(int addr)
		{
			if (!wram_ram_selected)
			{
				addr = CalcWRAMAddress(addr, prg_bank_mask_8k);
				return ROM[addr];
			}
			else if (!wram_ram_enabled)
				return 0xFF; //empty bus
			else
			{
				addr = CalcWRAMAddress(addr, wram_bank_mask_8k);
				return WRAM[addr];
			}
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (!wram_ram_selected) return;
			else if (!wram_ram_enabled)
				return; //empty bus
			else
			{
				addr = CalcWRAMAddress(addr, wram_bank_mask_8k);
				WRAM[addr] = value;
			}
		}
	
	}
}
