--Author Pasky13

local xbase = 0x330
local x_page = 0x348
local ybase = 0x378
local y_page = 0x390

local camx = 0xFC
local camx_page = 0xF9
local cx

-- Compensation for megaman's projectiles
local pcompx = 0
local pcompy = 0

-- Compensation for megaman's hitbox
local compx = 2
local compy = 2

local xm 
local ym

memory.usememorydomain("PRG ROM")

local function camera()
	cx = mainmemory.read_u8(camx) + (mainmemory.read_u8(camx_page) * 256)
end

local function hex(val)
	val = string.format("%X",val)
	return val
end

local function megaman()
	local x = mainmemory.read_u8(xbase) + (mainmemory.read_u8(x_page) * 256) - cx
	local y = mainmemory.read_u8(ybase)
	local hp = bit.band(mainmemory.read_u8(0xB0),0x7F)
	compx = 6
	compy = 10
	gui.drawBox(x+compx,y+compy,x-compx,y-compy,0xFF0000FF,0x400000FF)
	gui.text(23 * xm,15 * ym,hp)
	gui.drawLine(x,y+2,x,y-2)
	gui.drawLine(x+2,y,x-2,y)
end

local function megaman_projectiles()
	local x
	local y
	local xrad
	local yrad
	local offset
	local active
	
	for i = 1,3,1 do
		active = mainmemory.read_u8(0x300 + i)
		
		if active > 0 then
			offset = mainmemory.read_u8(0x408 + i) * 2
			x = mainmemory.read_u8(xbase + i) + (mainmemory.read_s8(x_page + i) * 256) - cx
			y = mainmemory.read_u8(ybase + i)
			xrad = memory.read_u8(0x7FACF + offset)
			yrad = memory.read_u8(0x7FACE + offset)
			
			
			-- Compensate the boxes if xrad/yrad is 0 or 1
			if xrad <= 1 then
				pcompx = 2 - xrad
			else
				pcompx = 0
			end
			
			if yrad <= 1 then
				pcompy = 2 - yrad
			else
				pcompy = 0
			end
			
			gui.drawBox(x+xrad + pcompx,y+yrad + pcompy,x-xrad - pcompx,y-yrad - pcompy,0xFFFFFFFF,0x40FFFFFF)
		end
	end
end

local function objects()
	local x
	local y
	local xrad
	local yrad
	local hp
	local offset
	local active 
	local otype
	
	for i = 4,0x17,1 do
		active = mainmemory.read_u8(0x300 + i)
		otype = mainmemory.read_s8(0x408 + i)
		
		if active > 0 then
			x = mainmemory.read_u8(xbase + i) + (mainmemory.read_s8(x_page + i) * 256) - cx
			y = mainmemory.read_u8(ybase + i) + (mainmemory.read_s8(y_page + i ) * 256)
			
			if otype < 0 then  -- if it's an enemy/projectile
				offset = bit.band(mainmemory.read_u8(0x408 + i),0x3F)
				xrad = memory.read_u8(0x7F9F4 + offset)
				
				if mainmemory.read_u8(0x558) == 0x10 then  -- megaman is sliding
					yrad = memory.read_u8(0x7F9B4 + offset) - 0x10
				else
					yrad = memory.read_u8(0x7F9B4 + offset)
				end
				
				hp = mainmemory.read_u8(0x450 + i)
				
				-- Calculate compensation for megaman
				xrad = xrad - compx
				yrad = yrad - compx
				
				gui.text(x * xm,y * ym,"HP:" .. hp)
				--gui.text(x,y,hp)
				gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFF0000,0x90FF0000)
				
				-- Projectile vuln box
				yrad = memory.read_u8(0x7FADC + offset) - pcompx
				xrad = memory.read_u8(0x7FB1C + offset) - pcompy
				gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF00FF00,0x5000FF00)
			end
		end
	end
end

local function scaler()
	xm = client.screenwidth() / 256
	ym = client.screenheight() / 224
end

while true do
	scaler()
	camera()
	megaman()
	megaman_projectiles()
	objects()
	emu.frameadvance()
end
