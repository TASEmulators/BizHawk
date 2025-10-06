-- Core must be loaded to get memory domain sizes
-- Throw an error so Lua doesn't cache the module in an invalid state
assert(emu.getsystemid() == "Doom", "Doom core not loaded")

local dsda = {
	doom = {},
	heretic = {},
	hexen = {},
}

 -- Read the high word of every pointer and check that it matches the expected base address (for debugging)
dsda.check_pointers = false



-- Constants ---
local NULL_OBJECT <const>    = 0x88888888 -- no object at that index
local OUT_OF_BOUNDS <const>  = 0xFFFFFFFF -- no such index
local WBX_POINTER_HI <const> = 0x36F
local BusDomain <const>      = "System Bus"

dsda.MAX_PLAYERS = 4



-- Locals
local read_u8   = memory.read_u8
local read_u24  = memory.read_u24_le
local read_u32  = memory.read_u32_le
local readfloat = memory.readfloat

-- Utilities ---

local function assertf(condition, format, ...)
	if not condition then
		error(string.format(format, ...), 2)
	end
end

function dsda.read_s64_le(addr, domain)
	return read_u32(addr, domain) | read_u32(addr + 4, domain) << 32
end

-- Returns the lower 4 bytes of an 8 byte pointer
function dsda.read_ptr(addr, domain)
	if dsda.check_pointers and read_u32(addr + 4, domain) ~= WBX_POINTER_HI then
		error(string.format("Invalid pointer 0x%016X at %s 0x%X", dsda.read_s64_le(addr, domain), domain, addr))
	end
	return read_u32(addr, domain)
end

function dsda.read_bool(addr, domain)
	return read_u32(addr, domain) ~= 0
end

local function read_float_le(addr, domain)
	return readfloat(addr, false, domain)
end



-- Structs ---

function dsda.struct_layout(struct, padded_size, domain, max_count)
	struct = struct or {}
	struct.padded_size = padded_size
	struct.domain = domain
	struct.size = 0
	struct.alignment = 1
	struct.offsets = {}
	struct.items = {} -- This should be iterated with pairs()

	assert((padded_size ~= nil) == (domain ~= nil), "padded_size and domain must be specified together")

	local max_address
	if domain then
		max_count = max_count or math.floor(memory.getmemorydomainsize(domain) / padded_size)
		max_address = (max_count - 1) * padded_size
		struct.max_count = max_count
		struct.max_address = max_address
	else
		max_address = 0xFFFFFFFF
	end

	local items_meta = {}
	local item_props = {}
	local item_meta = {}
	setmetatable(struct.items, items_meta)

	function struct.add(name, size, alignment, read_func)
		assertf(struct.offsets[name] == nil, "Duplicate %s name %s", domain, name)

		if alignment == true then alignment = size end
		struct.align(alignment)
		local offset = struct.size
		struct.offsets[name] = offset
		struct.size = offset + size
		struct.align(alignment) -- add padding to structs
		--print(string.format("%-19s %3X %3X", name, size, offset));

		if read_func then
			item_props[name] = function(self)
				return read_func(self._address + offset, self._domain)
			end
		end
		return struct
	end
	function struct.align(alignment)
		struct.alignment = math.max(struct.alignment, alignment or 1)
		if alignment and struct.size % alignment > 0 then
			--print(string.format("%i bytes padding", alignment - (struct.size % alignment)))
			struct.pad(alignment - (struct.size % alignment))
		end
		return struct
	end
	function struct.pad(size)
		struct.size = struct.size + size
		return struct
	 end
	function struct.s8   (name) return struct.add(name, 1, true, memory.read_s8) end
	function struct.s16  (name) return struct.add(name, 2, true, memory.read_s16_le) end
	function struct.s32  (name) return struct.add(name, 4, true, memory.read_s32_le) end
	function struct.u8   (name) return struct.add(name, 1, true, memory.read_u8) end
	function struct.u16  (name) return struct.add(name, 2, true, memory.read_u16_le) end
	function struct.u32  (name) return struct.add(name, 4, true, memory.read_u32_le) end
	function struct.s64  (name) return struct.add(name, 8, true, dsda.read_s64_le) end
	function struct.float(name) return struct.add(name, 4, true, read_float_le) end
	function struct.ptr  (name) return struct.s64(name) end
	function struct.bool (name) return struct.add(name, 4, true, dsda.read_bool) end
	function struct.array(name, type, count, ...)
		--console.log("array", type, count, ...)
		for i = 1, count do
			struct[type](name .. i, ...)
		end
		return struct
	end
	function struct.ptrto(name, target_struct)
		local size = struct.size
		struct.ptr(name .. "_ptr")
		struct.size = size
		struct.add(name, 8, true, function(addr, domain)
			local ptr = dsda.read_ptr(addr, domain)
			return target_struct.from_pointer(ptr)
		end)
		return struct
	end
	function struct.done()
		struct.align(struct.alignment)
		return struct
	end

	local function create_item(address, domain)
		local item = {
			_address = address,
			_domain = domain,
		}
		setmetatable(item, item_meta)
		return item
	end
	local function get_item(address)
		assert(domain ~= nil, "Struct can only be accessed by pointer")
		assertf(address >= 0 and address <= max_address and address % padded_size == 0,
			"Invalid %s address %X", domain, address)

		local peek = read_u32(address, domain)
		if peek == NULL_OBJECT then
			--print("NULL_OBJECT", domain, bizstring.hex(address))
			return nil
		elseif peek == OUT_OF_BOUNDS then
			--print("OUT_OF_BOUNDS", domain, bizstring.hex(address))
			return false
		end

		return create_item(address, domain)
	end

	-- iterator for each instance of the struct in the domain
	local function next_item(_, address)
		address = address and address + padded_size or 0
		while address <= max_address do
			local item = get_item(address)
			if item then
				return address, item
			elseif item == false then -- OUT_OF_BOUNDS
				break
			else -- NULL_OBJECT
				address = address + padded_size
			end
		end
	end

	-- iterator for each readable field in the struct
	local function next_item_prop(item, key)
		local next_key = next(item_props, key)
		return next_key, item[next_key]
	end

	-- Get a struct instance from its dedicated memory domain
	function struct.from_address(address)
		return get_item(address) or nil
	end

	-- Get a struct instance from its dedicated memory domain
	function struct.from_index(index)
		return get_item((index - 1) * (padded_size or 0)) or nil
	end

	-- Get a struct instance from the system bus
	function struct.from_pointer(pointer)
		if pointer == 0 then return nil end
		assertf(pointer % struct.alignment == 0, "Unaligned pointer %X", pointer)
		return create_item(pointer, BusDomain)
	end

	function items_meta:__index(index)
		return struct.from_address(index)
	end

	function items_meta:__pairs()
		return next_item
	end

	function item_meta:__index(name)
		local prop = item_props[name]
		if prop then return prop(self) end
	end

	function item_meta:__pairs()
		return next_item_prop, self, nil
	end

	return struct
