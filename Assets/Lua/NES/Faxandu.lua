------------------------------
--Faxandu collision box viewer
--Author: Pasky
------------------------------

local HP = true -- toggle to false to turn off hitpoint display on enemies

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function hex(val)
	val = string.format("%X",val)
	return val
end

memory.usememorydomain("System Bus")

local function player()
	local x = memory.read_u8(0x9E)
	local y = memory.read_u8(0xA1) + 0x1F
	local hp = memory.read_u8(0x431)
	-- vuln box
	gui.drawBox(x,y,x+0xB,y+0x1B,0xFF0000FF,0x300000FF)
	
	--attack box
	local xrad = memory.read_u8(0xD2)
	local yrad = memory.read_u8(0xD3)
	local atk = memory.read_u8(0xA4)
	local x2 = memory.read_u8(0xCE)
	local y2 = memory.read_u8(0xD0) + 0x1F
	
	if hasbit(atk,findbit(8)) then
		gui.drawBox(x2,y2,x2+xrad,y2+yrad)
	end
	
	--Magic
	local mag = memory.read_u8(0x02B3)
	if mag ~= 0xFF then
		local addr = 0x8B73 + mag
		local magx = memory.read_u8(0x2B6)
		local magy = memory.read_u8(0x2B8) + 0x1F
		xrad = memory.read_u8(addr)
		yrad = memory.read_u8(addr + 5)
		gui.drawBox(magx,magy,magx+xrad,magy+yrad)
	end
end

local function enemies()
	local base = 0xBA
	for i = 0,7,1 do
		local etype = memory.read_u8(0x2CC + i)
		if etype ~= 0xFF then
			local x = memory.read_u8(base + i)
			local y = memory.read_u8(base + 8 + i) + 0x1F
			local hp = memory.read_u8(0x344 + i)
			local atk = memory.read_u8(0x2E4 + i)
			local atk2 = false
			local facing = memory.read_u8(0x2DC + i)
			local addr
			
			if etype == 0x1F or etype == 0x21 then  -- Check if dwarf or ???
				if atk == 0x02 then
					addr = 0x8A71
					atk2 = true
				else
					addr = 0xB200 + (etype * 4) + 0x73
				end
			elseif etype == 0x20 then
				if atk == 0x04 then
					addr = 0x8A75
					atk2 = true
				else
					addr = 0xB200 + (etype * 4) + 0x73
				end
			else
				addr = 0xB200 + (etype * 4) + 0x73
			end

			local xoff = memory.read_s8(addr)
			local yoff = memory.read_s8(addr + 1)
			local xrad = memory.read_s8(addr + 2)
			local yrad = memory.read_s8(addr + 3)
			
			 if not hasbit(facing,findbit(1)) and atk2 == true then
				 xoff = xoff * -1
				 xrad = xrad * -1
			 end
			
			--Attack box
			gui.drawBox(x,y+yoff,x+xoff+xrad,y+yoff+yrad,0xFFFF0000,0x70FF0000)
			if HP == true then
				gui.text(x,y-10,"HP: " .. hp)
			end
			--Vuln box
			if memory.read_u8(etype + 0xB544) == 0 then
				addr = 0xB407 + (etype * 2)
				xrad = memory.read_u8(addr)
				yrad = memory.read_u8(addr + 1)
				gui.drawBox(x,y,x+xrad,y+yrad,0xFFFFFF00,0x30FFFF00)
			end
		end
	end
end

while true do
	player()
	enemies()
	emu.frameadvance()
end