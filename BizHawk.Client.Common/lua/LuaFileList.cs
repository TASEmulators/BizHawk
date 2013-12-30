using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Client.Common
{
	public class LuaFileList : List<LuaFile>
	{
		internal LuaFileList() { }
		
		private string _filename = String.Empty;
		private bool _changes;

		public Action ChangedCallback { get; set; }
		public Action LoadCallback { get; set; }

		public void StopAllScripts()
		{
			ForEach(x => x.Enabled = false);
		}

		public bool Changes
		{
			get
			{
				return _changes;
			}

			set
			{
				_changes = value;
				if (ChangedCallback != null && _changes != value)
				{
					ChangedCallback();
				}
			}
		}

		public string Filename
		{
			get
			{
				return _filename;
			}

			set
			{
				_filename = value ?? String.Empty;
			}
		}

		public new void Clear()
		{
			StopAllScripts();
			_filename = String.Empty;
			Changes = false;
			base.Clear();
		}

		public new void Add(LuaFile item)
		{
			Changes = true;
			base.Add(item);
		}

		public new void Insert(int index, LuaFile item)
		{
			Changes = true;
			base.Insert(index, item);
		}

		public new bool Remove(LuaFile item)
		{
			Changes = true;
			return base.Remove(item);
		}

		public new int RemoveAll(Predicate<LuaFile> match)
		{
			return base.RemoveAll(match);
		}

		public bool LoadLuaSession(string path)
		{
			var file = new FileInfo(path);
			if (file.Exists)
			{
				Clear();
				using (var sr = file.OpenText())
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						if (line.StartsWith("---"))
						{
							Add(LuaFile.SeparatorInstance);
						}
						else
						{
							Add(new LuaFile(line.Substring(2, line.Length - 2))
							{
								Enabled = !Global.Config.DisableLuaScriptsOnLoad &&
								line.Substring(0, 1) == "1",
							});
						}
					}
				}

				Global.Config.RecentLuaSession.Add(path);
				ForEach(lua => Global.Config.RecentLua.Add(lua.Path));

				_filename = path;
				if (LoadCallback != null)
				{
					LoadCallback();
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public void SaveSession()
		{
			if (!String.IsNullOrWhiteSpace(_filename))
			{
				SaveSession(_filename);
			}
		}

		public void SaveSession(string path)
		{
			using (var sw = new StreamWriter(path))
			{
				var sb = new StringBuilder();
				foreach (var file in this)
				{
					sb
						.Append(file.Enabled ? "1" : "0")
						.Append(' ')
						.Append(file.Path)
						.AppendLine();
				}

				sw.Write(sb.ToString());
			}

			Global.Config.RecentLuaSession.Add(path);
			Changes = false;
		}
	}
}
