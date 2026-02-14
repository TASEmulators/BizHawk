local function inputPoll()
	local f = io.open("check_file_visible_callback_2.lua", "r")
	if f == nil then
		print("fail")
	else
		io.close(f)
		print("pass")
	end
end

event.oninputpoll(inputPoll)
