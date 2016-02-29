-- M64 reader script
-- Translates M64 file movies into button presses for Bizhawk, accounting for lag frames.
-- This script will automatically pause at the end of the movie

-- This script will not clear the saveram. If a movie requires empty saveram it must be cleared before using this script.

-- If you are trying to convert a M64 into BKM format, you can try pausing the emulator and starting recording a movie, then loading this script and letting it run. Beginning a new movie will clear the saveram for you.


local m64_filename = forms.openfile(nil,nil,"Mupen Movie Files (*.M64)|*.M64|All Files (*.*)|*.*")

console.clear()
if m64_filename == "" then
  console.log("No movie selected. Exiting.")
  return
end

console.log("Opening movie for playback: " .. m64_filename)

-- Open the file and read past the header data
local input_file = assert(io.open(m64_filename, "rb"))
local header = input_file:read(0x400)

-- Check the file and display some info
if string.sub(header,1,3) ~= "M64" or string.byte(header,4) ~= 0x1A then
  console.log("File signature is not M64\\x1A. This might not be an .m64 movie, but I'll try to play it anyway")
end

function remove_nulls(s)
  if string.len(s) == 0 then
    return s
  end
  
  local i = 1
  while string.byte(s,i) ~= 0 and i <= string.len(s) do
    i = i + 1
  end
  
  return string.sub(s,1,i-1)
end

local movie_rom_name = string.sub(header,0x0C5,0x0E4)
movie_rom_name = remove_nulls(movie_rom_name)
console.log("Rom name: " .. movie_rom_name)

local rerecords = string.byte(header,0x11) + string.byte(header,0x12) * 0x100 + string.byte(header,0x13) * 0x10000 + string.byte(header,0x14) * 0x1000000
console.log("# of rerecords: " .. rerecords)

local rerecords = string.byte(header,0x0D) + string.byte(header,0x0E) * 0x100 + string.byte(header,0x0F) * 0x10000 + string.byte(header,0x10) * 0x1000000
console.log("# of frames: " .. rerecords)

local author_info = string.sub(header,0x223,0x300)
author_info = remove_nulls(author_info)
console.log("Author: " .. author_info)

local description = string.sub(header,0x301,0x400)
description = remove_nulls(description)
console.log("Description: " .. description)

local video_plugin = string.sub(header,0x123,0x162)
video_plugin = remove_nulls(video_plugin)
console.log("Video Plugin: " .. video_plugin)

local audio_plugin = string.sub(header,0x163,0x1A2)
audio_plugin = remove_nulls(audio_plugin)
console.log("Audio Plugin: " .. audio_plugin)

local input_plugin = string.sub(header,0x1A3,0x1E2)
input_plugin = remove_nulls(input_plugin)
console.log("Input Plugin: " .. input_plugin)

local rsp_plugin = string.sub(header,0x1E3,0x222)
rsp_plugin = remove_nulls(rsp_plugin)
console.log("RSP Plugin: " .. rsp_plugin)

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
  local data = input_file:read(4)
  if not data or string.len(data) ~= 4 then 
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
    console.log("Movie finished")
    client.pause()
    return
  end
  
  emu.frameadvance()
end
