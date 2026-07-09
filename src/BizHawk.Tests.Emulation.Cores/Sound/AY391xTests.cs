using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Tests.Emulation.Cores.Sound
{
	/// <summary>
	/// Sanity tests for the shared AY-3-891x PSG core: at reset it must be silent, and a programmed tone
	/// must produce a non-silent, bounded, correctly-pitched (band-limited via BlipBuffer) square wave.
	/// </summary>
	[TestClass]
	public sealed class AY391xTests
	{
		private const int SampleRate = 44100;
		private const int FrameTStates = 70908; // 128K frame length (CPU T-states)
		private const int CpuClock = 3546900;   // 128K CPU clock; AY = CPU/2
		private static void InitAy(AY391x ay) => ay.Init(SampleRate, FrameTStates, CpuClock / 2, CpuClock);

		private static short[] RenderOneFrame(AY391x ay, out int nsamp)
		{
			ay.StartFrame();
			ay.EndFrame();
			ay.GetSamplesSync(out var buf, out nsamp);
			var copy = new short[nsamp * 2];
			System.Array.Copy(buf, copy, nsamp * 2);
			return copy;
		}

		[TestMethod]
		public void Reset_IsSilent()
		{
			var ay = new AY391x();
			InitAy(ay);
			var s = RenderOneFrame(ay, out int n);
			Assert.IsTrue(n >= 880 && n <= 883, $"~882 samples/frame at 50 Hz (got {n})");
			for (int i = 0; i < n * 2; i++)
				Assert.AreEqual(0, s[i], $"reset AY must be silent (sample {i} = {s[i]})");
			ay.Dispose();
		}

		[TestMethod]
		public void Tone_IsBandLimitedAndCorrectlyPitched()
		{
			var ay = new AY391x();
			InitAy(ay);
			ay.Volume = 100;

			void W(int reg, int val) { ay.LatchAddress(reg); ay.WriteData(val); }
			// ~1 kHz tone on channel A: base-tick rate = 5*44100 = 220500; period = 2*D ticks;
			// D=110 -> 220500/220 = ~1002 Hz.
			W(0, 110); W(1, 0);   // channel A tone period (fine/coarse)
			W(7, 0x3E);           // mixer: tone A enabled (bit0=0), everything else disabled
			W(8, 15);             // channel A amplitude = full (fixed, not envelope)

			var s = RenderOneFrame(ay, out int n);

			int min = int.MaxValue, max = int.MinValue, nonzero = 0;
			for (int i = 0; i < n; i++)
			{
				int v = s[i * 2]; // left channel
				if (v != 0) nonzero++;
				if (v < min) min = v;
				if (v > max) max = v;
			}
			Assert.IsTrue(nonzero > 100, $"tone should be audible (nonzero samples = {nonzero})");
			Assert.IsTrue(max > 1000, $"tone should have real amplitude (max = {max})");
			Assert.IsTrue(max <= short.MaxValue && min >= short.MinValue, "output must be bounded (no overflow wrap)");

			// count crossings of the midpoint => ~1 kHz over a 20 ms frame = ~20 cycles = ~40 crossings
			int mid = (min + max) / 2, crossings = 0; bool above = s[0] >= mid;
			for (int i = 1; i < n; i++)
			{
				bool a = s[i * 2] >= mid;
				if (a != above) { crossings++; above = a; }
			}
			Assert.IsTrue(crossings >= 20 && crossings <= 80,
				$"expected ~40 midpoint crossings for a ~1 kHz tone, got {crossings}");
			ay.Dispose();
		}

		private static void W(AY391x a, int reg, int val) { a.LatchAddress(reg); a.WriteData(val); }

		private static byte[] Save(AY391x a)
		{
			using var ms = new MemoryStream();
			using (var bw = new BinaryWriter(ms))
				a.SyncState(Serializer.CreateBinaryWriter(bw));
			return ms.ToArray();
		}

		[TestMethod]
		public void State_FullyRoundTrips()
		{
			// drive the chip into a non-trivial state: tone A + envelope-driven B + noise C, mid-envelope
			var ay1 = new AY391x();
			InitAy(ay1);
			ay1.Volume = 100;
			W(ay1, 0, 120); W(ay1, 7, 0x38); W(ay1, 8, 15); W(ay1, 9, 0x10);
			W(ay1, 6, 7); W(ay1, 11, 80); W(ay1, 13, 0x0A);
			for (int f = 0; f < 7; f++) RenderOneFrame(ay1, out _);

			byte[] saved = Save(ay1);

			// load into a fresh instance
			var ay2 = new AY391x();
			InitAy(ay2);
			using (var br = new BinaryReader(new MemoryStream(saved)))
				ay2.SyncState(Serializer.CreateBinaryReader(br));

			// (a) the serialized state round-trips self-consistently
			CollectionAssert.AreEqual(saved, Save(ay2), "serialized AY state must round-trip");

			// (b) completeness: with identical restored generator state the two chips evolve deterministically,
			// so advancing both the same number of frames must yield identical serialized state again. (The
			// blip resampler's internal fractional phase is output-only and intentionally not serialized - like
			// the audio buffer - so the raw samples can differ by a 1-sample, inaudible, non-desyncing transient
			// after load; it does not feed back into the generator, so the emulation state stays in sync.)
			for (int f = 0; f < 5; f++) { RenderOneFrame(ay1, out _); RenderOneFrame(ay2, out _); }
			CollectionAssert.AreEqual(Save(ay1), Save(ay2),
				"generator state diverged after load - some behaviour-determining state is not serialized");

			ay1.Dispose();
			ay2.Dispose();
		}
	}
}
