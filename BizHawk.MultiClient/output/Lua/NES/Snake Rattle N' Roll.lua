-- Snake Rattle N' Roll 'Simple HUD Script'
-- Retrieved from http://tasvideos.org/GameResources/NES/SnakeRattleNRoll.html

dim1speed = 0
dim2speed = 0
xspeed    = 0
xsubspeed = 0
yspeed    = 0
ysubspeed = 0
xm = 0
ym = 0

function stuff()
	xspeed    = mainmemory.read_u8(0x417)
	xsubspeed = mainmemory.read_u8(0x419)
	if (xspeed == 255) then xspeed = -1 end
	
	yspeed    = mainmemory.read_u8(0x41b)
	ysubspeed = mainmemory.read_u8(0x41d)
	if (yspeed == 255) then yspeed = -1 end

	gui.text(1 * xm,19 * ym,"L:"..mainmemory.read_u8(0x67) % 16) -- left axis
	gui.text(1 * xm,29 * ym,"R:"..mainmemory.read_u8(0x69) % 16) -- right axis
	gui.text(1 * xm,39 * ym,"H:"..mainmemory.read_u8(0x6b) % 16)	-- height axis
	gui.text(30 * xm,19 * ym, "LVel:"..(xspeed * 256) + xsubspeed + (yspeed * 256) + ysubspeed)
	gui.text(30 * xm,29 * ym,"RVel:"..(xspeed * 256) + xsubspeed - (yspeed * 256) - ysubspeed)
end

local function scaler()
	xm = client.screenwidth() / 256
	ym = client.screenheight() / 224
end
while true do
	scaler()
	stuff()
	emu.frameadvance()
end	