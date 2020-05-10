using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper241 : NesBoardBase
	{
		//163 is for nanjing games

		//configuration
		int prg_bank_mask_32k;

		//state
		byte[] prg_banks_32k = new byte[1];

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER241":
					break;
				default:
					return false;
			}
			prg_bank_mask_32k = (Cart.PrgSize / 32) - 1;

			prg_banks_32k[0] = 0;

			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override byte ReadExp(int addr)
		{
			//some kind of magic number..
			return 0x50;
		}

		public override void WritePrg(int addr, byte value)
		{
			prg_banks_32k[0] = value;
			ApplyMemoryMapMask(prg_bank_mask_32k, prg_banks_32k);
		}

		public override byte ReadPrg(int addr)
		{
			addr = ApplyMemoryMap(15, prg_banks_32k, addr);
			return Rom[addr];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_32k), ref prg_banks_32k, false);
		}
	}
}
