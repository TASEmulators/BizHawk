-- feos, 2025

-- CONSTANTS
local NULL_OBJECT      = 0x88888888 -- no object at that index
local OUT_OF_BOUNDS    = 0xFFFFFFFF -- no such index
local MINIMAL_ZOOM     = 0.0001     -- ???
local ZOOM_FACTOR      = 0.02
local PAN_FACTOR       = 10
local CHAR_WIDTH       = 10
local CHAR_HEIGHT      = 16
local NEGATIVE_MAXIMUM = 1 << 63
local POSITIVE_MAXIMUM = ~NEGATIVE_MAXIMUM
local MAX_PLAYERS      = 4
-- sizes in bytes
local LINE_SIZE = 256 -- sizeof(line_t) is 232, but we padded it for niceness
local MOBJ_SIZE = 512 -- sizeof(mobj_t) is 464, but we padded it for niceness
local PLAYER_SIZE = 1024 -- sizeof(player_t) is 729, but we padded it for niceness
-- shortcuts
local rl     = memory.read_u32_le
local rw     = memory.read_u16_le
local rb     = memory.read_u8
local rls    = memory.read_s32_le
local rws    = memory.read_s16_le
local rbs    = memory.read_s8
local text   = gui.text
local box    = gui.drawBox
local line   = gui.drawLine
--local text = gui.pixelText -- INSANELY SLOW

-- TOP LEVEL VARIABLES
local Zoom     = 1
local Init     = true
-- tables
-- view offset
local Pan = {
	x = 0,
	y = 0
}
-- object positions bounds
local OB = {
	top    = POSITIVE_MAXIMUM,
	left   = POSITIVE_MAXIMUM,
	bottom = NEGATIVE_MAXIMUM,
	right  = NEGATIVE_MAXIMUM
}
local LastScreenSize = {
	w = client.screenwidth(),
	h = client.screenheight()
}
-- forward declarations
local PlayerOffsets = {}-- player member offsets in bytes
local MobjOffsets   = {} -- mobj member offsets in bytes
local MobjType      = {}
local SpriteNumber  = {}
local Objects       = {}

--gui.defaultPixelFont("fceux")
gui.use_surface("client")

local function mapify_x(coord)
	return math.floor(((coord / 0xffff)+Pan.x)*Zoom)
end

local function mapify_y(coord)
	return math.floor(((coord / 0xffff)+Pan.y)*Zoom)
end

local function in_range(var, minimum, maximum)
	return var >= minimum and var <= maximum
end

local function reset_view()
	Init = true
	update_zoom()
end

local function zoom_out()
	local newZoom = Zoom * (1 - ZOOM_FACTOR)
	if newZoom < MINIMAL_ZOOM then return end
	Zoom = newZoom
end

local function zoom_in()
	Zoom = Zoom * (1 + ZOOM_FACTOR)
end

local function pan_left()
	Pan.x = Pan.x + PAN_FACTOR/Zoom/2
end

local function pan_right()
	Pan.x = Pan.x - PAN_FACTOR/Zoom/2
end

local function pan_up()
	Pan.y = Pan.y + PAN_FACTOR/Zoom/2
end

local function pan_down()
	Pan.y = Pan.y - PAN_FACTOR/Zoom/2
end

function maybe_swap(left, right)
	if left > right then
		local smallest = right
		right = left
		left = smallest
	end
end

local function get_line_count(str)
	local lines = 1
	local longest = 0
	local size = 0
	for i = 1, #str do
		local c = str:sub(i, i)
		if c == '\n' then
			lines = lines + 1
			if size > longest then
				longest = size
				size = -1
			end
		end
		size = size + 1
	end
	if size > longest then longest = size end
	return lines, longest
end

