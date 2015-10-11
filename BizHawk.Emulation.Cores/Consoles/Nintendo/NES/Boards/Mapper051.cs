using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper051 : NES.NESBoardBase
	{
		private int _bank;
		private int _mode = 2;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER051":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void NESSoftReset()
		{
			_bank = 0;
			_mode = 2;
			base.NESSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank", ref _bank);
			ser.Sync("mode", ref _mode);
		}

		public override byte ReadWRAM(int addr)
		{
			int prgBank8k;
			if ((_mode & 0x02) > 0)
			{
				prgBank8k = ((_bank & 7) << 2) | 0x23;
			}
			else
			{
				prgBank8k = ((_bank & 4) << 2) | 0x2F;
			}

			return ROM[(prgBank8k * 0x2000) + addr];
		}

		public override byte ReadPRG(int addr)
		{
			int prgBank16k_8;
			int prgBank16k_C;

			int prgBank;

			if ((_mode & 0x02) > 0)
			{
				prgBank16k_8 = (_bank << 1) | 0;
				prgBank16k_C = (_bank << 1) | 1;

				prgBank = _bank << 1;
			}
			else
			{
				prgBank16k_8 = (_bank << 1) | (_mode >> 4);
				prgBank16k_C = ((_bank & 0xC) << 1) | 7;
			}

			if (addr < 0x4000)
			{
				return ROM[(prgBank16k_8 * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				return ROM[(prgBank16k_C * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				_mode = value & 0x012;
				SyncMirroring();
			}
			else
			{
				base.WriteWRAM(addr, value);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			_bank = value & 0x0F;
			if ((addr & 0x4000) > 0)
			{
				_mode = (_mode & 0x02) | (value & 0x10);
			}

			SyncMirroring();
		}

		private void SyncMirroring()
		{
			if (_mode == 0x12)
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}
		}
	}
}
