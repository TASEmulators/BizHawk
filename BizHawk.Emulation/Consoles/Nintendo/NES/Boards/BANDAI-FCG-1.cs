using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 16 & 159
	/*
		Example Games:
	--------------------------
	Dragon Ball - Dai Maou Jukkatsu      (016)   Garble graphics and unplayable (Works in FCEUX)
	Dragon Ball Z Gaiden                 (016)   Works
	Dragon Ball Z 2                      (016)   Works
	Rokudenashi Blues                    (016)   Gray  (Gray in FCEUX as well, Nestopia plays it)
	Akuma-kun - Makai no Wana            (016)   Works
	Dragon Ball Z - Kyoushuu! Saiya Jin  (159)   Gray
	SD Gundam Gaiden                     (159)   Gray
	Magical Taruruuto Kun 1, 2           (159)   Gray
	
	PRG_ROM: 128KB
	PRG_RAM: None
	CHR-ROM: 128KB
	CHR_RAM: None
	No Batter
	Mapper controlled mirroring
	No CIC present
	*/

	class BANDAI_FCG_1 : NES.NESBoardBase 
	{
		//configuration
		int prg_bank_mask_16k, chr_bank_mask_1k;
		bool has_eprom = false;

		//regenerable state
		IntBuffer prg_banks_16k = new IntBuffer(2);

		//state
		int prg_reg_16k, eprom;
		ByteBuffer regs = new ByteBuffer(8);
		bool irq_enabled, irq_asserted;
		ushort irq_counter;
		int clock_counter;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_reg_16k", ref prg_reg_16k);
			ser.Sync("regs", ref regs);
			ser.Sync("eprom", ref eprom);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_asserted", ref irq_asserted);
			ser.Sync("clock_counter", ref clock_counter);

			SyncPRG();
			SyncIrq();
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
				case "BANDAI-FCG-1":
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertWram(0, 8); AssertVram(0);
					break;
				case "BANDAI-FCG-2":
					AssertPrg(128); AssertChr(128); AssertWram(0); AssertVram(0);
					break;
				case "BANDAI-LZ93D50+24C01":
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0); AssertVram(0);
					break;
				case "BANDAI-LZ93D50+24C02":
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0, 8); AssertVram(0);
					break;
				default:
					return false;
			}
			if (Cart.mapper == 159)
				has_eprom = true;

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
					if (!irq_enabled) irq_asserted = false;
					SyncIrq();
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
					eprom = value;
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

		void SyncIrq()
		{
			IRQSignal = irq_asserted;
		}

		public override void ClockCPU()
		{
			irq_counter--;
			if (irq_counter == 0x0000)
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

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) { }
			base.WritePPU(addr, value);
		}
	}
}
