using System.Collections.Generic;
using System.Windows.Forms;
using NLua;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaWinform : Form, IKeepFileRunning
	{
		public List<LuaEvent> ControlEvents { get; } = new List<LuaEvent>();

		private readonly LuaFile _ownerFile;
		private readonly ILuaLibraries _luaImp;
		private readonly LuaFunction/*?*/ _closeCallback;
		private bool _closed = false;

		public bool BlocksInputWhenFocused { get; set; } = true;

		public LuaWinform(LuaFile ownerFile, ILuaLibraries luaLibraries, Action<IntPtr> formsWindowClosedCallback, LuaFunction luaCloseCallback = null)
		{
			_ownerFile = ownerFile;
			_luaImp = luaLibraries;
			_closeCallback = luaCloseCallback;

			InitializeComponent();

			Icon = Properties.Resources.TextDocIcon;
			StartPosition = FormStartPosition.CenterParent;

			_ownerFile.AddDisposable(this);
			Closing += (o, e) =>
			{
				if (_closed) return;
				_closed = true;

				if (_closeCallback != null)
				{
					_luaImp.Sandbox(_ownerFile, () => _ = _closeCallback.Call());
				}
				formsWindowClosedCallback(Handle);
				_ownerFile.RemoveDisposable(this);
			};
		}

		public void DoLuaEvent(IntPtr handle)
		{
			_luaImp.Sandbox(_ownerFile, () =>
			{
				foreach (LuaEvent luaEvent in ControlEvents)
				{
					if (luaEvent.Control == handle)
					{
						luaEvent.Event.Call();
					}
				}
			});
		}

		public class LuaEvent
		{
			public LuaEvent(IntPtr handle, LuaFunction luaFunction)
			{
				Event = luaFunction;
				Control = handle;
			}

			public LuaFunction Event { get; }
			public IntPtr Control { get; }
		}
	}
}
