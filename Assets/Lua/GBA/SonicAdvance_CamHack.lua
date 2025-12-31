-- feos, 2019

local offX, offY, camX, camY
local addr_offX = 0x5B96
local addr_offY = 0x5B98
local addr_camX = 0x59D0
local addr_camY = 0x59D2

local id = client.show_future(function(f)
	if f == 0 then
		offX = mainmemory.read_u16_le(addr_offX)
		offY = mainmemory.read_u16_le(addr_offY)
		camX = mainmemory.read_u16_le(addr_camX)
		camY = mainmemory.read_u16_le(addr_camY)
		
		Xval = camX + offX - 128
		Yval = camY + offY - 80
		
		mainmemory.write_u16_le(addr_camX, Xval)
		mainmemory.write_u16_le(addr_camY, Yval)
	elseif f == 2 then
		return true
	end
	
	return false
end, 2)
event.onexit(function() event.unregisterbyid(id) end)

while true do	
	emu.frameadvance()
end
