-- Super Mario Bros. 3 (USA) Rev A collision box viewer V1.0
-- For use with Bizhawk
-- Author: Pasky (June 22, 2015)
-- TODO:
-- Find coin and hidden block collisions

memory.usememorydomain("System Bus")

local camx
local camy

local TOGGLE_MARIO = "ON"
local TOGGLE_OBJECTS = "ON" --Enemies etc...
local TOGGLE_PROJECTILES = "ON" 
local TOGGLE_OTHERATTACKS = "ON" -- Tanooki tail etc...
local TOGGLE_UI = "ON"
local K1,K2,K3,K4,K5,K6


function axis(x,y,color,size)
	if size == nil then
		size = 2
	end
	gui.drawLine(x+size,y,x-size,y,color)
	gui.drawLine(x,y+size,x,y-size,color)
	gui.drawPixel(x,y,0xFF000000)
end


local function camera()
	camx = memory.read_u8(0xFD) + (memory.read_u8(0x12) * 0xFF)
	camy = memory.read_u8(0xFC) + (memory.read_u8(0x12) * 0xFF)
end

--Player
local function player()
	if TOGGLE_MARIO == "ON" then
		if memory.read_u8(0xD87F) == 0x20 and memory.read_u8(0xD880) == 0x7B and memory.read_u8(0xD881) == 0xD9 then -- Bank check (BANK #0)
			local x = memory.read_u8(0xAB)
			local y = memory.read_u8(0xB4)
			local x1 = memory.read_u8(0x02)
			local y1 = memory.read_u8(0x06)
			local x2 = x1 + memory.read_u8(0x03)
			local y2 = y1 + memory.read_u8(0x07)
			gui.drawBox(x1,y1,x2,y2,0xFF0000FF,0x400000FF)
		end
	end
end

--Enemies hit boxes
local function objects()
	if TOGGLE_OBJECTS == "ON" then
		if memory.read_u8(0xD855) == 0xA5 and memory.read_u8(0xD856) == 0xED and memory.read_u8(0xD857) == 0xF0 and memory.read_u8(0xD858) == 0x09 then -- Bank check (BANK #0)
			local xreg = emu.getregister("X")
			local yreg = emu.getregister("Y")
			local x = memory.read_u8(0xAC + xreg)
			local y = memory.read_u8(0xB5 + xreg)
			local x1 = memory.read_u8(0x00)
			local y1 = memory.read_u8(0x04)
			local x2 = x1 + memory.read_u8(0x01)
			local y2 = y1 + memory.read_u8(0x05)
			gui.drawBox(x1,y1,x2,y2,0xFFFF0000,0x40FF0000)
		end
	end
end

--Projectiles 
local function eprojectiles()
	if TOGGLE_PROJECTILES == "ON" then
		if memory.read_u8(0xB7E4) == 0xBD and memory.read_u8(0xB7E5) == 0xBF and memory.read_u8(0xB7E6) == 0x05 then -- Bank check (BANK #3)
			local xreg = emu.getregister("X")
			local yreg = emu.getregister("Y")
			local x = bit.band(memory.read_u8(0x05C9 + xreg) - camx,0xFF) + 6
			local y = bit.band(memory.read_u8(0x05BF + xreg) - camy,0xFF) + 8
			axis(x,y,0xFFFFFFFF,4)
			
			local mx = memory.read_u8(0xAB)
			local my = memory.read_u8(0xB4)
			x1 = mx
			y1 = my + memory.read_u8(0xB6DE + yreg)
			x2 = x1 + 0x10
			y2 = y1 + memory.read_u8(0xB6E0 + yreg)
			gui.drawBox(x1,y1,x2,y2,0xFFFFFF00,0x40FFFF00)
		end
	end
end

--Player Projectiles
local function projectiles()
	if TOGGLE_PROJECTILES == "ON" then
		if memory.read_u8(0xA69A) == 0xDD and memory.read_u8(0xA69B) == 0x6D and memory.read_u8(0xA69C) == 0xA6 then -- Bank check (BANK #3)
			local xreg = emu.getregister("X")
			local yreg = emu.getregister("Y")
			local x = memory.read_u8(0xAC + yreg)
			local y = memory.read_u8(0xB5 + yreg)
			axis(memory.read_u8(0x0D),memory.read_u8(0x0C),0xFFFFFFFF,4)
			local x1 = x
			local y1 = y 
			local x2 = x1 + memory.read_u8(0xA67D + xreg)
			local y2 = y1 + memory.read_u8(0xA66D + xreg)
			gui.drawBox(x1,y1,x2,y2,0xFFFFFF00,0x40FFFF00)
		end
	end
end

--Player Other Attacks
local function other()
	if TOGGLE_OTHERATTACKS == "ON" then
		if memory.read_u8(0xDB4E) == 0x20 and memory.read_u8(0xDB4F) == 0x54 and memory.read_u8(0xDB50) == 0xDD then -- Bank check (BANK #0)
			--Tail
			local x = memory.read_u8(0xAB)
			local y = memory.read_u8(0xB4)
			local x1 = memory.read_u8(0x02)
			local y1 = memory.read_u8(0x06)
			local x2 = x1 + memory.read_u8(0x03)
			local y2 = y1 + memory.read_u8(0x07)
			gui.drawBox(x1,y1,x2,y2)
			
			--Enemy vuln (tail)
			local xreg = emu.getregister("X")
			local yreg = bit.band(memory.read_u8(0xC2F4 + memory.read_u8(0x0671 + xreg)),0xF) * 4
			x = memory.read_u8(0xAC + xreg)
			y = memory.read_u8(0xB5 + xreg)
			x1 = x + memory.read_u8(0xC2B4 + yreg)
			y1 = y + memory.read_u8(0xC2B6 + yreg)
			x2 = x1 + memory.read_u8(0xC2B5 + yreg)
			y2 = y1 + memory.read_u8(0xC2B7 + yreg)
			gui.drawBox(x1,y1,x2,y2,0xFFFFFF00,0x40FFFF00)
		end
	end
end

local function toggle(option)
	if option == "ON" then
		option = "OFF"
	else
		option = "ON"
	end
	return option
end	

local function PowerCycle()
	local powerup = memory.read_u8(0xED)
	if powerup == 0x06 then
		powerup = 0
	else
		powerup = powerup + 1
	end
	if powerup == 0x02 then
		memory.write_u8(0x7D2,0x27)
		memory.write_u8(0x7D4,0x16)
	else
		memory.write_u8(0x7D2,0x16)
		memory.write_u8(0x7D4,0x0F)
	end
	memory.write_u8(0xED,powerup)
end

local function check_inputs()
	local inputs = input.get()
	-- Mario boxes
	if inputs["NumberPad1"] == true then
		K1 = true
	end
	if inputs["NumberPad1"] == nil and K1 == true then -- Key released
		TOGGLE_MARIO = toggle(TOGGLE_MARIO)
		K1 = false
	end
	-- Enemy boxes
	if inputs["NumberPad2"] == true then
		K2 = true
	end
	if inputs["NumberPad2"] == nil and K2 == true then
		TOGGLE_OBJECTS = toggle(TOGGLE_OBJECTS)
		K2 = false
	end
	-- Projectile boxes
	if inputs["NumberPad3"] == true then
		K3 = true
	end
	if inputs["NumberPad3"] == nil and K3 == true then
		TOGGLE_PROJECTILES = toggle(TOGGLE_PROJECTILES)
		K3 = false
	end
	-- Other attacks
	if inputs["NumberPad4"] == true then
		K4 = true
	end
	if inputs["NumberPad4"] == nil and K4 == true then
		TOGGLE_OTHERATTACKS = toggle(TOGGLE_OTHERATTACKS)
		K4 = false
	end

	-- UI
	if inputs["Home"] == true then
		K5 = true
	end
	if inputs["Home"] == nil and K5 == true then
		TOGGLE_UI = toggle(TOGGLE_UI)
		K5 = false
	end
	
	--Powerup cycling
	if inputs["Insert"] == true then
		K6 = true
	end
	if inputs["Insert"] == nil and K6 == true then
		PowerCycle()
		K6 = false
	end
end

local function draw_UI()
	check_inputs()
	if TOGGLE_UI == "ON" then
		gui.drawBox(154,30,255,92,0xFF000000,0xA0000000)
		gui.drawText(156,28,"MARIO",0xFFFFFFFF,10,"Segoe UI")
		gui.drawText(190,28,"(NUM1)",0xFFFF0000,10,"Segoe UI")
		gui.drawText(226,28,"["..TOGGLE_MARIO.."]",0xFFFFFF00,10,"Segoe UI")
		gui.drawText(156,38,"ENEMY",0xFFFFFFFF,10,"Segoe UI")
		gui.drawText(226,38,"["..TOGGLE_OBJECTS.."]",0xFFFFFF00,10,"Segoe UI")
		gui.drawText(190,38,"(NUM2)",0xFFFF0000,10,"Segoe UI")
		gui.drawText(156,48,"PROJ.",0xFFFFFFFF,10,"Segoe UI")
		gui.drawText(226,48,"["..TOGGLE_PROJECTILES.."]",0xFFFFFF00,10,"Segoe UI")
		gui.drawText(190,48,"(NUM3)",0xFFFF0000,10,"Segoe UI")
		gui.drawText(156,58,"OTHER",0xFFFFFFFF,10,"Segoe UI")
		gui.drawText(226,58,"["..TOGGLE_OTHERATTACKS.."]",0xFFFFFF00,10,"Segoe UI")
		gui.drawText(190,58,"(NUM4)",0xFFFF0000,10,"Segoe UI")
		gui.drawText(156,68,"POWERUP",0xFFFFFFFF,10,"Segoe UI")
		gui.drawText(214,68,"(INSERT)",0xFFFF0000,10,"Segoe UI")
		gui.drawText(156,78,"TOGGLE UI",0xFFFFFFFF,10,"Segoe UI")
		gui.drawText(214,78,"(HOME)",0xFFFF0000,10,"Segoe UI")
	end
end

event.onmemoryexecute(player,0xD87F)
event.onmemoryexecute(objects,0xD855)
event.onmemoryexecute(projectiles,0xA69A)
event.onmemoryexecute(eprojectiles,0xB7E4)
event.onmemoryexecute(other,0xDB4E)

while true do
	draw_UI()
	camera()
	emu.frameadvance()
end