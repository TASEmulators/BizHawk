using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Super Gun 20-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_214
	internal sealed class Mapper214 : NesBoardBase
	{
		private int _chrReg, _prgReg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER214":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrReg", ref _chrReg);
		}

		public override void WritePrg(int addr, byte value)
		{
			_chrReg = addr & 3;
			_prgReg = (addr >> 2) & 3;
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(_prgReg * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(_chrReg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
