--Splatterhouse 2 (USA) Collision box viewer
--Author Pasky
--For use with Bizhawk


local cx = 0
local player = false
local attack = false
local weapon = false

local function camera()
	cx = mainmemory.read_u16_be(0x9E)
end

local function drawAxis(x1,y1,x2,y2)
	local x = ((x2 - x1) / 2) + x1
	local y = ((y2 - y1) / 2) + y1
	local xrad = (x2 - x1) / 2
	local yrad = (y2 - y1) / 2
	gui.drawLine(x-xrad,y,x+xrad,y)
	gui.drawLine(x,y-yrad,x,y+yrad)
end

local function touch_collision()
	
	local A6 = bit.band(emu.getregister("M68K A6"),0xFFFF)
	local e = {0,0,0,0}
	local p = {0,0,0,0}
	
	for i = 0,3,1 do
		e[i] = mainmemory.read_s16_be(A6 - 0x0A - (i * 2))
		p[i] = mainmemory.read_s16_be(A6 - 0x02 - (i * 2))
	end
	gui.drawBox(e[0]-cx,e[2],e[1]-cx,e[3],0xFFFF0000,0x40FF0000)
	if player == false then
		gui.drawBox(p[0]-cx,p[2],p[1]-cx,p[3],0xFF0000FF,0x400000FF)
		if mainmemory.read_u16_be(0xEA) > 0 then
			drawAxis(p[0]-cx,p[2],p[1]-cx,p[3])
		end
		player = true
	end
end

local function attack_collision()
	local A6 = bit.band(emu.getregister("M68K A6"),0xFFFF)
	local a = {0,0,0,0}
	for i = 0,3,1 do
		a[i] = mainmemory.read_s16_be(A6 - 0x1A - (i * 2))
	end
	if attack == false then
		gui.drawBox(a[0]-cx,a[2],a[1]-cx,a[3],0xFFFFFFFF,0x40FFFFFF)
		attack = true
	end
	
end

local function weapon_collision()
	local A6 = bit.band(emu.getregister("M68K A6"),0xFFFF)
	local w = {0,0,0,0}
	for i = 0,3,1 do
		w[i] = mainmemory.read_s16_be(A6 - 0x12 - (i * 2))
	end
	if weapon == false then
		gui.drawBox(w[0]-cx,w[2],w[1]-cx,w[3],0xFFFFFFFF,0x40FFFFFF)
		weapon = true
	end
end

local function reset()
	player = false
	attack = false
	weapon = false
end


event.onmemoryexecute(touch_collision,0x1494E)
event.onmemoryexecute(attack_collision,0x14826)
event.onmemoryexecute(weapon_collision,0x14650)

while true do
	camera()
	emu.frameadvance()
	reset()
end