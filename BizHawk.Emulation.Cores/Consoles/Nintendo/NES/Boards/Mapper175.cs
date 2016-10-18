using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper175 : NES.NESBoardBase
	{
		private bool delay;

		private int reg;
		private bool mirr;

		private int chrReg;
		private int prgReg8;
		private int prgRegC;
		private int prgRegE;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER175":
					break;
				default:
					return false;
			}

			Sync();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("delay", ref delay);

			ser.Sync("reg", ref reg);
			ser.Sync("mirr", ref mirr);

			ser.Sync("chrReg", ref chrReg);
			ser.Sync("prgReg8", ref prgReg8);
			ser.Sync("prgRegC", ref prgRegC);
			ser.Sync("prgRegE", ref prgRegE);


			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr == 0)
			{
				mirr = value.Bit(2);
				delay = true;
				Sync();
			}
			else if (addr == 0x2000)
			{
				reg = value & 0x0F;
				delay = true;
				Sync();
			}
		}

		private void Sync()
		{
			SetMirrorType(mirr ? EMirrorType.Horizontal : EMirrorType.Vertical);
			chrReg = reg;
			prgRegE = (reg << 1) + 1;
			if (!delay)
			{
				prgReg8 = reg;
				prgRegC = reg << 1;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(chrReg << 13) + addr];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr == 0x7FFC)
			{
				delay = false;
				Sync();
			}

			if (addr < 0x4000)
			{
				return ROM[(prgReg8 << 14) + (addr & 0x3FFF)];
			}
			else if (addr < 0x6000)
			{
				return ROM[(prgRegC << 13) + (addr & 0x1FFF)];
			}
			else
			{
				return ROM[(prgRegE << 13) + (addr & 0x1FFF)];
			}
		}
	}
}
