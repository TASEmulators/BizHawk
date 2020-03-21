namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// sachen
	// NROM plus random copy protection circuit

	// dancing blocks refuses to run; see comments below	
	internal sealed class Mapper143 : NesBoardBase
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER143":
				case "UNIF_UNL-SA-NROM":
					break;
				default:
					return false;
			}
			AssertPrg(32);
			AssertChr(8);
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override byte ReadExp(int addr)
		{
			if ((addr & 0x100) != 0)
				return (byte)(NES.DB & 0xc0 | ~addr & 0x3f);
			else
				return NES.DB;
		}

		/* if this awful hack is uncommented, dancing blocks runs
		   it initializes ram to a different pseudo-random pattern

		bool first = true;
		public override void ClockPPU()
		{
			if (first)
			{
				first = false;
				for (int i = 0; i < 0x800; i++)
					NES.WriteMemory((ushort)i, (i & 4) != 0 ? (byte)0x7f : (byte)0x00);
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("first", ref first);
		}
		*/
	}
}
