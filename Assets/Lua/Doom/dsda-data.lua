local dsda = {}



-- Constants ---

dsda.MAX_PLAYERS = 4
-- sizes in bytes
dsda.LINE_SIZE   = 256  -- sizeof(line_t) is 232, but we padded it for niceness
dsda.MOBJ_SIZE   = 512  -- sizeof(mobj_t) is 464, but we padded it for niceness
dsda.PLAYER_SIZE = 1024 -- sizeof(player_t) is 729, but we padded it for niceness
dsda.SECTOR_SIZE = 512  -- sizeof(sector_t) is 344, but we padded it for niceness



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
		return struct
	end
	function struct.pad(size)
		struct.size = struct.size + size
		return struct
	 end
	function struct.s8   (name) return struct.add(name, 1, true) end
	function struct.s16  (name) return struct.add(name, 2, true) end
	function struct.s32  (name) return struct.add(name, 4, true) end
	function struct.u8   (name) return struct.add(name, 1, true) end
	function struct.u16  (name) return struct.add(name, 2, true) end
	function struct.u32  (name) return struct.add(name, 4, true) end
	function struct.u64  (name) return struct.add(name, 8, true) end
	function struct.float(name) return struct.add(name, 4, true) end
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

-- player_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/d_player.h#L143-L267
dsda.player = dsda.struct_layout()
	.ptr  ("mobj")
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
	.ptr  ("attacker")
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
	.ptr  ("rain1")
	.ptr  ("rain2")
	-- hexen
	.s32  ("pclass") -- pclass_t
	.s32  ("morphTics")
	.s32  ("pieces")
	.s16  ("yellowMessage")
	.s32  ("poisoncount")
	.ptr  ("poisoner")
	.u32  ("jumpTics")
	.u32  ("worldTimer")
	-- zdoom
	.s32  ("hazardcount")
	.u8   ("hazardinterval")

-- mobj_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/p_mobj.h#L277-L413
dsda.mobj = dsda.struct_layout()
	.add  ("thinker", 44, 8)
	.s32  ("x")
	.s32  ("y")
	.s32  ("z")
	.ptr  ("snext")
	.ptr  ("sprev")
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
	.u64  ("flags")
	.s32  ("intflags")
	.s32  ("health")
	.s16  ("movedir")
	.s16  ("movecount")
	.s16  ("strafecount")
	.ptr  ("target")
	.s16  ("reactiontime")
	.s16  ("threshold")
	.s16  ("pursuecount")
	.s16  ("gear")
	.ptr  ("player")
	.s16  ("lastlook")
	.add  ("spawnpoint", 58, 4) -- mapthing_t
	.ptr  ("tracer")
	.ptr  ("lastenemy")
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
	.u64  ("flags2")
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

-- sector_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L124-L213
dsda.sector = dsda.struct_layout()
	.s32  ("iSectorID")
	.u32  ("flags")
	.s32  ("floorheight")
	.s32  ("ceilingheight")
	.u8   ("soundtraversed")
	.ptr  ("soundtarget")
	.array("blockbox", "s32", 4)
--	.array("bbox", "s32", 4)
	.s32  ("bbox_top")
	.s32  ("bbox_bottom")
	.s32  ("bbox_left")
	.s32  ("bbox_right")
	.add  ("soundorg", 60, 8) -- degenmobj_t;
	.s32  ("validcount")
	.s32  ("gl_validcount")
	.ptr  ("thinglist")
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
	.ptr  ("lines")
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
  	-- e6y
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

-- line_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L312-L347
-- followed by v1, v2 coords
dsda.line = dsda.struct_layout()
	.s32  ("iLineID")
	.ptr  ("v1")
	.ptr  ("v2")
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
	.ptr  ("frontsector")
	.ptr  ("backsector")
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
	.align(8)
	-- BizHawk
	.s32  ("v1_x")
	.s32  ("v1_y")
	.s32  ("v2_x")
	.s32  ("v2_y")



-- Enums ---

dsda.doom = {}

-- mobjtype_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/info.h#L5778-L6498
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
	"BLD2"
}

return dsda;
