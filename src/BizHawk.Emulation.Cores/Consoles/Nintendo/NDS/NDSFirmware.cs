using System;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	// mostly a c++ -> c# port of melon's firmware verification code
	public class NDSFirmware
	{
		public static bool MaybeWarnIfBadFw(byte[] fw, CoreComm comm)
		{
			if (fw.Length != 0x20000 && fw.Length != 0x40000 && fw.Length != 0x80000)
			{
				comm.ShowMessage("Bad firmware length detected! Firmware might not work!");
				return false;
			}
			if (fw[0x17C] != 0xFF)
			{
				comm.ShowMessage("Hacked firmware detected! Firmware might not work!");
				return false;
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
			if (badCrc16s != "")
			{
				comm.ShowMessage("Bad Firmware CRC16(s) detected! Firmware might not work! Bad CRC16(s): " + badCrc16s);
				return false;
			}

			return CheckDecryptedCodeChecksum(fw, comm);
		}

		public static void SanitizeFw(byte[] fw)
		{
			var fwMask = fw.Length - 1;
			var apstart = new int[3] { 0x07FA00 & fwMask, 0x07FB00 & fwMask, 0x07FC00 & fwMask };

			for (var i = 0; i < 3; i++)
			{
				for (var j = 0; j < 0x100; j++)
				{
					fw[apstart[i] + j] = 0;
				}
			}

			// gbatek marks these as unknown, they seem to depend on the mac address???
			// bytes 4 (upper nibble only) and 5 also seem to be just random?
			// various combinations noted (noting last 2 bytes are crc16)
			// F8 98 C1 E6 CC DD A9 E1 85 D4 9B
			// F8 98 C1 E6 CC 1D 66 E1 85 D8 A4
			// F8 98 C1 E6 CC 9D 6B E1 85 60 A7
			// F8 98 C1 E6 CC 5D 92 E1 85 8C 96
			// different mac address
			// 18 90 15 E9 7C 1D F1 E1 85 74 02
			var macdependentbytes = new byte[11] { 0xF8, 0x98, 0xC1, 0xE6, 0xCC, 0x9D, 0xBE, 0xE1, 0x85, 0x71, 0x5F };

			var apoffset = 0xF5;

			for (var i = 0; i < 2; i++)
			{
				for (var j = 0; j < 11; j++)
				{
					fw[apstart[i] + apoffset + j] = macdependentbytes[j];
				}
			}

			var ffoffset = 0xE7;

			for (var i = 0; i < 3; i++)
			{
				fw[apstart[i] + ffoffset] = 0xFF;
			}

			// slot 3 doesn't have those mac dependent bytes???
			fw[apstart[2] + 0xFE] = 0x0A;
			fw[apstart[2] + 0xFF] = 0xF0;

			var usersettings = new int[2] { 0x7FE00 & fwMask, 0x7FF00 & fwMask };

			for (var i = 0; i < 2; i++)
			{
				unsafe
				{
					fixed (byte* us = &fw[usersettings[i]])
					{
						// alarm settings
						us[0x52] = 0;
						us[0x53] = 0;
						us[0x56] = 0;
						// year of first boot
						us[0x66] = 0;
						// rtc offset
						us[0x68] = 0;
						us[0x69] = 0;
						us[0x6A] = 0;
						us[0x6B] = 0;
						// update counter
						us[0x70] = 0;
						us[0x71] = 0;
						// fix crc16 (probably redundant)
						ushort crc16 = Crc16(us, 0x70, 0xFFFF);
						us[0x72] = (byte)(crc16 & 0xFF);
						us[0x73] = (byte)(crc16 >> 8);
					}
				}
			}
		}

		private static unsafe ushort Crc16(byte* data, int len, int seed)
		{
			var poly = new ushort[8] { 0xC0C1, 0xC181, 0xC301, 0xC601, 0xCC01, 0xD801, 0xF001, 0xA001 };

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

		private static bool CheckDecryptedCodeChecksum(byte[] fw, CoreComm comm)
		{
			if (!GetDecryptedFirmware(fw, fw.Length, out var decryptedfw, out var decrypedfwlen))
			{
				comm.ShowMessage("Firmware could not be decryped for verification! This firmware might be not work!");
				return false;
			}

			var DecryptedFirmware = new byte[decrypedfwlen];
			Marshal.Copy(decryptedfw, DecryptedFirmware, 0, decrypedfwlen);
			FreeDecryptedFirmware(decryptedfw);
			var hash = SHA1Checksum.ComputeDigestHex(DecryptedFirmware);
			if (hash != goodhashes[0] && hash != goodhashes[1] && hash != goodhashes[2])
			{
				comm.ShowMessage("Potentially bad firmware dump! Decrypted hash " + hash + " does not match known good dumps.");
				return false;
			}

			return true;
		}
	}
}
