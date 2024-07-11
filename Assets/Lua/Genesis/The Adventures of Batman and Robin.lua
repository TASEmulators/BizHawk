-- The Adventures of Batman and Robin
-- 2013-2024, feos and r57shell

-- GLOBALS --
MsgTable  = {}
MsgTime   = 30
MsgOffs   = 16
MsgCutoff = 60
RNGcount  = 0
SpawnCount= 0
SpawnOpac = 192
SpawnX    = 0
SpawnY    = 0
Enemies   = 0
Items     = 0
Hearts    = 0

-- SHORTCUTS --
rb  = memory.read_u8
rbs = memory.read_s8
rw  = memory.read_u16_be
rws = memory.read_s16_be
rl  = memory.read_u32_be
rls = memory.read_s32_be
rex = event.onmemoryexecute
getr= emu.getregister
box = gui.drawBox
text= gui.pixelText
line= gui.drawLine

memory.usememorydomain("M68K BUS")

function ToSigned16(num) 
	if num > 32768 then
		num = num - (2 * 32768)
		return num
	else return num
	end
end

function FitX(x, text)
	local length = 0
	if text ~= nil then length = string.len(text)*5 end
	if x <   0 then x = 0
	elseif x+length > 319 then x = 319-length end
	return x
end

function FitY(y)
	    if y <   0 then y = 0
	elseif y > 210 then y = 210 end
	return y
end

function GetCam()
	xcam = rws(0xFFDFC4)
	if rb(0xFFFFF6) == 50 then
		ycam = rws(0xFFDFE0)-20
	else
		ycam = 0
	end
end

function EnemyPos(Base)
	GetCam()
	x1 = rws(Base + 0x12) - xcam
	y1 = rws(Base + 0x14) - ycam
	x2 = rws(Base + 0x16) - xcam
	y2 = rws(Base + 0x18) - ycam
	hp = rws(Base + 0x1E)
end

function PlayerPos()
	local sbase1 = rw(0xFFAD5C) + 0xFF0000
	local sbase2 = rw(0xFFADB6) + 0xFF0000
	p1speedx = rls(sbase1 + 0x18) / 0x10000
	p1speedy = rls(sbase1 + 0x1C) / 0x10000
	p2speedx = rls(sbase2 + 0x18) / 0x10000
	p2speedy = rls(sbase2 + 0x1C) / 0x10000
  	LIFT = rw(0xFF9024)
end

function HandleMsgTable(clear)
	for i = 1, #MsgTable do
		if (clear) then
			MsgTable[i] = nil
		end		
		if (MsgTable[i]) then
			GetCam()
			if (MsgTable[i].y_ > MsgCutoff) then
				MsgY1 = 0
				MsgY2 = 6
			else
				MsgY1 = 203
				MsgY2 = 203				
			end
			local opacity = ((MsgTable[i].timer_ - emu.framecount() + 2)*7 & 0xFF) << 24
			line(i * MsgOffs + 3, MsgY2, MsgTable[i].x_ - xcam, MsgTable[i].y_, 0xFF0000+opacity)
			text(i * MsgOffs    , MsgY1, MsgTable[i].damage_, "red")
			if (MsgTable[i].timer_ < emu.framecount()) then
				MsgTable[i] = nil
			end
		end
	end
end

function HandleDamage()
	local damage = getr("M68K D0") &   0xFFFF
	local base   = getr("M68K A2") & 0xFFFFFF
	EnemyPos(base)
	unit = {
		timer_ = emu.framecount() + MsgTime,
		damage_ = damage,
		x_ = x1 + xcam,
		y_ = y1
	}
	for i = 1, 200 do
		if MsgTable[i] == nil then
			MsgTable[i] = unit
			break
		end
	end
end

function Collision()
	GetCam()
	local a0     = getr("M68K A0") & 0xFFFFFF
	local a6     = getr("M68K A6") & 0xFFFFFF
	local wx1    = ToSigned16(getr("M68K D4") & 0xFFFF) - xcam
	local wy1    = ToSigned16(getr("M68K D5") & 0xFFFF) - ycam
	local wx2    = ToSigned16(getr("M68K D6") & 0xFFFF) - xcam
	local wy2    = ToSigned16(getr("M68K D7") & 0xFFFF) - ycam
	local damage = rw(a6 + 0x12)
	local id     = rw(a6 + 2)
