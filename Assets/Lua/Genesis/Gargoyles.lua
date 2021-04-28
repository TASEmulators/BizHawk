-- Gargoyles, Genesis (BizHawk)
-- feos, 2015-2017

--== Shortcuts ==--
local rb    = memory.read_u8
local rw    = memory.read_u16_be
local rws   = memory.read_s16_be
local r24   = memory.read_u24_be
local rl    = memory.read_u32_be
local box   = gui.drawBox
local text  = gui.text
local ptext = gui.pixelText
local line  = gui.drawLine
local AND   = bit.band
local SHIFT = bit.rshift

--== RAM addresses ==--
local levnum      = 0xff00ba
local LevelFlr    = 0xff00c0
local LevelCon    = 0xff00c4
local mapline_tab = 0xff0244
local GlobalBase  = 0xff1c76
local GolBase     = 0xff2c76
local MapA_Buff   = 0xff4af0

--== Camera Hack ==--
local camhack = false
local div     = 1      -- scale
local size    = 16/div -- block size

--== Block cache ==--
local col   = 0           -- block color
local opout = 0x33000000  -- outer opacity
local opin  = 0x66000000  -- inner opacity
local op    = 0xff000000
local cache = {}

--== Other stuff ==--
local MsgCutoff   = 20
local MsgTime     = 1
local MsgStep     = 14
local MsgOffs     = 2
local MsgTable    = {}
local XposLast    = 0
local YposLast    = 0
local room        = 0
local workinglast = 0
local wSize       = client.getwindowsize()
local lagcount    = emu.lagcount()
gui.defaultTextBackground(0xff000000)

--== Object types ==--
local types = {
	"Goliath","Orb","Health1PLR","Health2PLR","Health1NME","Health2NME","Numeral",
	"BigExplode","SmallExplode","GlassDebris","MetalDebris","WoodDebris",
	"WallDebris","SignPiece","SteamVent","BreakWall","SkyLight","BreakLight",
	"ThrowCrate","BreakEdgeLeft","BreakEdgeRight","Spark","Spark2","Sparks",
	"Sparks2","Fireball","HomingProj1","HorzProj1","VertProj1","DirProj1",
	"DirProj2","DropMine","Scratch","Icon","RaptorBot","SniperBot","SpiderBot",
	"WaspBot","Xanatos","PlasmaBot","RabidHH","MorningStar","Archer","Arrow",
	"Valkyrie","Axe","WeaponExp","Couldron","SpittingCouldron","FireballHead",
	"FireballTrail","BigFireballHead","BigFireballTrail","Oil","OilGenerator",
	"Claw","Stump","StumpBubble","StumpFire","ClawStump","StumpFireGen","Vent",
	"VentSparks","Chain","FlameLick","Floor","MutVikBody","MutVikHead",
	"MutVikHammer","EyeOfOdin","EyeOfOdinTrail","L1BreakWall","Catapult",
	"L1BreakFloor","Gate","GateCrusher","Weight","WeightCrusher","WallFire",
	"Balista","BalistaLog","PasteWall","FlameBoulder","CastlePiece",
	"MutantSpiderBot","MutSpiLegs","MutSpiHead","MutSpiHeadFlame","MutSpiProj",
	"MutSpiElecV","MutSpiElecH","PlasmaBall","PlasmaBallTail","PlasmaDeadHead",
	"VertFlame","WallFlame","FloorFlame","OPPlatform","OPLink","OPOrb",
	"Furnace","RobotGenerator","RockGenerator","BigRock","MediumRock",
	"SmallRock","BigCouldronGen","BigCouldron","Trough","TroughGen","Energizer",
	"Demona","TrajectoryProj","WallPaste","EdgePaste","Tentacle","Infuser",
	"BigGuns","BigGunsProj","HighSignPole","HighSign","LowLight","L5Skylight",
	"L5Wall","ElecGenerator","Electricity","WaspGenerator","TunnelEdge",
	"ForegroundPost","Sorcerer","LightningTop","LightningBot","MDemonaWallFire",
	"MDemonaFloorFire","EyeRooftopUp","EyeRooftopDn","EyeRaptor"
}