end

-- mobj_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/p_mobj.h#L277-L413
dsda.mobj = dsda.struct_layout(nil, 512, "Things")

-- player_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/d_player.h#L143-L267
dsda.player = dsda.struct_layout(nil, 1024, "Players", dsda.MAX_PLAYERS)
	.ptrto("mo", dsda.mobj)
	.s32  ("playerstate") -- playerstate_t
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
	.s32  ("readyweapon") -- weapontype_t
	.s32  ("pendingweapon") -- weapontype_t
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
	.ptrto("attacker", dsda.mobj)
	.s32  ("extralight")
	.s32  ("fixedcolormap")
	.s32  ("colormap")
	.add  ("psprites", 24*2, 8) -- pspdef_t[2]
	.bool ("didsecret")
	.s32  ("momx")
	.s32  ("mony")
	.s32  ("maxkilldiscount")
	.s32  ("prev_viewz")
	.u32  ("prev_viewangle")
	.u32  ("prev_viewpitch")
	-- heretic
	.s32  ("flyheight")
	.s32  ("lookdir")
	.bool ("centering")
	.array("inventory", "s32", 33*2) -- inventory_t { int type, int count }
	.s32  ("readyArtifact") -- artitype_t
	.s32  ("artifactCount")
	.s32  ("inventorySlotNum")
	.s32  ("flamecount")
	.s32  ("chickenTics")
	.s32  ("chickenPeck")
	.ptrto("rain1", dsda.mobj)
	.ptrto("rain2", dsda.mobj)
	-- hexen
	.s32  ("pclass") -- pclass_t
	.s32  ("morphTics")
	.s32  ("pieces")
	.s16  ("yellowMessage")
	.s32  ("poisoncount")
	.ptrto("poisoner", dsda.mobj)
	.u32  ("jumpTics")
	.u32  ("worldTimer")
	-- zdoom
	.s32  ("hazardcount")
	.u8   ("hazardinterval")
	.done ()

-- mobj_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/p_mobj.h#L277-L413
dsda.mobj
	.add  ("thinker", 44, 8)
	.s32  ("x")
	.s32  ("y")
	.s32  ("z")
	.ptrto("snext", dsda.mobj)
	.ptr  ("sprev") -- pointer to pointer
	.u32  ("angle")
	.s32  ("sprite") -- spritenum_t
	.s32  ("frame")
	.ptr  ("bnext")
	.ptr  ("bprev")
	.ptr  ("subsector")
	.s32  ("floorz")
	.s32  ("ceilingz")
	.s32  ("dropoffz")
	.s32  ("radius")
	.s32  ("height")
	.s32  ("momx")
	.s32  ("momy")
	.s32  ("momz")
	.s32  ("validcount")
	.s32  ("type") -- mobjtype_t
	.ptr  ("info")
	.s32  ("tics")
	.ptr  ("state")
	.s64  ("flags")
	.s32  ("intflags")
	.s32  ("health")
	.s16  ("movedir")
	.s16  ("movecount")
	.s16  ("strafecount")
	.ptrto("target", dsda.mobj)
	.s16  ("reactiontime")
	.s16  ("threshold")
	.s16  ("pursuecount")
	.s16  ("gear")
	.ptrto("player", dsda.player)
	.s16  ("lastlook")
	.add  ("spawnpoint", 58, 4) -- mapthing_t
	.ptrto("tracer", dsda.mobj)
	.ptrto("lastenemy", dsda.mobj)
	.s32  ("friction")
	.s32  ("movefactor")
	.ptr  ("touching_sectorlist")
	.s32  ("PrevX")
	.s32  ("PrevY")
	.s32  ("PrevZ")
	.u32  ("pitch")
	.s32  ("index")
	.s16  ("patch_width")
	.s32  ("iden_nums")
	-- heretic
	.s32  ("damage")
	.s64  ("flags2")
	.add  ("special1", 16, 8) -- specialval_t
	.add  ("special2", 16, 8) -- specialval_t
	-- hexen
	.s32  ("floorpic")
	.s32  ("floorclip")
	.s32  ("archiveNum")
	.s16  ("tid")
	.s32  ("special")
	.array("special_args", "s32", 5)
	-- zdoom
	.s32  ("gravity")
	.float("alpha")
	-- misc
	.u8   ("color")
	.ptr  ("tranmap")
	.done ()

-- sector_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L124-L213
dsda.sector = dsda.struct_layout(nil, 512, "Sectors")
	.s32  ("iSectorID")
	.u32  ("flags")
	.s32  ("floorheight")
	.s32  ("ceilingheight")
	.u8   ("soundtraversed")
	.ptrto("soundtarget", dsda.mobj)
	.array("blockbox", "s32", 4)
--	.array("bbox", "s32", 4)
	.s32  ("bbox_top")
	.s32  ("bbox_bottom")
	.s32  ("bbox_left")
	.s32  ("bbox_right")
	.add  ("soundorg", 60, 8) -- degenmobj_t;
	.s32  ("validcount")
	.s32  ("gl_validcount")
	.ptrto("thinglist", dsda.mobj) -- start of snext linked list
	.s32  ("friction")
	.s32  ("movefactor")
	.ptr  ("floordata")
	.ptr  ("ceilingdata")
	.ptr  ("lightingdata")
	.s8   ("stairlock")
	.s32  ("prevsec")
	.s32  ("nextsec")
	.s32  ("heightsec")
	.s16  ("bottommap")
	.s16  ("midmapmap")
	.s16  ("topmap")
	.s16  ("colormap")
	.ptr  ("touching_thinglist")
	.s32  ("linecount")
	.ptr  ("lines") -- pointer to pointer
	.s32  ("floorsky")
	.s32  ("ceilingsky")
	.s32  ("floor_xoffs")
	.s32  ("floor_yoffs")
	.s32  ("ceiling_xoffs")
	.s32  ("ceiling_yoffs")
	.s32  ("floorlightsec")
	.s32  ("ceilinglightsec")
	.s16  ("floorpic")
	.s16  ("ceilingpic")
	.s16  ("lightlevel")
	.s16  ("special")
	.s16  ("tag")
	.s32  ("cachedheight")
	.s32  ("scaleindex")
	.s32  ("INTERP_SectorFloor")
	.s32  ("INTERP_SectorCeiling")
	.s32  ("INTERP_FloorPanning")
	.s32  ("INTERP_CeilingPanning")
	.array("fakegroup", "s32", 2)
	-- hexen
	.s32  ("seqType") -- seqtype_t
	-- zdoom
	.s32  ("gravity")
	-- begin damage_t
	.s16  ("damage_amount")
	.u8   ("damage_leakrate")
	.u8   ("damage_interval")
	.align(2)
	-- end damage_t
	.s16  ("lightlevel_floor")
	.s16  ("lightlevel_ceiling")
	.u32  ("floor_rotation")
	.u32  ("ceiling_rotation")
	.s32  ("floor_xscale")
	.s32  ("floor_yscale")
	.s32  ("ceiling_xscale")
	.s32  ("ceiling_yscale")
	.done ()