--	text(wx2 + 2, wy1 + 1, string.format("%X",a6))
	if (damage == 0) then
		damage = rw(a0 + 0x34)
	end
	if (DamageHitbox) then
		box(wx1, wy1, wx2, wy2, 0xFFFF0000)
		text(wx1 + 2, wy1 + 1, damage)
	else
		box(wx1, wy1, wx2, wy2, 0xFFFFFF00)
		if id == 0x53B4 then Hearts = Hearts + 1 end
	end
end

function InRange(var, num1, num2)
	if (var >=  num1) and (var <= num2)
	then return true
	end
end

function Item()
	GetCam()
	local a6   = getr("M68K A6") & 0xFFFFFF
	local x    = rw(a6 + 0x3E) - xcam
	local y    = rw(a6 + 0x42)
	local code = rb(a6 + 0x19)
	if     InRange(code,  0,  1) then return
	elseif InRange(code,  7, 19) then item = "Amo" -- ammo
	elseif InRange(code, 21, 23) then item = "Cha" -- fast charge
	elseif InRange(code, 24, 26) then item = "Bom" -- bomb
	elseif InRange(code, 27, 29) then item = "Lif" -- life
	elseif InRange(code, 30, 47) then item = "HiP" -- hearts
	else                              item = tostring(code)
	end
	text(x-7, y, string.format("%s"  , item   ), "yellow")
--	text(x-7, y, string.format("\n%X", a6+0x19), "yellow")
end

function Hitbox(address)
	local i = 0
	local base = rw(address)	
	while (base ~= 0) do
		base = base + 0xFF0000
		if (rw(base + 2) == 0) then break end
		EnemyPos(base)
		if (address == 0xFFDEB2) then
			box(x1, y1, x2, y2, 0xFF00FF00)
		elseif (address == 0xFFDEBA) then
			box(x1, y1, x2, y2, 0xFF00FFFF)
			text(FitX(x1, hp) + 2, FitY(y1) + 1, hp, 0xFFFF00FF)
		--	if (x2 <    0) then text(x1 + 2, y2 - 7, "x:" .. x1      ) end
		--	if (x1 >= 320) then text(x1 + 2, y2 - 7, "x:" .. x1 - 320) end
		--	if (y2 <    0) then text(x2 + 2, y2 - 7, "y:" .. y2      ) end
			local offtext = ""
			if (x2 <    0) then offtext = offtext .. "x:" .. x1       end
			if (x1 >= 320) then offtext = offtext .. "x:" .. x1 - 320 end
			if (y2 <    0) then offtext = offtext .. "y:" .. y2       end
			if (y2 >= 224) then offtext = offtext .. "y:" .. y2 - 224 end
			if offtext ~= "" then
				text(FitX(x1, offtext), FitY(y1) - 7, offtext)
			end
		end
		base = rw(base + 2)
		i = i + 1
		if (i > 400) then break end
	end
end

function Objects()
	if rb(0xFFFFF6) ~= 50 then return end
	Enemies = 0
	Items   = 0
	GetCam()
	local base = 0xFFAD54
	for i=0,100 do
		local link = rw (base+   6)
		local ptr1 = rw (base+0x0A)+0xFF0000
		local x    = rws(base+0x3E)
		local xsub = rb (base+0x40)
		local y    = rws(base+0x42)
		local ysub = rb (base+0x44)
		local hp   = rw (base+0x52)
		local ptr2 = rl (ptr1+0x2A)
		local code = rw (ptr2)
		if base > 0 then
			if ptr2 == 0x27DEE -- helicopter black
			or ptr2 == 0x27F9C -- helicopter red
			or ptr2 == 0x2804E -- plane black
			or ptr2 == 0x28134 -- helicopter green
			or ptr2 == 0x282B8 -- plane red
			or ptr2 == 0x2860A -- missile
			or ptr2 == 0x28DD2 -- helicopter red phase 1
			or ptr2 == 0x28E08 -- helicopter red phase 2
			then
				Enemies = Enemies + 1
			elseif ptr2 == 0x13326
			or     ptr2 == 0x13BDE
			then
				Items = Items + 1
			else
			--	text(x - xcam, y - ycam, string.format("%X", ptr2), "green")
			end
		end
		base = link + 0xFF0000
		local a5   = rl (base)
		if a5 == 0x88BE then return end
	end
