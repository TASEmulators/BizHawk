using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	public class M3U_File
	{
		public static M3U_File Read(StreamReader sr)
		{
			M3U_File ret = new M3U_File();
			if (!ret.Parse(sr))
				return null;
			else return ret;
		}

		bool Parse(StreamReader sr)
		{
			bool ext = false;
			int runtime = -1;
			string title = null;
			for (; ; )
			{
				string line = sr.ReadLine();
				if (line == null)
					break;
				if (line.StartsWith("#"))
				{
					if (line == "#EXTM3U")
					{
						ext = true;
						continue;
					}
					if (line.StartsWith("#EXTINF:"))
					{
						//TODO - maybe we shouldnt be so harsh. should probably add parse options.
						if (!ext) continue;

						line = line.Substring(8);
						int cidx = line.IndexOf(',');

						//dont know what to do with this, but its a comment, so ignore it
						if (cidx == -1)
							continue;

						runtime = int.Parse(line.Substring(0, cidx));
						title = line.Substring(cidx);
					}

					//just a comment. ignore it
					continue;
				}
				else
				{
					var e = new Entry {
						Path = line,
						Runtime = runtime,
						Title = title
					};
					Entries.Add(e);
					runtime = -1;
					title = null;
				}
			} //parse loop

			return true;
		} //Parse()

		public List<Entry> Entries = new List<Entry>();

		public void Rebase(string basepath)
		{
			foreach (var e in Entries)
			{
				//dont change rooted paths
				if (Path.IsPathRooted(e.Path)) continue;
				//adjust relative paths
				e.Path = Path.Combine(basepath, e.Path);
			}
		}

		public class Entry
		{
			public string Path;

			/// <summary>
			/// if the title is null, it isn't set
			/// </summary>
			public string Title;

			/// <summary>
			/// if the runtime is -1, it isn't set
			/// </summary>
			public int Runtime;
		}

	} //class M3U_File
}


