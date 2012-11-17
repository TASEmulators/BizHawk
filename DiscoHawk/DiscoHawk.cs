using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

//cue format preferences notes

//pcejin -
//does not like session commands
//it can handle binpercue
//it seems not to be able to handle binpertrack, or maybe i am doing something wrong (still havent ruled it out)

namespace BizHawk
{
	class DiscoHawk
	{

		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}

		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			//load missing assemblies by trying to find them in the dll directory
			string dllname = new AssemblyName(args.Name).Name + ".dll";
			string directory = System.IO.Path.Combine(GetExeDirectoryAbsolute(), "dll");
			string fname = Path.Combine(directory, dllname);
			if (!File.Exists(fname)) return null;
			//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
			return Assembly.LoadFile(fname);
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetDllDirectory(string lpPathName);
#endif

		[STAThread]
		static void Main(string[] args)
		{
#if WINDOWS
			// this will look in subdirectory "dll" to load pinvoked stuff
			SetDllDirectory(System.IO.Path.Combine(GetExeDirectoryAbsolute(), "dll"));

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif

			SubMain(args);
		}

		static void SubMain(string[] args)
		{
			var ffmpegPath = Path.Combine(GetExeDirectoryAbsolute(), "ffmpeg.exe");
			if(!File.Exists(ffmpegPath))
				ffmpegPath = Path.Combine(Path.Combine(GetExeDirectoryAbsolute(), "dll"), "ffmpeg.exe");
			DiscSystem.FFMpeg.FFMpegPath = ffmpegPath;
			AudioExtractor.FFmpegPath = ffmpegPath;
			new DiscoHawk().Run(args);
		}

		void Run(string[] args)
		{
			bool gui = true;
			foreach (var arg in args)
			{
				if (arg.ToUpper() == "COMMAND") gui = false;
			}

			if (gui)
			{
				var dialog = new MainDiscoForm();
				dialog.ShowDialog();
				return;
			}
		}
	}

}

//code to test ECM:
//static class test
//{
//  public static void Shuffle<T>(this IList<T> list, Random rng)
//  {
//    int n = list.Count;
//    while (n > 1)
//    {
//      n--;
//      int k = rng.Next(n + 1);
//      T value = list[k];
//      list[k] = list[n];
//      list[n] = value;
//    }
//  }

//  public static void Test()
//  {
//    var plaindisc = BizHawk.DiscSystem.Disc.FromCuePath("d:\\ecmtest\\test.cue", BizHawk.MainDiscoForm.GetCuePrefs());
//    var ecmdisc = BizHawk.DiscSystem.Disc.FromCuePath("d:\\ecmtest\\ecmtest.cue", BizHawk.MainDiscoForm.GetCuePrefs());

//    //var prefs = new BizHawk.DiscSystem.CueBinPrefs();
//    //prefs.AnnotateCue = false;
//    //prefs.OneBlobPerTrack = false;
//    //prefs.ReallyDumpBin = true;
//    //prefs.SingleSession = true;
//    //prefs.DumpToBitbucket = true;
//    //var dump = ecmdisc.DumpCueBin("test", prefs);
//    //dump.Dump("test", prefs);

//    //var prefs = new BizHawk.DiscSystem.CueBinPrefs();
//    //prefs.AnnotateCue = false;
//    //prefs.OneBlobPerTrack = false;
//    //prefs.ReallyDumpBin = true;
//    //prefs.SingleSession = true;
//    //var dump = ecmdisc.DumpCueBin("test", prefs);
//    //dump.Dump(@"D:\ecmtest\myout", prefs);

//    int seed = 102;

//    for (; ; )
//    {
//      Console.WriteLine("running seed {0}", seed);
//      Random r = new Random(seed);
//      seed++;

//      byte[] chunkbuf_corlet = new byte[2352 * 20];
//      byte[] chunkbuf_mine = new byte[2352 * 20];
//      int length = ecmdisc.LBACount * 2352;
//      int counter = 0;
//      List<Tuple<int, int>> testChunks = new List<Tuple<int, int>>();
//      while (counter < length)
//      {
//        int chunk = r.Next(1, 2352 * 20);
//        if (r.Next(20) == 0)
//          chunk /= 100;
//        if (r.Next(40) == 0)
//          chunk = 0;
//        if (counter + chunk > length)
//          chunk = length - counter;
//        testChunks.Add(new Tuple<int, int>(counter, chunk));
//        counter += chunk;
//      }
//      testChunks.Shuffle(r);

//      for (int t = 0; t < testChunks.Count; t++)
//      {
//        //Console.WriteLine("skank");
//        var item = testChunks[t];
//        //Console.WriteLine("chunk {0} of {3} is {1} bytes @ {2:X8}", t, item.Item2, item.Item1, testChunks.Count);
//        plaindisc.ReadLBA_2352_Flat(item.Item1, chunkbuf_corlet, 0, item.Item2);
//        ecmdisc.ReadLBA_2352_Flat(item.Item1, chunkbuf_mine, 0, item.Item2);
//        for (int i = 0; i < item.Item2; i++)
//          if (chunkbuf_corlet[i] != chunkbuf_mine[i])
//          {
//            Debug.Assert(false);
//          }
//      }
//    }
//  }
//}