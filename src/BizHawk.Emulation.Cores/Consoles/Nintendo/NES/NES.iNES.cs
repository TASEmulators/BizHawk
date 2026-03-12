using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed partial class NES
	{
		private static int iNES2Wram(int i)
		{
			if (i == 0) return 0;
			if (i == 15) throw new InvalidOperationException();
			return 1 << (i + 6);
		}

		public static bool DetectFromINES(ReadOnlySpan<byte> data, out CartInfo Cart, out CartInfo CartV2)
		{
			if (!data[..4].SequenceEqual("NES\x1A"u8))
			{
				Cart = null;
				CartV2 = null;
				return false;
			}

			if ((data[7] & 0x0c) == 0x08)
			{
				// process as iNES v2

				static int ReadEMFormAreaSize(int msn, int lsb, int nonEMFormBlockSizeKiB)
				{
					if (msn is not 0xF) return ((msn << 8) | lsb) * nonEMFormBlockSizeKiB;
					// else exponent-multiplier form: 2^E * (M * 2 + 1) bytes, where M is the least significant 2 bits, and E is the next 6 bits

					// exponent needs to be at least 10 in order to have KiB granularity
					// we also can't handle >= 2GiB files, so any exponent too large can't be handled
					var exponent = (lsb >> 2) & 0x3F;
					if (exponent is < 10 or > 30) throw new InvalidOperationException();

					var multiplier = (lsb & 0x03) * 2 + 1;
					var prgSize = (1UL << exponent) * (ulong)multiplier;
					if (prgSize > int.MaxValue) throw new InvalidOperationException();

					// convert to KiB
					return (int)(prgSize >> 10);
				}

				CartV2 = new CartInfo
				{
					PrgSize = ReadEMFormAreaSize(msn: data[9] & 0x0F, lsb: data[4], nonEMFormBlockSizeKiB: 16),
					ChrSize = ReadEMFormAreaSize(msn: data[9] >> 4, lsb: data[5], nonEMFormBlockSizeKiB: 8),
				};

				CartV2.WramBattery = (data[6] & 2) != 0; // should this be respected in v2 mode??

				int wrambat = iNES2Wram(data[10] >> 4);
				int wramnon = iNES2Wram(data[10] & 15);
				CartV2.WramBattery |= wrambat > 0;
				// fixme - doesn't handle sizes not divisible by 1024
				CartV2.WramSize = (short)((wrambat + wramnon) / 1024);

				int mapper = data[6] >> 4 | data[7] & 0xf0 | data[8] << 8 & 0xf00;
				int submapper = data[8] >> 4;
				CartV2.BoardType = $"MAPPER{mapper:d4}-{submapper:d2}";

				int vrambat = iNES2Wram(data[11] >> 4);
				int vramnon = iNES2Wram(data[11] & 15);
				// hopefully a game with battery backed vram understands what to do internally
				CartV2.WramBattery |= vrambat > 0;
				CartV2.VramSize = (vrambat + vramnon) / 1024;

				CartV2.InesMirroring = data[6] & 1 | data[6] >> 2 & 2;
				switch (CartV2.InesMirroring)
				{
					case 0: CartV2.PadV = 1; break;
					case 1: CartV2.PadH = 1; break;
				}

				CartV2.System = (data[12] & 3) switch
				{
					// 2 indicates dual NTSC/PAL compatibility, might as well use NTSC
					0 or 2 => "NES-NTSC",
					1 => "NES-PAL",
					3 => "Dendy",
					_ => CartV2.System
				};

				if ((data[6] & 4) != 0)
					CartV2.TrainerSize = 512;
			}
			else
			{
				CartV2 = null;
			}

			// process as iNES v1
			// the DiskDude cleaning is no longer; get better roms
			Cart = new CartInfo
			{
				PrgSize = data[4],
				ChrSize = data[5]
			};

			if (Cart.PrgSize == 0)
				Cart.PrgSize = 256;
			Cart.PrgSize *= 16;
			Cart.ChrSize *= 8;

			Cart.WramBattery = (data[6] & 2) != 0;
			Cart.WramSize = 8; // should be data[8], but that never worked

			{
				int mapper = data[6] >> 4 | data[7] & 0xf0;
				Cart.BoardType = $"MAPPER{mapper:d3}";
			}

			Cart.VramSize = Cart.ChrSize > 0 ? 0 : 8;

			Cart.InesMirroring = data[6] & 1 | data[6] >> 2 & 2;
			switch (Cart.InesMirroring)
			{
				case 0: Cart.PadV = 1; break;
				case 1: Cart.PadH = 1; break;
			}

			if (data[6].Bit(2))
				Cart.TrainerSize = 512;

			// iNES v1 does not have a reliable way to indicate the game's region
			// Officially, data[9] bit 0 indicates region, but practically no ROM uses it
			// Unofficially, data[10] bit 1-0 indicates region, but practically no ROM uses it
			// However, iNES v2 will reliably indicate the game's region, so we can use that if present
			Cart.System = CartV2?.System;

			return true;
		}
	}
}
