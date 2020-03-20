using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// caltron 6 in 1
	public sealed class Mapper041 : NesBoardBase
	{
		int prg;
		int chr;
		bool regenable;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER041":
				case "MLT-CALTRON6IN1":
					break;
				default:
					return false;
			}
			AssertPrg(256);
			AssertChr(128);
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteWram(int addr, byte value)
		{
			if (addr < 0x800)
			{
				prg = addr & 7;
				SetMirrorType((addr & 32) != 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
				regenable = (addr & 4) != 0;
				chr &= 3;
				chr |= (addr >> 1) & 0xc;
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			if (regenable)
			{
				chr &= 0xc;
				chr |= addr & 3;
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[addr | chr << 13];
			else
				return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr | prg << 15];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(regenable), ref regenable);
		}
	}
}
