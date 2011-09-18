using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 16 & 159
	/*
		Example Games:
	--------------------------
	Dragon Ball - Dai Maou Jukkatsu      (016)
	Dragon Ball Z Gaiden                 (016)
	Dragon Ball Z 2                      (016)
	Rokudenashi Blues                    (016)
	Akuma-kun - Makai no Wana            (016)
	Dragon Ball Z - Kyoushuu! Saiya Jin  (159)
	SD Gundam Gaiden                     (159)
	Magical Taruruuto Kun 1, 2           (159)
	
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
		int prg, chr_bank_mask_1k, mirror;
		bool irq_countdown, irq_enabled, irq_asserted;
		ushort irq_counter;
		int clock_counter;

		int addr_reg;
		ByteBuffer regs = new ByteBuffer(12);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "BANDAI-FCG-1":
					AssertPrg(128); AssertChr(128); AssertWram(0); AssertVram(0);
					break;
				default:
					return false;
			}
			return true;
		}

		protected void BaseConfigure()
		{
			chr_bank_mask_1k = Cart.chr_size - 1;
			SetMirrorType(EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg * 0x8000)];
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr)
			{
				case 0: case 1: case 2: case 3:
				case 4: case 5: case 6: case 7:
					regs[addr_reg] = value;
					break;
				case 8:
					prg = value;
					break;
				case 9:
					mirror = value & 0x03;
					switch (mirror)
					{
						case 0: SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); break;
						case 1: SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal); break;
						case 2: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB); break;
					}
					break;
				case 0xA:
					irq_enabled = value.Bit(0);
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
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			//return VRAM[addr + chr];
			return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			//VRAM[addr + chr] = value;
			base.WritePPU(addr, value);
		}

		void SyncIrq()
		{
			NES.irq_cart = irq_asserted;
		}

		public override void ClockPPU()
		{
			clock_counter++;
			if (clock_counter == 3)
			{
				ClockCPU();
				clock_counter = 0;
			}
		}

		void ClockCPU()
		{
			if (!irq_countdown) return;
			irq_counter--;
			if (irq_counter == 0x0000)
			{
				irq_asserted = true;
				SyncIrq();
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("chr_bank_mask_1k", ref chr_bank_mask_1k);
			ser.Sync("mirror", ref mirror);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_countdown", ref irq_countdown);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_asserted", ref irq_asserted);
			ser.Sync("clock_counter", ref clock_counter);
		}
	}
}
