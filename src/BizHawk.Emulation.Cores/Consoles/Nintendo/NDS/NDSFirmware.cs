using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	// mostly a c++ -> c# port of melon's firmware verification code
	// TODO: a lot of this has been removed as it's not really needed anymore (our c++ code forces correctness everywhere)
	internal static class NDSFirmware
	{
		public static void MaybeWarnIfBadFw(byte[] fw, Action<string> warningCallback)
		{
			if (fw[0x17C] != 0xFF)
			{
				warningCallback("Hacked firmware detected! Firmware might not work!");
				return;
			}

			if (fw.Length != 0x20000) // no code in DSi firmware
			{
				// TODO: is this hashing strat actually a good idea?
				CheckDecryptedCodeChecksum(fw, warningCallback);
			}
		}

		[DllImport("libfwunpack", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetDecryptedFirmware(byte[] fw, int fwlen, out IntPtr decryptedFw, out int decryptedlen);

		[DllImport("libfwunpack", CallingConvention = CallingConvention.Cdecl)]
		private static extern void FreeDecryptedFirmware(IntPtr decryptedFw);

		private static readonly string[] _goodHashes =
		{
			"D83861C66796665A9777B4E9078E9CC8EB13D880", // MACP DS (v1, 2004-10-05)
			"71324E1C2DA7F3D2CFD45B08171BA0603CCA9C8B", // MACP DS (v2, 2004-11-26)
			"1B4EF392331D41B170DA0C929B7834D3006DCD8B", // MACP DS (v3, 2005-02-28)
			"3A3F3F06E0F5D5FC7BC140757160BF9682B73D0A", // MACg DS (v4, 2005-06-06)
			"9DD2A76A49DECD64408EE640443E0A14DDCA5F09", // MAC\xC2 iQue (v3, 2005-06-09)
			"D83AEBD1A10B41161C6FC48C5E44A619CD4A5C7F", // MACh DS Lite (Beta v5, 2005-11-30)
			"F87038265D24677419FE0AF9EED63B4CE1378CC9", // MACg DS (v5, 2005-12-07)
			"39B6084CBC9BCE1E42E442B633B83EDDEE3FBBCE", // MACh DS Lite (Kiosk, 2006-01-26)
			"C197A559489158AFC35F472E4FA4E22A88558F85", // MACh DS Lite (v5, 2006-02-05)
			"674639373F16539F718C728D6CA0C83A2DB66770", // MACh DS Lite (v6, 2006-03-08)
			"4C1B2C60D4DD0C3B4CAAC22B8CB765B4FC05DB3D", // MACi iQue Lite (v5, 2006-04-26)
			"BFBC33D996AA73A050F1951529327D5844461A00", // MACi DS Lite (Korean v5, 2006-11-09)
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
			if (!_goodHashes.Contains(hash))
			{
				warningCallback("Potentially bad firmware dump! Decrypted hash " + hash + " does not match known good dumps.");
			}
		}
	}
}
