-- Gargoyles, Genesis (BizHawk)
-- feos, 2015-2016

--== Shortcuts ==--
rb    = memory.read_u8
rw    = memory.read_u16_be
rws   = memory.read_s16_be
r24   = memory.read_u24_be
rl    = memory.read_u32_be
box   = gui.drawBox
text  = gui.pixelText
line  = gui.drawLine
AND   = bit.band
SHIFT = bit.rshift

--== RAM addresses ==--
levnum      = 0xff00ba
LevelFlr    = 0xff00c0
LevelCon    = 0xff00c4
mapline_tab = 0xff0244
GlobalBase  = 0xff1c76
GolBase     = 0xff2c76
MapA_Buff   = 0xff4af0

--== Camera Hack ==--
camhack = false
div     = 1      -- scale
size    = 16/div -- block size

--== Block cache ==--
col   = 0           -- block color
opout = 0x33000000  -- outer opacity
opin  = 0x66000000  -- inner opacity
op    = 0xff000000
cache = {}

--== Other stuff ==--
XposLast    = 0
YposLast    = 0
room        = 0
workinglast = 0
lagcount    = emu.lagcount()
gui.defaultPixelFont("fceux")

function main()
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
	facing   = AND(rb(GolBase+0x48),2) -- object flag 1
	
	Background()
	CamhackHUD()
	Objects()
	PlayerBoxes()
	HUD()
	RoomTime()
	Input()
end

function RoomTime()
	local start11 = 894--767
	local start12 = 2294
	local start13 = 4101
	local startl4 = 6000
	timer = emu.framecount()
	
	if     timer < start11 then room = timer
	elseif timer < start12 then room = timer - start11
	elseif timer < start13 then room = timer - start12
	elseif timer < startl4 then room = timer - start13
	end
	text(160,214,"room cnt: "..room, "white")
end

function HUD()
	text(1, 0,emu.framecount(),     framecol)
	text(1,20,emu.lagcount(),       "red")
	text(1,30,movie.rerecordcount(),"orange")
	
	if working>0 then return end
	if rndlast ~= rnd1 then rndcol = "red" else rndcol = "white" end
	
	text(  0,214,"rnd: ","yellow")
	text( 26,214,string.format("%08X %04X",rnd1,rnd2),rndcol)
	text(277,  0,string.format(
		"x: %4d\ny: %4d\ndx: %3d\ndy: %3d\nhp: %3d\nrun:%3d\ninv:%3d",
		Xpos,Ypos,Xspd,Yspd,health,run,inv)
	)
end

function CamhackHUD()	
	if working==0 then
		-- screen edge
		box((backx-camx-  1)/div,
			(backy-camy-  1)/div,
			(backx-camx+320)/div,
			(backy-camy+224)/div,
			0xff0000ff)
		-- map edge
		box(       0-camx/div+size,
			       0-camy/div+size,
			mapw/div-camx/div,
			maph/div-camy/div,
			0xff0000ff)
	end
	
	text(260,206,string.format("cHack: %s\nscale: %d",ch,div))
end

function Background()
	if working>0 then
		cache = {}
		return
	end
	
	if camhack then
		camx = Xpos-320/2*div
		camy = Ypos-224/2*div
		box(0,0,320,240,0,0x66000000)
		ch = "on"
	else
		ch = "off"
	end
	
	local border = 0
	local offset = 32
	local basex  = camx+border
	local basey  = camy+border
	local basei  = PosToIndex(basex-offset,basey-offset)
	local boundx = 320*div-border
	local boundy = 224*div-border
	local xblockstockeck = ((camx+boundx+offset)-(basex-offset))/size/div
	local yblockstockeck = ((camy+boundy+offset)-(basey-offset))/size/div

	for yblock = 0,yblockstockeck do
		for xblock = 0,xblockstockeck do
			local i = yblock*xblocks+xblock+basei
			local x = basex+xblock*size*div
			local y = basey+yblock*size*div
			
			if InBounds(x,basex-offset,camx+boundx+offset) then
				local unit = cache[i]
				
				if unit == nil or workinglast>0 then
					if  InBounds(x,basex,camx+boundx)
					and InBounds(y,basey,camy+boundy)
					then cache[i] = GetBlock(x,y)
					end
				else
					if  not InBounds(x,basex,camx+boundx)
					and not InBounds(y,basey,camy+boundy)
					then cache[i] = nil
					end
				end
				
				if unit ~= nil then
					DrawBG(unit,x,y)
				end
			elseif cache[i] ~= nil
			then cache[i] = nil		
			end
		end
	end
end

