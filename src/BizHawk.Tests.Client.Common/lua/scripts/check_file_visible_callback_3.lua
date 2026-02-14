local function lookForFile()
	local f = io.open("check_file_visible_callback_3.lua", "r")
	if f == nil then
		print("fail")
	else
		io.close(f)
		print("pass")
	end
end

local eventId = nil
local function frameEnd()
	event.onframeend(lookForFile)
	event.unregisterbyid(eventId)
end

eventId = event.onframeend(frameEnd)

while true do
	emu.frameadvance()
end
