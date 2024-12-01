using System.Threading;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
		public PathEntryCollection PathEntries { get; set; }

		protected LuaLibraryBase(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
		{
			LogOutputCallback = logOutputCallback;
			_luaLibsImpl = luaLibsImpl;
			_th = _luaLibsImpl.GetTableHelper();
			APIs = apiContainer;
			PathEntries = _luaLibsImpl.PathEntries;
		}

		protected static LuaFile CurrentFile { get; private set; }

		private static Thread _currentHostThread;
		private static readonly object ThreadMutex = new();

		public abstract string Name { get; }

		public ApiContainer APIs { get; set; }

		protected readonly Action<string> LogOutputCallback;

		protected readonly ILuaLibraries _luaLibsImpl;

		protected readonly NLuaTableHelper _th;

		public static void ClearCurrentThread()
		{
			lock (ThreadMutex)
			{
				_currentHostThread = null;
				CurrentFile = null;
			}
		}

		/// <remarks>for implementors to reset any fields whose value depends on <see cref="APIs"/> or a <see cref="IEmulatorService">service</see></remarks>
		public virtual void Restarted() {}

		/// <exception cref="InvalidOperationException">attempted to have Lua running in two host threads at once</exception>
		public static void SetCurrentThread(LuaFile luaFile)
		{
			lock (ThreadMutex)
			{
				if (_currentHostThread != null)
				{
					throw new InvalidOperationException("Can't have lua running in two host threads at a time!");
				}

				_currentHostThread = Thread.CurrentThread;
				CurrentFile = luaFile;
			}
		}

		protected void Log(string message)
			=> LogOutputCallback?.Invoke(message);
	}
}
