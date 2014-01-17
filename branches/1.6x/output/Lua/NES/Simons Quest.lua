--Author Pasky13

-- Toggles
local show_hud = true
local show_elife = true -- Enemy life

-- Player
local px = 0x348
local py = 0x324
local pl = 0x80
local ph = 0x48
local pe = 0x46
local pt = 0x85

-- Enemy
local el = 0x4C2
local ex = 0x348
local ey = 0x324
local oob = 0x3C6
local point = 0x3FC

local xm
local ym

local function hex(val)
	val = string.format("%X",val)
	return val
end

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

memory.usememorydomain("PRG ROM")

local function formatstring(adr)
	local str
	if string.len(string.format("%X",mainmemory.read_u8(adr+1))) == 1 then
		str = "0" .. string.format("%X", mainmemory.read_u8(adr+1))
	else
		str = string.format("%X", mainmemory.read_u8(adr+1))
	end
	if adr == pt then
		str = str .. ":"
	end
	if string.len(string.format("%X",mainmemory.read_u8(adr))) == 1 then
		str = str .. "0" .. string.format("%X",mainmemory.read_u8(adr))
	else
		str = str .. string.format("%X", mainmemory.read_u8(adr))
	end
	return str
end

local function buildbox(i)
	local offset1 = mainmemory.read_u8(0x3B4 + i) * 2
	local pointer1 = memory.read_u8(0x4AC0 + offset1) + (memory.read_u8(0x4AC1 + offset1) * 0x100)
	local offset2 = mainmemory.read_u8(0x3FC + i)
		
	if offset2 > 0 then
		offset2 = offset2 - 1
	end
	
	local offset3 = memory.read_u8(pointer1 + offset2 - 0x4000)
	local offset3 = ((offset3 * 2) + offset3) % 0x100
	local box = { memory.read_s8(0x4B7E + offset3), memory.read_u8(0x4B7F + offset3),memory.read_u8(0x4B80 + offset3) } -- yoff/yrad/xrad
	
	return box
end

local function player()
	local x = mainmemory.read_u8(px)
	local y = mainmemory.read_u8(py)

	gui.drawBox(x - 6,y + 3 - 0x0A,x + 6,y + 3 + 0x0A,0xFF0000FF,0x400000FF)
	
	-- Whip
	if mainmemory.read_u8(0x445) == 3 then
		local wxoff = 0x16
		local wyoff = -4
		local wxrad
		local wyrad = 4
		local woff = mainmemory.read_u8(0x434)
		if mainmemory.read_u8(0x420) == 0 then
			wxoff = wxoff * -1
		end
		wxrad = memory.read_u8(0x4BDD + woff)
		gui.drawBox(x+wxoff-wxrad,y+wyoff-wyrad,x+wxoff+wxrad,y+wyoff+wyrad,0xFFFFFFFF,0x40FFFFFF)
	end
	
end

local function objects()
	local x 
	local y 
	local l
	local box
	local active
	local fill 
	local outl 
	local isoob
	local etype
	for i = 2,19,1 do
		active = mainmemory.read_u8(0x3D8 + i)
		isoob = mainmemory.read_u8(oob + i)
		etype = mainmemory.read_u8(0x3B4 + i)
		
		if etype > 0 and etype ~= 0x43 and etype ~= 0x1E and etype ~= 0x2A then
			box = buildbox(i)
			x = mainmemory.read_u8(ex + i)
			y = mainmemory.read_u8(ey + i)
			l = mainmemory.read_u8(el + i)
			if hasbit(active,findbit(1)) and not hasbit(active,findbit(8)) then  -- Enemy
				
				if bit.rshift(bit.band(isoob,0xF0),4) ~= 8 and bit.rshift(bit.band(isoob,0xF0),4) ~= 4 then  -- If not offscreen
					if show_elife == true then
						gui.text((x-8) * xm,(y-28) * ym,"HP: " .. l)
					end
				end
				fill = 0x40FF0000
				outl = 0xFFFF0000
			elseif hasbit(active,findbit(8)) and hasbit(active,findbit(1)) then -- Hidden enemy, no active box
				outl = 0x40FF0000
				fill = 0x00FF0000
				
				if bit.rshift(bit.band(isoob,0xF0),4) ~= 8 and bit.rshift(bit.band(isoob,0xF0),4) ~= 4 then  -- If not offscreen
					if show_elife == true then
						gui.text((x-8) * xm,(y-28) * ym,"HP: " .. l)
					end
				end
			elseif hasbit(active,findbit(8)) and hasbit(active,findbit(2)) then  -- Simon's projectiles
				outl = 0xFF00FFFF
				fill = 0x4000FFFF
			elseif not hasbit(active,findbit(8)) and hasbit(active,(2)) then -- Enemy projectile
				outl = 0xFFFFFF00
				fill = 0x40FFFF00
			elseif hasbit(active,findbit(8)) and not hasbit(active,findbit(2)) then  -- Inactive box
				outl = 0
				fill = 0
			elseif hasbit(active,findbit(7)) then -- NPC
				outl = 0xFFFF00FF
				fill = 0x40FF00FF
			elseif hasbit(active,findbit(3)) then -- Item pickups
				outl = 0xFFFFA500
				fill = 0x40FFA500
			end
			if bit.rshift(bit.band(isoob,0xF0),4) ~= 8 and bit.rshift(bit.band(isoob,0xF0),4) ~= 4 then  -- If not offscreen
				gui.drawBox(x - box[3],y+box[1]+box[2],x+box[3],y+box[1]-box[2],outl,fill)
			end

		end
		
	end
	
end

local function HUD()
	local l = mainmemory.read_u8(pl)
	local h = 0	
	local e = 0
	local t = 0
	
	-- Hearts
	h = formatstring(ph)
	-- Experience
	e = formatstring(pe)
	-- Time
	t = formatstring(pt)
	
	gui.text(14 * xm, 9 * ym, l)
	gui.text((256 - 40) * xm, 9 * ym,"H: " .. h)
	gui.text((256 - 40) * xm, 17 * ym,"E: " .. e)
	gui.text((256 - 85) * xm, 9 * ym, "T: " .. t)
end

local function scaler()
	xm = client.screenwidth() / 256
	ym = client.screenheight() / 224
end

while true do 
	scaler()
	if show_hud == true then
		HUD()
	end
	player()
	objects()
	emu.frameadvance()
end