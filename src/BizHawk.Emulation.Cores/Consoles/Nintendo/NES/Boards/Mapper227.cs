using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper227 : NesBoardBase
	{
		private int _prgBankMask16K;
		private int prg;
		private bool _vramProtected;
		private byte[] _prgBanks16K = new byte[2];

		// 1200-in-1
		// [NJXXX] Xiang Shuai Chuan Qi
		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER227":
					//AssertVram(16);
					Cart.VramSize = 16;
					break;
				default:
					return false;
			}
			_prgBankMask16K = (Cart.PrgSize / 16) - 1;

			SetMirrorType(EMirrorType.Vertical);
			_vramProtected = false;
			_prgBanks16K[0] = _prgBanks16K[1] = 0;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(_prgBanks16K), ref _prgBanks16K, false);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = _prgBanks16K[bank_16k];
			bank_16k &= _prgBankMask16K;
			addr = (bank_16k << 14) | ofs;
			return Rom[addr];
		}

		public override void WritePrg(int addr, byte value)
		{
			bool S = addr.Bit(0);
			bool M_horz = addr.Bit(1);
			int p = (addr >> 2) & 0x1F;
			p += addr.Bit(8) ? 0x20 : 0;
			bool o = addr.Bit(7);
			bool L = addr.Bit(9);

			//virtuaNES doesnt do this.
			//fceux does it...
			//if we do it, [NJXXX] Xiang Shuai Chuan Qi will not be able to set any patterns
			//maybe only the multicarts do it, to keep the game from clobbering vram on accident
			//vram_protected = o;

			if (o && !S)
			{
				_prgBanks16K[0] = (byte)(p);
				_prgBanks16K[1] = (byte)(p);
			}
			if (o && S)
			{
				_prgBanks16K[0] = (byte)((p & ~1));
				_prgBanks16K[1] = (byte)((p & ~1) + 1);
			}
			if (!o && !S && !L)
			{
				_prgBanks16K[0] = (byte)p;
				_prgBanks16K[1] = (byte)(p & 0x38);
			}
			if (!o && S && !L)
			{
				_prgBanks16K[0] = (byte)(p & 0x3E);
				_prgBanks16K[1] = (byte)(p & 0x38);
			}
			if (!o && !S && L)
			{
				_prgBanks16K[0] = (byte)p;
				_prgBanks16K[1] = (byte)(p | 0x07);
			}
			if (!o && S && L)
			{
				_prgBanks16K[0] = (byte)(p & 0x3E);
				_prgBanks16K[1] = (byte)(p | 0x07);
			}

			_prgBanks16K[0] = (byte)(_prgBanks16K[0]&_prgBankMask16K);
			_prgBanks16K[1] = (byte)(_prgBanks16K[1]&_prgBankMask16K);

			if (M_horz) SetMirrorType(EMirrorType.Horizontal);
			else SetMirrorType(EMirrorType.Vertical);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (_vramProtected) 
					return;
				else base.WritePpu(addr, value);
			}
			else base.WritePpu(addr, value);
		}
	}
}
