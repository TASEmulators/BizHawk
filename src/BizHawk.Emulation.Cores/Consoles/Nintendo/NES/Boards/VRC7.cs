using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//mapper 85
	//If you change any of the IRQ logic here, be sure to change it in VRC 2/3/4/6 as well.
	internal sealed class VRC7 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_8k, chr_bank_mask_1k;
		private Func<int, int> remap;

		//state
		private YM2413 fm; //= new Sound.YM2413(Sound.YM2413.ChipType.VRC7);

		private byte[] prg_banks_8k = new byte[4];
		private byte[] chr_banks_1k = new byte[8];
		private bool irq_mode;
		private bool irq_enabled, irq_pending, irq_autoen;
		private byte irq_reload;
		private byte irq_counter;
		private int irq_prescaler;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			fm?.SyncState(ser);
			ser.Sync(nameof(prg_banks_8k), ref prg_banks_8k, false);
			ser.Sync(nameof(chr_banks_1k), ref chr_banks_1k, false);
			ser.Sync(nameof(irq_mode), ref irq_mode);
			ser.Sync(nameof(irq_enabled), ref irq_enabled);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_autoen), ref irq_autoen);
			ser.Sync(nameof(irq_reload), ref irq_reload);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_prescaler), ref irq_prescaler);
			SyncIRQ();
		}

		private void SyncIRQ()
		{
			IrqSignal = (irq_pending && irq_enabled);
		}


		private static int RemapM117(int addr)
		{
			//addr &= 0x7007; // i don't know all of which bits are decoded, but this breaks stuff
			switch (addr)
			{
				//prg
				case 0x0000: return 0x0000;
				case 0x0001: return 0x0001;
				case 0x0002: return 0x1000;
				//chr
				case 0x2000: return 0x2000;
				case 0x2001: return 0x2001;
				case 0x2002: return 0x3000;
				case 0x2003: return 0x3001;
				case 0x2004: return 0x4000;
				case 0x2005: return 0x4001;
				case 0x2006: return 0x5000;
				case 0x2007: return 0x5001;
				//irq
				// fake addressees to activate different irq handling logic
				case 0x4001: return 0x10001;
				case 0x4002: return 0x10002;
				case 0x4003: return 0x10003;
				case 0x6000: return 0x10004;
				//mir
				case 0x5000: return 0x6000;

				//probably nothing at all
				default: return 0xffff;
			}
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER085":
					// as in some VRC2/VRC4 cases, this is actually a "composite" mapping that catches
					// both kinds of VRC7 (but screws up some of the address mirrors)
					remap = (addr) => (addr & 0xF000) | (addr & 0x30) >> 4 | (addr & 0x8) >> 3;
					fm = new YM2413(YM2413.ChipType.VRC7);
					break;
				case "KONAMI-VRC-7":
					AssertPrg(128, 512); AssertChr(0, 128); AssertVram(0, 8); AssertWram(0, 8);
					if (Cart.Pcb == "353429")
					{
						//tiny toons 2
						// for consistency, we map the addr line used for the FM chip even though
						// there is no resonator or crystal on the board for the fm chip
						remap = (addr) => (addr & 0xF000) | ((addr & 0x8) >> 3) | (addr & 0x20) >> 4;
						fm = null;
					}
					else if (Cart.Pcb == "352402")
					{
						//lagrange point
						remap = addr => ((addr & 0xF000) | ((addr & 0x30) >> 4));
						fm = new YM2413(YM2413.ChipType.VRC7);
					}
					else
						throw new Exception("Unknown PCB type for VRC7");
					break;
				case "MAPPER117":
					// not sure quite what this is
					// different address mapping, and somewhat different irq logic
					Cart.VramSize = 0;
					Cart.WramSize = 0;
					remap = RemapM117;
					fm = null;
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_1k = 0xff; // Cart.chr_size - 1;

			SetMirrorType(EMirrorType.Vertical);

			prg_banks_8k[3] = (byte)(0xFF & prg_bank_mask_8k);

			return true;
		}
		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		private int Map_PPU(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);
			bank_1k = chr_banks_1k[bank_1k];
			addr = (bank_1k << 10) | ofs;
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = Map_PPU(addr);
				if (Cart.VramSize != 0)
					return base.ReadPpu(addr);

				return Vrom[addr];
			}

			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				base.WritePpu(Map_PPU(addr),value);
			}
			else base.WritePpu(addr, value);
		}

		public override void ApplyCustomAudio(short[] samples)
		{
			if (fm != null)
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
		}

		public override void WritePrg(int addr, byte value)
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
					if (fm != null)
						fm.RegisterLatch = value;
					break;
				case 0x1003:
					//sound data port
					fm?.Write(value);
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
						case 0: SetMirrorType(EMirrorType.Vertical); break;
						case 1: SetMirrorType(EMirrorType.Horizontal); break;
						case 2: SetMirrorType(EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(EMirrorType.OneScreenB); break;
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
						irq_prescaler = 341 + 3;
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

				// special irq logic for M117
				// RemapM117() sends some addresses to these "virtual addresses" for irq handling
				case 0x10001:
					irq_reload = (byte)(237 - value); // what
					break;
				case 0x10002:
					irq_pending = false;
					SyncIRQ();
					break;
				case 0x10003:
					irq_counter = irq_reload;
					break;
				case 0x10004:
					irq_enabled = value.Bit(0);
					irq_pending = false;
					SyncIRQ();
					break;
			}
		}

		private void ClockIRQ()
		{
			if (irq_counter == 0xFF)
			{
				irq_pending = true;
				irq_counter = irq_reload;
				//SyncIRQ();
			}
			else
				irq_counter++;
		}

		public override void ClockCpu()
		{
			if (irq_pending)
			{
				SyncIRQ();
			}

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
	}
}