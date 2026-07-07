using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Tape
{
	/// <summary>
	/// Regression anchor for the tape SIGNAL path (datacorder). The per-model fingerprint guard runs with no
	/// tape, so it does not cover LoadTape -> DataBlocks -> Play -> GetEarBit -> tape beeper. This test loads a
	/// synthetic TAP (a single header block, which produces a long steady pilot tone), presses "Play Tape", and
	/// hashes the rendered video + sync audio over a fixed number of frames. It exists so the upcoming
	/// extraction of the shared TapeDeck (and the per-model clock scaling) can be proven byte-identical, and so
	/// the deliberate 128K timing fix shows up as an intended, reviewed change to the 128K golden only.
	///
	/// The synthetic TAP is generated in code (no copyrighted media, fully deterministic). AutoLoadTape is off,
	/// so nothing auto-stops the tape: the single header block plays a continuous pilot tone for the whole run.
	/// </summary>
	[TestClass]
	public sealed class TapeLoadRegressionTests
	{
		private sealed class StubFiles : ICoreFileProvider
		{
			public string GetRetroSaveRAMDirectory(string corePath) => throw new NotImplementedException();
			public string GetRetroSystemPath(string corePath) => throw new NotImplementedException();
			public string GetUserPath(string sysID, bool temp) => throw new NotImplementedException();
			public byte[]? GetFirmware(FirmwareID id, string? msg = null) => throw new NotImplementedException();
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

		/// <summary>
		/// A controller that reports a fixed set of buttons as pressed.
		/// </summary>
		private sealed class PressController : IController
		{
			private readonly HashSet<string> _pressed;
			public PressController(params string[] pressed) => _pressed = new HashSet<string>(pressed);
			public ControllerDefinition Definition => null;
			public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();
			public bool IsPressed(string button) => _pressed.Contains(button);
			public int AxisValue(string name) => 0;
			public void SetHapticChannelStrength(string name, int strength) { }
		}

		// A standard-speed TAP block: [len lo][len hi][flag][payload...][checksum], where the length covers the
		// flag byte, the payload and the XOR checksum, and the checksum is flag XOR every payload byte.
		private static byte[] MakeTapBlock(byte flag, byte[] payload)
		{
			int len = payload.Length + 2; // flag + checksum
			var block = new byte[2 + len];
			block[0] = (byte)(len & 0xFF);
			block[1] = (byte)((len >> 8) & 0xFF);
			block[2] = flag;
			Array.Copy(payload, 0, block, 3, payload.Length);
			byte chk = flag;
			foreach (var b in payload) chk ^= b;
			block[^1] = chk;
			return block;
		}

		// A 17-byte header block (flag 0x00 -> long pilot tone) followed by a small data block (flag 0xFF).
		// Values are arbitrary; only the pulse timing matters for the signal path. The data block keeps the
		// file comfortably larger than any converter's fixed-size CheckType header probe (CSW reads 22 bytes).
		private static byte[] MakeSyntheticTap()
		{
			var header = new byte[17];
			header[0] = 0x00; // type: Program
			var name = Encoding.ASCII.GetBytes("test      "); // 10 chars
			Array.Copy(name, 0, header, 1, 10);
			header[11] = 0x80; header[12] = 0x00; // data length (128)
			header[13] = 0x0A; header[14] = 0x00; // param 1
			header[15] = 0x00; header[16] = 0x80; // param 2

			var payload = new byte[128];
			for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 3 + 1); // deterministic pattern

			var headerBlock = MakeTapBlock(0x00, header);
			var dataBlock = MakeTapBlock(0xFF, payload);
			var tap = new byte[headerBlock.Length + dataBlock.Length];
			Array.Copy(headerBlock, 0, tap, 0, headerBlock.Length);
			Array.Copy(dataBlock, 0, tap, headerBlock.Length, dataBlock.Length);
			return tap;
		}

		private static ZX MakeCore(MachineType machineType, byte[] tape)
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var roms = new List<IRomAsset>
			{
				new RomAsset { RomData = tape, FileData = tape, Extension = ".tap", RomPath = "test.tap", Game = new GameInfo { Name = "test" } },
			};
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = machineType, AutoLoadTape = false },
				Roms = roms,
			};
			return new ZX(lp);
		}

		private const int Frames = 120;

		// GOLDEN tape-signal fingerprints. The 48K is the reference and stays byte-identical (its 3.5MHz clock
		// IS the tape reference, so TapeDeck scales by 1.0). The 128K value below is POST-FIX: the datacorder
		// now scales the 3.5MHz-referenced pulse periods into the 128K's 3.5469MHz clock (ratio ~1.0134) so the
		// tape plays at the correct rate instead of ~1.3% fast. Before the fix its audio hash was
		// 496D30C65EE7A645; only the tape audio rate changed (its video hash is unchanged), which is why the
		// two lines below now share no audio hash by accident. Regenerate only for an intended tape-timing change.
		private static readonly Dictionary<MachineType, string> Golden = new()
		{
			[MachineType.ZXSpectrum48]  = "videoHash=23638072986F6115 audioHash=0D4D91208F958EB1 audioSamples=105840",
			[MachineType.ZXSpectrum128] = "videoHash=9AE68AEBE9E14D5D audioHash=E19C3E9345EDBE41 audioSamples=105840",
		};

		// Plays the tape ("Play Tape" pressed on the first frame, when requested) and hashes video + sync audio
		// across the run. The tape beeper is fed by TapeCycle whenever the tape is playing, independent of
		// whether the ROM is polling the EAR port - so no LOAD "" keystroke sequence is needed to exercise it.
		private static string RunTapeFingerprint(MachineType mt, bool play)
		{
			var core = MakeCore(mt, MakeSyntheticTap());
			var emu = (IEmulator)core;
			var vp = core.ServiceProvider.GetService<IVideoProvider>()!;
			var sp = core.ServiceProvider.GetService<ISoundProvider>();
			if (sp != null && sp.CanProvideAsync) sp.SetSyncMode(SyncSoundMode.Sync);

			var press = new PressController("Play Tape");
			var idle = new PressController();

			const ulong FnvOffset = 14695981039346656037UL, FnvPrime = 1099511628211UL;
			ulong vHash = FnvOffset, aHash = FnvOffset;
			long samplesTotal = 0;

			for (int f = 0; f < Frames; f++)
			{
				emu.FrameAdvance(play && f == 0 ? press : idle, true, true);

				var fb = vp.GetVideoBuffer();
				unchecked { foreach (var px in fb) { vHash ^= (uint)px; vHash *= FnvPrime; } }

				if (sp != null)
				{
					sp.GetSamplesSync(out short[] samples, out int nsamp);
					samplesTotal += nsamp;
					unchecked { for (int i = 0; i < nsamp * 2 && i < samples.Length; i++) { aHash ^= (ushort)samples[i]; aHash *= FnvPrime; } }
				}
			}

			return $"videoHash={vHash:X16} audioHash={aHash:X16} audioSamples={samplesTotal}";
		}

		[TestMethod]
		public void TapeSignal_PlaysDeterministically_PerModel()
		{
			var sb = new StringBuilder();
			var mismatches = new List<string>();
			sb.AppendLine($"ZX tape signal fingerprint ({Frames} frames, synthetic TAP, Play pressed frame 0):");

			foreach (var mt in new[] { MachineType.ZXSpectrum48, MachineType.ZXSpectrum128 })
			{
				string a = RunTapeFingerprint(mt, play: true);
				string b = RunTapeFingerprint(mt, play: true);
				sb.AppendLine($"  {mt,-20}: {a}");
				Assert.AreEqual(a, b, $"{mt}: tape signal must be deterministic across identical runs");

				// the played tape must actually change the output vs an idle run, otherwise this guards nothing
				string idleRun = RunTapeFingerprint(mt, play: false);
				Assert.AreNotEqual(idleRun, a, $"{mt}: playing the tape must change the audio (signal is being exercised)");

				if (Golden.TryGetValue(mt, out var expected) && a != expected)
					mismatches.Add($"{mt}:\n    golden {expected}\n    actual {a}");
			}

			string outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zx_tape_fingerprints.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, sb.ToString());
			Console.WriteLine(sb.ToString());

			Assert.IsTrue(mismatches.Count == 0,
				"tape signal diverged from the captured golden - the datacorder's output changed:\n" + string.Join("\n", mismatches));
		}
	}
}
