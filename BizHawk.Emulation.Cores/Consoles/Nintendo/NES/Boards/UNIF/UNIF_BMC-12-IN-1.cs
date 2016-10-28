using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_12_IN_1 : NES.NESBoardBase
	{
		private ByteBuffer regs = new ByteBuffer(2);
		private byte ctrl;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
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
			ser.Sync("regs", ref regs);
			ser.Sync("ctrl", ref ctrl);
			base.SyncState(ser);
		}

		public override void Dispose()
		{
			regs.Dispose();
			base.Dispose();
		}

		public override void WritePRG(int addr, byte value)
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
			if (horizontal)
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int basebank = (ctrl & 3) << 3;
				int bank = 0;
				if (addr < 0x1000)
				{
					bank = regs[0] >> 3 | (basebank << 2);
				}
				else
				{
					bank = regs[1] >> 3 | (basebank << 2);
				}

				return VROM[(bank << 12) + (addr & 0xFFF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			var basebank = (ctrl & 3) << 3;
			int bank = 0;

			if (ctrl.Bit(3))
			{
				if (addr < 0x4000)
				{
					bank = basebank | (regs[0] & 6) | 0;
					
				}
				else
				{
					bank = basebank | (regs[0] & 6) | 1;
				}
			}
			else
			{
				if (addr < 0x4000)
				{
					bank = basebank | (regs[0] & 7);
				}
				else
				{
					bank = basebank | 7;
				}
			}

			return ROM[(bank << 14) + (addr & 0x3FFF)];
		}
	}
}