local function iterate_players()
	local playercount = 0
	local total_killcount = 0
	local total_itemcount = 0
	local total_secretcount = 0

	local stats = "    Heal Armr Kill Item Secr\n"
	for i = 1, MAX_PLAYERS do
		local addr = PLAYER_SIZE * (i - 1)
		local mobj = rl(addr + PlayerOffsets.mobj, "Players")

		if mobj ~= NULL_OBJECT then
			playercount = playercount + 1
			local health = rls(addr + PlayerOffsets.health, "Players")
			local armor = rls(addr + PlayerOffsets.armorpoints1, "Players")
			local killcount = rls(addr + PlayerOffsets.killcount, "Players")
			local itemcount = rls(addr + PlayerOffsets.itemcount, "Players")
			local secretcount = rls(addr + PlayerOffsets.secretcount, "Players")

			total_killcount = total_killcount + killcount
			total_itemcount = total_itemcount + itemcount
			total_secretcount = total_secretcount + secretcount

			stats = string.format("%s P%i %4i %4i %4i %4i %4i\n", stats, i, health, armor, killcount, itemcount, secretcount)
		end
	end
	if playercount > 1 then
		stats = string.format("%s %-12s %4i %4i %4i\n", stats, "All", total_killcount, total_itemcount, total_secretcount)
	end
	gui.text(0, 0, stats, nil, "topright")
end

local function iterate()
	if Init then return end
	
	for _, addr in ipairs(Objects) do
		local x      = rls(addr + MobjOffsets.x, "Things")
		local y      = rls(addr + MobjOffsets.y, "Things") * -1
		local health = rls(addr + MobjOffsets.health, "Things")
		local radius = math.floor((rls(addr + MobjOffsets.radius, "Things") >> 16) * Zoom)
		local sprite = SpriteNumber[rls(addr + MobjOffsets.sprite, "Things")]
		local type   = rl(addr + MobjOffsets.type, "Things")
		local pos    = { x = mapify_x(x), y = mapify_y(y) }
		local color  = "white"
			
		if type == 0
		then type = "PLAYER"
		else type = MobjType[type]
		end
		if health <= 0 then color = "red" end
		--[[--
		local z      = rls(addr + Offsets.z) / 0xffff
		local index  = rl (addr + Offsets.index)
		local tics   = rl (addr + Offsets.tics)
		--]]--
		if  in_range(pos.x, 0, client.screenwidth())
		and in_range(pos.y, 0, client.screenheight())
		then
			text(pos.x, pos.y, string.format("%s", type), color)
			box(pos.x - radius, pos.y - radius, pos.x + radius, pos.y + radius, color)
		end
	end
	
	for i = 0, 100000 do
		local addr = i * LINE_SIZE
		if addr > 0xFFFFFF then break end
		
		local id = rl(addr, "Lines") & 0xFFFFFFFF
		if id == OUT_OF_BOUNDS then break end
		
		if id ~= NULL_OBJECT then
			local vertices_offset = 0xe8
			local v1 = { x =  rls(addr+vertices_offset   , "Lines"),
						 y = -rls(addr+vertices_offset+ 4, "Lines") }
			local v2 = { x =  rls(addr+vertices_offset+ 8, "Lines"),
						 y = -rls(addr+vertices_offset+12, "Lines") }
			line(
				mapify_x(v1.x),
				mapify_y(v1.y),
				mapify_x(v2.x),
				mapify_y(v2.y),
				0xffcccccc)
		end
	end
end

local function init_objects()
	for i = 0, 100000 do
		local addr = i * MOBJ_SIZE
		if addr > 0xFFFFFF then break end
		
		local thinker = rl(addr, "Things") & 0xFFFFFFFF -- just to check if mobj is there
		if thinker == OUT_OF_BOUNDS then break end
		
		if thinker ~= NULL_OBJECT then
			local x    = rls(addr + MobjOffsets.x, "Things") / 0xffff
			local y    = rls(addr + MobjOffsets.y, "Things") / 0xffff * -1
			local type = rl (addr + MobjOffsets.type, "Things")
			
			if type == 0
			then type = "PLAYER"
			else type = MobjType[type]
			end
		--	print(string.format("%d %f %f %02X", index, x, y, type))
			if type
			and not string.find(type, "MISC")
			then
				if x < OB.left   then OB.left   = x end
				if x > OB.right  then OB.right  = x end
				if y < OB.top    then OB.top    = y end
				if y > OB.bottom then OB.bottom = y end
				-- cache the Objects we need
				table.insert(Objects, addr)
			end
		end
	end
end

