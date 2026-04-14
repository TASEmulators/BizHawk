-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class memorysavestate
memorysavestate = {}

---clears all savestates stored in memory
---
---Example:
---
---	memorysavestate.clearstatesfrommemory( );
function memorysavestate.clearstatesfrommemory() end

---loads an in memory state with the given identifier
---
---Example:
---
---	memorysavestate.loadcorestate( "3fcf120f-0778-43fd-b2c5-460fb7d34184" );
---@param identifier string
function memorysavestate.loadcorestate(identifier) end

---removes the savestate with the given identifier from memory
---
---Example:
---
---	memorysavestate.removestate( "3fcf120f-0778-43fd-b2c5-460fb7d34184" );
---@param identifier string
function memorysavestate.removestate(identifier) end

---creates a core savestate and stores it in memory.  Note: a core savestate is only the raw data from the core, and not extras such as movie input logs, or framebuffers. Returns a unique identifer for the savestate
---
---Example:
---
---	local mmsvstsvcst = memorysavestate.savecorestate( );
---@return string
function memorysavestate.savecorestate() end

