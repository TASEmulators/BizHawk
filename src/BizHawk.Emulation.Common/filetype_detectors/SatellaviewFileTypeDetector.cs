using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.Common
{
	public static class SatellaviewFileTypeDetector
	{
		/// <remarks>
		/// https://wiki.superfamicom.org/bs-x-satellaview-header
		/// https://satellaview.fandom.com/wiki/Satellaview_ROM_header
		/// </remarks>
		public readonly ref struct SatellaviewHeader
		{
			private const byte LIMITED_0_PLAYS_LEFT = 0b10000000;

			private const byte LIMITED_1_PLAYS_LEFT = 0b10000100;

			private const byte LIMITED_2_PLAYS_LEFT = 0b10001100;

			private const byte LIMITED_3_PLAYS_LEFT = 0b10011100;

			private const byte LIMITED_4_PLAYS_LEFT = 0b10111100;

			private const byte LIMITED_5_PLAYS_LEFT = 0b11111100;

			private const int OFFSET_BROADCAST_DATE = 0x26; // 2 octets

			private const int OFFSET_CHECKSUM = 0x2C; // 2 octets

			private const int OFFSET_CHECKSUM_COMPLEMENT = 0x2E; // 2 octets

			private const int OFFSET_CONTENT_TYPE = 0x29; // 1 octet

			private const int OFFSET_MAGIC_DRM_BYTE = 0x2A; // 1 octet

			private const int OFFSET_REVISION = 0x2B; // 1 octet

			private const int OFFSET_SELFDESTRUCT = 0x25; // 1 octet; technically 2 octets LE at 0x24, but the least-significant 10 bits are always 0

			private const int OFFSET_SPEED = 0x28; // 1 octet

			private const int OFFSET_TITLE = 0x10; // 16 octets

			internal const byte UNLIMITED_PLAYS_LEFT = 0b00000000;

			private readonly ReadOnlySpan<byte> _header;

			public byte ContentTypeField
				=> _header[OFFSET_CONTENT_TYPE];

			public bool IsHiROM
				=> (SpeedField & 1) is 1;

			public bool IsSelfDestructing
				=> (SelfDestructionField & LIMITED_0_PLAYS_LEFT) is LIMITED_0_PLAYS_LEFT;

			internal byte MagicDRMByte
				=> _header[OFFSET_MAGIC_DRM_BYTE];

			public int RemainingPlays
				=> SelfDestructionField switch
				{
					LIMITED_5_PLAYS_LEFT => 5,
					LIMITED_4_PLAYS_LEFT => 4,
					LIMITED_3_PLAYS_LEFT => 3,
					LIMITED_2_PLAYS_LEFT => 2,
					LIMITED_1_PLAYS_LEFT => 1,
					LIMITED_0_PLAYS_LEFT => 0,
					_ => -1
				};

			public byte Revision
				=> _header[OFFSET_REVISION];

			internal byte SelfDestructionField
				=> _header[OFFSET_SELFDESTRUCT];

			internal byte SpeedField
				=> _header[OFFSET_SPEED];

			public string Title
				=> IOExtensions.ShiftJISEncoding.GetString(_header.Slice(start: OFFSET_TITLE, length: 0x10)).TrimEnd();

			public SatellaviewHeader(ReadOnlySpan<byte> header)
				=> _header = header;

			public override string ToString()
				=> $"[{ContentTypeField >> 4:X1}] {Title} r{Revision} ({(IsSelfDestructing ? RemainingPlays.ToString() : "unlimited")} plays left)";

			public bool VerifyChecksum(ReadOnlySpan<byte> rom)
				=> true; //TODO need to parse page mapping from offset 0x20..0x23 in order to calculate this
		}

		private const int HEADER_LENGTH = 0x50;

		private const int ROM_LENGTH = 0x100000;

		private const int THRESHOLD = 3;

		private static bool CheckHeaderHeuristics(bool checkHiROM, ReadOnlySpan<byte> rom, IList<string> warnings)
		{
			SatellaviewHeader header = new(rom.Slice(start: checkHiROM ? 0xFFB0 : 0x7FB0, length: HEADER_LENGTH));
			var corruption = 0;
			// "invalid" states were assigned a higher value if the wiki page was less vague

			if (header.Title.Length is 0) corruption++;

			if (header.IsSelfDestructing)
			{
				if (header.RemainingPlays is -1) corruption += 2;
			}
			else
			{
				if (header.SelfDestructionField is not SatellaviewHeader.UNLIMITED_PLAYS_LEFT) corruption += 2;
			}

			//TODO broadcast date, CBB

			if ((header.SpeedField & 0b1110) is not 0 || header.IsHiROM != checkHiROM) corruption += 2;

			if ((header.ContentTypeField & 0b1111) is not 0) corruption += 2;
			else if ((header.ContentTypeField >> 4) is not (0 or 1 or 2 or 3 or 10)) corruption++;

			if (header.MagicDRMByte is not 0x33) corruption += 3; // just this would probably have sufficed

			var checksumMatches = header.VerifyChecksum(rom);
			if (!checksumMatches)
			{
				corruption++;
				warnings.Add("mismatch with rom's internal checksum");
			}

			var detected = corruption <= THRESHOLD;
			if (detected) Util.DebugWriteLine($"heuristic match for Satellaview game/content ({(checkHiROM ? "HiROM" : "LoROM")}, -{corruption} pts.): {header.ToString()}");
			else
			{
//				Util.DebugWriteLine($"didn't match {(checkHiROM ? "HiROM" : "LoROM")}, -{corruption} pts.: {header.ToString()}");
				warnings.Clear();
			}
			return detected;
		}

		/// <remarks>not to be confused with a "slotted cart" i.e. base game, which we treat as either firmware or a normal SNES rom</remarks>
		public static bool IsSatellaviewRom(ReadOnlySpan<byte> rom, out IReadOnlyList<string> warnings)
		{
			if (rom.Length is not ROM_LENGTH)
			{
				warnings = Array.Empty<string>();
				return false;
			}
			List<string> warnings1 = new();
			//TODO which should we check first?
			var detected = CheckHeaderHeuristics(checkHiROM: false, rom, warnings1)
				|| CheckHeaderHeuristics(checkHiROM: true, rom, warnings1);
			warnings = warnings1;
			return detected;
		}
	}
}
