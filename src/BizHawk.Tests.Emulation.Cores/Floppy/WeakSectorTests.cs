using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Weak/fuzzy sector behaviour: a weak sector reads unpredictably (so copy-protection checks see variation),
	/// yet the read sequence is fully deterministic from the <see cref="WeakBitRng"/> state, so it replays
	/// identically across savestate/TAS load.
	/// </summary>
	[TestClass]
	public sealed class WeakSectorTests
	{
		// a track with one sector whose two recorded copies differ -> the differing bytes are weak
		private static MfmTrack WeakTrack()
		{
			var a = new byte[512];
			var b = new byte[512];
			for (int i = 0; i < 512; i++) { a[i] = 0x11; b[i] = 0x11; }
			for (int i = 100; i < 116; i++) b[i] = 0xEE; // 16 bytes disagree => weak region
			return StandardMfmFormat.BuildStandardTrack(new List<TrackSector>
			{
				new TrackSector { C = 0, H = 0, R = 1, N = 2, Data = a, WeakCopies = new[] { a, b } },
			});
		}

		private static byte[] ReadSectorData(MfmTrack t, WeakBitRng rng)
		{
			foreach (var s in StandardMfmFormat.DecodeSectors(t, rng))
				if (s.R == 1) return s.Data;
			return null;
		}

		[TestMethod]
		public void WeakSector_ReadsVaryButAreDeterministicFromState()
		{
			var t = WeakTrack();

			// same seed -> identical sequence (deterministic)
			var read1 = ReadSectorData(t, new WeakBitRng(0));
			var read1b = ReadSectorData(t, new WeakBitRng(0));
			CollectionAssert.AreEqual(read1, read1b, "same RNG state yields the same read (deterministic)");

			// successive reads on one advancing RNG vary (what a protection check looks for)
			var rng = new WeakBitRng(0);
			var passA = ReadSectorData(t, rng);
			ulong midState = rng.State;      // <- the state a savestate would capture here
			var passB = ReadSectorData(t, rng);
			CollectionAssert.AreNotEqual(passA, passB, "a weak sector reads differently on successive passes");

			// restoring the captured state reproduces the very next pass exactly (savestate replay)
			var restored = new WeakBitRng();
			restored.State = midState;
			var passB2 = ReadSectorData(t, restored);
			CollectionAssert.AreEqual(passB, passB2, "restoring WeakRng.State replays the next read identically");
		}

		[TestMethod]
		public void WeakSector_OnlyWeakBytesVary()
		{
			var t = WeakTrack();
			var r1 = ReadSectorData(t, new WeakBitRng(1));
			var r2 = ReadSectorData(t, new WeakBitRng(0x1234_5678));
			// bytes outside the weak region are stable across any RNG; only 100..115 may differ
			for (int i = 0; i < 512; i++)
				if (i < 100 || i >= 116)
					Assert.AreEqual(r1[i], r2[i], $"non-weak byte {i} must be stable");
		}
	}
}
