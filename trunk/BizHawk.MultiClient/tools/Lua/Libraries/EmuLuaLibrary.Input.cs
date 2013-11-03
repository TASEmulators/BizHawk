using System.Drawing;
using System.Windows.Forms;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class InputLuaLibrary : LuaLibraryBase
	{
		public InputLuaLibrary(Lua lua)
			: base()
		{
			_lua = lua;
		}

		public override string Name { get { return "input"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"get",
					"getmouse"
				};
			}
		}

		private Lua _lua;

		public LuaTable input_get()
		{
			LuaTable buttons = _lua.NewTable();
			foreach (var kvp in GlobalWinF.ControllerInputCoalescer.BoolButtons())
				if (kvp.Value)
					buttons[kvp.Key] = true;
			return buttons;
		}

		public LuaTable input_getmouse()
		{
			LuaTable buttons = _lua.NewTable();
			Point p = GlobalWinF.RenderPanel.ScreenToScreen(Control.MousePosition);
			buttons["X"] = p.X;
			buttons["Y"] = p.Y;
			buttons[MouseButtons.Left.ToString()] = (Control.MouseButtons & MouseButtons.Left) != 0;
			buttons[MouseButtons.Middle.ToString()] = (Control.MouseButtons & MouseButtons.Middle) != 0;
			buttons[MouseButtons.Right.ToString()] = (Control.MouseButtons & MouseButtons.Right) != 0;
			buttons[MouseButtons.XButton1.ToString()] = (Control.MouseButtons & MouseButtons.XButton1) != 0;
			buttons[MouseButtons.XButton2.ToString()] = (Control.MouseButtons & MouseButtons.XButton2) != 0;
			return buttons;
		}
	}
}
