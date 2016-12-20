using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_UNL_TF1201 : NES.NESBoardBase
	{
		private byte prg0;
		private byte prg1;
		private byte swap;
		private ByteBuffer chr = new ByteBuffer(8);

		private bool IRQa;
		private int IRQCount;
		private int IRQpre = 341;

		private int prg_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
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

		public override void Dispose()
		{
			chr.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg0", ref prg0);
			ser.Sync("prg1", ref prg1);
			ser.Sync("swap", ref swap);
			ser.Sync("chr", ref chr);
			ser.Sync("IRQa", ref IRQa);
			ser.Sync("IRQCount", ref IRQCount);
			ser.Sync("IRQpre", ref IRQpre);
		}

		public override void WritePRG(int addr, byte value)
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
						IRQSignal = false;
						if (NES.ppu.ppuphase !=PPU.PPUPHASE.VBL)
							IRQCount -= 8;
						break;
			}
		}

		public override void ClockPPU()
		{
			if ((NES.ppu.ppuphase != PPU.PPUPHASE.VBL))// && IRQa)
			{
				IRQpre--;
				if (IRQpre==0)
				{
					IRQCount++;
					IRQpre = 341;
					if (IRQCount == 237)
					{
						IRQSignal = IRQa;
					}
					if (IRQCount == 256)
						IRQCount = 0;
				}
			}
		}

		public override byte ReadPRG(int addr)
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


			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int x = (addr >> 10) & 7;

				return VROM[(chr[x] << 10) + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
