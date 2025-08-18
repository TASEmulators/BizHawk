﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// describes Sachen8259A/B/C.  D is in a different class
	// behavior from fceumm

	internal sealed class Sachen8259ABC : NesBoardBase
	{
		// config
		private int prg_bank_mask_32k;
		private int chr_bank_mask_2k;

		private int shiftout; // reg lines are shifted on the PCB to increase capacity
		private int shiftmask;

		// state
		private int port; // register that gets written next
		private int prg; // 32K prg swap
		private int[] chr = new int[4]; // 6 bits of chr, 3 from an outer bank
		private bool simple; // when true, we're in some sort of "simplified" mode

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				// quite a few crappy games on these boards, shouldn't be hard to find examples?
				case "MAPPER141":
				case "UNIF_UNL-Sachen-8259A":
				case "SACHEN-8259A":
					shiftout = 1; // 256KiB chr max
					break;
				case "MAPPER138":
				case "UNIF_UNL-Sachen-8259B":
				case "SACHEN-8259B":
					shiftout = 0; // 128KiB chr max
					break;
				case "MAPPER139":
				case "UNIF_UNL-Sachen-8259C":
				case "SACHEN-8259C":
					shiftout = 2; // 512KiB chr max
					break;
				default:
					return false;
			}
			Cart.WramSize = 0; // cart responds to regs in 6000:7fff

			//zero 13-dec-2014 - Q-boy is example of game with vram, apparently.
			//lets only clear vram if theres a chr rom
			if(Cart.ChrSize != 0)
				Cart.VramSize = 0;

			shiftmask = (1 << shiftout) - 1;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			chr_bank_mask_2k = Cart.ChrSize / 2 - 1;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			Write(addr, value);
		}
		public override void WriteWram(int addr, byte value)
		{
			Write(addr, value);
		}

		private void Write(int addr, byte value)
		{
			addr &= 0x0101;
			if (addr == 0x100)
			{
				port = value & 7;
			}
			else
			{
				switch (port)
				{
					case 0:
					case 1:
					case 2:
					case 3:
						// low 3 bits
						chr[port] &= 0x38;
						chr[port] |= value & 7;
						break;
					case 4:
						// outer bank high 3 bits
						for (int i = 0; i < 4; i++)
						{
							chr[i] &= 0x07;
							chr[i] |= (value & 7) << 3;
						}
						break;
					case 5:
						prg = value & 7;
						break;
					case 6: // unused?
						break;
					case 7:
						simple = value.Bit(0);
						if (simple)
						{
							SetMirrorType(EMirrorType.Vertical);
						}
						else
						{
							switch (value & 6)
							{
								case 0: SetMirrorType(EMirrorType.Vertical); break;
								case 2: SetMirrorType(EMirrorType.Horizontal); break;
								case 4: SetMirroring(0, 1, 1, 1); break;
								case 6: SetMirrorType(EMirrorType.OneScreenA); break;
							}
						}
						break;
				}
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank = prg & prg_bank_mask_32k;
			return Rom[addr | bank << 15];
		}
		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if(Vrom == null)
					return base.ReadPpu(addr);

				int idx = addr >> 11;
				// in addition to fixing V-mirroring, simple fixes us to 1 bank
				// this means for type C, simple has 1 8KiB chr bank
				int bank = chr[simple ? 0 : idx];
				// on the PCB, all of the CHR addr lines are shifted up,
				// while the low lines that were lost are connected directly to PPU A lines
				bank <<= shiftout;
				bank |= idx & shiftmask;
				bank &= chr_bank_mask_2k;
				return Vrom[addr & 0x7ff | bank << 11];
			}
			else
			{
				return base.ReadPpu(addr);
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(port), ref port);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(chr), ref chr, false);
			ser.Sync(nameof(simple), ref simple);
		}
	}





	// similar in some ways to 8259ABC, but different
	// fceumm combines the code to implement them; i think that's too messy

	// this mapper is stupid and was most certainly made for 1 game.  it has an awkward
	// chr mapping that can only support up to 32KiB chr rom, and it uses it to show
	// a few famous persons portraits while playing a stupid block game

	// i think there's something wrong with the mapper implementation; but the game
	// sucks so hard, it's hard to tell
	internal sealed class Sachen8259D : NesBoardBase
	{
		// config
		private int prg_bank_mask_32k;
		private int chr_bank_mask_1k;

		// state
		private int port;
		private int prg;
		private int[] chr = new int[8];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				// only game i'm aware of is "The Great Wall"
				case "MAPPER137":
				case "UNIF_UNL-Sachen-8259D":
				case "SACHEN-8259D":
					break;
				default:
					return false;
			}
			Cart.WramSize = 0; // cart responds to regs in 6000:7fff
			Cart.VramSize = 0;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			chr_bank_mask_1k = Cart.ChrSize / 1 - 1;

			// last 4k of chr is fixed
			chr[4] = 0x1c;
			chr[5] = 0x1d;
			chr[6] = 0x1e;
			chr[7] = 0x1f;

			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			Write(addr, value);
		}
		public override void WriteWram(int addr, byte value)
		{
			Write(addr, value);
		}

		private void Write(int addr, byte value)
		{
			addr &= 0x0101;
			if (addr == 0x100)
			{
				port = value & 7;
			}
			else
			{
				switch (port)
				{
					case 0:
					case 1:
					case 2:
					case 3:
						// low 3 bits
						chr[port] &= 0x18;
						chr[port] |= value & 7;
						break;
					case 4:
						// a single high bit for chr regs 1:3
						// note that this bit goes in bit #4, not #3
						chr[1] &= 0x0f;
						chr[1] |= (value & 1) << 4;
						chr[2] &= 0x0f;
						chr[2] |= (value & 2) << 3;
						chr[3] &= 0x0f;
						chr[3] |= (value & 4) << 2;
						break;
					case 5:
						prg = value & 7;
						break;
					case 6:
						// one more single bit of chr for 1 reg
						// so only chr[3] is 5 full bits
						chr[3] &= 0x17;
						chr[3] |= (value & 1) << 3;
						break;
					case 7:
						// as in A\B\C, but we don't need to store "simple"
						if (value.Bit(0))
						{
							SetMirrorType(EMirrorType.Vertical);
						}
						else
						{
							switch (value & 6)
							{
								case 0: SetMirrorType(EMirrorType.Vertical); break;
								case 2: SetMirrorType(EMirrorType.Horizontal); break;
								case 4: SetMirroring(0, 1, 1, 1); break;
								case 6: SetMirrorType(EMirrorType.OneScreenA); break;
							}
						}
						break;
				}
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank = prg & prg_bank_mask_32k;
			return Rom[addr | bank << 15];
		}
		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = chr[addr >> 10] & chr_bank_mask_1k;
				return Vrom[addr & 0x3ff | bank << 10];
			}
			else
			{
				return base.ReadPpu(addr);
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(port), ref port);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(chr), ref chr, false);
		}
	}
}
