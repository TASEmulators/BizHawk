-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class savestate
savestate = {}

---Loads a savestate with the given path. Returns true iff succeeded. If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes (and the path is ignored).
---
---Example:
---
---	savestate.load( "C:\state.bin" );
---@param path string
---@param suppressOSD? boolean Defaults to `false`
---@return boolean
function savestate.load(path, suppressOSD) end

---Loads the savestate at the given slot number (must be an integer between 1 and 10). Returns true iff succeeded. If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes with the slot number.
---
---Example:
---
---	savestate.loadslot( 7 );
---@param slotNum integer
---@param suppressOSD? boolean Defaults to `false`
---@return boolean
function savestate.loadslot(slotNum, suppressOSD) end

---Saves a state at the given path. If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes (and the path is ignored).
---
---Example:
---
---	savestate.save( "C:\state.bin" );
---@param path string
---@param suppressOSD? boolean Defaults to `false`
---@return boolean
function savestate.save(path, suppressOSD) end

---Saves a state at the given save slot (must be an integer between 1 and 10). If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes with the slot number.
---
---Example:
---
---	savestate.saveslot( 7 );
---@param slotNum integer
---@param suppressOSD? boolean Defaults to `false`
---@return boolean
function savestate.saveslot(slotNum, suppressOSD) end

