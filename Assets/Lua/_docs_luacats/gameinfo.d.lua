-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class gameinfo
gameinfo = {}

---returns identifying information about the 'mapper' or similar capability used for this game.  empty if no such useful distinction can be drawn
---
---Example:
---
---	local stgamget = gameinfo.getboardtype( );
---@return string
function gameinfo.getboardtype() end

---returns the game options for the currently loaded rom. Options vary per platform
---
---Example:
---
---	local nlgamget = gameinfo.getoptions( );
---@return table
function gameinfo.getoptions() end

---returns the hash of the currently loaded rom, if a rom is loaded
---
---Example:
---
---	local stgamget = gameinfo.getromhash( );
---@return string
function gameinfo.getromhash() end

---returns the name of the currently loaded rom, if a rom is loaded
---
---Example:
---
---	local stgamget = gameinfo.getromname( );
---@return string
function gameinfo.getromname() end

---returns the game database status of the currently loaded rom. Statuses are for example: GoodDump, BadDump, Hack, Unknown, NotInDatabase
---
---Example:
---
---	local stgamget = gameinfo.getstatus( );
---@return string
function gameinfo.getstatus() end

---returns whether or not the currently loaded rom is in the game database
---
---Example:
---
---	if ( gameinfo.indatabase( ) ) then
---		console.log( "returns whether or not the currently loaded rom is in the game database" );
---	end;
---@return boolean
function gameinfo.indatabase() end

---returns the currently loaded rom's game database status is considered 'bad'
---
---Example:
---
---	if ( gameinfo.isstatusbad( ) ) then
---		console.log( "returns the currently loaded rom's game database status is considered 'bad'" );
---	end;
---@return boolean
function gameinfo.isstatusbad() end