local function RoomTime()
	local start11 = 894--767
	local start12 = 2294
	local start13 = 5468 -- 4254 -- 4101
	local startl4 = 5506
	local startl5 = 7117
	local startl6 = 8412
	local startl7 = 17117
	local timer   = emu.framecount()
	if     timer < start11 then room = timer
	elseif timer < start12 then room = timer - start11
	elseif timer < start13 then room = timer - start12
	elseif timer < startl4 then room = timer - start13
	elseif timer < startl5 then room = timer - startl4
	elseif timer < startl6 then room = timer - startl5
	elseif timer < startl7 then room = timer - startl6
	end
	text(2, 2, string.format("cx:%5d\ncy:%5d\nroom:%d", camx, camy, room), "white", "bottomright")
end

local function HUD()
	--if working > 0 then return end
	local rndcol = "white"
	if rndlast ~= rnd1 then rndcol = "red" end
	text(0, 2, string.format("RNG:%08X %04X", rnd1, rnd2), rndcol, "bottomleft")
	text(2, 0, string.format(
		"x: %4d\ny: %4d\ndx: %3d\ndy: %3d\nhp: %3d\nrun:%3d\ninv:%3d",
		Xpos, Ypos, Xspd, Yspd, health, run, inv),
	"white", "topright")
end

local function CamhackHUD()
	if working == 0 then
		-- screen edge
		box((backx-camx-  1)/div,
			(backy-camy-  1)/div,
			(backx-camx+320)/div,
			(backy-camy+224)/div,
			0xff0000ff, 0)
		-- map edge
		box(       0-camx/div+size,
			       0-camy/div+size,
			mapw/div-camx/div,
			maph/div-camy/div,
			0xff0000ff, 0)
	end
	if camhack or div > 1 then
		text(0, 0, string.format("div:%d", div), "white", "topleft")
	end
end

local function PosToIndex(x, y)
	return math.floor(x/16)+math.floor(y/16)*xblocks
end

local function IndexToPos(i)
	return { x=(i%xblocks)*16, y=math.floor(i/xblocks)*16 }
end

local function InBounds(x, minimum, maximum)
	if x >= minimum and x <= maximum
	then return true
	else return false
	end
end

local function GetBlock(x, y)
	if working > 0 then return nil end
	local final = { contour={}, block=0 }
	if  x > 0 and x < mapw
	and y > 0 and y < maph then
		local pixels = 0
		local x1  = x/div-camx/div
		local x2  = x1+size-1
		local y1  = y/div-camy/div
		local y2  = y1+size-1
		local d4  = rw(mapline_tab+SHIFT(y, 4)*2)
		local a1  = r24(LevelFlr+1)
		local d1  = SHIFT(rw(MapA_Buff+d4+SHIFT(x, 4)*2), 1)
		final.block = rb(a1+d1+2)
		d1 = rw(a1+d1)
		a1 = r24(LevelCon+1)+d1
		if rb(a1) > 0 or rb(a1+8) > 0 then
			for pixel=0, 15 do
				final.contour[pixel] = rb(a1+pixel)
			end
		else
			final.contour = nil
		end
	else
		return nil
	end
	return final
end

