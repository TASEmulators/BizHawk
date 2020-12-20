using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 4-in-1 1993 (CK-001) [U][!].unf
	internal class UNIF_BMC_8157 : NesBoardBase
	{
		[MapperProp]
		public bool _4in1Mode;

		private int _cmdreg;
		private int _prgMask16k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_BMC-8157":
					break;
				default:
					return false;
			}

			_prgMask16k = Cart.PrgSize / 16 - 1;
			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("cmdreg", ref _cmdreg);
			ser.Sync("4in1Mode", ref _4in1Mode);
		}

		public override void WritePrg(int addr, byte value)
		{
			_cmdreg = addr;
			int mir = ((_cmdreg & 2) >> 1) ^ 1;
			SetMirrorType(mir == 1 ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPrg(int addr)
		{
			if (_4in1Mode)
			{
				if (((_cmdreg & 0x100) > 0) && Cart.PrgSize < 1024)
				{
					addr = (addr & 0xFFF0) + 1;
				}
			}

			int basei = ((_cmdreg & 0x060) | ((_cmdreg & 0x100) >> 1)) >> 2;
			int bank = (_cmdreg & 0x01C) >> 2;
			int lbank = ((_cmdreg & 0x200) > 0) ? 7 : (((_cmdreg & 0x80) > 0) ? bank : 0);

			int final = basei | (addr < 0x4000 ? bank : lbank);
			return Rom[((final & _prgMask16k) * 0x4000) + (addr & 0x3FFF)];
		}
	}
}
