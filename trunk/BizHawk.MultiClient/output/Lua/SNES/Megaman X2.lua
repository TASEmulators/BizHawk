--Author Pasky13

---------------
----GLOBALS----
---------------
local pbase = 0x9D8
local px = pbase +0x5
local py = pbase +0x8
local cx = 0x7E1E5D
local cy = 0x7E1E60
local xm
local ym

---------------
----TOGGLES----
---------------
local draw_megaman = true
local draw_enemies = true
local draw_hpvalues = true
local draw_projectiles = true

memory.usememorydomain("CARTROM")

local function megaman()

	local camx = mainmemory.read_u16_le(cx)
	local camy = mainmemory.read_u16_le(cy)
	local x = mainmemory.read_u16_le(px) - camx
	local y = mainmemory.read_u16_le(py) - camy
	local facing = mainmemory.read_u8(pbase + 0x11)
	local boxpointer = mainmemory.read_u16_le(pbase + 0x20) + 0x28000
	local xoff = memory.read_s8(boxpointer + 0)
	local yoff = memory.read_s8(boxpointer + 1)
	local xrad = memory.read_u8(boxpointer + 2)
	local yrad = memory.read_u8(boxpointer + 3)
	
	if facing > 0x45 then
		xoff = xoff * -1
	end
	
	gui.drawBox(x + xoff +xrad,y + yoff + yrad, x + xoff - xrad, y + yoff - yrad,0xFF0000FF,0x400000FF)
end

local function enemies()

	local x 
	local xoff
	local xrad
	local y
	local yoff
	local yrad
	local camx = mainmemory.read_u16_le(cx)
	local camy = mainmemory.read_u16_le(cy)
	local base
	local boxpointer
	local facing
	local fill
	local outl
	local start = 0xD18
	local oend = 31
	
	for i = 0, oend,1 do
	
		base = start + (i * 0x40)
		
		if i == 0 then
			base = start
		end
		
		if mainmemory.read_u8(base) ~= 0 and mainmemory.read_u8(base + 0x27) ~= 128 then
			
			if i > 14 and i < 25 then
				if draw_projectiles == true then
					fill = 0x40FFFFFF
					outl = 0xFFFFFFFF
				else
					fill = 0x00000000
					outl = 0x00000000
				end
			else
				fill = 0x40FF0000
				outl = 0xFFFF0000
			end	
			
			if i > 24 then
				fill = 0x40FFFF00
				outl = 0xFFFFFF00
			end
			
			facing = mainmemory.read_u8(base + 0x11)
			x = mainmemory.read_u16_le(base + 5) - camx
			y = mainmemory.read_u16_le(base + 8) - camy
			boxpointer = mainmemory.read_u16_le(base +0x20) + 0x28000
			xoff = memory.read_s8(boxpointer + 0)
			yoff = memory.read_s8(boxpointer + 1)
			xrad = memory.read_u8(boxpointer + 2)
			yrad = memory.read_u8(boxpointer + 3)
			
		if facing > 0x45 then
				xoff = xoff * -1
		end
		
		
		--gui.text(x,y,string.format("%X",base))  -- Debug
		gui.drawBox(x + xoff +xrad,y + yoff + yrad, x + xoff - xrad, y + yoff - yrad,outl, fill)	
			
			if draw_hpvalues == true and mainmemory.read_u8(base+0x27) > 0 and mainmemory.read_u8(base+0x27) ~= 128 then
				if i < 15 or i > 20 then
					gui.text((x-5) * xm,(y-5) * ym,"HP: " .. mainmemory.read_u8(base+0x27))
				end
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
	if draw_megaman == true then
		megaman()
	end
	if draw_enemies == true then
		enemies()
	end
	emu.frameadvance()
end