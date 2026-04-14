-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class movie
movie = {}

---Returns the file name including path of the currently loaded movie
---
---Example:
---
---	local stmovfil = movie.filename( );
---@return string
function movie.filename() end

---If a movie is active, will return the movie comments as a lua table
---
---Example:
---
---	local nlmovget = movie.getcomments( );
---@return table # Zero-indexed array.
function movie.getcomments() end

---If a movie is loaded, gets the frames per second used by the movie to determine the movie length time
---
---Example:
---
---	local domovget = movie.getfps( );
---@return number
function movie.getfps() end

---If a movie is active, will return the movie header as a lua table
---
---Example:
---
---	local nlmovget = movie.getheader( );
---@return table
function movie.getheader() end

---Returns a table of buttons pressed on a given frame of the loaded movie
---
---Example:
---
---	local nlmovget = movie.getinput( 500 );
---@param frame integer
---@param controller? integer
---@return table
function movie.getinput(frame, controller) end

---Returns the input of a given frame of the loaded movie in a raw inputlog string
---
---Example:
---
---	local stmovget = movie.getinputasmnemonic( 500 );
---@param frame integer
---@return string
function movie.getinputasmnemonic(frame) end

---Returns true if the movie is in read-only mode, false if in read+write
---
---Example:
---
---	if ( movie.getreadonly( ) ) then
---		console.log( "Returns true if the movie is in read-only mode, false if in read+write" );
---	end;
---@return boolean
function movie.getreadonly() end

---Gets the rerecord count of the current movie.
---
---Example:
---
---	local ulmovget = movie.getrerecordcount();
---@return integer
function movie.getrerecordcount() end

---Returns whether or not the current movie is incrementing rerecords on loadstate
---
---Example:
---
---	if ( movie.getrerecordcounting( ) ) then
---		console.log( "Returns whether or not the current movie is incrementing rerecords on loadstate" );
---	end;
---@return boolean
function movie.getrerecordcounting() end

---If a movie is active, will return the movie subtitles as a lua table
---
---Example:
---
---	local nlmovget = movie.getsubtitles( );
---@return table # Zero-indexed array.
function movie.getsubtitles() end

---Returns true if a movie is loaded in memory (play, record, or finished modes), false if not (inactive mode)
---
---Example:
---
---	if ( movie.isloaded( ) ) then
---		console.log( "Returns true if a movie is loaded in memory ( play, record, or finished modes ), false if not ( inactive mode )" );
---	end;
---@return boolean
function movie.isloaded() end

---Returns the total number of frames of the loaded movie
---
---Example:
---
---	local domovlen = movie.length( );
---@return integer
function movie.length() end

---Returns the mode of the current movie. Possible modes: "PLAY", "RECORD", "FINISHED", "INACTIVE"
---
---Example:
---
---	local stmovmod = movie.mode( );
---@return string
function movie.mode() end

---Resets the core to frame 0 with the currently loaded movie in playback mode. If a path to a movie is specified, attempts to load it, then continues with playback if it was successful. Returns true iff successful.
---
---Example:
---
---	movie.play_from_start("C:\\moviename.ext");
---@param path? string
---@return boolean
function movie.play_from_start(path) end

---Saves the current movie to the disc. If the filename is provided (no extension or path needed), the movie is saved under the specified name to the current movie directory. The filename may contain a subdirectory, it will be created if it doesn't exist. Existing files won't get overwritten.
---
---Example:
---
---	movie.save( "C:\moviename.ext" );
---@param filename? string
function movie.save(filename) end

---Sets the read-only state to the given value. true for read only, false for read+write
---
---Example:
---
---	movie.setreadonly( false );
---@param readOnly boolean
function movie.setreadonly(readOnly) end

---Sets the rerecord count of the current movie.
---
---Example:
---
---	movie.setrerecordcount( 20 );
---@param count integer
function movie.setrerecordcount(count) end

---Sets whether or not the current movie will increment the rerecord counter on loadstate
---
---Example:
---
---	movie.setrerecordcounting( true );
---@param counting boolean
function movie.setrerecordcounting(counting) end

---Returns whether or not the movie is a saveram-anchored movie
---
---Example:
---
---	if ( movie.startsfromsaveram( ) ) then
---		console.log( "Returns whether or not the movie is a saveram-anchored movie" );
---	end;
---@return boolean
function movie.startsfromsaveram() end

---Returns whether or not the movie is a savestate-anchored movie
---
---Example:
---
---	if ( movie.startsfromsavestate( ) ) then
---		console.log( "Returns whether or not the movie is a savestate-anchored movie" );
---	end;
---@return boolean
function movie.startsfromsavestate() end

---Stops the current movie. Pass false to discard changes.
---
---Example:
---
---	movie.stop( );
---@param saveChanges? boolean Defaults to `true`
function movie.stop(saveChanges) end

