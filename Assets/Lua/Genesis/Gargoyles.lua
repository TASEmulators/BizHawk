-- Gargoyles, Genesis (BizHawk)
-- feos, 2015-2016

--== Shortcuts ==--
rb   = memory.read_u8
rw   = memory.read_u16_be
rws  = memory.read_s16_be
r24  = memory.read_u24_be
rl   = memory.read_u32_be
box  = gui.drawBox
text = gui.pixelText
line = gui.drawLine
AND  = bit.band
SHIFT= bit.rshift

--== RAM addresses ==--
GlobalBase  = 0xff1c76
GolBase     = 0xff2c76
MapA_Buff   = 0xff4af0
mapline_tab = 0xff0244
LevelFlr    = 0xff00c0
LevelCon    = 0xff00c4
levnum      = 0xff00ba

--== Camera Hack ==--
camhack  = true
div      = 2      -- scale
size     = 16/div -- block size

--== Other stuff ==--
XposLast = 0
YposLast = 0
room     = 0
lagcount = emu.lagcount()
gui.defaultPixelFont("fceux")

function main()
	camx     = rws(0xff010c)+16
	camy     = rws(0xff010e)+16
	backx    = camx
	backy    = camy
	mapw     = rw(0xff00d4)*8
	maph     = rw(0xff00d6)*8
	Xpos     = rws(0xff0106)
	Ypos     = rws(0xff0108)
	health   = rws(0xff2cc6)
	facing   = AND(rb(GolBase+0x48),2) -- object flag 1
	working  = rb(0xff0073)
	run      = rb(0xff1699)
	inv      = rw(0xff16d2)
	rnd1     = rl(0xff001c)
	rnd2     = rw(0xff0020)
	Xspd     = Xpos-XposLast
	Yspd     = Ypos-YposLast
	XposLast = Xpos
	YposLast = Ypos
	
	CamhackHUD()
	Background()
	Objects()
	PlayerBoxes()
	HUD()
	RoomTime()
	Input()
	rndlast = rnd1
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

function Background()
	for i=0,20*div do
		for j=0,14*div do
			GetBlock(i,j)
		end
	end
end

function CamhackHUD()
	if camhack then
		camx = Xpos-320/2*div
		camy = Ypos-224/2*div
		box(0,0,320,240,0,0x66000000)
		ch = "on"
	else
		ch = "off"
	end
	text(260,206,string.format("cHack: %s\nscale: %d",ch,div))
	if working==0 then
		-- screen borders
		box((backx-camx-  1)/div,
			(backy-camy-  1)/div,
			(backx-camx+320)/div,
			(backy-camy+224)/div,
			0xff0000ff)
		-- map borders
		box(       0-camx/div+size,
			       0-camy/div+size,
			mapw/div-camx/div,
			maph/div-camy/div,
			0xff0000ff)
	end
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

function GetBlock(x,y)
	if working>0 then return end
	x = camx+x*size*div-AND(camx,0xf)
	y = camy+y*size*div-AND(camy,0xf)
	if  x>0 and x<mapw
	and y>0 and y<maph then
		local x1    = x/div-camx/div
		local x2    = x1+size-1
		local y1    = y/div-camy/div
		local y2    = y1+size-1
		local d4    = rw(mapline_tab+SHIFT(y,4)*2)
		local a1    = r24(LevelFlr+1)
		local d1    = SHIFT(rw(MapA_Buff+d4+SHIFT(x,4)*2),1)
		local ret   = rb(a1+d1+2) -- block
		local col   = 0           -- block color
		local opout = 0x33000000  -- outer opacity
		local opin  = 0x66000000  -- inner opacity
		local op    = 0xff000000
		d1 = rw(a1+d1)
		a1 = r24(LevelCon+1)+d1
		if rb(a1)>0 or rb(a1+8)>0 then
			box(x1,y1,x2,y2,0x5500ff00,0x5500ff00)
			for pixel=0,15 do
				retc = rb(a1+pixel) -- contour
				if retc>0 then
					gui.drawPixel(x1+pixel/div,y1+retc/div-1/div,0xffffff00)
				--	text(x1,y1,string.format("%X",retc))		
				end
			end
		end
		if ret>0 then
			if     ret==0x80 then        -- WALL
				col = 0x00ffffff         -- white
				line(x1,y1,x1,y2,col+op) -- left
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x81 then        -- CEILING
				col = 0x00ffffff         -- white
				line(x1,y2,x2,y2,col+op) -- bottom
			elseif ret==0x82 then        -- CLIMB_U
				col = 0x0000ffff         -- cyan
				line(x1,y2,x2,y2,col+op) -- bottom
			elseif ret==0x83 then        -- CLIMB_R
		--		col = 0x00ffffff         -- white
		--		line(x2,y1,x2,y2,col+op) -- right
				col = 0x0000ffff         -- cyan
				line(x1,y1,x1,y2,col+op) -- left
			elseif ret==0x84 then        -- CLIMB_L
		--		col = 0x00ffffff         -- white
		--		line(x1,y1,x1,y2,col+op) -- left
				col = 0x0000ffff         -- cyan
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x85 then        -- CLIMB_LR
				col = 0x0000ffff         -- cyan
				line(x1,y1,x1,y2,col+op) -- left
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x86 then        -- CLIMB_R_STAND_R
				col = 0x00ffffff         -- white
				line(x1,y1,x2,y1,col+op) -- top
				col = 0x0000ffff         -- cyan
				line(x1,y1,x1,y2,col+op) -- left
			elseif ret==0x87 then        -- CLIMB_L_STAND_L
				col = 0x00ffffff         -- white
				line(x1,y1,x2,y1,col+op) -- top
				col = 0x0000ffff         -- cyan
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x87 then        -- CLIMB_LR_STAND_LR
				col = 0x00ffffff         -- white
				line(x1,y1,x2,y1,col+op) -- top
				col = 0x00ff00ff         -- cyan
				line(x1,y1,x1,y2,col+op) -- left
				col = 0x0000ffff         -- cyan
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x70 then        -- GRAB_SWING
				col = 0x0000ff00         -- green
				box(x1,y1,x2,y2,col,col+opout)
		--	elseif ret==0x72 then        -- FORCE_TURN (for enemies)
		--		col = 0x0088ff00         -- green
		--		box(x1,y1,x2,y2,col,col+opout)
			elseif ret==0x7f then        -- EXIT
				col = 0x00ffff00         -- yellow
			elseif ret==0xd0
			or ret==0xd1 then            -- SPIKES
				col = 0x00ff0000         -- red
				box(x1,y1,x2,y2,col,col+opout)
			else                         -- LEVEL_SPECIFIC
				col = 0x00ff8800         -- orange
				box(x1,y1,x2,y2,col+opin,col+opout)
			end
			box(x1,y1,x2,y2,col+opin,col+opout)
		--	text(x1,y1,string.format("%X",ret))
		end
	end
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
					col = 0xff00ff00     -- body
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
	xx = (Xpos-camx)/div
	yy = (Ypos-camy)/div
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

event.onframeend(function()
	emu.setislagged(rb(0xfff6d4)==0)
	if rb(0xfff6d4)==0 then
		lagcount = lagcount+1
		framecol = "red"
	else
		framecol = "white"
	end
	emu.setlagcount(lagcount)
end)

while true do
	main()
	emu.frameadvance()
end