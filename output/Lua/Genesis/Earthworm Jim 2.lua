-- feos, 2015

--== Globals ==--
local lastcfg    = 0
local dcfg       = 0
local rngcount   = 0
local rngobject  = 0
local rngroutine = 0
local rngcolor   = "white"
local MsgTime    = 16
local MsgStep    = 256/MsgTime
local MsgTable   = {}

--== Slow stuff ==--
local drawwalls = 1
local drawfloor = 1
local drawbg    = 1
local drawrng   = 1

--== Shortcuts ==--
local rb     = memory.read_u8
local rbs    = memory.read_s8
local rw     = memory.read_u16_be
local rws    = memory.read_s16_be
local rl     = memory.read_u32_be
local box    = gui.drawBox
local text   = gui.pixelText
local line   = gui.drawLine
local AND    = bit.band
local SHIFTL = bit.lshift
local SHIFTR = bit.rshift

event.onframestart(function()
	rngcount  = 0
	rngobject = 0
	rngcolor  = "white"
end)

event.onmemorywrite(function()
	rngcount   = rngcount+1
	rngcolor   = "red"
	rngobject  = emu.getregister("M68K A1")
	rngroutine = emu.getregister("M68K A0")	
	for i = 1, 30 do
		if MsgTable[i] == nil then
			MsgTable[i] = {
				timer_   = MsgTime + emu.framecount(),
				object_  = rngobject-0xff0000,
				routine_ = rngroutine
			}
			break
		end
	end
end, 0xffa1d4)

local function PostRngRoll(object,x,y)
	if drawrng==0 then return end
	for i = 1, #MsgTable do
		if (MsgTable[i]) then
			if object==MsgTable[i].object_ then
				local color = 0x00ff0000+SHIFTL((MsgTable[i].timer_-emu.framecount())*MsgStep,24)
				line(130,7*i+8,x,y,color)
				text(120,7*i+8,string.format("%X",MsgTable[i].routine_),color)
			end
			if (MsgTable[i].timer_<emu.framecount()) then
				MsgTable[i] = nil
			end
		end
	end
end

local function Objects(base0, amount)
	for i=0,amount do
		local base  = base0+i*0x6e
		local id    = rb(base)
		local color = 0xff00ff00
		local of    = 128
		if id~=0 then
			-- attributes --
			local hp     = rbs(base+   1)
			local x      = rw (base+   2)-0x1000-camx
			local y      = rw (base+   4)-0x1000-camy
			local facing = rbs(base+   9)
			local hbbase = rl (base+0x14)
			local d      = rb (base+0x35)
			PostRngRoll(base,x,y)
			if (id>0 and id<130 and hbbase<0x300000) then
				-- hitbox --
				local x1 = rb(hbbase+2,"MD CART")
				local x2 = rb(hbbase+4,"MD CART")
				local y1 = rb(hbbase+3,"MD CART")
				local y2 = rb(hbbase+5,"MD CART")
				if x1>0 and x2>0 and y1>0 and y2>0 then
					if facing==-1 then
						x1 = 256-x1
						x2 = 256-x2
					end
					if d==-1 then
						y1 = 256-y1
						y2 = 256-y2
					end
					x1 = x+x1-of
					x2 = x+x2-of
					y1 = y+y1-of
					y2 = y+y2-of
					if id>=120 and id<=124 then color = 0xffff0000 end
					box(x1,y1,x2,y2,color,0)
					if hp>0 and id~=120 then text(x,y-2,hp,color) end
					--text(x,y,string.format("%X",id),"yellow")
				end
				-- whip
				if base==0xa20e then
					local wx = rb(hbbase+6,"MD CART")
					local wy = rb(hbbase+7,"MD CART")
					local ww = rb(0xff0c)
					local wh = rb(0xff0d)
					if facing==-1 then wx = 256-wx end
					if d     ~= 0 then wy = 256-wy end
					wx = wx+x-of
					wy = wy+y-of
					box(wx-ww,wy-wh,wx+ww,wy+wh,0xffff0000,0)
					if invcount>0 then text(x,y,invcount) end
				end
			end
		end
	end
