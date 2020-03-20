using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper170 : NesBoardBase
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
			ser.Sync(nameof(reg), ref reg);
			base.SyncState(ser);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
			{
				return base.ReadPrg(addr);
			}

			int last16kBank = Rom.Length - 0x4000;
			return Rom[last16kBank + (addr & 0x3FFF)];
		}

		public override void WriteWram(int addr, byte value)
		{
			if (addr == 0x502 || addr == 0x1000)
			{
				reg = (byte)(value << 1 & 0x80);
			}


			base.WriteWram(addr, value);
		}

		public override byte ReadWram(int addr)
		{
			if (addr == 0x1001 || addr == 0x1777)
			{
				return (byte)(reg | NES.DB & 0x7F);
			}

			return base.ReadWram(addr);
		}
	}
}
