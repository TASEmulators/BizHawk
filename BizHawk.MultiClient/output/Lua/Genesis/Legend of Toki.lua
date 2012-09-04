--Author Pasky13

-- Player
local pbase = 0xFF1CD4
local px = pbase + 0x19
local py = pbase + 0x1D

local camx = 0xFF1BC5
local camy = 0xFF1BC9

--Player projectiles

local projbase = 0xFF1D34

--Enemies
local ebase = 0xFF21B4

--Bosses
local bbase = 0xFF35F4

--Text scaler
local xs 
local ys


local function endian(address)
	local result = mainmemory.read_u8(address) + (mainmemory.read_u8(address-1) * 256)
	return result
end

local function Toki()
	local cx = endian(camx)
	local cy = endian(camy)
	local x = endian(px) - cx
	local y = endian(py) - cy
	local xoff = mainmemory.read_s8(pbase + 0x11)
	local yoff = mainmemory.read_s8(pbase + 0x15)
	local xrad = mainmemory.read_u8(pbase +0x13)
	local yrad = mainmemory.read_u8(pbase +0x17)
	local flip = mainmemory.read_u8(pbase +1)
	if flip == 1 then
		xoff = xoff * -1
	end
	gui.drawBox(x+xoff-xrad,y+yoff-yrad,x+xoff+xrad,y+yoff+yrad,0xFF0000FF,0x300000FF)
end

local function enemies()
	local cx = endian(camx)
	local cy = endian(camy)
	local x
	local y
	local xoff
	local xrad
	local yoff
	local yrad
	local oend = 44
	local base = ebase	
	local flip
	local hp
	for i = 0,oend,1 do
		if i > 0 then
			base = ebase + (i * 0x60)
		end
		flip = mainmemory.read_u8(base +1)
		if mainmemory.read_u8(base) > 0 then
			hp = mainmemory.read_u8(base + 0xD)
			x = endian(base + 0x19) - cx
			xrad = mainmemory.read_u8(base + 0x13)
			xoff = mainmemory.read_s8(base + 0x11)
			yrad = mainmemory.read_u8(base + 0x17)
			yoff = mainmemory.read_s8(base + 0x15)
			
			if flip == 1 then
				xoff = xoff * -1
			end
			y = endian(base + 0x1D) - cy
			if hp > 0 then
				gui.text((x-10) * xs,(y-10) * ys, "HP: " .. hp)
			end
			gui.drawBox(x+xoff-xrad,y+yoff-yrad,x+xoff+xrad,y+yoff+yrad,0xFFFF0000,0x35FF0000)
		end
	end
end

local function boss()
	local cx = endian(camx)
	local cy = endian(camy)
	local x = endian(bbase + 0x19) - cx
	local y = endian(bbase + 0x1D) - cy
	local xrad = mainmemory.read_u8(bbase + 0x11)
	local yrad = mainmemory.read_u8(bbase + 0x15)
	local hp = mainmemory.read_u8(bbase+ 0x0D)
	
	if hp > 0 then
		gui.text((x-10) * xs,(y-10) * ys,"HP: " .. mainmemory.read_u8(bbase + 0x0D))
	end
	gui.drawBox(x-xrad,y-yrad,x+xrad,y+yrad,0xFFFF0000,0x35FF0000)
end

local function projectiles()
	local cx = endian(camx)
	local cy = endian(camy)
	local x
	local y
	local xoff
	local xrad
	local yoff
	local yrad
	local oend = 11
	local base = projbase
	local flip
	
	for i = 0,oend,1 do
		if i > 0 then
			base = projbase + (i * 0x60)
		end
		flip = mainmemory.read_u8(base +1)
		
		if mainmemory.read_u8(base) > 0 then
			x = endian(base + 0x19) - cx
			y = endian(base + 0x1D) - cy
			xoff = mainmemory.read_s8(base + 0x11)
			yoff = mainmemory.read_s8(base + 0x15)
			xrad = mainmemory.read_u8(base + 0x13)
			yrad = mainmemory.read_u8(base + 0x17)
			
			if flip == 1 then
				xoff = xoff * -1
			end
			gui.drawBox(x+xoff-xrad,y+yoff-yrad,x+xoff+xrad,y+yoff+yrad,0xFFFFFFFF,0x40FFFFFF)
		end
	end

end

local function scaler()
	xs = client.screenwidth() / 320
	ys = client.screenheight() / 224
end


while true do
	scaler()
	Toki()
	enemies()
	projectiles()
	boss()
	emu.frameadvance()
end