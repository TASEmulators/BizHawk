using NLua;

namespace BizHawk.Client.Common
{
	public sealed class InputLuaLibrary : LuaLibraryBase
	{
		public InputLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "input";

		[LuaMethodExample("local buttons_down = input.get();\nlocal is_b_down = buttons_down[\"B\"];\nif is_b_down and not was_b_down then console.writeline(\"B pressed\"); end\nwas_b_down = is_b_down;")]
		[LuaMethod("get", "Returns a dict-like table of key/button names (of host). Only pressed buttons will appear (with a value of {{true}}); unpressed buttons are omitted. Includes gamepad axes ({{!axis.isNeutral}}, with sticks as 4 \"buttons\" suffixed {{\"Up\"}}/{{\"Down\"}}/{{\"Left\"}}/{{\"Right\"}}). Includes mouse buttons, but not axes (cursor position and wheel rotation). Unlike {{getmouse}}, these have the names {{\"WMouse L\"}}, {{\"WMouse R\"}}, {{\"WMouse M\"}}, {{\"WMouse 1\"}}, and {{\"WMouse 2\"}} for LMB, RMB, MMB, Mouse4, and Mouse5, respectively.")]
		public LuaTable Get()
#pragma warning disable CS0618 // the ApiHawk equivalent of this function is warn-level deprecated; Lua can't and shouldn't make that distinction
			=> _th.DictToTable(APIs.Input.Get());
#pragma warning restore CS0618

		[LuaMethodExample("local mouse_buttons_down = input.getmouse();\nlocal is_m3_down = mouse_buttons_down[\"Middle\"];\nif is_m3_down and not was_m3_down then console.writeline(\"M3 pressed\"); end\nwas_m3_down = is_m3_down;")]
		[LuaMethod("getmouse", "Returns a lua table of the mouse X/Y coordinates and button states. Table keys are X, Y, Left, Middle, Right, XButton1, XButton2, Wheel.")]
		public LuaTable GetMouse()
			=> _th.DictToTable(APIs.Input.GetMouse());

		[LuaMethodExample("local axis_values = input.get_pressed_axes();\nif axis_values[\"X1 RightThumbY Axis\"] < -8000 then console.writeline(\"LStick is down\"); end")]
		[LuaMethod("get_pressed_axes", "Returns a dict-like table of (host) axis names and their state. Axes may not appear if they have never been seen with a value other than {{0}} (for example, if the gamepad has been set down on a table since launch, or if it was recently reconnected). Includes mouse cursor position axes, but not mouse wheel rotation. Unlike {{getmouse}}, these have the names {{\"WMouse X\"}} and {{\"WMouse Y\"}}.")]
		public LuaTable GetPressedAxes()
			=> _th.DictToTable(APIs.Input.GetPressedAxes());
	}
}
