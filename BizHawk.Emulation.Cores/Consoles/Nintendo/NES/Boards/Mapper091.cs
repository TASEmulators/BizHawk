using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper91 : NesBoardBase
	{
		/*
		*Note: Street Fighter III (Unl) is actually mapper 197.  However variations such as Street Fighter III (9 Fighter) and Mari Street Fighter III use this mapper
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_091
		*/

		byte[] chr_regs_2k = new byte[4];
		byte[] prg_regs_8k = new byte[4];
		int chr_bank_mask_2k, prg_bank_mask_8k;
		MMC3 mmc3;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER091":
					break;
				default:
					return false;
			}

			int chrSize = Cart.chr_size;
			if (chrSize > 256) // Hack to support some bad dumps
			{
				chrSize = 512;
			}

			chr_bank_mask_2k = chrSize / 2 - 1;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;

			prg_regs_8k[3] = 0xFF;
			prg_regs_8k[2] = 0xFE;
			
			mmc3 = new MMC3(this, 0x7FFFFFFF);

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			mmc3.SyncState(ser);
			ser.Sync("prg_regs", ref prg_regs_8k, false);
			ser.Sync("chr_regs", ref chr_regs_2k, false);
			base.SyncState(ser);
		}

		public override void WriteWram(int addr, byte value)
		{
			switch (addr & 0x7003)
			{
				case 0x0000:
					chr_regs_2k[0] = value;
					break;
				case 0x0001:
					chr_regs_2k[1] = value;
					break;
				case 0x0002:
					chr_regs_2k[2] = value;
					break;
				case 0x0003:
					chr_regs_2k[3] = value;
					break;
				case 0x1000:
					prg_regs_8k[0] = (byte)(value & 0x0F);
					break;
				case 0x1001:
					prg_regs_8k[1] = (byte)(value & 0x0F);
					break;
				case 0x1002: //$7002
					mmc3.WritePRG(0xE000, value);
					break;
				case 0x1003: //$7003
					mmc3.WritePRG(0xC000, 7);
					mmc3.WritePRG(0xC001, value);
					mmc3.WritePRG(0xE001, value);
					break;
			}
		}

		public override void ClockPpu()
		{
			mmc3.ClockPPU();
		}

		public override void AddressPpu(int addr)
		{
			mmc3.AddressPPU(addr);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_2k = (addr >> 11);
				bank_2k = chr_regs_2k[bank_2k];
				bank_2k &= chr_bank_mask_2k;
				return Vrom[(bank_2k * 0x800) + (addr & 0x7ff)];
			}
			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			return Rom[(bank_8k * 0x2000) + (addr & 0x1FFF)];
		}
	}
}
