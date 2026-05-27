-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Functions related specifically to NES Cores
---@class nes
nes = {}

---Gets the NES setting 'Allow more than 8 sprites per scanline' value
---
---Example:
---
---	if ( nes.getallowmorethaneightsprites( ) ) then
---		console.log( "Gets the NES setting 'Allow more than 8 sprites per scanline' value" );
---	end;
---@return boolean
function nes.getallowmorethaneightsprites() end

---Gets the current value for the bottom scanline value
---
---Example:
---
---	local innesget = nes.getbottomscanline( false );
---@param pal? boolean Defaults to `false`
---@return integer
function nes.getbottomscanline(pal) end

---Gets the current value for the Clip Left and Right sides option
---
---Example:
---
---	if ( nes.getclipleftandright( ) ) then
---		console.log( "Gets the current value for the Clip Left and Right sides option" );
---	end;
---@return boolean
function nes.getclipleftandright() end

---Indicates whether or not the bg layer is being displayed
---
---Example:
---
---	if ( nes.getdispbackground( ) ) then
---		console.log( "Indicates whether or not the bg layer is being displayed" );
---	end;
---@return boolean
function nes.getdispbackground() end

---Indicates whether or not sprites are being displayed
---
---Example:
---
---	if ( nes.getdispsprites( ) ) then
---		console.log( "Indicates whether or not sprites are being displayed" );
---	end;
---@return boolean
function nes.getdispsprites() end

---Gets the current value for the top scanline value
---
---Example:
---
---	local innesget = nes.gettopscanline(false);
---@param pal? boolean Defaults to `false`
---@return integer
function nes.gettopscanline(pal) end

---Sets the NES setting 'Allow more than 8 sprites per scanline'
---
---Example:
---
---	nes.setallowmorethaneightsprites( true );
---@param allow boolean
function nes.setallowmorethaneightsprites(allow) end

---Sets the Clip Left and Right sides option
---
---Example:
---
---	nes.setclipleftandright( true );
---@param leftandright boolean
function nes.setclipleftandright(leftandright) end

---Sets whether or not the background layer will be displayed
---
---Example:
---
---	nes.setdispbackground( true );
---@param show boolean
function nes.setdispbackground(show) end

---Sets whether or not sprites will be displayed
---
---Example:
---
---	nes.setdispsprites( true );
---@param show boolean
function nes.setdispsprites(show) end

---sets the top and bottom scanlines to be drawn (same values as in the graphics options dialog). Top must be in the range of 0 to 127, bottom must be between 128 and 239. Not supported in the Quick Nes core
---
---Example:
---
---	nes.setscanlines( 10, 20, false );
---@param top integer
---@param bottom integer
---@param pal? boolean Defaults to `false`
function nes.setscanlines(top, bottom, pal) end

