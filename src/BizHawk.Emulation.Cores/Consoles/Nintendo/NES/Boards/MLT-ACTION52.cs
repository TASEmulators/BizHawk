using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_228
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class MLT_ACTION52 : NesBoardBase
	{
		[MapperProp]
		public bool prg_mode = false;
		[MapperProp]
		public int prg_reg = 0;
		public int chr_reg;
		public int chip_offset;
		public bool cheetahmen = false;
		private byte[] eRAM = new byte[1 << 3];
		private int chr_bank_mask_8k, prg_bank_mask_16k, prg_bank_mask_32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER228":
				case "MLT-ACTION52":
					break;
				default:
					return false;
			}

			AssertPrg(256, 1536);

			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;
			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;

			if (Cart.PrgSize == 256)
			{
				cheetahmen = true;
			}
			else
			{
				prg_bank_mask_16k = 0x1F;
				prg_bank_mask_32k = 0xF;
			}

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);
			ser.Sync(nameof(chr_reg), ref chr_reg);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync("chip", ref chip_offset);
			ser.Sync(nameof(eRAM), ref eRAM, false);
			base.SyncState(ser);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1800)
			{
				eRAM[(addr & 0x07)] = (byte)(value & 0x0F);
			}
		}

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x1800)
			{
				return eRAM[(addr & 0x07)];
			}
			else
			{
				return base.ReadExp(addr);
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			//$8000-FFFF:    [.... ..CC]   Low 2 bits of CHR
			//A~[..MH HPPP PPO. CCCC]

			addr += 0x8000;

			if (addr.Bit(13))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			prg_mode = addr.Bit(5);
			prg_reg = (addr >> 6) & 0x1F;
			chr_reg = ((addr & 0x0F) << 2) | (value & 0x03);
			if (!cheetahmen)
			{
				int chip = ((addr >> 11) & 0x03);
				switch (chip)
				{
					case 0:
						chip_offset = 0x0;
						break;
					case 1:
						chip_offset = 0x80000;
						break;
					case 2:
						break; //TODO: this chip doesn't exist and should access open bus
					case 3:
						chip_offset = 0x100000;
						break;
				}
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

		public override byte ReadPrg(int addr)
		{
			if (!prg_mode)
			{
				int bank = (prg_reg >> 1) & prg_bank_mask_32k;
				return Rom[(bank * 0x8000) + addr + chip_offset];
			}
			else
			{
				return Rom[((prg_reg & prg_bank_mask_16k) * 0x4000) + (addr & 0x3FFF) + chip_offset];
			}
		}
	}
}