end

local function GetFloor(x, y)
	if drawfloor==0 then return {0,0} end
	x = x*16+AND(camx,0xfff0)
	y = y*16+AND(camy,0xfff0)
	local d6 = SHIFTR(AND(y,0xfff0),3)
	local a0 = rw(d6+0xb4e8)
	local d0 = SHIFTR(x+0x10,4)
	local d2 = AND(x,0xf)
	local a1 = 0xb806+rw(0xfb9e)
	local a2 = 0x273e1e
	local d3 = SHIFTR(rw(a0+d0*2),1)
	local temp = a1+d3
	if temp>0xffff then return {0,0} end
	local d5 = SHIFTL(rb(temp),4)+d2
	local newd5 = SHIFTL(rb(a1+d3),4)+d2+15
	local newd0 = AND(rb(a2+newd5,"MD CART"),0x1f)
	return {AND(rb(a2+d5,"MD CART"),0x1f), newd0}
end

local function GetWall(x, y)
	if drawwalls==0 then return 0 end
	x = x*16+AND(camx,0xfff0)
	y = y*16+AND(camy,0xfff0)
	if y<0 then return 0 end
	local d6 = SHIFTR(AND(y+6,0xfff0),3)
	local a0 = rw(d6+0xb4e6)
	local temp = a0+SHIFTR(x,4)*2
	if temp>0xffff then return 0 end
	local d0 = rw(temp)
	temp = 0xb808+SHIFTR(d0,1)
	if temp>0xffff then return 0 end
	return rb(temp)
end

local function DrawBG()
	if drawbg==0 then return end
	for j=1,15 do
		for i=1,21 do
			local a0 = GetWall(i,j)
			if a0>0 then
				x1 = i*16-AND(camx,0xf)-16
				y1 = j*16-AND(camy,0xf)-16
				x2 = i*16-AND(camx,0xf)-1
				y2 = j*16-AND(camy,0xf)-1
				if     a0==255 then -- normal
					box(x1,y1,x2,y2,0xff00ffff,0x4400ffff)
				elseif a0==228 then -- snot
					box(x1,y1,x2,y2,0xff00ff00,0x4400ff00)
				elseif a0==117 then -- right
					box(x1,y1,x2,y2,0x4400ffff,0x4400ffff)
					line(x2,y1,x2,y2,0xff00ffff)
				elseif a0==116 then -- left
					box(x1,y1,x2,y2,0x4400ffff,0x4400ffff)
					line(x1,y1,x1,y2,0xff00ffff)
				elseif a0==113 or a0==114 then -- grab corner
					box(x1,y1,x2,y2,0x66ffff00,0x66ffff00)
				elseif a0==60 or a0==61
					or a0>=74 and a0<=77
					or a0==58 or a0==59 then -- stomack
					box(x1,y1,x2,y2,0xffff0000,0x44ff0000)
				elseif a0==73 then -- stomack
					box(x1,y1,x2,y2,0x44ff0000,0x44ff0000)
				elseif a0==54 or a0==40 then -- kitchen
					box(x1,y1,x2,y2,0xffff0000,0x44ff0000)
				elseif a0==41 then -- kitchen
					box(x1,y1,x2,y2,0x44ff0000,0x44ff0000)
				else -- other occasional crap
					box(x1,y1,x2,y2,0x66ffffff,0x66ffffff)
					--text(x1,y1,a0)
				end
			end
			
			local d0 = GetFloor(i,j)[1]
			if d0>0 then
				local newd0 = GetFloor(i,j)[2]
				if newd0>0 then
					x1 = i*16-AND(camx,0xf)
					y1 = j*16-AND(camy,0xf)+d0
					x2 = i*16-AND(camx,0xf)+15
					y2 = j*16-AND(camy,0xf)+newd0
				else
					for k=-2,2 do
						newd0 = GetFloor(i+1,j+k)[1]
						if newd0>0 then
							local add = 1
							if k==-2 then
								k = 0
								add = -1
							elseif k==-1 then
								add = 2
							elseif k==2 then
								add = 1
							end
							x1 = i*16-AND(camx,0xf)
							y1 = j*16-AND(camy,0xf)+d0
							x2 = i*16-AND(camx,0xf)+16
							y2 = j*16-AND(camy,0xf)-newd0+k*16+add
							--text(x1,y2,k,"red")
						end
					end
				end
				line(x1,y1,x2,y2,0xff00ff00)
			end
		end
	end
