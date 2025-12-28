-- Seek forward to a given frame.
-- This will unpause emulation, and restore the users' desired pause state when seeking is completed.
-- Note that this may interfere with TAStudio's seeking behavior.
local function force_seek_frame(frame)
	local pause = client.unpause()
	while emu.framecount() < frame do
		-- The user may pause mid-seek, perhaps even by accident.
		-- In this case, we will unpause but remember that the user wants to pause at the end.
		if client.ispaused() then
			pause = true
			client.unpause()
		end
		-- Yield, not frameadvance. With frameadvance we cannot detect pauses, since frameadvance would not return.
		-- This is true even if we have just called client.unpause.
		emu.yield()
	end
	
	if pause then client.pause() end
end

-- Seek but without touching the pause state. Function will not return if the given frame is never reached due to the user manaully pausing/rewinding.
local function seek_frame(frame)
	while emu.framecount() < frame do
		emu.frameadvance()
	end
end
