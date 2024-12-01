using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Strike Wolf (MGC-014) [!].nes
	// Using https://wiki.nesdev.com/w/index.php/INES_Mapper_036
	internal sealed class Mapper036 : NesBoardBase
	{
		private int chr;
		private int prg;
		private int chr_mask;
		private int prg_mask;
		private byte R;
		private bool M;
		private byte P;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER036":
					AssertVram(0);
					Cart.WramSize = 0; // AssertWram(0); // GoodNES good dump of Strike Wolf specifies 8kb of wram
					break;
				default:
					return false;
			}
			chr_mask = Cart.ChrSize / 8 - 1;
			prg_mask = Cart.PrgSize / 32 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
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

		public override void WritePrg(int addr, byte value)
		{
			// either hack emulation of a weird bus conflict, or crappy pirate safeguard
			prg = (R >> 4) & prg_mask;
		}

		public override byte ReadExp(int addr)
		{
			return (byte)(R | (NES.DB & 0xCF));
		}

		public override void WriteExp(int addr, byte value)
		{
			Console.WriteLine(addr);
			Console.WriteLine(value);
			if ((addr & 0xE200) == 0x200)
			{
				chr = value & 15 & chr_mask;
			}
			switch (addr & 0xE103)
			{
				case 0x100:
					if (!M)
					{
						R = P;
					}
					else
					{
						R++;
						R &= 0x30;
					}
					

					break;
				case 0x102:
					P = (byte)(value & 0x30);
					prg = (value >> 4) & prg_mask;
					break;
				case 0x103:
					M = value.Bit(4);
					break;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(R), ref R);
			ser.Sync(nameof(M), ref M);
			ser.Sync(nameof(P), ref P);
		}
	}
}
