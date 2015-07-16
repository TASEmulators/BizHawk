-- Ninja Gaiden II (USA) Collision Box Viewer
-- Author Pasky
-- For use with Bizhawk

local drawn = false -- Used to draw ryu's vulnaribility box only once per frame
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
	gui.drawLine(x,y,x,y,0xFF000000)
end

-- Player attacks

local function pattack()
	if memory.read_u8(0xCA55) == 0xB9 and memory.read_u8(0xCA56) == 0xF0 and memory.read_u8(0xCA57) == 0x04 then -- Bank check
		local xreg = emu.getregister("X")
		local yreg = emu.getregister("Y")
		-- Sword
		local x = memory.read_u8(0x550 + yreg)
		local y = memory.read_u8(0x580 + yreg) - 0x03
		local xrad = 0x20
		if bit.band(memory.read_u8(0x4F0 + yreg),0x40) == 0x40 then -- check ryu facing direction
			xrad = -0x20
		end
		--  NOTE:  The sword has no yradius, using drawBox() with a duplicate y coordinate does not draw a box, drawLine() is used in its place
		gui.drawLine(x,y,x+xrad,y,0xFFFFFFFF) -- Draw sword hitbox (line)
		
		-- Enemy vuln boxes (green)
		x = memory.read_u8(0x550 + xreg)
		y = memory.read_u8(0x580 + xreg)
		xrad = memory.read_u8(0x610 + xreg)
		local yrad = memory.read_u8(0x628 + xreg)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF00FF00,0x4000FF00)
	end
end

-- Touch collision
local function collision()
	if memory.read_u8(0xCC33) == 0xB9 and memory.read_u8(0xCC34) == 0x50 and memory.read_u8(0xCC35) == 0x05 then -- Bank check
		local xreg = emu.getregister("X")
		local yreg = emu.getregister("Y")
		local x = memory.read_u8(0x550 + yreg)
		local y = memory.read_u8(0x580 + yreg)
		local xrad = memory.read_u8(0x610 + yreg)
		local yrad = memory.read_u8(0x628 + yreg)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFF0000,0x40FF0000)
		
		-- Player
		if drawn == false then
			local x = memory.read_u8(0x550)
			local y = memory.read_u8(0x580)
			local hp = memory.read_u8(0x80)
			local xrad = 0x06
			local yrad = 0x10
			if bit.band(memory.read_u8(0x520),0x02) == 0x02 then -- Check if ryu is crouching
				yrad = 0x0C
			end
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF0000FF,0x400000FF)
			drawn = true
		end
	end
end

-- Subweapons
local function subweapon()
	if memory.read_u8(0xCB2F) == 0xBD and memory.read_u8(0xCB30) == 0x50 and memory.read_u8(0xCB31) == 0x05 then -- Bank check
		-- sub weapon
		local xreg = emu.getregister("X")
		local yreg = emu.getregister("Y")
		local x = memory.read_u8(0x550 + yreg)
		local y = memory.read_u8(0x580 + yreg)
		local xrad = memory.read_u8(0xC0)
		local yrad = memory.read_u8(0xC0)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFFFFFF,0x40FFFFFF)
		
		-- Enemy vuln (green)
		x = memory.read_u8(0x550 + xreg)
		y = memory.read_u8(0x580 + xreg)
		xrad = memory.read_u8(0x610 + xreg)
		yrad = memory.read_u8(0x628 + xreg)
		gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFF00FF00,0x4000FF00)
	end
end

event.onmemoryexecute(collision,0xCC33)
event.onmemoryexecute(pattack,0xCA55)
event.onmemoryexecute(subweapon,0xCB2F)

while true do
	emu.frameadvance()
	drawn = false
end