using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// china
	// behavior from fceux
	internal sealed class Mapper177 : NesBoardBase
	{
		private int prg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER177":
					break;
				default:
					return false;
			}
			AssertPrg(1024);
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			prg = value & 0x1f;

			if ((value & 0x20) != 0)
				SetMirrorType(EMirrorType.Horizontal);
			else
				SetMirrorType(EMirrorType.Vertical);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr | prg << 15];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
		}
	}
}