function update_zoom()
	if not Init
	and LastScreenSize.w == client.screenwidth()
	and LastScreenSize.h == client.screenheight()
	then return end
	
	if  OB.top    ~= POSITIVE_MAXIMUM
	and OB.left   ~= POSITIVE_MAXIMUM
	and OB.right  ~= NEGATIVE_MAXIMUM
	and OB.bottom ~= NEGATIVE_MAXIMUM
	and not emu.islagged()
	then
		maybe_swap(OB.right, OB.left)
		maybe_swap(OB.top,   OB.bottom)
		local span        = { x = OB.right-OB.left+200,        y = OB.bottom-OB.top+200         }
		local scale       = { x = client.screenwidth()/span.x, y = client.screenheight()/span.y }
		local spanCenter  = { x = OB.left+span.x/2, y = OB.top+span.y/2 }
		      Zoom        = math.min(scale.x, scale.y)
		local sreenCenter = { x = client.screenwidth()/Zoom/2, y = client.screenheight()/Zoom/2 }
		      Pan.x       = -math.floor(spanCenter.x - sreenCenter.x)
		      Pan.y       = -math.floor(spanCenter.y - sreenCenter.y)
		      Init        = false
	end
end

local function make_button(x, y, name, func)
	local boxWidth   = CHAR_WIDTH
	local boxHeight  = CHAR_HEIGHT
	local lineCount, longest = get_line_count(name)
	local textWidth  = longest  *CHAR_WIDTH
	local textHeight = lineCount*CHAR_HEIGHT
	local colors     = { 0x66bbddff, 0xaabbddff, 0xaa88aaff }
	local colorIndex = 1
	
	if textWidth  + 10 > boxWidth  then boxWidth  = textWidth  + 10 end
	if textHeight + 10 > boxHeight then boxHeight = textHeight + 10 end
	
	local textX    = x + boxWidth /2 - textWidth /2
	local textY    = y + boxHeight/2 - textHeight/2 - boxHeight
	local mouse    = input.getmouse()
	local mousePos = client.transformPoint(mouse.X, mouse.Y)
	
	if  in_range(mousePos.x, x,           x+boxWidth)
	and in_range(mousePos.y, y-boxHeight, y         ) then
		if mouse.Left then
			colorIndex = 3
			func()
		else colorIndex = 2 end
	end
	
	box(x, y, x+boxWidth, y-boxHeight, 0xaaffffff, colors[colorIndex])
	text(textX, textY, name, colors[colorIndex] | 0xff000000) -- full alpha
end

local function struct_layout(struct)
	struct = struct or {}
	struct.size = 0
	struct.offsets = {}

	function struct.add(name, size, alignment)
		if alignment == true then alignment = size end
		struct.align(alignment)
		--print(string.format("%-19s %3X %3X", name, size, struct.size)); emu.yield()
		struct.offsets[name] = struct.size
		struct.size = struct.size + size
		struct.align(alignment) -- add padding to structs
		return struct
	end
	function struct.align(alignment)
		if alignment and struct.size % alignment > 0 then
			--print(string.format("%i bytes padding", alignment - (struct.size % alignment)))
			struct.pad(alignment - (struct.size % alignment))
		end
	end
	function struct.pad(size)
		struct.size = struct.size + size
		return struct
	 end
	function struct.s8(name) return struct.add(name, 1, true) end
	function struct.s16(name) return struct.add(name, 2, true) end
	function struct.s32(name) return struct.add(name, 4, true) end
	function struct.u8(name) return struct.add(name, 1, true) end
	function struct.u32(name) return struct.add(name, 4, true) end
	function struct.u64(name) return struct.add(name, 8, true) end
	function struct.ptr(name) return struct.u64(name) end
	function struct.bool(name) return struct.s32(name) end
	function struct.array(name, type, count, ...)
		for i = 1, count do
			struct[type](name .. i, ...)
		end
		return struct
	end

	return struct
end

