-- Author Pasky13

--Player
local px = 0x54C
local py = 0x532

--Player projectiles
local projxbase = 0x588
local projybase = 0x578

--Enemies
local etype = 0xC6
local ex = 0x53C
local ey = 0x522

local xm
local ym

local function draw_axis(x,y,color)
	gui.drawLine(x,y-4,x,y+4,color)
	gui.drawLine(x-4,y,x+4,y,color)
end

local function sign(val)
	if val > 0x7F then
		val = 256 + (val * -1)
	end
	return val
end

memory.usememorydomain("PRG ROM")

local function player()
	-- Player 1
	if mainmemory.read_u8(0xC6) ~= 0 then
		local x = mainmemory.read_u8(px)
		local y = mainmemory.read_u8(py)
		draw_axis(x,y,0xFF0000FF)
	end
	-- Player 2
	if mainmemory.read_u8(0xC7) ~= 0 then
		x = mainmemory.read_u8(px+1)
		y = mainmemory.read_u8(py+1)
		draw_axis(x,y,0xFF0000FF)
	end
end

local function bullets()
	-- P1
	if mainmemory.read_u8(0xC6) ~= 0 then
		for i = 0,4,1 do
			local x = mainmemory.read_u8(projxbase + i)
			local y = mainmemory.read_u8(projybase + i)
			
			if mainmemory.read_u8(0x638 + i) > 0 then
				gui.drawBox(x-2,y-2,x+2,y+2,0xFFFFFFFF,0x40FFFFFF)
			end
		end
	end
	
	-- P2
	if mainmemory.read_u8(0xC7) ~= 0 then
		for i = 0,9,1 do
			local x = mainmemory.read_u8(0x592 + i)
			local y = mainmemory.read_u8(0x582 + i)
			
			if mainmemory.read_u8(0x642 + i) > 0 then
				gui.drawBox(x-2,y-2,x+2,y+2,0xFFFFFFFF,0x40FFFFFF)
			end
		end
	end
end


local function enemies()
	local active
	local etype
	local fill
	local outl
	for i = 0,0x0D,1 do
		active = mainmemory.read_u8(0x508+i)  -- sprite frame
		etype = mainmemory.read_u8(0x73A + i)
		if etype == 4 or etype == 0x0C then
			fill = 0xFF00FF00
			outl = 0x3500FF00
		else
			fill = 0xFFFF0000
			outl = 0x35FF0000
		end

		local x = mainmemory.read_u8(ex + i)
		local y = mainmemory.read_u8(ey + i)
		
		-- Player 1 collision detection
		if mainmemory.read_u8(0xC6) ~= 0 then
			if active > 0 then
				local offset = ((mainmemory.read_u8(0xC6) + etype + 1) % 0x100)
				local xoff = sign(memory.read_u8(0x6000 + offset + 1)) + 1
				local xrad = memory.read_u8(0x6000 + offset + 3)
				local yoff = sign(memory.read_u8(0x6000 + offset)) + 1
				local yrad = memory.read_u8(0x6000 + offset + 2)
				gui.drawBox(x-xoff,y-yoff,x-xoff+xrad,y-yoff+yrad,fill,outl)
				
			end
		end
		
		-- Player 2 collision detection
		if mainmemory.read_u8(0xC7) ~= 0 then
			if active > 0 then
				local offset = ((mainmemory.read_u8(0xC7) + etype + 1) % 0x100)
				local xoff = sign(memory.read_u8(0x6000 + offset + 1)) + 1
				local xrad = memory.read_u8(0x6000 + offset + 3)
				local yoff = sign(memory.read_u8(0x6000 + offset)) + 1
				local yrad = memory.read_u8(0x6000 + offset + 2)
				gui.drawBox(x-xoff,y-yoff,x-xoff+xrad,y-yoff+yrad,fill,outl)
			end
		end	
		
		-- Projectile collision
		if active > 0 and etype ~= 4 and etype ~= 0x0C then
			local poffset = etype + 0xA0
			local pxoff = sign(memory.read_u8(0x6000 + poffset + 1)) + 1
			local pxrad = memory.read_u8(0x6000 + poffset + 3) - 2
			local pyoff = sign(memory.read_u8(0x6000 + poffset)) + 1
			local pyrad = memory.read_u8(0x6000 + poffset + 2) - 2
			gui.drawBox(x-pxoff,y-pyoff,x-pxoff+pxrad,y-pyoff+pyrad,0xFFFFFF00,0x40FFFF00)
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
	bullets()
	enemies()
	emu.frameadvance()
end