--trace cv.log,0,{tracelog "A0=%08X, A1=%08X, A2=%08X, A3=%08X, A4=%08X, A5=%08X, A6=%08X, A7=%08X, D0=%08X, D1=%08X, D2=%08X, D3=%08X, D4=%08X, D5=%08X, D6=%08X ",a0,a1,a2,a3,a4,a5,a6,a7,d0,d1,d2,d3,d4,d5,d6}

-------------------------------
----Castlevania Blood Lines----
-------------------------------

---------------
----Toggles----
---------------
local draw_ehp = true -- Set to false to turn off HP display for enemies
local draw_php = true -- Set to false to turn off HP display for 1P
local draw_pproj = true --Set to false to turn off player projectiles

local draw_ph = true -- Set to false to turn off player hitboxes
local draw_eh = true -- Set to false to turn off enemie hitboxes

local infhp = true --Set to false to turn off infinite HP for the player


local playerbase = 0xFFB300
local face = 0x2C
local php = 0xFF9C11
local weapon = 0xFFB480
local spear = 0xFF9CA7
local enemybase = 0xFFB380

local xcord = 0x18
local ycord = 0x1C




local function playerhitbox()

	-------------------------
	----Vulnerability box----
	-------------------------
	local facing = mainmemory.read_u8(playerbase+face)
	local x = mainmemory.read_u16_be(playerbase+0x18)
	local y = mainmemory.read_u16_be(playerbase+0x1C)
	local xoff = mainmemory.read_s16_be(playerbase+0x34) 
	local yoff = mainmemory.read_s16_be(playerbase+0x36)
	local xrad = mainmemory.read_u16_be(playerbase+0x38)
	local yrad = mainmemory.read_u16_be(playerbase+0x3A)
		
	gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xF00000FF,0x300000FF)

		
	---------------------	
	----weapon hitbox----
	---------------------
	facing = mainmemory.read_u8(playerbase+face)
	
	x = mainmemory.read_u16_be(weapon+0x18)
	y = mainmemory.read_u16_be(weapon+0x1C)
	xoff = mainmemory.read_s16_be(weapon+0x34) 
	yoff = mainmemory.read_s16_be(weapon+0x36)
	xrad = mainmemory.read_u16_be(weapon+0x38)
	yrad = mainmemory.read_u16_be(weapon+0x3A)
	
	if mainmemory.read_u8(weapon+0x01) == 1 and mainmemory.read_u8(weapon+0x28) > 0 then
		gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFFFFFFFF,0x40FFFFFF)
	end

end

local function player_projectiles()

	local start = 0xFFC900
	local base 
	local x 
	local y
	local xoff
	local yoff
	local xrad
	local yrad
	local facing
	local ehp
	local oend = 32
	local active = false
	
	for i = 0,oend,1 do
		active = false
		base = start + (i * 0x80)
		
		x = mainmemory.read_u16_be(base + xcord)
		y = mainmemory.read_u16_be(base + ycord)
		
		if mainmemory.read_u8(base+1) == 1 then
			active = true
		end
		
		if y < 224 and y > 0 and x < 320 and x > 0 and active == true and (mainmemory.read_u8(base+0x45)) > 0 and mainmemory.read_u8(base+0x4F) > 0 and mainmemory.read_u8(base+0x28) == 1 then
			
			facing = mainmemory.read_u8(base+face)
			x = mainmemory.read_u16_be(base+0x18)
			y = mainmemory.read_u16_be(base+0x1C)
			xoff = mainmemory.read_s16_be(base+0x34) 
			yoff = mainmemory.read_s16_be(base+0x36)
			xrad = mainmemory.read_u16_be(base+0x38)
			yrad = mainmemory.read_u16_be(base+0x3A)
			
			gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFFFFFFFF,0x40FFFFFF)
									
			gui.text((x-8) * xmult,(y+10) * ymult,"DMG: " .. mainmemory.read_u8(base+0x45))
		end
				
	end
end

local function playerinfo()
	local x = mainmemory.read_u16_be(playerbase+xcord)
	local y = mainmemory.read_u16_be(playerbase+ycord)
	local hp = mainmemory.read_u8(php)
	
	if draw_php == true then
		gui.text((x-12) * xmult,y * ymult,"HP: " .. hp)
	end
	
	if draw_ph == true then
		playerhitbox()
	end
	
	if draw_pproj == true then
		player_projectiles()
	end
	
end

local function enemyinfo()

	local base 
	local x 
	local y
	local xoff
	local yoff
	local xrad
	local yrad
	local facing
	local ehp
	local oend = 44
	local active = false

	for i = 0,oend,1 do
		
		active = false
		base = enemybase + (i * 0x80)
		
		x = mainmemory.read_u16_be(base + xcord)
		y = mainmemory.read_u16_be(base + ycord)
		
		if mainmemory.read_u8(base+1) == 1 then
			active = true
		end
		
		local ehp = mainmemory.read_u8(base+0x55)
		
		if base ~= weapon then
		
			if y < 224 and y > 0 and x < 320 and x > 0 and active == true then
			
				if draw_ehp == true and ehp > 0 then
					gui.text((x-10) * xmult,y  * ymult,"HP: " .. ehp)
				end 
				
				--------------------
				----Enemy Hitbox----
				--------------------
				
				if draw_eh == true then
					facing = mainmemory.read_u8(base+face)
					x = mainmemory.read_u16_be(base+0x18)
					y = mainmemory.read_u16_be(base+0x1C)
					xoff = mainmemory.read_s16_be(base+0x34) 
					yoff = mainmemory.read_s16_be(base+0x36)
					xrad = mainmemory.read_u16_be(base+0x38)
					yrad = mainmemory.read_u16_be(base+0x3A)
					gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFFFF0000,0x40FF0000)
				end
					
			end
		end
		
	end
	
end



local function infinitehp()
	memory.writebyte(php,80)
end

local function scaler()
	xmult = client.screenwidth() / 320
	ymult = client.screenheight() / 224
end

while true do
	scaler()
	playerinfo()
	enemyinfo()
	if infhp == true then
		infinitehp()
	end
	emu.frameadvance()
end
