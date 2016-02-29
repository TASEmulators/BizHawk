-- Gargoyles, Genesis
-- feos, 2015

--== Shortcuts ==--
local rb   = memory.read_u8
local rw   = memory.read_u16_be
local rws  = memory.read_s16_be
local rl   = memory.read_u32_be
local box  = gui.drawBox
local text = gui.pixelText
local line = gui.drawLine
local AND  = bit.band
local SHIFT= bit.rshift
--== RAM addresses ==--
local GlobalBase  = 0x1c76
local GolBase     = 0x2c76
local MapA_Buff   = 0x4af0
local mapline_tab = 0x0244
local LevelFlr    = 0x00c0
local LevelCon    = 0x00c4
local levnum      = 0x00ba
--== Camera Hack ==--
local camhack  = false
local div      = 1      -- scale
local size     = 16/div -- block size
--== Other stuff ==--
local XposLast = 0
local YposLast = 0

function main()
	camx = (rws(0x010c)+16)
	camy = (rws(0x010e)+16)
	backx = camx
	backy = camy
	mapw = rw(0x00d4)*8
	maph = rw(0x00d6)*8
--	text(100,0,mapw.." "..maph)
	Xpos = rws(0x0106)
	Ypos = rws(0x0108)
	health = rws(0x2cc6)
	facing = AND(rb(GolBase+0x48),2) -- object flag 1
	working= rb(0x0073)
	if camhack then
		camx = Xpos-320/2*div
		camy = Ypos-224/2*div
	end
	run  = rb(0x1699)
	inv  = rw(0x16d2)
	rnd1 = rl(0x001c)
	rnd2 = rw(0x0020)
	Xspd = Xpos-XposLast
	Yspd = Ypos-YposLast
	XposLast = Xpos
	YposLast = Ypos
	rndlast = rnd1
    --[ [--
	if camhack then box(0,0,320,240,0,0x66000000) end
	if working==0 then
		box(
			(backx-camx-  1)/div,
			(backy-camy-  1)/div,
			(backx-camx+320)/div,
			(backy-camy+224)/div,
			0x0000ffff)
		box(       0-camx/div+size,
			       0-camy/div+size,
			mapw/div-camx/div,
			maph/div-camy/div,
			0xff0000ff)
	end
	for i=0,20*div do
		for j=0,14*div do
			GetBlock(i,j)
		end
	end
    --]]--
	Objects()
	PlayerBoxes()
	HUD()
    RoomTime()
end

function RoomTime()
	local start11 = 767
	local start12 = 2294
	local start13 = 4101
	local startl4 = 6000
	timer = emu.framecount()
	if     timer < start11 then room = timer
	elseif timer < start12 then room = timer - start11
	elseif timer < start13 then room = timer - start12
	elseif timer < startl4 then room = timer - start13
	end
	text(100,217,"room cnt: "..room)
end

