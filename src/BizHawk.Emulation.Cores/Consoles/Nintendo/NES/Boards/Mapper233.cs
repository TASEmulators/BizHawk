using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_233
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper233 : NesBoardBase
	{
		public int prg_page;
		public bool prg_mode;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER233":
					break;
				default:
					return false;
			}

			prg_mode = false;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_page), ref prg_page);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			prg_page = value & 0x1F;
			prg_mode = value.Bit(5);

			int mirror = value >> 6;
			switch (mirror)
			{
				case 0:
					SetMirroring(0, 0, 0, 1);
					break;
				case 1:
					SetMirrorType(EMirrorType.Vertical);
					break;
				case 2:
					SetMirrorType(EMirrorType.Horizontal);
					break;
				case 3:
					SetMirrorType(EMirrorType.OneScreenB);
					break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			if (!prg_mode)
			{
				return Rom[((prg_page >> 1) * 0x8000) + addr];
			}

			return Rom[(prg_page * 0x4000) + (addr & 0x3FFF)];
		}
	}
}
