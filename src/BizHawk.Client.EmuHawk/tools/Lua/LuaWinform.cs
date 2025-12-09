using System.Collections.Generic;
using System.Windows.Forms;
using NLua;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaWinform : Form
	{
		public List<LuaEvent> ControlEvents { get; } = new List<LuaEvent>();

		private readonly LuaFile _ownerFile;
		private readonly ILuaLibraries _luaImp;

		public bool BlocksInputWhenFocused { get; set; } = true;

		public LuaWinform(LuaFile ownerFile, ILuaLibraries luaLibraries, Action<IntPtr> formsWindowClosedCallback)
		{
			_ownerFile = ownerFile;
			_luaImp = luaLibraries;
			InitializeComponent();
			Icon = Properties.Resources.TextDocIcon;
			StartPosition = FormStartPosition.CenterParent;
			Closing += (o, e) => formsWindowClosedCallback(Handle);
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
