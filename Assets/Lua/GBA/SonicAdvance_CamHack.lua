local offX, offY, camX, camY
local addr_offX = 0x5B96
local addr_offY = 0x5B98
local addr_camX = 0x59D0
local addr_camY = 0x59D2

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
	
	client.seekframe(emu.framecount()+1)
	client.invisibleemulation(false)
	client.seekframe(emu.framecount()+1)
	client.invisibleemulation(true)
	memorysavestate.loadcorestate(memorystate)
	memorysavestate.removestate(memorystate)
--	client.invisibleemulation(false)
	emu.frameadvance()
end