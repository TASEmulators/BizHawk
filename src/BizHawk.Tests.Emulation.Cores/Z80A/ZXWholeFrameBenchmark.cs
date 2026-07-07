using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using ZX = BizHawk.Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum;

namespace BizHawk.Tests.Emulation.Cores.Z80ATests
{
	/// <summary>
	/// Whole-frame breakdown: how much of a real ZXHawk 48K frame is the CPU vs the rest of the
	/// per-cycle pipeline (ULA/RenderScreen, beepers, tape, contention, virtual memory dispatch).
	/// Instantiates the real 48K core (embedded firmware — no external files) and times
	/// FrameAdvance with rendering on vs off. Excluded from normal runs; run with
	/// --filter "TestCategory=Benchmark". Result written to a temp file + console.
	/// </summary>
	[TestClass]
	public sealed class ZXWholeFrameBenchmark
	{
		private const int TStatesPerFrame48K = 69888; // for the ns/tick comparison to the isolated CPU

		// 48K uses the embedded ROM, so these are never actually called — throw to prove it.
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

		private static ZX MakeCore(MachineType machineType = MachineType.ZXSpectrum48)
		{
			var comm = new CoreComm((_) => { }, (_, _) => { }, new StubFiles(), CoreComm.CorePreferencesFlags.None, new StubGL());
			var lp = new CoreLoadParameters<ZX.ZXSpectrumSettings, ZX.ZXSpectrumSyncSettings>
			{
				Comm = comm,
				Settings = new ZX.ZXSpectrumSettings(),
				SyncSettings = new ZX.ZXSpectrumSyncSettings { MachineType = machineType },
				// Roms left empty → bare boot-to-BASIC (no tape/disk media).
			};
			return new ZX(lp);
		}

		/// <summary>
		/// Correctness smoke test (runs in the normal harness, fast): the 48K core must boot to a
		/// non-blank screen and execute a full ~69888-T-state frame. Guards against the beeper/tape
		/// event-driven changes breaking boot or frame timing.
		/// </summary>
		[TestMethod]
		public void ZX48_BootsAndRunsFullFrame()
		{
			var core = MakeCore();
			var emu = (IEmulator)core;
			var dbg = (IDebuggable)core;

			for (int i = 0; i < 150; i++) emu.FrameAdvance(NullController.Instance, true, true); // boot

			// full frame length: measure T-states advanced over one frame
			long before = dbg.TotalExecutedCycles;
			emu.FrameAdvance(NullController.Instance, true, true);
			long delta = dbg.TotalExecutedCycles - before;
			Assert.IsTrue(delta is > 69000 and < 71000, $"48K frame executed {delta} T-states, expected ~69888");

			// booted to a rendered (non-blank) screen
			var vp = core.ServiceProvider.GetService<IVideoProvider>()!;
			var fb = vp.GetVideoBuffer();
			int first = fb[0];
			bool nonBlank = fb.Any(px => px != first);
			Assert.IsTrue(nonBlank, "framebuffer is uniform — core did not render a screen (boot failed?)");
		}

