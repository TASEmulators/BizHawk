-- Author Pasky13

local pbase = 0xC00
local ppbase = 0xCC0

local ebase = 0x19C0
local epbase = 0xEC0

local xm
local ym

memory.usememorydomain("CARTROM")

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

local function camera()
	cx = mainmemory.read_u16_le(0xD9)
	cy = mainmemory.read_u16_le(0xDB)
end


local function drawbox(p,x,y,base,player,fill,outl)
	local box = {0,0,0,0} --xoff/yoff/xrad/yrad
	
	box[1] = memory.read_s8(p)
	box[2] = memory.read_s8(p + 1)
	box[3] = memory.read_u8(p + 2)
	box[4] = memory.read_u8(p + 3)
	
	if player == false then
		if hasbit(mainmemory.read_u8(base+0x14),findbit(7)) then -- megaman facing right
			box[1] = box[1] * - 1
		end
	else
		if hasbit(mainmemory.read_u8(base+0x4A),findbit(3)) then -- enemy facing left
			box[1] = box[1] * - 1
		end
	end
	
	gui.drawBox(x+box[1]+box[3],y+box[2]+box[4],x+box[1]-box[3],y+box[2]-box[4],outl, fill)
	return box
end

local function proj_vuln(base,x,y,xoff,yoff,xrad,yrad)
	local offset = memory.read_u8(0x87D0 +(mainmemory.read_u8(base + 0x31) * 2))
	local property = memory.read_s8(0x87D0 + offset)
	if property <= 0 then
		gui.drawLine(x + xoff + xrad,y + yoff,x + xoff - xrad, y + yoff,0xFFFFFFFF)
		gui.drawLine(x + xoff,y + yoff + yrad, x + xoff,y + yoff - yrad,0xFFFFFFFF) 
	end
	--gui.text(x-40,y-20,hex(offset) .. "   " .. hex(base))
end

local function player()
	local x = mainmemory.read_u16_le(pbase + 5) - cx
	local y = mainmemory.read_u16_le(pbase + 8) - cy
	local pointer = mainmemory.read_u16_le(pbase + 0x24)
	local hp = mainmemory.read_s8(pbase + 0x2F)
	drawbox(pointer,x,y,pbase,true,0x400000FF,0xFF0000FF)
	gui.text((x-4) * xm,y * ym,"HP:" .. hp)
end



local function player_projectiles()
	local x
	local y
	local base
	local pointer
	local active
	local anim
	
	for i = 0,7,1 do
		base = ppbase + (i * 0x40)
		active = mainmemory.read_u8(base)
		anim = mainmemory.read_u8(base + 1)
		if active ~= 0 and anim > 1 then
			x = mainmemory.read_u16_le(base + 5) - cx
			y = mainmemory.read_u16_le(base + 8) - cy
			pointer = mainmemory.read_u16_le(base + 0x24)
			drawbox(pointer,x,y,base,false,0x40FFFFFF,0xFFFFFFFF)
		end
	end	
	
end

local function enemy_projectiles()
	local x
	local y
	local base
	local pointer
	local active
	local anim
	for i = 0,7,1 do
		base = epbase + (i * 0x40)
		active = mainmemory.read_u8(base)
		anim = mainmemory.read_u8(base + 1)
		if active ~= 0 and anim > 1 then
			x = mainmemory.read_u16_le(base + 5) - cx
			y = mainmemory.read_u16_le(base + 8) - cy
			pointer = mainmemory.read_u16_le(base + 0x24)
			drawbox(pointer,x,y,base,false,0x4000FF00,0xFF00FF00)
		end
	end	
	
end

local function objects()
	local x
	local y
	local hp 
	local base
	local pointer
	local active
	local anim
	local box
	for i = 0,15,1 do
		base = ebase + (i * 0x40)
		active = mainmemory.read_u8(base)
		anim = mainmemory.read_u8(base + 1)
		if active ~= 0 and anim > 1 then
			x = mainmemory.read_u16_le(base + 5) - cx
			y = mainmemory.read_u16_le(base + 8) - cy
			pointer = mainmemory.read_u16_le(base + 0x24)
			hp = mainmemory.read_s8(base + 0x2F)
			if hp > 0 then
				gui.text((x-8) * xm,(y-8) * ym,"HP: " .. hp)
			end
			box = drawbox(pointer,x,y,base,false,0x40FF0000,0xFFFF0000)
			proj_vuln(base,x,y,box[1],box[2],box[3],box[4])
		end
	end
end

-- local function unknown()  -- Hitstun animations?
	-- local x
	-- local y
	-- local base = 0x15C0
	
	-- for i = 0,8,1 do
		-- base = 0x15C0 + (i * 0x20)
		-- if mainmemory.read_u16_le(base) ~= 0 then
			-- x = mainmemory.read_u16_le(base + 5) - cx
			-- y = mainmemory.read_u16_le(base + 8) - cy
			-- gui.text(x,y,"UK:" .. i)
		-- end
	-- end
	
	-- for i = 0,15,1 do 
		-- base = 0x16C0 + (i * 0x30)
		-- if mainmemory.read_u16_le(base) ~= 0 then
			-- x = mainmemory.read_u16_le(base + 5) - cx
			-- y = mainmemory.read_u16_le(base + 8) - cy
			-- gui.text(x,y,"UK2:" .. i)
		-- end
	-- end
-- end

local function scaler()
	xm = client.screenwidth() / 256
	ym = client.screenheight() / 224
end

while true do
	scaler()
	camera()
	enemy_projectiles()
	player_projectiles()
	objects()
	player()
	emu.frameadvance()
end