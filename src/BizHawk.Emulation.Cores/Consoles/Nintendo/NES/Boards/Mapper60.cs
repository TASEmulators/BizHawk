using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper60 : NesBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_060

		private int _reg;
		private bool IsPrg16Mode => _reg.Bit(7);

#pragma warning disable CS0649
		[MapperProp]
		public int Mapper60_DipSwitch;
#pragma warning restore CS0169

		private const int DipSwitchMask = 3;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER060":
					// Hack, Reset 4-in-1 is a different board but still assign to mapper 60
					if (Cart.PrgSize != 64 || Cart.ChrSize != 32)
					{
						break;
					}

					return false;
				default:
					return false;
			}

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(_reg), ref _reg);
		}

		public override void WritePrg(int addr, byte value)
		{
			_reg = addr;
			int mirr = ((_reg & 8) >> 3) ^ 1;
			SetMirrorType(mirr > 0 ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPrg(int addr)
		{
			if ((_reg & 0x100) > 0)
			{
				return (byte)(Mapper60_DipSwitch & DipSwitchMask);
			}

			if (IsPrg16Mode)
			{
				int bank = (_reg >> 4) & 7;
				return Rom[(bank * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = (_reg >> 5) & 3;
				return Rom[(bank * 0x8000) + (addr & 0x7FFF)];
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((_reg & 7) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}

	internal sealed class Reset4in1 : NesBoardBase
	{
		private int resetSwitch = 0;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER060":
					// Hack, Reset 4-in-1 is a different board but still assign to mapper 60
					if (Cart.PrgSize == 64 && Cart.ChrSize == 32)
					{
						break;
					}
					return false;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(resetSwitch), ref resetSwitch);
			base.SyncState(ser);
		}

		public override void NesSoftReset()
		{
			resetSwitch = (resetSwitch + 1) & 3;
			base.NesSoftReset();
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(resetSwitch << 14) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(resetSwitch << 13) + addr];
			}

			return base.ReadPpu(addr);
		}
	}
}
