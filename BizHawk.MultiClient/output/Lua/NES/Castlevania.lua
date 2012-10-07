local px = 0x38C
local py = 0x354
local pl = 0x45

local ex = 0x38C
local ey = 0x354

memory.usememorydomain("PRG ROM")

local function whip()
	local box = {0,0,0} -- xoff/yoff/xrad/yrad
	local wtype = mainmemory.read_u8(0x70)
	local pos = bit.band(mainmemory.read_u8(0x434),0x7F)  -- Simons acting positon (jumping/crouching, etc...)
	box[1] = memory.read_s8(0x1E45D + wtype)  
	wtype = wtype * 2
	box[2] = memory.read_s8(0x1E460 + pos) 
	box[3] = memory.read_u8(0x1E464 + wtype) 
	box[4] = memory.read_u8(0x1E465 + wtype)
	
	if mainmemory.read_u8(0x450) == 1 then  -- Simon is facing left
		box[1] = box[1] * -1
		box[3] = box[3] * -1
	end
	return box
end

local function player()
	local x = mainmemory.read_u8(px)
	local y = mainmemory.read_u8(py)
	local box
	gui.drawBox(x+4,y+mainmemory.read_u8(0x5F),x-4,y-mainmemory.read_u8(0x5F),0xFF0000FF,0x400000FF)
	
	if mainmemory.read_u8(0x568) == 0x11 then -- Whip is active 
		box = whip()
		gui.drawBox(x+box[1]+box[3],y+box[2]+box[4],x+box[1]-box[3],y+box[2]-box[4],0xFFFFFFFF,0x40FFFFFF)
	end
	
end

local function buildbox(i)
	local box = {0,0}  -- xrad/yrad
	local offset = mainmemory.read_u8(0x434 + i)
		
	if offset == 0x1D then
		offset = mainmemory.read_u8(0x7A)
		if offset == 0 then
			offset = 0x30 * 2
		else
			offset = offset * 2
		end
	else
		offset = offset * 2
	end

	box = { memory.read_u8(0x1E46A + offset),memory.read_u8(0x1E46B + offset) }
	
	return box
end

local function getcolor(x)
	local color = {0,0} -- Fill/Outline
	if x >= 0x28 and x < 0x30 then
		color = {0x40FFA500,0xFFFFA500}
	elseif x >= 0x30 and x <= 0x33 then
		return color
	elseif x == 0x17 then -- Simon subweapon
		color = {0x4000FFFF,0xFF00FFFF}
	else
		color = {0x40FF0000,0xFFFF0000}
	end
	return color
end

local function objects()
	local x
	local y
	local active
	local box
	local etype
	local c
	local oob
	
	for i = 3,20,1 do
		oob = mainmemory.read_u8(0x300 + i)
		if oob == 0 then
			box = buildbox(i)
			etype = mainmemory.read_u8(0x434 + i)
			c = getcolor(etype)
			x = mainmemory.read_u8(ex + i)
			y = mainmemory.read_u8(ey + i)
			gui.drawBox(x+box[1],y+box[2],x-box[1],y-box[2],c[2],c[1])
		end
	end
end


while true do
	player()
	objects()
	emu.frameadvance()
end