using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper175 : NesBoardBase
	{
		private bool delay;

		private int reg;
		private bool mirr;

		private int chrReg;
		private int prgReg8;
		private int prgRegC;
		private int prgRegE;

		public override bool Configure(EDetectionOrigin origin)
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
			ser.Sync(nameof(delay), ref delay);

			ser.Sync(nameof(reg), ref reg);
			ser.Sync(nameof(mirr), ref mirr);

			ser.Sync(nameof(chrReg), ref chrReg);
			ser.Sync(nameof(prgReg8), ref prgReg8);
			ser.Sync(nameof(prgRegC), ref prgRegC);
			ser.Sync(nameof(prgRegE), ref prgRegE);


			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
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

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(chrReg << 13) + addr];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr == 0x7FFC)
			{
				delay = false;
				Sync();
			}

			if (addr < 0x4000)
			{
				return Rom[(prgReg8 << 14) + (addr & 0x3FFF)];
			}
			else if (addr < 0x6000)
			{
				return Rom[(prgRegC << 13) + (addr & 0x1FFF)];
			}
			else
			{
				return Rom[(prgRegE << 13) + (addr & 0x1FFF)];
			}
		}
	}
}
