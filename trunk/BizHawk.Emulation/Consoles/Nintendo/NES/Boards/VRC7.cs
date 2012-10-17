using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//mapper 85
	//If you change any of the IRQ logic here, be sure to change it in VRC 2/3/4/6 as well.
	public class VRC7 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_1k;
		Func<int, int> remap;

		//state
		BizHawk.Emulation.Sound.YM2413 fm; //= new Sound.YM2413(Sound.YM2413.ChipType.VRC7);

		ByteBuffer prg_banks_8k = new ByteBuffer(4);
		ByteBuffer chr_banks_1k = new ByteBuffer(8);
		bool irq_mode;
		bool irq_enabled, irq_pending, irq_autoen;
		byte irq_reload;
		byte irq_counter;
		int irq_prescaler;

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			fm.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_1k", ref chr_banks_1k);
			ser.Sync("irq_mode", ref irq_mode);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_autoen", ref irq_autoen);
			ser.Sync("irq_reload", ref irq_reload);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_prescaler", ref irq_prescaler);
			SyncIRQ();
		}

		void SyncIRQ()
		{
			IRQSignal = (irq_pending && irq_enabled);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER085":
					break;
				case "KONAMI-VRC-7":
					AssertPrg(128,512); AssertChr(0,128); AssertVram(0,8); AssertWram(0,8);
					break;
				default:
					return false;
			}

			if (Cart.pcb == "353429")
				//tiny toons 2
				remap = (addr) => ((addr & 0xF000) | ((addr & 0x8) >> 3));
			else if(Cart.pcb == "352402")
				//lagrange point
				remap = (addr) => ((addr & 0xF000) | ((addr & 0x30) >> 4));
			else throw new Exception("Unknown PCB type for VRC7");

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			SetMirrorType(EMirrorType.Vertical);

			prg_banks_8k[3] = (byte)(0xFF & prg_bank_mask_8k);

			fm = new Sound.YM2413(Sound.YM2413.ChipType.VRC7);

			return true;
		}
		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}

		int Map_PPU(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);
			bank_1k = chr_banks_1k[bank_1k];
			addr = (bank_1k << 10) | ofs;
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = Map_PPU(addr);
				if (Cart.vram_size != 0)
					return base.ReadPPU(addr);
				else return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				base.WritePPU(Map_PPU(addr),value);
			}
			else base.WritePPU(addr, value);
		}

		public override void ApplyCustomAudio(short[] samples)
		{
			short[] fmsamples = new short[samples.Length];
			fm.GetSamples(fmsamples);
			//naive mixing. need to study more
			int len = samples.Length;
			for (int i = 0; i < len; i++)
			{
				short fmsamp = fmsamples[i];
				samples[i] = (short)(samples[i] + fmsamp);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("    mapping {0:X4} = {1:X2}", addr, value);
			addr = remap(addr);
			//Console.WriteLine("- remapping {0:X4} = {1:X2}", addr, value);
			switch (addr)
			{
				case 0x0000: prg_banks_8k[0] = (byte)(value & prg_bank_mask_8k); break;
				case 0x0001: prg_banks_8k[1] = (byte)(value & prg_bank_mask_8k); break;
				case 0x1000: prg_banks_8k[2] = (byte)(value & prg_bank_mask_8k); break;

				case 0x1001:
					//sound address port
					fm.RegisterLatch = value;
					break;
				case 0x1003:
					//sound data port
					fm.Write(value);
					break;

					//a bit creepy to mask this for lagrange point which has no VROM, but the mask will be 0xFFFFFFFF so its OK
				case 0x2000: chr_banks_1k[0] = (byte)(value & chr_bank_mask_1k); break;
				case 0x2001: chr_banks_1k[1] = (byte)(value & chr_bank_mask_1k); break;
				case 0x3000: chr_banks_1k[2] = (byte)(value & chr_bank_mask_1k); break;
				case 0x3001: chr_banks_1k[3] = (byte)(value & chr_bank_mask_1k); break;
				case 0x4000: chr_banks_1k[4] = (byte)(value & chr_bank_mask_1k); break;
				case 0x4001: chr_banks_1k[5] = (byte)(value & chr_bank_mask_1k); break;
				case 0x5000: chr_banks_1k[6] = (byte)(value & chr_bank_mask_1k); break;
				case 0x5001: chr_banks_1k[7] = (byte)(value & chr_bank_mask_1k); break;

				case 0x6000:
					switch (value & 3)
					{
						case 0: SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); break;
						case 1: SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal); break;
						case 2: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB); break;
					}
					break;

				case 0x6001: //(reload)
					irq_reload = value;
					break;
				case 0x7000: //(control)
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
				
				case 0x7001: //(ack)
					irq_pending = false;
					irq_enabled = irq_autoen;
					SyncIRQ();
					break;
			}
		}

		void ClockIRQ()
		{
			if (irq_counter == 0xFF)
			{
				irq_pending = true;
				irq_counter = irq_reload;
				SyncIRQ();
			}
			else
				irq_counter++;
		}

		public override void ClockPPU()
		{
			if (!irq_enabled) return;

			if (irq_mode)
			{
				ClockIRQ();
				throw new InvalidOperationException("needed a test case for this; you found one!");
			}
			else
			{
				irq_prescaler--;
				if (irq_prescaler == 0)
				{
					irq_prescaler += 341;
					ClockIRQ();
				}
			}
		}

	}
}