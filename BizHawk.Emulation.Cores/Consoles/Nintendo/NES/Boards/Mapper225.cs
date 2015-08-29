using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_225
	public sealed class Mapper225 : NES.NESBoardBase
	{
		bool prg_mode = false;
		int chr_reg;
		int prg_reg;
		ByteBuffer eRAM = new ByteBuffer(4);
		int chr_bank_mask_8k, prg_bank_mask_16k, prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER225":
				case "MAPPER255": // Duplicate of 225 accoring to: http://problemkaputt.de/everynes.htm
					break;
				default:
					return false;
			}
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("eRAM", ref eRAM);
			base.SyncState(ser);
		}

		public override void Dispose()
		{
			eRAM.Dispose();
			base.Dispose();
		}

		public override void WritePRG(int addr, byte value)
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

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				int bank = (prg_reg >> 1) & prg_bank_mask_32k;
				return ROM[(bank * 0x8000) + addr];
			}
			else
			{
				return ROM[((prg_reg & prg_bank_mask_16k) * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[((chr_reg & chr_bank_mask_8k) * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1800)
			{
				eRAM[(addr & 0x03)] = (byte)(value & 0x0F);
			}
		}

		public override byte ReadEXP(int addr)
		{
			if (addr >= 0x1800)
			{
				return eRAM[(addr & 0x03)];
			}
			else
			{
				return base.ReadEXP(addr);
			}
		}
	}
}
