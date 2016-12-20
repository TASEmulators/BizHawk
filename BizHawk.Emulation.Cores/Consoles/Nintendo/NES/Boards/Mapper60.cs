using System;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper60 : NES.NESBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_060

		private int _reg;
		private bool IsPrg16Mode { get { return _reg.Bit(7); } }

		[MapperProp]
		public int Mapper60_DipSwitch;

		private const int DipSwitchMask = 3;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER060":
					// Hack, Reset 4-in-1 is a different board but still assign to mapper 60
					if (Cart.prg_size != 64 || Cart.chr_size != 32)
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
			ser.Sync("_reg", ref _reg);
		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = addr;

			int mirr = ((_reg & 8) >> 3) ^ 1;

			SetMirrorType(mirr > 0 ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPRG(int addr)
		{
			if ((_reg & 0x100) > 0)
			{
				return (byte)(Mapper60_DipSwitch & DipSwitchMask);
			}

			if (IsPrg16Mode)
			{
				int bank = (_reg >> 4) & 7;
				return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = (_reg >> 5) & 3;
				return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{

				return VROM[((_reg & 7) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}

	public class Reset4in1 : NES.NESBoardBase
	{
		private int resetSwitch = 0;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER060":
					// Hack, Reset 4-in-1 is a different board but still assign to mapper 60
					if (Cart.prg_size == 64 && Cart.chr_size == 32)
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
			ser.Sync("resetSwitch", ref resetSwitch);
			base.SyncState(ser);
		}

		public override void NESSoftReset()
		{
			resetSwitch = (resetSwitch + 1) & 3;
			base.NESSoftReset();
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(resetSwitch << 14) + (addr & 0x3FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(resetSwitch << 13) + addr];
			}

			return base.ReadPPU(addr);
		}
	}
}
