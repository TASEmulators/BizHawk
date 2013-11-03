--Author pasky13

local cx 
local cy
local data = 0x3C0000

local function camera()
	cx = mainmemory.read_u16_le(0x493)
	cy = mainmemory.read_u16_le(0x497)
end

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function hex(val)
	val = string.format("%X",val)
	return val
end

memory.usememorydomain("CARTROM")

local function player()
	local pbase = 0x878
	for i = 0,1,1 do
		pbase = pbase + (i * 0x6E)
		if mainmemory.read_u8(pbase + 0x24) > 0 then
			local point = memory.read_u16_le(mainmemory.read_u16_le(pbase + 0x24) + 0x3C8003)
			local properties = mainmemory.read_u16_le(pbase + 0x1E)
			local x = mainmemory.read_u16_le(pbase + 0x12) - cx 
			local y = mainmemory.read_u16_le(pbase + 0x16) - cy
			local xoff = memory.read_s8(point + data) 
			local yoff = memory.read_s8(point + 2 + data)
			local xrad = memory.read_u8(point + 4 + data)
			local yrad = memory.read_u8(point + 6 + data)
			
			if hasbit(properties,findbit(15)) then  -- If facing right
				xoff = xoff * -1
				xrad = xrad * -1
			elseif hasbit(properties,findbit(16)) then -- Upside down?
				yoff = yoff * -1
				yrad = yrad * -1
			end
			

			gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFF0000FF,0x400000FF)
		end
	end
end

local function enemy()
	local start = 0x954
	local base
	local x
	local y
	local xoff
	local yoff
	local xrad
	local yrad
	local properties
	local point
	local active
	for i = 0,15,1 do
		base = start + (i * 0x6E)
		active = mainmemory.read_u8(base + 0x30)
		properties = mainmemory.read_u16_le(base + 0x1E)
		
		if mainmemory.read_u8(base + 0x24) > 0 then
			point = memory.read_u16_le(mainmemory.read_u16_le(base + 0x24) + 0x3C8003)
			properties = mainmemory.read_u16_le(base + 0x1E)
			x = mainmemory.read_u16_le(base + 0x12) - cx
			y = mainmemory.read_u16_le(base + 0x16) - cy
			xoff = memory.read_s8(point + data)
			yoff = memory.read_s8(point + 2 + data)
			xrad = memory.read_u8(point + 4 + data)
			yrad = memory.read_u8(point + 6 + data)

			
			if hasbit(properties,findbit(15)) then	-- If facing right
				xoff = xoff * -1
				xrad = xrad * -1
			elseif hasbit(properties,findbit(16)) then -- Upside down?
				yoff = yoff * -1
				yrad = yrad * -1
			end
			
			gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFFFF0000,0x40FF0000)
		end
	end
end

while true do
	camera()
	player()
	enemy()
	emu.frameadvance()
end