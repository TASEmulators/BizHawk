using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.Common
{
	public class LuaFileList : List<LuaFile>
	{
		private bool _changes;

		public Action ChangedCallback { get; set; }
		public bool Changes
		{
			get => _changes;
			set
			{
				_changes = value;
				if (ChangedCallback != null && _changes != value)
				{
					ChangedCallback();
				}
			}
		}

		public string Filename { get; set; } = "";

		public void StopAllScripts()
		{
			ForEach(lf => lf.State = LuaFile.RunState.Disabled);
		}

		public new void Clear()
		{
			StopAllScripts();
			Filename = "";
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

		public bool LoadLuaSession(string path, bool disableOnLoad)
		{
			var file = new FileInfo(path);
			if (!file.Exists)
			{
				return false;
			}

			Clear();
			using var sr = file.OpenText();
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
						scriptPath = Path.GetFullPath(Path.Combine(directory ?? "", scriptPath));
					}

					Add(new LuaFile(scriptPath)
					{
						State = !disableOnLoad && line.Substring(0, 1) == "1"
							? LuaFile.RunState.Running
							: LuaFile.RunState.Disabled
					});
				}
			}

			Filename = path;
			return true;
		}

		public void SaveSession(string path)
		{
			using var sw = new StreamWriter(path);
			var sb = new StringBuilder();
			foreach (var file in this)
			{
				if (file.IsSeparator)
				{
					sb.AppendLine("---");
				}
				else
				{
					sb
						.Append(file.Enabled ? "1" : "0")
						.Append(' ')
						.Append(Global.Config.PathEntries.AbsolutePathFor(file.Path, "").MakeRelativeTo(Path.GetDirectoryName(path)))
						.AppendLine();
				}
			}

			sw.Write(sb.ToString());

			Filename = path;
			Global.Config.RecentLuaSession.Add(path);
			Changes = false;
		}
	}
}
