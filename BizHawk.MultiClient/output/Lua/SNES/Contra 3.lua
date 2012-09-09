function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function Player()
	local x = mainmemory.read_u8(0x206)
	local y = mainmemory.read_u8(0x210)
	local x2 = mainmemory.read_u8(0x228)
	local y2 = mainmemory.read_u8(0x22a)
	local x_offscreen = mainmemory.read_u8(0x207)
	local y_offscreen = mainmemory.read_u8(0x211)
	local x2_offscreen = mainmemory.read_u8(0x229)
	local y2_offscreen = mainmemory.read_u8(0x22b)
	local active = mainmemory.read_u8(0x216)
	
	-- Checks if the box went off screen and adjusts
	if x2_offscreen == 1 then
		x2 = 255 + x2
	elseif x2_offscreen == 255 then
		x2 = 0 -(255 - x2)
	end
	
	if y2_offscreen == 1 then
		y2 = 255 + y
	elseif y2_offscreen == 255 then
		y2 = 0 - (255 - y2)
	end
	
	if x_offscreen == 1 then
		x = 255 + x
	elseif x_offscreen == 255 then
		x = 0 -(255 - x)
	end

	if y_offscreen == 1 then
		y = 255 + y
	elseif y_offscreen == 255 then
		y = 0 - (255 - y)
	end
	
	if active > 0 then
		if hasbit(active,findbit(2)) == true or mainmemory.read_u16_le(0x1F88) > 0 then
			gui.drawBox(x,y,x2,y2,0xFFFDD017,0x35FDD017)
		else
			gui.drawBox(x,y,x2,y2,0xFF0000FF,0x350000FF)
		end
	end
end

local function Enemies()
	local start = 0x240
	local base = 0
	local oend = 33
	local x
	local x_offscreen
	local y
	local y_offscreen
	local xrad
	local yrad
	local active
	local touch
	local projectile
	local hp
	
	for i = 0,oend,1 do
		base = start 
		
		if i > 0 then
			base = start + (i * 0x40)
		end
		
		active = mainmemory.read_u8(base + 0x16)
		hp = mainmemory.read_s16_le(base + 6)
		if active > 0 then
			
			touch = hasbit(active,findbit(4))
			projectile = hasbit(active,findbit(5))
			
			x = mainmemory.read_u8(base + 0xa)
			x2 = mainmemory.read_u8
			x_offscreen = mainmemory.read_u8(base + 0xb)
			y = mainmemory.read_u8(base + 0xe)
			y_offscreen = mainmemory.read_u8(base + 0xf)
			xrad = mainmemory.read_s16_le(base+0x28)
			yrad = mainmemory.read_s16_le(base+0x2A)
			x2  = x
			
			-- Checks if the box went off screen and adjusts
			if x_offscreen == 1 then
				x = 255 + x
			elseif x_offscreen == 255 then
				x = 0 -(255 - x)
			end
			
			if y_offscreen == 1 then
				y = 255 + y
			elseif y_offscreen == 255 then
				y = 0 - (255 - y)
			end
			
			if projectile and touch == true then
				 gui.drawBox(x,y,x+ xrad,y+yrad,0xFFFF0000,0x35FF0000)
				if hp > 0 then
					gui.text((x-5) * xmult,(y-5) * ymult,"HP: " .. hp)
				end
			elseif projectile == true then
				gui.drawBox(x,y,x+ xrad,y+yrad,0xFF00FF00,0x3500FF00)
				if hp > 0 then
					gui.text((x-5) * xmult,(y-5) * ymult,"HP: " .. hp)
				end
			elseif touch == true then
				gui.drawBox(x,y,x + xrad,y + yrad,0xFFFFFF00,0x35FFFF00)
				if hp > 0 then
					gui.text((x-5) * xmult,(y-5) * ymult,"HP: " .. hp)
				end
			end
		end
	end	
	
end

local function scaler()
	xmult = client.screenwidth() / 256
	ymult = client.screenheight() / 224
end

while true do
	scaler()
	Player()
	Enemies()
	emu.frameadvance()
end