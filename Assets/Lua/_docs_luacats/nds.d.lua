-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Functions specific to the NDS system (functions may not run when an NDS game is not loaded)
---@class nds
nds = {}

---Returns the audio bitdepth setting
---
---Example:
---
---	if ( nds.getaudiobitdepth( ) ) then
---		console.log( "Returns the audio bitdepth setting" );
---	end;
---@return string
function nds.getaudiobitdepth() end

---Returns the gap between the screens
---
---Example:
---
---	if ( nds.getscreengap( ) ) then
---		console.log( "Returns the gap between the screens" );
---	end;
---@return integer
function nds.getscreengap() end

---Returns whether screens are inverted
---
---Example:
---
---	if ( nds.getscreeninvert( ) ) then
---		console.log( "Returns whether screens are inverted" );
---	end;
---@return boolean
function nds.getscreeninvert() end

---Returns which screen layout is active
---
---Example:
---
---	if ( nds.getscreenlayout( ) ) then
---		console.log( "Returns which screen layout is active" );
---	end;
---@return string
function nds.getscreenlayout() end

---Returns how screens are rotated
---
---Example:
---
---	if ( nds.getscreenrotation( ) ) then
---		console.log( "Returns how screens are rotated" );
---	end;
---@return string
function nds.getscreenrotation() end

---Sets the audio bitdepth setting
---
---Example:
---
---	nds.setaudiobitdepth( "Auto" );
---@param value string
function nds.setaudiobitdepth(value) end

---Sets the gap between the screens
---
---Example:
---
---	nds.setscreengap( 0 );
---@param value integer
function nds.setscreengap(value) end

---Sets whether screens are inverted
---
---Example:
---
---	nds.setscreeninvert( false );
---@param value boolean
function nds.setscreeninvert(value) end

---Sets which screen layout is active
---
---Example:
---
---	nds.setscreenlayout( "Vertical" );
---@param value string
function nds.setscreenlayout(value) end

---Sets how screens are rotated
---
---Example:
---
---	nds.setscreenrotation( "Rotate0" );
---@param value string
function nds.setscreenrotation(value) end

