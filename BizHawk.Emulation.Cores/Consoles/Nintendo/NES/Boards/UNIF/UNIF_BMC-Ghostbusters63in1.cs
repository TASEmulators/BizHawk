using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	public sealed class UNIF_BMC_Ghostbusters63in1 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(2);
		private readonly int[] banks = new [] { 0, 0, 524288, 1048576};
		private int bank;

		[MapperProp]
		public bool Ghostbusters63in1_63set=true;
		[MapperProp]
		public int Ghostbusters63in1_chip_22_select;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-Ghostbusters63in1":
					break;
				default:
					return false;
			}

			AutoMapperProps.Apply(this);

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
			ser.Sync("bank", ref bank);
			ser.Sync("bank", ref Ghostbusters63in1_63set);
			ser.Sync("bank", ref Ghostbusters63in1_chip_22_select);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			reg[addr & 1] = value;

			bank = ((reg[0] & 0x80) >> 7) | ((reg[1] & 1) << 1);

			SetMirrorType(reg[0].Bit(6) ? EMirrorType.Vertical : EMirrorType.Horizontal);
			Console.WriteLine(reg[0]);
			Console.WriteLine(reg[1]);
		}

		public override byte ReadPRG(int addr)
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
				return ROM[offset + (b << 14) + (addr & 0x3FFF)];
			}
			else
			{
				var offset = 0;
				if (Ghostbusters63in1_63set)
					offset = banks[bank];
				else
					offset = banks[Ghostbusters63in1_chip_22_select];

				int b = ((reg[0] >> 1) & 0x0F);
				return ROM[offset + (b << 15) + addr];
			} 
		}
	}
}
