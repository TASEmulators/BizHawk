using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_061
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper61 : NES.NESBoardBase
	{
		public int prg_page;
		public bool prg_mode;
		public int prg_byte_mask;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER061":
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			prg_page = 0;
			prg_mode = false;
			prg_byte_mask = Cart.prg_size * 1024 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_page), ref prg_page);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr.Bit(7))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			prg_mode = addr.Bit(4);
			prg_page = ((addr & 0x0F) << 1) | ((addr & 0x20) >> 5);
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				return ROM[(((prg_page >> 1) * 0x8000) + addr) & prg_byte_mask];
			}
			else
			{
				return ROM[((prg_page * 0x4000) + (addr & 0x03FFF)) & prg_byte_mask];
			}
		}
	}
}
