-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Functions specific to the Genesis system (functions may not run when a Genesis game is not loaded)
---@class genesis
genesis = {}

---Adds an address to deepfreeze to a given value. The value will not change at any point during emulation.
---
---Example:
---
---	genesis.add_deepfreeze_value( 0xFF00, 0x01 );
---@param address integer
---@param value integer
---@return integer
function genesis.add_deepfreeze_value(address, value) end

---Clears the list of deep frozen variables
---
---Example:
---
---	genesis.clear_deepfreeze_list();
function genesis.clear_deepfreeze_list() end

---Returns whether the bg layer A is displayed
---
---Example:
---
---	if ( genesis.getlayer_bga( ) ) then
---		console.log( "Returns whether the bg layer A is displayed" );
---	end;
---@return boolean
function genesis.getlayer_bga() end

---Returns whether the bg layer B is displayed
---
---Example:
---
---	if ( genesis.getlayer_bgb( ) ) then
---		console.log( "Returns whether the bg layer B is displayed" );
---	end;
---@return boolean
function genesis.getlayer_bgb() end

---Returns whether the bg layer W is displayed
---
---Example:
---
---	if ( genesis.getlayer_bgw( ) ) then
---		console.log( "Returns whether the bg layer W is displayed" );
---	end;
---@return boolean
function genesis.getlayer_bgw() end

---Sets whether the bg layer A is displayed
---
---Example:
---
---	genesis.setlayer_bga( true );
---@param value boolean
function genesis.setlayer_bga(value) end

---Sets whether the bg layer B is displayed
---
---Example:
---
---	genesis.setlayer_bgb( true );
---@param value boolean
function genesis.setlayer_bgb(value) end

---Sets whether the bg layer W is displayed
---
---Example:
---
---	genesis.setlayer_bgw( true );
---@param value boolean
function genesis.setlayer_bgw(value) end

