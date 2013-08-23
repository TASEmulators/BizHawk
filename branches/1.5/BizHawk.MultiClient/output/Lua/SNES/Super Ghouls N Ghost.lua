----------------------------------------------------
--Super Ghouls n' Ghosts Hitbox Display Lua Script--
----------------------------------------------------

--Author Pasky13


---Blue = Arthur's Hitbox
---Red = If this box touches arthur's he is damaged
---Green = Projectile vulnerability for enemies
---White = Arthurs magic and weapons

local left_edge = 0x7E15DD
local top_edge = 0x7E15E1
local xm 
local ym

memory.usememorydomain("CARTROM")

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function draw_axis(x,y)
	gui.drawLine(x,y-2,x,y+2,0x80FFFFFF)
	gui.drawLine(x-2,y,x+2,y,0x80FFFFFF)
end

local function draw_arthur()
	local arthur = 0x43C
	local pointoff = bit.lshift(bit.lshift(bit.band(mainmemory.read_u8(arthur + 0x09),0x01),1),1)
	local yrad = memory.read_u8(0x3963 + pointoff)
	local xrad = memory.read_u8(0x3963 + pointoff + 1)
	local yoff = memory.read_s8(0x3965 + pointoff)
	local xoff = memory.read_s8(0x3965 + pointoff + 1)
	local x = mainmemory.read_u16_le(arthur + 0x1F)
	local y = mainmemory.read_u16_le(arthur + 0x22)
	
	x = x - mainmemory.read_u16_le(left_edge)
	y = y - mainmemory.read_u16_le(top_edge)
	gui.drawBox((x+xoff) - (xrad), (y+yoff) - (yrad), (x+xoff) + (xrad), (y+yoff) + (yrad),0xF00000FF,0x300000FF)
	--Comment/Uncomment out next line to Show/hide arthur's hitbox center axis
	--draw_axis(x,y)
end

local function draw_projvuln(x,y,xrad,yrad,base)
		gui.drawBox(x - (xrad), y - (yrad), x + (xrad), y + (yrad),0xF02AFF00,0x402AFF00)
end



local function draw_enemies()

	local ostart = 0x090F
	local oend = 30
	local base = 0
	local pointoff = 0
	local xrad = 0
	local yrad = 0
	local x = 0
	local y = 0
		
	for i = 0,oend,1 do
	
		if i ~= 0 then
			base = ostart + (0x41 * i)
		else
			base = ostart
		end
		
		
		if hasbit(mainmemory.read_u8(base + 0x09),findbit(7)) == true then
			
			if mainmemory.read_u8(base+1) ~= 0 and mainmemory.read_u8(base) ~= 0 then
			
				pointoff = bit.lshift(bit.band(mainmemory.read_u8(base + 0x06),0x00FF),1)
				xrad = memory.read_u8(0x56E1 + pointoff)
				yrad = memory.read_u8(0x56E2 + pointoff)
				x = mainmemory.read_u16_le(base + 0x1F)
				y = mainmemory.read_u16_le(base + 0x22)
				
				life = mainmemory.read_u8(base + 0x0E)
				
				x = x - mainmemory.read_u16_le(left_edge)
				y = y - mainmemory.read_u16_le(top_edge)
				
				--Red boxes
				gui.drawBox(x - (xrad), y - (yrad), x + (xrad), y + (yrad),0xFFFF0000,0x70FF0000)
				xrad = memory.read_u8(0x585D + pointoff)
				yrad = memory.read_u8(0x585E + pointoff)
				
				--Comment/Uncomment out next line to hide/show drawing the projectile vulnerability boxes
				
				draw_projvuln(x,y,xrad,yrad,base)
				
				if life > 0 then
				--Comment out next line to hide HP display over objects
				  gui.text(x * xm, (y-10) * ym, life)
				end
				
				--Comment out next line to hide box center axis
				draw_axis(x,y)
				
			end
		end
		
	end
end

local function draw_weapons()

	local ostart = 0x047D
	local oend = 9
	local base = 0
	local weapon = 0x14D3
	local dmg = 0
	local pointoff = 0
	local wep_xrad = 0
	local wep_yrad = 0
	local mag_xrad = 0
	local mag_yrad = 0
	local x = 0
	local y = 0
	
	pointoff = bit.lshift(mainmemory.read_u8(weapon),1)
	wep_xrad = memory.read_u8(0x398b + pointoff)
	wep_yrad = memory.read_u8(0x398c + pointoff)
	mag_xrad = memory.read_u8(0x39ab + pointoff)
	mag_yrad = memory.read_u8(0x39ac + pointoff)
		
	for i = 0,oend,1 do
	
	
		if i ~= 0 then
			base = ostart + (0x41 * i)
		else
			base = ostart
		end
		
		if mainmemory.read_u8(base+1) ~= 0 and mainmemory.read_u8(base) ~= 0 then
			x = mainmemory.read_u16_le(base + 0x1F)
			y = mainmemory.read_u16_le(base + 0x22)
		
			dmg = mainmemory.read_u8(base + 0x0E)
			
			x = x - mainmemory.read_u16_le(left_edge)
			y = y - mainmemory.read_u16_le(top_edge)
			gui.drawBox(x - (wep_xrad), y - (wep_yrad), x + (wep_xrad), y + (wep_yrad),0xFFFFFFFF,0x40FFFFFF)
			
			if dmg > 0 then
			--Comment out next line to hide weapon DMG display over objects
				gui.text(x * xm, (y-10) * ym, dmg)
			end
			--Comment out next line to hide box center axis
			--draw_axis(x,y)

		end
	end
	
	-- If we have the gold armor and not the magic bracelet, check for magic hitboxes
	if mainmemory.read_u8(0x7E14BA) == 0x04 and mainmemory.read_u8(weapon) ~= 0x0E then 
		
		ostart = 0x0707
		
		for i = 0,oend-2,1 do
		
			if i ~= 0 then
				base = ostart + (0x41 * i)
			else
				base = ostart
			end
			
			if mainmemory.read_u8(base+1) ~= 0 and mainmemory.read_u8(base) ~= 0 then
				x = mainmemory.read_u16_le(base + 0x1F)
				y = mainmemory.read_u16_le(base + 0x22)
		
				dmg = mainmemory.read_u8(base + 0x0E)
			
				x = x - mainmemory.read_u16_le(left_edge)
				y = y - mainmemory.read_u16_le(top_edge)
				gui.drawBox(x - (mag_xrad), y - (mag_yrad), x + (mag_xrad), y + (mag_yrad),0xFFFFFFFF,0x40FFFFFF)

				if dmg > 0 then
				--Comment out next line to hide magic DMG display over objects
					gui.text(x * xm, (y-10) * ym, dmg)
				end
				--Comment out next line to hide box center axis
				--draw_axis(x,y)
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
	draw_arthur()
	draw_enemies()
	draw_weapons()
	emu.frameadvance()
end