function GetBlock(x,y)
	if working>0 then return end
	x = camx+x*size*div-AND(camx,0xF)
	y = camy+y*size*div-AND(camy,0xF)
	if  x>0 and x<mapw and y>0 and y<maph then
		local x1    = x/div-camx/div
		local x2    = x1+size-1
		local y1    = y/div-camy/div
		local y2    = y1+size-1
		local d4    = rw(mapline_tab+SHIFT(y,4)*2)
		local a1    = AND(rl(LevelFlr),0xffff)
		local d1    = SHIFT(rw(MapA_Buff+d4+SHIFT(x,4)*2),1)
		local ret   = rb(a1+d1+2) -- block
		local col   = 0           -- block color
		local opout = 0x33000000  -- outer opacity
		local opin  = 0x66000000  -- inner opacity
		local op    = 0xff000000
		d1 = rw(a1+d1)
		a1 = AND(rl(LevelCon),0xffffff)+d1
		if rb(a1,"MD CART")>0 or rb(a1+8,"MD CART")>0 then
			box(x1,y1,x2,y2,0x5500ff00,0x5500ff00)
			for pixel=0,15 do
				retc = rb(a1+pixel,"MD CART") -- contour
				if retc>0 then
					gui.drawPixel(x1+pixel/div,y1+retc/div-1/div,0xffffff00)
				--	text(x1,y1,string.format("%X",retc))		
				end
			end
		end
		local lol = 0
		--if lol==0 then return end
		if ret>0 then
			if     ret==0x80 then -- WALL
				col = 0x00ffffff  -- white
				line(x1,y1,x1,y2,col+op) -- left
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x81 then -- CEILING
				col = 0x00ffffff  -- white
				line(x1,y2,x2,y2,col+op) -- bottom
			elseif ret==0x82 then -- CLIMB_U
				col = 0x0000ffff  -- cyan
				line(x1,y2,x2,y2,col+op) -- bottom
			elseif ret==0x83 then -- CLIMB_R
			--	col = 0x00ffffff  -- white
			--	line(x2,y1,x2,y2,col+op) -- right
				col = 0x0000ffff  -- cyan
				line(x1,y1,x1,y2,col+op) -- left
			elseif ret==0x84 then -- CLIMB_L
			--	col = 0x00ffffff  -- white
			--	line(x1,y1,x1,y2,col+op) -- left
				col = 0x0000ffff  -- cyan
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x85 then -- CLIMB_LR
				col = 0x0000ffff  -- cyan
				line(x1,y1,x1,y2,col+op) -- left
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x86 then -- CLIMB_R_STAND_R
				col = 0x00ffffff  -- white
				line(x1,y1,x2,y1,col+op) -- top
				col = 0x0000ffff  -- cyan
				line(x1,y1,x1,y2,col+op) -- left
			elseif ret==0x87 then -- CLIMB_L_STAND_L
				col = 0x00ffffff  -- white
				line(x1,y1,x2,y1,col+op) -- top
				col = 0x0000ffff  -- cyan
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x87 then -- CLIMB_LR_STAND_LR
				col = 0x00ffffff  -- white
				line(x1,y1,x2,y1,col+op) -- top
				col = 0x00ff00ff  -- cyan
				line(x1,y1,x1,y2,col+op) -- left
				col = 0x0000ffff  -- cyan
				line(x2,y1,x2,y2,col+op) -- right
			elseif ret==0x70 then -- GRAB_SWING
				col = 0x0000ff00  -- green
				box(x1,y1,x2,y2,col,col+opout)
		--	elseif ret==0x72 then -- FORCE_TURN (for enemies)
		--		col = 0x0088ff00  -- green
		--		box(x1,y1,x2,y2,col,col+opout)
			elseif ret==0x7f then -- EXIT
				col = 0x00ffff00  -- yellow
			elseif ret==0xd0 or ret==0xd1 then -- SPIKES
				col = 0x00ff0000  -- red
				box(x1,y1,x2,y2,col,col+opout)
			else -- LEVEL_SPECIFIC
				col = 0x00ff8800 -- orange
				box(x1,y1,x2,y2,col+opin,col+opout)
			end
			box(x1,y1,x2,y2,col+opin,col+opout)
		--	text(x1,y1,string.format("%X",ret))
		end
	end
end

function HUD()
	if working>0 then return end
	if camhack then ch = "on" else ch = "off" end
	if rndlast~= rnd1 then rndcol = "red" else rndcol = "white" end
	if memory.readbyte(0xF6D4)==0 then text(170,217,"LAG","red") end
	text(280,210,string.format("cHack: %s\nscale: %d",ch,div))
	text(  0,217,"rnd: ","yellow")
	text( 20,217,string.format("%08X %04X",rnd1,rnd2),rndcol)
	text(290,  0,string.format(
		"x: %4d\ny: %4d\ndx: %3d\ndy: %3d\nhp: %3d\nrun:%3d\ninv:%3d",
		Xpos,Ypos,Xspd,Yspd,health,run,inv),"yellow"
	)
end

function Objects()
	if working>0 then return end
	for i=0,63 do
		local base = GlobalBase+i*128
		local flag2 = AND(rb(base+0x49),0x10) -- active
		if flag2==0x10 then
			local xpos = rw(base+0x00)
			local ypos = rw(base+0x02)
		--	local anm  = rl(base+0x20)
			local dmg  = rb(base+0x10)
			local type = rw(base+0x40)
			local hp   = rw(base+0x50)
			local cRAM = rw(base+0x76) -- pointer to 4 collision boxes per object
			local col  = 0 -- collision color
			local xscr = (xpos-camx)/div
			local yscr = (ypos-camy)/div
			if type==0 then
		--		gui.text(xscr,yscr,string.format("%X",anm))
			end
			for boxx=0,4 do
				local x1 = (rws(cRAM+boxx*8+0)-camx)/div
				local y1 = (rws(cRAM+boxx*8+2)-camy)/div
				local x2 = (rws(cRAM+boxx*8+4)-camx)/div
				local y2 = (rws(cRAM+boxx*8+6)-camy)/div
				if boxx==0 then 
					col = 0xff00ff00 -- body
					if type==282 or type==258 then hp = 1 end -- archer hp doesn't matter
					if hp>0 and type>0 then
						text(x1+2,y1+1,string.format("%d",hp),col)
					end
				elseif boxx==1 then
					col = 0xffffff00 -- floor
				elseif boxx==2 then
					if dmg>0 then
						col = 0xffff0000 -- projectile
					else
						col = 0xff8800ff -- item
					end
					if dmg>0 then
						text(x1+2,y2+1,string.format("%d",dmg),col)
					end
				else
					col = 0xffffffff -- other
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
	local swcol = col -- usual detection
	if Yspd>0 then -- gimme swings to grab!!!
		swcol = 0xff00ff00
	elseif Yspd==0 then -- can tell that too
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

--event.onframeend(main)

while true do
	main()
	emu.frameadvance()
end