-- vertex_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/r_defs.h#L70-L80
dsda.vertex = dsda.struct_layout()
	.s32  ("x")
	.s32  ("y")
	.s32  ("px")
	.s32  ("py")
	.done ()

-- line_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L312-L347
dsda.line = dsda.struct_layout(nil, 256, "Lines")
	.s32  ("iLineID")
	.ptrto("v1", dsda.vertex)
	.ptrto("v2", dsda.vertex)
	.s32  ("dx")
	.s32  ("dy")
	.float("texel_length")
	.u32  ("flags")
	.s16  ("special")
	.s16  ("tag")
	.array("sidenum", "u16", 2)
--	.array("bbox", "s32", 4)
	.s32  ("bbox_top")
	.s32  ("bbox_bottom")
	.s32  ("bbox_left")
	.s32  ("bbox_right")
	.s32  ("slopetype") -- slopetype_t
	.ptrto("frontsector", dsda.sector)
	.ptrto("backsector", dsda.sector)
	.s32  ("validcount")
	.s32  ("validcount2")
	.ptr  ("specialdata")
	.s32  ("r_validcount")
	.u8   ("r_flags")
	.add  ("soundorg", 60, 8) -- degenmobj_t;
	-- dsda
	.u8   ("player_activation")
	-- hexen
	.array("special_args", "u32", 5)
	-- zdoom
	.u16  ("activation")
	.u8   ("locknumber")
	.s32  ("automap_style") -- automap_style_t
	.s32  ("health")
	.s32  ("healthgroup")
	.ptr  ("tranmap")
	.float("alpha")
	.done ()

assert(dsda.line.size   == 232, "line.size does not match sizeof(line_t)")
assert(dsda.mobj.size   == 464, "mobj.size does not match sizeof(mobj_t)")
assert(dsda.player.size == 792, "player.size does not match sizeof(player_t)")
assert(dsda.sector.size == 344, "sector.size does not match sizeof(sector_t)")



-- Enums ---

-- Assign (v, k) for every (k, v) so that the enums can be accessed by name, e.g. `mobjtype.PLAYER`
local function assign_keys(table, from, to)
	for i = from or 0, to or math.huge do
		local name = table[i]
		if name ~= nil then
			--assert(table[name] == nil, "duplicate name "..name)
			table[name] = i
		elseif to == nil then
			return
		end
	end
end

