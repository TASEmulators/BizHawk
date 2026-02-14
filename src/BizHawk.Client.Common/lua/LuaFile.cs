using System.Collections.Generic;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// If a <see cref="LuaFile"/> owns an instance of this interface, it will not stop until all such instances are removed.
	/// This is similar to how a script with registered callbacks does not stop until all callbacks are removed.
	/// </summary>
	public interface IKeepFileRunning : IDisposable { }

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

		private List<IDisposable> _disposables = new();

		public enum RunState
		{
			Disabled,
			Running,
			Paused,
			AwaitingStart,
		}

		public RunState State { get; private set; }

		public void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);

		public void RemoveDisposable(IDisposable disposable) => _disposables.Remove(disposable);

		public bool ShouldKeepRunning()
		{
			return _disposables.Exists((d) => d is IKeepFileRunning)
				|| Functions.Any(f => f.Event != NamedLuaFunction.EVENT_TYPE_ENGINESTOP);
		}

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

			foreach (IDisposable disposable in _disposables.ToList())
				disposable.Dispose();
			_disposables.Clear();

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
