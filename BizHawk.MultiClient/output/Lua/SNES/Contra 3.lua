----------------------------------------------
-----Contra III hitbox viewer script SNES-----
----------------------------------------------
--Player Colors:  
--Gold = Invuln
--Blue = Vulnerable

--Enemy colors:
--Red = Can be touched and hit with projectiles
--Green = Can be hit with projectiles but has no collision
--Yellow = Can touch you, cannot be hit with player projectiles
--White Axis in middle of box = Box is invulnerable

local xm
local ym

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function check_offscreen(pos,val)

	if val ~= 0 then
		if val == 1 then
			pos = 255 + pos
		elseif val == 255 then
			pos = 0 -(255 - pos)
		end
	end
	
	return pos
	
end

local function draw_invuln(x,y,xrad,yrad)
	gui.drawLine(x + (xrad / 2), y, x + (xrad / 2), y + yrad,0xFFFFFFFF)
	gui.drawLine(x, y + (yrad / 2), x + xrad, y + (yrad / 2),0xFFFFFFFF) 
end

local function Player()
	local pbase = 0x200

	for i = 0,1,1 do 
		pbase = pbase + (i * 0x40)
		local x = mainmemory.read_u8(pbase + 6)
		local y = mainmemory.read_u8(pbase + 0x10)
		local x2 = mainmemory.read_u8(pbase + 0x28)
		local y2 = mainmemory.read_u8(pbase + 0x2A)
		local x_offscreen = mainmemory.read_u8(pbase + 0x7)
		local y_offscreen = mainmemory.read_u8(pbase + 0x11)
		local x2_offscreen = mainmemory.read_u8(pbase + 0x29)
		local y2_offscreen = mainmemory.read_u8(pbase + 0x2B)
		local active = mainmemory.read_u8(pbase + 0x16)
		
		-- Checks if the box went off screen and adjusts
		x = check_offscreen(x,x_offscreen)
		x2 = check_offscreen(x2,x2_offscreen)
		y = check_offscreen(y,y_offscreen)
		y2 = check_offscreen(y2,y2_offscreen)
		
		if active > 0 then
			if hasbit(active,findbit(2)) == true or mainmemory.read_u16_le(0x1F88 + (i * 0x40)) > 0 then
				gui.drawBox(x,y,x2,y2,0xFFFDD017,0x35FDD017)
			else
				gui.drawBox(x,y,x2,y2,0xFF0000FF,0x350000FF)
			end
		end
	end



end

local function Enemies()
	local start = 0x280
	local base = 0
	local oend = 32
	local x
	local x_offscreen
	local y
	local y_offscreen
	local xrad
	local yrad
	local active
	local touch
	local projectile
	local invuln
	local hp
	
	for i = 0,oend,1 do
		base = start + (i * 0x40)
		
		active = mainmemory.read_u8(base + 0x16)
		hp = mainmemory.read_s16_le(base + 6)
		if active > 0 then
			
			touch = hasbit(active,findbit(4))
			projectile = hasbit(active,findbit(5))
			invuln = hasbit(active,findbit(6))
			x = mainmemory.read_u8(base + 0xa)
			x_offscreen = mainmemory.read_u8(base + 0xb)
			y = mainmemory.read_u8(base + 0xe)
			y_offscreen = mainmemory.read_u8(base + 0xf)
			xrad = mainmemory.read_s16_le(base+0x28)
			yrad = mainmemory.read_s16_le(base+0x2A)
			
			-- Checks if the box went off screen and adjusts
			
			x = check_offscreen(x,x_offscreen)
			y = check_offscreen(y,y_offscreen)
			
			if projectile and touch then
				 gui.drawBox(x,y,x+ xrad,y+yrad,0xFFFF0000,0x35FF0000)
			elseif projectile then
				gui.drawBox(x,y,x + xrad,y+yrad,0xFF00FF00,0x3500FF00)
			elseif touch then
				gui.drawBox(x,y,x + xrad,y + yrad,0xFFFFFF00,0x35FFFF00)
			end
			if hp > 0 and invuln == false then
				gui.text((x-5) * xm,(y-5) * ym,"HP: " .. hp)
			end
			if invuln then
				draw_invuln(x,y,xrad,yrad)
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
	local stage = mainmemory.read_u8(0x7E0086) 
	if stage ~= 2 and stage ~= 5 then
		Player()
		Enemies()
	end
	emu.frameadvance()
end