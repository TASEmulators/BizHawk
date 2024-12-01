using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	internal sealed class UNIF_BMC_Ghostbusters63in1 : NesBoardBase
	{
		private byte[] reg = new byte[2];
		private readonly int[] banks = { 0, 0, 524288, 1048576 };
		private int bank;

		[MapperProp]
		public bool Ghostbusters63in1_63set=true;
		[MapperProp]
		public int Ghostbusters63in1_chip_22_select;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_BMC-Ghostbusters63in1":
					break;
				default:
					return false;
			}

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg), ref reg, false);
			ser.Sync(nameof(bank), ref bank);
			ser.Sync(nameof(bank), ref Ghostbusters63in1_63set);
			ser.Sync(nameof(bank), ref Ghostbusters63in1_chip_22_select);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			reg[addr & 1] = value;

			bank = ((reg[0] & 0x80) >> 7) | ((reg[1] & 1) << 1);

			SetMirrorType(reg[0].Bit(6) ? EMirrorType.Vertical : EMirrorType.Horizontal);
			Console.WriteLine(reg[0]);
			Console.WriteLine(reg[1]);
		}

		public override byte ReadPrg(int addr)
		{
			//if (bank == 1)
			//{
			//	return NES.DB;
			//}

			if (reg[0].Bit(5))
			{
				var offset=0;
				if (Ghostbusters63in1_63set)
					offset = banks[bank];
				else
					offset = banks[Ghostbusters63in1_chip_22_select];

				int b = (reg[0] & 0x1F);
				return Rom[offset + (b << 14) + (addr & 0x3FFF)];
			}
			else
			{
				var offset = 0;
				if (Ghostbusters63in1_63set)
					offset = banks[bank];
				else
					offset = banks[Ghostbusters63in1_chip_22_select];

				int b = ((reg[0] >> 1) & 0x0F);
				return Rom[offset + (b << 15) + addr];
			} 
		}
	}
}
