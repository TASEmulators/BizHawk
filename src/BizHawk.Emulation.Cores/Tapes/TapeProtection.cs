using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Tapes
{
	/// <summary>
	/// A recognized ZX Spectrum tape loading-scheme / copy-protection (None if nothing matched).
	/// </summary>
	public enum TapeProtectionScheme
	{
		None,
		StandardRom,
		Speedlock,
		Alkatraz,
		Bleepload,
		DigitalIntegration,
		SearchLoader,
		PaulOwens,
		Dinaload,
		Ftl,
		Zydroload,
		Novaload,
		Gremlin,
		Gremlin2,
		Micromega,
		TheEdge,
		SoftwareProjects,
		Microprose,
		Sentient,
		RollerCoaster,
		Haxpoc,
		Rqfl,
		Edos,
		Moonlighter,
		ZetaLoad,
		Players,
		PowerLoad,
		EliteUniLoader,
		SoftLock,
	}

	/// <summary>
	/// Identifies well-known ZX Spectrum tape loading / copy-protection schemes from the parsed tape blocks,
	/// for logging (the tape analogue of DiskProtection).
	/// Detection is PASSIVE - report-only; it never changes loading behaviour, so it runs unconditionally
	/// (no determinism gate). Two signal sources, mirroring the disk detector:
	/// (1) CODE-side - most custom loaders deliver their edge-poll stub as ordinary block data, so the loop's
	///     opcode bytes around IN A,(0xFE) are present in TapeDataBlock.BlockData. The
	///     bytes around the port read - the counter (INC B/DEC B), the bit mask (0x20 = bit 5 vs
	///     0x40 = bit 6) and the no-edge exit (RET NC / RET Z / NOP / JP) - are what
	///     distinguish one scheme's ROM-derived loop from another's.
	/// (2) PULSE-side - a few schemes have a uniquely distinctive pilot-tone pulse width (Gremlin 2 = 2670T,
	///     Micromega = 1739T) that identifies them straight from the pulse list.
	/// Signatures follow the documented loader fingerprints (community references / Fuse's documented opcode
	/// tables read as facts); this is an independent implementation of those facts, not copied code.
	/// </summary>
	public static class TapeProtection
	{
		public static string DisplayName(TapeProtectionScheme scheme)
			=> scheme switch
			{
				TapeProtectionScheme.None => "None (or unknown)",
				TapeProtectionScheme.StandardRom => "Standard ROM loader",
				TapeProtectionScheme.DigitalIntegration => "Digital Integration",
				TapeProtectionScheme.SearchLoader => "Search Loader (MultiLoad)",
				TapeProtectionScheme.PaulOwens => "Paul Owens Protection System",
				TapeProtectionScheme.Ftl => "FTL / Gargoyle",
				TapeProtectionScheme.Gremlin => "Gremlin 1",
				TapeProtectionScheme.Gremlin2 => "Gremlin 2",
				TapeProtectionScheme.TheEdge => "The Edge",
				TapeProtectionScheme.SoftwareProjects => "Software Projects",
				TapeProtectionScheme.RollerCoaster => "Roller Coaster",
				TapeProtectionScheme.Haxpoc => "Haxpoc-Lock",
				TapeProtectionScheme.Rqfl => "Really Quite Fast Loader (RQFL)",
				TapeProtectionScheme.Edos => "EDOS",
				TapeProtectionScheme.ZetaLoad => "ZetaLoad (RAMSOFT)",
				TapeProtectionScheme.PowerLoad => "Power-Load",
				TapeProtectionScheme.EliteUniLoader => "Elite Uni-Loader",
				TapeProtectionScheme.SoftLock => "SoftLock",
				_ => scheme.ToString(),
			};

		/// <summary>
		/// Detect a tape loading scheme from the parsed block list (best-effort, for reporting).
		/// </summary>
		public static TapeProtectionScheme Detect(IReadOnlyList<TapeDataBlock> blocks)
		{
			if (blocks == null || blocks.Count == 0) return TapeProtectionScheme.None;

			// Gather the signals in one pass. IMPORTANT (learned from validating against real dumps): many
			// loaders are ENCRYPTED on tape, so their in-memory poll-loop opcodes are NOT present in the block
			// data - only PLAINTEXT first-stage code (ROM-edge relocators) is matchable. The reliably-present
			// signals are the PULSE ones (a turbo data block's bit-0/bit-1 cell timings, its flag byte, pilot
			// width/count) plus block structure, since those are the physical encoding and cannot be encrypted.
			bool novaload = false, call5e7 = false, edgeReloc = false, spReloc = false, bleep = false,
				search = false, microprose = false, rollercoaster = false, rqfl = false, zydro = false, zeta = false;
			foreach (var b in blocks)
			{
				var d = b.BlockData;
				if (d == null || d.Length < 4) continue;
				if (ContainsAscii(d, "PSS NOVALOAD")) novaload = true;
				if (Contains(d, 0xCD, 0xE7, 0x05)) call5e7 = true;                                          // CALL 05E7 (ROM edge routine)
				if (Contains(d, 0x11, 0x00, 0xFF, 0x01, 0x00, 0x01, 0xED, 0xB0)) edgeReloc = true;           // relocate loader to FF00 (The Edge)
				if (Contains(d, 0x31, 0x01, 0xFF)) spReloc = true;                                          // LD SP,#FF01 (Software Projects)
				if (Contains(d, 0xDB, 0xFE, 0xA9, 0xE6, 0x40)) search = true;                               // IN A,(FE); XOR C; AND #40 (Search Loader, bit-6)
				if (Contains(d, 0x32, 0x15, 0xFF)) bleep = true;                                            // LD (#FF15),A self-mod (Bleepload, per MakeTZX source)
				if (Contains(d, 0xDD, 0x21, 0x03, 0xF8, 0xFD, 0x21, 0x00, 0x60)) microprose = true;         // LD IX,#F803; LD IY,#6000 (Microprose)
				if (Contains(d, 0xD3, 0xFE, 0xDB, 0xFE, 0xE6, 0x20)) rollercoaster = true;                  // OUT (FE),A; IN A,(FE); AND #20 (Roller Coaster)
				if (Contains(d, 0x21, 0x00, 0x58, 0x11, 0x01, 0x58, 0x01, 0xFF, 0x02, 0xED, 0xB0)) rqfl = true; // attr-clear LDIR (RQFL)
				if (Contains(d, 0xFB, 0xED, 0x4D, 0xFB, 0xE1, 0xC9)) zydro = true;                          // IM2 handler EI;RETI;EI;POP HL;RET (Zydroload)
				if (Contains(d, 0x10, 0xFF, 0x02, 0x00, 0x80)) zeta = true;                                 // ZetaLoad table marker (MakeTZX source)
			}

			// --- distinctive plaintext code signatures (most specific first) ---
			if (novaload) return TapeProtectionScheme.Novaload;
			if (zeta) return TapeProtectionScheme.ZetaLoad;
			if (microprose) return TapeProtectionScheme.Microprose;
			if (rqfl) return TapeProtectionScheme.Rqfl;
			if (rollercoaster) return TapeProtectionScheme.RollerCoaster;
			if (zydro) return TapeProtectionScheme.Zydroload;
			if (call5e7 && edgeReloc) return TapeProtectionScheme.TheEdge;
			if (call5e7 && spReloc) return TapeProtectionScheme.SoftwareProjects;
			if (search) return TapeProtectionScheme.SearchLoader;
			if (bleep) return TapeProtectionScheme.Bleepload;

			// Speedlock FIRST (before the Alkatraz bit test): Speedlock's ~555/1110 turbo bits are almost identical
			// to Alkatraz's 560/1120, but only Speedlock carries the composite sync - key on that. The composite
			// sync appears as either a ~2100T Pure_Tone run OR a Pulse_Sequence carrying the ~1420T mid-pulse
			// (1420 = 2x710, neither a standard sync 667/735 nor any bit cell - unique to Speedlock's sync).
			foreach (var b in blocks)
			{
				if (b.BlockDescription == BlockType.Pure_Tone && PilotPulseCount(b, 2100) >= 100)
					return TapeProtectionScheme.Speedlock;
				if (b.BlockDescription == BlockType.Pulse_Sequence && HasPulseNear(b, 1420))
					return TapeProtectionScheme.Speedlock;
			}

			// Speedlock also breaks the load into MANY small blocks (real dumps: 140-565). Its ~560/1120 turbo
			// bits are identical to Alkatraz's and Players', but those use only a handful of blocks (<=14 / ~9),
			// so a high block count + 560/1120 bits reliably means Speedlock. (Its data is encrypted, so the flag
			// byte varies and is not a tell.) Checked before the Alkatraz/Players bit tests below.
			if (blocks.Count > 64)
				foreach (var b in blocks)
					if (IsDataBlock(b.BlockDescription))
					{
						var (sb0, sb1) = BitCells(b);
						if (Near(sb0, 560) && Near(sb1, 1120)) return TapeProtectionScheme.Speedlock;
					}

			// --- FLAG-BYTE tells: scan EVERY block with real data (the loader's flag byte is encryption-proof
			// and a non-00/FF flag can NOT occur on a normal ROM block, so it uniquely marks a custom loader).
			// FTL in particular delivers its flag-99 data as a STANDARD-speed block on some rips, so we can't
			// restrict this to turbo blocks. ---
			bool f84 = false, f21 = false, f80 = false, f81 = false, f82 = false;
			foreach (var b in blocks)
			{
				var d = b.BlockData;
				if (d == null || d.Length <= 64) continue;
				byte flag = d[0];
				var (fb0, fb1) = BitCells(b);
				if (flag == 0x99 && fb1 > 1200) return TapeProtectionScheme.Ftl;        // FTL/Gargoyle: flag #99
				if (flag == 0x98 && fb1 > 1200) return TapeProtectionScheme.PaulOwens;  // Paul Owens: the #98 loader constant is the block flag
				if (flag == 0xFD && b.BlockDescription == BlockType.Pure_Data_Block) return TapeProtectionScheme.DigitalIntegration; // DI: Pure Data block, flag #FD
				if (flag == 0x07 && fb1 is >= 1300 and <= 1360) return TapeProtectionScheme.Novaload; // PSS Novaload: flag #07, bit ~670/1330
				// Power-Load and Elite Uni-Loader use fixed flag bytes on their data blocks - keyed on the
				// COMBINATION (not any single byte) so encrypted-flag noise from other schemes can't false-match.
				if (flag == 0x84) f84 = true; else if (flag == 0x21) f21 = true;
				else if (flag == 0x80) f80 = true; else if (flag == 0x81) f81 = true; else if (flag == 0x82) f82 = true;
			}
			if (f84 && f21) return TapeProtectionScheme.PowerLoad;              // Power-Load: flag #84 turbo + flag #21 data
			if (f80 && f81 && f82) return TapeProtectionScheme.EliteUniLoader;  // Elite Uni-Loader: sequential flags #80/#81/#82

			// --- BIT-CELL tells on the turbo data blocks. Fast bit-timings collide across schemes, so the FLAG
			// byte breaks the ties: Players' turbo flag is #00, Alkatraz's is always non-#00 (9B/65/24/A1/28...).
			foreach (var b in blocks)
			{
				if (!IsDataBlock(b.BlockDescription)) continue;
				var d = b.BlockData;
				byte flag = d is { Length: > 0 } ? d[0] : (byte)0;
				var (bit0, bit1) = BitCells(b);
				if (call5e7 && Near(bit0, 465) && Near(bit1, 930)) return TapeProtectionScheme.Gremlin;   // Gremlin 1: ROM-edge loader + 465/930 bits
				if (b.BlockDescription == BlockType.Pure_Data_Block && Near(bit0, 680) && Near(bit1, 1340)) return TapeProtectionScheme.SoftLock; // SoftLock: Pure Data @680/1340 (Novaload uses Turbo+flag07, caught above)
				if (flag == 0x00 && Near(bit0, 620) && Near(bit1, 1240)) return TapeProtectionScheme.Gremlin2; // Gremlin 2: 620/1240, flag #00 (vs Haxpoc 620/1220 flag #FF)
				if (flag == 0x00 && bit0 is >= 555 and <= 605 && bit1 is >= 1115 and <= 1205) return TapeProtectionScheme.Players; // Players 1/2: 560-600/1120-1200, flag #00
				if (flag != 0x00 && ((Near(bit0, 560) && Near(bit1, 1120)) || (Near(bit0, 620) && Near(bit1, 1060))))
					return TapeProtectionScheme.Alkatraz;                                               // Alkatraz: 560/1120 (or 720 Degrees' 620/1060), non-#00 flag
			}

			// --- pulse pass: distinctive pilot widths and counts ---
			foreach (var b in blocks)
			{
				int pilot = PilotPulseWidth(b);
				if (pilot != 0 && Near(pilot, 1739)) return TapeProtectionScheme.Micromega;
			}
			// pilot-COUNT tells: a standard 2168T pilot but an unusual number of pulses
			foreach (var b in blocks)
			{
				int cnt = PilotPulseCount(b, 2168);
				if (cnt == 0) continue;
				if (cnt is >= 8188 and <= 8199) return TapeProtectionScheme.Edos;       // 8193 / 8194 pilot
				if (cnt is >= 6885 and <= 6940) return TapeProtectionScheme.Moonlighter; // ~6912 pilot
			}


			// --- fallback: a tape made only of standard-speed blocks is a plain ROM load ---
			bool anyData = false, allStandard = true;
			foreach (var b in blocks)
			{
				switch (b.BlockDescription)
				{
					case BlockType.Standard_Speed_Data_Block:
						anyData = true;
						break;
					case BlockType.Turbo_Speed_Data_Block:
					case BlockType.Pure_Data_Block:
					case BlockType.Generalized_Data_Block:
					case BlockType.CSW_Recording:
					case BlockType.WAV_Recording:
					case BlockType.Direct_Recording:
						anyData = true;
						allStandard = false;
						break;
				}
			}
			if (anyData && allStandard) return TapeProtectionScheme.StandardRom;

			return TapeProtectionScheme.None;
		}

		// The pilot tone is a long run of (near-)equal pulses. Return the pilot pulse width if a run of at least
		// 256 near-equal pulses is present near the block start, else 0. Robust to the odd jittered value.
		private static int PilotPulseWidth(TapeDataBlock b)
		{
			var p = b.DataPeriods;
			if (p == null || p.Count < 256) return 0;
			int runVal = 0, runLen = 0;
			foreach (var v in p)
			{
				if (v > 0 && Near(v, runVal)) runLen++;
				else { runVal = v; runLen = 1; }
				if (runLen >= 256 && runVal > 100) return runVal; // a genuine pilot tone
			}
			return 0;
		}

		// Length of the longest run of pulses near width (the pilot tone at the given width),
		// so a scheme can be keyed on an unusual pilot COUNT at the standard 2168T width. 0 if none.
		private static int PilotPulseCount(TapeDataBlock b, int width)
		{
			var p = b.DataPeriods;
			if (p == null) return 0;
			int best = 0, run = 0;
			foreach (var v in p)
			{
				if (Near(v, width)) { run++; if (run > best) best = run; }
				else run = 0;
			}
			return best;
		}

		private static bool IsDataBlock(BlockType t) => t is BlockType.Turbo_Speed_Data_Block
			or BlockType.Pure_Data_Block or BlockType.Generalized_Data_Block;

		// The two most common short pulse widths in a block = its bit-0 / bit-1 cell timings (returned low,high).
		// A ZX bit is two equal pulses, so these values are directly comparable to the documented bit-0/bit-1.
		private static (int bit0, int bit1) BitCells(TapeDataBlock b)
		{
			var p = b.DataPeriods;
			if (p == null) return (0, 0);
			var tally = new Dictionary<int, int>();
			foreach (var v in p)
				if (v is > 200 and < 2000) { int k = (v + 10) / 20 * 20; tally[k] = (tally.TryGetValue(k, out int c) ? c : 0) + 1; }
			int a = 0, an = 0, bb = 0, bn = 0;
			foreach (var kv in tally)
			{
				if (kv.Value > an) { bb = a; bn = an; a = kv.Key; an = kv.Value; }
				else if (kv.Value > bn) { bb = kv.Key; bn = kv.Value; }
			}
			return a <= bb ? (a, bb) : (bb, a);
		}

		// True if any pulse in the block is near target T-states.
		private static bool HasPulseNear(TapeDataBlock b, int target)
		{
			var p = b.DataPeriods;
			if (p == null) return false;
			foreach (var v in p) if (Near(v, target)) return true;
			return false;
		}

		private static bool Near(int a, int b) => a > 0 && b > 0 && System.Math.Abs(a - b) <= 30;

		// Does the byte array contain the given contiguous opcode sequence anywhere?
		private static bool Contains(byte[] data, params byte[] pattern)
		{
			if (data == null || data.Length < pattern.Length) return false;
			for (int i = 0; i <= data.Length - pattern.Length; i++)
			{
				int j = 0;
				for (; j < pattern.Length; j++) if (data[i + j] != pattern[j]) break;
				if (j == pattern.Length) return true;
			}
			return false;
		}

		private static bool ContainsAscii(byte[] data, string s)
		{
			if (data == null || data.Length < s.Length) return false;
			for (int i = 0; i <= data.Length - s.Length; i++)
			{
				int j = 0;
				for (; j < s.Length; j++) if (data[i + j] != (byte)s[j]) break;
				if (j == s.Length) return true;
			}
			return false;
		}
	}
}
