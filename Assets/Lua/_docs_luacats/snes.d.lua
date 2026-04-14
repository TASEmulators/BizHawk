-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Functions specific to the SNES system (functions may not run when an SNES game is not loaded)
---@class snes
snes = {}

---Returns whether the bg 1 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_bg_1( ) ) then
---		console.log( "Returns whether the bg 1 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_bg_1() end

---Returns whether the bg 2 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_bg_2( ) ) then
---		console.log( "Returns whether the bg 2 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_bg_2() end

---Returns whether the bg 3 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_bg_3( ) ) then
---		console.log( "Returns whether the bg 3 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_bg_3() end

---Returns whether the bg 4 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_bg_4( ) ) then
---		console.log( "Returns whether the bg 4 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_bg_4() end

---Returns whether the obj 1 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_obj_1( ) ) then
---		console.log( "Returns whether the obj 1 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_obj_1() end

---Returns whether the obj 2 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_obj_2( ) ) then
---		console.log( "Returns whether the obj 2 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_obj_2() end

---Returns whether the obj 3 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_obj_3( ) ) then
---		console.log( "Returns whether the obj 3 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_obj_3() end

---Returns whether the obj 4 layer is displayed
---
---Example:
---
---	if ( snes.getlayer_obj_4( ) ) then
---		console.log( "Returns whether the obj 4 layer is displayed" );
---	end;
---@return boolean
function snes.getlayer_obj_4() end

---Sets whether the bg 1 layer is displayed
---
---Example:
---
---	snes.setlayer_bg_1( true );
---@param value boolean
function snes.setlayer_bg_1(value) end

---Sets whether the bg 2 layer is displayed
---
---Example:
---
---	snes.setlayer_bg_2( true );
---@param value boolean
function snes.setlayer_bg_2(value) end

---Sets whether the bg 3 layer is displayed
---
---Example:
---
---	snes.setlayer_bg_3( true );
---@param value boolean
function snes.setlayer_bg_3(value) end

---Sets whether the bg 4 layer is displayed
---
---Example:
---
---	snes.setlayer_bg_4( true );
---@param value boolean
function snes.setlayer_bg_4(value) end

---Sets whether the obj 1 layer is displayed
---
---Example:
---
---	snes.setlayer_obj_1( true );
---@param value boolean
function snes.setlayer_obj_1(value) end

---Sets whether the obj 2 layer is displayed
---
---Example:
---
---	snes.setlayer_obj_2( true );
---@param value boolean
function snes.setlayer_obj_2(value) end

---Sets whether the obj 3 layer is displayed
---
---Example:
---
---	snes.setlayer_obj_3( true );
---@param value boolean
function snes.setlayer_obj_3(value) end

---Sets whether the obj 4 layer is displayed
---
---Example:
---
---	snes.setlayer_obj_4( true );
---@param value boolean
function snes.setlayer_obj_4(value) end

