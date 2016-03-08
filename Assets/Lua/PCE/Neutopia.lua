-- Neutopia (USA) Collision Box Viewer v1.0
-- Author: Pasky (6/22/2015)
-- For use with Bizhawk

memory.usememorydomain("System Bus")

local function player()
	local hp = mainmemory.read_u8(0x14c5)
	local x = mainmemory.read_u8(0x1063)
	local h = mainmemory.read_u8(0x112F)
	local y = mainmemory.read_u8(0x10C9) - 0xA - h
	local xrad = 0x6
	local yrad = 0xC
	gui.drawText(1,1,"HP: " .. hp,0xFFFFFFFF,10,"Arial")
	gui.drawBox(x+xrad,y+yrad,x-xrad,y-yrad,0xFF0000FF,0x400000FF)
	
end

local function enemy()
	local XREG = emu.getregister("X")
	local YREG = emu.getregister("Y")
	local hp = mainmemory.read_u8(0x14C5 + XREG) + 1
	local x = mainmemory.read_u8(0x1063 + XREG)
	local h = mainmemory.read_u8(0x112F + XREG) 
	local y = mainmemory.read_u8(0x10C9 + XREG) - h
	local xoff = memory.read_s8(mainmemory.read_u16_le(0x0018) + YREG)
	local xrad = memory.read_u8(mainmemory.read_u16_le(0x0018) + YREG - 1)
	local yoff = memory.read_s8(mainmemory.read_u16_le(0x0018) + YREG + 2) 
	local yrad = memory.read_u8(mainmemory.read_u16_le(0x0018) + YREG + 1) 
	gui.drawText(x+xoff-xrad,y+yrad+yoff+4,"HP: " .. hp,0xFFFFFFFF,8,"Arial")
	gui.drawBox(x+xrad,y+yoff+yrad,x-xrad,y+yoff-yrad,0xFFFF0000,0x40FF0000)
end

local function attack()
	local XREG = emu.getregister("X")
	local YREG = emu.getregister("Y")
	local x = mainmemory.read_u8(0x1063)
	local h = mainmemory.read_u8(0x112F)
	local y = mainmemory.read_u8(0x10C9) - h
	local xoff = memory.read_s8(0xD453 + YREG)
	local yoff = memory.read_s8(0xD45B + YREG)
	local xrad = mainmemory.read_u8(0x15BE)
	local yrad = mainmemory.read_u8(0x15BF)
	gui.drawBox(x+xoff+xrad,y+yoff+yrad,x+xoff-xrad,y+yoff-yrad,0xFFFFFFFF,0x40FFFFFF)
end

local function subweapon()
	local XREG = emu.getregister("X")
	local YREG = emu.getregister("Y")
	local x = mainmemory.read_u8(0x1063 + XREG)
	local h = mainmemory.read_u8(0x112F + XREG)
	local y = mainmemory.read_u8(0x10C9 + XREG) - h
	local xoff = memory.read_s8(mainmemory.read_u16_le(0x0018) + YREG - 1)
	local yoff = memory.read_s8(mainmemory.read_u16_le(0x0018) + YREG + 1)
	local yrad = memory.read_u8(mainmemory.read_u16_le(0x0018) + YREG)
	local xrad = memory.read_u8(mainmemory.read_u16_le(0x0018) + YREG - 2)
	gui.drawBox(x+xoff+xrad,y+yoff+yrad,x+xoff-xrad,y+yoff-yrad,0xFFFFFFFF,0x40FFFFFF)
end	

event.onmemoryexecute(attack,0x00D430)
event.onmemoryexecute(enemy,0x00D48F)
event.onmemoryexecute(subweapon,0x00D88E)
while true do
	player()
	emu.frameadvance()
end