function DrawBG(unit, x, y)
	local val= 0
	local x1 = x/div-camx/div-(camx%16)/div
	local x2 = x1+size-1
	local y1 = y/div-camy/div-(camy%16)/div
	local y2 = y1+size-1
	
	if unit.contour ~= nil then
		box(x1,y1,x2,y2,0x5500ff00,0x5500ff00)
		
		for pixel=0,15 do
			val = unit.contour[pixel]
			if val>0 then
				gui.drawPixel(
					x1+pixel/div,
					y1+val/div-1/div,
					0xffffff00)
			end
		end
	end	
	
	if unit.block>0 then
		local Fn = DrawBlock[unit.block] or DrawBlockDefault
		Fn(x1,y1,x2,y2)
		box(x1,y1,x2,y2,col+opin,col+opout)
	end
end

function GetBlock(x,y)
	if working>0 then return nil end
	
	local final = { contour={}, block=0 }
	
	if  x>0 and x<mapw
	and y>0 and y<maph then
		local pixels = 0
		local x1  = x/div-camx/div
		local x2  = x1+size-1
		local y1  = y/div-camy/div
		local y2  = y1+size-1
		local d4  = rw(mapline_tab+SHIFT(y,4)*2)
		local a1  = r24(LevelFlr+1)
		local d1  = SHIFT(rw(MapA_Buff+d4+SHIFT(x,4)*2),1)
		final.block = rb(a1+d1+2)
		d1 = rw(a1+d1)
		a1 = r24(LevelCon+1)+d1
		
		if rb(a1)>0 or rb(a1+8)>0 then
			for pixel=0,15 do
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

function PosToIndex(x,y)
	return math.floor(x/16)+math.floor(y/16)*xblocks
end

function IndexToPos(i)
	return { x=(i%xblocks)*16, y=math.floor(i/xblocks)*16 }
end

function InBounds(x,minimum,maximum)
	if x>=minimum and x<=maximum
	then return true
	else return false
	end
end

DrawBlock = {
	[0x80] = function(x1,y1,x2,y2)    -- WALL
		col = 0x00ffffff              -- white
		line(x1,y1,x1,y2,col+op)      -- left
		line(x2,y1,x2,y2,col+op)      -- right
	end,
	[0x81] = function(x1,y1,x2,y2)    -- CEILING
		col = 0x00ffffff              -- white
		line(x1,y2,x2,y2,col+op)      -- bottom
	end,
	[0x82] = function(x1,y1,x2,y2)    -- CLIMB_U
		col = 0x0000ffff              -- cyan
		line(x1,y2,x2,y2,col+op)      -- bottom
	end,
	[0x83] = function(x1,y1,x2,y2)    -- CLIMB_R
		col = 0x0000ffff              -- cyan
		line(x1,y1,x1,y2,col+op)      -- left
	end,
	[0x84] = function(x1,y1,x2,y2)    -- CLIMB_L
		col = 0x0000ffff              -- cyan
		line(x2,y1,x2,y2,col+op)      -- right
	end,
	[0x85] = function(x1,y1,x2,y2)    -- CLIMB_LR
		col = 0x0000ffff              -- cyan
		line(x1,y1,x1,y2,col+op)      -- left
		line(x2,y1,x2,y2,col+op)      -- right
	end,
	[0x86] = function(x1,y1,x2,y2)    -- CLIMB_R_STAND_R
		col = 0x00ffffff              -- white
		line(x1,y1,x2,y1,col+op)      -- top
		col = 0x0000ffff              -- cyan
		line(x1,y1,x1,y2,col+op)      -- left
	end,
	[0x87] = function(x1,y1,x2,y2)    -- CLIMB_L_STAND_L
		col = 0x00ffffff              -- white
		line(x1,y1,x2,y1,col+op)      -- top
		col = 0x0000ffff              -- cyan
		line(x2,y1,x2,y2,col+op)      -- right
	end,
	[0x88] = function(x1,y1,x2,y2)    -- CLIMB_LR_STAND_LR
		col = 0x00ffffff              -- white
		line(x1,y1,x2,y1,col+op)      -- top
		col = 0x00ff00ff              -- cyan
		line(x1,y1,x1,y2,col+op)      -- left
		col = 0x0000ffff              -- cyan
		line(x2,y1,x2,y2,col+op)      -- right
	end,
	[0x70] = function(x1,y1,x2,y2)    -- GRAB_SWING
		col = 0x0000ff00              -- green
		box(x1,y1,x2,y2,col,col+opout)
	end,
	[0x7f] = function(x1,y1,x2,y2)    -- EXIT
		col = 0x00ffff00              -- yellow
	end,
	[0xd0] = function(x1,y1,x2,y2)    -- SPIKES
		col = 0x00ff0000              -- red
		box(x1,y1,x2,y2,col,col+opout)
	end,
	[0xd1] = function(x1,y1,x2,y2)    -- SPIKES
		col = 0x00ff0000              -- red
		box(x1,y1,x2,y2,col,col+opout)
	end
}

