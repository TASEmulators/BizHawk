----------TOGGLES----------

DISPLAY_ENEMY_HP = true
--------END TOGGLES--------

xbase = 0x64E
ybase = 0x668
abase = 0x682
hpbase = 0x796
offscreen = 0x600
xm = 0
ym = 0
--P1X 
-- animation 0x61A

local proj_fill = 0xFF00FF00
local proj_outl = 0x3000FF00

local enemy_fill = 0xFFFF0000
local enemy_outl = 0x30FF0000

local player_fill = 0xFF0000FF
local player_outl = 0x300000FF

local pproj_fill = 0xFFFFFFFF
local pproj_oult = 0x30FFFFFF

memory.usememorydomain("System Bus")

local function hex(val)
	val = string.format("%X",val)
	return val
end

local function player()
	local active = mainmemory.read_u8(abase)
	if active > 0 then
		local offset = bit.band(mainmemory.read_u8(0x720),0x3F) * 2
		local xrad = memory.read_u8(0xF94A + offset)
		local yrad = memory.read_u8(0xF94B + offset)
		local hp = mainmemory.read_u8(hpbase)
		local x = mainmemory.read_u8(xbase)
		local y = mainmemory.read_u8(ybase)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad, player_fill, player_outl)
	end
end

local function player_proj()
	for i = 10,11,1 do
		local active = mainmemory.read_u8(abase + i)
		local oscr = bit.band(mainmemory.read_u8(offscreen + i),0xF0)
		
		if oscr == 0x20 then
			oscr = -255
		elseif oscr == 0x10 then
			oscr = 255
		end	
		
		if active > 0 then
			local offset = bit.band(mainmemory.read_u8(0x720 + i),0x3F) * 2
			local xrad = memory.read_u8(0xF94A + offset)
			local yrad = memory.read_u8(0xF94B + offset)
			local x = mainmemory.read_u8(xbase + i) + oscr
			local y = mainmemory.read_u8(ybase + i)
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad, pproj_fill, pproj_outl)
		end
	end
	
end

local function enemy()
	for i = 2,8,1 do
		local active = mainmemory.read_u8(abase + i)
		local oscr = bit.band(mainmemory.read_u8(offscreen + i),0xF0)
		if oscr == 0x20 then
			oscr = -255
		elseif oscr == 0x10 then
			oscr = 255
		end	
		if active > 0 then
			local offset = bit.band(mainmemory.read_u8(0x720 + i),0x3F) * 2
			local xrad = memory.read_u8(0xF94A + offset)
			local yrad = memory.read_u8(0xF94B + offset)
			local hp = mainmemory.read_u8(hpbase + i)
			local x = mainmemory.read_u8(xbase + i) + oscr
			local y = mainmemory.read_u8(ybase + i)
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad, enemy_fill, enemy_outl)
			if DISPLAY_ENEMY_HP == true then
				gui.text(x * xm,y * ym,hp)
			end
		end
	end
end

local function e_proj()
	local pbase = 0x720
	for i = 18,22,1 do
		local active = mainmemory.read_u8(abase + i)
		local oscr = bit.band(mainmemory.read_u8(offscreen + i),0xF0)
		if oscr == 0x20 then
			oscr = -255
		elseif oscr == 0x10 then
			oscr = 255
		end	
		if active > 0 then
			local offset = bit.band(mainmemory.read_u8(0x720 + i),0x3F) * 2
			local xrad = memory.read_u8(0xF94A + offset)
			local yrad = memory.read_u8(0xF94B + offset)
			local x = mainmemory.read_u8(xbase + i) + oscr
			local y = mainmemory.read_u8(ybase + i)
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad, proj_fill, proj_outl)
		end
	end
end

local function scaler()
	xm = client.screenwidth() / 256
	ym = client.screenheight() / 224
end

while true do
	scaler()
	player()
	enemy()
	e_proj()
	player_proj()
	emu.frameadvance()
end