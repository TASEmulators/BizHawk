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
			State = RunState.Running;
			FrameWaiting = false;
		}

		public LuaFile(string name, string path)
		{
			Name = name;
			Path = path;
			IsSeparator = false;

			// the current directory for the lua task will start off wherever the lua file is located
			CurrentDirectory = System.IO.Path.GetDirectoryName(path);
		}

		public LuaFile(bool isSeparator)
		{
			IsSeparator = isSeparator;
			Name = string.Empty;
			Path = string.Empty;
			State = RunState.Disabled;
		}

		public string Name { get; set; }
		public string Path { get; set; }
		public bool Enabled { get { return State != RunState.Disabled; } }
		public bool Paused { get { return State == RunState.Paused; } }
		public bool IsSeparator { get; set; }
		public LuaInterface.Lua Thread { get; set; }
		public bool FrameWaiting { get; set; }
		public string CurrentDirectory { get; set; }

		public enum RunState
		{
			Disabled, Running, Paused
		}

		public RunState State { get; set; }

		public static LuaFile SeparatorInstance
		{
			get { return new LuaFile(true); }
		}

		public void Stop()
		{
			State = RunState.Disabled;
			Thread = null;
		}

		public void Toggle()
		{
			if (State == RunState.Paused)
				State = RunState.Running;
			else if (State == RunState.Disabled)
				State = RunState.Running;
			else State = RunState.Disabled;
		}

		public void TogglePause()
		{
			if (State == RunState.Paused)
				State = RunState.Running;
			else if(State == RunState.Running)
				State = RunState.Paused;
		}
	}
}
