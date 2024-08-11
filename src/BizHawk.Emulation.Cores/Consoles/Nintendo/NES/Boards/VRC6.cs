using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//mapper 24 + 26
	//If you change any of the IRQ logic here, be sure to change it in VRC 4/7 as well.
	internal sealed class VRC6 : NesBoardBase
	{
		// what did i do in a previous life to deserve this?

		// given the bottom four bits of $b003, and a 1K address region in PPU $0000:$3fff,
		private static readonly byte[] Banks = new byte[16 * 16]; // which of the 8 chr regs is used to determine the bank here?
		private static readonly byte[] Masks = new byte[16 * 16]; // what is the resulting 8 bit chr reg value ANDed with?
		private static readonly byte[] A10s = new byte[16 * 16]; // and then what is it ORed with?

		private static readonly byte[] PTables = 
		{
			0x00,0x01,0x02,0x03,0x04,0x05,0x06,0x07,
			0x80,0xc0,0x81,0xc1,0x82,0xc2,0x83,0xc3,
			0x00,0x01,0x02,0x03,0x84,0xc4,0x85,0xc5,	
		};

		private static void GetBankByte(int b003, int banknum, out byte bank, out byte mask, out byte a10)
		{
			if (banknum < 8) // pattern tables
			{
				int ptidx = b003 & 3;
				if (ptidx == 3) ptidx--;
				byte pt = PTables[ptidx * 8 + banknum];

				bank = (byte)(pt & 7);
				mask = (byte)(pt.Bit(7) ? 0xfe : 0xff);
				a10 = (byte)(pt.Bit(7) && pt.Bit(6) ? 1 : 0);
			}
			else // nametables
			{
				banknum &= 3;
				switch (b003 & 7)
				{
					case 0:
					case 6:
					case 7: // H-mirror, 6677
						bank = (byte)(banknum >> 1 | 6);
						break;
					case 2:
					case 3:
					case 4: // V-mirror, 6767
						bank = (byte)(banknum | 6);
						break;
					case 1:
					case 5: // 4 screen, 4567
					default:
						bank = (byte)(banknum | 4);
						break;
				}
				switch (b003)
				{
					case 0:
					case 7: // V-mirror
						mask = 0xfe;
						a10 = (byte)(banknum & 1);
						break;
					case 3:
					case 4: // H-mirror
						mask = 0xfe;
						a10 = (byte)(banknum >> 1);
						break;
					case 8:
					case 15: // 1scA
						mask = 0xfe;
						a10 = 0;
						break;
					case 11:
					case 12: // 1scB
						mask = 0xfe;
						a10 = 1;
						break;
					default: // no replacement
						mask = 0xff;
						a10 = 0;
						break;
				}
			}
		}

		static VRC6()
		{
			int idx = 0;
			for (int b003 = 0; b003 < 16; b003++)
			{
				for (int banknum = 0; banknum < 16; banknum++)
				{
					GetBankByte(b003, banknum, out byte bank, out byte mask, out byte a10);
					Banks[idx] = bank;
					Masks[idx] = mask;
					A10s[idx] = a10;
					idx++;
				}
			}
		}

		//configuration
		private int prg_bank_mask_8k, chr_bank_mask_1k;
		private int chr_byte_mask;
		private bool newer_variant;

		private VRC6Alt VRC6Sound;

		//state
		private int prg_bank_16k, prg_bank_8k;
		private readonly byte[] prg_banks_8k = new byte[4];
		private byte[] chr_banks_1k = new byte[8];
		private bool irq_mode;
		private bool irq_enabled, irq_pending, irq_autoen;
		private byte irq_reload;
		private byte irq_counter;
		private int irq_prescaler;

		private bool chrA10replace;
		private bool NTROM;
		private int PPUBankingMode;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			VRC6Sound.SyncState(ser);
			ser.Sync(nameof(prg_bank_16k), ref prg_bank_16k);
			ser.Sync(nameof(prg_bank_8k), ref prg_bank_8k);
			ser.Sync(nameof(chr_banks_1k), ref chr_banks_1k, false);
			ser.Sync(nameof(irq_mode), ref irq_mode);
			ser.Sync(nameof(irq_enabled), ref irq_enabled);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_autoen), ref irq_autoen);
			ser.Sync(nameof(irq_reload), ref irq_reload);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_prescaler), ref irq_prescaler);

			ser.Sync(nameof(chrA10replace), ref chrA10replace);
			ser.Sync(nameof(NTROM), ref NTROM);
			ser.Sync(nameof(PPUBankingMode), ref PPUBankingMode);

			SyncPRG();
			SyncIRQ();
		}

		private void SyncPRG()
		{
			prg_banks_8k[0] = (byte)(prg_bank_16k * 2);
			prg_banks_8k[1] = (byte)(prg_bank_16k * 2 + 1);
			prg_banks_8k[2] = (byte)(prg_bank_8k);
			prg_banks_8k[3] = 0xFF;
		}

		private void SyncIRQ()
		{
			IrqSignal = (irq_pending && irq_enabled);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER024":
					newer_variant = false;
					break;
				case "MAPPER026":
					newer_variant = true;
					break;
				case "KONAMI-VRC-6":
					if (Cart.Pcb == "351951")
						newer_variant = false;
					else if (Cart.Pcb == "351949A")
						newer_variant = true;
					else throw new Exception("Unknown PCB type for VRC6");
					AssertPrg(256); AssertChr(128, 256);
					break;
				default:
					return false;
			}
			AssertVram(0); AssertWram(0, 8);

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_1k = Cart.ChrSize - 1;
			chr_byte_mask = Cart.ChrSize * 1024 - 1;

			prg_bank_16k = 0;
			prg_bank_8k = 0;
			SyncPRG();

			if (NES.apu != null) // don't start up sound when in configurator
				VRC6Sound = new VRC6Alt(NES.apu.ExternalQueue);

			return true;
		}
		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		private int MapPPU(int addr)
		{
			int lutidx = addr >> 10 | PPUBankingMode << 4;
			int bank = chr_banks_1k[Banks[lutidx]];
			if (chrA10replace)
			{
				bank &= Masks[lutidx];
				bank |= A10s[lutidx];
			}
			return addr & 0x3ff | bank << 10;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr >= 0x2000 && !NTROM)
				return NES.CIRAM[MapPPU(addr) & 0x7ff];
			else
				return Vrom[MapPPU(addr) & chr_byte_mask];
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr >= 0x2000 && !NTROM)
				NES.CIRAM[MapPPU(addr) & 0x7ff] = value;
		}

		public override void WritePrg(int addr, byte value)
		{
			if (newer_variant)
			{
				addr = (addr & 0xFFFC) | ((addr >> 1) & 1) | ((addr << 1) & 2);
			}
			switch (addr)
			{
				case 0x0000: //$8000
				case 0x0001:
				case 0x0002:
				case 0x0003:
					prg_bank_16k = value;
					SyncPRG();
					break;

				case 0x1000: //$9000
					VRC6Sound.Write9000(value);
					break;
				case 0x1001: //$9001
					VRC6Sound.Write9001(value);
					break;
				case 0x1002: //$9002
					VRC6Sound.Write9002(value);
					break;
				case 0x1003: //$9003
					VRC6Sound.Write9003(value);
					break;

				case 0x2000: //$A000
					VRC6Sound.WriteA000(value);
					break;
				case 0x2001: //$A001
					VRC6Sound.WriteA001(value);
					break;
				case 0x2002: //$A002
					VRC6Sound.WriteA002(value);
					break;

				case 0x3000: //$B000
					VRC6Sound.WriteB000(value);
					break;
				case 0x3001: //$B001
					VRC6Sound.WriteB001(value);
					break;
				case 0x3002: //$B002
					VRC6Sound.WriteB002(value);
					break;

				case 0x3003: //$B003
					PPUBankingMode = value & 15;
					NTROM = value.Bit(4);
					chrA10replace = value.Bit(5);
					break;

				case 0x4000: //$C000
				case 0x4001:
				case 0x4002:
				case 0x4003:
					prg_bank_8k = value;
					SyncPRG();
					break;

				case 0x5000: //$D000
				case 0x5001: //$D001
				case 0x5002: //$D002
				case 0x5003: //$D003
					chr_banks_1k[addr - 0x5000] = value;
					break;

				case 0x6000: //$E000
				case 0x6001: //$E001
				case 0x6002: //$E002
				case 0x6003: //$E003
					chr_banks_1k[4 + addr - 0x6000] = value;
					break;

				case 0x7000: //$F000 (reload)
					irq_reload = value;
					break;
				case 0x7001: //$F001 (control)
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

				case 0x7002: //$F002 (ack)
					irq_pending = false;
					irq_enabled = irq_autoen;
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
				SyncIRQ();
			}
			else
				irq_counter++;
		}

		public override void ClockCpu()
		{
			VRC6Sound.Clock();

			if (!irq_enabled) return;

			if (irq_mode)
			{
				ClockIRQ();
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
