-- M64 reader script
-- Translates M64 file movies into button presses for Bizhawk, accounting for lag frames.
-- This script will automatically pause at the end of the movie

-- This script will not clear the saveram. If a movie requires empty saveram it must be cleared before using this script.

-- If you are trying to convert a M64 into BKM format, you can try pausing the emulator and starting recording a movie, then loading this script and letting it run. Beginning a new movie will clear the saveram for you.


-- Change this filename to be the .m64 file you want to play. Lua will look for the movie file right next to this script unless a directory location is given.
local m64_filename = "CHANGE_ME.m64"


-- Open the file and read past the header data
local input_file = assert(io.open(m64_filename, "rb"))
local data = input_file:read(0x400)

-- Flag to note that we've reached the end of the movie
local finished = false

-- Since m64 movies do not record on lag frames, we need to know if the input was actually used for the current frame
local input_was_used = false
function input_used()
	if not finished then
   input_was_used = true
  end
end
event.oninputpoll(input_used)

local buttons = { }
local X
local Y
-- Reads in the next frame of data from the movie, or sets the finished flag if no frames are left
function read_next_frame()
  data = input_file:read(4)
  if not data then 
    finished = true
    return
  end
  local byte = string.byte(string.sub(data,1,1))
  if bit.band(byte,0x01) ~= 0 then
    buttons["DPad R"] = true
  else
    buttons["DPad R"] = false
  end
  if bit.band(byte,0x02) ~= 0 then
    buttons["DPad L"] = true
  else
    buttons["DPad L"] = false
  end
  if bit.band(byte,0x04) ~= 0 then
    buttons["DPad D"] = true
  else
    buttons["DPad D"] = false
  end
  if bit.band(byte,0x08) ~= 0 then
    buttons["DPad U"] = true
  else
    buttons["DPad U"] = false
  end
  if bit.band(byte,0x10) ~= 0 then
    buttons["Start"] = true
  else
    buttons["Start"] = false
  end
  if bit.band(byte,0x20) ~= 0 then
    buttons["Z"] = true
  else
    buttons["Z"] = false
  end
  if bit.band(byte,0x40) ~= 0 then
    buttons["B"] = true
  else
    buttons["B"] = false
  end
  if bit.band(byte,0x80) ~= 0 then
    buttons["A"] = true
  else
    buttons["A"] = false
  end
  
  byte = string.byte(string.sub(data,2,2))
  if bit.band(byte,0x01) ~= 0 then
    buttons["C Right"] = true
  else
    buttons["C Right"] = false
  end
  if bit.band(byte,0x02) ~= 0 then
    buttons["C Left"] = true
  else
    buttons["C Left"] = false
  end
  if bit.band(byte,0x04) ~= 0 then
    buttons["C Down"] = true
  else
    buttons["C Down"] = false
  end
  if bit.band(byte,0x08) ~= 0 then
    buttons["C Up"] = true
  else
    buttons["C Up"] = false
  end
  if bit.band(byte,0x10) ~= 0 then
    buttons["R"] = true
  else
    buttons["R"] = false
  end
  if bit.band(byte,0x20) ~= 0 then
    buttons["L"] = true
  else
    buttons["L"] = false
  end
  
  X = string.byte(string.sub(data,3,3))
  if X > 127 then
    X = X - 256
  end
  
  Y = string.byte(string.sub(data,4,4))
  if Y > 127 then
    Y = Y - 256
  end
end

while true do
  -- Only read the next frame of data if the last one was used
  if input_was_used and not finished then
    read_next_frame()
    input_was_used = false
  end
  
  if not finished then
    joypad.set(buttons, 1)
    local analogs = { ["X Axis"] = X, ["Y Axis"] = Y }
    joypad.setanalog(analogs, 1)
  end
  
  if finished then
    console.output("done!")
    emu.pause()
  end
  
  emu.frameadvance()
end

