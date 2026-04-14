-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class dotnetcolor : userdata

---A color in one of the following formats:
--- - Number in the format `0xAARRGGBB`
--- - String in the format `"#RRGGBB"` or `"#AARRGGBB"`
--- - A CSS3/X11 color name e.g. `"blue"`, `"palegoldenrod"`
--- - Color created with `forms.createcolor`
---@alias color integer | string | dotnetcolor

---@alias surface
---| "emucore" # Draw on the emulated screen. Resolution depends on emulated system and game. Drawing is scaled with the rest of the display.
---| "client" # Draw on the BizHawk window. Resolution depends on the window size. Drawing is not scaled.

