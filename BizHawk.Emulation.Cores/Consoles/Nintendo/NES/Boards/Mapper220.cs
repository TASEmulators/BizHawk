using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Ported from FCEUX
	public sealed class Mapper220 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(8);
		private int prg_mask_2k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-KS7057":
				case "MAPPER220":
					break;
				default:
					return false;
			}

			prg_mask_2k = Cart.prg_size / 2 - 1;
			return true;
		}

		public override void Dispose()
		{
			reg.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr & 0xF003)
			{
				case 0x8000:
				case 0x8001:
				case 0x8002:
				case 0x8003:
				case 0x9000:
				case 0x9001:
				case 0x9002:
				case 0x9003:
					SetMirrorType(value.Bit(0) ? EMirrorType.Vertical : EMirrorType.Horizontal);
					break;
				case 0xB000: reg[0] = (byte)((reg[0] & 0xF0) | (value & 0x0F)); break;
				case 0xB001: reg[0] = (byte)((reg[0] & 0x0F) | (value << 4)); break;
				case 0xB002: reg[1] = (byte)((reg[1] & 0xF0) | (value & 0x0F)); break;
				case 0xB003: reg[1] = (byte)((reg[1] & 0x0F) | (value << 4)); break;
				case 0xC000: reg[2] = (byte)((reg[2] & 0xF0) | (value & 0x0F)); break;
				case 0xC001: reg[2] = (byte)((reg[2] & 0x0F) | (value << 4)); break;
				case 0xC002: reg[3] = (byte)((reg[3] & 0xF0) | (value & 0x0F)); break;
				case 0xC003: reg[3] = (byte)((reg[3] & 0x0F) | (value << 4)); break;
				case 0xD000: reg[4] = (byte)((reg[4] & 0xF0) | (value & 0x0F)); break;
				case 0xD001: reg[4] = (byte)((reg[4] & 0x0F) | (value << 4)); break;
				case 0xD002: reg[5] = (byte)((reg[5] & 0xF0) | (value & 0x0F)); break;
				case 0xD003: reg[5] = (byte)((reg[5] & 0x0F) | (value << 4)); break;
				case 0xE000: reg[6] = (byte)((reg[6] & 0xF0) | (value & 0x0F)); break;
				case 0xE001: reg[6] = (byte)((reg[6] & 0x0F) | (value << 4)); break;
				case 0xE002: reg[7] = (byte)((reg[7] & 0xF0) | (value & 0x0F)); break;
				case 0xE003: reg[7] = (byte)((reg[7] & 0x0F) | (value << 4)); break;
			}
		}

		public override byte ReadWRAM(int addr)
		{
			int i = ((addr >> 11) & 3) + 4;
			int bank = reg[i] & prg_mask_2k;
			return ROM[(bank << 11) + (addr & 0x7FF)];
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x2000)
			{
				int i = (addr >> 11) & 3;
				int bank = reg[i] & prg_mask_2k;
				return ROM[(bank << 11) + (addr & 0x7FF)];
			}
			else if (addr < 0x4000)
			{
				return ROM[0x1A000 /* bank 0xd*/ + (addr & 0x1FFF)];
			}
			else
			{
				return ROM[0x1C000 /* bank 7*/ + (addr & 0x3FFF)];
			}
		}
	}
}
