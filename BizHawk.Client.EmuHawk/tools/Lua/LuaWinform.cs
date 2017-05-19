using System;
using System.Collections.Generic;
using System.Windows.Forms;

using LuaInterface;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaWinform : Form
	{
		public List<LuaEvent> ControlEvents { get; } = new List<LuaEvent>();

		private readonly string _currentDirectory = Environment.CurrentDirectory;
		private readonly Lua _ownerThread;

		public LuaWinform(Lua ownerThread)
		{
			InitializeComponent();
			_ownerThread = ownerThread;
			StartPosition = FormStartPosition.CenterParent;
			Closing += (o, e) => CloseThis();
		}

		private void LuaWinform_Load(object sender, EventArgs e)
		{
		}

		private void CloseThis()
		{
			GlobalWin.Tools.LuaConsole.LuaImp.WindowClosed(Handle);
		}

		public void DoLuaEvent(IntPtr handle)
		{
			LuaSandbox.Sandbox(_ownerThread, () =>
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
			public LuaEvent(IntPtr handle, LuaFunction lfunction)
			{
				Event = lfunction;
				Control = handle;
			}

			public LuaFunction Event { get; }
			public IntPtr Control { get; }
		}
	}
}
