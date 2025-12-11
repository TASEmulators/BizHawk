local function frameEnd()
	local f = io.open("check_file_visible_callback_1.lua", "r")
	if f == nil then
		print("fail")
	else
		io.close(f)
		print("pass")
	end
end

event.onframeend(frameEnd)
