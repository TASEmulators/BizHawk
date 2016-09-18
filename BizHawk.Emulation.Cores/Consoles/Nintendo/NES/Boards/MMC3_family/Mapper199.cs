using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper199 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER199":
					break;
				default:
					return false;
			}

			exRegs[0] = (byte)(Cart.prg_size / 8 - 2);
			exRegs[1] = (byte)(Cart.prg_size / 8 - 1);
			exRegs[2] = 1;
			exRegs[3] = 3;

			BaseSetup();
			mmc3.MirrorMask = 3;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			if (addr < 0x400)
			{
				return mmc3.regs[0];
			}
			else if (addr < 0x800)
			{
				return exRegs[2];
			}
			else if (addr < 0xC00)
			{
				return mmc3.regs[1];
			}
			else if (addr < 0x1000)
			{
				return exRegs[3];
			}

			return base.Get_CHRBank_1K(addr);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			if (addr >= 0x4000 && addr < 0x6000)
			{
				return exRegs[0];
			}
			else if (addr >= 0x6000)
			{
				return exRegs[1];
			}

			return base.Get_PRGBank_8K(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr == 1)
			{
				if ((mmc3.cmd & 0x8) > 0)
				{
					exRegs[mmc3.cmd & 3] = value;
				}
			}

			base.WritePRG(addr, value);
		}
	}
}
