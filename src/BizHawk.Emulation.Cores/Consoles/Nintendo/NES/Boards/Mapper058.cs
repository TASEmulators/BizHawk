using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_058
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper058 : NesBoardBase
	{
		private bool prg_mode = false;
		private int chr_reg;
		private int prg_reg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER058":
					break;
				default:
					return false;
			}
			AssertChr(8, 16, 32, 64);
			AssertPrg(16, 32, 64, 128, 256);

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);
			ser.Sync(nameof(chr_reg), ref chr_reg);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			prg_mode = addr.Bit(6);
			if (addr.Bit(7))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			prg_reg = addr & 0x07;
			chr_reg = (addr >> 3) & 0x07;
		}

		public override byte ReadPrg(int addr)
		{
			if (!prg_mode)
			{
				return Rom[((prg_reg >> 1) * 0x8000) + addr];
			}
			else
			{
				return Rom[(prg_reg * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(chr_reg * 0x2000) + addr];
			}
			return base.ReadPpu(addr);
		}
	}
}
