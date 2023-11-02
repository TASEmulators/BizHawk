using System;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	// mostly a c++ -> c# port of melon's firmware verification code
	internal static class NDSFirmware
	{
		public static void MaybeWarnIfBadFw(byte[] fw, Action<string> warningCallback)
		{
			if (fw[0x17C] != 0xFF)
			{
				warningCallback("Hacked firmware detected! Firmware might not work!");
				return;
			}

			var fwMask = fw.Length - 1;
			var badCrc16s = string.Empty;

			if (!VerifyCrc16(fw, 0x2C, (fw[0x2C + 1] << 8) | fw[0x2C], 0x0000, 0x2A))
			{
				badCrc16s += " Wifi ";
			}

			if (!VerifyCrc16(fw, 0x7FA00 & fwMask, 0xFE, 0x0000, 0x7FAFE & fwMask))
			{
				badCrc16s += " AP1 ";
			}

			if (!VerifyCrc16(fw, 0x7FB00 & fwMask, 0xFE, 0x0000, 0x7FBFE & fwMask))
			{
				badCrc16s += " AP2 ";
			}

			if (!VerifyCrc16(fw, 0x7FC00 & fwMask, 0xFE, 0x0000, 0x7FCFE & fwMask))
			{
				badCrc16s += " AP3 ";
			}

			if (!VerifyCrc16(fw, 0x7FE00 & fwMask, 0x70, 0xFFFF, 0x7FE72 & fwMask))
			{
				badCrc16s += " USER0 ";
			}

			if (!VerifyCrc16(fw, 0x7FF00 & fwMask, 0x70, 0xFFFF, 0x7FF72 & fwMask))
			{
				badCrc16s += " USER1 ";
			}

			if (badCrc16s != string.Empty)
			{
				warningCallback("Bad Firmware CRC16(s) detected! Firmware might not work! Bad CRC16(s): " + badCrc16s);
				return;
			}

			CheckDecryptedCodeChecksum(fw, warningCallback);
		}

		private static unsafe ushort Crc16(byte* data, int len, int seed)
		{
			Span<ushort> poly = stackalloc ushort[8] { 0xC0C1, 0xC181, 0xC301, 0xC601, 0xCC01, 0xD801, 0xF001, 0xA001 };

			for (var i = 0; i < len; i++)
			{
				seed ^= data[i];

				for (var j = 0; j < 8; j++)
				{
					if ((seed & 0x1) != 0)
					{
						seed >>= 1;
						seed ^= (poly[j] << (7 - j));
					}
					else
					{
						seed >>= 1;
					}
				}
			}

			return (ushort)(seed & 0xFFFF);
		}

		private static unsafe bool VerifyCrc16(byte[] fw, int startaddr, int len, int seed, int crcaddr)
		{
			var storedCrc16 = (ushort)((fw[crcaddr + 1] << 8) | fw[crcaddr]);
			fixed (byte* start = &fw[startaddr])
			{
				var actualCrc16 = Crc16(start, len, seed);
				return storedCrc16 == actualCrc16;
			}
		}

		[DllImport("libfwunpack", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetDecryptedFirmware(byte[] fw, int fwlen, out IntPtr decryptedFw, out int decryptedlen);

		[DllImport("libfwunpack", CallingConvention = CallingConvention.Cdecl)]
		private static extern void FreeDecryptedFirmware(IntPtr decryptedFw);

		private static readonly string[] goodhashes =
		{
			"D83861C66796665A9777B4E9078E9CC8EB13D880", // MACP nds (one of v1-v4), supposedly the most common
			"F87038265D24677419FE0AF9EED63B4CE1378CC9", // MACg nds (v5)
			"674639373F16539F718C728D6CA0C83A2DB66770", // MACh nds-lite (v6)
		};

		private static void CheckDecryptedCodeChecksum(byte[] fw, Action<string> warningCallback)
		{
			if (!GetDecryptedFirmware(fw, fw.Length, out var decryptedfw, out var decrypedfwlen))
			{
				warningCallback("Firmware could not be decryped for verification! This firmware might be not work!");
				return;
			}

			var decryptedFirmware = new byte[decrypedfwlen];
			Marshal.Copy(decryptedfw, decryptedFirmware, 0, decrypedfwlen);
			FreeDecryptedFirmware(decryptedfw);
			var hash = SHA1Checksum.ComputeDigestHex(decryptedFirmware);
			if (hash != goodhashes[0] && hash != goodhashes[1] && hash != goodhashes[2])
			{
				warningCallback("Potentially bad firmware dump! Decrypted hash " + hash + " does not match known good dumps.");
			}
		}
	}
}
