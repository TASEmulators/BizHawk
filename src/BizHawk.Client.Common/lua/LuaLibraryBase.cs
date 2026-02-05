using BizHawk.Emulation.Common;

using NLua;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
		public delegate INamedLuaFunction NLFAddCallback(
			LuaFunction function,
			string theEvent,
			string name = null);

		public delegate bool NLFRemoveCallback(Func<INamedLuaFunction, bool> predicate);

		public PathEntryCollection PathEntries { get; set; }

		protected LuaLibraryBase(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
		{
			LogOutputCallback = logOutputCallback;
			_luaLibsImpl = luaLibsImpl;
			_th = _luaLibsImpl.GetTableHelper();
			APIs = apiContainer;
			PathEntries = _luaLibsImpl.PathEntries;
		}

		public abstract string Name { get; }

		public ApiContainer APIs { get; set; }

		protected readonly Action<string> LogOutputCallback;

		protected readonly ILuaLibraries _luaLibsImpl;

		protected readonly NLuaTableHelper _th;

		/// <remarks>for implementors to reset any fields whose value depends on <see cref="APIs"/> or a <see cref="IEmulatorService">service</see></remarks>
		public virtual void Restarted() {}

		protected void Log(string message)
			=> LogOutputCallback?.Invoke(message);
	}
}
