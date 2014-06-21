using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper030 : NES.NESBoardBase
	{

		enum flashmode { fm_default, fm_erase, fm_write, fm_id }

		// config
		int prg_bank_mask_16k;
		int vram_bank_mask_8k;

		// state
		int prg;
		int chr;
		int flash_state = 0;
		flashmode flash_mode = flashmode.fm_default;
		byte[] flash_rom = null;

		int get_flash_write_count(int addr)
		{
			if (flash_rom == null)
				return 0;
			int[] value = new int[1];
			int bank = (addr >= 0x4000) ? prg_bank_mask_16k : prg;
			Buffer.BlockCopy(flash_rom, (bank << 2 | (addr >> 12) & 3) << 2, value, 0, 4);
			return value[0];
		}

		void increment_flash_write_count(int addr, bool direct = false)
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
			ser.Sync("prg", ref prg);
			ser.Sync("chr", ref chr);
			ser.Sync("flash_state", ref flash_state);
			int tmp = (int)flash_mode;
			ser.Sync("flash_mode", ref tmp);
			flash_mode = (flashmode)tmp;
			ser.Sync("flash_rom", ref flash_rom, true);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER030":
					Cart.vram_size = 32;
					break;
				case "MAPPER0030-00":
					AssertVram(8, 16, 32);
					break;
				case "UNIF_UNROM-512-8":
					Cart.vram_size = 8;
					break;
				case "UNIF_UNROM-512-16":
					Cart.vram_size = 16;
					break;
				case "UNIF_UNROM-512-32":
					Cart.vram_size = 32;
					break;
				default:
					return false;
			}

			if (Cart.wram_battery)
			{
				flash_state = 0;
				flash_mode = flashmode.fm_default;
				if (flash_rom == null)
				{
					// extra space is used to hold information about what sectors have been flashed
					flash_rom = new byte[Cart.prg_size * 1024 + Cart.prg_size];
				}
			}
			SetMirrorType(CalculateMirrorType(Cart.pad_h, Cart.pad_v));
			AssertChr(0);
			AssertPrg(128, 256, 512);   //Flash chip sizes that fits sealie unrom-512 are 39SF010, 39SF020, 39SF040.
			Cart.wram_size = 0;
			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			vram_bank_mask_8k = Cart.vram_size / 8 - 1;
			return true;
		}

		static readonly int[] addr_state = new int[5] { 0x1555, 0x2AAA, 0x1555, 0x1555, 0x2AAA };
		static readonly int[] addr_bank = new int[5] { 1, 0, 1, 1, 0 };
		static readonly byte[] addr_data = new byte[5] { 0xAA, 0x55, 0x80, 0xAA, 0x55 };

		public override void WritePRG(int addr, byte value)
		{
			if ((!Cart.wram_battery) || (addr >= 0x4000))
			{
				byte value2 = value;

				if (!Cart.wram_battery)
					value2 = HandleNormalPRGConflict(addr, value);
				chr = value2 >> 5 & 3 & vram_bank_mask_8k;
				prg = value2 & prg_bank_mask_16k;
				if ((Cart.pad_h == 0) && (Cart.pad_v == 0))
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
						for (int i = 0; i < (Cart.prg_size / 4); i++)
							increment_flash_write_count(i, true);
						for (int i = 0; i < flash_rom.Count(); i++)
							flash_rom[Cart.prg_size + i] = 0xFF;
					}
					else if (value == 0x30)
					{
						increment_flash_write_count(addr);
						for (int i = 0; i < 0x1000; i++)
							flash_rom[(prg << 14 | addr & 0x3000) + i + Cart.prg_size] = 0xFF;
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
							flash_rom[(prg << 14 | addr & 0x3000) + i + Cart.prg_size] = ROM[(prg << 14 | addr & 0x3000) + i];
					}
					flash_rom[Cart.prg_size + (prg << 14 | addr & 0x3fff)] &= value;
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

		public override byte ReadPRG(int addr)
		{
			int bank = addr >= 0x4000 ? prg_bank_mask_16k : prg;
			if (Cart.wram_battery)
			{
				if (flash_mode == flashmode.fm_id)
				{
                    switch (addr & 0x1FF)
                    {
                        case 0:
                            return 0xBF;
                        case 1:
                            switch (Cart.prg_size)
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
					return flash_rom[Cart.prg_size + (bank << 14 | addr & 0x3fff)];
			}
			return ROM[bank << 14 | addr & 0x3fff];
		}

		public override byte[] SaveRam { get { return flash_rom; } }

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VRAM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
				VRAM[addr | chr << 13] = value;
			else
				base.WritePPU(addr, value);
		}
	}
}
