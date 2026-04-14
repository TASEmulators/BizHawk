-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class input
input = {}

---Returns a dict-like table of key/button names (of host). Only pressed buttons will appear (with a value of `true`); unpressed buttons are omitted. Includes gamepad axes (`!axis.isNeutral`, with sticks as 4 "buttons" suffixed `"Up"`/`"Down"`/`"Left"`/`"Right"`). Includes mouse buttons, but not axes (cursor position and wheel rotation). Unlike `getmouse`, these have the names `"WMouse L"`, `"WMouse R"`, `"WMouse M"`, `"WMouse 1"`, and `"WMouse 2"` for LMB, RMB, MMB, Mouse4, and Mouse5, respectively.
---
---Example:
---
---	local buttons_down = input.get();
---	local is_b_down = buttons_down["B"];
---	if is_b_down and not was_b_down then console.writeline("B pressed"); end
---	was_b_down = is_b_down;
---@return table
function input.get() end

---Returns a dict-like table of (host) axis names and their state. Axes may not appear if they have never been seen with a value other than `0` (for example, if the gamepad has been set down on a table since launch, or if it was recently reconnected). Includes mouse cursor position axes, but not mouse wheel rotation. Unlike `getmouse`, these have the names `"WMouse X"` and `"WMouse Y"`.
---
---Example:
---
---	local axis_values = input.get_pressed_axes();
---	if axis_values["X1 RightThumbY Axis"] < -8000 then console.writeline("LStick is down"); end
---@return table
function input.get_pressed_axes() end

---Returns a lua table of the mouse X/Y coordinates and button states. Table keys are X, Y, Left, Middle, Right, XButton1, XButton2, Wheel.
---
---Example:
---
---	local mouse_buttons_down = input.getmouse();
---	local is_m3_down = mouse_buttons_down["Middle"];
---	if is_m3_down and not was_m3_down then console.writeline("M3 pressed"); end
---	was_m3_down = is_m3_down;
---@return table
function input.getmouse() end

