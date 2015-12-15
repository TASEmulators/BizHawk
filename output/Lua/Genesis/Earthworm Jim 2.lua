-- feos, 2015

--== Globals ==--
lastcfg = 0
dcfg = 0
rngcount = 0
rngcolor = "white"
rngobject = 0
rngroutine = 0
MsgTime  = 16
MsgStep  = 256/MsgTime
MsgTable = {}

--== Shortcuts ==--
local rb   = memory.read_u8
local rbs  = memory.read_s8
local rw   = memory.read_u16_be
local rws  = memory.read_s16_be
local rl   = memory.read_u32_be
local box  = gui.drawBox
local text = gui.pixelText
local line = gui.drawLine
local AND  = bit.band
local SHIFT= bit.lshift

function Configs()
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

function Seek()
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

function Clamp(v1,v2,v3)
	if     v1<v2 then v1=v2
	elseif v1>v3 then v1=v3
	end
	return v1
end

function Bounce()
	if rb(0xa515)==0x60
	then offset = 8
	else offset = 0
	end
	local counter = rb(0xfc87)
	local a0 = 0xfc88
	local d0 = SHIFT(rb(a0+counter),5)+offset
	local vel = rw(0x25d482+d0, "MD CART")
	if     vel == 0x200 then bounce = 3
	elseif vel == 0x3e0 then bounce = 1
	else                     bounce = 2
	end
	text(60,0,string.format("bounce: %X",bounce))
end

function Objects()	
	local base0 = 0xa2ea
	for i=0,0x32 do
		local base = base0+i*0x6e
		local id = rb(base)
		if id>0 and id~=0x82 then
			local hp = rbs(base+1)
			local x = rw(base+2)-4096-camx
			local y = rw(base+4)-4096-camy
			--x = Clamp(x,0,300)
			--y = Clamp(y,8,210)
			local dx = rws(base+0x18)
			local dy = rws(base+0x1a)
			local hitboxbase = rl(base+0x14)
			if hitboxbase<0x300000 then
				local x1 = rb(hitboxbase+2,"MD CART")
				local x2 = rb(hitboxbase+4,"MD CART")
				local y1 = rb(hitboxbase+3,"MD CART")
				local y2 = rb(hitboxbase+5,"MD CART")
				local of = 124
				if x1>0 and x2>0 and y1>0 and y2>0 then
					x1 = x1+x-of
					x2 = x2+x-of
					y1 = y1+y-of
					y2 = y2+y-of
					box(x1,y1,x2,y2,0xff00ff00,0)
				end
			end
			--text(x,y,string.format("%X",id),"yellow")
			if hp~=0 then
				text(x,y+6,hp,0xff00ff00)
			end
			PostRngRoll(base,x,y)
		end
	end
end

function PostRngRoll(object,x,y)
	for i = 1, #MsgTable do
		if (MsgTable[i]) then
			if object==MsgTable[i].object_ then
				local color = 0x00ff0000+SHIFT((MsgTable[i].timer_-emu.framecount())*MsgStep,24)
				line(130,7*i+8,x,y,color)
				text(120,7*i+8,string.format("%X",MsgTable[i].routine_),color)
			end
			if (MsgTable[i].timer_<emu.framecount()) then
				MsgTable[i] = nil
			end
		end
	end
end

event.onframestart(function()
	rngcount = 0
	rngcolor = "white"
	rngobject = 0
end)

event.onmemorywrite(function()
	rngcount = rngcount+1
	rngcolor = "red"
	rngobject = emu.getregister("M68K A1")
	rngroutine = emu.getregister("M68K A0")	
	for i = 1, 30 do
		if MsgTable[i] == nil then
			MsgTable[i] = {
				timer_ = MsgTime + emu.framecount(),
				object_  = rngobject-0xff0000,
				routine_ = rngroutine
			}
			break
		end
	end
end, 0xffa1d4)

while true do
	mult = client.getwindowsize()
	camx = rw(0xa172)
	camy = rw(0xa174)
	
	Objects()
	Configs()
	emu.frameadvance()
end