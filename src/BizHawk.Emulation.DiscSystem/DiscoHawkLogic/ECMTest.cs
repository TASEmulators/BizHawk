using System.Collections.Generic;
using System.Diagnostics;

namespace BizHawk.Emulation.DiscSystem
{
	public static class ECMTest
	{
		[Conditional("FALSE")]
		public static void TestMain()
		{
			static void Shuffle<T>(IList<T> list, Random rng)
			{
				int n = list.Count;
				while (n > 1)
				{
					n--;
					int k = rng.Next(n + 1);
					T value = list[k];
					list[k] = list[n];
					list[n] = value;
				}
			}

			var plaindisc = Disc.LoadAutomagic("d:\\ecmtest\\test.cue");
			var ecmdisc = Disc.LoadAutomagic("d:\\ecmtest\\ecmtest.cue");

//			var prefs = new CueBinPrefs
//			{
//				AnnotateCue = false,
//				OneBlobPerTrack = false,
//				ReallyDumpBin = true,
//				SingleSession = true,
//				DumpToBitbucket = true
//			};
//			var dump = ecmdisc.DumpCueBin("test", prefs);
//			dump.Dump("test", prefs);

//			var prefs = new CueBinPrefs
//			{
//				AnnotateCue = false,
//				OneBlobPerTrack = false,
//				ReallyDumpBin = true,
//				SingleSession = true
//			};
//			var dump = ecmdisc.DumpCueBin("test", prefs);
//			dump.Dump(@"D:\ecmtest\myout", prefs);

			int seed = 102;

			for (; ; )
			{
				Console.WriteLine("running seed {0}", seed);
				Random r = new Random(seed);
				seed++;

				byte[] chunkbuf_corlet = new byte[2352 * 20];
				byte[] chunkbuf_mine = new byte[2352 * 20];
//				int length = (ecmdisc._Sectors.Count - 150) * 2352; // API has changed
				var length = 0;
				int counter = 0;
				List<Tuple<int, int>> testChunks = new List<Tuple<int, int>>();
				while (counter < length)
				{
					int chunk = r.Next(1, 2352 * 20);
					if (r.Next(20) == 0)
						chunk /= 100;
					if (r.Next(40) == 0)
						chunk = 0;
					if (counter + chunk > length)
						chunk = length - counter;
					testChunks.Add(new Tuple<int, int>(counter, chunk));
					counter += chunk;
				}
				Shuffle(testChunks, r);

				for (int t = 0; t < testChunks.Count; t++)
				{
					//Console.WriteLine("skank");
					var item = testChunks[t];
					//Console.WriteLine("chunk {0} of {3} is {1} bytes @ {2:X8}", t, item.Item2, item.Item1, testChunks.Count);
//					plaindisc.ReadLBA_2352_Flat(item.Item1, chunkbuf_corlet, 0, item.Item2); // API has changed
//					ecmdisc.ReadLBA_2352_Flat(item.Item1, chunkbuf_mine, 0, item.Item2); // API has changed
					for (int i = 0; i < item.Item2; i++)
					{
						Debug.Assert(chunkbuf_corlet[i] == chunkbuf_mine[i], $"buffers differ at [{t}; {i}]");
					}
				}
			}
		}
	}
}
