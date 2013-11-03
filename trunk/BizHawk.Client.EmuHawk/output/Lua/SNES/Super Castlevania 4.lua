----------------------------
----Super Castlevania IV----
----------------------------

--Author Pasky13

---------------
----TOGGLES----
---------------

--Box type toggles
local candle = true
local enemy = true
local player = true


--Center axis toggles
local player_axis = false
local whip_axis = false
local projectile_axis = true
local enemy_axis = true
local candle_axis = false

--Hitpoint display
local playerhp = true
local enemyhp = true

--Cheats
local cheats = false

------------------
---END TOGGLES----
------------------


---------------
----GLOBALS----
---------------

local pbase = 0x540
local ebase = 0x580
local wbase = 0x200
local pjbase = 0x440

local ux = 0xA
local uy = 0xE

local uxrad = 0x28
local uyrad = 0x2A

local eactive = 0x10

local camx = 0x1280
local camy = 0x1298

local elife = 0x06

local plife = 0x13F4
local hearts = 0x13F2
local facing = 0x0578
local timer = 0x13F0

local xs = client.screenwidth() / 256
local ys = client.screenheight() / 224
-------------------
----END GLOBALS----
-------------------

local function centeraxis(x,y)
	gui.drawLine(x,y+2,x,y-2,0xFFFFFFFF)
	gui.drawLine(x+2,y,x-2,y,0xFFFFFFFF)
end

local function player_hitbox()
	local x = mainmemory.read_u16_le(pbase+ux) - mainmemory.read_u16_le(camx)
	local y = mainmemory.read_u16_le(pbase+uy) - mainmemory.read_u16_le(camy)
	local cr = mainmemory.read_u8(0x576)

	if cr ~= 0x0F then
		gui.drawBox(x+7,y+27,x-7,y-19,0xFF0000FF,0x400000FF)
		if playerhp == true then
			gui.text((x-10) * xs,(y-26) * ys,"HP:" .. mainmemory.read_u8(plife))
		end
	else
		gui.drawBox(x+7,y+cr,x-7,y-cr,0xFF0000FF,0x400000FF)
			if playerhp == true then
				gui.text((x-10) * xs,(y-cr-7) * ys,"HP:" .. mainmemory.read_u8(plife))
			end
	end
	

	
	if player_axis == true then
		centeraxis(x,y)
	end
	
end

local function player_projectiles()
	local base = 0
	local x
	local y
	local xrad
	local yrad
	local oend = 3
	
	for i = 0,oend,1 do
	
		base = pjbase + (i * 0x40)
		
		if i == 0 then
			base = pjbase
		end
		
		if mainmemory.read_u16_le(base) ~= 0 then
			x = mainmemory.read_u16_le(base+ux) - mainmemory.read_u16_le(camx)
			y = mainmemory.read_u16_le(base+uy) - mainmemory.read_u16_le(camy)
			xrad = mainmemory.read_u16_le(base+uxrad)
			yrad = mainmemory.read_u16_le(base+uyrad)
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFFFFFF,0x40FFFFFF)
			
			if projectile_axis == true then
				centeraxis(x,y)
			end
			
		end
		
	end
end

local function player_whip()

	local base = 0
	local x
	local y
	local xrad
	local yrad
	local oend = 8
	
	for i = 0,oend,1 do
	
		base = wbase + (i * 0x40)
		
		if i == 0 then
			base = wbase
		end
		
		if mainmemory.read_u16_le(base) ~= 0 then
			x = mainmemory.read_u16_le(base+ux) - mainmemory.read_u16_le(camx)
			y = mainmemory.read_u16_le(base+uy) - mainmemory.read_u16_le(camy)
			xrad = mainmemory.read_u16_le(base+uxrad)
			yrad = mainmemory.read_u16_le(base+uyrad)
			
			if xrad == 0 and yrad == 0 then
				if base == 0x0400 then
					gui.drawBox(x-0x10,y-0x04,x+0x10,y+0x04,0xFFFFFFFF,0x40FFFFFF)
				else
					gui.drawBox(x-0x04,y-0x04,x+0x04,y+0x04,0xFFFFFFFF,0x40FFFFFF)
				end
			else
				gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFFFFFF,0x40FFFFFF)
			end
			
			if whip_axis == true then
				centeraxis(x,y)
			end
		end
		
	end
end

local function object_hitbox()
	local base = 0
	local x 
	local y
	local xrad
	local yrad
	local oend = 36
	local drawn
	local life 
	for i = 0,oend,1 do
	
		base = ebase + (i * 0x40)
		drawn = false
		
		if i == 0 then
			base = ebase
		elseif
			base == 0x540 then
			drawn = true
		end
		
		if mainmemory.read_u16_le(base+0x10) == 0x0E then
			if candle == true then
				x = mainmemory.read_u16_le(base+ux) - mainmemory.read_u16_le(camx)
				y = mainmemory.read_u16_le(base+uy) - mainmemory.read_u16_le(camy)
				xrad = mainmemory.read_u16_le(base+uxrad)
				yrad = mainmemory.read_u16_le(base+uyrad)
				gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFFD000,0x40FFD000)
				drawn = true
				
				if candle_axis == true then
					centeraxis(x,y)
				end
			end
			
		end
		
		if mainmemory.read_u16_le(base+0x10) ~= 0 and drawn == false then
		
			x = mainmemory.read_u16_le(base+ux) - mainmemory.read_u16_le(camx)
			y = mainmemory.read_u16_le(base+uy) - mainmemory.read_u16_le(camy)
			xrad = mainmemory.read_u16_le(base+uxrad)
			yrad = mainmemory.read_u16_le(base+uyrad)
			life = mainmemory.read_u16_le(base+elife)
			
			gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFF0000,0x40FF0000)
			drawn = true
			
			if enemyhp == true then
				if life > 0 and life ~= 255 then
					gui.text((x-10) * xs,(y-yrad-7) * ys,"HP:" .. life)
				end
			end
			if enemy_axis == true then
				centeraxis(x,y)
			end
		end
		
		
	end
end

local function cheat()
	memory.writebyte(plife,16)
	memory.writeword(timer,1024)
	memory.writebyte(hearts,32)
end

local function scaler()
	xs = client.screenwidth() / 256
	ys = client.screenheight() / 224
end

while true do
	scaler()
	if player == true then
		player_hitbox()
		player_whip()
		player_projectiles()
	end
	
	if enemy == true then
		object_hitbox()
	end
	if cheats == true then
		cheat()
	end
	emu.frameadvance()
end