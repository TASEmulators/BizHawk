-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Functions specific to Doom games (functions may not run when a Doom game is not loaded)
---@class doom
doom = {}

---Fires when P_CrossCompatibleSpecialLine() is called by a mobj (thing). Your callback can have 2 parameters, which will be pointers to activated line and to mobj that triggered it.
---
---Example:
---
---		local crossline_cb_id = doom.on_cross(function(line, thing)
---			console.log("line "..line.." crossed by mobj "..mobj);
---		end, "Cross notifier");
---@param luaf function
---@param name? string
---@return string
function doom.on_cross(luaf, name) end

---Fires immediately after a new line or thing intercept is added by Doom. Your callback can have 3 parameters: integers identifying x and y position of the map block the intercept happened in, and whether the call is from `PIT_AddThingIntercepts()` (0) or `PIT_AddLineIntercepts()` (1).
---
---Example:
---
---		local intercept_cb_id = doom.on_intercept(function(block)
---			console.log("intercept in block "..intercept);
---		end, "intercept notifier");
---@param luaf function
---@param name? string
---@return string
function doom.on_intercept(luaf, name) end

---Fires immediately after each P_Random() call by Doom. Your callback can have 1 parameter, which will be an integer identifying what kind of object or action made the RNG call.
---
---Example:
---
---		local rngcall_cb_id = doom.on_prandom(function(pr_class)
---			console.log("RNG advanced (class-"..pr_class.." caller)");
---		end, "RNG notifier");
---@param luaf function
---@param name? string
---@return string
function doom.on_prandom(luaf, name) end

---Fires when P_UseSpecialLine() is called by a mobj (thing). Your callback can have 2 parameters, which will be pointers to activated line and to mobj that triggered it.
---
---Example:
---
---		local usesuccess_cb_id = doom.on_use(function(line, thing)
---			console.log("line "..line.." used by mobj "..mobj);
---		end, "Use notifier");
---@param luaf function
---@param name? string
---@return string
function doom.on_use(luaf, name) end