local DrawBlock = {
	[0x80] = function(x1, y1, x2, y2)     -- WALL
		col = 0x00ffffff                  -- white
		line(x1, y1, x1, y2, col+op)      -- left
		line(x2, y1, x2, y2, col+op)      -- right
	end,
	[0x81] = function(x1, y1, x2, y2)     -- CEILING
		col = 0x00ffffff                  -- white
		line(x1, y2, x2, y2, col+op)      -- bottom
	end,
	[0x82] = function(x1, y1, x2, y2)     -- CLIMB_U
		col = 0x0000ffff                  -- cyan
		line(x1, y2, x2, y2, col+op)      -- bottom
	end,
	[0x83] = function(x1, y1, x2, y2)     -- CLIMB_R
		col = 0x0000ffff                  -- cyan
		line(x1, y1, x1, y2, col+op)      -- left
	end,
	[0x84] = function(x1, y1, x2, y2)     -- CLIMB_L
		col = 0x0000ffff                  -- cyan
		line(x2, y1, x2, y2, col+op)      -- right
	end,
	[0x85] = function(x1, y1, x2, y2)     -- CLIMB_LR
		col = 0x0000ffff                  -- cyan
		line(x1, y1, x1, y2, col+op)      -- left
		line(x2, y1, x2, y2, col+op)      -- right
	end,
	[0x86] = function(x1, y1, x2, y2)     -- CLIMB_R_STAND_R
		col = 0x00ffffff                  -- white
		line(x1, y1, x2, y1, col+op)      -- top
		col = 0x0000ffff                  -- cyan
		line(x1, y1, x1, y2, col+op)      -- left
	end,
	[0x87] = function(x1, y1, x2, y2)     -- CLIMB_L_STAND_L
		col = 0x00ffffff                  -- white
		line(x1, y1, x2, y1, col+op)      -- top
		col = 0x0000ffff                  -- cyan
		line(x2, y1, x2, y2, col+op)      -- right
	end,
	[0x88] = function(x1, y1, x2, y2)     -- CLIMB_LR_STAND_LR
		col = 0x00ffffff                  -- white
		line(x1, y1, x2, y1, col+op)      -- top
		col = 0x00ff00ff                  -- cyan
		line(x1, y1, x1, y2, col+op)      -- left
		col = 0x0000ffff                  -- cyan
		line(x2, y1, x2, y2, col+op)      -- right
	end,
	[0x70] = function(x1, y1, x2, y2)     -- GRAB_SWING
		col = 0x0000ff00                  -- green
		box(x1, y1, x2, y2, col, col+opout)
	end,
	[0x7f] = function(x1, y1, x2, y2)     -- EXIT
		col = 0x00ffff00                  -- yellow
	end,
	[0xd0] = function(x1, y1, x2, y2)     -- SPIKES
		col = 0x00ff0000                  -- red
		box(x1, y1, x2, y2, col, col+opout)
	end,
	[0xd1] = function(x1, y1, x2, y2)     -- SPIKES
		col = 0x00ff0000                  -- red
		box(x1, y1, x2, y2, col, col+opout)
	end
}

local function DrawBlockDefault(x1, y1, x2, y2) -- LEVEL_SPECIFIC
	col = 0x00ff8800                      -- orange
	box(x1, y1, x2, y2, col+opin, col+opout)
end

local function DrawBG(unit, x, y)
	local val= 0
	local x1 = x/div-camx/div-(camx%16)/div
	local x2 = x1+size-1
	local y1 = y/div-camy/div-(camy%16)/div
	local y2 = y1+size-1
	if unit.contour ~= nil then
		box(x1, y1, x2, y2, 0x5500ff00, 0x5500ff00)
		for pixel=0, 15 do
			val = unit.contour[pixel]
			--[ [--
			if val > 0 then
				gui.drawPixel(
					x1+pixel/div,
					y1+val/div-1/div,
					0xffffff00)
			end
			--]]--
		end
	end
	if unit.block > 0 then
		local Fn = DrawBlock[unit.block] or DrawBlockDefault
		Fn(x1, y1, x2, y2)
		box(x1, y1, x2, y2, col+opin, col+opout)
	end
end

local function Background()
	if working > 0 then
		cache = {}
		return
	end
	if camhack then
		camx = Xpos-320/2*div
		camy = Ypos-224/2*div
		box(0, 0, 320, 240, 0, 0x66000000)
	end
	local border = 0
	local offset = 32
	local basex  = camx+border
	local basey  = camy+border
	local basei  = PosToIndex(basex-offset, basey-offset)
	local boundx = 320*div-border
	local boundy = 224*div-border
	local xblockstockeck = ((camx+boundx+offset)-(basex-offset))/size/div
	local yblockstockeck = ((camy+boundy+offset)-(basey-offset))/size/div
	for yblock = 0, yblockstockeck do
		for xblock = 0, xblockstockeck do
			local i = yblock*xblocks+xblock+basei
			local x = basex+xblock*size*div
			local y = basey+yblock*size*div
			if InBounds(x, basex-offset, camx+boundx+offset) then
				local unit = cache[i]
				if unit == nil or workinglast > 0 then
					if  InBounds(x, basex, camx+boundx)
					and InBounds(y, basey, camy+boundy)
					then cache[i] = GetBlock(x, y)
					end
				else
					if  not InBounds(x, basex, camx+boundx)
					and not InBounds(y, basey, camy+boundy)
					then cache[i] = nil
					end
				end
				if unit ~= nil then
					DrawBG(unit, x, y)
				end
			elseif cache[i] ~= nil
			then cache[i] = nil
			end
		end
	end
	CamhackHUD()
