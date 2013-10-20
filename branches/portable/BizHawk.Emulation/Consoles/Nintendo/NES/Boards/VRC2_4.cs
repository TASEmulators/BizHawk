//TODO - for chr, refactor to use 8 registers of 8 bits instead of 16 registers of 4 bits. more realistic, less weird code.

using System;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//mapper 21 + 22 + 23 + 25 (docs largely in 021.txt for VRC4 and 22.txt for VRC2)
	//If you change any of the IRQ logic here, be sure to change it in VRC 3/6/7 as well.
	public sealed class VRC2_4 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_1k;
		int prg_reg_mask_8k;
		Func<int, int> remap;
		Func<int, int> fix_chr;
		int type;
		bool latch6k_exists = false;
		bool extrabig_chr = false;

		//state
		public int[] prg_bank_reg_8k = new int[2];
		public int[] chr_bank_reg_1k = new int[16];
		bool prg_mode;
		ByteBuffer prg_banks_8k = new ByteBuffer(4);
		public IntBuffer chr_banks_1k = new IntBuffer(8);
		bool irq_mode;
		bool irq_enabled, irq_pending, irq_autoen;
		byte irq_reload;
		byte irq_counter;
		int irq_prescaler;
		public int extra_vrom;
		int latch6k_value;

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			for (int i = 0; i < 2; i++) ser.Sync("prg_bank_reg_8k_" + i, ref prg_bank_reg_8k[i]);
			for (int i = 0; i < 16; i++) ser.Sync("chr_bank_reg_1k_" + i, ref chr_bank_reg_1k[i]);
			ser.Sync("irq_mode", ref irq_mode);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_autoen", ref irq_autoen);
			ser.Sync("irq_reload", ref irq_reload);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_prescaler", ref irq_prescaler);
			ser.Sync("extra_vrom", ref extra_vrom);
			if (latch6k_exists)
				ser.Sync("latch6k_value", ref latch6k_value);
			SyncPRG();
			SyncCHR();
			SyncIRQ();
		}

		void SyncPRG()
		{
			if (prg_mode)
			{
				prg_banks_8k[0] = 0xFE;
				prg_banks_8k[1] = (byte)(prg_bank_reg_8k[1]);
				prg_banks_8k[2] = (byte)(prg_bank_reg_8k[0]);
				prg_banks_8k[3] = 0xFF;
			}
			else
			{
				prg_banks_8k[0] = (byte)(prg_bank_reg_8k[0]);
				prg_banks_8k[1] = (byte)(prg_bank_reg_8k[1]);
				prg_banks_8k[2] = 0xFE;
				prg_banks_8k[3] = 0xFF;
			}
		}

		public void SyncCHR()
		{
			//Console.Write("{0}: ", NES.ppu.ppur.status.sl);
			for (int i = 0; i < 8; i++)
			{
				int low = (chr_bank_reg_1k[i * 2]);
				int high = (chr_bank_reg_1k[i * 2 + 1]);
				int temp = low + high * 16;
				temp = fix_chr(temp);
				//Console.Write("{0},", temp);
				temp &= chr_bank_mask_1k;
				chr_banks_1k[i] = temp;
			}
		}

		void SyncIRQ()
		{
			IRQSignal = (irq_pending && irq_enabled);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			fix_chr = (b) => b;
			switch (Cart.board_type)
			{
				case "MAPPER021":
				case "MAPPER022":
				case "MAPPER023":
				case "MAPPER025":
					throw new InvalidOperationException("someone will need to bug me to set these up for failsafe mapping");

				case "MAPPER027":
					//not exactly the same implementation as FCEUX, but we're taking functionality from it step by step as we discover and document it
					//world hero (unl) is m027 and depends on the extrabig_chr functionality to have correct graphics.
					//otherwise, cah4e3 says its the same as VRC4
					extrabig_chr = true;
					remap = (addr) => addr;
					type = 4;
				  break;

				case "MAPPER116_HACKY":
					remap = (addr) => addr;
					type = 2;
					break;

				case "KONAMI-VRC-4":
					AssertPrg(128,256); AssertChr(128,256); AssertVram(0); AssertWram(0,2,8);
					type = 4;
					if (Cart.pcb == "352396")
						remap = (addr) => ((addr & 0xF000) | ((addr >> 2) & 3));
					else if (Cart.pcb == "351406")
						remap = (addr) => ((addr & 0xF000) | ((addr & 1) << 1) | ((addr & 2) >> 1));
					else throw new Exception("Unknown PCB type for VRC4");

					break;
				case "KONAMI-VRC-2":
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					type = 2;
					latch6k_exists = true;
					if (Cart.pcb == "350926")
						//likely VRC2b [mapper 23]
						remap = (addr) => addr;
					else if (Cart.pcb == "351618")
					{
						//ex. ganbare pennant race - important chr note applies (VRC2a [mapper 22])
						remap = (addr) => ((addr & 0xF000) | ((addr & 1) << 1) | ((addr & 2) >> 1));
						fix_chr = (b) => (b >> 1);
					}
					else if (Cart.pcb == "LROG009-00") // Contra (J)
					{
						remap = (addr) => addr;
					}
					else
						throw new Exception(string.Format("Unknown PCB type for VRC2: \"{0}\"", Cart.pcb));
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			if(type == 4) prg_reg_mask_8k = 0x1F;
			else prg_reg_mask_8k = 0xF;
			
			//this VRC2 variant has an extra large PRG reg
			if (Cart.board_type == "MAPPER116_HACKY")
				prg_reg_mask_8k = 0x1F;

			prg_bank_reg_8k[0] = 0;
			prg_bank_reg_8k[1] = 1;
			SyncPRG();
			SyncCHR();
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}
		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
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
				return VROM[addr + extra_vrom];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("mapping {0:X4} = {1:X2}", addr + 0x8000, value);
			addr = remap(addr);

			int chr_value = value & 0xF;
			if ((addr & 1) == 1 && extrabig_chr)
				chr_value = value & 0x1F;

			switch (addr)
			{
				default:
					Console.WriteLine("missed case: {0:X4}", addr + 0x8000);
					break;

				case 0x0000: //$8000
				case 0x0001:
				case 0x0002:
				case 0x0003:
					prg_bank_reg_8k[0] = value & prg_reg_mask_8k;
					SyncPRG();
					break;
				
				case 0x1000: //$9000
				case 0x1001: //$9001
					switch (value & 3)
					{
						case 0: SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); break;
						case 1: SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal); break;
						case 2: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB); break;
					}
					break;
				
				case 0x1002: //$9002
				case 0x1003: //$9003
					if (type == 4) prg_mode = value.Bit(1);
					else goto case 0x1000;
					SyncPRG();
					break;

				case 0x2000: //$A000
				case 0x2001: //$A001
				case 0x2002: //$A002
				case 0x2003: //$A003
					prg_bank_reg_8k[1] = value & prg_reg_mask_8k;
					SyncPRG();
					break;

				case 0x3000: //$B000
				case 0x3001: //$B001
				case 0x3002: //$B002
				case 0x3003: //$B003
					chr_bank_reg_1k[addr - 0x3000] = chr_value;
					SyncCHR();
					break;

				case 0x4000: //$C000
				case 0x4001: //$C001
				case 0x4002: //$C002
				case 0x4003: //$C003
					chr_bank_reg_1k[addr - 0x4000 + 4] = chr_value;
					SyncCHR();
					break;

				case 0x5000: //$D000
				case 0x5001: //$D001
				case 0x5002: //$D002
				case 0x5003: //$D003
					chr_bank_reg_1k[addr - 0x5000 + 8] = chr_value;
					SyncCHR();
					break;

				case 0x6000: //$E000
				case 0x6001: //$E001
				case 0x6002: //$E002
				case 0x6003: //$E003
					chr_bank_reg_1k[addr - 0x6000 + 12] = chr_value;
					SyncCHR();
					break;

				case 0x7000: //$F000 (reload low)
					if (type == 2) break;
					irq_reload = (byte)((irq_reload&0xF0) | value);
					break;
				case 0x7001: //$F001 (reload high)
					if (type == 2) break;
					irq_reload = (byte)((irq_reload&0x0F)|(value<<4));
					break;
				case 0x7002: //$F001 (control)
					if (type == 2) break;
					irq_mode = value.Bit(2);
					irq_autoen = value.Bit(0);

					if (value.Bit(1))
					{
						//enabled
						irq_enabled = true;
						irq_counter = irq_reload;
						irq_prescaler = 341;
					}
					else
					{
						//disabled
						irq_enabled = false;
					}

					//acknowledge
					irq_pending = false;

					SyncIRQ();

					break;
				
				case 0x7003: //$F003 (ack)
					if (type == 2) break;
					irq_pending = false;
					irq_enabled = irq_autoen;
					SyncIRQ();
					break;
			}
		}

		void ClockIRQ()
		{
			if (type == 2) return;
			if (irq_counter == 0xFF)
			{
				irq_pending = true;
				irq_counter = irq_reload;
				SyncIRQ();
			}
			else
				irq_counter++;
		}

		public override void ClockCPU()
		{
			if (type == 2) return;
			if (!irq_enabled) return;

			if (irq_mode)
			{
				ClockIRQ();
				//throw new InvalidOperationException("needed a test case for this; you found one!");
			}
			else
			{
				irq_prescaler -= 3;
				if (irq_prescaler <= 0)
				{
					irq_prescaler += 341;
					ClockIRQ();
				}
			}
		}

		// a single bit of data can be read at 6000:6fff on some VRC2 carts without wram
		// games will lock if they can't do this
		// this isn't a problem in many emus, because if you put wram at 6000:7fff unconditionally, it works.
		public override byte ReadWRAM(int addr)
		{
			if (!latch6k_exists)
				return base.ReadWRAM(addr);
			else if (addr >= 0x1000)
				return NES.DB;
			else
				return (byte)(NES.DB & 0xfe | latch6k_value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (!latch6k_exists)
				base.WriteWRAM(addr, value);
			else if (addr < 0x1000)
				latch6k_value = value & 1;
		}
	}
}