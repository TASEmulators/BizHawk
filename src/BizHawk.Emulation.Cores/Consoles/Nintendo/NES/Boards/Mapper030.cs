using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper030 : NesBoardBase
	{
		private enum flashmode { fm_default, fm_erase, fm_write, fm_id }

		// config
		private int prg_bank_mask_16k;
		private int vram_bank_mask_8k;

		// state
		private int prg;
		private int chr;
		private int flash_state = 0;
		private flashmode flash_mode = flashmode.fm_default;
		private byte[] flash_rom = null;

		private int get_flash_write_count(int addr)
		{
			if (flash_rom == null)
				return 0;
			int[] value = new int[1];
			int bank = (addr >= 0x4000) ? prg_bank_mask_16k : prg;
			Buffer.BlockCopy(flash_rom, (bank << 2 | (addr >> 12) & 3) << 2, value, 0, 4);
			return value[0];
		}

		private void increment_flash_write_count(int addr, bool direct = false)
		{
			if (flash_rom == null)
				return;
			uint[] value = new uint[1];
			int bank = (addr >= 0x4000) ? prg_bank_mask_16k : prg;
			if (!direct)
			{
				Buffer.BlockCopy(flash_rom, (bank << 2 | (addr >> 12) & 3) << 2, value, 0, 4);
				if(value[0] < 0xFFFFFFFF) value[0]++;
				Buffer.BlockCopy(value, 0, flash_rom, (bank << 2 | (addr >> 12) & 3) << 2, 4);
			}
			else
			{
				Buffer.BlockCopy(flash_rom, addr << 2, value, 0, 4);
				if (value[0] < 0xFFFFFFFF) value[0]++;
				Buffer.BlockCopy(value, 0, flash_rom, addr << 2, 4);
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(flash_state), ref flash_state);
			int tmp = (int)flash_mode;
			ser.Sync(nameof(flash_mode), ref tmp);
			flash_mode = (flashmode)tmp;
			ser.Sync(nameof(flash_rom), ref flash_rom, true);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER030":
					Cart.VramSize = 32;
					break;
				case "MAPPER0030-00":
					AssertVram(8, 16, 32);
					break;
				case "UNIF_UNROM-512-8":
					Cart.VramSize = 8;
					break;
				case "UNIF_UNROM-512-16":
					Cart.VramSize = 16;
					break;
				case "UNIF_UNROM-512-32":
					Cart.VramSize = 32;
					break;
				default:
					return false;
			}

			if (Cart.WramBattery)
			{
				flash_state = 0;
				flash_mode = flashmode.fm_default;
				if (flash_rom == null)
				{
					// extra space is used to hold information about what sectors have been flashed
					flash_rom = new byte[Cart.PrgSize * 1024 + Cart.PrgSize];
				}
			}
			SetMirrorType(CalculateMirrorType(Cart.PadH, Cart.PadV));
			AssertChr(0);
			AssertPrg(128, 256, 512);   //Flash chip sizes that fits sealie unrom-512 are 39SF010, 39SF020, 39SF040.
			Cart.WramSize = 0;
			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
			vram_bank_mask_8k = Cart.VramSize / 8 - 1;
			return true;
		}

		private static readonly int[] addr_state = new int[5] { 0x1555, 0x2AAA, 0x1555, 0x1555, 0x2AAA };
		private static readonly int[] addr_bank = new int[5] { 1, 0, 1, 1, 0 };
		private static readonly byte[] addr_data = new byte[5] { 0xAA, 0x55, 0x80, 0xAA, 0x55 };

		public override void WritePrg(int addr, byte value)
		{
			if ((!Cart.WramBattery) || (addr >= 0x4000))
			{
				byte value2 = value;

				if (!Cart.WramBattery)
					value2 = HandleNormalPRGConflict(addr, value);
				chr = value2 >> 5 & 3 & vram_bank_mask_8k;
				prg = value2 & prg_bank_mask_16k;
				if ((Cart.PadH == 0) && (Cart.PadV == 0))
				{
					int mirror = (value2 & 0x80) >> 7;
					SetMirrorType(CalculateMirrorType(mirror, mirror));
				}
			}
			else
			{
				if (flash_mode == flashmode.fm_default)
				{
					if (addr_state[flash_state] == addr && addr_bank[flash_state] == prg && addr_data[flash_state] == value)
					{
						flash_state++;
						if (flash_state == 5)
							flash_mode = flashmode.fm_erase;
					}
					else if (flash_state == 2 && addr == 0x1555 && prg == 1 && value == 0x90)
					{
						flash_mode = flashmode.fm_id;
					}
					else if (flash_state == 2 && addr == 0x1555 && prg == 1 && value == 0xA0)
					{
						flash_state++;
						flash_mode = flashmode.fm_write;
					}
					else
					{
						flash_state = 0;
						flash_mode = flashmode.fm_default;
					}
				}
				else if (flash_mode == flashmode.fm_erase)
				{
					if (value == 0x10)  //You probably don't want to do this, as this is erase entire flash chip. :)
					{                   //Of course, we gotta emulate the behaviour.
						for (int i = 0; i < (Cart.PrgSize / 4); i++)
							increment_flash_write_count(i, true);
						for (int i = 0; i < flash_rom.Length; i++)
							flash_rom[Cart.PrgSize + i] = 0xFF;
					}
					else if (value == 0x30)
					{
						increment_flash_write_count(addr);
						for (int i = 0; i < 0x1000; i++)
							flash_rom[(prg << 14 | addr & 0x3000) + i + Cart.PrgSize] = 0xFF;
					}
					flash_mode = 0;
					flash_state = 0;
				}
				else if (flash_mode == flashmode.fm_write)
				{
					if (get_flash_write_count(addr) == 0)
					{
						increment_flash_write_count(addr);
						for (int i = 0; i < 0x1000; i++)
							flash_rom[(prg << 14 | addr & 0x3000) + i + Cart.PrgSize] = Rom[(prg << 14 | addr & 0x3000) + i];
					}
					flash_rom[Cart.PrgSize + (prg << 14 | addr & 0x3fff)] &= value;
					flash_state = 0;
					flash_mode = 0;
				}
				if (flash_mode == flashmode.fm_id && value == 0xF0)
				{
					flash_state = 0;
					flash_mode = 0;
				}
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank = addr >= 0x4000 ? prg_bank_mask_16k : prg;
			if (Cart.WramBattery)
			{
				if (flash_mode == flashmode.fm_id)
				{
					switch (addr & 0x1FF)
					{
						case 0:
							return 0xBF;
						case 1:
							switch (Cart.PrgSize)
							{
								case 128:
									return 0xB5;
								case 256:
									return 0xB6;
								case 512:
									return 0xB7;
							}
							return 0xFF;    //Shouldn't ever reach here, as the size was asserted earlier.
						default:
							return 0xFF;    //Other unknown data is returned from addresses 2-511, in software ID mode, mostly 0xFF.
					}
				}
				if (get_flash_write_count(addr) > 0)
					return flash_rom[Cart.PrgSize + (bank << 14 | addr & 0x3fff)];
			}
			return Rom[bank << 14 | addr & 0x3fff];
		}

		public override byte[] SaveRam => flash_rom;

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vram[addr | chr << 13];
			return base.ReadPpu(addr);
		}
		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
				Vram[addr | chr << 13] = value;
			else
				base.WritePpu(addr, value);
		}
	}
}
