using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public class LuaFile
	{
		public string Name { get; set; }
		public string Path { get; set; }
		public bool Enabled { get; set; }
		public bool Paused { get; set; }
		public bool IsSeparator { get; set; }
		public LuaInterface.Lua Thread { get; set; }
		public bool FrameWaiting { get; set; }
		public string CurrentDirectory { get; set; }

		public LuaFile(string path)
		{
			Name = String.Empty;
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

			//the current directory for the lua task will start off wherever the lua file is located
			var directory_info = new FileInfo(path).Directory;
			if (directory_info != null) CurrentDirectory = directory_info.FullName;
		}

		public LuaFile(bool isSeparator)
		{
			IsSeparator = isSeparator;
			Name = String.Empty;
			Path = String.Empty;
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
