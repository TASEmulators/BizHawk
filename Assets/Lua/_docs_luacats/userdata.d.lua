-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for setting and retrieving dynamic data that will be saved and loaded with savestates
---@class bizuserdata
userdata = {}

---clears all user data
---
---Example:
---
---	userdata.clear( );
function userdata.clear() end

---returns whether or not there is an entry for the given key
---
---Example:
---
---	if ( userdata.containskey( "Unique key" ) ) then
---		console.log( "returns whether or not there is an entry for the given key" );
---	end;
---@param key string
---@return boolean
function userdata.containskey(key) end

---gets the data with the given key, if the key does not exist it will return nil
---
---Example:
---
---	local obuseget = userdata.get( "Unique key" );
---@param key string
---@return any
function userdata.get(key) end

---returns a list-like table of valid keys
---
---Example:
---
---	console.writeline(#userdata.get_keys());
---@return table
function userdata.get_keys() end

---remove the data with the given key. Returns true if the element is successfully found and removed; otherwise, false.
---
---Example:
---
---	if ( userdata.remove( "Unique key" ) ) then
---		console.log( "remove the data with the given key.Returns true if the element is successfully found and removed; otherwise, false." );
---	end;
---@param key string
---@return boolean
function userdata.remove(key) end

---adds or updates the data with the given key with the given value
---
---Example:
---
---	userdata.set("Unique key", "Current key data");
---@param name string
---@param value any
function userdata.set(name, value) end