--[[
player_t https://github.com/TASEmulators/dsda-doom/blob/master/prboom2/src/d_player.h
mobj              8   0
playerstate       4   8
cmd               E   C
viewz             4  1C
viewheight        4  20
deltaviewheight   4  24
bob               4  28
health            4  2C
armorpoints1      4  30
armorpoints2      4  34
armorpoints3      4  38
armorpoints4      4  3C
armortype         4  40
powers1           4  44
powers2           4  48
powers3           4  4C
powers4           4  50
powers5           4  54
powers6           4  58
powers7           4  5C
powers8           4  60
powers9           4  64
powers10          4  68
powers11          4  6C
powers12          4  70
cards1            4  74
cards2            4  78
cards3            4  7C
cards4            4  80
cards5            4  84
cards6            4  88
cards7            4  8C
cards8            4  90
cards9            4  94
cards10           4  98
cards11           4  9C
backpack          4  A0
frags1            4  A4
frags2            4  A8
frags3            4  AC
frags4            4  B0
frags5            4  B4
frags6            4  B8
frags7            4  BC
frags8            4  C0
readyweapon       4  C4
pendingweapon     4  C8
weaponowned1      4  CC
weaponowned2      4  D0
weaponowned3      4  D4
weaponowned4      4  D8
weaponowned5      4  DC
weaponowned6      4  E0
weaponowned7      4  E4
weaponowned8      4  E8
weaponowned9      4  EC
ammo1             4  F0
ammo2             4  F4
ammo3             4  F8
ammo4             4  FC
ammo5             4 100
ammo6             4 104
maxammo1          4 108
maxammo2          4 10C
maxammo3          4 110
maxammo4          4 114
maxammo5          4 118
maxammo6          4 11C
attackdown        4 120
usedown           4 124
cheats            4 128
refire            4 12C
killcount         4 130
itemcount         4 134
secretcount       4 138
damagecount       4 13C
bonuscount        4 140
attacker          8 148
extralight        4 150
fixedcolormap     4 154
colormap          4 158
psprites         30 160
didsecret         4 190
momx              4 194
mony              4 198
maxkilldiscount   4 19C
prev_viewz        4 1A0
prev_viewangle    4 1A4
prev_viewpitch    4 1A8
(padding omitted)
]]

PlayerOffsets = struct_layout()
	.ptr  ("mobj")
	.s32  ("playerstate")
	.add  ("cmd", 14, 2)
	.s32  ("viewz")
	.s32  ("viewheight")
	.s32  ("deltaviewheight")
	.s32  ("bob")
	.s32  ("health")
	.array("armorpoints", "s32", 4)
	.s32  ("armortype")
	.array("powers", "s32", 12)
	.array("cards", "bool", 11)
	.bool ("backpack")
	.array("frags", "s32", 8)
	.s32  ("readyweapon")
	.s32  ("pendingweapon")
	.array("weaponowned", "bool", 9)
	.array("ammo", "s32", 6)
	.array("maxammo", "s32", 6)
	.s32  ("attackdown")
	.s32  ("usedown")
	.s32  ("cheats")
	.s32  ("refire")
	.s32  ("killcount")
	.s32  ("itemcount")
	.s32  ("secretcount")
	.s32  ("damagecount")
	.s32  ("bonuscount")
	.ptr  ("attacker")
	.s32  ("extralight")
	.s32  ("fixedcolormap")
	.s32  ("colormap")
	.add  ("psprites", 24*2, 8)
	.bool ("didsecret")
	.s32  ("momx")
	.s32  ("mony")
	.s32  ("maxkilldiscount")
	.s32  ("prev_viewz")
	.u32  ("prev_viewangle")
	.u32  ("prev_viewpitch")
	-- the rest are non-doom
	.offsets

--[[
mobj_t https://github.com/TASEmulators/dsda-doom/blob/master/prboom2/src/p_mobj.h
thinker              2C   0
x                     4  30
y                     4  34
z                     4  38
snext                 8  40
sprev                 8  48
angle                 4  50
sprite                4  54
frame                 4  58
bnext                 8  60
bprev                 8  68
subsector             8  70
floorz                4  78
ceilingz              4  7C
dropoffz              4  80
radius                4  84
height                4  88
momx                  4  8C
momy                  4  90
momz                  4  94
validcount            4  98
type                  4  9C
info                  8  A0
tics                  4  A8
state                 8  B0
flags                 8  B8
intflags              4  C0
health                4  C4
movedir               2  C8
movecount             2  CA
strafecount           2  CC
target                8  D0
reactiontime          2  D8
threshold             2  DA
pursuecount           2  DC
gear                  2  DE
player                8  E0
lastlook              2  E8
spawnpoint           3A  EC
tracer                8 128
lastenemy             8 130
friction              4 138
movefactor            4 13C
touching_sectorlist   8 140
PrevX                 4 148
PrevY                 4 14C
PrevZ                 4 150
pitch                 4 154
index                 4 158
patch_width           2 15C
iden_nums             4 160
(padding omitted)
--]]--

