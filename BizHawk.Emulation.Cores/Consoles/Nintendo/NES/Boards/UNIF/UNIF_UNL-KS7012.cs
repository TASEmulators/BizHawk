using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_UNL_KS7012 : NES.NESBoardBase
	{
		private int reg;
		private byte[] wram = new byte[8192];
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-KS7012":
					break;
				default:
					return false;
			}

			reg = 0xFF;

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr)
			{
				case 0xE0A0:
					reg = 0; break;
				case 0xEE36:
					reg = 1; break;
			}
		}

		public override byte ReadWRAM(int addr)
		{
			return wram[addr];
			//int offset = ROM.Length - 0x2000;
			//return ROM[offset + addr];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			wram[addr] = value;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[((reg & 1) << 15) + addr];
		}
	}
}
