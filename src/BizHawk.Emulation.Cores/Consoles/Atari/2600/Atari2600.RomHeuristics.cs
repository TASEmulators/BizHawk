using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		// Heuristics logic based on Stella's logic
		private static string DetectMapper(byte[] rom)
		{
			if (rom.Length % 8448 == 0 || rom.Length == 6114) // Only AR could be these odd numbers
			{
				return "AR";
			}
			
			if (rom.Length < 2048) // Less than 2k, then no bank switching needed
			{
				return "2K";
			}
			
			if (rom.Length is 2048
				|| (rom.Length is 4096
					&& rom.AsSpan(start: 0, length: 2048).SequenceEqual(rom.AsSpan(start: 2048, length: 2048))))
			{
				// If 2k or the same 2k twice...Why would a rom be that way? Overdump?
				return IsProablyCV(rom) ? "CV" : "2K";
			}
			
			if (rom.Length == 4096)
			{
				return IsProablyCV(rom) ? "CV" : "4K";
			}
			
			if (rom.Length == 8192) // Several 8K Options
			{
				if (IsProbablySC(rom))
				{
					return "F8SC";
				}

				if (rom.AsSpan(start: 0, length: 4096).SequenceEqual(rom.AsSpan(start: 4096, length: 4096)))
				{
					return "4K"; // Again if it is simply the same 4k twice. Got this scenario from Stella logic.  Will assume a good reason for it
				}

				if (IsProbablyE0(rom))
				{
					return "E0";
				}

				if (IsProbably3E(rom))
				{
					return "3E";
				}

				if (IsProbably3F(rom))
				{
					return "3F";
				}

				if (IsProbablyUA(rom))
				{
					return "UA";
				}

				if (IsProbablyFE(rom))
				{
					return "FE";
				}

				if (IsProbably0840(rom))
				{
					return "0840";
				}

				return "F8";
			}

			if (rom.Length >= 10240 && rom.Length <= 10496)  // ~10K - Pitfall2
			{
				return "DPC";
			}

			if (rom.Length == 12 * 1024) // 12K
			{
				return "FA";
			}

			if (rom.Length == 16 * 1024) // 16K
			{
				if (IsProbablySC(rom))
				{
					return "F6SC";
				}

				if (IsProbablyE7(rom))
				{
					return "E7";
				}

				if (IsProbably3E(rom))
				{
					return "3E";
				}

				if (IsProbably3F(rom))
				{
					return "3F";
				}

				return "F6";
			}

			if (rom.Length == 24 * 1024 || rom.Length == 28 * 1024)  // 24K & 28K
			{
				return "FA2";
			}

			if (rom.Length == 29 * 1024)  // 29K
			{
				return "DPC+";
			}

			if (rom.Length == 32 * 1024) // 32K
			{
				if (IsProbablySC(rom))
				{
					return "F4SC";
				}

				if (IsProbably3E(rom))
				{
					return "3E";
				}

				if (IsProbably3F(rom))
				{
					return "3F";
				}

				if (IsProbablyDpcPlus(rom))
				{
					return "DPC+";
				}

				return "F4";
			}

			if (rom.Length == 64 * 1024) // 64K
			{
				if (IsProbably3E(rom))
				{
					return "3E";
				}

				if (IsProbably3F(rom))
				{
					return "3F";
				}

				if (IsProbably4A50(rom))
				{
					return "4A50";
				}

				if (IsProbablyEF(rom))
				{
					return IsProbablySC(rom) ? "EFSC" : "EF";
				}

				if (IsProbablyX07(rom))
				{
					return "X07";
				}

				return "F0";
			}

			if (rom.Length == 128 * 1024) // 128K
			{
				if (IsProbably3E(rom))
				{
					return "3E";
				}

				if (IsProbably3F(rom))
				{
					return "3F";
				}

				if (IsProbably4A50(rom))
				{
					return "4A50";
				}

				if (IsProbablySB(rom))
				{
					return "SB";
				}

				return "MC";
			}

			if (rom.Length == 256 * 1024) // 256K
			{
				if (IsProbably3E(rom))
				{
					return "3E";
				}

				if (IsProbably3F(rom))
				{
					return "3F";
				}

				return "SB";
			}

			// What else could it be? Try 3E or 3F
			if (IsProbably3E(rom))
			{
				return "3E";
			}

			if (IsProbably3F(rom))
			{
				return "3F";
			}

			return "UNKNOWN";
		}

		private static bool IsProbablySC(IList<byte> rom)
		{
			// We assume a Superchip cart contains the same bytes for its entire
			// RAM area; obviously this test will fail if it doesn't
			// The RAM area will be the first 256 bytes of each 4K bank
			var numBanks = rom.Count / 4096;
			for (var i = 0; i < numBanks; i++)
			{
				var first = rom[i * 4096];
				for (var j = 0; j < 256; j++)
				{
					if (rom[(i * 4096) + j] != first)
					{
						return false;
					}
				}
			}

			return false;
		}

		private static bool IsProbably3E(byte[] rom)
		{
			// 3E cart bankswitching is triggered by storing the bank number
			// in address 3E using 'STA $3E', commonly followed by an
			// immediate mode LDA
			return rom.FindBytes(new byte[] { 0x85, 0x3E, 0xA9, 0x00 }); // STA $3E; LDA #$00
		}

		private static bool IsProbably3F(byte[] rom)
		{
			// 3F cart bankswitching is triggered by storing the bank number
			// in address 3F using 'STA $3F'
			// We expect it will be present at least 2 times, since there are at least two banks
			return ContainsAll(rom, new List<byte[]>
			{
				new byte[] { 0x85, 0x3F },
				new byte[] { 0x85, 0x3F }
			});
		}

		private static bool IsProbably4A50(IList<byte> rom)
		{
			// 4A50 carts store address $4A50 at the NMI vector, which
			// in this scheme is always in the last page of ROM at
			// $1FFA - $1FFB (at least this is true in rev 1 of the format)
			if (rom[rom.Count - 6] == 0x50 && rom[rom.Count - 5] == 0x4A)
			{
				return true;
			}

			// Program starts at $1Fxx with NOP $6Exx or NOP $6Fxx?
			if ((rom[0xFFFD] & 0x1F) is 0x1F
				&& rom[rom[0xFFFD] * 256 + rom[0xFFFC]] is 0x0C
				&& (rom[rom[0xFFFD] * 256 + rom[0xFFFC] + 2] & 0xFE) is 0x6E)
			{
				return true;
			}

			return false;
		}

		private static bool IsProbablyUA(byte[] rom)
		{
			// UA cart bankswitching switches to bank 1 by accessing address 0x240
			// using 'STA $240' or 'LDA $240'
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0x8D, 0x40, 0x02 },  // STA $240
				new byte[] { 0xAD, 0x40, 0x02 },  // LDA $240
				new byte[] { 0xBD, 0x1F, 0x02 }   // LDA $21F,X
			});
		}

		private static bool IsProbablyE0(byte[] rom)
		{
			// E0 cart bankswitching is triggered by accessing addresses
			// $FE0 to $FF9 using absolute non-indexed addressing
			// To eliminate false positives (and speed up processing), we
			// search for only certain known signatures
			// Thanks to "stella@casperkitty.com" for this advice
			// These signatures are attributed to the MESS project
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0x8D, 0xE0, 0x1F },  // STA $1FE0
				new byte[] { 0x8D, 0xE0, 0x5F },  // STA $5FE0
				new byte[] { 0x8D, 0xE9, 0xFF },  // STA $FFE9
				new byte[] { 0x0C, 0xE0, 0x1F },  // NOP $1FE0
				new byte[] { 0xAD, 0xE0, 0x1F },  // LDA $1FE0
				new byte[] { 0xAD, 0xE9, 0xFF },  // LDA $FFE9
				new byte[] { 0xAD, 0xED, 0xFF },  // LDA $FFED
				new byte[] { 0xAD, 0xF3, 0xBF }   // LDA $BFF3
			});
		}

		private static bool IsProablyCV(byte[] rom)
		{
			// According to Stella: CV RAM access occurs at addresses $f3ff and $f400
			// These signatures are attributed to the MESS project
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0x9D, 0xFF, 0xF3 },
				new byte[] { 0x99, 0x00, 0xF4 }
			});
		}

		private static bool IsProbablyFE(byte[] rom)
		{
			// FE bankswitching is very weird, but always seems to include a
			// 'JSR $xxxx'
			// These signatures are attributed to the MESS project
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0x20, 0x00, 0xD0, 0xC6, 0xC5 },  // JSR $D000; DEC $C5
				new byte[] { 0x20, 0xC3, 0xF8, 0xA5, 0x82 },  // JSR $F8C3; LDA $82
				new byte[] { 0xD0, 0xFB, 0x20, 0x73, 0xFE },  // BNE $FB; JSR $FE73
				new byte[] { 0x20, 0x00, 0xF0, 0x84, 0xD6 }   // JSR $F000; STY $D6
			});
		}

		private static bool IsProbably0840(byte[] rom)
		{
			// 0840 cart bankswitching is triggered by accessing addresses 0x0800
			// or 0x0840
			if (ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0xAD, 0x00, 0x08 },  // LDA $0800
				new byte[] { 0xAD, 0x40, 0x08 }   // LDA $0840
			}))
			{
				return true;
			}

			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0x0C, 0x00, 0x08, 0x4C },  // NOP $0800; JMP ...
				new byte[] { 0x0C, 0xFF, 0x0F, 0x4C }   // NOP $0FFF; JMP ...
			});
		}

		private static bool IsProbablyE7(byte[] rom)
		{
			// E7 cart bankswitching is triggered by accessing addresses
			// $FE0 to $FE6 using absolute non-indexed addressing
			// To eliminate false positives (and speed up processing), we
			// search for only certain known signatures
			// Thanks to "stella@casperkitty.com" for this advice
			// These signatures are attributed to the MESS project
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0xAD, 0xE2, 0xFF },  // LDA $FFE2
				new byte[] { 0xAD, 0xE5, 0xFF },  // LDA $FFE5
				new byte[] { 0xAD, 0xE5, 0x1F },  // LDA $1FE5
				new byte[] { 0xAD, 0xE7, 0x1F },  // LDA $1FE7
				new byte[] { 0x0C, 0xE7, 0x1F },  // NOP $1FE7
				new byte[] { 0x8D, 0xE7, 0xFF },  // STA $FFE7
				new byte[] { 0x8D, 0xE7, 0x1F }   // STA $1FE7
			});
		}

		private static bool IsProbablyDpcPlus(byte[] rom)
		{
			// E0 cart bankswitching is triggered by accessing addresses
			// $FE0 to $FF9 using absolute non-indexed addressing
			// To eliminate false positives (and speed up processing), we
			// search for only certain known signatures
			// Thanks to "stella@casperkitty.com" for this advice
			// These signatures are attributed to the MESS project
			return ContainsAny(rom, new List<byte[]>
			{
				// why is this checking the same value twice? ...
				"DPC+"u8.ToArray(),
				"DPC+"u8.ToArray(),
			});
		}

		private static bool IsProbablyEF(byte[] rom)
		{
			// EF cart bankswitching switches banks by accessing addresses 0xFE0
			// to 0xFEF, usually with either a NOP or LDA
			// It's likely that the code will switch to bank 0, so that's what is tested
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0x0C, 0xE0, 0xFF },  // NOP $FFE0
				new byte[] { 0xAD, 0xE0, 0xFF },  // LDA $FFE0
				new byte[] { 0x0C, 0xE0, 0x1F },  // NOP $1FE0
				new byte[] { 0xAD, 0xE0, 0x1F }   // LDA $1FE0
			});
		}

		private static bool IsProbablyX07(byte[] rom)
		{
			// X07 bankswitching switches to bank 0, 1, 2, etc by accessing address 0x08xd
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0xAD, 0x0D, 0x08 },  // LDA $080D
				new byte[] { 0xAD, 0x1D, 0x08 },  // LDA $081D
				new byte[] { 0xAD, 0x2D, 0x08 }   // LDA $082D
			});
		}

		private static bool IsProbablySB(byte[] rom)
		{
			// SB cart bankswitching switches banks by accessing address 0x0800
			return ContainsAny(rom, new List<byte[]>
			{
				new byte[] { 0xBD, 0x00, 0x08 },  // LDA $0800,x
				new byte[] { 0xAD, 0x00, 0x08 }   // LDA $0800
			});
		}

		private static bool ContainsAny(byte[] rom, IEnumerable<byte[]> signatures)
		{
			return signatures.Any(rom.FindBytes);
		}

		private static bool ContainsAll(byte[] rom, IEnumerable<byte[]> signatures)
		{
			return signatures.All(rom.FindBytes);
		}
	}
}
