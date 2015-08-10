#region Disch's Notes
/*
 *  Here are Disch's original notes:  
 ========================
 =  Mapper 105          =
 ========================
 
 aka
 --------------------------
 NES-EVENT
 
 
 Example Game:
 --------------------------
 Nintendo World Championships 1990
 
 
 Notes:
 ---------------------------
 This mapper is an MMC1 with crazy wiring and a huge 30-bit CPU cycle driven IRQ counter.  Registers are all
 internal and not directly accessable -- and the latch must be written to 1 bit at a time -- just like on a
 normal MMC1.  For details on how regs are written to, see mapper 001.
 
 This mapper has 8k CHR-RAM, and it is not swappable.
 
 
 Registers:
 ---------------------------
 
 Note that like a normal MMC1, registers are internal and not accessed directly.
 
 
   $8000-9FFF:   [.... PSMM]  Same as MMC1 (but CHR mode bit isn't used)
 
   $A000-BFFF:   [...I OAA.]
        I = IRQ control / initialization toggle
        O = PRG Mode/Chip select
        A = PRG Reg 'A'
 
   $C000-DFFF:   [.... ....]  Unused
 
   $E000-FFFF:   [...W BBBB]
        W = WRAM disable (same as MMC1)
        B = PRG Reg 'B'
 
 
 
 Powerup / Reset / Initialization:
 ---------------------------
 
   On powerup and reset, the first 32k of PRG (from the first PRG chip) is selected at $8000 *no matter what*.
 PRG cannot be swapped until the mapper has been "initialized" by setting the 'I' bit to 0, then to '1'.  This
 toggling will "unlock" PRG swapping on the mapper.
 
   Note 'I' also controls the IRQ counter (see below)
 
 
 PRG Setup:
 ---------------------------
 
   There are 2 PRG chips, each 128k.  The 'O' bit selects between the chips, and also determines which PRG Reg
 is used to select the page.
 
   O=0:  Use first PRG chip (first 128k), use 'A' PRG Reg, 32k swap
   O=1:  Use second PRG chip (second 128k), use 'B' PRG Reg, MMC1 style swap
 
   In addition, if the mapper has not been "unlocked", the first 32k of the first chip is always selected
 regardless (as if $A000 contained $00).
 
   Modes as listed below:
 
                   $8000   $A000   $C000   $E000
                 +-------------------------------+
 Uninitialized:  |             { 0 }             |  <-- use first 128k
                 +-------------------------------+
 O=0:            |             $A000             |  <-- use first 128k
                 +-------------------------------+
 O=1, P=0:       |            <$E000>            |  <-- use second 128k
                 +-------------------------------+
 O=1, P=1, S=0:  |     { 0 }     |     $E000     |  <-- use second 128k
                 +---------------+---------------+
 O=1, P=1, S=1:  |     $E000     |     {$07}     |  <-- use second 128k
                 +---------------+---------------+
 
 
 
 
 IRQ Counter:
 ---------------------------
 
   The 'I' bit in $A000 controls the IRQ counter.  When cleared, the IRQ counter counts up every cycle.  When
 set, the IRQ counter is reset to 0 and stays there (does not count), and the pending IRQ is acknowledged.
 
   The cart has 4 dipswitches which control how high the counter must reach for an IRQ to be generated.
 
   The IRQ counter is 30 bits wide.. when it reaches the following value, an IRQ is fired:
 
   [1D CBAx xxxx xxxx xxxx xxxx xxxx xxxx]
     ^ ^^^
     | |||
     either 0 or 1, depending on the corresponding dipswitch.
 
 So if all dipswitches are open (use '0' above), the counter must reach $20000000.
 If all dipswitches are closed (use '1' above), the counter must reach $3E000000.
 etc
 
   In the official tournament, 'C' was closed, and the others were open, so the counter had to reach $2800000.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
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
		bool c000_swappable, prg_32k_mode;
		bool irq_enable;
		int prg_a, prg_b;
		int init_sequence;
		bool chip_select;
		bool wram_disable;

		int irq_count;
		int irq_destination;
		bool irq_pending;

		[MapperProp]
		public bool Dipswitch1 = false;

		[MapperProp]
		public bool Dipswitch2 = true;

		[MapperProp]
		public bool Dipswitch3 = false;

		[MapperProp]
		public bool Dipswitch4 = false;

		private List<bool> Switches
		{
			get
			{
				return new List<bool>
				{
					{ Dipswitch1 },
					{ Dipswitch2 },
					{ Dipswitch3 },
					{ Dipswitch4 }
				};
			}
		}

		public int IrqDestination
		{
			get
			{
				SyncIRQDestination();
				return irq_destination;
			}
		}

		private void SyncIRQDestination()
		{
			//0b001D_CBAx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx

			int val = 0;
			for (int i = 0; i < Switches.Count; i++)
			{
				val <<= 1;
				if (Switches[i])
				{
					val |= 1;
				}
			}

			irq_destination = 0x20000000 | (val << 25);
		}

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_16k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);

			scnt.SyncState(ser);
			ser.Sync("c000_swappable", ref c000_swappable);
			ser.Sync("prg_16k_mode", ref prg_32k_mode);
			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_count", ref irq_count);
			ser.Sync("prg_a", ref prg_a);
			ser.Sync("prg_b", ref prg_b);
			ser.Sync("init_sequence", ref init_sequence);
			ser.Sync("chip_select", ref chip_select);
			ser.Sync("wram_disable", ref wram_disable);
			ser.Sync("prg_banks_16k", ref prg_banks_16k);
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
			init_sequence = 0;

			SetMirrorType(EMirrorType.Vertical);

			scnt = new MMC1.MMC1_SerialController();
			scnt.WriteRegister = SerialWriteRegister;
			scnt.Reset = SerialReset;

			InitValues();

			return true;
		}

		void SerialReset()
		{
			prg_32k_mode = true;
			c000_swappable = true;
		}

		void Sync()
		{
			SyncIRQDestination();
			SyncIRQ();

			if (init_sequence != 2)
			{
				// prg banks locked to first 32k of first 128k chip
				prg_banks_16k[0] = 0;
				prg_banks_16k[1] = 1;
			}
			else
			{
				if (!chip_select)
				{
					//use prg banks in first 128k as indicated by prg_a reg
					prg_banks_16k[0] = prg_a * 2;
					prg_banks_16k[1] = prg_a * 2 + 1;
				}
				else
				{
					if (!prg_32k_mode)
					{
						//use prg banks in second 128k (add 8*16k as offset) in 32k mode, as determined by prg_b reg
						prg_banks_16k[0] = ((prg_b & ~1) & 7) + 8;
						prg_banks_16k[1] = ((prg_b & ~1) & 7) + 9;
					}
					else
					{
					//((these arent tested, i think...))
						//"use second 128k"
						if (!c000_swappable)
						{
							prg_banks_16k[0] = 8;
							prg_banks_16k[1] = prg_b + 8;
						}
						else
						{
							prg_banks_16k[0] = prg_b + 8;
							prg_banks_16k[1] = 15; //last bank of second 128k
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
					c000_swappable = value.Bit(2);
					prg_32k_mode = value.Bit(3);
					Sync();
					break;
				case 1: //A000-BFFF
					{
						irq_enable = !value.Bit(4);

						//Acknowledge IRQ
						if (!irq_enable)
						{
							irq_count = 0;
							irq_pending = false;
						}

						if (init_sequence == 0 && irq_enable)
						{
							init_sequence = 1;
						}
						else if  (init_sequence == 1 && !irq_enable)
						{
							init_sequence = 2;
						}

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

		public override void WriteWRAM(int addr, byte value)
		{
			if (!wram_disable)
			{
				base.WriteWRAM(addr, value);
			}
		}

		public override byte ReadWRAM(int addr)
		{
			return wram_disable ? NES.DB : base.ReadWRAM(addr);
		}

		public override void NESSoftReset()
		{
			InitValues();
			base.NESSoftReset();
		}

		private void InitValues()
		{
			AutoMapperProps.Apply(this);

			irq_enable = false;
			init_sequence = 0;
			irq_count = 0;

			Sync();
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

		public override void ClockCPU()
		{
			if (irq_enable)
			{
				ClockIRQ();
			}
		}

		private void ClockIRQ()
		{
			irq_count++;
			if (irq_count >= irq_destination)
			{
				irq_enable = false;
				irq_pending = true;
			}

			SyncIRQ();
		}

		private void SyncIRQ()
		{
			SyncIRQ(irq_pending);
		}
	}
}