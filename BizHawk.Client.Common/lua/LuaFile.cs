namespace BizHawk.Client.Common
{
	public class LuaFile
	{
		public LuaFile(string path)
		{
			Name = "";
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

		private LuaFile(bool isSeparator)
		{
			IsSeparator = isSeparator;
			Name = "";
			Path = "";
			State = RunState.Disabled;
		}

		public static LuaFile SeparatorInstance => new LuaFile(true);

		public string Name { get; set; }
		public string Path { get; }
		public bool Enabled => State != RunState.Disabled;
		public bool Paused => State == RunState.Paused;
		public bool IsSeparator { get; }
		public NLua.Lua Thread { get; set; }
		public bool FrameWaiting { get; set; }
		public string CurrentDirectory { get; set; }

		public enum RunState
		{
			Disabled, Running, Paused
		}

		public RunState State { get; set; }

		public void Stop()
		{
			if (Thread == null)
			{
				return;
			}

			State = RunState.Disabled;
			//if(NLua.Lua.WhichLua == "NLua")
				Thread.GetTable("keepalives")[Thread] = null;
			Thread = null;
		}

		public void Toggle()
		{
			switch (State)
			{
				case RunState.Paused:
					State = RunState.Running;
					break;
				case RunState.Disabled:
					State = RunState.Running;
					FrameWaiting = false;
					break;
				default:
					State = RunState.Disabled;
					break;
			}
		}

		public void TogglePause()
		{
			if (State == RunState.Paused)
			{
				State = RunState.Running;
			}
			else if (State == RunState.Running)
			{
				State = RunState.Paused;
			}
		}
	}
}
