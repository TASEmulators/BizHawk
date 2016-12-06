using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper170 : NES.NESBoardBase
	{
		private byte reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER170":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return base.ReadPRG(addr);
			}

			int last16kBank = ROM.Length - 0x4000;
			return ROM[last16kBank + (addr & 0x3FFF)];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr == 0x502 || addr == 0x1000)
			{
				reg = (byte)(value << 1 & 0x80);
			}


			base.WriteWRAM(addr, value);
		}

		public override byte ReadWRAM(int addr)
		{
			if (addr == 0x1001 || addr == 0x1777)
			{
				return (byte)(reg | NES.DB & 0x7F);
			}

			return base.ReadWRAM(addr);
		}
	}
}
