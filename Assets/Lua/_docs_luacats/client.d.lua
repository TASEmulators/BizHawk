-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for manipulating the EmuHawk client UI
---@class client
client = {}

---adds a cheat code, if supported
---
---Example:
---
---	client.addcheat("NNNPAK");
---@param code string
function client.addcheat(code) end

---Gets the current height in pixels of the letter/pillarbox area (top side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.
---
---Example:
---
---	local inclibor = client.borderheight( );
---@return integer
function client.borderheight() end

---Gets the current width in pixels of the letter/pillarbox area (left side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.
---
---Example:
---
---	local inclibor = client.borderwidth( );
---@return integer
function client.borderwidth() end

---Gets the visible height of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.
---
---Example:
---
---	local inclibuf = client.bufferheight( );
---@return integer
function client.bufferheight() end

---Gets the visible width of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.
---
---Example:
---
---	local inclibuf = client.bufferwidth( );
---@return integer
function client.bufferwidth() end

---Clears all autohold keys
---
---Example:
---
---	client.clearautohold( );
function client.clearautohold() end

---Closes the loaded Rom
---
---Example:
---
---	client.closerom( );
function client.closerom() end

---returns a default instance of the given type of object if it exists (not case sensitive). Note: This will only work on objects which have a parameterless constructor.  If no suitable type is found, or the type does not have a parameterless constructor, then nil is returned
---
---Example:
---
---	local nlclicre = client.createinstance( "objectname" );
---@param name string
---@return table
function client.createinstance(name) end

---sets whether or not on screen messages will display
---
---Example:
---
---	client.displaymessages( true );
---@param value boolean
function client.displaymessages(value) end

---Sets whether or not the rewind feature is enabled
---
---Example:
---
---	client.enablerewind( true );
---@param enabled boolean
function client.enablerewind(enabled) end

---sleeps exactly for n milliseconds
---
---Example:
---
---	client.exactsleep( 50 );
---@param millis integer
function client.exactsleep(millis) end

---Closes the emulator
---
---Example:
---
---	client.exit( );
function client.exit() end

---Closes the emulator and returns the provided code
---
---Example:
---
---	client.exitCode( 0 );
---@param exitCode integer
function client.exitCode(exitCode) end

---Sets the frame skip value of the client UI (use 0 to disable)
---
---Example:
---
---	client.frameskip( 8 );
---@param numFrames integer
function client.frameskip(numFrames) end

---Gets the (host) framerate, approximated from frame durations.
---
---Example:
---
---	local sounds_terrible = client.get_approx_framerate() < 55;
---@return integer
function client.get_approx_framerate() end

---returns the name of the Lua engine currently in use
---@return string
function client.get_lua_engine() end

---Returns a list of the tools currently open
---
---Example:
---
---	local nlcliget = client.getavailabletools( );
---@return table # Zero-indexed array.
function client.getavailabletools() end

---gets the current config settings object
---
---Example:
---
---	local curSpeed = client.getconfig().SpeedPercent
---@return any
function client.getconfig() end

---returns a list of implemented functions
---
---Example:
---
---	local stconget = client.getluafunctionslist( );
---@return string
function client.getluafunctionslist() end

---Gets the state of the Sound On toggle
---
---Example:
---
---	if ( client.GetSoundOn( ) ) then
---		console.log( "Gets the state of the Sound On toggle" );
---	end;
---@return boolean
function client.GetSoundOn() end

---Gets the current scanline intensity setting, used for the scanline display filter
---
---Example:
---
---	local incliget = client.gettargetscanlineintensity( );
---@return integer
function client.gettargetscanlineintensity() end

---Returns an object that represents a tool of the given name (not case sensitive). If the tool is not open, it will be loaded if available. Use getavailabletools to get a list of names
---
---Example:
---
---	local nlcliget = client.gettool( "Tool name" );
---@param name string
---@return table
function client.gettool(name) end

---Returns the current stable BizHawk version
---
---Example:
---
---	local incbhver = client.getversion( );
---@return string
function client.getversion() end

---Gets the main window's size Possible values are 1, 2, 3, 4, 5, and 10
---
---Example:
---
---	local incliget = client.getwindowsize( );
---@return integer
function client.getwindowsize() end

---Enters/exits turbo mode and disables/enables most emulator updates.
---
---Example:
---
---	client.invisibleemulation( true );
---@param invisible boolean
function client.invisibleemulation(invisible) end

---Returns true iff the frontend is rewinding.
---
---Example:
---
---		local speed = read_lateral_speed();
---		if (client.is_rewinding()) then speed = -speed; end
---		gui.text(0, 100, tostring(speed));
---@return boolean
function client.is_rewinding() end

---Returns true if emulator is paused, otherwise, false
---
---Example:
---
---	if ( client.ispaused( ) ) then
---		console.log( "Returns true if emulator is paused, otherwise, false" );
---	end;
---@return boolean
function client.ispaused() end

---Returns true if emulator is seeking, otherwise, false
---
---Example:
---
---	if ( client.isseeking( ) ) then
---		console.log( "Returns true if emulator is seeking, otherwise, false" );
---	end;
---@return boolean
function client.isseeking() end

---Returns true if emulator is in turbo mode, otherwise, false
---
---Example:
---
---	if ( client.client.isturbo( ) ) then
---		console.log( "Returns true if emulator is in turbo mode, otherwise, false" );
---	end;
---@return boolean
function client.isturbo() end

---opens the Cheats dialog
---
---Example:
---
---	client.opencheats( );
function client.opencheats() end

---opens the Hex Editor dialog
---
---Example:
---
---	client.openhexeditor( );
function client.openhexeditor() end

---opens the RAM Search dialog
---
---Example:
---
---	client.openramsearch( );
function client.openramsearch() end

---opens the RAM Watch dialog
---
---Example:
---
---	client.openramwatch( );
function client.openramwatch() end

---Loads a ROM from the given path. Returns true if the ROM was successfully loaded, otherwise false.
---
---Example:
---
---	client.openrom( "C:\rom.bin" );
---@param path string
---@return boolean
function client.openrom(path) end

---opens the TAStudio dialog
---
---Example:
---
---	client.opentasstudio( );
function client.opentasstudio() end

---opens the Toolbox Dialog
---
---Example:
---
---	client.opentoolbox( );
function client.opentoolbox() end

---opens the tracelogger if it is available for the given core
---
---Example:
---
---	client.opentracelogger( );
function client.opentracelogger() end

---Pauses the emulator
---
---Example:
---
---	client.pause( );
function client.pause() end

---If currently capturing Audio/Video, this will suspend the record. Frames will not be captured into the AV until client.unpause_av() is called
---
---Example:
---
---	client.pause_av( );
function client.pause_av() end

---Reboots the currently loaded core
---
---Example:
---
---	client.reboot_core( );
function client.reboot_core() end

---removes a cheat, if it already exists
---
---Example:
---
---	client.removecheat("NNNPAK");
---@param code string
function client.removecheat(code) end

---flushes save ram to disk
---
---Example:
---
---	client.saveram( );
function client.saveram() end

---Gets the current height in pixels of the emulator's drawing area
---
---Example:
---
---	local incliscr = client.screenheight( );
---@return integer
function client.screenheight() end

---if a parameter is passed it will function as the Screenshot As menu item of EmuHawk, else it will function as the Screenshot menu item
---
---Example:
---
---	client.screenshot( "C:\" );
---@param path? string
function client.screenshot(path) end

---Performs the same function as EmuHawk's Screenshot To Clipboard menu item
---
---Example:
---
---	client.screenshottoclipboard( );
function client.screenshottoclipboard() end

---Gets the current width in pixels of the emulator's drawing area
---
---Example:
---
---	local incliscr = client.screenwidth( );
---@return integer
function client.screenwidth() end

---Does nothing. Use the pause/unpause functions instead and a loop that waits for the desired frame.
---@deprecated
---@param frame integer
function client.seekframe(frame) end

---Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
---
---Example:
---
---	client.SetClientExtraPadding( 5, 10, 15, 20 );
---@param left integer
---@param top integer
---@param right integer
---@param bottom integer
function client.SetClientExtraPadding(left, top, right, bottom) end

---Sets the extra padding added to the 'emu' surface so that you can draw HUD elements in predictable placements
---
---Example:
---
---	client.SetGameExtraPadding( 5, 10, 15, 20 );
---@param left integer
---@param top integer
---@param right integer
---@param bottom integer
function client.SetGameExtraPadding(left, top, right, bottom) end

---Sets the screenshot Capture OSD property of the client
---
---Example:
---
---	client.setscreenshotosd( true );
---@param value boolean
function client.setscreenshotosd(value) end

---Sets the state of the Sound On toggle
---
---Example:
---
---	client.SetSoundOn( true );
---@param enable boolean
function client.SetSoundOn(enable) end

---Sets the current scanline intensity setting, used for the scanline display filter
---
---Example:
---
---	client.settargetscanlineintensity( -1000 );
---@param val integer
function client.settargetscanlineintensity(val) end

---Sets the main window's size to the give value. Accepted values are 1, 2, 3, 4, 5, and 10
---
---Example:
---
---	client.setwindowsize( 100 );
---@param size integer
function client.setwindowsize(size) end

---sleeps for n milliseconds
---
---Example:
---
---	client.sleep( 50 );
---@param millis integer
function client.sleep(millis) end

---Sets the speed of the emulator (in terms of percent)
---
---Example:
---
---	client.speedmode( 75 );
---@param percent integer
function client.speedmode(percent) end

---Toggles the current pause state
---
---Example:
---
---	client.togglepause( );
function client.togglepause() end

---Transforms a point (x, y) in emulator space to a point in client space
---
---Example:
---
---	local newY = client.transform_point( 32, 100 ).y;
---@param x integer
---@param y integer
---@return table
function client.transformPoint(x, y) end

---Unpauses the emulator. Note that the user can pause again before the next frame, either with the pause key or by releasing frame advance. If you want to force emulation to continue, put this and emu.yield (not frameadvance) inside a loop that runs until the desired frame.
---
---Example:
---
---	client.unpause( );
function client.unpause() end

---If currently capturing Audio/Video this resumes capturing
---
---Example:
---
---	client.unpause_av( );
function client.unpause_av() end

---Returns the x value of the screen position where the client currently sits
---
---Example:
---
---	local inclixpo = client.xpos( );
---@return integer
function client.xpos() end

---Returns the y value of the screen position where the client currently sits
---
---Example:
---
---	local incliypo = client.ypos( );
---@return integer
function client.ypos() end

