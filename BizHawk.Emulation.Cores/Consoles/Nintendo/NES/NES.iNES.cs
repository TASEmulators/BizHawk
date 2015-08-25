using System;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	partial class NES
	{
		private static int iNES2Wram(int i)
		{
			if (i == 0) return 0;
			if (i == 15) throw new InvalidDataException();
			return 1 << (i + 6);
		}

		public static bool DetectFromINES(byte[] data, out CartInfo Cart, out CartInfo CartV2)
		{
			byte[] ID = new byte[4];
			Buffer.BlockCopy(data, 0, ID, 0, 4);
			if (!ID.SequenceEqual(Encoding.ASCII.GetBytes("NES\x1A")))
			{
				Cart = null;
				CartV2 = null;
				return false;
			}

			if ((data[7] & 0x0c) == 0x08)
			{
				// process as iNES v2
				CartV2 = new CartInfo();

				CartV2.prg_size = data[4] | data[9] << 8 & 0xf00;
				CartV2.chr_size = data[5] | data[9] << 4 & 0xf00;
				CartV2.prg_size *= 16;
				CartV2.chr_size *= 8;

				CartV2.wram_battery = (data[6] & 2) != 0; // should this be respected in v2 mode??

				int wrambat = iNES2Wram(data[10] >> 4);
				int wramnon = iNES2Wram(data[10] & 15);
				CartV2.wram_battery |= wrambat > 0;
				// fixme - doesn't handle sizes not divisible by 1024
				CartV2.wram_size = (short)((wrambat + wramnon) / 1024);

				int mapper = data[6] >> 4 | data[7] & 0xf0 | data[8] << 8 & 0xf00;
				int submapper = data[8] >> 4;
				CartV2.board_type = string.Format("MAPPER{0:d4}-{1:d2}", mapper, submapper);

				int vrambat = iNES2Wram(data[11] >> 4);
				int vramnon = iNES2Wram(data[11] & 15);
				// hopefully a game with battery backed vram understands what to do internally
				CartV2.wram_battery |= vrambat > 0;
				CartV2.vram_size = (vrambat + vramnon) / 1024;

				CartV2.inesmirroring = data[6] & 1 | data[6] >> 2 & 2;
				switch (CartV2.inesmirroring)
				{
					case 0: CartV2.pad_v = 1; break;
					case 1: CartV2.pad_h = 1; break;
				}
				switch (data[12] & 1)
				{
					case 0:
						CartV2.system = "NES-NTSC";
						break;
					case 1:
						CartV2.system = "NES-PAL";
						break;
				}

				if ((data[6] & 4) != 0)
					CartV2.trainer_size = 512;
			}
			else
			{
				CartV2 = null;
			}

			// process as iNES v1
			// the DiskDude cleaning is no longer; get better roms
			Cart = new CartInfo();

			Cart.prg_size = data[4];
			Cart.chr_size = data[5];
			if (Cart.prg_size == 0)
				Cart.prg_size = 256;
			Cart.prg_size *= 16;
			Cart.chr_size *= 8;


			Cart.wram_battery = (data[6] & 2) != 0;
			Cart.wram_size = 8; // should be data[8], but that never worked

			{
				int mapper = data[6] >> 4 | data[7] & 0xf0;
				Cart.board_type = string.Format("MAPPER{0:d3}", mapper);
			}

			Cart.vram_size = Cart.chr_size > 0 ? 0 : 8;

			Cart.inesmirroring = data[6] & 1 | data[6] >> 2 & 2;
			switch (Cart.inesmirroring)
			{
				case 0: Cart.pad_v = 1; break;
				case 1: Cart.pad_h = 1; break;
			}

			if (data[6].Bit(2))
				Cart.trainer_size = 512;

			return true;
		}
	}
}