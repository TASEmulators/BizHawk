using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public class LuaFile
	{
		public LuaFile(string path)
		{
			Name = string.Empty;
			Path = path;
			Enabled = true;
			Paused = false;
			FrameWaiting = false;
		}

		public LuaFile(string name, string path)
		{
			Name = name;
			Path = path;
			IsSeparator = false;

			// the current directory for the lua task will start off wherever the lua file is located
			var directoryInfo = new FileInfo(path).Directory;
			if (directoryInfo != null)
			{
				CurrentDirectory = directoryInfo.FullName;
			}
		}

		public LuaFile(bool isSeparator)
		{
			IsSeparator = isSeparator;
			Name = string.Empty;
			Path = string.Empty;
			Enabled = false;
		}

		public LuaFile(LuaFile file)
		{
			Name = file.Name;
			Path = file.Path;
			Enabled = file.Enabled;
			Paused = file.Paused;
			IsSeparator = file.IsSeparator;
			CurrentDirectory = file.CurrentDirectory;
		}

		public string Name { get; set; }
		public string Path { get; set; }
		public bool Enabled { get; set; }
		public bool Paused { get; set; }
		public bool IsSeparator { get; set; }
		public LuaInterface.Lua Thread { get; set; }
		public bool FrameWaiting { get; set; }
		public string CurrentDirectory { get; set; }

		public static LuaFile SeparatorInstance
		{
			get { return new LuaFile(true); }
		}

		public void Stop()
		{
			Enabled = false;
			Thread = null;
		}

		public void Toggle()
		{
			Enabled ^= true;
			if (Enabled)
			{
				Paused = false;
			}
		}

		public void TogglePause()
		{
			Paused ^= true;
		}
	}
}