-- mobjtype_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/info.h#L5778-L6498
dsda.mobjtype = {
--	[-1] = "NULL",
	[ 0] = "PLAYER",
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
	"PLASMA2",
	"SCEPTRE",
	"BIBLE",
	"MUSICSOURCE",
	"DOOM_NUMMOBJTYPES",
	"HERETIC_MISC0",
	"HERETIC_ITEMSHIELD1",
	"HERETIC_ITEMSHIELD2",
	"HERETIC_MISC1",
	"HERETIC_MISC2",
	"HERETIC_ARTIINVISIBILITY",
	"HERETIC_MISC3",
	"HERETIC_ARTIFLY",
	"HERETIC_ARTIINVULNERABILITY",
	"HERETIC_ARTITOMEOFPOWER",
	"HERETIC_ARTIEGG",
	"HERETIC_EGGFX",
	"HERETIC_ARTISUPERHEAL",
	"HERETIC_MISC4",
	"HERETIC_MISC5",
	"HERETIC_FIREBOMB",
	"HERETIC_ARTITELEPORT",
	"HERETIC_POD",
	"HERETIC_PODGOO",
	"HERETIC_PODGENERATOR",
	"HERETIC_SPLASH",
	"HERETIC_SPLASHBASE",
	"HERETIC_LAVASPLASH",
	"HERETIC_LAVASMOKE",
	"HERETIC_SLUDGECHUNK",
	"HERETIC_SLUDGESPLASH",
	"HERETIC_SKULLHANG70",
	"HERETIC_SKULLHANG60",
	"HERETIC_SKULLHANG45",
	"HERETIC_SKULLHANG35",
	"HERETIC_CHANDELIER",
	"HERETIC_SERPTORCH",
	"HERETIC_SMALLPILLAR",
	"HERETIC_STALAGMITESMALL",
	"HERETIC_STALAGMITELARGE",
	"HERETIC_STALACTITESMALL",
	"HERETIC_STALACTITELARGE",
	"HERETIC_MISC6",
	"HERETIC_BARREL",
	"HERETIC_MISC7",
	"HERETIC_MISC8",
	"HERETIC_MISC9",
	"HERETIC_MISC10",
	"HERETIC_MISC11",
	"HERETIC_KEYGIZMOBLUE",
	"HERETIC_KEYGIZMOGREEN",
	"HERETIC_KEYGIZMOYELLOW",
	"HERETIC_KEYGIZMOFLOAT",
	"HERETIC_MISC12",
	"HERETIC_VOLCANOBLAST",
	"HERETIC_VOLCANOTBLAST",
	"HERETIC_TELEGLITGEN",
	"HERETIC_TELEGLITGEN2",
	"HERETIC_TELEGLITTER",
	"HERETIC_TELEGLITTER2",
	"HERETIC_TFOG",
	"HERETIC_TELEPORTMAN",
	"HERETIC_STAFFPUFF",
	"HERETIC_STAFFPUFF2",
	"HERETIC_BEAKPUFF",
	"HERETIC_MISC13",
	"HERETIC_GAUNTLETPUFF1",
	"HERETIC_GAUNTLETPUFF2",
	"HERETIC_MISC14",
	"HERETIC_BLASTERFX1",
	"HERETIC_BLASTERSMOKE",
	"HERETIC_RIPPER",
	"HERETIC_BLASTERPUFF1",
	"HERETIC_BLASTERPUFF2",
	"HERETIC_WMACE",
	"HERETIC_MACEFX1",
	"HERETIC_MACEFX2",
	"HERETIC_MACEFX3",
	"HERETIC_MACEFX4",
	"HERETIC_WSKULLROD",
	"HERETIC_HORNRODFX1",
	"HERETIC_HORNRODFX2",
	"HERETIC_RAINPLR1",
	"HERETIC_RAINPLR2",
	"HERETIC_RAINPLR3",
	"HERETIC_RAINPLR4",
	"HERETIC_GOLDWANDFX1",
	"HERETIC_GOLDWANDFX2",
	"HERETIC_GOLDWANDPUFF1",
	"HERETIC_GOLDWANDPUFF2",
	"HERETIC_WPHOENIXROD",
	"HERETIC_PHOENIXFX1",
	"HERETIC_PHOENIXFX_REMOVED",
	"HERETIC_PHOENIXPUFF",
	"HERETIC_PHOENIXFX2",
	"HERETIC_MISC15",
	"HERETIC_CRBOWFX1",
	"HERETIC_CRBOWFX2",
	"HERETIC_CRBOWFX3",
	"HERETIC_CRBOWFX4",
	"HERETIC_BLOOD",
	"HERETIC_BLOODSPLATTER",
	"HERETIC_PLAYER",
	"HERETIC_BLOODYSKULL",
	"HERETIC_CHICPLAYER",
	"HERETIC_CHICKEN",
	"HERETIC_FEATHER",
	"HERETIC_MUMMY",
	"HERETIC_MUMMYLEADER",
	"HERETIC_MUMMYGHOST",
	"HERETIC_MUMMYLEADERGHOST",
	"HERETIC_MUMMYSOUL",
	"HERETIC_MUMMYFX1",
	"HERETIC_BEAST",
	"HERETIC_BEASTBALL",
	"HERETIC_BURNBALL",
	"HERETIC_BURNBALLFB",
	"HERETIC_PUFFY",
	"HERETIC_SNAKE",
	"HERETIC_SNAKEPRO_A",
	"HERETIC_SNAKEPRO_B",
	"HERETIC_HEAD",
	"HERETIC_HEADFX1",
	"HERETIC_HEADFX2",
	"HERETIC_HEADFX3",
	"HERETIC_WHIRLWIND",
	"HERETIC_CLINK",
	"HERETIC_WIZARD",
	"HERETIC_WIZFX1",
	"HERETIC_IMP",
	"HERETIC_IMPLEADER",
	"HERETIC_IMPCHUNK1",
	"HERETIC_IMPCHUNK2",
	"HERETIC_IMPBALL",
	"HERETIC_KNIGHT",
	"HERETIC_KNIGHTGHOST",
	"HERETIC_KNIGHTAXE",
	"HERETIC_REDAXE",
	"HERETIC_SORCERER1",
	"HERETIC_SRCRFX1",
	"HERETIC_SORCERER2",
	"HERETIC_SOR2FX1",
	"HERETIC_SOR2FXSPARK",
	"HERETIC_SOR2FX2",
	"HERETIC_SOR2TELEFADE",
	"HERETIC_MINOTAUR",
	"HERETIC_MNTRFX1",
	"HERETIC_MNTRFX2",
	"HERETIC_MNTRFX3",
	"HERETIC_AKYY",
	"HERETIC_BKYY",
	"HERETIC_CKEY",
	"HERETIC_AMGWNDWIMPY",
	"HERETIC_AMGWNDHEFTY",
	"HERETIC_AMMACEWIMPY",
	"HERETIC_AMMACEHEFTY",
	"HERETIC_AMCBOWWIMPY",
	"HERETIC_AMCBOWHEFTY",
	"HERETIC_AMSKRDWIMPY",
	"HERETIC_AMSKRDHEFTY",
	"HERETIC_AMPHRDWIMPY",
	"HERETIC_AMPHRDHEFTY",
	"HERETIC_AMBLSRWIMPY",
	"HERETIC_AMBLSRHEFTY",
	"HERETIC_SOUNDWIND",
	"HERETIC_SOUNDWATERFALL",
	"HERETIC_NUMMOBJTYPES",
	"HEXEN_MAPSPOT",
	"HEXEN_MAPSPOTGRAVITY",
	"HEXEN_FIREBALL1",
	"HEXEN_ARROW",
	"HEXEN_DART",
	"HEXEN_POISONDART",
	"HEXEN_RIPPERBALL",
	"HEXEN_PROJECTILE_BLADE",
	"HEXEN_ICESHARD",
	"HEXEN_FLAME_SMALL_TEMP",
	"HEXEN_FLAME_LARGE_TEMP",
	"HEXEN_FLAME_SMALL",
	"HEXEN_FLAME_LARGE",
	"HEXEN_HEALINGBOTTLE",
	"HEXEN_HEALTHFLASK",
	"HEXEN_ARTIFLY",
	"HEXEN_ARTIINVULNERABILITY",
	"HEXEN_SUMMONMAULATOR",
	"HEXEN_SUMMON_FX",
	"HEXEN_THRUSTFLOOR_UP",
	"HEXEN_THRUSTFLOOR_DOWN",
	"HEXEN_TELEPORTOTHER",
	"HEXEN_TELOTHER_FX1",
	"HEXEN_TELOTHER_FX2",
	"HEXEN_TELOTHER_FX3",
	"HEXEN_TELOTHER_FX4",
	"HEXEN_TELOTHER_FX5",
	"HEXEN_DIRT1",
	"HEXEN_DIRT2",
	"HEXEN_DIRT3",
	"HEXEN_DIRT4",
	"HEXEN_DIRT5",
	"HEXEN_DIRT6",
	"HEXEN_DIRTCLUMP",
	"HEXEN_ROCK1",
	"HEXEN_ROCK2",
	"HEXEN_ROCK3",
	"HEXEN_FOGSPAWNER",
	"HEXEN_FOGPATCHS",
	"HEXEN_FOGPATCHM",
	"HEXEN_FOGPATCHL",
	"HEXEN_QUAKE_FOCUS",
	"HEXEN_SGSHARD1",
	"HEXEN_SGSHARD2",
	"HEXEN_SGSHARD3",
	"HEXEN_SGSHARD4",
	"HEXEN_SGSHARD5",
	"HEXEN_SGSHARD6",
	"HEXEN_SGSHARD7",
	"HEXEN_SGSHARD8",
	"HEXEN_SGSHARD9",
	"HEXEN_SGSHARD0",
	"HEXEN_ARTIEGG",
	"HEXEN_EGGFX",
	"HEXEN_ARTISUPERHEAL",
	"HEXEN_ZWINGEDSTATUENOSKULL",
	"HEXEN_ZGEMPEDESTAL",
	"HEXEN_ARTIPUZZSKULL",
	"HEXEN_ARTIPUZZGEMBIG",
	"HEXEN_ARTIPUZZGEMRED",
	"HEXEN_ARTIPUZZGEMGREEN1",
	"HEXEN_ARTIPUZZGEMGREEN2",
	"HEXEN_ARTIPUZZGEMBLUE1",
	"HEXEN_ARTIPUZZGEMBLUE2",
	"HEXEN_ARTIPUZZBOOK1",
	"HEXEN_ARTIPUZZBOOK2",
	"HEXEN_ARTIPUZZSKULL2",
	"HEXEN_ARTIPUZZFWEAPON",
	"HEXEN_ARTIPUZZCWEAPON",
	"HEXEN_ARTIPUZZMWEAPON",
	"HEXEN_ARTIPUZZGEAR",
	"HEXEN_ARTIPUZZGEAR2",
	"HEXEN_ARTIPUZZGEAR3",
	"HEXEN_ARTIPUZZGEAR4",
	"HEXEN_ARTITORCH",
	"HEXEN_FIREBOMB",
	"HEXEN_ARTITELEPORT",
	"HEXEN_ARTIPOISONBAG",
	"HEXEN_POISONBAG",
	"HEXEN_POISONCLOUD",
	"HEXEN_THROWINGBOMB",
	"HEXEN_SPEEDBOOTS",
	"HEXEN_BOOSTMANA",
	"HEXEN_BOOSTARMOR",
	"HEXEN_BLASTRADIUS",
	"HEXEN_HEALRADIUS",
	"HEXEN_SPLASH",
	"HEXEN_SPLASHBASE",
	"HEXEN_LAVASPLASH",
	"HEXEN_LAVASMOKE",
	"HEXEN_SLUDGECHUNK",
	"HEXEN_SLUDGESPLASH",
	"HEXEN_MISC0",
	"HEXEN_MISC1",
	"HEXEN_MISC2",
	"HEXEN_MISC3",
	"HEXEN_MISC4",
	"HEXEN_MISC5",
	"HEXEN_MISC6",
	"HEXEN_MISC7",
	"HEXEN_MISC8",
	"HEXEN_TREEDESTRUCTIBLE",
	"HEXEN_MISC9",
	"HEXEN_MISC10",
	"HEXEN_MISC11",
	"HEXEN_MISC12",
	"HEXEN_MISC13",
	"HEXEN_MISC14",
	"HEXEN_MISC15",
	"HEXEN_MISC16",
	"HEXEN_MISC17",
	"HEXEN_MISC18",
	"HEXEN_MISC19",
	"HEXEN_MISC20",
	"HEXEN_MISC21",
	"HEXEN_MISC22",
	"HEXEN_MISC23",
	"HEXEN_MISC24",
	"HEXEN_MISC25",
	"HEXEN_MISC26",
	"HEXEN_MISC27",
	"HEXEN_MISC28",
	"HEXEN_MISC29",
	"HEXEN_MISC30",
	"HEXEN_MISC31",
	"HEXEN_MISC32",
	"HEXEN_MISC33",
	"HEXEN_MISC34",
	"HEXEN_MISC35",
	"HEXEN_MISC36",
	"HEXEN_MISC37",
	"HEXEN_MISC38",
	"HEXEN_MISC39",
	"HEXEN_MISC40",
	"HEXEN_MISC41",
	"HEXEN_MISC42",
	"HEXEN_MISC43",
	"HEXEN_MISC44",
	"HEXEN_MISC45",
	"HEXEN_MISC46",
	"HEXEN_MISC47",
	"HEXEN_MISC48",
	"HEXEN_MISC49",
	"HEXEN_MISC50",
	"HEXEN_MISC51",
	"HEXEN_MISC52",
	"HEXEN_MISC53",
	"HEXEN_MISC54",
	"HEXEN_MISC55",
	"HEXEN_MISC56",
	"HEXEN_MISC57",
	"HEXEN_MISC58",
	"HEXEN_MISC59",
	"HEXEN_MISC60",
	"HEXEN_MISC61",
	"HEXEN_MISC62",
	"HEXEN_MISC63",
	"HEXEN_MISC64",
	"HEXEN_MISC65",
	"HEXEN_MISC66",
	"HEXEN_MISC67",
	"HEXEN_MISC68",
	"HEXEN_MISC69",
	"HEXEN_MISC70",
	"HEXEN_MISC71",
	"HEXEN_MISC72",
	"HEXEN_MISC73",
	"HEXEN_MISC74",
	"HEXEN_MISC75",
	"HEXEN_MISC76",
	"HEXEN_POTTERY1",
	"HEXEN_POTTERY2",
	"HEXEN_POTTERY3",
	"HEXEN_POTTERYBIT1",
	"HEXEN_MISC77",
	"HEXEN_ZLYNCHED_NOHEART",
	"HEXEN_MISC78",
	"HEXEN_CORPSEBIT",
	"HEXEN_CORPSEBLOODDRIP",
	"HEXEN_BLOODPOOL",
	"HEXEN_MISC79",
	"HEXEN_MISC80",
	"HEXEN_LEAF1",
	"HEXEN_LEAF2",
	"HEXEN_ZTWINEDTORCH",
	"HEXEN_ZTWINEDTORCH_UNLIT",
	"HEXEN_BRIDGE",
	"HEXEN_BRIDGEBALL",
	"HEXEN_ZWALLTORCH",
	"HEXEN_ZWALLTORCH_UNLIT",
	"HEXEN_ZBARREL",
	"HEXEN_ZSHRUB1",
	"HEXEN_ZSHRUB2",
	"HEXEN_ZBUCKET",
	"HEXEN_ZPOISONSHROOM",
	"HEXEN_ZFIREBULL",
	"HEXEN_ZFIREBULL_UNLIT",
	"HEXEN_FIRETHING",
	"HEXEN_BRASSTORCH",
	"HEXEN_ZSUITOFARMOR",
	"HEXEN_ZARMORCHUNK",
	"HEXEN_ZBELL",
	"HEXEN_ZBLUE_CANDLE",
	"HEXEN_ZIRON_MAIDEN",
	"HEXEN_ZXMAS_TREE",
	"HEXEN_ZCAULDRON",
	"HEXEN_ZCAULDRON_UNLIT",
	"HEXEN_ZCHAINBIT32",
	"HEXEN_ZCHAINBIT64",
	"HEXEN_ZCHAINEND_HEART",
	"HEXEN_ZCHAINEND_HOOK1",
	"HEXEN_ZCHAINEND_HOOK2",
	"HEXEN_ZCHAINEND_SPIKE",
	"HEXEN_ZCHAINEND_SKULL",
	"HEXEN_TABLE_SHIT1",
	"HEXEN_TABLE_SHIT2",
	"HEXEN_TABLE_SHIT3",
	"HEXEN_TABLE_SHIT4",
	"HEXEN_TABLE_SHIT5",
	"HEXEN_TABLE_SHIT6",
	"HEXEN_TABLE_SHIT7",
	"HEXEN_TABLE_SHIT8",
	"HEXEN_TABLE_SHIT9",
	"HEXEN_TABLE_SHIT10",
	"HEXEN_TFOG",
	"HEXEN_MISC81",
	"HEXEN_TELEPORTMAN",
	"HEXEN_PUNCHPUFF",
	"HEXEN_FW_AXE",
	"HEXEN_AXEPUFF",
	"HEXEN_AXEPUFF_GLOW",
	"HEXEN_AXEBLOOD",
	"HEXEN_FW_HAMMER",
	"HEXEN_HAMMER_MISSILE",
	"HEXEN_HAMMERPUFF",
	"HEXEN_FSWORD_MISSILE",
	"HEXEN_FSWORD_FLAME",
	"HEXEN_CW_SERPSTAFF",
	"HEXEN_CSTAFF_MISSILE",
	"HEXEN_CSTAFFPUFF",
	"HEXEN_CW_FLAME",
	"HEXEN_CFLAMEFLOOR",
	"HEXEN_FLAMEPUFF",
	"HEXEN_FLAMEPUFF2",
	"HEXEN_CIRCLEFLAME",
	"HEXEN_CFLAME_MISSILE",
	"HEXEN_HOLY_FX",
	"HEXEN_HOLY_TAIL",
	"HEXEN_HOLY_PUFF",
	"HEXEN_HOLY_MISSILE",
	"HEXEN_HOLY_MISSILE_PUFF",
	"HEXEN_MWANDPUFF",
	"HEXEN_MWANDSMOKE",
	"HEXEN_MWAND_MISSILE",
	"HEXEN_MW_LIGHTNING",
	"HEXEN_LIGHTNING_CEILING",
	"HEXEN_LIGHTNING_FLOOR",
	"HEXEN_LIGHTNING_ZAP",
	"HEXEN_MSTAFF_FX",
	"HEXEN_MSTAFF_FX2",
	"HEXEN_FW_SWORD1",
	"HEXEN_FW_SWORD2",
	"HEXEN_FW_SWORD3",
	"HEXEN_CW_HOLY1",
	"HEXEN_CW_HOLY2",
	"HEXEN_CW_HOLY3",
	"HEXEN_MW_STAFF1",
	"HEXEN_MW_STAFF2",
	"HEXEN_MW_STAFF3",
	"HEXEN_SNOUTPUFF",
	"HEXEN_MW_CONE",
	"HEXEN_SHARDFX1",
	"HEXEN_BLOOD",
	"HEXEN_BLOODSPLATTER",
	"HEXEN_GIBS",
	"HEXEN_PLAYER_FIGHTER",
	"HEXEN_BLOODYSKULL",
	"HEXEN_PLAYER_SPEED",
	"HEXEN_ICECHUNK",
	"HEXEN_PLAYER_CLERIC",
	"HEXEN_PLAYER_MAGE",
	"HEXEN_PIGPLAYER",
	"HEXEN_PIG",
	"HEXEN_CENTAUR",
	"HEXEN_CENTAURLEADER",
	"HEXEN_CENTAUR_FX",
	"HEXEN_CENTAUR_SHIELD",
	"HEXEN_CENTAUR_SWORD",
	"HEXEN_DEMON",
	"HEXEN_DEMONCHUNK1",
	"HEXEN_DEMONCHUNK2",
	"HEXEN_DEMONCHUNK3",
	"HEXEN_DEMONCHUNK4",
	"HEXEN_DEMONCHUNK5",
	"HEXEN_DEMONFX1",
	"HEXEN_DEMON2",
	"HEXEN_DEMON2CHUNK1",
	"HEXEN_DEMON2CHUNK2",
	"HEXEN_DEMON2CHUNK3",
	"HEXEN_DEMON2CHUNK4",
	"HEXEN_DEMON2CHUNK5",
	"HEXEN_DEMON2FX1",
	"HEXEN_WRAITHB",
	"HEXEN_WRAITH",
	"HEXEN_WRAITHFX1",
	"HEXEN_WRAITHFX2",
	"HEXEN_WRAITHFX3",
	"HEXEN_WRAITHFX4",
	"HEXEN_WRAITHFX5",
	"HEXEN_MINOTAUR",
	"HEXEN_MNTRFX1",
	"HEXEN_MNTRFX2",
	"HEXEN_MNTRFX3",
	"HEXEN_MNTRSMOKE",
	"HEXEN_MNTRSMOKEEXIT",
	"HEXEN_SERPENT",
	"HEXEN_SERPENTLEADER",
	"HEXEN_SERPENTFX",
	"HEXEN_SERPENT_HEAD",
	"HEXEN_SERPENT_GIB1",
	"HEXEN_SERPENT_GIB2",
	"HEXEN_SERPENT_GIB3",
	"HEXEN_BISHOP",
	"HEXEN_BISHOP_PUFF",
	"HEXEN_BISHOPBLUR",
	"HEXEN_BISHOPPAINBLUR",
	"HEXEN_BISH_FX",
	"HEXEN_DRAGON",
	"HEXEN_DRAGON_FX",
	"HEXEN_DRAGON_FX2",
	"HEXEN_ARMOR_1",
	"HEXEN_ARMOR_2",
	"HEXEN_ARMOR_3",
	"HEXEN_ARMOR_4",
	"HEXEN_MANA1",
	"HEXEN_MANA2",
	"HEXEN_MANA3",
	"HEXEN_KEY1",
	"HEXEN_KEY2",
	"HEXEN_KEY3",
	"HEXEN_KEY4",
	"HEXEN_KEY5",
	"HEXEN_KEY6",
	"HEXEN_KEY7",
	"HEXEN_KEY8",
	"HEXEN_KEY9",
	"HEXEN_KEYA",
	"HEXEN_KEYB",
	"HEXEN_SOUNDWIND",
	"HEXEN_SOUNDWATERFALL",
	"HEXEN_ETTIN",
	"HEXEN_ETTIN_MACE",
	"HEXEN_FIREDEMON",
	"HEXEN_FIREDEMON_SPLOTCH1",
	"HEXEN_FIREDEMON_SPLOTCH2",
	"HEXEN_FIREDEMON_FX1",
	"HEXEN_FIREDEMON_FX2",
	"HEXEN_FIREDEMON_FX3",
	"HEXEN_FIREDEMON_FX4",
	"HEXEN_FIREDEMON_FX5",
	"HEXEN_FIREDEMON_FX6",
	"HEXEN_ICEGUY",
	"HEXEN_ICEGUY_FX",
	"HEXEN_ICEFX_PUFF",
	"HEXEN_ICEGUY_FX2",
	"HEXEN_ICEGUY_BIT",
	"HEXEN_ICEGUY_WISP1",
	"HEXEN_ICEGUY_WISP2",
	"HEXEN_FIGHTER_BOSS",
	"HEXEN_CLERIC_BOSS",
	"HEXEN_MAGE_BOSS",
	"HEXEN_SORCBOSS",
	"HEXEN_SORCBALL1",
	"HEXEN_SORCBALL2",
	"HEXEN_SORCBALL3",
	"HEXEN_SORCFX1",
	"HEXEN_SORCFX2",
	"HEXEN_SORCFX2_T1",
	"HEXEN_SORCFX3",
	"HEXEN_SORCFX3_EXPLOSION",
	"HEXEN_SORCFX4",
	"HEXEN_SORCSPARK1",
	"HEXEN_BLASTEFFECT",
	"HEXEN_WATER_DRIP",
	"HEXEN_KORAX",
	"HEXEN_KORAX_SPIRIT1",
	"HEXEN_KORAX_SPIRIT2",
	"HEXEN_KORAX_SPIRIT3",
	"HEXEN_KORAX_SPIRIT4",
	"HEXEN_KORAX_SPIRIT5",
	"HEXEN_KORAX_SPIRIT6",
	"HEXEN_DEMON_MASH",
	"HEXEN_DEMON2_MASH",
	"HEXEN_ETTIN_MASH",
	"HEXEN_CENTAUR_MASH",
	"HEXEN_KORAX_BOLT",
	"HEXEN_BAT_SPAWNER",
	"HEXEN_BAT",
	"HEXEN_NUMMOBJTYPES",
}
assign_keys(dsda.mobjtype)

