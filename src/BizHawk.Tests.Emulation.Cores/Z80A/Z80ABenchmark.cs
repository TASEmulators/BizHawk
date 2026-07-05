using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using RefZ80 = BizHawk.Emulation.Cores.Components.Z80A.Z80A<BizHawk.Tests.Emulation.Cores.Z80ATests.TestLink>;
using NewZ80 = BizHawk.Emulation.Cores.Components.Z80AOpt.Z80AOpt<BizHawk.Tests.Emulation.Cores.Z80ATests.TestLink>;

namespace BizHawk.Tests.Emulation.Cores.Z80ATests
{
	/// <summary>
	/// Directional micro-benchmark: times the reference Z80A vs the fork Z80AOpt over an identical
	/// fixed pseudo-random program. Not rigorous (single process, no isolation), but stable enough
	/// to see whether a Z80AOpt optimisation actually moves the needle. Excluded from the normal run;
	/// invoke with: dotnet test ... --filter "TestCategory=Benchmark".
	/// Result is written to the scratchpad file below and to the console.
	/// </summary>
	[TestClass]
	public sealed class Z80ABenchmark
	{
		private const int Warmup = 5_000_000;
		private const int Measure = 60_000_000;

		[TestMethod]
		[TestCategory("Benchmark")]
		public void CompareThroughput()
		{
			// Reduce scheduling/migration noise: high priority, pinned to a single CPU.
			try
			{
				System.Diagnostics.Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
				System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity = (System.IntPtr)0x2; // CPU #1
			}
			catch { /* best-effort */ }

			var prog = new byte[0x10000];
			new System.Random(0x2C0FFEE).NextBytes(prog);
			for (int i = 0; i < prog.Length; i++) if (prog[i] == 0x76) prog[i] = 0x00; // avoid HALT idling

			// Warm up both thoroughly, then run many rounds and take the MEDIAN (robust to outliers).
			// Compare via the per-round new/ref ratio, which cancels common-mode machine noise.
			TimeRef(prog, Warmup, Measure); TimeNew(prog, Warmup, Measure);
			var sb = new System.Text.StringBuilder();
			var refs = new List<double>();
			var news = new List<double>();
			var ratios = new List<double>();
			for (int round = 0; round < 9; round++)
			{
				double r = TimeRef(prog, 0, Measure);
				double n = TimeNew(prog, 0, Measure);
				refs.Add(r); news.Add(n); ratios.Add(n / r);
				sb.AppendLine($"round {round}: ref={r:F3} ns/cyc  new={n:F3} ns/cyc  ratio={n / r:F4}");
			}
			double Median(List<double> xs) { var s = xs.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
			double refMed = Median(refs), newMed = Median(news), ratioMed = Median(ratios);
			string msg = sb.ToString() +
				$"\nMEDIAN Z80A (reference): {refMed:F3} ns/cycle\n" +
				$"MEDIAN Z80AOpt (fork)    : {newMed:F3} ns/cycle\n" +
				$"MEDIAN fork/ref ratio  : {ratioMed:F4}  ({(1 - ratioMed) * 100:+0.0;-0.0}% vs reference)";

			string outPath = @"C:\Users\matt\AppData\Local\Temp\claude\D--Repos-BH-BizHawk\856ebaad-1f4b-4da2-9a07-b5626fdb9560\scratchpad\z80_bench.txt";
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, msg);
			System.Console.WriteLine(msg);
		}

		/// <summary>
		/// Raw per-T-state throughput: managed Z80AOpt vs the native floooh Z80 (LibFz80Wrapper) driven
		/// exactly as the CPC core drives it — one LibFz80_Tick P/Invoke per clock. NO contention here
		/// (best case for the native path); real ZX contention would add per-tick managed work on top.
		/// If native isn't faster even in this best case, it can't win once contention is added.
		/// </summary>
		[TestMethod]
		[TestCategory("Benchmark")]
		public void NativeVsManaged_Throughput()
		{
			try
			{
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
				Process.GetCurrentProcess().ProcessorAffinity = (System.IntPtr)0x2;
			}
			catch { }

			var prog = new byte[0x10000];
			new System.Random(0x2C0FFEE).NextBytes(prog);
			for (int i = 0; i < prog.Length; i++) if (prog[i] == 0x76) prog[i] = 0x00;

			TimeNew(prog, Warmup, Measure); TimeNative(prog, Warmup, Measure); // warm both
			var sb = new System.Text.StringBuilder();
			var managed = new List<double>();
			var native = new List<double>();
			var ratios = new List<double>();
			for (int round = 0; round < 9; round++)
			{
				double m = TimeNew(prog, 0, Measure);
				double nat = TimeNative(prog, 0, Measure);
				managed.Add(m); native.Add(nat); ratios.Add(nat / m);
				sb.AppendLine($"round {round}: managed(Z80AOpt)={m:F3}  native(floooh)={nat:F3}  native/managed={nat / m:F4}");
			}
			double Median(List<double> xs) { var s = xs.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
			string msg = sb.ToString() +
				$"\nMEDIAN managed Z80AOpt   : {Median(managed):F3} ns/cycle\n" +
				$"MEDIAN native floooh   : {Median(native):F3} ns/cycle\n" +
				$"MEDIAN native/managed  : {Median(ratios):F4}  (>1 means native is SLOWER per tick)";

			string outPath = @"C:\Users\matt\AppData\Local\Temp\claude\D--Repos-BH-BizHawk\856ebaad-1f4b-4da2-9a07-b5626fdb9560\scratchpad\z80_native.txt";
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllText(outPath, msg);
			System.Console.WriteLine(msg);
		}

		private static double TimeNative(byte[] prog, int warmup, int measure)
		{
			var mem = new byte[0x10000];
			System.Array.Copy(prog, mem, prog.Length);
			var cpu = new BizHawk.Emulation.Cores.Computers.AmstradCPC.LibFz80Wrapper
			{
				ReadMemory = a => mem[a],
				WriteMemory = (a, v) => mem[a] = v,
				ReadPort = a => (byte)(a >> 8),
				WritePort = (a, v) => { },
			};
			cpu.AttachIRQACKOnCallback(() => { });
			for (int i = 0; i < warmup; i++) cpu.ExecuteOne();
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < measure; i++) cpu.ExecuteOne();
			sw.Stop();
			return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / measure;
		}

		private static double TimeRef(byte[] prog, int warmup, int measure)
		{
			var bus = new TestBus { Record = false };
			System.Array.Copy(prog, bus.Mem, prog.Length);
			var cpu = new RefZ80(new TestLink(bus));
			for (int i = 0; i < warmup; i++) cpu.ExecuteOne();
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < measure; i++) cpu.ExecuteOne();
			sw.Stop();
			return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / measure;
		}

		private static double TimeNew(byte[] prog, int warmup, int measure)
		{
			var bus = new TestBus { Record = false };
			System.Array.Copy(prog, bus.Mem, prog.Length);
			var cpu = new NewZ80(new TestLink(bus));
			for (int i = 0; i < warmup; i++) cpu.ExecuteOne();
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < measure; i++) cpu.ExecuteOne();
			sw.Stop();
			return sw.Elapsed.TotalMilliseconds * 1_000_000.0 / measure;
		}
	}
}
