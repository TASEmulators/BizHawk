using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using BizHawk.Emulation.DiscSystem;

// cue format preferences notes

// PCEjin -
// does not like session commands
// it can handle binpercue
// it seems not to be able to handle binpertrack, or maybe i am doing something wrong (still haven't ruled it out)

namespace BizHawk.Client.DiscoHawk
{
	static class Program
	{
		static Program()
		{
#if WINDOWS
			// http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
			// this will look in subdirectory "dll" to load pinvoked stuff
			string dllDir = Path.Combine(GetExeDirectoryAbsolute(), "dll");
			SetDllDirectory(dllDir);

			// in case assembly resolution fails, such as if we moved them into the dll subdirectory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			// but before we even try doing that, whack the MOTW from everything in that directory (that's a dll)
			// otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
			// some people are getting MOTW through a combination of browser used to download BizHawk, and program used to dearchive it
			WhackAllMOTW(dllDir);
#endif
		}

		[STAThread]
		static void Main(string[] args)
		{
			SubMain(args);
		}

		// NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which haven't been setup by the resolver yet... or something like that
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, ChangeWindowMessageFilterExAction action, ref CHANGEFILTERSTRUCT changeInfo);

		private static class Win32
		{
			[DllImport("kernel32.dll")]
			public static extern IntPtr LoadLibrary(string dllToLoad);
			[DllImport("kernel32.dll")]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
			[DllImport("kernel32.dll")]
			public static extern bool FreeLibrary(IntPtr hModule);
		}

		static void SubMain(string[] args)
		{
			// MICROSOFT BROKE DRAG AND DROP IN WINDOWS 7. IT DOESN'T WORK ANYMORE
			// WELL, OBVIOUSLY IT DOES SOMETIMES. I DON'T REMEMBER THE DETAILS OR WHY WE HAD TO DO THIS SHIT
#if WINDOWS
			// BUT THE FUNCTION WE NEED DOESN'T EXIST UNTIL WINDOWS 7, CONVENIENTLY
			// SO CHECK FOR IT
			IntPtr lib = Win32.LoadLibrary("user32.dll");
			IntPtr proc = Win32.GetProcAddress(lib, "ChangeWindowMessageFilterEx");
			if (proc != IntPtr.Zero)
			{
				ChangeWindowMessageFilter(WM_DROPFILES, ChangeWindowMessageFilterFlags.Add);
				ChangeWindowMessageFilter(WM_COPYDATA, ChangeWindowMessageFilterFlags.Add);
				ChangeWindowMessageFilter(0x0049, ChangeWindowMessageFilterFlags.Add);
			}
			Win32.FreeLibrary(lib);
#endif

			var ffmpegPath = Path.Combine(GetExeDirectoryAbsolute(), "ffmpeg.exe");
			if (!File.Exists(ffmpegPath))
				ffmpegPath = Path.Combine(Path.Combine(GetExeDirectoryAbsolute(), "dll"), "ffmpeg.exe");
			FFMpeg.FFMpegPath = ffmpegPath;
			AudioExtractor.FFmpegPath = ffmpegPath;
			new DiscoHawk().Run(args);
		}


		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			lock (AppDomain.CurrentDomain)
			{
				var asms = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var asm in asms)
					if (asm.FullName == args.Name)
						return asm;

				//load missing assemblies by trying to find them in the dll directory
				string dllName = $"{new AssemblyName(args.Name).Name}.dll";
				string directory = Path.Combine(GetExeDirectoryAbsolute(), "dll");
				string fname = Path.Combine(directory, dllName);
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;

				// it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
			}
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetDllDirectory(string lpPathName);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
		static void RemoveMOTW(string path)
		{
			DeleteFileW($"{path}:Zone.Identifier");
		}

		static void WhackAllMOTW(string dllDir)
		{
			var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(dllDir) });
			while (todo.Count > 0)
			{
				var di = todo.Dequeue();
				foreach (var diSub in di.GetDirectories()) todo.Enqueue(diSub);
				foreach (var fi in di.GetFiles("*.dll"))
					RemoveMOTW(fi.FullName);
				foreach (var fi in di.GetFiles("*.exe"))
					RemoveMOTW(fi.FullName);
			}

		}
