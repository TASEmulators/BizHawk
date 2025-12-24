-- feos, 2019

local offX, offY, camX, camY
local addr_offX = 0x5B96
local addr_offY = 0x5B98
local addr_camX = 0x59D0
local addr_camY = 0x59D2

-- Seek forward to a given frame.
-- This will unpause emulation, and restore the users' desired pause state when seeking is completed.
local function seek_frame(frame)
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

while true do
	client.invisibleemulation(true)
	local memorystate = memorysavestate.savecorestate()
	
	offX = mainmemory.read_u16_le(addr_offX)
	offY = mainmemory.read_u16_le(addr_offY)
	camX = mainmemory.read_u16_le(addr_camX)
	camY = mainmemory.read_u16_le(addr_camY)
	
	Xval = camX + offX - 128
	Yval = camY + offY - 80
	
	mainmemory.write_u16_le(addr_camX, Xval)
	mainmemory.write_u16_le(addr_camY, Yval)
	
	seek_frame(emu.framecount()+1)
	client.invisibleemulation(false)
	seek_frame(emu.framecount()+1)
	client.invisibleemulation(true)
	memorysavestate.loadcorestate(memorystate)
	memorysavestate.removestate(memorystate)
--	client.invisibleemulation(false)
	emu.frameadvance()
end