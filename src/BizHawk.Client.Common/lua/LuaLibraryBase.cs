using System;
using System.Drawing;
using System.Threading;

using NLua;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
		protected LuaLibraryBase(Lua lua)
		{
			Lua = lua;
		}

		protected LuaLibraryBase(Lua lua, Action<string> logOutputCallback)
			: this(lua)
		{
			LogOutputCallback = logOutputCallback;
		}

		protected static LuaFile CurrentFile { get; private set; }

		private static Thread _currentHostThread;
		private static readonly object ThreadMutex = new object();

		public abstract string Name { get; }
		public Action<string> LogOutputCallback { protected get; set; }
		public Lua Lua { get; }

		public static void ClearCurrentThread()
		{
			lock (ThreadMutex)
			{
				_currentHostThread = null;
				CurrentFile = null;
			}
		}

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

		protected static int LuaInt(object luaArg)
		{
			return (int)(double)luaArg;
		}

		protected static Color? ToColor(object o)
		{
			return o switch
			{
				null => null,
				double d => Color.FromArgb((int) (long) d),
				string s => Color.FromName(s),
				_ => null
			};
		}

		protected void Log(object message)
		{
			LogOutputCallback?.Invoke(message.ToString());
		}
	}
}