end

function Spawns()
	if rb(0xFFFFF6) ~= 50 then return end
	local base = getr("M68K A6") & 0xFFFFFF
	local ptr1 = rw(base+0x0A) + 0xFF0000
	local ptr2 = rl(ptr1+0x2A)
	local code = rw(ptr2)
	SpawnX = rws(base+0x3E) - xcam
	SpawnY = rws(base+0x42) - ycam
	if  code ~= 0xAE6  -- drone
	and code ~= 0xAF2  -- mini-missile
	and code ~= 0x2384 -- item
	then
		SpawnOpac  = 192
		SpawnCount = SpawnCount + 1
	end
end

function PredictItem(rng)
	rng = rng & 0xFFFF
	local a2 = 0x29B78
	local d0 = 0
	local carry = rw(a2) > rng
	a2 = a2 + 2
	if carry then
		a2 = a2 + 2
		while rw(a2) > rng do
			a2 = a2 + 4
		end
		a2 = a2 + 2
	end
	a2 = a2 + rw(a2) + 2
	if a2 == 0x29BBA then a2 = 0 end
	return rw(a2) & 0xFF
end

function RNGroll(seed1, seed2)                     -- subroutine $995C
	local d0 = seed1                                           
	local d1 = seed2                                           
	d1 =  (d1           <<  1) | (d1        >> 31) -- ROL.L   #1,D1
	d1 = ((d1 & 0xFFFF) ~  d0) | (d1 & 0xFFFF0000) -- EOR.W   D0,D1
	d1 = ((d1 & 0xFFFF) << 16) | (d1        >> 16) -- SWAP.W  D1
	d0 =  (d1 ~ d0)  & 0xFFFF                      -- EOR.W   D1,D0
	d0 = ((d0 << 1)  & 0xFFFF) | ((d0 << 1) >> 16) -- ROL.W   #1,D0
	d1 = ((d1 & 0xFFFF) ~  d0) | (d1 & 0xFFFF0000) -- EOR.W   D0,D1
	d1 = ((d1 & 0xFFFF) << 16) | (d1        >> 16) -- SWAP.W  D1
	d0 = ((d1 & 0xFFFF) ~  d0)                     -- EOR.W   D1,D0
	return {(d0 & 0xFFFF), d1}
end

function ItemPrediction()
	local RNG1 = rw (0xFFF5FC)
	local RNG2 = rl (0xFFF5FE)
	local RNG  = RNGroll(RNG1, RNG2)
	local RNG  = RNGroll(RNG[1], RNG[2])
	local item = PredictItem(RNG[1])
	gui.text(0, 70, string.format("%2X", item),"yellow")
end

function Main()
	local color0     = "yellow"
	local color1     = "yellow"
	local color2     = "yellow"
	local base1      = 0xFFAD54
	local base2      = 0xFFADAE
--	local hp1        = rw (0xFFF654)
--	local life1      = rw (0xFFF644)
	local X1         = rw (base1 + 0x3E)
	local X1sub      = rb (base1 + 0x40)
	local Y1         = rws(base1 + 0x42)
	local Y1sub      = rb (base1 + 0x44)
	local X2         = rw (base2 + 0x3E)
	local X2sub      = rb (base2 + 0x40)
	local Y2         = rws(base2 + 0x42)
	local Y2sub      = rb (base2 + 0x44)
	local RNG1       = rw (0xFFF5FC)
