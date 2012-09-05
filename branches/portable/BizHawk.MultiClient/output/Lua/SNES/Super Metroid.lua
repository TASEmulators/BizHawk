--Author Pasky13

--Player
local px = 0x000AF6
local py = 0x000AFA
local plife = 0x0009C2
--Camera
local camx = 0x000911
local camy = 0x000915

local function Samus()
	local x = mainmemory.read_u16_le(px) - mainmemory.read_u16_le(camx)
	local y = mainmemory.read_u16_le(py) - mainmemory.read_u16_le(camy)
	local xrad = mainmemory.read_u8(0x7E0AFE)
	local yrad = mainmemory.read_u8(0x7E0B00)
	
	gui.drawBox(x + (xrad * -1), y + (yrad * -1), x+xrad,y+yrad,0xFF0000FF,0x350000FF)
	
end

local function EnemyBoxes()
	local x = 0
	local y = 0
	local xrad = 0
	local yrad = 0
	local oend = 20
	local base = 0xF7A
	
	for i = 0, oend, 1 do
		if i > 0 then
			base = 0xF7A + (i * 0x40)
		else
			base = 0xF7A
		end
		
		x = mainmemory.read_u16_le(base) - mainmemory.read_u16_le(camx)
		y = mainmemory.read_u16_le(base+ 4) - mainmemory.read_u16_le(camy)
		xrad = mainmemory.read_u8(0x0F82 + (i * 0x40))
		yrad = mainmemory.read_u8(0x0F84 + (i * 0x40))
		
		gui.drawBox(x + (xrad * -1),y + (yrad * -1),x+xrad,y+yrad,0xFFFF0000,0x35FF0000)
		
	end
end

local function Projectiles()
	local x
	local y
	local xrad
	local yrad
	local oend = 8
	local projxbase = 0xB64
	local projybase = 0xB78
	local projxrbase = 0xBB4
	local projyrbase = 0xBC8
	
	for i = 0, oend, 1 do
	
		
		x = mainmemory.read_u16_le(projxbase + (i*2)) - mainmemory.read_u16_le(camx)
		y = mainmemory.read_u16_le(projybase + (i*2)) - mainmemory.read_u16_le(camy)

		xrad = mainmemory.read_u8(projxrbase + (i * 2))
		yrad = mainmemory.read_u8(projyrbase + (i * 2))
		
		gui.drawBox(x + (xrad * -1), y + (yrad * -1), x+xrad,y+yrad,0xFFFFFFFF,0x35FFFFFF)
	end
	
end

while true do
	Samus()
	EnemyBoxes()
	Projectiles()
	emu.frameadvance()
end