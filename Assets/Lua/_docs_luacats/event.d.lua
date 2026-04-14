-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for registering lua functions to emulator events.
--- All events support multiple registered methods.
---All registered event methods can be named and return a Guid when registered
---@class event
event = {}

---Lists the available scopes that can be specified for on_bus_* events
---
---Example:
---
---	local scopes = event.availableScopes();
---@return table # Zero-indexed array.
function event.availableScopes() end

---Returns whether EmuHawk will pass arguments to callbacks. The current version passes arguments to "memory" callbacks (RAM/ROM/bus R/W), so this function will return true for that input. (It returns false for any other input.) This tells you whether it's necessary to enable workarounds/hacks because a script is running in a version without parameter support.
---
---Example:
---
---	local mem_callback = event.can_use_callback_params("memory") and mem_callback or mem_callback_pre_29;
---@param subset? string
---@return boolean
function event.can_use_callback_params(subset) end

---Fires immediately before the given address is executed by the core. Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value to be executed (or `0` always, if this feature is only partially implemented).
---
---Example:
---
---	local exec_cb_id = event.on_bus_exec(
---		function(addr, val, flags)
---			console.log( "Fires immediately before the given address is executed by the core. `val` is the value to be executed (or `0` always, if this feature is only partially implemented)." );
---		end
---		, 0x200, "Frame name", "System Bus" );
---@param luaf function
---@param address integer
---@param name? string
---@param scope? string
---@return string
function event.on_bus_exec(luaf, address, name, scope) end

---Fires immediately before every instruction executed (in the specified scope) by the core (CPU-intensive). Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value to be executed (or `0` always, if this feature is only partially implemented).
---
---Example:
---
---	local exec_cb_id = event.on_bus_exec_any(
---		function(addr, val, flags)
---			console.log( "Fires immediately before every instruction executed (in the specified scope) by the core (CPU-intensive). `val` is the value to be executed (or `0` always, if this feature is only partially implemented)." );
---		end
---		, "Frame name", "System Bus" );
---@param luaf function
---@param name? string
---@param scope? string
---@return string
function event.on_bus_exec_any(luaf, name, scope) end

---Fires immediately before the given address is read by the core. Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value read. If no address is given, it will fire on every memory read.
---
---Example:
---
---	local exec_cb_id = event.on_bus_read(
---		function(addr, val, flags)
---			console.log( "Fires immediately before the given address is read by the core. `val` is the value read. If no address is given, it will fire on every memory read." );
---		end
---		, 0x200, "Frame name" );
---@param luaf function
---@param address? integer
---@param name? string
---@param scope? string
---@return string
function event.on_bus_read(luaf, address, name, scope) end

---Fires immediately before the given address is written by the core. Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value to be written (or `0` always, if this feature is only partially implemented). If no address is given, it will fire on every memory write.
---
---Example:
---
---	local exec_cb_id = event.on_bus_write(
---		function(addr, val, flags)
---			console.log( "Fires immediately before the given address is written by the core. `val` is the value to be written (or `0` always, if this feature is only partially implemented). If no address is given, it will fire on every memory write." );
---		end
---		, 0x200, "Frame name" );
---@param luaf function
---@param address? integer
---@param name? string
---@param scope? string
---@return string
function event.on_bus_write(luaf, address, name, scope) end

---Fires when the emulator console closes
---
---Example:
---
---	local closeGuid = event.onconsoleclose(
---		function()
---			console.log( "Fires when the emulator console closes" );
---		end
---		, "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.onconsoleclose(luaf, name) end

---Fires after the calling script has stopped
---
---Example:
---
---	local steveone = event.onexit(
---		function()
---			console.log( "Fires after the calling script has stopped" );
---		end
---		, "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.onexit(luaf, name) end

---Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts
---
---Example:
---
---	local steveonf = event.onframeend(
---		function()
---			console.log( "Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts" );
---		end
---		, "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.onframeend(luaf, name) end

---Calls the given lua function at the beginning of each frame before any emulation and drawing occurs
---
---Example:
---
---	local steveonf = event.onframestart(
---		function()
---			console.log( "Calls the given lua function at the beginning of each frame before any emulation and drawing occurs" );
---		end
---		, "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.onframestart(luaf, name) end

---Calls the given lua function after each time the emulator core polls for input
---
---Example:
---
---	local steveoni = event.oninputpoll(
---		function()
---			console.log( "Calls the given lua function after each time the emulator core polls for input" );
---		end
---		, "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.oninputpoll(luaf, name) end

---Fires after a state is loaded. Your callback can have 1 parameter, which will be the name of the loaded state.
---
---Example:
---
---	local steveonl = event.onloadstate(
---		function()
---		console.log( "Fires after a state is loaded. Receives a lua function name, and registers it to the event immediately following a successful savestate event" );
---	end", "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.onloadstate(luaf, name) end

---Fires immediately before the given address is executed by the core. Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value to be executed (or `0` always, if this feature is only partially implemented).
---@deprecated
---@param luaf function
---@param address integer
---@param name? string
---@param scope? string
---@return string
function event.onmemoryexecute(luaf, address, name, scope) end

---Fires immediately before every instruction executed (in the specified scope) by the core (CPU-intensive). Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value to be executed (or `0` always, if this feature is only partially implemented).
---@deprecated
---@param luaf function
---@param name? string
---@param scope? string
---@return string
function event.onmemoryexecuteany(luaf, name, scope) end

---Fires immediately before the given address is read by the core. Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value read. If no address is given, it will fire on every memory read.
---@deprecated
---@param luaf function
---@param address? integer
---@param name? string
---@param scope? string
---@return string
function event.onmemoryread(luaf, address, name, scope) end

---Fires immediately before the given address is written by the core. Your callback can have 3 parameters `(addr, val, flags)`. `val` is the value to be written (or `0` always, if this feature is only partially implemented). If no address is given, it will fire on every memory write.
---@deprecated
---@param luaf function
---@param address? integer
---@param name? string
---@param scope? string
---@return string
function event.onmemorywrite(luaf, address, name, scope) end

---Fires after a state is saved. Your callback can have 1 parameter, which will be the name of the saved state.
---
---Example:
---
---	local steveons = event.onsavestate(
---		function()
---			console.log( "Fires after a state is saved" );
---		end
---		, "Frame name" );
---@param luaf function
---@param name? string
---@return string
function event.onsavestate(luaf, name) end

---Removes the registered function that matches the guid. If a function is found and remove the function will return true. If unable to find a match, the function will return false.
---
---Example:
---
---	if ( event.unregisterbyid( "4d1810b7 - 0d28 - 4acb - 9d8b - d87721641551" ) ) then
---		console.log( "Removes the registered function that matches the guid.If a function is found and remove the function will return true.If unable to find a match, the function will return false." );
---	end;
---@param guid string
---@return boolean
function event.unregisterbyid(guid) end

---Removes the first registered function that matches Name. If a function is found and remove the function will return true. If unable to find a match, the function will return false.
---
---Example:
---
---	if ( event.unregisterbyname( "Function name" ) ) then
---		console.log( "Removes the first registered function that matches Name.If a function is found and remove the function will return true.If unable to find a match, the function will return false." );
---	end;
---@param name string
---@return boolean
function event.unregisterbyname(name) end

