using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_BMC_12_IN_1 : NesBoardBase
	{
		private byte[] regs = new byte[2];
		private byte ctrl;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_BMC-12-IN-1":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(ctrl), ref ctrl);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr & 0xE000)
			{
				case 0xA000:
					regs[0] = value;
					SetMirroring(ctrl.Bit(2));
					break;
				case 0xC000:
					regs[1] = value;
					SetMirroring(ctrl.Bit(2));
					break;
				case 0xE000:
					ctrl = (byte)(value & 0x0F);
					SetMirroring(ctrl.Bit(2));
					break;
			}
		}

		private void SetMirroring(bool horizontal)
		{
			SetMirrorType(horizontal ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int baseBank = (ctrl & 3) << 3;
				int bank;
				if (addr < 0x1000)
				{
					bank = regs[0] >> 3 | (baseBank << 2);
				}
				else
				{
					bank = regs[1] >> 3 | (baseBank << 2);
				}

				return Vrom[(bank << 12) + (addr & 0xFFF)];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			var baseBank = (ctrl & 3) << 3;
			int bank;
			if (ctrl.Bit(3))
			{
				if (addr < 0x4000)
				{
					bank = baseBank | (regs[0] & 6) | 0;
					
				}
				else
				{
					bank = baseBank | (regs[0] & 6) | 1;
				}
			}
			else
			{
				if (addr < 0x4000)
				{
					bank = baseBank | (regs[0] & 7);
				}
				else
				{
					bank = baseBank | 7;
				}
			}

			return Rom[(bank << 14) + (addr & 0x3FFF)];
		}
	}
}
