using System.IO;
using System.Collections.Generic;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	public class M3U_File
	{
		public static M3U_File Read(StreamReader sr)
		{
			var ret = new M3U_File();
			return !ret.Parse(sr) ? null : ret;
		}

		private bool Parse(StreamReader sr)
		{
			var ext = false;
			var runtime = -1;
			string title = null;
			while (true)
			{
				var line = sr.ReadLine();
				if (line == null)
					break;
				if (line.StartsWith('#'))
				{
					if (line == "#EXTM3U")
					{
						ext = true;
						continue;
					}
					if (line.StartsWithOrdinal("#EXTINF:"))
					{
						//TODO - maybe we shouldn't be so harsh. should probably add parse options.
						if (!ext) continue;

						line = line.Substring(8);
						var cidx = line.IndexOf(',');

						//don't know what to do with this, but its a comment, so ignore it
						if (cidx == -1)
							continue;

						runtime = int.Parse(line.Substring(0, cidx));
						title = line.Substring(cidx);
					}

					//just a comment. ignore it
					continue;
				}

				var e = new Entry
				{
					Path = line,
					Runtime = runtime,
					Title = title
				};
				Entries.Add(e);
				runtime = -1;
				title = null;
			}

			return true;
		}

		public readonly IList<Entry> Entries = new List<Entry>();

		public void Rebase(string basepath)
		{
			foreach (var e in Entries)
			{
				//don't change rooted paths
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
	}
}
