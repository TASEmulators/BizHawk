-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class joypad
joypad = {}

---returns a lua table of the controller buttons pressed. If supplied, it will only return a table of buttons for the given controller
---
---Example:
---
---	local nljoyget = joypad.get( 1 );
---@param controller? integer
---@return table
function joypad.get(controller) end

---returns a lua table of any controller buttons currently pressed by the user
---
---Example:
---
---	local nljoyget = joypad.getimmediate( );
---@param controller? integer
---@return table
function joypad.getimmediate(controller) end

---returns a lua table of the controller buttons pressed, including ones pressed by the current movie. If supplied, it will only return a table of buttons for the given controller
---
---Example:
---
---	local nljoyget = joypad.getwithmovie( 1 );
---@param controller? integer
---@return table
function joypad.getwithmovie(controller) end

---sets the given buttons to their provided values for the current frame
---
---Example:
---
---	joypad.set( { ["Left"] = true, [ "A" ] = true, [ "B" ] = true } );
---@param buttons table
---@param controller? integer
function joypad.set(buttons, controller) end

---Sets the given analog controls to their provided values as autoholds. Set axes to the empty string to clear individual holds.
---
---Example:
---
---	joypad.setanalog( { [ "Tilt X" ] = -63, [ "Tilt Y" ] = 127 } );
---@param controls table
---@param controller? integer
function joypad.setanalog(controls, controller) end

---sets the given buttons to their provided values for the current frame, string will be interpreted the same way an entry from a movie input log would be
---
---Example:
---
---	joypad.setfrommnemonicstr( "|    0,    0,    0,  100,...R..B....|" );
---@param inputLogEntry string
function joypad.setfrommnemonicstr(inputLogEntry) end

