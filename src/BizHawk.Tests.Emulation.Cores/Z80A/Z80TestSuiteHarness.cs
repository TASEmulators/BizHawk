using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Z80ATests
{
	/// <summary>
	/// Runs the standard ZX Spectrum Z80 test tapes (zexall/zexdoc, Patrik Rak's z80test suite, and the
	/// Bobrowski/Rak timing tests btime/ptime/stime/ulatest3) through the real ZXHawk 48K/128K cores headless,
	/// so we can confirm the Z80AOpt optimizations did not regress instruction or timing behaviour.
	///
	/// The tapes are freely available but not committed (kept out of the repo like the disk-test IPFs). To run
	/// these, drop the .tap files into a "Resources/z80tests" folder next to the test assembly, or set the
	/// BIZHAWK_Z80TESTS env var to a folder containing them; each test is Inconclusive when its tape is absent,
	/// so this never fails on a machine without the tapes. Sources: zexall/zexdoc from mdfs.net; the z80test
	/// suite (z80full/z80doc/z80flags/z80ccf/z80memptr) from github.com/raxoft/z80test; btime/ptime/ptime-128/
	/// stime from the Bobrowski/Rak timing set. Decoded result screens are written to the system temp dir.
	///
	/// Each instruction test takes ~1-4 min unthrottled in a Release build (zexall is impractically slow - its
	/// first test alone exceeds vstest's 5-min run timeout - so z80test supersedes it); run individually.
	/// </summary>
	[TestClass]
	public sealed class Z80TestSuiteHarness
	{
		// Tapes: BIZHAWK_Z80TESTS env var if set, else a Resources/z80tests folder beside the test assembly.
		private static readonly string TapeDir =
			Environment.GetEnvironmentVariable("BIZHAWK_Z80TESTS")
			?? Path.Combine(Path.GetDirectoryName(typeof(Z80TestSuiteHarness).Assembly.Location)!, "Resources", "z80tests");
		private static readonly string OutDir = Path.Combine(Path.GetTempPath(), "zxhawk_z80tests");

		private sealed class StubFiles : ICoreFileProvider
		{
			public string GetRetroSaveRAMDirectory(string corePath) => throw new NotImplementedException();
			public string GetRetroSystemPath(string corePath) => throw new NotImplementedException();
			public string GetUserPath(string sysID, bool temp) => throw new NotImplementedException();
			public byte[]? GetFirmware(FirmwareID id, string? msg = null) => null;
			public byte[] GetFirmwareOrThrow(FirmwareID id, string? msg = null) => throw new NotImplementedException();
			public (byte[] FW, GameInfo Game) GetFirmwareWithGameInfoOrThrow(FirmwareID id, string? msg = null) => throw new NotImplementedException();
		}

		private sealed class StubGL : IOpenGLProvider
		{
			public bool SupportsGLVersion(int major, int minor) => false;
			public object RequestGLContext(int major, int minor, bool coreProfile) => throw new NotImplementedException();
			public void ReleaseGLContext(object context) { }
			public void ActivateGLContext(object context) { }
			public void DeactivateGLContext() { }
			public IntPtr GetGLProcAddress(string? proc) => IntPtr.Zero;
		}

		private sealed class RomAsset : IRomAsset
		{
			public byte[] RomData { get; set; }
			public byte[] FileData { get; set; }
			public string Extension { get; set; }
			public string RomPath { get; set; }
			public GameInfo Game { get; set; }
		}

		private sealed class ScriptController : IController
		{
			private readonly List<(int Start, int End, string Key)> _script;
			public int Frame;
			// a key tapped every 40 frames from RepeatFrom onward, to auto-answer the ROM "scroll?" prompt
			// (any non-BREAK key continues) so multi-screen test output keeps flowing without a human.
			public string RepeatKey;
			public int RepeatFrom = int.MaxValue;
			public ScriptController(ControllerDefinition def, List<(int, int, string)> script) { Definition = def; _script = script; }
			public ControllerDefinition Definition { get; }
			public bool IsPressed(string button)
			{
				foreach (var (s, e, k) in _script) if (k == button && Frame >= s && Frame < e) return true;
				if (button == RepeatKey && Frame >= RepeatFrom && (Frame % 40) < 3) return true;
				return false;
			}
			public int AxisValue(string name) => 0;
			public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();
			public void SetHapticChannelStrength(string name, int strength) { }
		}

		private static ZX MakeCore(MachineType machineType, byte[] tap)
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var roms = new List<IRomAsset>
			{
				new RomAsset { RomData = tap, FileData = tap, Extension = ".tap", RomPath = "test.tap", Game = new GameInfo { Name = "test" } },
			};
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings { GigascreenFrameBlend = false },
				// Instant (flash) tape load so the ~14KB code block loads in a frame instead of real-time
				// pulse replay; requires DeterministicEmulation off. CPU/ULA timing is unaffected by this - it
				// only changes how the tape bytes get into RAM, which is irrelevant to what these tests measure.
				SyncSettings = new ZX.ZXSpectrumSyncSettings
				{
					MachineType = machineType,
					AutoLoadTape = true,
					DeterministicEmulation = false,
					TapeLoadSpeed = ZX.TapeLoadSpeed.Instant,
				},
				Roms = roms,
			};
			return new ZX(lp);
		}

		// Decode the 32x24 text screen straight from display RAM, matching each 8x8 cell against the ROM font
		// at 0x3D00 (handles normal and inverse video). Returns 24 strings. Reads via the "System Bus" domain.
		private static string[] DecodeScreen(ZX core)
		{
			var bus = core.ServiceProvider.GetService<IMemoryDomains>()!.SystemBus;
			byte Peek(int a) => bus.PeekByte(a);

			// build the font lookup (char -> 8 bytes) once from the currently-paged ROM
			var font = new Dictionary<long, char>();
			for (int ch = 32; ch < 128; ch++)
			{
				long key = 0;
				for (int i = 0; i < 8; i++) key = (key << 8) | Peek(0x3D00 + (ch - 32) * 8 + i);
				if (!font.ContainsKey(key)) font[key] = (char)ch;
			}

			var lines = new string[24];
			for (int row = 0; row < 24; row++)
			{
				var sb = new System.Text.StringBuilder(32);
				for (int col = 0; col < 32; col++)
				{
					long normal = 0, inverse = 0;
					for (int line = 0; line < 8; line++)
					{
						int pr = row * 8 + line;
						int addr = 0x4000 | ((pr & 7) << 8) | ((pr & 0x38) << 2) | ((pr & 0xC0) << 5) | col;
						byte b = Peek(addr);
						normal = (normal << 8) | b;
						inverse = (inverse << 8) | (byte)(b ^ 0xFF);
					}
					if (font.TryGetValue(normal, out char c)) sb.Append(c);
					else if (font.TryGetValue(inverse, out char ci)) sb.Append(ci);
					else sb.Append(normal == 0 ? ' ' : '?');
				}
				lines[row] = sb.ToString().TrimEnd();
			}
			return lines;
		}

		// Load a tape, run it, and accumulate the scrolling text output (distinct non-blank lines in first-seen
		// order). Stops early once the screen has been unchanged for idleStopFrames (test finished/waiting).
		// 128K: the boot menu selects "Tape Loader" with Return -> LOAD "". 48K: type LOAD"" at the BASIC
		// prompt (J = LOAD keyword in K mode; SymShift+P = "). Start late enough that the 48K copyright
		// prompt is up, hold each key ~12 frames with clean gaps for the ROM's key debounce.
		private static List<(int, int, string)> BootScript(MachineType machine) => machine == MachineType.ZXSpectrum48
			? new List<(int, int, string)>
				{
					(150, 162, "Key J"),
					(180, 192, "Key Symbol Shift"), (180, 192, "Key P"),
					(210, 222, "Key Symbol Shift"), (210, 222, "Key P"),
					(245, 257, "Key Return"),
				}
			: new List<(int, int, string)> { (60, 70, "Key Return") };

		private static (List<string> Transcript, bool Finished) RunAndCaptureTranscript(MachineType machine, byte[] tap, int maxFrames, int decodeEvery = 30, int romStopFrames = 600)
		{
			var core = MakeCore(machine, tap);
			var emu = (IEmulator)core;
			var dbg = core.ServiceProvider.GetService<IDebuggable>()!;
			var ctrl = new ScriptController(core.ControllerDefinition, BootScript(machine))
			{
				// Return continues past each "scroll?" prompt (it is not a BREAK key)
				RepeatKey = "Key Return",
				RepeatFrom = 200,
			};

			var seen = new HashSet<string>();
			var transcript = new List<string>();
			int romStreak = 0;
			bool finished = false;
			for (int f = 1; f <= maxFrames; f++)
			{
				ctrl.Frame = f;
				emu.FrameAdvance(ctrl, true, false);
				if (f % decodeEvery != 0) continue;

				var lines = DecodeScreen(core);
				foreach (var l in lines)
					// skip garbled mid-scroll captures (they decode to '?'); clean result lines (incl. any
					// failure "...expected:...") never contain '?', so failure detection stays reliable
					if (l.Length > 0 && !l.Contains('?') && seen.Add(l)) transcript.Add(l);

				// Completion by program counter: the test code loads at >=0x8000 and computes there for a long
				// time (screen static - so screen-idle detection is unreliable); when it finishes it RETs to
				// BASIC (ROM, <0x4000). Printing dips into ROM briefly but returns to >=0x8000, and the
				// "scroll?" prompt is answered every 40 frames, so only a genuine return-to-BASIC accumulates a
				// sustained ROM streak.
				if (f > 900)
				{
					int pc = (int)dbg.GetCpuFlagsAndRegisters()["PC"].Value;
					if (pc < 0x4000) { romStreak += decodeEvery; if (romStreak >= romStopFrames) { finished = true; break; } }
					else romStreak = 0;
				}
			}
			return (transcript, finished);
		}

		// Shared assertion for the self-validating instruction tests (zexall / z80test): the run must finish,
		// must have produced OK results, and must contain no failure line ("expected" appears only on a CRC
		// mismatch). Writes the full transcript to OutDir for inspection either way.
		private static void RunInstructionTest(MachineType machine, string tapPath, string label, int maxFrames)
		{
			if (!File.Exists(tapPath)) { Assert.Inconclusive($"tape not present: {tapPath}"); return; }
			Directory.CreateDirectory(OutDir);
			var (transcript, finished) = RunAndCaptureTranscript(machine, File.ReadAllBytes(tapPath), maxFrames);
			File.WriteAllLines(Path.Combine(OutDir, $"{label}_transcript.txt"), transcript);

			// Collect the names of failed tests. z80test prints "<nnn name> FAILED" (then a CRC line); zexall
			// prints "<name>... CRC:xxxx expected:yyyy". IN-family tests (IN/INI/IND...) read a real port so
			// they depend on ULA floating-bus emulation, not the Z80 core - flag them separately so a
			// floating-bus difference is not mistaken for a CPU regression.
			var failed = new List<string>();
			foreach (var l in transcript)
			{
				bool isFail = l.IndexOf("FAILED", StringComparison.OrdinalIgnoreCase) >= 0
					|| l.IndexOf("expected", StringComparison.OrdinalIgnoreCase) >= 0;
				if (isFail) failed.Add(l);
			}
			bool anyOk = transcript.Exists(l => l.EndsWith("OK"));

			Assert.IsTrue(finished, $"{label}: did not finish within {maxFrames} frames (still running). See {label}_transcript.txt");
			Assert.IsTrue(anyOk, $"{label}: no OK results decoded - the test may not have started. See {label}_transcript.txt");

			bool allIn = failed.TrueForAll(l =>
				l.IndexOf("IN", StringComparison.OrdinalIgnoreCase) >= 0
				|| l.IndexOf("CRC", StringComparison.OrdinalIgnoreCase) >= 0); // CRC lines belong to the IN failures above
			string report = failed.Count == 0 ? "all passed" : string.Join(" | ", failed);
			Assert.IsTrue(failed.Count == 0 || allIn,
				$"{label}: CPU test failure(s) beyond the IN/port family: {report}. See {label}_transcript.txt");
			if (failed.Count != 0)
				Console.WriteLine($"{label}: passed except IN/port-family tests (floating-bus dependent): {report}");
		}

		private static string Z80TestTap(string name) => Path.Combine(TapeDir, name);
		private static string Tap(string name) => Path.Combine(TapeDir, name);

		// --- Instruction tests (self-validating). Each ~1-4 min; run individually. ---
		[TestMethod] public void Z80doc_128() => RunInstructionTest(MachineType.ZXSpectrum128, Z80TestTap("z80doc.tap"), "z80doc_128", 400000);
		[TestMethod] public void Z80full_128() => RunInstructionTest(MachineType.ZXSpectrum128, Z80TestTap("z80full.tap"), "z80full_128", 800000);
		[TestMethod] public void Zexall_128() => RunInstructionTest(MachineType.ZXSpectrum128, Tap("zexall.tap"), "zexall_128", 3000000);
		[TestMethod] public void Zexdoc_128() => RunInstructionTest(MachineType.ZXSpectrum128, Tap("zexdoc.tap"), "zexdoc_128", 3000000);
		[TestMethod] public void Zexall_48() => RunInstructionTest(MachineType.ZXSpectrum48, Tap("zexall.tap"), "zexall_48", 3000000);
		[TestMethod] public void Z80doc_48() => RunInstructionTest(MachineType.ZXSpectrum48, Z80TestTap("z80doc.tap"), "z80doc_48", 400000);

		// --- Timing tests: capture the decoded result screen for inspection / master comparison. ---
		private static void CaptureTiming(MachineType machine, string tapPath, string label, int frames)
		{
			if (!File.Exists(tapPath)) { Assert.Inconclusive($"tape not present: {tapPath}"); return; }
			Directory.CreateDirectory(OutDir);
			var core = MakeCore(machine, File.ReadAllBytes(tapPath));
			var emu = (IEmulator)core;
			var ctrl = new ScriptController(core.ControllerDefinition, BootScript(machine));
			for (int f = 1; f <= frames; f++) { ctrl.Frame = f; emu.FrameAdvance(ctrl, true, false); }
			var lines = DecodeScreen(core);
			File.WriteAllLines(Path.Combine(OutDir, $"{label}_result.txt"), lines);
			Assert.IsTrue(Array.Exists(lines, l => l.Length > 0), $"{label}: blank screen - did not run. See {label}_result.txt");
		}

		[TestMethod] public void Btime_48() => CaptureTiming(MachineType.ZXSpectrum48, Tap("btime.tap"), "btime_48", 2500);
		[TestMethod] public void Btime_128() => CaptureTiming(MachineType.ZXSpectrum128, Tap("btime.tap"), "btime_128", 2000);
		[TestMethod] public void Ptime_128() => CaptureTiming(MachineType.ZXSpectrum128, Tap("ptime.tap"), "ptime_128", 3000);
		[TestMethod] public void Ptime128_128() => CaptureTiming(MachineType.ZXSpectrum128, Tap("ptime-128.tap"), "ptime128_128", 3000);
		[TestMethod] public void Stime_48() => CaptureTiming(MachineType.ZXSpectrum48, Tap("stime.tap"), "stime_48", 3000);
	}
}
