using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper035 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(8);
		private ByteBuffer chr = new ByteBuffer(8);

		private int prg_bank_mask_8k;
		private int chr_bank_mask_1k;

		private bool IRQa;
		private int IRQCount;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER035":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size / 1 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void Dispose()
		{
			reg.Dispose();
			chr.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			ser.Sync("chr", ref chr);
			ser.Sync("IRQa", ref IRQa);
			ser.Sync("IRQCount", ref IRQCount);
			base.SyncState(ser);
		}

		public override byte ReadEXP(int addr)
		{
			if (addr == 0x800)
			{
				return 0x20;
			}

			return base.ReadEXP(addr);
		}

		private void SetMirror()
		{
			SetMirrorType(reg[3].Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr)
			{
				case 0x8000: reg[0] = value; break;
				case 0x8001: reg[1] = value; break;
				case 0x8002: reg[2] = value; break;
				case 0x9000: chr[0] = value; break;
				case 0x9001: chr[1] = value; break;
				case 0x9002: chr[2] = value; break;
				case 0x9003: chr[3] = value; break;
				case 0x9004: chr[4] = value; break;
				case 0x9005: chr[5] = value; break;
				case 0x9006: chr[6] = value; break;
				case 0x9007: chr[7] = value; break;

				case 0xC002: IRQa = false; IRQSignal = false; break;
				case 0xC005: IRQCount = value; break;
				case 0xC003: IRQa = true; break;

				case 0xD001: reg[3] = value;
					SetMirror(); break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int index = addr >> 13;

			int bank = reg[index];
			if (index == 3) { bank = 0xFF; }
			bank &= prg_bank_mask_8k;

			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int index = addr >> 10;
				int bank = chr[index];
				bank &= chr_bank_mask_1k;

				return VROM[(bank << 10) + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}

		public override void ClockCPU()
		{
			IrqHook(1);
		}

		private void IrqHook(int a)
		{
			if (IRQa)
			{
				IRQCount += a;
				if (IRQCount > 0x10000)
				{
					IRQSignal = true;
					IRQa = false;
				}
			}
		}
	}
}
