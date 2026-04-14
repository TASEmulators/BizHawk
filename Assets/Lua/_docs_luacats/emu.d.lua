-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for interacting with the currently loaded emulator core
---@class emu
emu = {}

---Returns the disassembly object (disasm string and length int) for the given PC address. Uses System Bus domain if no domain name provided
---
---Example:
---
---	local obemudis = emu.disassemble( 0x8000 );
---@param pc integer
---@param name? string
---@return table
function emu.disassemble(pc, name) end

---Sets the display vsync property of the emulator
---
---Example:
---
---	emu.displayvsync( true );
---@param enabled boolean
function emu.displayvsync(enabled) end

---Signals to the emulator to resume emulation. Necessary for any lua script while loop or else the emulator will freeze!
---
---Example:
---
---	emu.frameadvance( );
function emu.frameadvance() end

---Returns the current frame count
---
---Example:
---
---	local inemufra = emu.framecount( );
---@return integer
function emu.framecount() end

---returns (if available) the board name of the loaded ROM
---
---Example:
---
---	local stemuget = emu.getboardname();
---@return string
function emu.getboardname() end

---returns the display type (PAL vs NTSC) that the emulator is currently running in
---
---Example:
---
---	local stemuget = emu.getdisplaytype();
---@return string
function emu.getdisplaytype() end

---returns the value of a cpu register or flag specified by name. For a complete list of possible registers or flags for a given core, use getregisters
---
---Example:
---
---	local inemuget = emu.getregister( emu.getregisters( )[ 0 ] );
---@param name string
---@return integer
function emu.getregister(name) end

---returns the complete set of available flags and registers for a given core
---
---Example:
---
---	local nlemuget = emu.getregisters( );
---@return table
function emu.getregisters() end

---Returns the ID string of the current core loaded. Note: No ROM loaded will return the string NULL
---
---Example:
---
---	local stemuget = emu.getsystemid( );
---@return string
function emu.getsystemid() end

---Returns whether or not the current frame is a lag frame
---
---Example:
---
---	if ( emu.islagged( ) ) then
---		console.log( "Returns whether or not the current frame is a lag frame" );
---	end;
---@return boolean
function emu.islagged() end

---Returns the current lag count
---
---Example:
---
---	local inemulag = emu.lagcount( );
---@return integer
function emu.lagcount() end

---sets the limit framerate property of the emulator
---
---Example:
---
---	emu.limitframerate( true );
---@param enabled boolean
function emu.limitframerate(enabled) end

---Sets the autominimizeframeskip value of the emulator
---
---Example:
---
---	emu.minimizeframeskip( true );
---@param enabled boolean
function emu.minimizeframeskip(enabled) end

---Sets the lag flag for the current frame. If no value is provided, it will default to true
---
---Example:
---
---	emu.setislagged( true );
---@param value? boolean Defaults to `true`
function emu.setislagged(value) end

---Sets the current lag count
---
---Example:
---
---	emu.setlagcount( 50 );
---@param count integer
function emu.setlagcount(count) end

---sets the given register name to the given value
---
---Example:
---
---	emu.setregister( emu.getregisters( )[ 0 ], -1000 );
---@param register string
---@param value integer
function emu.setregister(register, value) end

---Toggles the drawing of sprites and background planes. Set to false or nil to disable a pane, anything else will draw them
---
---Example:
---
---	emu.setrenderplanes( true, false );
---@vararg boolean
function emu.setrenderplanes(...) end

---gets the total number of executed cpu cycles
---
---Example:
---
---	local inemutot = emu.totalexecutedcycles( );
---@return integer
function emu.totalexecutedcycles() end

---allows a script to run while emulation is paused and interact with the gui/main window in realtime 
---
---Example:
---
---	emu.yield( );
function emu.yield() end