-- spritenum_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/info.h#L50-L632
dsda.doom.spritenum = {
	[0] = "TROO",
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
	"BLD2",
}
assign_keys(dsda.doom.spritenum)

dsda.heretic.spritenum = {
	[0] = "IMPX",
	"ACLO",
	"PTN1",
	"SHLD",
	"SHD2",
	"BAGH",
	"SPMP",
	"INVS",
	"PTN2",
	"SOAR",
	"INVU",
	"PWBK",
	"EGGC",
	"EGGM",
	"FX01",
	"SPHL",
	"TRCH",
	"FBMB",
	"XPL1",
	"ATLP",
	"PPOD",
	"AMG1",
	"SPSH",
	"LVAS",
	"SLDG",
	"SKH1",
	"SKH2",
	"SKH3",
	"SKH4",
	"CHDL",
	"SRTC",
	"SMPL",
	"STGS",
	"STGL",
	"STCS",
	"STCL",
	"KFR1",
	"BARL",
	"BRPL",
	"MOS1",
	"MOS2",
	"WTRH",
	"HCOR",
	"KGZ1",
	"KGZB",
	"KGZG",
	"KGZY",
	"VLCO",
	"VFBL",
	"VTFB",
	"SFFI",
	"TGLT",
	"TELE",
	"STFF",
	"PUF3",
	"PUF4",
	"BEAK",
	"WGNT",
	"GAUN",
	"PUF1",
	"WBLS",
	"BLSR",
	"FX18",
	"FX17",
	"WMCE",
	"MACE",
	"FX02",
	"WSKL",
	"HROD",
	"FX00",
	"FX20",
	"FX21",
	"FX22",
	"FX23",
	"GWND",
	"PUF2",
	"WPHX",
	"PHNX",
	"FX04",
	"FX08",
	"FX09",
	"WBOW",
	"CRBW",
	"FX03",
	"BLOD",
	"PLAY",
	"FDTH",
	"BSKL",
	"CHKN",
	"MUMM",
	"FX15",
	"BEAS",
	"FRB1",
	"SNKE",
	"SNFX",
	"HEAD",
	"FX05",
	"FX06",
	"FX07",
	"CLNK",
	"WZRD",
	"FX11",
	"FX10",
	"KNIG",
	"SPAX",
	"RAXE",
	"SRCR",
	"FX14",
	"SOR2",
	"SDTH",
	"FX16",
	"MNTR",
	"FX12",
	"FX13",
	"AKYY",
	"BKYY",
	"CKYY",
	"AMG2",
	"AMM1",
	"AMM2",
	"AMC1",
	"AMC2",
	"AMS1",
	"AMS2",
	"AMP1",
	"AMP2",
	"AMB1",
	"AMB2",
}
assign_keys(dsda.heretic.spritenum)

