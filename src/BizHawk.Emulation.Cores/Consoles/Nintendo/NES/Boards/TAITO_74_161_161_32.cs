using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//Mapper 152
	//Arkanoid 2 (J)
	//Gegege no Kitarou 2

	internal sealed class TAITO_74_161_161_32 : NesBoardBase
	{
		private int chr;
		private int prg_bank_mask_16k;
		private byte prg_bank_16k;
		private byte[] prg_banks_16k = new byte[2];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER152":
					break;
				case "TAITO-74*161/161/32":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.PadH, Cart.PadV);
			prg_bank_mask_16k = (Cart.PrgSize / 16) - 1;
			prg_banks_16k[1] = 0xFF;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg_bank_mask_16k), ref prg_bank_mask_16k);
			ser.Sync(nameof(prg_bank_16k), ref prg_bank_16k);
			ser.Sync(nameof(prg_banks_16k), ref prg_banks_16k, false);
		}

		private void SyncPRG()
		{
			prg_banks_16k[0] = prg_bank_16k;
		}

		public override void WritePrg(int addr, byte value)
		{
			chr = (byte)(value & 15);
			prg_bank_16k = (byte)((value >> 4) & 7);
			SyncPRG();

			if (value.Bit(7))
				SetMirrorType(EMirrorType.OneScreenB);
			else
				SetMirrorType(EMirrorType.OneScreenA);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			bank_16k &= prg_bank_mask_16k;
			addr = (bank_16k << 14) | ofs;
			return Rom[addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPpu(addr);
		}
	}
}
