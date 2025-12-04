using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	public class LuaFile
	{
		public LuaFile(string path, Action onFunctionListChange)
		{
			Path = path;
			State = RunState.Disabled;
			Functions = new(onFunctionListChange);
		}

		private LuaFile(bool isSeparator) : this("", () => { })
		{
			IsSeparator = isSeparator;
		}

		public static LuaFile SeparatorInstance => new(true);

		public string Path { get; }
		public bool Enabled => State != RunState.Disabled;
		public bool Paused => State == RunState.Paused;
		public bool IsSeparator { get; }
		public LuaThread Thread { get; private set; }
		public bool FrameWaiting { get; set; }
		public bool RunningEventsOnly { get; set; } = false;

		public LuaFunctionList Functions { get; }

		public enum RunState
		{
			Disabled,
			Running,
			Paused,
			AwaitingStart,
		}

		public RunState State { get; private set; }

		public void Stop()
		{
			if (Thread is null)
			{
				return;
			}

			State = RunState.Disabled;

			foreach (NamedLuaFunction func in Functions
				.Where(l => l.Event == NamedLuaFunction.EVENT_TYPE_ENGINESTOP)
				.ToList())
			{
				func.Call();
			}
			Functions.Clear();

			Thread.Dispose();
			Thread = null;
		}

		public void Start(LuaThread thread)
		{
			if (Thread is not null) throw new InvalidOperationException("Cannot start an already started Lua file.");

			Thread = thread;
			State = RunState.Running;
			FrameWaiting = false;
			RunningEventsOnly = false;

			// Execution will not actually begin until the client calls LuaConsole.ResumeScripts
		}

		public void ScheduleStart()
		{
			if (State != RunState.Disabled) throw new InvalidOperationException("A Lua file that wasn't stopped was scheduled to start.");

			State = RunState.AwaitingStart;
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