dsda.hexen.spritenum = {
	[0] = "MAN1",
	"ACLO",
	"TLGL",
	"FBL1",
	"XPL1",
	"ARRW",
	"DART",
	"RIPP",
	"CFCF",
	"BLAD",
	"SHRD",
	"FFSM",
	"FFLG",
	"PTN1",
	"PTN2",
	"SOAR",
	"INVU",
	"SUMN",
	"TSPK",
	"TELO",
	"TRNG",
	"ROCK",
	"FOGS",
	"FOGM",
	"FOGL",
	"SGSA",
	"SGSB",
	"PORK",
	"EGGM",
	"FHFX",
	"SPHL",
	"STWN",
	"GMPD",
	"ASKU",
	"ABGM",
	"AGMR",
	"AGMG",
	"AGG2",
	"AGMB",
	"AGB2",
	"ABK1",
	"ABK2",
	"ASK2",
	"AFWP",
	"ACWP",
	"AMWP",
	"AGER",
	"AGR2",
	"AGR3",
	"AGR4",
	"TRCH",
	"PSBG",
	"ATLP",
	"THRW",
	"SPED",
	"BMAN",
	"BRAC",
	"BLST",
	"HRAD",
	"SPSH",
	"LVAS",
	"SLDG",
	"STTW",
	"RCK1",
	"RCK2",
	"RCK3",
	"RCK4",
	"CDLR",
	"TRE1",
	"TRDT",
	"TRE2",
	"TRE3",
	"STM1",
	"STM2",
	"STM3",
	"STM4",
	"MSH1",
	"MSH2",
	"MSH3",
	"MSH4",
	"MSH5",
	"MSH6",
	"MSH7",
	"MSH8",
	"SGMP",
	"SGM1",
	"SGM2",
	"SGM3",
	"SLC1",
	"SLC2",
	"SLC3",
	"MSS1",
	"MSS2",
	"SWMV",
	"CPS1",
	"CPS2",
	"TMS1",
	"TMS2",
	"TMS3",
	"TMS4",
	"TMS5",
	"TMS6",
	"TMS7",
	"CPS3",
	"STT2",
	"STT3",
	"STT4",
	"STT5",
	"GAR1",
	"GAR2",
	"GAR3",
	"GAR4",
	"GAR5",
	"GAR6",
	"GAR7",
	"GAR8",
	"GAR9",
	"BNR1",
	"TRE4",
	"TRE5",
	"TRE6",
	"TRE7",
	"LOGG",
	"ICT1",
	"ICT2",
	"ICT3",
	"ICT4",
	"ICM1",
	"ICM2",
	"ICM3",
	"ICM4",
	"RKBL",
	"RKBS",
	"RKBK",
	"RBL1",
	"RBL2",
	"RBL3",
	"VASE",
	"POT1",
	"POT2",
	"POT3",
	"PBIT",
	"CPS4",
	"CPS5",
	"CPS6",
	"CPB1",
	"CPB2",
	"CPB3",
	"CPB4",
	"BDRP",
	"BDSH",
	"BDPL",
	"CNDL",
	"LEF1",
	"LEF3",
	"LEF2",
	"TWTR",
	"WLTR",
	"BARL",
	"SHB1",
	"SHB2",
	"BCKT",
	"SHRM",
	"FBUL",
	"FSKL",
	"BRTR",
	"SUIT",
	"BBLL",
	"CAND",
	"IRON",
	"XMAS",
	"CDRN",
	"CHNS",
	"TST1",
	"TST2",
	"TST3",
	"TST4",
	"TST5",
	"TST6",
	"TST7",
	"TST8",
	"TST9",
	"TST0",
	"TELE",
	"TSMK",
	"FPCH",
	"WFAX",
	"FAXE",
	"WFHM",
	"FHMR",
	"FSRD",
	"FSFX",
	"CMCE",
	"WCSS",
	"CSSF",
	"WCFM",
	"CFLM",
	"CFFX",
	"CHLY",
	"SPIR",
	"MWND",
	"WMLG",
	"MLNG",
	"MLFX",
	"MLF2",
	"MSTF",
	"MSP1",
	"MSP2",
	"WFR1",
	"WFR2",
	"WFR3",
	"WCH1",
	"WCH2",
	"WCH3",
	"WMS1",
	"WMS2",
	"WMS3",
	"WPIG",
	"WMCS",
	"CONE",
	"SHEX",
	"BLOD",
	"GIBS",
	"PLAY",
	"FDTH",
	"BSKL",
	"ICEC",
	"CLER",
	"MAGE",
	"PIGY",
	"CENT",
	"CTXD",
	"CTFX",
	"CTDP",
	"DEMN",
	"DEMA",
	"DEMB",
	"DEMC",
	"DEMD",
	"DEME",
	"DMFX",
	"DEM2",
	"DMBA",
	"DMBB",
	"DMBC",
	"DMBD",
	"DMBE",
	"D2FX",
	"WRTH",
	"WRT2",
	"WRBL",
	"MNTR",
	"FX12",
	"FX13",
	"MNSM",
	"SSPT",
	"SSDV",
	"SSXD",
	"SSFX",
	"BISH",
	"BPFX",
	"DRAG",
	"DRFX",
	"ARM1",
	"ARM2",
	"ARM3",
	"ARM4",
	"MAN2",
	"MAN3",
	"KEY1",
	"KEY2",
	"KEY3",
	"KEY4",
	"KEY5",
	"KEY6",
	"KEY7",
	"KEY8",
	"KEY9",
	"KEYA",
	"KEYB",
	"ETTN",
	"ETTB",
	"FDMN",
	"FDMB",
	"ICEY",
	"ICPR",
	"ICWS",
	"SORC",
	"SBMP",
	"SBS4",
	"SBMB",
	"SBS3",
	"SBMG",
	"SBS1",
	"SBS2",
	"SBFX",
	"RADE",
	"WATR",
	"KORX",
	"ABAT",
}
assign_keys(dsda.hexen.spritenum)


