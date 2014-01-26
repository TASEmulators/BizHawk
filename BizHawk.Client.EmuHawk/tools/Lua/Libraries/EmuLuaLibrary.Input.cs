using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using LuaInterface;

namespace BizHawk.Client.EmuHawk
{
	public class InputLuaLibrary : LuaLibraryBase
	{
		public InputLuaLibrary(Lua lua)
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

		private readonly Lua _lua;

		[LuaMethodAttributes(
			"get",
			"TODO"
		)]
		public LuaTable Get()
		{
			var buttons = _lua.NewTable();
			foreach (var kvp in Global.ControllerInputCoalescer.BoolButtons().Where(kvp => kvp.Value))
			{
				buttons[kvp.Key] = true;
			}

			return buttons;
		}

		[LuaMethodAttributes(
			"getmouse",
			"TODO"
		)]
		public LuaTable GetMouse()
		{
			var buttons = _lua.NewTable();
			var p = GlobalWin.RenderPanel.ScreenToScreen(Control.MousePosition);
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
