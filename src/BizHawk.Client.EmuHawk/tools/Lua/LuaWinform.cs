using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NLua;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaWinform : Form
	{
		public List<LuaEvent> ControlEvents { get; } = new List<LuaEvent>();

		private readonly LuaLibraries _luaImp;
		private readonly string _currentDirectory = Environment.CurrentDirectory;
		private readonly LuaFile _ownerFile;

		public LuaWinform(LuaFile ownerFile, LuaLibraries luaImp)
		{
			_ownerFile = ownerFile;
			_luaImp = luaImp;
			InitializeComponent();
			StartPosition = FormStartPosition.CenterParent;
			Closing += (o, e) => CloseThis();
		}

		private void CloseThis()
		{
			_luaImp.WindowClosed(Handle);
		}

		public void DoLuaEvent(IntPtr handle)
		{
			// #1957 - ownerFile can be full, if the script that generated the form ended which will happen if the script does not have a while true loop
			LuaSandbox.Sandbox(_ownerFile?.Thread, () =>
			{
				Environment.CurrentDirectory = _currentDirectory;
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
