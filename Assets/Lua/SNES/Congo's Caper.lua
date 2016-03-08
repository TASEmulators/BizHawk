-- Congo Caper's Collision Box Viewer
-- Author: Pasky13

camx = 0
camy = 0

memory.usememorydomain("CARTROM")

function findbit(p) 
	return 2 ^ (p - 1)
end

function hasbit(x, p) 
	return x % (p + p) >= p 
end

local function RetrieveBox(obase, switch)
	if switch == 1 then
		pointer = mainmemory.read_u8(obase + 0x19)
	elseif switch == 2 then
		pointer = mainmemory.read_u8(obase + 0x18)
	elseif switch == 3 then
		pointer = mainmemory.read_u8(obase + 0x1A)
	end
	
	local pointer = bit.lshift(pointer,3)
	local base = 0x66962
	local box = {0, 0, 0, 0 }
	box[1] = memory.read_s16_le(base + pointer) -- x1
	box[2] = memory.read_s16_le(base + pointer + 4) -- y1
	box[3] = memory.read_s16_le(base + pointer + 2) -- x2
	box[4] = memory.read_s16_le(base + pointer + 6) -- y2
	return box
end

local function camera()
	camx = mainmemory.read_u16_le(0x7E0427)
	camy = mainmemory.read_u16_le(0x7E0429)
end

local function player()
	local x = mainmemory.read_u16_le(0x23) - camx
	local y = mainmemory.read_u16_le(0x26) - camy
	local face = mainmemory.read_u8(0x2D)
	local box
	local box = RetrieveBox(0x20,1)
	if hasbit(face,findbit(7)) then
		gui.drawBox(x - box[1],y + box[2],x - box[3],y + box[4],0xFF0000FF,0x350000FF) -- Hurt box
		box = RetrieveBox(0x20,2)
		gui.drawBox(x - box[1],y + box[2],x - box[3],y + box[4],0xFFFFFFFF,0x35FFFFFF) -- Hit box
	else
		gui.drawBox(x + box[1],y + box[2],x + box[3],y + box[4],0xFF0000FF,0x350000FF) -- Hurt box
		box = RetrieveBox(0x20,2)
		gui.drawBox(x + box[1],y + box[2],x + box[3],y + box[4],0xFFFFFFFF,0x35FFFFFF) -- Hit box
	end
end

local function enemies()
	local ebase = 0x1100
	for i = 0,64,1 do
		base = ebase + (i * 0x40)
		if mainmemory.read_u8(base) > 0 then
			local x = mainmemory.read_u16_le(base + 3) - camx
			local y = mainmemory.read_u16_le(base + 6) - camy
					
			
			local face = mainmemory.read_u8(base + 0xD)
			local box = RetrieveBox(base,2)
			
			if hasbit(face,findbit(7)) then
				gui.drawBox(x - box[1],y + box[2],x - box[3],y + box[4],0xFFFF0000,0x35FF0000) -- Hit box
				box = RetrieveBox(base,1)
				gui.drawBox(x - box[1],y + box[2],x - box[3],y + box[4],0xFF0000FF,0x350000FF) -- Hurt box
				box = RetrieveBox(base,3)
				gui.drawBox(x - box[1],y + box[2],x - box[3],y + box[4],0xFF00FF00,0x3500FF00) -- Can be jumped on box
			else
				gui.drawBox(x + box[1],y + box[2],x + box[3],y + box[4],0xFFFF0000,0x35FF0000) -- Hit  box
				box = RetrieveBox(base,1)
				gui.drawBox(x + box[1],y + box[2],x + box[3],y + box[4],0xFF0000FF,0x350000FF) -- Hurt box
				box = RetrieveBox(base,3)
				gui.drawBox(x + box[1],y + box[2],x + box[3],y + box[4],0xFF00FF00,0x3500FF00)  -- Can be jumped on box
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
	camera()
	player()
	enemies()
	emu.frameadvance()
end