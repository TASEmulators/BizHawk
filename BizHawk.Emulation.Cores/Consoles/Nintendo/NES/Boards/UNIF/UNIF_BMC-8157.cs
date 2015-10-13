using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 4-in-1 1993 (CK-001) [U][!].unf
	public class UNIF_BMC_8157 : NES.NESBoardBase
	{
		[MapperProp]
		public bool _4in1Mode;

		private int _cmdreg;

		private int _prgMask16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-8157":
					break;
				default:
					return false;
			}

			_prgMask16k = Cart.prg_size / 16 - 1;

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("cmdreg", ref _cmdreg);
			ser.Sync("4in1Mode", ref _4in1Mode);
		}

		public override void WritePRG(int addr, byte value)
		{
			_cmdreg = addr;
			int mir = ((_cmdreg & 2) >> 1) ^ 1;
			SetMirrorType(mir == 1 ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPRG(int addr)
		{
			if (_4in1Mode)
			{
				if (((_cmdreg & 0x100) > 0) && Cart.prg_size < 1024)
				{
					addr = (addr & 0xFFF0) + (1);
				}
			}

			int basei = ((_cmdreg & 0x060) | ((_cmdreg & 0x100) >> 1)) >> 2;
			int bank = (_cmdreg & 0x01C) >> 2;
			int lbank = ((_cmdreg & 0x200) > 0) ? 7 : (((_cmdreg & 0x80) > 0) ? bank : 0);

			int final = basei | (addr < 0x4000 ? bank : lbank);
			return ROM[((final & _prgMask16k) * 0x4000) + (addr & 0x3FFF)];
		}
	}
}
