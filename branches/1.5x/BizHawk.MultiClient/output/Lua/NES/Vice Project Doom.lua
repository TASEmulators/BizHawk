--Vice Project Doom Collision Box Viewer
--Author Pasky13

local camx

local function hex(val)
	val = string.format("%X",val)
	return val
end

memory.usememorydomain("PRG ROM")

local function camera()
	camx = mainmemory.read_u8(0xB) + (mainmemory.read_u8(0xC) * 256)
end

local function player()
	local x = mainmemory.read_u8(0x1F0) + (mainmemory.read_u8(0x200) * 256) - camx
	local y = mainmemory.read_u8(0x210)
	local offset = mainmemory.read_u8(0x2A0) * 4
	local yoff = memory.read_s8(0x1C4C0 + offset + 1)
	local yrad = memory.read_u8(0x1C4C0 + offset + 3)
	local xoff = memory.read_s8(0x1C4C0 + offset)
	local xrad = memory.read_u8(0x1C4C0 + offset + 2)
	gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad, 0xFF0000FF,0x350000FF)
end

local function enemies()
	local active = 0x161
	local base = 0x1F1
	for i = 0,15,1 do
		if mainmemory.read_u8(active + i) ~= 0 then
			local offset = mainmemory.read_u8(0x2A1 + i) * 4
			local x = mainmemory.read_u8(0x1F1 + i) + (mainmemory.read_u8(0x201 + i) * 256) -camx
			local y = mainmemory.read_u8(0x211 + i)
			local yoff = memory.read_s8(0x1C4C0 + offset + 1)
			local yrad = memory.read_u8(0x1C4C0 + offset + 3)
			local xoff = memory.read_s8(0x1C4C0 + offset)
			local xrad = memory.read_u8(0x1C4C0 + offset + 2)
			if i == 1 or i == 2 then  -- Player's weapon
				gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad, 0xFFFFFFFF,0x35FFFFFF)
			else
				gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad, 0xFFFF0000,0x35FF0000)
			end
		end
	end
	
end

while true do
	camera()
	player()
	enemies()
	emu.frameadvance()
end