local camx
local camy
local HPDISPLAY = true

memory.usememorydomain("PRG ROM")

local function camera()
	camx = mainmemory.read_u8(0x02) 
	if mainmemory.read_u8(0x03) > 0x7F then
		camx = mainmemory.read_s8(0x02)
	else
		camx = camx + mainmemory.read_u8(0x03) * 256
	end
	camy = mainmemory.read_u8(0x04) 
	if mainmemory.read_u8(0x05) > 0x7F then
		camy = mainmemory.read_s8(0x04) + 17
	else
		camy = camy + mainmemory.read_u8(0x05) * 239 
	end 
end

local function player()
	local x = mainmemory.read_u8(0x70) + mainmemory.read_u8(0x90) * 256 - camx
	local y = mainmemory.read_u8(0xB0) + mainmemory.read_u8(0xD0) * 239 - camy
	local x2 = mainmemory.read_u8(0x5C0)
	local y2 = mainmemory.read_u8(0x5E0)
	local invuln = mainmemory.read_u8(0x71A)
	
	if bit.band(mainmemory.read_u8(0x3A1),0xF0) ~= 0 and invuln == 0 then -- If box exists
		local off1 = bit.band(mainmemory.read_u8(0x3A1),0xF) * 4
		local off2 = bit.bor(bit.band(mainmemory.read_u8(0x421),0x40),off1)
		
		local xoff = memory.read_s8(0x35691 + off2)
		local xrad = memory.read_u8(0x35692 + off2)
		local yoff = memory.read_s8(0x35693 + off2)
		local yrad = memory.read_u8(0x35694 + off2)
		
		gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFF0000FF,0x300000FF)
	end
	
	
	--Player's attacks
	for i = 2,4,1 do
	
		if mainmemory.read_u8(0x4A0 + i) ~= 0 and i ~= 3 then
			x = mainmemory.read_u8(0x70 + i) + mainmemory.read_u8(0x90 + i) * 256 - camx
			y = mainmemory.read_u8(0xB0 + i) + mainmemory.read_u8(0xD0 + i) * 239 - camy
			x2 = mainmemory.read_u8(0x5C0 + i)
			y2 = mainmemory.read_u8(0x5E0 + i)
			
			off1 = bit.band(mainmemory.read_u8(0x3A0 + i),0xF) * 4
			off2 = bit.bor(bit.band(mainmemory.read_u8(0x422),0x40),off1)
			
			xoff = memory.read_s8(0x35691 + off2)
			xrad = memory.read_u8(0x35692 + off2)
			yoff = memory.read_s8(0x35693 + off2)
			yrad = memory.read_u8(0x35694 + off2)
			
			gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad)
		end
	end
end

local function enemies()
	for i = 5,0x1F,1 do
	
		if mainmemory.read_u8(0x4A0 + i) ~= 0 then
			local x = mainmemory.read_u8(0x70 + i) + mainmemory.read_u8(0x90 + i) * 256 - camx
			local y = mainmemory.read_u8(0xB0 + i) + mainmemory.read_u8(0xD0 + i) * 239 - camy
			local x2 = mainmemory.read_u8(0x5C0 + i)
			local y2 = mainmemory.read_u8(0x5E0 + i)
			local hp = mainmemory.read_u8(0x3C0 + i)
			
			if x < 256 and x > 0 and y < 239 and y > 0 then			
				if bit.band(mainmemory.read_u8(0x3A0 + i),0xF0) ~= 0 then -- If box exists
					local off1 = bit.band(mainmemory.read_u8(0x3A0 + i),0xF) * 4
					local off2 = bit.bor(bit.band(mainmemory.read_u8(0x420 + i),0x40),off1)
					
					
					local xoff = memory.read_s8(0x35691 + off2)
					local xrad = memory.read_u8(0x35692 + off2)
					local yoff = memory.read_s8(0x35693 + off2)
					local yrad = memory.read_u8(0x35694 + off2)
					
					gui.drawBox(x+xoff,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFFFF0000,0x30FF0000)
					
					if HPDISPLAY == true then
						gui.text(x,y,"HP: " .. hp)
					end
				end
			end
		end
	end	
end

while true do
	camera()
	player()
	enemies()
	emu.frameadvance()
end