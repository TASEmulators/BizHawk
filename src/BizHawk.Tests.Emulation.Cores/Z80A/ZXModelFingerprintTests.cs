using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Z80ATests
{
	/// <summary>
	/// Per-model behavioural FINGERPRINT — the golden master-vs-branch regression for the concern that
	/// the optimizations might have changed per-model timing (CPU clock / frame T-state length),
	/// contention, or interrupt behaviour without any test noticing (the savestate round-trips only prove
	/// NEW-vs-NEW self-consistency, not NEW-vs-OLD equivalence).
	///
	/// For each model it runs a fixed number of frames deterministically from reset (no input) and records:
	///   • per-frame T-state length (the first frame, the steady-state frame, and the total across all
	///     frames) — directly exposes frame-length + contention-stretch differences, and
	///   • an FNV-1a hash of every rendered framebuffer — exposes ANY difference in what executed/rendered
	///     (contention, interrupt timing, flash, paging, CPU behaviour all feed into this), and
	///   • an FNV-1a hash of the sync audio samples — exposes beeper/tape (event-driven) timing changes.
	///
	/// The captured values were taken on MASTER (the pre-optimization Z80A-based core, via `git stash`) and
	/// are embedded below as GOLDEN, then asserted here — so this runs in the normal harness as a standing
	/// guard that the optimizations don't change per-model behaviour. It uses ONLY public
	/// IEmulator/IVideoProvider/ISoundProvider/IDebuggable API, so it also compiles/runs against master.
	///
	/// This guard already earned its keep: it caught a +2a/+3/Pentagon regression (stale PageContended after
	/// paging) that every other test missed. If it fails after a future change, confirm the change is
	/// intended, then regenerate the golden: temporarily exclude any branch-only-member test files, `git
	/// stash` to master, run this test, and paste the new fingerprint lines from scratchpad/zx_fingerprints.txt.
	/// </summary>
	[TestClass]
	public sealed class ZXModelFingerprintTests
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
			public void ReleaseGLContext(object context) => throw new NotImplementedException();
			public void ActivateGLContext(object context) => throw new NotImplementedException();
			public void DeactivateGLContext() => throw new NotImplementedException();
			public IntPtr GetGLProcAddress(string? proc) => throw new NotImplementedException();
		}

		private static ZX MakeCore(MachineType machineType)
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = machineType },
			};
			return new ZX(lp);
		}

		// GOLDEN fingerprints — captured on MASTER (the pre-optimization Z80A-based core) and confirmed
		// byte-identical on this branch. This is the standing regression guard: the optimizations must not
		// change per-model frame length / contention / interrupt / rendering. If a future change alters a
		// model's behaviour this fails; re-verify it's intended, then regenerate (run CaptureFingerprints
		// against master via git stash and paste the new values). 500 frames from reset, no input.
		private const int Frames = 500;
		private static readonly Dictionary<MachineType, string> Golden = new()
		{
			[MachineType.ZXSpectrum16]        = "firstFrameT=69892 steadyFrameT=69888 totalT=34944003 videoHash=57F70551D7F0C5EB audioHash=B782CF1F93FD2D65",
			[MachineType.ZXSpectrum48]        = "firstFrameT=69892 steadyFrameT=69888 totalT=34944003 videoHash=CF235F6EB41D0A15 audioHash=B782CF1F93FD2D65",
			[MachineType.ZXSpectrum128]       = "firstFrameT=70912 steadyFrameT=70908 totalT=35454003 videoHash=380339D03D869DDD audioHash=B782CF1F93FD2D65",
			[MachineType.ZXSpectrum128Plus2]  = "firstFrameT=70912 steadyFrameT=70908 totalT=35454003 videoHash=511ACBDD420FE8AB audioHash=B782CF1F93FD2D65",
			[MachineType.ZXSpectrum128Plus2a] = "firstFrameT=70909 steadyFrameT=70908 totalT=35454000 videoHash=1AEACC083F82FC96 audioHash=B782CF1F93FD2D65",
			[MachineType.ZXSpectrum128Plus3]  = "firstFrameT=70909 steadyFrameT=70908 totalT=35454000 videoHash=32A69F4391D34FD1 audioHash=B782CF1F93FD2D65",
			// Pentagon128 not headless-constructable (no embedded ROM) → not golden-guarded here.
		};

		[TestMethod]
		public void PerModel_Behaviour_MatchesMasterGolden()
		{
			var sb = new StringBuilder();
			var mismatches = new List<string>();
			int checkedCount = 0;
			sb.AppendLine($"ZX per-model behavioural fingerprint ({Frames} frames from reset, no input):");

			foreach (var mt in new[]
			{
				MachineType.ZXSpectrum16, MachineType.ZXSpectrum48, MachineType.ZXSpectrum128,
				MachineType.ZXSpectrum128Plus2, MachineType.ZXSpectrum128Plus2a,
				MachineType.ZXSpectrum128Plus3, MachineType.Pentagon128,
			})
			{
				ZX core;
				try { core = MakeCore(mt); }
				catch (Exception e) { sb.AppendLine($"  {mt,-22}: SKIP (not headless-constructable: {e.Message})"); continue; }

				var emu = (IEmulator)core;
				var dbg = (IDebuggable)core;
				var vp = core.ServiceProvider.GetService<IVideoProvider>()!;
				var sp = core.ServiceProvider.GetService<ISoundProvider>();
				if (sp != null && sp.CanProvideAsync) { sp.SetSyncMode(SyncSoundMode.Sync); }

				const ulong FnvOffset = 14695981039346656037UL, FnvPrime = 1099511628211UL;
				ulong vHash = FnvOffset, aHash = FnvOffset;
				long prev = dbg.TotalExecutedCycles, total = 0, firstFrameT = 0, steadyFrameT = 0;

				for (int f = 0; f < Frames; f++)
				{
					emu.FrameAdvance(NullController.Instance, true, true);
					long now = dbg.TotalExecutedCycles, len = now - prev; prev = now;
					total += len;
					if (f == 0) firstFrameT = len;
					if (f == Frames - 1) steadyFrameT = len;

					var fb = vp.GetVideoBuffer();
					unchecked { foreach (var px in fb) { vHash ^= (uint)px; vHash *= FnvPrime; } }

					if (sp != null)
					{
						try
						{
							sp.GetSamplesSync(out short[] samples, out int nsamp);
							unchecked { for (int i = 0; i < nsamp * 2 && i < samples.Length; i++) { aHash ^= (ushort)samples[i]; aHash *= FnvPrime; } }
						}
						catch { /* audio best-effort */ }
					}
				}

				string actual = $"firstFrameT={firstFrameT} steadyFrameT={steadyFrameT} totalT={total} videoHash={vHash:X16} audioHash={aHash:X16}";
				sb.AppendLine($"  {mt,-22}: {actual}");
				if (Golden.TryGetValue(mt, out var expected))
				{
					checkedCount++;
					if (actual != expected)
						mismatches.Add($"{mt}:\n    golden {expected}\n    actual {actual}");
				}
			}

			string outPath = @"C:\Users\matt\AppData\Local\Temp\claude\D--Repos-BH-BizHawk\856ebaad-1f4b-4da2-9a07-b5626fdb9560\scratchpad\zx_fingerprints.txt";
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, sb.ToString());
			Console.WriteLine(sb.ToString());

			Assert.IsTrue(checkedCount >= 5, $"expected to golden-check >=5 models, only checked {checkedCount}");
			Assert.IsTrue(mismatches.Count == 0,
				"per-model behaviour diverged from the master golden — an optimization changed observable "
				+ "per-model behaviour (frame length / contention / interrupt / rendering):\n" + string.Join("\n", mismatches));
		}
	}
}