-- MF_* https://github.com/TASEmulators/dsda-doom/blob/7f03360ce0e9000c394fb99869d78adf4603ade5/prboom2/src/p_mobj.h#L121-L233
dsda.mobjflags = {
	SPECIAL = 0x1,
	SOLID = 0x2,
	SHOOTABLE = 0x4,
	NOSECTOR = 0x8,
	NOBLOCKMAP = 0x10,
	AMBUSH = 0x20,
	JUSTHIT = 0x40,
	JUSTATTACKED = 0x80,
	SPAWNCEILING = 0x100,
	NOGRAVITY = 0x200,
	DROPOFF = 0x400,
	PICKUP = 0x800,
	NOCLIP = 0x1000,
	SLIDE = 0x2000,
	FLOAT = 0x4000,
	TELEPORT = 0x8000,
	MISSILE = 0x10000,
	DROPPED = 0x20000,
	SHADOW = 0x40000,
	NOBLOOD = 0x80000,
	CORPSE = 0x100000,
	INFLOAT = 0x200000,
	COUNTKILL = 0x400000,
	COUNTITEM = 0x800000,
	SKULLFLY = 0x1000000,
	NOTDMATCH = 0x2000000,
	TRANSLATION = 0xc000000,
	TRANSLATION1 = 0x4000000,
	TRANSLATION2 = 0x8000000,
	UNUSED2 = 0x10000000,
	UNUSED3 = 0x20000000,
	TRANSLUCENT = 0x40000000,
	TOUCHY = 0x100000000,
	BOUNCES = 0x200000000,
	FRIEND = 0x400000000,
	RESSURECTED = 0x1000000000,
	NO_DEPTH_TEST = 0x2000000000,
	FOREGROUND = 0x4000000000,
	PLAYERSPRITE = 0x8000000000,
	NOTARGET = 0x10000000000,
	FLY = 0x20000000000,
	ALTSHADOW = 0x40000000000,
	ICECORPSE = 0x80000000000,
}



return dsda;
