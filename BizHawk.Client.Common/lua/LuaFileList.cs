using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Client.Common
{
	public class LuaFileList : List<LuaFile>
	{
		private string _filename = string.Empty;
		private bool _changes;

		public Action ChangedCallback { get; set; }
		public Action LoadCallback { get; set; }

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
				_filename = value ?? string.Empty;
			}
		}

		public void StopAllScripts()
		{
			ForEach(x => x.State = LuaFile.RunState.Disabled);
		}

		public new void Clear()
		{
			StopAllScripts();
			_filename = string.Empty;
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
							var scriptPath = line.Substring(2, line.Length - 2);
							if (!Path.IsPathRooted(scriptPath))
							{
								var directory = Path.GetDirectoryName(path);
								scriptPath = Path.GetFullPath(Path.Combine(directory, scriptPath));
							}

							Add(new LuaFile(scriptPath)
							{
								State = (
										!Global.Config.DisableLuaScriptsOnLoad 
										&& line.Substring(0, 1) == "1"
									) ? LuaFile.RunState.Running : LuaFile.RunState.Disabled
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
			
			return false;
		}

		public void SaveSession()
		{
			if (!string.IsNullOrWhiteSpace(_filename))
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
						.Append(PathManager.MakeRelativeTo(PathManager.MakeAbsolutePath(file.Path, ""), Path.GetDirectoryName(path)))
						.AppendLine();
				}

				sw.Write(sb.ToString());
			}

			Filename = path;
			Global.Config.RecentLuaSession.Add(path);
			Changes = false;
		}
	}
}
