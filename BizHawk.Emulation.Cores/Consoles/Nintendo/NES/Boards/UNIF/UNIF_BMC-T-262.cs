using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_BMC_T_262 : NES.NESBoardBase
	{
		private bool _mode;
		private bool _locked;
		private bool _verticalMirror;

		private int _base;
		private int _bank;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-T-262":
					break;
				default:
					return false;
			}

			SetMirroring();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("mode", ref _mode);
			ser.Sync("locked", ref _locked);
			ser.Sync("verticalMirror", ref _verticalMirror);

			ser.Sync("base", ref _base);
			ser.Sync("bank", ref _bank);

			base.SyncState(ser);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				int bank = _base | _bank;
				return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = _base | (_mode ? _bank : 7);
				return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			if (!_locked)
			{
				_base = ((addr & 0x60) >> 2) | ((addr & 0x100) >> 3);
				_mode = (addr & 0x80) > 0;
				_verticalMirror = (((addr & 2) >> 1) ^ 1) > 0;
				_locked = ((addr & 0x2000) >> 13) > 0;
			}

			_bank = value & 0x07;
			SetMirroring();
		}

		private void SetMirroring()
		{
			SetMirrorType(_verticalMirror ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}
	}
}