--	local RNG2       = rl (0xFFF5FE)
	local Weapon1    = rb (0xFFF67B)
	local Weapon2    = rb (0xFFF6BB)
	local Charge1    = (rw(0xFFF658) - 0x2800) / -0x80
	local Charge2    = (rw(0xFFF698) - 0x2800) / -0x80
	local ScreenLock = rw(0xFFDFC0)
	if Charge1 <= 0 then Charge1 = 0; color1 = "red" end
	if Charge2 <= 0 then Charge2 = 0; color2 = "red" end
	if RNGcount > 1 then              color0 = "red" end
	HandleMsgTable()
	PlayerPos()
	Objects()
	if rb(0xFFFFF6) == 50 then
		line( 34,  42, SpawnX, SpawnY, 0x00FF00 + SpawnOpac << 24)
		text(  4,  35, string.format("Obj: %d", SpawnCount),"green")
		text(  4,  43, string.format("%d %d %d", Enemies, Items, Hearts/2))
	end
	text( 80,  22, string.format("%2.0f"  , Charge1),    color1)
--	text(235,  20, string.format("%2.0f"  , Charge2),    color2)
	text(  1, 217, string.format("RNG:%X" , RNG1))
	text( 40, 217, string.format("Lock:%d", ScreenLock))
	text( 34, 217, string.format("%d"     , RNGcount),   color0)
	text(180, 217, string.format("%2d"    , Weapon1+1), "yellow")
--	text(300, 217, string.format("%2d"    , Weapon2+1), "yellow")
	text( 81, 210, string.format("Pos: %d.%d\nSpd: %.5f", X1, X1sub, p1speedx))
	text(137, 210, string.format("/ %d.%d\n/ %.5f"      , Y1, Y1sub, p1speedy))
--	text(203, 210, string.format("Pos: %d.%d\nSpd: %.5f", X2, X2sub, p2speedx), 0xFF00FF00)
--	text(260, 210, string.format("/ %d.%d\n/ %.5f"      , Y2, Y2sub, p2speedy), 0xFF00FF00)
	Hitbox(0xFFDEB2)
	Hitbox(0xFFDEBA)
	RNGcount  = 0

	if rb(0xFF4633) == 5 then	
		text(143,3,string.format("     %2d", LIFT), 0xFFFF00FF, clear) 
	end
	
	DER =  rw(0xFFAEA0)
	text(135,3,string.format("     %2d", DER), 0xFFFF00FF, clear)
	
--	emu.frameadvance()
--	gui.clearGraphics()
end

event.onframeend(function()
	SpawnOpac = SpawnOpac - 4
	if SpawnOpac < 0 then SpawnOpac = 0 end
	Main()
end)

event.onframestart(function()
	Hearts = 0
	ItemPrediction()
end)

event.onloadstate(function()
	SpawnCount = 0
	SpawnOpac  = 192
	Enemies    = 0
	Items      = 0
	Hearts     = 0
	return HandleMsgTable(1)
end)

rex(function() DamageHitbox = false end   , 0x375A, "DamageHitbox-")
rex(function() DamageHitbox = true  end   , 0x375E, "DamageHitbox+")
rex(function() DamageHitbox = false end   , 0x3768, "DamageHitbox-")
rex(function() DamageHitbox = true  end   , 0x376C, "DamageHitbox+")
rex(function() DamageHitbox = false end   , 0x65C4, "DamageHitbox-")
rex(function() DamageHitbox = true  end   , 0x65C8, "DamageHitbox+")
rex(function() RNGcount = RNGcount + 1 end, 0x995C,    "RNGcount++")
rex(Item                                  , 0x4738,          "Item")
rex(Item                                  , 0x4534,          "Item")
rex(Spawns                                , 0x8DE6,        "Spawns")
rex(Spawns                                , 0x8DCE,        "Spawns")
rex(Collision                             , 0x8C9A,     "Collision")
rex(HandleDamage                          , 0x1085A, "MeeleeDamage")
rex(HandleDamage                          , 0x10CBA, "WeaponDamage")
rex(HandleDamage                          , 0x10CC4, "WeaponDamage")