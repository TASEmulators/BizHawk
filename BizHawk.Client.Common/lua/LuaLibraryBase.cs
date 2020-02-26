using System;
using System.Drawing;
using System.Threading;

using NLua;
using BizHawk.Common.ReflectionExtensions;

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
		protected Lua Lua { get; }

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
			if (o == null)
			{
				return null;
			}

			if (o is double d)
			{
				return Color.FromArgb((int)(long)d);
			}

			if (o is string s)
			{
				return Color.FromName(s);
			}

			return null;
		}

		protected void Log(object message)
		{
			LogOutputCallback?.Invoke(message.ToString());
		}

		public void LuaRegister(Type callingLibrary, LuaDocumentation docs = null)
		{
			Lua.NewTable(Name);
			foreach (var method in GetType().GetMethods())
			{
				var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
				if (foundAttrs.Length == 0) continue;
				Lua.RegisterFunction($"{Name}.{((LuaMethodAttribute) foundAttrs[0]).Name}", this, method);
				docs?.Add(new LibraryFunction(Name, callingLibrary.Description(), method));
			}
		}
	}
}
