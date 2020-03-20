using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	internal sealed class Mapper183 : NesBoardBase
	{
		private byte[] prg = new byte[4];
		private byte[] chr = new byte[8];

		private int IRQLatch = 0;
		private int IRQCount = 0;
		private bool IRQMode;
		private bool IRQa = false;
		private bool IRQr = false;

		private int prg_bank_mask_8k, chr_bank_mask_1k, IRQPre=341;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER183":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_1k = Cart.ChrSize - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg, false);
			ser.Sync(nameof(chr), ref chr, false);
		}

		private void SetMirroring(int mirr)
		{
			switch (mirr & 3)
			{
				case 0: SetMirrorType(EMirrorType.Vertical); break;
				case 1: SetMirrorType(EMirrorType.Horizontal); break;
				case 2: SetMirrorType(EMirrorType.OneScreenA); break;
				case 3: SetMirrorType(EMirrorType.OneScreenB); break;
			}
		}

		public override void ClockCpu()
		{
			if (IRQa)
			{
				if (IRQMode)
				{
					IRQCount++;
					if (IRQCount == 0xFF)
					{
						IrqSignal = true;
						IRQCount = IRQLatch;
					}
				}
				else
				{
					IRQPre -= 3;
					if (IRQPre <= 0)
					{
						IRQPre += 341;
						IRQCount++;
						if (IRQCount >= 0x100)
						{
							IrqSignal = IRQa;
							IRQCount = IRQLatch;
						}
					}
				}

			}
		}



		public override void WriteWram(int addr, byte value)
		{
			WriteReg(addr + 0x6000, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			WriteReg(addr + 0x8000, value);
		}

		private void WriteReg(int addr, byte value)
		{
			if ((addr & 0xF800) == 0x6800)
			{
				prg[3] = (byte)(addr & 0x3F);
			}
			else if (((addr & 0xF80C) >= 0xB000) && ((addr & 0xF80C) <= 0xE00C))
			{
				int index = (((addr >> 11) - 6) | (addr >> 3)) & 7;
				chr[index] = (byte)((chr[index] & (0xF0 >> (addr & 4))) | ((value & 0x0F) << (addr & 4)));
			}
			else switch (addr & 0xF80C)
				{
					case 0x8800: prg[0] = value; break;
					case 0xA800: prg[1] = value; break;
					case 0xA000: prg[2] = value; break;
					case 0x9800: SetMirroring(value & 3); break;

					// TODO: IRQ
					case 0xF000: IRQLatch = ((IRQLatch & 0xF0) | (value & 0xF)); break;
					case 0xF004: IRQLatch = ((IRQLatch & 0x0F) | ((value & 0xF) << 4)); break;
					case 0xF008:
						IRQMode = value.Bit(2);
						IRQa = value.Bit(1);//value>0 ? true:false;
						IRQr = value.Bit(0);
						if (IRQa)
						{
							IRQPre = 341;
							IRQCount = IRQLatch;
						}
						IrqSignal = false;
						break;
					case 0xF00C:
						IrqSignal = false;
						IRQa = IRQr;
						break;

				}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int x = (addr >> 10) & 7;
				int bank = (chr[x] & chr_bank_mask_1k) << 10;
				return Vrom[bank + (addr & 0x3FF)]; // TODO
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadWram(int addr)
		{
			return Rom[(((prg[3] & prg_bank_mask_8k)) << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k;
			if (addr < 0x2000) // 0x8000
			{
				bank_8k = prg[0] & prg_bank_mask_8k;
				
			}
			else if (addr < 0x4000) // 0xA000
			{
				bank_8k = prg[1] & prg_bank_mask_8k;
			}
			else if (addr < 0x6000) // 0xC000
			{
				bank_8k = prg[2] & prg_bank_mask_8k;
			}
			else
			{
				bank_8k = prg_bank_mask_8k;
			}

			return Rom[(bank_8k << 13) + (addr & 0x1FFF)];
		}
	}
}