end

local function Seek()
	bytes = 0
	waves = 0
	steps = 0
	local ret = ""
	for bytes=0,10000 do
		local cfg = rl(0xfc2a)+bytes
		local action = rb(cfg, "MD CART")
		local newaction = rb(cfg+1, "MD CART")
		if action==0x7a then
			waves=waves+1
			steps=steps+1
		end
		if action==0x63 or action==0x64 or (action==0 and newaction==0) then
			steps=steps+1
		end
		if action>=0x30 and action<=0x32 then
			if newaction==0x70 then
				steps=steps+1
			elseif newaction==0x62 then
				ret = string.format("BOMB in %d waves %d steps",waves,steps,bytes)
				break
			end
		elseif action==3 then
			ret = string.format("Forth in %d waves %d steps",waves,steps,bytes)
			break
		elseif action==0xe and newaction==8 then
			ret = string.format("Back in %d waves %d steps",waves,steps,bytes)
			break
		end
	end
	text(120,7,ret)
end

local function Bounce()
	if rb(0xa515)==0x60
	then offset = 8
	else offset = 0
	end
	local counter = rb(0xfc87)
	local a0 = 0xfc88
	local d0 = SHIFTL(rb(a0+counter),5)+offset
	local vel = rw(0x25d482+d0, "MD CART")
	if     vel == 0x200 then bounce = 3
	elseif vel == 0x3e0 then bounce = 1
	else                     bounce = 2
	end
	text(60,0,string.format("bounce: %X",bounce))
end

local function Configs()
	local rng = rl(0xa1d4)
	text(120,0,string.format("rng: %08X:%d",rng,rngcount),rngcolor)
	local cfg0 = rl(0xfc2a)
	if cfg0==0 then return end
	local cfg1 = rl(0xfc9a)
	text(220,0,string.format("cfg old:  %X\ncfg step: %d",cfg1,dcfg))
	if lastcfg~=cfg0 then dcfg = cfg0-lastcfg end
	lastcfg = cfg0
	local h = 7
	for i=0,20 do
		local config = rl(0xfc2a)+i
		local action = rb(config,"MD CART")
		local newaction = memory.readbyte(config+1,"MD CART")
		if action==0x62
		or (action==0xe and newaction==8)
		or action==8
		or action==3 then color = "red"
		elseif action>=0x63 and action<=0x64 then color = "orange"
		elseif action>=0x30 and action<=0x32 then color = 0xff00ff00
		elseif action>=0x65 and action<=0x70 then color = 0xff00cc00
		elseif action==0x7a then color = "white"
		else color = 0xffaaaaaa
		end
		text(270,i*h+42,string.format("%X:%02X",config,action),color)		
		if i>0
		and action==0x7a
		or action==0x2b
		or action==0x2d
		then break
		end
	end
	Bounce()
	Seek()
end

while true do
	camx = rw(0xa172)
	camy = rw(0xa174)
	scrx = rw(0xa176)-0x1000
	scry = rw(0xa178)-0x1000
	absx = rw(0xa17e)-0x1000
	absy = rw(0xa180)-0x1000
	invcount = rb(0xfbd6)	
	if rb(0xa313)==4 and rb(0xa317)~=2 then
		DrawBG()
		Objects(0xa2ea, 0x23)
		Objects(0xa20e, 0)
		Configs()
	end
	emu.frameadvance()
end