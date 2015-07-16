-- Ninja Gaiden 3 (USA) Collision box viewer
-- Author: Pasky
-- For use with Bizhawk

local drawn = false -- Used so the player box isn't constantly drawn everytime the collision function is hit
memory.usememorydomain("System Bus")

function axis(x,y,color,xsize,ysize)
	if xsize == nil then
		xsize = 2
	end
	if ysize == nil then
		ysize = 2
	end
	gui.drawLine(x+xsize,y,x-xsize,y,color)
	gui.drawLine(x,y+ysize,x,y-ysize,color)
	gui.drawLine(x,y,x,y,"#000000FF")
end

local function collision()  -- Collision between enemies
	if memory.read_u8(0xA2CA) == 0xBD and memory.read_u8(0xA2CB) == 0x16 and memory.read_u8(0xA2CC) == 0x05 then -- Bank check
		local xreg = emu.getregister("X")
		local yreg = emu.getregister("Y")
		-- Enemy
		local x = memory.read_u8(0x516 + xreg)
		local y = memory.read_u8(0x58E + xreg)
		-- Enemy boxes
		local xrad = memory.read_u8(0x052E + xreg)
		local yrad = memory.read_u8(0x05A6 + xreg)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFF0000,0x40FF0000)
		local invuln = bit.bor(memory.read_u8(0x4E6 + xreg),memory.read_u8(0x4CE + xreg))
		if bit.band(invuln,0x02) == 0x02 then
			axis(x,y,0xFFFFFFFF,xrad,yrad)
		end	
		-- Player
		if drawn == false then
			x = memory.read_u8(0x516)
			y = memory.read_u8(0x58E)
			xrad = 0x06
			yrad = memory.read_u8(0x8C)
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF0000FF,0x400000FF)
			axis(x,y)
			drawn = true
		end
	end
end

local function pattack() -- Player attacks
	if memory.read_u8(0xA050) == 0xBD and memory.read_u8(0xA051) == 0x16 and memory.read_u8(0xA052) == 0x05 then -- Bank check
		local xreg = emu.getregister("X")
		local yreg = emu.getregister("Y")
		local x = memory.read_u8(0x516)
		if bit.band(memory.read_u8(0x4CE),0x40) == 0x40 then
			x = x - memory.read_u8(0xAA)
		else
			x = x + memory.read_u8(0xAA)
		end
		local y = memory.read_u8(0x58E) - 0x08
		local xrad = memory.read_u8(0xAA)
		local yrad = memory.read_u8(0xAB)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFFFFFF,0x40FFFFFF)
		
		-- Enemy vulnerability boxes (green)
		x = memory.read_u8(0x516 + xreg)
		y = memory.read_u8(0x58E + xreg)
		xrad = memory.read_u8(0x052E + xreg)
		yrad = memory.read_u8(0x05A6 + xreg)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF00FF00,0x4000FF00)
		local invuln = bit.bor(memory.read_u8(0x4E6 + xreg),memory.read_u8(0x4CE + xreg))
		if bit.band(invuln,0x02) == 0x02 then
			axis(x,y,0xFFFFFFFF,xrad,yrad)
		end	
	end
end

local function subweapon() -- Player sub weapons
	if memory.read_u8(0xA050) == 0xBD and memory.read_u8(0xA051) == 0x16 and memory.read_u8(0xA052) == 0x05 then -- Bank check
		local xreg = emu.getregister("X")
		local yreg = emu.getregister("Y")
		local x = memory.read_u8(0x51E + yreg)
		local y = memory.read_u8(0x596 + yreg)
		local wrad = memory.read_u8(0x9E)
		gui.drawBox(x-wrad,y-wrad,x+wrad,y+wrad)
		
		-- Enemy projectile vulnerability boxes (green)
		x = memory.read_u8(0x516 + xreg)
		y = memory.read_u8(0x58E + xreg)
		xrad = memory.read_u8(0x052E + xreg)
		yrad = memory.read_u8(0x05A6 + xreg)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF00FF00,0x4000FF00)
		local invuln = bit.bor(memory.read_u8(0x4E6 + xreg),memory.read_u8(0x4CE + xreg))
		if bit.band(invuln,0x02) == 0x02 then
			axis(x,y,0xFFFFFFFF,xrad,yrad)
		end	
	end
end

event.onmemoryexecute(collision,0xA2CA)
event.onmemoryexecute(pattack,0xA050)
event.onmemoryexecute(subweapon,0xA1DB)

while true do
	emu.frameadvance()
	drawn = false
end