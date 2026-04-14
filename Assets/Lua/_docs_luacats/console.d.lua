-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class console
console = {}

---clears the output box of the Lua Console window
---
---Example:
---
---	console.clear( );
function console.clear() end

---Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable
---
---Example:
---
---	console.log( "New log." );
---@vararg any
function console.log(...) end

---Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable
---
---Example:
---
---	console.write( "New log message." );
---@vararg any
function console.write(...) end

---Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable
---
---Example:
---
---	console.writeline( "New log line." );
---@vararg any
function console.writeline(...) end