MobjOffsets = struct_layout()
	.add("thinker", 44, 8)
	.s32("x")
	.s32("y")
	.s32("z")
	.ptr("snext")
	.ptr("sprev")
	.u32("angle")
	.s32("sprite")
	.s32("frame")
	.ptr("bnext")
	.ptr("bprev")
	.ptr("subsector")
	.s32("floorz")
	.s32("ceilingz")
	.s32("dropoffz")
	.s32("radius")
	.s32("height")
	.s32("momx")
	.s32("momy")
	.s32("momz")
	.s32("validcount")
	.s32("type")
	.ptr("info")
	.s32("tics")
	.ptr("state")
	.u64("flags")
	.s32("intflags")
	.s32("health")
	.s16("movedir")
	.s16("movecount")
	.s16("strafecount")
	.ptr("target")
	.s16("reactiontime")
	.s16("threshold")
	.s16("pursuecount")
	.s16("gear")
	.ptr("player")
	.s16("lastlook")
	.add("spawnpoint", 58, 4)
	.ptr("tracer")
	.ptr("lastenemy")
	.s32("friction")
	.s32("movefactor")
	.ptr("touching_sectorlist")
	.s32("PrevX")
	.s32("PrevY")
	.s32("PrevZ")
	.u32("pitch")
	.s32("index")
	.s16("patch_width")
	.s32("iden_nums")
	-- the rest are non-doom
	.offsets

MobjType = {
--	"NULL" = -1,
--	"ZERO",
--	"PLAYER = ZERO",
	"POSSESSED",
	"SHOTGUY",
	"VILE",
	"FIRE",
	"UNDEAD",
	"TRACER",
	"SMOKE",
	"FATSO",
	"FATSHOT",
	"CHAINGUY",
	"TROOP",
	"SERGEANT",
	"SHADOWS",
	"HEAD",
	"BRUISER",
	"BRUISERSHOT",
	"KNIGHT",
	"SKULL",
	"SPIDER",
	"BABY",
	"CYBORG",
	"PAIN",
	"WOLFSS",
	"KEEN",
	"BOSSBRAIN",
	"BOSSSPIT",
	"BOSSTARGET",
	"SPAWNSHOT",
	"SPAWNFIRE",
	"BARREL",
	"TROOPSHOT",
	"HEADSHOT",
	"ROCKET",
	"PLASMA",
	"BFG",
	"ARACHPLAZ",
	"PUFF",
	"BLOOD",
	"TFOG",
	"IFOG",
	"TELEPORTMAN",
	"EXTRABFG",
	"MISC0",
	"MISC1",
	"MISC2",
	"MISC3",
	"MISC4",
	"MISC5",
	"MISC6",
	"MISC7",
	"MISC8",
	"MISC9",
	"MISC10",
	"MISC11",
	"MISC12",
	"INV",
	"MISC13",
	"INS",
	"MISC14",
	"MISC15",
	"MISC16",
	"MEGA",
	"CLIP",
	"MISC17",
	"MISC18",
	"MISC19",
	"MISC20",
	"MISC21",
	"MISC22",
	"MISC23",
	"MISC24",
	"MISC25",
	"CHAINGUN",
	"MISC26",
	"MISC27",
	"MISC28",
	"SHOTGUN",
	"SUPERSHOTGUN",
	"MISC29",
	"MISC30",
	"MISC31",
	"MISC32",
	"MISC33",
	"MISC34",
	"MISC35",
	"MISC36",
	"MISC37",
	"MISC38",
	"MISC39",
	"MISC40",
	"MISC41",
	"MISC42",
	"MISC43",
	"MISC44",
	"MISC45",
	"MISC46",
	"MISC47",
	"MISC48",
	"MISC49",
	"MISC50",
	"MISC51",
	"MISC52",
	"MISC53",
	"MISC54",
	"MISC55",
	"MISC56",
	"MISC57",
	"MISC58",
	"MISC59",
	"MISC60",
	"MISC61",
	"MISC62",
	"MISC63",
	"MISC64",
	"MISC65",
	"MISC66",
	"MISC67",
	"MISC68",
	"MISC69",
	"MISC70",
	"MISC71",
	"MISC72",
	"MISC73",
	"MISC74",
	"MISC75",
	"MISC76",
	"MISC77",
	"MISC78",
	"MISC79",
	"MISC80",
	"MISC81",
	"MISC82",
	"MISC83",
	"MISC84",
	"MISC85",
	"MISC86",
	"PUSH",
	"PULL",
	"DOGS",
	"PLASMA1",
	"PLASMA2"
}