function DrawBlockDefault(x1,y1,x2,y2)-- LEVEL_SPECIFIC
	col = 0x00ff8800                  -- orange
	box(x1,y1,x2,y2,col+opin,col+opout)
end

function Objects()
	if working>0 then return end
	
	for i=0,63 do
		local base = GlobalBase+i*128
		local flag2 = AND(rb(base+0x49),0x10) -- active
		
		if flag2==0x10 then
			local xpos = rw (base+0x00)
			local ypos = rw (base+0x02)
			local dmg  = rb (base+0x10)
			local type = rw (base+0x40)
			local hp   = rw (base+0x50)
			local cRAM = r24(base+0x75) -- pointer to 4 collision boxes per object
			local col  = 0              -- collision color
			local xscr = (xpos-camx)/div
			local yscr = (ypos-camy)/div
			
			for boxx=0,4 do
				local x1 = (rws(cRAM+boxx*8+0)-camx)/div
				local y1 = (rws(cRAM+boxx*8+2)-camy)/div
				local x2 = (rws(cRAM+boxx*8+4)-camx)/div
				local y2 = (rws(cRAM+boxx*8+6)-camy)/div
				
				if boxx==0 then 
					col = 0xff00ff00      -- body
					if type==282 or type==258 then hp = 1 end -- archer hp doesn't matter
					if hp>0 and type>0 then
						text(x1+2,y1+1,string.format("%d",hp),col,0x88000000,"gens")
					end
				elseif boxx==1 then 
					col = 0xffffff00      -- floor
				elseif boxx==2 then
					if dmg>0
					then col = 0xffff0000 -- projectile
					else col = 0xff8800ff -- item
					end
					if dmg>0 then
						text(x1+2,y2+1,string.format("%d",dmg),col,0x88000000,"gens")
					end
				else
					col = 0xffffffff     -- other
				end
				
				if x1~=0x8888 and x2<320 and x1>0 and y2<224 and y1>0 then
					box(x1,y1,x2,y2,col)
				end
			end
		end
	end
end

function PlayerBoxes()
	if working>0 then return end
	
	local xx = (Xpos-camx)/div
	local yy = (Ypos-camy)/div
	local col = 0xff00ffff
	local swcol = col      -- usual detection
	
	if Yspd>0 then         -- gimme swings to grab!
		swcol = 0xff00ff00
	elseif Yspd==0 then    -- can tell that too
		swcol = 0xffffffff
	end
	
	if facing==2 then
		box(xx-0xf /div-2,yy-0x2c/div-1,xx-0xf /div+0,yy-0x2c/div+1,swcol) -- lefttop
	else
		box(xx+0xf /div-1,yy-0x2c/div-1,xx+0xf /div+1,yy-0x2c/div+1,swcol) -- rightttop
	end
	
	box(xx         -1,yy-0x2c/div-1,xx         +1,yy-0x2c/div+1,col) -- top
	box(xx-0xf /div-2,yy-0x1f/div-1,xx-0xf /div+0,yy-0x1f/div+1,col) -- left
	box(xx+0x10/div-1,yy-0x1f/div-1,xx+0x10/div+1,yy-0x1f/div+1,col) -- right
--	box(xx         -1,yy-0x1f/div-1,xx         +1,yy-0x1f/div+1,col) -- center
	box(xx         -1,yy-0x0f/div-1,xx         +1,yy-0x0f/div+1,col) -- bottom
	box(xx         -1,yy         -1,xx         +1,yy  +1,0xffffff00) -- feet
--	box(xx         -1,yy+0x10/div-1,xx         +1,yy+0x10/div+1,col) -- ground
end

function Input()
	local i,u,d,l,r,a,b,c,s
	
	if movie.isloaded() then
		i = movie.getinput(emu.framecount()-1)
	else
		i = joypad.getimmediate()
	end
	
	if i["P1 Up"   ] then u = "U" else u = " " end
	if i["P1 Down" ] then d = "D" else d = " " end
	if i["P1 Left" ] then l = "L" else l = " " end
	if i["P1 Right"] then r = "R" else r = " " end
	if i["P1 A"    ] then a = "A" else a = " " end
	if i["P1 B"    ] then b = "B" else b = " " end
	if i["P1 C"    ] then c = "C" else c = " " end
	if i["P1 Start"] then s = "S" else s = " " end
	
	text(1,10,u..d..l..r..a..b..c..s,"yellow")
end

event.onframeend(function()
	emu.setislagged(rb(0xfff6d4)==0)
	
	if rb(0xfff6d4)==0 then
		lagcount = lagcount+1
		framecol = "red"
	else
		framecol = "white"
	end
	
	emu.setlagcount(lagcount)
	
	rndlast = rnd1
	workinglast = working
	XposLast = Xpos
	YposLast = Ypos
end)

while true do
	main()
	emu.frameadvance()
end