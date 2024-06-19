using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_225
	internal sealed class Mapper225 : NesBoardBase
	{
		private bool prg_mode = false;
		private int chr_reg;
		private int prg_reg;
		private byte[] eRAM = new byte[4];
		private int chr_bank_mask_8k, prg_bank_mask_16k, prg_bank_mask_32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER225":
				case "MAPPER255": // Duplicate of 225 accoring to: http://problemkaputt.de/everynes.htm
					break;
				default:
					return false;
			}
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;
			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);
			ser.Sync(nameof(chr_reg), ref chr_reg);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync(nameof(eRAM), ref eRAM, false);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			prg_mode = addr.Bit(12);
			if (addr.Bit(13))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			int high = (addr & 0x4000) >> 8;
			prg_reg = (addr >> 6) & 0x3F | high;
			chr_reg = addr & 0x3F | high;
		}

		public override byte ReadPrg(int addr)
		{
			if (!prg_mode)
			{
				int bank = (prg_reg >> 1) & prg_bank_mask_32k;
				return Rom[(bank * 0x8000) + addr];
			}
			else
			{
				return Rom[((prg_reg & prg_bank_mask_16k) * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((chr_reg & chr_bank_mask_8k) * 0x2000) + addr];
			}

			return base.ReadPpu(addr);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1800)
			{
				eRAM[(addr & 0x03)] = (byte)(value & 0x0F);
			}
		}

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x1800)
			{
				return eRAM[addr & 0x03];
			}

			return base.ReadExp(addr);
		}
	}
}
