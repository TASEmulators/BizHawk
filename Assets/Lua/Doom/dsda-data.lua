local dsda = {}



-- Constants ---

dsda.MAX_PLAYERS = 4
-- sizes in bytes
dsda.LINE_SIZE   = 256  -- sizeof(line_t) is 232, but we padded it for niceness
dsda.MOBJ_SIZE   = 512  -- sizeof(mobj_t) is 464, but we padded it for niceness
dsda.PLAYER_SIZE = 1024 -- sizeof(player_t) is 729, but we padded it for niceness



-- Structs ---

function dsda.struct_layout(struct)
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
	function struct.s8   (name) return struct.add(name, 1, true) end
	function struct.s16  (name) return struct.add(name, 2, true) end
	function struct.s32  (name) return struct.add(name, 4, true) end
	function struct.u8   (name) return struct.add(name, 1, true) end
	function struct.u32  (name) return struct.add(name, 4, true) end
	function struct.u64  (name) return struct.add(name, 8, true) end
	function struct.ptr  (name) return struct.u64(name) end
	function struct.bool (name) return struct.s32(name) end
	function struct.array(name, type, count, ...)
		for i = 1, count do
			struct[type](name .. i, ...)
		end
		return struct
	end

	return struct
end

dsda.player = dsda.struct_layout()
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

dsda.mobj = dsda.struct_layout()
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



-- Enums ---

dsda.doom = {}

dsda.doom.mobjtype = {
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
	"PLASMA2"
}

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
	"BLD2"
}

return dsda;
