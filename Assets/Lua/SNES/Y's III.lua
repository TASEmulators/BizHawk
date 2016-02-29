-- Y's III (SNES) Collision box viewer
-- Bizhawk
-- Author: Pasky

local camx
local camy

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function hex(val)
	val = string.format("%X",val)
	if string.len(val) == 1 then
		val = "0" .. val
	end
	return val
end

local function camera()
	camx = mainmemory.read_u16_le(0x12D8)
	camy = mainmemory.read_u16_le(0x12E0)
end

memory.usememorydomain("CARTROM")

local function player()
	local x = mainmemory.read_u8(0x1274)
	local y = mainmemory.read_u8(0x1276)
	local x1 = x
	local y1 = y
	local x2 = 0
	local y2 = 0
	if mainmemory.read_u8(0x1273) == 3 then
		x1 = x1 - 4
		x2= x1 + 0x18
		y1= y1 + 0x12
		y2= y1 + 0x10
	else
		x2 = x1 + 0x10
		y1 = y1 + 0x08
		y2 = y1 + 0x18
	end

	gui.drawBox(x1,y1,x2,y2,0xFF0000FF,0x300000FF)
	-- cheat
	-- memory.writebyte(0x1294,0x90) -- infinite hp
	-- memory.writeword(0x1299,0x400) -- experience
	
	-- Attacking
	if mainmemory.read_u8(0x1283) ~= 0 then
		local offset1 = bit.band(mainmemory.read_u8(0x1287),0x3)
		offset1 = offset1 + mainmemory.read_u8(0x1278) + mainmemory.read_u8(0x127A) - 0x10
		offset1 = bit.lshift(offset1,1)
		local offset2 = bit.band(mainmemory.read_u8(0x127C),0x40)
		offset2 = bit.rshift(offset2,6)
		offset2 = bit.bor(offset2,offset1)
		offset2 = bit.lshift(offset2,2)
		local ax1 = bit.band(x + memory.read_u8(0xBA4BE + offset2),0xFF)
		local ay1 = bit.band(y + memory.read_u8(0xBA4BF + offset2),0xFF)
		local ax2 = bit.band(x + memory.read_u8(0xBA4BC + offset2),0xFF)
		local ay2 = bit.band(y + memory.read_u8(0xBA4BD + offset2),0xFF)
		gui.drawBox(ax1,ay1,ax2,ay2)
	end
end

local function enemies()
	local start = 0x17FB - 0x22
	for i =0,15,1 do
		local base = start + (i * 0x22)
		local x = mainmemory.read_u16_le(base) - camx
		local y = mainmemory.read_u16_le(base + 2) - camy
		local facing = mainmemory.read_u8(base + 0xf)
		local offset = mainmemory.read_u8(base + 0xD) * 2
		local pointer = memory.read_u16_le(0xB9A25 + offset)
		local hp = mainmemory.read_u8(base + 0x8)
		local active = mainmemory.read_u8(base + 0x1D)
		
		if active ~= 0 then
			if bit.band(facing,0x40) ~= 0 then
				-- left
				pointer = pointer + 4
			else
				-- right
			end
			local x1 = x + memory.read_u8(0xB9A26 + pointer)
			local y1 = y + memory.read_u8(0xB9A27 + pointer)
			local x2 = x + memory.read_u8(0xB9A28 + pointer)
			local y2 = y + memory.read_u8(0xB9A29 + pointer)
			
			gui.text(x,y,"HP: " .. hp )
			gui.drawBox(x1,y1,x2,y2,0xFFFF0000,0x30FF0000)
			end
	end	
end


while true do
	camera()
	player()
	enemies()
	emu.frameadvance()
end