SpriteNumber = {
--	"TROO",
	"SHTG",
	"PUNG",
	"PISG",
	"PISF",
	"SHTF",
	"SHT2",
	"CHGG",
	"CHGF",
	"MISG",
	"MISF",
	"SAWG",
	"PLSG",
	"PLSF",
	"BFGG",
	"BFGF",
	"BLUD",
	"PUFF",
	"BAL1",
	"BAL2",
	"PLSS",
	"PLSE",
	"MISL",
	"BFS1",
	"BFE1",
	"BFE2",
	"TFOG",
	"IFOG",
	"PLAY",
	"POSS",
	"SPOS",
	"VILE",
	"FIRE",
	"FATB",
	"FBXP",
	"SKEL",
	"MANF",
	"FATT",
	"CPOS",
	"SARG",
	"HEAD",
	"BAL7",
	"BOSS",
	"BOS2",
	"SKUL",
	"SPID",
	"BSPI",
	"APLS",
	"APBX",
	"CYBR",
	"PAIN",
	"SSWV",
	"KEEN",
	"BBRN",
	"BOSF",
	"ARM1",
	"ARM2",
	"BAR1",
	"BEXP",
	"FCAN",
	"BON1",
	"BON2",
	"BKEY",
	"RKEY",
	"YKEY",
	"BSKU",
	"RSKU",
	"YSKU",
	"STIM",
	"MEDI",
	"SOUL",
	"PINV",
	"PSTR",
	"PINS",
	"MEGA",
	"SUIT",
	"PMAP",
	"PVIS",
	"CLIP",
	"AMMO",
	"ROCK",
	"BROK",
	"CELL",
	"CELP",
	"SHEL",
	"SBOX",
	"BPAK",
	"BFUG",
	"MGUN",
	"CSAW",
	"LAUN",
	"PLAS",
	"SHOT",
	"SGN2",
	"COLU",
	"SMT2",
	"GOR1",
	"POL2",
	"POL5",
	"POL4",
	"POL3",
	"POL1",
	"POL6",
	"GOR2",
	"GOR3",
	"GOR4",
	"GOR5",
	"SMIT",
	"COL1",
	"COL2",
	"COL3",
	"COL4",
	"CAND",
	"CBRA",
	"COL6",
	"TRE1",
	"TRE2",
	"ELEC",
	"CEYE",
	"FSKU",
	"COL5",
	"TBLU",
	"TGRN",
	"TRED",
	"SMBT",
	"SMGT",
	"SMRT",
	"HDB1",
	"HDB2",
	"HDB3",
	"HDB4",
	"HDB5",
	"HDB6",
	"POB1",
	"POB2",
	"BRS1",
	"TLMP",
	"TLP2",
	"TNT1",
	"DOGS",
	"PLS1",
	"PLS2",
	"BON3",
	"BON4",
	"BLD2"
}

while true do
	if Init then init_objects() end
	iterate()	
	update_zoom()
	make_button( 10, client.screenheight()-70, "Zoom\nIn",    zoom_in   )
	make_button( 10, client.screenheight()-10, "Zoom\nOut",   zoom_out  )
	make_button( 80, client.screenheight()-40, "Pan\nLeft",   pan_left  )
	make_button(150, client.screenheight()-70, "Pan \nUp",    pan_up    )
	make_button(150, client.screenheight()-10, "Pan\nDown",   pan_down  )
	make_button(220, client.screenheight()-40, "Pan\nRight",  pan_right )
	make_button(300, client.screenheight()-10, "Reset\nView", reset_view)
	text(10, client.screenheight()-170, string.format(
		"Zoom: %.4f\nPanX: %s\nPanY: %s", 
		Zoom, Pan.x, Pan.y), 0xffbbddff)
	LastScreenSize.w = client.screenwidth()
	LastScreenSize.h = client.screenheight()
	emu.frameadvance()
end