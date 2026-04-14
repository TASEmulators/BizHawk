-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for manipulating the Tastudio dialog of the EmuHawk client
---@class tastudio
tastudio = {}

---Extends the piano roll with an extra column for data visualisation. The text parameter is used as the column header, while the name parameter is used to identify the column for `onqueryitem*` callbacks. And width is obviously the width (in dp).
---
---Example:
---
---		local cache = { [0] = "0" };
---		tastudio.onqueryitemtext(function(frame, col)
---			if col == "xp" then return cache[frame]; end
---		end);
---		tastudio.addcolumn("xp", "Experience", 30);
---		--TODO each frame, set `cache[emu.framecount()]` to e.g. `tostring(memory.readbyte(addr_of_xp))`
---		--TODO on loadstate, clear `cache` from `emu.framecount()` through to end
---		-- or you could try to de/serialise the `cache` table to a string and sync it via `userdata`
---@param name string
---@param text string
---@param width integer
function tastudio.addcolumn(name, text, width) end

---Applies the queued edit operations to the TAStudio project as a single batch. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitinputchange(10000, "P1 A", true);
---		tastudio.applyinputchanges();
function tastudio.applyinputchanges() end

---Clears the cache that is built up by using `tastudio.onqueryitemicon`, so that changes to the icons on disk can be picked up.
---
---Example:
---
---	tastudio.clearIconCache();
function tastudio.clearIconCache() end

---Discards any edits queued for the TAStudio project by scripts. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitinputchange(10000, "P1 A", true);
---		tastudio.clearinputchanges();
---		tastudio.applyinputchanges(); -- does nothing
function tastudio.clearinputchanges() end

---creates a new branch at the current frame
---
---Example:
---
---		tastudio.createnewbranch();
function tastudio.createnewbranch() end

---returns whether or not tastudio is currently engaged (active)
---
---Example:
---
---	if ( tastudio.engaged( ) ) then
---		console.log( "returns whether or not tastudio is currently engaged ( active )" );
---	end;
---@return boolean
function tastudio.engaged() end

---Returns the frame number of the marker closest to the given frame (including that frame, but not after it). This may be the power-on marker at 0. Returns nil if the arguments are invalid or TAStudio isn't active. If branchID is specified, searches the markers in that branch instead.
---
---Example:
---
---		local marker_label = tastudio.getmarker(tastudio.find_marker_on_or_before(100));
---@param frame integer
---@param branchID? string
---@return integer
function tastudio.find_marker_on_or_before(frame, branchID) end

---Finds the branch with the given UUID (0-indexed). Returns nil if not found.
---
---Example:
---
---		tastudio.setbranchtext("New label", tastudio.get_branch_index_by_id(branch_id));
---@param id string
---@return integer
function tastudio.get_branch_index_by_id(id) end

---Returns a list of all the frames which have markers on them. If branchID is specified, instead returns the frames which have markers in that branch.
---
---Example:
---
---		local marker_label = tastudio.getmarker(tastudio.get_frames_with_markers()[2]);
---@param branchID? string
---@return table
function tastudio.get_frames_with_markers(branchID) end

---Returns a list of the current tastudio branches.  Each entry will have the Id, Frame, and Text properties of the branch
---
---Example:
---
---	local nltasget = tastudio.getbranches( );
---@return table # Zero-indexed array.
function tastudio.getbranches() end

---Gets the controller state of the given frame with the given branch identifier
---
---Example:
---
---	local nltasget = tastudio.getbranchinput( "97021544-2454-4483-824f-47f75e7fcb6a", 500 );
---@param branchId string
---@param frame integer
---@return table
function tastudio.getbranchinput(branchId, frame) end

---Returns the label of the marker on the given frame. This may be an empty string. If that frame doesn't have a marker (or TAStudio isn't running), returns nil. If branchID is specified, searches the markers in that branch instead.
---
---Example:
---
---	local sttasget = tastudio.getmarker( 500 );
---@param frame integer
---@param branchID? string
---@return string
function tastudio.getmarker(frame, branchID) end

---returns whether or not TAStudio is in recording mode
---
---Example:
---
---	if ( tastudio.getrecording( ) ) then
---		console.log( "returns whether or not TAStudio is in recording mode" );
---	end;
---@return boolean
function tastudio.getrecording() end

---Gets the frame that TAStudio is seeking to, or the current frame is no seek is in progress.
---
---Example:
---
---	local famesLeftToSeek = tastudio.getseekframe() - emu.framecount();
---@return integer
function tastudio.getseekframe() end

---gets the currently selected frames
---
---Example:
---
---	local nltasget = tastudio.getselection( );
---@return table # Zero-indexed array.
function tastudio.getselection() end

---Returns whether or not the given frame has a savestate associated with it
---
---Example:
---
---	if ( tastudio.hasstate( 500 ) ) then
---		console.log( "Returns whether or not the given frame has a savestate associated with it" );
---	end;
---@param frame integer
---@return boolean
function tastudio.hasstate(frame) end

---Returns whether or not the given frame was a lag frame, null if unknown
---
---Example:
---
---	local botasisl = tastudio.islag( 500 );
---@param frame integer
---@return boolean
function tastudio.islag(frame) end

---Loads a branch at the given index, if a branch at that index exists.
---
---Example:
---
---	tastudio.loadbranch(0)
---@param index integer
function tastudio.loadbranch(index) end

---called whenever a branch is loaded. luaf must be a function that takes the integer branch index as a parameter
---
---Example:
---
---	tastudio.onbranchload( function( currentindex )
---		console.log( "Called whenever a branch is loaded." );
---	end );
---@param luaf function
---@param name? string
---@return string
function tastudio.onbranchload(luaf, name) end

