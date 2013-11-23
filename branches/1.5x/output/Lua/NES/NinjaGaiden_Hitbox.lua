--Author Pasky13

--Player
local px = 0x0086
local py = 0x008A

local projxbase = 0x4BB  -- Object X speed base is 0x4C1
local projybase = 0x4BE  -- Object Y speed base ix 0x4C4

--Enemies
local ex = 0x0460
local ey = 0x0480
local efacing = 0x440
local etype = 0x0438

--Objects
local ox = 0x4D9
local oy = 0x4E1

memory.usememorydomain("PRG ROM")

local function Ryu()
		local x = mainmemory.read_u8(px)
		local y = mainmemory.read_u8(py)
		local yrad = mainmemory.read_u8(0x90)
		local act = mainmemory.read_u8(0x83)
		local subwep = mainmemory.read_u8(0xC9)
		local flip = 0
		local swdactive = mainmemory.read_u8(0x83)
		
		if bit.band(mainmemory.read_u8(0x84),0x40) == 0x40 then
			flip = 1
		end	
		--Spin slash check
		if subwep == 0x85 then
			if mainmemory.read_u8(0x83) >= 0x0C and mainmemory.read_u8(0x83) <= 0x0F then 
				gui.drawBox(x+9,y-8,x-9,y-yrad,0xFFFFFF00,0x60FFFF00)
			end
		end
		--Check if Ryu is attacking with sword 7 = stand/jump, A = crouching
		if swdactive == 7 or swdactive == 0x0A then
			if flip == 0 then
				gui.drawBox(x,y-yrad,x+0x20,y-yrad+0x10,0xFFFFFFFF,0x60FFFFFF)
			else
				gui.drawBox(x,y-yrad,x-0x20,y-yrad+0x10,0xFFFFFFFF,0x60FFFFFF)
			end
		end
		-- Check spinning flame subweapon
		if subwep == 0x84 then
			if mainmemory.read_u8(0x4C8) > 0 then
				gui.drawBox(x+9,y-8,x-9,y-yrad,0xFFFFFF00,0x60FFFF00)
			end
		end
		gui.drawLine(x,y,x,y-yrad,0xFF0000FF)
end

local function weapons()
	for i = 0,2,1 do
		active = bit.band(mainmemory.read_u8(0xC8),memory.read_u8(0x1E66F + i))
		if active > 0 then
			local wtype = bit.band(mainmemory.read_u8(0xC9),0x7F)
			local x = mainmemory.read_u8(projxbase + i)
			local y = mainmemory.read_u8(projybase + i)
			local xrad = memory.read_u8(0x1E605 + wtype)
			gui.drawBox(x+xrad,y,x-xrad,y-xrad,0xFFFF00FF,0x60FF00FF)
		end
	end
end

local function enemies()
	for i = 0,8,1 do
		local active = bit.band(mainmemory.read_u8(0x73),memory.read_u8(0x1E66F + i))
		if active > 0 then
			local offset = mainmemory.read_u8(etype + i)
			local x = mainmemory.read_u8(ex + i)
			local y = mainmemory.read_u8(ey + i)
			local xrad = memory.readbyte(0x3300 + offset)
			local yrad = memory.readbyte(0x3400 + offset)
			gui.drawBox(x+xrad,y,x-xrad,y-yrad,0xFFFF0000,0x60FF0000)
			gui.drawLine(x,y,x,y-yrad,0xFFFFFF00)
		end
	end
end

local function objects()
	for i = 0,3,1 do
		local active = bit.band(mainmemory.read_u8(0xC0),memory.read_u8(0x1E66F + i))
		if active > 0 then
			active = bit.band(mainmemory.read_u8(0x4D1 + i),8)
			if active == 0 then
				x = mainmemory.read_u8(ox + i)
				y = mainmemory.read_u8(oy + i)
				gui.drawLine(x,y,x,y-0x10,0xFF00FFF0)
				gui.drawBox(x-0x0C,y,x+0x0C,y-0x10,0xFF00FFF0,0x6000FFF0)
			end
		end
	end
end

while true do
	Ryu()
	weapons()
	enemies()
	objects()
	emu.frameadvance()
end