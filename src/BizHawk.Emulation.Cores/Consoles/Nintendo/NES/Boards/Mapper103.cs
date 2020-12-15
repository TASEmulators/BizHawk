using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Doki Doki Panic (FDS port)
	// "BTL 2708"
	internal sealed class Mapper103 : NesBoardBase
	{
		private int prg;
		private bool romenable;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER103": // ines identification
					Cart.WramSize = 16;
					Cart.VramSize = 8;
					AssertPrg(128);
					break;
				case "BTL-2708": // ??
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WriteWram(int addr, byte value)
		{
			// writes always go to wram, even if rom is mapped in for read
			Wram[addr] = value;
		}

		public override byte ReadWram(int addr)
		{
			if (romenable)
				return Rom[addr | prg << 13];
			else
				return Wram[addr];
		}

		public override byte ReadPrg(int addr)
		{
			if (!romenable && addr >= 0x3800 && addr < 0x5800)
				return Wram[addr - 0x1800];
			else
				return Rom[addr | 0x18000];
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr >= 0x3800 && addr < 0x5800)
				Wram[addr - 0x1800] = value;
			else
			{
				switch (addr & 0x7000)
				{
					case 0x0000:
						prg = value & 15;
						break;
					case 0x6000:
						SetMirrorType((value & 8) != 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
						break;
					case 0x7000:
						romenable = (value & 16) != 0;
						break;
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(romenable), ref romenable);
			ser.Sync(nameof(prg), ref prg);
		}

	}
}
