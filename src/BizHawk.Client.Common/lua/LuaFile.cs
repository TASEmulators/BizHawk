using NLua;

namespace BizHawk.Client.Common
{
	public class LuaFile
	{
		public LuaFile(string path)
		{
			Path = path;
			State = RunState.Disabled;
		}

		private LuaFile(bool isSeparator) : this("")
		{
			IsSeparator = isSeparator;
		}

		public static LuaFile SeparatorInstance => new(true);

		public string Path { get; }
		public bool Enabled => State != RunState.Disabled;
		public bool Paused => State == RunState.Paused;
		public bool IsSeparator { get; }
		public LuaThread Thread { get; set; }
		public bool FrameWaiting { get; set; }

		public enum RunState
		{
			Disabled,
			Running,
			Paused,
		}

		public RunState State { get; set; }

		public void Stop()
		{
			if (Thread is null)
			{
				return;
			}

			State = RunState.Disabled;
			Thread.Dispose();
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
