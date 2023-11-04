using System;
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