#endif

		private const uint WM_DROPFILES = 0x0233;
		private const uint WM_COPYDATA = 0x004A;
		[DllImport("user32")]
		public static extern bool ChangeWindowMessageFilter(uint msg, ChangeWindowMessageFilterFlags flags);
		public enum ChangeWindowMessageFilterFlags : uint
		{
			Add = 1, Remove = 2
		}
		public enum MessageFilterInfo : uint
		{
			None = 0, AlreadyAllowed = 1, AlreadyDisAllowed = 2, AllowedHigher = 3
		}

		public enum ChangeWindowMessageFilterExAction : uint
		{
			Reset = 0, Allow = 1, DisAllow = 2
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CHANGEFILTERSTRUCT
		{
			public uint size;
			public MessageFilterInfo info;
		}
	}

	internal class DiscoHawk
	{
		private static List<string> FindCuesRecurse(string dir)
		{
			var ret = new List<string>();
			var dpTodo = new Queue<string>();
			dpTodo.Enqueue(dir);
			for (; ; )
			{
				if (dpTodo.Count == 0)
					break;
				var dpCurr = dpTodo.Dequeue();
				foreach(var fi in new DirectoryInfo(dpCurr).GetFiles("*.cue"))
				{
					ret.Add(fi.FullName);
				}
				Parallel.ForEach(new DirectoryInfo(dpCurr).GetDirectories(), di =>
				{
					lock(dpTodo)
						dpTodo.Enqueue(di.FullName);
				});
			}

			return ret;
		}

		public void Run(string[] args)
		{
			if (args.Length == 0)
			{
				var dialog = new MainDiscoForm();
				dialog.ShowDialog();
				return;
			}

			bool scanCues = false;
			string dirArg = null;
			string infile = null;
			var loadDiscInterface = DiscInterface.BizHawk;
			var compareDiscInterfaces = new List<DiscInterface>();
			bool hawk = false;
			
			int idx = 0;
			while (idx < args.Length)
			{
				string a = args[idx++];
				string au = a.ToUpperInvariant();
				if (au == "LOAD")
					loadDiscInterface = (DiscInterface)Enum.Parse(typeof(DiscInterface), args[idx++], true);
				else if (au == "COMPARE")
					compareDiscInterfaces.Add((DiscInterface)Enum.Parse(typeof(DiscInterface), args[idx++], true));
				else if (au == "HAWK")
					hawk = true;
				else if (au == "CUEDIR")
				{
					dirArg = args[idx++];
					scanCues = true;
				}
				else infile = a;
			}

			if (hawk)
			{
				if (infile == null)
				{
					return;
				}

				// TODO - write it out
				var dmj = new DiscMountJob { IN_DiscInterface = loadDiscInterface, IN_FromPath = infile };
				dmj.Run();
			}

			bool verbose = true;

			if (scanCues)
			{
				verbose = false;
				var todo = FindCuesRecurse(dirArg);
				var po = new ParallelOptions();
				var cts = new CancellationTokenSource();
				po.CancellationToken = cts.Token;
				po.MaxDegreeOfParallelism = 1;
				if(po.MaxDegreeOfParallelism < 0) po.MaxDegreeOfParallelism = 1;
				object olock = new object();
				int ctr=0;
				bool blocked = false;
				try
				{
					Parallel.ForEach(todo, po, (fp) =>
					{
						lock (olock)
						{
							ctr++;
							int strlen = todo.Count.ToString().Length;
							string fmt = string.Format("{{0,{0}}}/{{1,{0}}} {{2}}", strlen);
							Console.WriteLine(fmt, ctr, todo.Count, Path.GetFileNameWithoutExtension(fp));
						}

						if(!blocked)
							foreach (var cmpif in compareDiscInterfaces)
							{
								var sw = new StringWriter();
								bool success = CompareFile(fp, loadDiscInterface, cmpif, verbose, cts, sw);
								if (!success)
								{
									lock (Console.Out)
										Console.Out.Write(sw.ToString());

									cts.Cancel();
									return;
								}
							}
					});
				}
				catch (AggregateException ae) {
					Console.WriteLine(ae.ToString());
				}
				catch (OperationCanceledException oce)
				{
					Console.WriteLine(oce.ToString());
				}
				Console.WriteLine("--TERMINATED--");
				return;
			}

			if (compareDiscInterfaces.Count != 0)
			{
				var sw = new StringWriter();
				foreach (var cmpif in compareDiscInterfaces)
				{
					CompareFile(infile, loadDiscInterface, cmpif, verbose, null, sw);
				}

				sw.Flush();
				string results = sw.ToString();
				var cr = new ComparisonResults { textBox1 = { Text = results } };
				cr.ShowDialog();
			}


		} //Run()

		private static bool CompareFile(string infile, DiscInterface loadDiscInterface, DiscInterface cmpif, bool verbose, CancellationTokenSource cancelToken, StringWriter sw)
		{
			Disc srcDisc = null, dstDisc = null;

			try
			{
				bool success = false;

				sw.WriteLine("BEGIN COMPARE: {0}\nSRC {1} vs DST {2}", infile, loadDiscInterface, cmpif);

				//reload the original disc, with new policies as needed
				var dmj = new DiscMountJob
				{
					IN_DiscInterface = loadDiscInterface,
					IN_DiscMountPolicy = new DiscMountPolicy { CUE_PregapContradictionModeA = cmpif != DiscInterface.MednaDisc },
					IN_FromPath = infile
				};

				dmj.Run();

				srcDisc = dmj.OUT_Disc;

				var dstDmj = new DiscMountJob { IN_DiscInterface = cmpif, IN_FromPath = infile };
				dstDmj.Run();
				dstDisc = dstDmj.OUT_Disc;

				var srcDsr = new DiscSectorReader(srcDisc);
				var dstDsr = new DiscSectorReader(dstDisc);

				var srcToc = srcDisc.TOC;
				var dstToc = dstDisc.TOC;

				var srcDataBuf = new byte[2448];
				var dstDataBuf = new byte[2448];

				void SwDumpTocOne(DiscTOC.TOCItem item)
				{
					if (!item.Exists)
					{
						sw.Write("(---missing---)");
					}
					else
					{
						sw.Write("({0:X2} - {1})", (byte) item.Control, item.LBA);
					}
				}

				void SwDumpToc(int index)
				{
					sw.Write("SRC TOC#{0,3} ", index);
					SwDumpTocOne(srcToc.TOCItems[index]);
					sw.WriteLine();
					sw.Write("DST TOC#{0,3} ", index);
					SwDumpTocOne(dstToc.TOCItems[index]);
					sw.WriteLine();
				}

				//verify sector count
				if (srcDisc.Session1.LeadoutLBA != dstDisc.Session1.LeadoutLBA)
				{
					sw.Write("LeadoutTrack.LBA {0} vs {1}\n", srcDisc.Session1.LeadoutTrack.LBA, dstDisc.Session1.LeadoutTrack.LBA);
					goto SKIPPO;
				}

				//verify TOC match
				if (srcDisc.TOC.FirstRecordedTrackNumber != dstDisc.TOC.FirstRecordedTrackNumber
					|| srcDisc.TOC.LastRecordedTrackNumber != dstDisc.TOC.LastRecordedTrackNumber)
				{
					sw.WriteLine("Mismatch of RecordedTrackNumbers: {0}-{1} vs {2}-{3}",
						srcDisc.TOC.FirstRecordedTrackNumber, srcDisc.TOC.LastRecordedTrackNumber,
						dstDisc.TOC.FirstRecordedTrackNumber, dstDisc.TOC.LastRecordedTrackNumber
						);
					goto SKIPPO;
				}

				bool badToc = false;
				for (int t = 0; t < 101; t++)
				{
					if (srcToc.TOCItems[t].Exists != dstToc.TOCItems[t].Exists
						|| srcToc.TOCItems[t].Control != dstToc.TOCItems[t].Control
						|| srcToc.TOCItems[t].LBA != dstToc.TOCItems[t].LBA
						)
					{
						sw.WriteLine("Mismatch in TOCItem");
						SwDumpToc(t);
						badToc = true;
					}
				}
				if (badToc)
					goto SKIPPO;

				void SwDumpChunkOne(string comment, int lba, byte[] buf, int addr, int count)
				{
					sw.Write("{0} -  ", comment);
					for (int i = 0; i < count; i++)
					{
						if (i + addr >= buf.Length) continue;
						sw.Write("{0:X2}{1}", buf[addr + i], (i == count - 1) ? " " : "  ");
					}

					sw.WriteLine();
				}

				int[] offenders = new int[12];

				void SwDumpChunk(int lba, int dispAddr, int addr, int count, int numOffenders)
				{
					var hashedOffenders = new HashSet<int>();
					for (int i = 0; i < numOffenders; i++)
					{
						hashedOffenders.Add(offenders[i]);
					}

					sw.Write("                          ");
					for (int i = 0; i < count; i++)
					{
						sw.Write((hashedOffenders.Contains(dispAddr + i)) ? "vvv " : "    ");
					}

					sw.WriteLine();
					sw.Write("                          ");
					for (int i = 0; i < count; i++)
					{
						sw.Write("{0:X3} ", dispAddr + i, (i == count - 1) ? " " : "  ");
					}

					sw.WriteLine();
					sw.Write("                          ");
					sw.Write(new string('-', count * 4));
					sw.WriteLine();
					SwDumpChunkOne($"SRC #{lba,6} ({new Timestamp(lba)})", lba, srcDataBuf, addr, count);
					SwDumpChunkOne($"DST #{lba,6} ({new Timestamp(lba)})", lba, dstDataBuf, addr, count);
				}

				//verify each sector contents
				int nSectors = srcDisc.Session1.LeadoutLBA;
				for (int lba = -150; lba < nSectors; lba++)
				{
					if (verbose)
						if (lba % 1000 == 0)
							Console.WriteLine("LBA {0} of {1}", lba, nSectors);

					if (cancelToken != null)
						if (cancelToken.Token.IsCancellationRequested)
							return false;

					srcDsr.ReadLBA_2448(lba, srcDataBuf, 0);
					dstDsr.ReadLBA_2448(lba, dstDataBuf, 0);

					//check the header
					for (int b = 0; b < 16; b++)
					{
						if (srcDataBuf[b] != dstDataBuf[b])
						{
							sw.WriteLine("Mismatch in sector header at byte {0}", b);
							offenders[0] = b;
							SwDumpChunk(lba, 0, 0, 16, 1);
							goto SKIPPO;
						}
					}

					// check userData
					for (int b = 16; b < 2352; b++)
					{
						if (srcDataBuf[b] != dstDataBuf[b])
						{
							sw.Write("LBA {0} mismatch at userdata byte {1}; terminating sector cmp\n", lba, b);
							goto SKIPPO;
						}
					}

					// check subChannels
					for (int c = 0, b = 2352; c < 8; c++)
					{
						int numOffenders = 0;
						for (int e = 0; e < 12; e++, b++)
						{
							if (srcDataBuf[b] != dstDataBuf[b])
							{
								offenders[numOffenders++] = e;
							}
						}

						if (numOffenders != 0)
						{
							sw.Write("LBA {0} mismatch(es) at subchannel {1}; terminating sector cmp\n", lba, (char)('P' + c));
							SwDumpChunk(lba, 0, 2352 + c * 12, 12, numOffenders);
							goto SKIPPO;
						}
					}
				}

				success = true;

			SKIPPO:
				sw.WriteLine("END COMPARE");
				sw.WriteLine("-----------------------------");

				return success;
			}
			finally
			{
				srcDisc?.Dispose();
				dstDisc?.Dispose();
			}
		
		} //CompareFile

	} //class DiscoHawk

#if false
	/// <summary>code to test ECM</summary>
	static class Test
	{
		public static void Shuffle<T>(this IList<T> list, Random rng)
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

		public static void TestMain()
		{
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
				testChunks.Shuffle(r);

				for (int t = 0; t < testChunks.Count; t++)
				{
					//Console.WriteLine("skank");
					var item = testChunks[t];
					//Console.WriteLine("chunk {0} of {3} is {1} bytes @ {2:X8}", t, item.Item2, item.Item1, testChunks.Count);
//					plaindisc.ReadLBA_2352_Flat(item.Item1, chunkbuf_corlet, 0, item.Item2); // API has changed
//					ecmdisc.ReadLBA_2352_Flat(item.Item1, chunkbuf_mine, 0, item.Item2); // API has changed
					for (int i = 0; i < item.Item2; i++)
						if (chunkbuf_corlet[i] != chunkbuf_mine[i])
						{
							Debug.Assert(false);
						}
				}
			}
		}
	}
#endif
}