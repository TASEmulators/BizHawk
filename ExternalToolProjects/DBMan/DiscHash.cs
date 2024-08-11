#nullable disable

using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.DiscSystem;

namespace BizHawk.DBManTool
{
	class DiscHash
	{
		static List<string> FindExtensionsRecurse(string dir, string extUppercaseWithDot)
		{
			List<string> ret = new List<string>();
			Queue<string> dpTodo = new Queue<string>();
			dpTodo.Enqueue(dir);
			for (; ; )
			{
				string dpCurr;
				if (dpTodo.Count == 0)
					break;
				dpCurr = dpTodo.Dequeue();
				Parallel.ForEach(new DirectoryInfo(dpCurr).GetFiles(), (fi) =>
				{
					if (fi.Extension.ToUpperInvariant() == extUppercaseWithDot)
						lock (ret)
							ret.Add(fi.FullName);
				});
				Parallel.ForEach(new DirectoryInfo(dpCurr).GetDirectories(), (di) =>
				{
					lock (dpTodo)
						dpTodo.Enqueue(di.FullName);
				});
			}

			return ret;
		}

		public void Run(string[] args)
		{
			string indir = null;
			string dpTemp = null;
			string fpOutfile = null;

			for (int i = 0; ; )
			{
				if (i == args.Length) break;
				var arg = args[i++];
				if (arg == "--indir")
					indir = args[i++];
				if (arg == "--tempdir")
					dpTemp = args[i++];
				if (arg == "--outfile")
					fpOutfile = args[i++];
			}

			var done = new HashSet<string>();
			foreach (var line in File.ReadAllLines(fpOutfile))
			{
				if (line.Trim() == "") continue;
				var parts = line.Split(new[] { "//" }, StringSplitOptions.None);
				done.Add(parts[1]);
			}

			using (var outf = new StreamWriter(fpOutfile))
			{

				Dictionary<string, string> FoundHashes = new();
				object olock = new object();

				var todo = FindExtensionsRecurse(indir, ".CUE");

				int progress = 0;

				//loop over games (parallel doesnt work well when reading tons of data over the network, as we are here to do the complete redump hash)
				var po = new ParallelOptions();
				//po.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
				po.MaxDegreeOfParallelism = 1;
				Parallel.ForEach(todo, po, (fiCue) =>
				{
					string name = Path.GetFileNameWithoutExtension(fiCue);

					lock (olock)
					{
						if (done.Contains(name))
						{
							progress++;
							return;
						}
					}

					//now look for the cue file
					using (var disc = Disc.LoadAutomagic(fiCue))
					{
						var hasher = new DiscHasher(disc);

						var bizHashId = hasher.Calculate_PSX_BizIDHash();
						uint redumpHash = hasher.Calculate_PSX_RedumpHash();

						lock (olock)
						{
							progress++;
							Console.WriteLine("{0}/{1} [{2}] {3}", progress, todo.Count, bizHashId, Path.GetFileNameWithoutExtension(fiCue));
							outf.WriteLine("bizhash:{0} datahash:{1:X8} //{2}", bizHashId, redumpHash, name);
							if (FoundHashes.TryGetValue(bizHashId, out string foundBizHashId))
							{
								Console.WriteLine("--> COLLISION WITH: {0}", foundBizHashId);
								outf.WriteLine("--> COLLISION WITH: {0}", foundBizHashId);
							}
							else
								FoundHashes[bizHashId] = name;

							Console.Out.Flush();
							outf.Flush();
						}
					}


				}); //major loop

			} //using(outfile)

		} //MyRun()
	} //class PsxRedump

}
