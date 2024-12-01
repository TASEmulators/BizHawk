//TODO - for chr, refactor to use 8 registers of 8 bits instead of 16 registers of 4 bits. more realistic, less weird code.

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//mapper 21 + 22 + 23 + 25 (docs largely in 021.txt for VRC4 and 22.txt for VRC2)
	//If you change any of the IRQ logic here, be sure to change it in VRC 3/6/7 as well.
	internal sealed class VRC2_4 : NesBoardBase
	{
		// remaps addresses into vrc2b form
		// all varieties of vrc2&4 require A15 = 1 (ie, we're in 8000:ffff), and key on A14:A12 in the same way
		// in addition, each variety has two other bits; a "low bit" and a "high bit"
		// for vrc2b, low bit is A0 and high bit is A1, so it's represented by AddrA0A1
		// other remaps are named similarly
		private int AddrA1A0(int addr)
		{
			return addr & 0x7000 | (addr >> 1) & 1 | (addr << 1) & 2;
		}

		private int AddrA0A1(int addr)
		{
			return addr & 0x7003;
		}

		private int AddrA1A2(int addr)
		{
			return addr & 0x7000 | (addr >> 1) & 3;
		}

		private int AddrA6A7(int addr)
		{
			return addr & 0x7000 | (addr >> 6) & 3;
		}

		private int AddrA2A3(int addr)
		{
			return addr & 0x7000 | (addr >> 2) & 3;
		}

		private int AddrA3A2(int addr)
		{
			return addr & 0x7000 | (addr >> 3) & 1 | (addr >> 1) & 2;
		}
		// these composite mappings are what's needed for ines mappers
		private int AddrA1A2_A6A7(int addr)
		{
			return addr & 0x7000 | (addr >> 1) & 3 | (addr >> 6) & 3;
		}

		private int AddrA0A1_A2A3(int addr)
		{
			return addr & 0x7003 | (addr >> 2) & 3;
		}

		private int AddrA3A2_A1A0(int addr)
		{
			return addr & 0x7000 | (addr >> 3) & 1 | (addr >> 1) & 3 | (addr << 1) & 2;
		}

		private int prg_bank_mask_8k, chr_bank_mask_1k;
		private int prg_reg_mask_8k;
		private Func<int, int> remap;
		private Func<int, int> fix_chr;
		private int type;
		private bool latch6k_exists = false;

		// it's been verified that this should be true on all real VRC4 chips, and
		// that some vrc4 boards support it: http://forums.nesdev.com/viewtopic.php?t=8569
		// but no vrc4 game ever used it
		private bool extrabig_chr = false;

		//state
		private readonly int[] prg_bank_reg_8k = new int[2];
		public int[] chr_bank_reg_1k = new int[16];
		private bool _prgMode;
		public byte[] prg_banks_8k = new byte[4];
		public int[] chr_banks_1k = new int[8];
		private bool irq_mode;
		private bool irq_enabled, irq_pending, irq_autoen;
		private byte irq_reload;
		private byte irq_counter;
		private int irq_prescaler;
		public int extra_vrom;
		private int latch6k_value;

		private bool isPirate = false;
		// needed for 2-in-1 - Yuu Yuu + Dragonball Z [p1][!]
		private bool _isBMC = false;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			for (int i = 0; i < 2; i++) ser.Sync("prg_bank_reg_8k_" + i, ref prg_bank_reg_8k[i]);
			for (int i = 0; i < 16; i++) ser.Sync("chr_bank_reg_1k_" + i, ref chr_bank_reg_1k[i]);
			ser.Sync(nameof(irq_mode), ref irq_mode);
			ser.Sync(nameof(irq_enabled), ref irq_enabled);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_autoen), ref irq_autoen);
			ser.Sync(nameof(irq_reload), ref irq_reload);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_prescaler), ref irq_prescaler);
			ser.Sync(nameof(extra_vrom), ref extra_vrom);
			ser.Sync(nameof(_prgMode), ref _prgMode);
			if (latch6k_exists)
				ser.Sync(nameof(latch6k_value), ref latch6k_value);
			//SyncPRG();
			ser.Sync("prg_banks", ref prg_banks_8k, false);
			SyncCHR();
			SyncIRQ();
			ser.Sync(nameof(isPirate), ref isPirate);
			ser.Sync("isBMC", ref _isBMC);
		}

		private void SyncPRG()
		{
			if (!_isBMC)
			{
				if (_prgMode)
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

		private void SyncIRQ()
		{
			IrqSignal = (irq_pending && irq_enabled);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			fix_chr = (b) => b;
			switch (Cart.BoardType)
			{
				// for INES, we assume VRC4 in many cases where it might be VRC2
				case "MAPPER021":
					type = 4;
					remap = AddrA1A2_A6A7;
					Cart.WramSize = 8;
					break;
				case "MAPPER022":
					type = 2;
					remap = AddrA1A0;
					Cart.WramSize = 8; // should be latch_exists = true but forget that
					fix_chr = (b) => (b >> 1);
					break;
				case "MAPPER023":
					type = 4;
					remap = AddrA0A1_A2A3;
					Cart.WramSize = 8;
					break;
				case "MAPPER023_BMC":
					type = 4;
					remap = AddrA0A1_A2A3;
					Cart.WramSize = 8;
					_isBMC = true;
					prg_banks_8k[0] = (byte)(prg_bank_reg_8k[0]);
					prg_banks_8k[1] = (byte)(prg_bank_reg_8k[1]);
					prg_banks_8k[2] = 0xFE;
					prg_banks_8k[3] = 0xFF;
					break;
				case "MAPPER025":
					type = 4;
					remap = AddrA3A2_A1A0;
					Cart.WramSize = 8;
					break;
				case "UNIF_UNL-T-230":
					isPirate = true;
					goto case "MAPPER023";
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
					AssertPrg(128, 256); AssertChr(128, 256); AssertVram(0); AssertWram(0, 2, 8);
					type = 4;
					switch (Cart.Pcb)
					{
						case "352398": // vrc4a A1 A2
							remap = AddrA1A2; break;
						case "351406": // vrc4b A1 A0
							remap = AddrA1A0; break;
						case "352889": // vrc4c A6 A7
							remap = AddrA6A7; break;
						case "352400": // vrc4d A3 A2
							remap = AddrA3A2; break;
						case "352396": // vrc4e A2 A3
							remap = AddrA2A3; break;
						default:
							throw new Exception("Unknown PCB type for VRC4");
					}
					break;
				case "KONAMI-VRC-2":
					AssertPrg(128, 256); AssertChr(128, 256); AssertVram(0); AssertWram(0, 8);
					type = 2;
					if (Cart.WramSize == 0)
						latch6k_exists = true;
					switch (Cart.Pcb)
					{
						case "351618": // vrc2a A1 A0
							remap = AddrA1A0;
							fix_chr = (b) => (b >> 1); // smaller chr regs on vrc2a
							break;
						case "LROG009-00":
						case "350603":
						case "350926":
						case "350636":
						case "351179": // vrc2b A0 A1
							remap = AddrA0A1;
							break;
						case "351948": // vrc2c A1 A0
							// this is a weird PCB
							// it's the only vrc2 with wram, and the only one with 256 prg rom
							// it also has extra chips that no other vrc2\4 has (probably because
							// the VRC2 doesn't support WRAM internally)
							remap = AddrA1A0;
							break;
						default:
							throw new Exception($"Unknown PCB type for VRC2: \"{Cart.Pcb}\"");
					}
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_1k = Cart.ChrSize - 1;

			// prg regs are 5 bits wide in all VRC2 and VRC4, believe it or not
			// todo: we don't even need this, do we?  removing it would
			// support those 'oversizes' that nesdev gets boners for
			prg_reg_mask_8k = 0x1F;

			prg_bank_reg_8k[0] = 0;
			prg_bank_reg_8k[1] = 1;
			SyncPRG();
			SyncCHR();
			SetMirrorType(EMirrorType.Vertical);

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

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000 && Vrom != null)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_banks_1k[bank_1k];
				addr = (bank_1k << 10) | ofs;

				return Vrom[addr + extra_vrom];
			}
			else return base.ReadPpu(addr);
		}

		public override void WritePrg(int addr, byte value)
		{
			//Console.WriteLine("mapping {0:X4} = {1:X2}", addr + 0x8000, value);
			addr = remap(addr);

			int chr_value = value & 0xF;
			if ((addr & 1) == 1 && extrabig_chr)
				chr_value = value & 0x1F;

			// special instructions for BMC 2 in 1
			if (_isBMC)
			{
				if (addr < 0x1000)
				{
					prg_banks_8k[_prgMode?1:0] = (byte)((prg_banks_8k[0] & 0x20) | (value & 0x1F));
					return;
				}
				else if (addr >= 0x2000 && addr < 0x3000)
				{
					prg_banks_8k[1] = (byte)((prg_banks_8k[0] & 0x20) | (value & 0x1F));
					return;
				}
				else if (addr >= 0x3000 && addr < 0x7000)
				{
					value = (byte)(value << 2 & 0x20);

					prg_banks_8k[0] = (byte)(value | (prg_banks_8k[0] & 0x1F));
					prg_banks_8k[1] = (byte)(value | (prg_banks_8k[1] & 0x1F));
					prg_banks_8k[2] = (byte)(value | (prg_banks_8k[2] & 0x1F));
					prg_banks_8k[3] = (byte)(value | (prg_banks_8k[3] & 0x1F));
					return;
				}
			}

			switch (addr)
			{
				default:
					Console.WriteLine("missed case: {0:X4}", addr + 0x8000);
					break;

				case 0x0000: //$8000
				case 0x0001:
				case 0x0002:
				case 0x0003:
					if (!isPirate)
					{
						prg_bank_reg_8k[0] = value & prg_reg_mask_8k;
						SyncPRG();
					}
					break;

				case 0x1000: //$9000
				case 0x1001: //$9001
					switch (value & (type - 1)) // VRC2 only supports V, H, and not A, B
					{
						case 0: SetMirrorType(EMirrorType.Vertical); break;
						case 1: SetMirrorType(EMirrorType.Horizontal); break;
						case 2: SetMirrorType(EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(EMirrorType.OneScreenB); break;
					}
					break;

				case 0x1002: //$9002
				case 0x1003: //$9003
					if (type == 4) _prgMode = value.Bit(1);
					else goto case 0x1000;
					SyncPRG();
					break;

				case 0x2000: //$A000
				case 0x2001: //$A001
				case 0x2002: //$A002
				case 0x2003: //$A003
					if (!isPirate)
					{
						prg_bank_reg_8k[1] = value & prg_reg_mask_8k;
					}
					else
					{
						prg_bank_reg_8k[0] = (value & 0x1F) << 1;
						prg_bank_reg_8k[1] = ((value & 0x1F) << 1) | 1;
					}
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
					irq_reload = (byte)((irq_reload & 0xF0) | value);
					break;
				case 0x7001: //$F001 (reload high)
					if (type == 2) break;
					irq_reload = (byte)((irq_reload & 0x0F) | (value << 4));
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

				case 0x7003: //$F003 (ack)
					if (type == 2) break;
					irq_pending = false;
					irq_enabled = irq_autoen;
					SyncIRQ();
					break;
			}
		}

		private void ClockIRQ()
		{
			if (type == 2) return;
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
		public override byte ReadWram(int addr)
		{
			if (!latch6k_exists)
				return base.ReadWram(addr);
			else if (addr >= 0x1000)
				return NES.DB;
			else
				return (byte)(NES.DB & 0xfe | latch6k_value);
		}

		public override void WriteWram(int addr, byte value)
		{
			if (!latch6k_exists)
				base.WriteWram(addr, value);
			else if (addr < 0x1000)
				latch6k_value = value & 1;
		}
	}
}