end

local function Clamp(v, vmin, vmax)
	if v < vmin then v = vmin end
	if v > vmax then v = vmax end
	return v
end

local function Objects()
	if working > 0 then return end
	for i=0, 63 do
		local base = GlobalBase+i*128
		local flag2 = AND(rb(base+0x49), 0x10) -- active
		if flag2 == 0x10 then
			local xpos  = rw (base+0x00)
			local ypos  = rw (base+0x02)
			local state = rw (base+0x0c)
			local dmg   = rb (base+0x10)
			local id    = rw (base+0x40)
			local hp    = rw (base+0x50)
			local cRAM  = r24(base+0x75) -- pointer to 4 collision boxes per object
			local xscr  = (xpos-camx)/div
			local yscr  = (ypos-camy)/div
			local num   = id/6
			local name  = types[num]
			local col   = 0              -- collision color
			for boxx=0, 4 do
				local x0 =  rw (cRAM+boxx*8)
				local x1 = (rws(cRAM+boxx*8+0)-camx)/div
				local y1 = (rws(cRAM+boxx*8+2)-camy)/div
				local x2 = (rws(cRAM+boxx*8+4)-camx)/div
				local y2 = (rws(cRAM+boxx*8+6)-camy)/div
				if boxx == 0 then
					col = 0xff00ff00      -- body
					-- archer hp doesn't matter
					if id == 282 or id == 258 then hp = 1 end
					if hp > 0 and id > 0 and x0 ~= 0x8888 then
						local xx = Clamp(xscr, 0, 318-string.len(name)*4)
						local yy = Clamp(yscr, 0, 214)
						ptext(xx, yy+2, string.format("%d", hp), col)
					end
				elseif boxx == 1 then
					col = 0xffffff00      -- floor
				elseif boxx == 2 then
					if dmg > 0
					then col = 0xffff0000 -- projectile
					else col = 0xff8800ff -- item
					end
					if dmg > 0 then
						text(x1*wSize+2, y2*wSize+1,
							string.format("%d", dmg), col, 0x88000000)
					end
				else
					col = 0xffffffff      -- other
				end
				if x1 ~= 0x8888
				and x1 <= 320 and x2 >= 0
				and y1 <= 224 and y2 >= 0 then
					box(x1, y1, x2, y2, col, 0)
				end
			end
		end
	end
end

local function PostRndRoll()
	for i = 1,#MsgTable do
		if MsgTable[i] and MsgTable[i].index == i then
			local base = MsgTable[i].base
			local xpos  = rw(base+0x00)
			local ypos  = rw(base+0x02)
			local id    = rw(base+0x40)
			local x     = (xpos-camx)/div
			local y     = (ypos-camy)/div
			local num   = id/6
			local ymsg  = 0
			local yoffs = math.floor((i-1)/MsgCutoff)*14
			local name  = types[num]
			local color = 0xffffff00
			
			if base == GolBase then 
				name = "Goliath"
			elseif not name then
				name = string.format("%X", base)
				color = 0xff00ffff
			end
			if y < 224/2 then
				yoffs = -yoffs
				ymsg  = 210
			end
			
			x = Clamp(x,  2, 320-string.len(name)*4)
			y = Clamp(y, 20, 214)
			
			line ((i-1)%MsgCutoff*MsgStep+3    +MsgOffs, ymsg+yoffs+4, x, y, color-0x88000000)
			ptext((i-1)%MsgCutoff*MsgStep*wSize+MsgOffs, ymsg+yoffs, i, color)
				
			MsgTable[i].timer = MsgTable[i].timer-1
			if MsgTable[i].timer <= 0 then
				MsgTable[i] = nil
			end
		end
	end
end