---Sets a callback which fires after any branch is removed. Your callback can have 1 parameter, which will be the index of the branch. Calling this function a second time will replace the existing callback with the new one.
---
---Example:
---
---		tastudio.onbranchremove(function(branch_index) console.writeline(branch_index); end);
---@param luaf function
---@param name? string
---@return string
function tastudio.onbranchremove(luaf, name) end

---Sets a callback which fires after any branch is created or updated. Your callback can have 1 parameter, which will be the index of the branch. Calling this function a second time will replace the existing callback with the new one.
---
---Example:
---
---		tastudio.onbranchsave(function(branch_index) console.writeline(branch_index); end);
---@param luaf function
---@param name? string
---@return string
function tastudio.onbranchsave(luaf, name) end

---Called whenever the movie is modified in a way that could invalidate savestates in the movie's state history. Called regardless of whether any states were actually invalidated. Your callback can have 1 parameter, which will be the last frame before the invalidated ones. That is, the first of the modified frames.
---
---Example:
---
---	tastudio.ongreenzoneinvalidated( function( currentindex )
---		console.log( "Called whenever the greenzone is invalidated." );
---	end );
---@param luaf function
---@param name? string
---@return string
function tastudio.ongreenzoneinvalidated(luaf, name) end

---called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)
---
---Example:
---
---	tastudio.onqueryitembg( function( currentindex, itemname )
---		console.log( "called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)" );
---	end );
---@param luaf function
---@param name? string
---@return string
function tastudio.onqueryitembg(luaf, name) end

---Called during the icon draw event of the tastudio listview. `luaf` must be a function that takes 2 params: `(index, column)`. The first is the integer row index of the listview, and the 2nd is the string column name. The callback should return a string, the path to the `.ico` file to be displayed. The file will be cached, so if you change the file on disk, call `tastudio.clearIconCache()`.
---
---Example:
---
---	tastudio.onqueryitemicon( function( currentindex, itemname )
---		console.log( "called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)" );
---	end );
---@param luaf function
---@param name? string
---@return string
function tastudio.onqueryitemicon(luaf, name) end

---Called during the text draw event of the tastudio listview. `luaf` must be a function that takes 2 params: `(index, column)`. The first is the integer row index of the listview, and the 2nd is the string column name. The callback should return a string to be displayed.
---
---Example:
---
---	tastudio.onqueryitemtext( function( currentindex, itemname )
---		console.log( "called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)" );
---	end );
---@param luaf function
---@param name? string
---@return string
function tastudio.onqueryitemtext(luaf, name) end

---if there is a marker for the given frame, it will be removed
---
---Example:
---
---	tastudio.removemarker( 500 );
---@param frame integer
function tastudio.removemarker(frame) end

---adds the given message to the existing branch, or to the branch that will be created next if branch index is not specified
---
---Example:
---
---	tastudio.setbranchtext( "Some text", 1 );
---@param text string
---@param index? integer
function tastudio.setbranchtext(text, index) end

---Sets the lag information for the given frame, if the frame does not exist in the lag log, it will be added. If the value is null, the lag information for that frame will be removed
---
---Example:
---
---	tastudio.setlag( 500, true );
---@param frame integer
---@param value? boolean
function tastudio.setlag(frame, value) end

---Adds or sets a marker at the given frame, with an optional message
---
---Example:
---
---	tastudio.setmarker( 500, "Some message" );
---@param frame integer
---@param message? string | number
function tastudio.setmarker(frame, message) end

---Seeks the given frame (a number) or marker (a string)
---
---Example:
---
---	tastudio.setplayback( 1500 );
---@param frame any
function tastudio.setplayback(frame) end

---sets the recording mode on/off depending on the parameter
---
---Example:
---
---	tastudio.setrecording( true );
---@param val boolean
function tastudio.setrecording(val) end

---Queues a change axis value operation for the frame specified. Edits will take effect once you call `tastudio.applyinputchanges`. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitanalogchange(10000, "P1 Stick X", 127);
---		tastudio.applyinputchanges();
---@param frame integer
---@param button string
---@param value number
function tastudio.submitanalogchange(frame, button, value) end

---Queues a clear operation for the specified number of frames, from the frame specified through to `frame + number - 1`. Edits will take effect once you call `tastudio.applyinputchanges`. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitclearframes(10000, 5);
---		tastudio.applyinputchanges();
---@param frame integer
---@param number integer
function tastudio.submitclearframes(frame, number) end

---Queues a delete operation for the specified number of frames, from the frame specified through to `frame + number - 1`. Edits will take effect once you call `tastudio.applyinputchanges`. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitdeleteframes(10000, 5);
---		tastudio.applyinputchanges();
---@param frame integer
---@param number integer
function tastudio.submitdeleteframes(frame, number) end

---Queues a hold/release button operation for the frame specified. Edits will take effect once you call `tastudio.applyinputchanges`. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitinputchange(10000, "P1 A", true);
---		tastudio.applyinputchanges();
---@param frame integer
---@param button string
---@param value boolean
function tastudio.submitinputchange(frame, button, value) end

---Queues an insert operation, creating the specified number of frames (rows) immediately before the frame specified. Edits will take effect once you call `tastudio.applyinputchanges`. (For technical reasons, the queue is shared between all loaded scripts.)
---
---Example:
---
---		tastudio.submitinsertframes(10000, 5);
---		tastudio.applyinputchanges();
---@param frame integer
---@param number integer
function tastudio.submitinsertframes(frame, number) end

---toggles tastudio recording mode on/off depending on its current state
---
---Example:
---
---	tastudio.togglerecording( );
function tastudio.togglerecording() end

