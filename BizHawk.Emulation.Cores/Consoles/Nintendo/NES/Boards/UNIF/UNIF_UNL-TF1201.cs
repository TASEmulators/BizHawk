using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal class UNIF_UNL_TF1201 : NesBoardBase
	{
		private byte prg0;
		private byte prg1;
		private byte swap;
		private byte[] chr = new byte[8];

		private bool IRQa;
		private int IRQCount;
		private int IRQpre = 341;

		private int prg_mask_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-TF1201":
					break;
				default:
					return false;
			}

			prg_mask_8k = Cart.prg_size / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg0), ref prg0);
			ser.Sync(nameof(prg1), ref prg1);
			ser.Sync(nameof(swap), ref swap);
			ser.Sync(nameof(chr), ref chr, false);
			ser.Sync(nameof(IRQa), ref IRQa);
			ser.Sync(nameof(IRQCount), ref IRQCount);
			ser.Sync(nameof(IRQpre), ref IRQpre);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			addr = (addr & 0xF003) | ((addr & 0xC) >> 2);
			if ((addr >= 0xB000) && (addr <= 0xE003))
			{
				int ind = (((addr >> 11) - 6) | (addr & 1)) & 7;
				int sar = ((addr & 2) << 1);
				chr[ind] = (byte)((chr[ind] & (0xF0 >> sar)) | ((value & 0x0F) << sar));
			}
			else switch (addr & 0xF003)
			{
				case 0x8000:
						prg0 = value;
						break;
				case 0xA000:
						prg1 = value;
						break;
				case 0x9000:
						SetMirrorType(value.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
						break;
				case 0x9001:
						swap = (byte)(value & 3);
						break;

				case 0xF000: IRQCount = (IRQCount & 0xF0) | (value & 0x0F); break;
				case 0xF002: IRQCount = (IRQCount & 0x0F) | (value << 4 & 0xF0); break;
				case 0xF001:
				case 0xF003:
						IRQa = value.Bit(1);
						IrqSignal = false;
						if (NES.ppu.ppuphase !=PPU.PPU_PHASE_VBL)
							IRQCount -= 8;
						break;
			}
		}

		public override void ClockPpu()
		{
			if ((NES.ppu.ppuphase != PPU.PPU_PHASE_VBL))// && IRQa)
			{
				IRQpre--;
				if (IRQpre==0)
				{
					IRQCount++;
					IRQpre = 341;
					if (IRQCount == 237)
					{
						IrqSignal = IRQa;
					}
					if (IRQCount == 256)
						IRQCount = 0;
				}
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank;

			if (addr < 0x2000)
			{
				if ((swap & 3) > 0)
				{
					bank = prg_mask_8k - 1;
				}
				else
				{
					bank = prg0;
				}
			}
			else if (addr < 0x4000)
			{
				bank = prg1;
			}
			else if (addr < 0x6000)
			{
				if ((swap & 3) > 0)
				{
					bank = prg0;
				}
				else
				{
					bank = prg_mask_8k-1;
				}
			}
			else
			{
				bank = prg_mask_8k;
			}


			return Rom[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int x = (addr >> 10) & 7;

				return Vrom[(chr[x] << 10) + (addr & 0x3FF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