local function PlayerBoxes()
	if working > 0 then return end
	local xx = (Xpos-camx)/div
	local yy = (Ypos-camy)/div
	local col = 0xff00ffff
	local swcol = col        -- usual detection
	if Yspd > 0 then         -- gimme swings to grab!
		swcol = 0xff00ff00
	elseif Yspd == 0 then    -- can tell that too
		swcol = 0xffffffff
	end
	if facing == 2
	then box(xx-0xf /div-2, yy-0x2c/div-1, xx-0xf/div+0, yy-0x2c/div+1, swcol, 0) -- lefttop
	else box(xx+0xf /div  , yy-0x2c/div-1, xx+0xf/div+2, yy-0x2c/div+1, swcol, 0) -- rightttop
	end
	box(xx         -1, yy-0x2c/div-1, xx         +1, yy-0x2c/div+1, col, 0) -- top
	box(xx-0xf /div-2, yy-0x1f/div-1, xx-0xf /div+0, yy-0x1f/div+1, col, 0) -- left
	box(xx+0x10/div-1, yy-0x1f/div-1, xx+0x10/div+1, yy-0x1f/div+1, col, 0) -- right
--	box(xx         -1, yy-0x1f/div-1, xx         +1, yy-0x1f/div+1, col, 0) -- center
	box(xx         -1, yy-0x0f/div-1, xx         +1, yy-0x0f/div+1, col, 0) -- bottom
	box(xx         -1, yy         -1, xx         +1, yy   +1,0xffffff00, 0) -- feet
--	box(xx         -1, yy+0x10/div-1, xx         +1, yy+0x10/div+1, col, 0) -- ground
end

local function Input()
	local i, u, d, l, r, a, b, c, s
	if movie.isloaded()
	then i = movie.getinput(emu.framecount()-1)
	else i = joypad.getimmediate()
	end
	if i["P1 Up"   ] then u = "U" else u = " " end
	if i["P1 Down" ] then d = "D" else d = " " end
	if i["P1 Left" ] then l = "L" else l = " " end
	if i["P1 Right"] then r = "R" else r = " " end
	if i["P1 A"    ] then a = "A" else a = " " end
	if i["P1 B"    ] then b = "B" else b = " " end
	if i["P1 C"    ] then c = "C" else c = " " end
	if i["P1 Start"] then s = "S" else s = " " end
	text(1, 10, u..d..l..r..a..b..c..s, "yellow")
end

event.onframeend(function()
	emu.setislagged(rb(0xfff6d4) == 0)
	if rb(0xfff6d4) == 0 then
		lagcount = lagcount+1
		framecol = "red"
	else
		framecol = "white"
	end
	emu.setlagcount(lagcount)
	wSize  = client.getwindowsize()
	rndlast = rnd1
	workinglast = working
	XposLast = Xpos
	YposLast = Ypos
end)

event.onmemoryexecute(function()
	local a0 = AND(emu.getregister("M68K A0"), 0xffffff)
	if a0 ~= 0xff4044 then
		for i = 1, 200 do
			if MsgTable[i] == nil then
				MsgTable[i] = { index = i, timer = MsgTime, base  = a0 }
				break
			end
		end
	end
end, 0x257A, "RNGseed")

local function main()
	rnd1     = rl (0xff001c)
	rnd2     = rw (0xff0020)
	working  = rb (0xff0073)
	xblocks  = rw (0xff00d4)
	mapw     = rw (0xff00d4)*8
	maph     = rw (0xff00d6)*8
	Xpos     = rws(0xff0106)
	Ypos     = rws(0xff0108)
	camx     = rws(0xff010c)+16
	camy     = rws(0xff010e)+16
	run      = rb (0xff1699)
	inv      = rw (0xff16d2)
	health   = rws(0xff2cc6)
	backx    = camx
	backy    = camy
	Xspd     = Xpos-XposLast
	Yspd     = Ypos-YposLast
	facing   = AND(rb(GolBase+0x48), 2) -- object flag 1
	if working > 0 then MsgTable = {} end
	Background()
	PlayerBoxes()
	Objects()
	PostRndRoll()
	HUD()
	RoomTime()
end

while true do
	main()
	emu.frameadvance()
end