-- Super Castlevania IV (USA/JP) Collision box viewer
-- For use with Bizhawk
-- Author Pasky


local player = false

function findbit(p) 
	return 2 ^ (p - 1)
end

local function ax(x,y)
	gui.drawLine(x,y+4,x,y-4,0xFFFF0000)
	gui.drawLine(x+4,y,x-4,y,0xFFFF0000)
	gui.drawPixel(x,y,0xFFFFFFFF)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function hex(val)
	val = string.format("%X",val)
	if string.len(val) == 1 then
		val = "0" .. val
	end
	return val
end

local function camera()
	camx = mainmemory.read_u16_le(0x1280)
	camy = mainmemory.read_u16_le(0x1298)
end

local function objects()
	local xreg = emu.getregister("X")
	local areg = emu.getregister("A")
	local c = {0xFFFF0000,0x40FF0000}
	local o = { mainmemory.read_u16_le(0x8) - camx,mainmemory.read_u16_le(0x10), mainmemory.read_u16_le(0xA) - camy, mainmemory.read_u16_le(0x12) }
	if mainmemory.read_u16_le(xreg + 0x10) == 0xE then
		c[1] = 0xFF0000FF
		c[2] = 0x400000FF
		gui.drawBox(o[1]-o[2],o[3]-o[4],o[1]+o[2],o[3]+o[4],c[1],c[2]) -- Draw non-player objects
	else
		if hasbit(areg,findbit(1)) then
			gui.drawBox(o[1]-o[2],o[3]-o[4],o[1]+o[2],o[3]+o[4],c[1],c[2])  -- Draw objects that simon can collide with
			if o[2] == 0 and o[4] == 0 then -- enemy projectile, mark the center with an axis since there is no box
				ax(o[1],o[3])
			end
		end
	end
	

	
	
	if player == false then
		c[1] = 0xFF0000FF
		c[2] = 0x400000FF
		o = { mainmemory.read_u16_le(0x54A) - camx, 0x08, mainmemory.read_u16_le(0x54E), 0x13 }
		gui.drawBox(o[1]-o[2],o[3]-o[4],o[1]+o[2],o[3]+o[4],c[1],c[2]) -- Draw player hurtbox
		player = true -- Used so it isn't drawn every collision check
	end
end

local function weapons()
	local x,y,xr,yr,base
	for i = 0,7,1 do
		base = 0x200 + (i * 0x40)
		if mainmemory.read_u16_le(base) ~= 0 then
			x = mainmemory.read_u16_le(base + 0xA) - camx
			y = mainmemory.read_u16_le(base + 0xE) - camy
			xr = mainmemory.read_u16_le(base + 0x28)
			yr = mainmemory.read_u16_le(base + 0x2A)
			if xr == 0 and yr == 0 then -- check if it's the whip
				if base == 0x400 then
					gui.drawBox(x-0x10,y-0x04,x+0x10,y+0x04,0xFFFFFFFF,0x40FFFFFF)
				else
					gui.drawBox(x-0x04,y-0x04,x+0x04,y+0x04,0xFFFFFFFF,0x40FFFFFF)
				end
			else
				gui.drawBox(x-xr,y-yr,x+xr,y-yr)
			end
		end
	end
end

local function pproj()
	local yreg = emu.getregister("Y")
	local o = { mainmemory.read_u16_le(yreg + 0xA) - camx, mainmemory.read_u16_le(yreg + 0x28), mainmemory.read_u16_le(yreg + 0xE) - camy, mainmemory.read_u16_le(yreg + 0x2A) }
	gui.drawBox(o[1]-o[2],o[3]-o[4],o[1]+o[2],o[3]+o[4],0xFFFFFFFF,0x40FFFFFF)
end

local function reset()
	player = false
end

event.onmemoryexecute(objects,0x00DC7A)
event.onmemoryexecute(pproj,0xDD74)

while true do
	camera()
	weapons()
	emu.frameadvance()
	reset()
end