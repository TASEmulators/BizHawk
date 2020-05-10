using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
PCB Class: Unknown
iNES Mapper 242
PRG-ROM: 32KB
PRG-RAM: None
CHR-ROM: 16KB
CHR-RAM: None
Battery is not available
mirroring - both
	 * 
	 * Games:
	 * Wai Xing Zhan Shi (Ch)
	 */

	internal sealed class Mapper242 : NesBoardBase
	{
		int prg;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER242":
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr + (prg * 0x8000)];
		}

		public override void WritePrg(int addr, byte value)
		{
			prg = (addr >> 3) & 15;
			//fceux had different logic here for the mirroring, but that didnt match with experiments on dragon quest 8 nor disch's docs
			//i changed it at the same time
			bool mirror = addr.Bit(1);
			SetMirrorType(mirror ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
		}
	}
}