		/// <summary>
		/// Savestate round-trip guard: save the 48K core mid-run, load into a fresh core, then run both
		/// forward in lockstep — the framebuffers must stay identical. This is the safety net for any
		/// change to the memory system (e.g. a devirtualisation memory-map): if load produces stale/wrong
		/// memory, the continuation diverges and this fails. Runs in the normal harness.
		/// </summary>
		[TestMethod]
		public void ZX48_SavestateRoundTrips()
		{
			var a = MakeCore();
			var ea = (IEmulator)a;
			for (int i = 0; i < 200; i++) ea.FrameAdvance(NullController.Instance, true, false); // reach a non-trivial state

			byte[] state;
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms)) a.ServiceProvider.GetService<IStatable>()!.SaveStateBinary(bw);
				state = ms.ToArray();
			}

			var b = MakeCore();
			var eb = (IEmulator)b;
			using (var ms = new MemoryStream(state))
			using (var br = new BinaryReader(ms))
				b.ServiceProvider.GetService<IStatable>()!.LoadStateBinary(br);

			var vpa = a.ServiceProvider.GetService<IVideoProvider>()!;
			var vpb = b.ServiceProvider.GetService<IVideoProvider>()!;
			for (int i = 0; i < 120; i++)
			{
				ea.FrameAdvance(NullController.Instance, true, false);
				eb.FrameAdvance(NullController.Instance, true, false);
				CollectionAssert.AreEqual(vpa.GetVideoBuffer(), vpb.GetVideoBuffer(),
					$"framebuffer diverged {i + 1} frames after savestate load — save/load is not faithful");
			}
		}

		/// <summary>
		/// Same boot + full-frame check for 128K (which has paging + a rebuilt map). 128K frame is ~70908 T-states.
		/// </summary>
		[TestMethod]
		public void ZX128_BootsAndRunsFullFrame()
		{
			var core = MakeCore(MachineType.ZXSpectrum128);
			var emu = (IEmulator)core;
			var dbg = (IDebuggable)core;

			for (int i = 0; i < 150; i++) emu.FrameAdvance(NullController.Instance, true, true);

			long before = dbg.TotalExecutedCycles;
			emu.FrameAdvance(NullController.Instance, true, true);
			long delta = dbg.TotalExecutedCycles - before;
			Assert.IsTrue(delta is > 70000 and < 72000, $"128K frame executed {delta} T-states, expected ~70908");

			var fb = core.ServiceProvider.GetService<IVideoProvider>()!.GetVideoBuffer();
			Assert.IsTrue(fb.Any(px => px != fb[0]), "128K framebuffer is uniform — boot failed?");
		}

		/// <summary>
		/// Savestate round-trip for 128K — the critical guard for the paged memory-map: the save captures
		/// the paging state, load must rebuild the map from it, and the continuation must stay identical.
		/// If RebuildMemoryMap-on-load is wrong (stale bank refs), this diverges.
		/// </summary>
		[TestMethod]
		public void ZX128_SavestateRoundTrips()
		{
			var a = MakeCore(MachineType.ZXSpectrum128);
			var ea = (IEmulator)a;
			for (int i = 0; i < 250; i++) ea.FrameAdvance(NullController.Instance, true, false); // boot exercises paging

			byte[] state;
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms)) a.ServiceProvider.GetService<IStatable>()!.SaveStateBinary(bw);
				state = ms.ToArray();
			}

			var b = MakeCore(MachineType.ZXSpectrum128);
			var eb = (IEmulator)b;
			using (var ms = new MemoryStream(state))
			using (var br = new BinaryReader(ms))
				b.ServiceProvider.GetService<IStatable>()!.LoadStateBinary(br);

			var vpa = a.ServiceProvider.GetService<IVideoProvider>()!;
			var vpb = b.ServiceProvider.GetService<IVideoProvider>()!;
			for (int i = 0; i < 120; i++)
			{
				ea.FrameAdvance(NullController.Instance, true, false);
				eb.FrameAdvance(NullController.Instance, true, false);
				CollectionAssert.AreEqual(vpa.GetVideoBuffer(), vpb.GetVideoBuffer(),
					$"128K framebuffer diverged {i + 1} frames after savestate load");
			}
		}

		/// <summary>
		/// +2 savestate round-trip. ZX128Plus2 derives from ZX128 with no memory overrides, so it uses
		/// the inherited paged map — verify that inheritance is actually correct end-to-end.
		/// </summary>
		[TestMethod]
		public void ZX128Plus2_SavestateRoundTrips()
		{
			var a = MakeCore(MachineType.ZXSpectrum128Plus2);
			var ea = (IEmulator)a;
			for (int i = 0; i < 250; i++) ea.FrameAdvance(NullController.Instance, true, false);

			byte[] state;
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms)) a.ServiceProvider.GetService<IStatable>()!.SaveStateBinary(bw);
				state = ms.ToArray();
			}

			var b = MakeCore(MachineType.ZXSpectrum128Plus2);
			var eb = (IEmulator)b;
			using (var ms = new MemoryStream(state))
			using (var br = new BinaryReader(ms))
				b.ServiceProvider.GetService<IStatable>()!.LoadStateBinary(br);

			var vpa = a.ServiceProvider.GetService<IVideoProvider>()!;
			var vpb = b.ServiceProvider.GetService<IVideoProvider>()!;
			for (int i = 0; i < 120; i++)
			{
				ea.FrameAdvance(NullController.Instance, true, false);
				eb.FrameAdvance(NullController.Instance, true, false);
				CollectionAssert.AreEqual(vpa.GetVideoBuffer(), vpb.GetVideoBuffer(),
					$"+2 framebuffer diverged {i + 1} frames after savestate load");
			}
		}

		/// <summary>
		/// +2a savestate round-trip. +2a/+3 are NOT devirtualised (their ReadMemory has a contention
		/// side effect the map path would bypass), so they use the fallback (map == null → virtual
		/// ReadMemory). This verifies the CpuLink's ReadMemoryMapped fallback preserves +2a behaviour.
		/// (+3 shares this memory logic AND the embedded ROM, so it builds headlessly too — covered by
		/// ExtraModels_BootAndRoundTrip.)
		/// </summary>
		[TestMethod]
		public void ZX128Plus2a_SavestateRoundTrips()
		{
			var a = MakeCore(MachineType.ZXSpectrum128Plus2a);
			var ea = (IEmulator)a;
			for (int i = 0; i < 250; i++) ea.FrameAdvance(NullController.Instance, true, false);

			byte[] state;
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms)) a.ServiceProvider.GetService<IStatable>()!.SaveStateBinary(bw);
				state = ms.ToArray();
			}

			var b = MakeCore(MachineType.ZXSpectrum128Plus2a);
			var eb = (IEmulator)b;
			using (var ms = new MemoryStream(state))
			using (var br = new BinaryReader(ms))
				b.ServiceProvider.GetService<IStatable>()!.LoadStateBinary(br);

			var vpa = a.ServiceProvider.GetService<IVideoProvider>()!;
			var vpb = b.ServiceProvider.GetService<IVideoProvider>()!;
			for (int i = 0; i < 120; i++)
			{
				ea.FrameAdvance(NullController.Instance, true, false);
				eb.FrameAdvance(NullController.Instance, true, false);
				CollectionAssert.AreEqual(vpa.GetVideoBuffer(), vpb.GetVideoBuffer(),
					$"+2a framebuffer diverged {i + 1} frames after savestate load");
			}
		}

		/// <summary>
		/// Coverage for the models that otherwise only had the construction-time contention guard:
		/// 16K (ZX16 : ZX48 — inherits the 48K CPU/port/beeper but opts OUT of memory-map devirt: regions
		/// 2/3 unmapped, fallback path), +3 (ZX128Plus3 — its own code, shares the embedded +2a ROM so it
		/// DOES build headlessly), and Pentagon128 (its own screen/memory; needs an external ROM, so it
		/// skips). Each constructable model boots to a non-blank screen and savestate round-trips over 120
		/// frames, exercising the shared optimizations (flag-accum CPU, event-driven beeper/tape, Tier-1,
		/// contention precompute) end to end. A model that can't be built headlessly is logged and skipped.
		/// </summary>
		[TestMethod]
		public void ExtraModels_BootAndRoundTrip()
		{
			int verified = 0;
			foreach (var mt in new[] { MachineType.ZXSpectrum16, MachineType.Pentagon128, MachineType.ZXSpectrum128Plus3 })
			{
				ZX a;
				try { a = MakeCore(mt); }
				catch (System.Exception e) { Console.WriteLine($"skip {mt}: not constructable headlessly ({e.Message})"); continue; }

				var ea = (IEmulator)a;
				for (int i = 0; i < 200; i++) ea.FrameAdvance(NullController.Instance, true, false);
				var fb = a.ServiceProvider.GetService<IVideoProvider>()!.GetVideoBuffer();
				Assert.IsTrue(fb.Any(px => px != fb[0]), $"{mt} framebuffer uniform — boot failed?");

				byte[] state;
				using (var ms = new MemoryStream())
				{
					using (var bw = new BinaryWriter(ms)) a.ServiceProvider.GetService<IStatable>()!.SaveStateBinary(bw);
					state = ms.ToArray();
				}
				var b = MakeCore(mt);
				var eb = (IEmulator)b;
				using (var ms = new MemoryStream(state))
				using (var br = new BinaryReader(ms))
					b.ServiceProvider.GetService<IStatable>()!.LoadStateBinary(br);

				var vpa = a.ServiceProvider.GetService<IVideoProvider>()!;
				var vpb = b.ServiceProvider.GetService<IVideoProvider>()!;
				for (int i = 0; i < 120; i++)
				{
					ea.FrameAdvance(NullController.Instance, true, false);
					eb.FrameAdvance(NullController.Instance, true, false);
					CollectionAssert.AreEqual(vpa.GetVideoBuffer(), vpb.GetVideoBuffer(),
						$"{mt} framebuffer diverged {i + 1} frames after savestate load");
				}
				verified++;
			}
			if (verified == 0) Assert.Inconclusive("neither 16K nor Pentagon128 was constructable headlessly");
		}

		/// <summary>
		/// Validation harness for the event-driven display (increment 1/2 of the reuse-opcodes hybrid): run
		/// two identical 48K cores — one per-cycle (default), one with EventDrivenDisplay on — in lockstep
		/// and assert their framebuffers are identical every frame. Both cores are otherwise deterministic
		/// and receive identical (null) input, so ANY divergence isolates an event-driven rendering bug.
		/// NOTE: boot-to-BASIC exercises the initial screen clear, the copyright text, the flashing cursor,
		/// and border writes — but NOT beam-racing (mid-scanline screen writes chasing the raster). Full
		/// coverage of those effects needs a dedicated test program (TODO) before trusting 128K/effects.
		/// </summary>
		[TestMethod]
		public void EventDrivenDisplay_MatchesPerCycle()
		{
			var refCore = MakeCore();                    // per-cycle (EventDrivenDisplay = false)
			var edCore = MakeCore();
			var edMachine = (SpectrumBase)edCore.GetType()
				.GetField("_machine", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(edCore)!;
			edMachine.EventDrivenDisplay = true;

			var er = (IEmulator)refCore;
			var ee = (IEmulator)edCore;
			var vr = refCore.ServiceProvider.GetService<IVideoProvider>()!;
			var ve = edCore.ServiceProvider.GetService<IVideoProvider>()!;

			for (int i = 0; i < 300; i++)
			{
				er.FrameAdvance(NullController.Instance, true, false);
				ee.FrameAdvance(NullController.Instance, true, false);
				CollectionAssert.AreEqual(vr.GetVideoBuffer(), ve.GetVideoBuffer(),
					$"event-driven display diverged from per-cycle at frame {i + 1}");
			}
		}

		/// <summary>
		/// Guard for the contention-decode precompute (before any ExecuteCycle change trusts it): the flat
		/// ContentionByCycle table must equal the RenderCycle source-of-truth for every frame T-state, and
		/// PageContended must equal IsContended for every 16K page across every paging state (the
		/// 128K-family high bank's contention depends on RAMPaged). Runs in the normal harness.
		/// </summary>
		[TestMethod]
		public void ContentionPrecompute_MatchesSourceOfTruth()
		{
			int tested = 0;
			foreach (var mt in new[]
			{
				MachineType.ZXSpectrum16, MachineType.ZXSpectrum48, MachineType.ZXSpectrum128,
				MachineType.ZXSpectrum128Plus2, MachineType.ZXSpectrum128Plus2a, MachineType.Pentagon128,
			})
			{
				ZX core;
				try { core = MakeCore(mt); }
				catch { Console.WriteLine($"skip {mt}: not constructable headlessly (no embedded ROM)"); continue; }
				tested++;

				var machine = (SpectrumBase)core.GetType()
					.GetField("_machine", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(core)!;

				// (a) flat ContentionByCycle == RenderCycle.ContentionValue for every frame T-state
				var rt = machine.ULADevice.RenderingTable;
				Assert.AreEqual(rt.Renderer.Length, rt.ContentionByCycle.Length, $"{mt}: flat table length mismatch");
				for (int t = 0; t < rt.Renderer.Length; t++)
					Assert.AreEqual(rt.Renderer[t].ContentionValue, rt.ContentionByCycle[t],
						$"{mt}: ContentionByCycle[{t}] != Renderer[{t}].ContentionValue");

				// (b) PageContended == IsContended as built at construction
				for (int p = 0; p < 4; p++)
					Assert.AreEqual(machine.IsContended((ushort)(p << 14)), machine.PageContended[p],
						$"{mt}: PageContended[{p}] != IsContended at construction");

				// (c) PageContended is correctly rebuilt for every paging state (16K/48K are invariant,
				//     which this also confirms; 128K/+2/+2a/Pentagon high bank depends on RAMPaged)
				for (int rp = 0; rp < 8; rp++)
				{
					machine.RAMPaged = rp;
					machine.RebuildMemoryMap();
					for (int p = 0; p < 4; p++)
						Assert.AreEqual(machine.IsContended((ushort)(p << 14)), machine.PageContended[p],
							$"{mt}: PageContended[{p}] != IsContended with RAMPaged={rp}");
				}
			}
			Assert.IsTrue(tested >= 2, "constructed too few machines to be a meaningful guard");
		}

		/// <summary>
		/// In-session A/B for the 48K memory-map devirtualisation: toggles the map on (devirt) vs off
		/// (null → falls back to the virtual ReadMemory) on the SAME core, interleaved per round, and
		/// compares. Because both are measured in the same session, common-mode machine noise cancels
		/// (the whole-frame absolute ns/tick drifts ±30-40% between sessions, so cross-run comparison
		/// is unreliable — this is the trustworthy measure). ratio off/on > 1 means devirt is faster.
		/// </summary>
		[TestMethod]
		[TestCategory("Benchmark")]
		public void WholeFrame_DevirtAB()
		{
			try
			{
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
				Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x2;
			}
			catch { }

			var core = MakeCore();
			var emu = (IEmulator)core;
			var machine = core.GetType().GetField("_machine", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(core)!;
			var readMapF = typeof(SpectrumBase).GetField("_readMap", BindingFlags.NonPublic | BindingFlags.Instance)!;
			var writeMapF = typeof(SpectrumBase).GetField("_writeMap", BindingFlags.NonPublic | BindingFlags.Instance)!;
			var rebuild = machine.GetType().GetMethod("RebuildMemoryMap")!;
			void MapOn() => rebuild.Invoke(machine, null);
			void MapOff() { readMapF.SetValue(machine, null); writeMapF.SetValue(machine, null); }

			MapOn();
			for (int i = 0; i < 300; i++) emu.FrameAdvance(NullController.Instance, true, false); // warm + boot

			const int Frames = 1200;
			double TimeFrames()
			{
				var sw = Stopwatch.StartNew();
				for (int i = 0; i < Frames; i++) emu.FrameAdvance(NullController.Instance, true, false);
				sw.Stop();
				return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / Frames / TStatesPerFrame48K;
			}
			double Median(List<double> xs) { var s = xs.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

			var on = new List<double>();
			var off = new List<double>();
			for (int r = 0; r < 7; r++)
			{
				MapOn(); on.Add(TimeFrames());
				MapOff(); off.Add(TimeFrames());
			}
			MapOn();
			double medOn = Median(on), medOff = Median(off);

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"48K memory-map devirt A/B (ns/T-state, median of {on.Count} rounds x {Frames} frames, same session):");
			sb.AppendLine($"  devirt ON  (map)     : {medOn:F2}");
			sb.AppendLine($"  devirt OFF (virtual) : {medOff:F2}");
			sb.AppendLine($"  ratio off/on         : {medOff / medOn:F4}  ({(medOff / medOn - 1) * 100:+0.0;-0.0}% : devirt is this much faster than virtual)");

			string outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zx_devirt_ab.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, sb.ToString());
			Console.WriteLine(sb.ToString());
		}

		[TestMethod]
		[TestCategory("Benchmark")]
		public void WholeFrame_Breakdown()
		{
			try
			{
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
				Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x2;
			}
			catch { }

			var core = MakeCore();
			var emu = (IEmulator)core;

			// warm up (also runs the boot sequence into steady state)
			for (int i = 0; i < 300; i++) emu.FrameAdvance(NullController.Instance, true, false);

			double Median(List<double> xs) { var s = xs.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
			double TimeFrames(bool render, bool sound, int frames)
			{
				var sw = Stopwatch.StartNew();
				for (int i = 0; i < frames; i++) emu.FrameAdvance(NullController.Instance, render, sound);
				sw.Stop();
				return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / frames; // ns/frame
			}

			const int Frames = 1500;
			var on = new List<double>();
			var off = new List<double>();
			var full = new List<double>();
			for (int r = 0; r < 7; r++)
			{
				on.Add(TimeFrames(render: true, sound: false, Frames));
				off.Add(TimeFrames(render: false, sound: false, Frames));
				full.Add(TimeFrames(render: true, sound: true, Frames));
			}
			double medOn = Median(on), medOff = Median(off), medFull = Median(full);
			double renderCost = medOn - medOff;

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"ZXHawk 48K whole-frame (median of {on.Count} rounds x {Frames} frames):");
			sb.AppendLine($"  render OFF, sound OFF : {medOff / 1000.0:F1} us/frame  ({medOff / TStatesPerFrame48K:F2} ns/T-state)");
			sb.AppendLine($"  render ON,  sound OFF : {medOn / 1000.0:F1} us/frame  ({medOn / TStatesPerFrame48K:F2} ns/T-state)");
			sb.AppendLine($"  render ON,  sound ON  : {medFull / 1000.0:F1} us/frame  ({medFull / TStatesPerFrame48K:F2} ns/T-state)");
			sb.AppendLine($"  => RenderScreen (per-cycle video) : ~{renderCost / 1000.0:F1} us/frame  ({100.0 * renderCost / medOn:F0}% of render-on frame)");
			sb.AppendLine($"  => sound render (frame-level)      : ~{(medFull - medOn) / 1000.0:F1} us/frame");
			sb.AppendLine($"  For reference, isolated Z80AOpt CPU ~= 39 ns/T-state (fake memory, no contention/dispatch).");
			sb.AppendLine($"  So non-CPU per-cycle overhead ~= ({medOff / TStatesPerFrame48K:F0} - 39) ns/T-state in the render-off frame,");
			sb.AppendLine($"  plus ~{renderCost / TStatesPerFrame48K:F0} ns/T-state for RenderScreen when rendering.");

			string outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zx_frame.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, sb.ToString());
			Console.WriteLine(sb.ToString());
		}

		/// <summary>
		/// Per-cycle component PROFILE, all in ONE session on the real booted 48K machine (real ROM code,
		/// so no random-instruction-mix distortion). The machine exposes two public entry points that
		/// differ by exactly the non-CPU per-cycle work:
		///   CPU.ExecuteOne()      = CPU core + memory dispatch (one T-state), NO ULA, NO contention.
		///   CPUMon.ExecuteCycle() = the same ExecuteOne PLUS ULA.CycleClock + the contention decode.
		/// Driving each in a bare loop from the same warm state and subtracting isolates ULA+contention
		/// cleanly (both run identical CPU work per call). Toggling the memory map on/off during the
		/// ExecuteOne loop isolates the virtual-dispatch penalty the devirt map removes. RenderScreen is
		/// measured at frame level (it only fires from CycleClock when _render is set). All medians,
		/// same session => the subtractions are valid (common-mode drift cancels).
		/// </summary>
		[TestMethod]
		[TestCategory("Benchmark")]
		public void WholeFrame_Profile()
		{
			try
			{
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
				Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x2;
			}
			catch { }

			double Median(List<double> xs) { var s = xs.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

			var core = MakeCore();
			var emu = (IEmulator)core;
			var machineObj = core.GetType().GetField("_machine", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(core)!;
			var machine = (SpectrumBase)machineObj;
			var readMapF = typeof(SpectrumBase).GetField("_readMap", BindingFlags.NonPublic | BindingFlags.Instance)!;
			var writeMapF = typeof(SpectrumBase).GetField("_writeMap", BindingFlags.NonPublic | BindingFlags.Instance)!;
			void MapOn() => machine.RebuildMemoryMap();
			void MapOff() { readMapF.SetValue(machine, null); writeMapF.SetValue(machine, null); }

			// boot to a steady idle state (keyboard-scan loop) so the bare loops run representative code
			MapOn();
			for (int i = 0; i < 300; i++) emu.FrameAdvance(NullController.Instance, false, false);

			var cpu = machine.CPU;

			const int Steps = 15_000_000;

			// CPU-only: bare ExecuteOne loop = one CPU T-state per call plus its memory dispatch, with NO
			// ULA clock and NO contention. Pure CPU. ExecuteOne indexes no per-frame table, so it can
			// free-run unbounded safely (it just executes whatever code is at PC — the idle ROM loop).
			double TimeCpuOnly()
			{
				var sw = Stopwatch.StartNew();
				for (int i = 0; i < Steps; i++) cpu.ExecuteOne();
				sw.Stop();
				return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / Steps; // ns per T-state
			}

			// (1) CPU-only rounds on this core, map on vs off (dispatch penalty). These free-run the CPU
			//     and corrupt the frame counters — fine for ExecuteOne, but this core can't be frame-run
			//     afterwards, so the full-pipeline timing uses a FRESH core below.
			for (int i = 0; i < 3_000_000; i++) cpu.ExecuteOne(); // warm
			var cpuOn = new List<double>();
			var cpuOff = new List<double>();
			for (int r = 0; r < 7; r++)
			{
				MapOn(); cpuOn.Add(TimeCpuOnly());
				MapOff(); cpuOff.Add(TimeCpuOnly());
			}
			double cpuOnly = Median(cpuOn);
			double cpuOnlyNoMap = Median(cpuOff);

			// (2) full per-cycle pipeline via the real frame loop on a FRESH core (clean frame cadence —
			//     ExecuteFrame manages all ULA/contention counters correctly, no hand-rolled bookkeeping).
			//     Same process/session as the CPU rounds, so the numbers are comparable.
			var core2 = MakeCore();
			var emu2 = (IEmulator)core2;
			for (int i = 0; i < 300; i++) emu2.FrameAdvance(NullController.Instance, true, false); // boot + warm
			const int Frames = 1200;
			double TimeFrame(bool render)
			{
				var sw = Stopwatch.StartNew();
				for (int i = 0; i < Frames; i++) emu2.FrameAdvance(NullController.Instance, render, false);
				sw.Stop();
				return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / Frames / TStatesPerFrame48K; // ns per frame T-state
			}
			var fOn = new List<double>();
			var fOff = new List<double>();
			for (int r = 0; r < 7; r++) { fOn.Add(TimeFrame(true)); fOff.Add(TimeFrame(false)); }
			double frameOn = Median(fOn), frameOff = Median(fOff);

			double nonCpu = frameOff - cpuOnly;              // ULA clock + contention decode + loop overhead
			double dispatchPenalty = cpuOnlyNoMap - cpuOnly; // virtual ReadMemory vs devirt map
			double render = frameOn - frameOff;              // RenderScreen (per-cycle video)

			var sb2 = new System.Text.StringBuilder();
			sb2.AppendLine("ZXHawk 48K per-cycle PROFILE (ns/T-state, median of 7 rounds, same session, real ROM code):");
			sb2.AppendLine($"  [A] CPU core + memory dispatch (bare ExecuteOne, map on) : {cpuOnly:F2}  ns/T");
			sb2.AppendLine($"  [B] full per-cycle pipeline    (frame, render off)       : {frameOff:F2}  ns/T");
			sb2.AppendLine($"      => non-CPU (B - A): ULA clock + contention decode + loop : {nonCpu:F2}  ns/T  ({100.0 * nonCpu / frameOff:F0}%)");
			sb2.AppendLine($"      => CPU + dispatch share (A / B)                          : {100.0 * cpuOnly / frameOff:F0}%");
			sb2.AppendLine($"  virtual-dispatch penalty if devirt OFF (map off - on)    : +{dispatchPenalty:F2}  ns/T  (what the map devirt saves)");
			sb2.AppendLine($"  RenderScreen (video, frame render on - off)              : {render:F2}  ns/T");
			sb2.AppendLine($"  frame render ON total                                    : {frameOn:F2}  ns/T");
			sb2.AppendLine("  NOTE: [A] is ns per ExecuteOne call; [B] is ns per frame T-state. Frame T-states include");
			sb2.AppendLine("  contention-stretch cycles that cost no call, so [B] slightly understates the true per-call");
			sb2.AppendLine("  pipeline cost and (B-A) slightly understates ULA+contention. Good for shares, not exact.");

			string outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zx_profile.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, sb2.ToString());
			Console.WriteLine(sb2.ToString());
		}
	}
}
