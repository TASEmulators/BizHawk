-- kalimag, 2025

local module_prefix = (...):match([[^(.-)[^./\]+$]])
local utils = require(module_prefix.."utils")
local symbols = require(module_prefix.."symbols")

local structs = {}

structs.MAX_PLAYERS = 4

-- Padded sizes as they appear in the Lines/Things/Players/Sectors memory domains
structs.PADDED_SIZE = {
	LINE   = 256,
	MOBJ   = 512,
	PLAYER = 1024,
	SECTOR = 512,
}

-- Native sizes as they appear in the System Bus domain
structs.SIZE = {
	LINE   = 232,
	MOBJ   = 464,
	PLAYER = 792,
	SECTOR = 344,
}

-- forward declarations because of cross references, see further down for fields
-- mobj_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/p_mobj.h#L277-L413
local mobj = utils.domain_struct_layout("mobj", structs.PADDED_SIZE.MOBJ, "Things")

-- sector_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L124-L213
local sector = utils.domain_struct_layout("sector", structs.PADDED_SIZE.SECTOR, "Sectors")

-- line_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L312-L347
local line = utils.domain_struct_layout("line", structs.PADDED_SIZE.LINE, "Lines")

-- thinker_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/d_think.h#L74-L90
local thinker = utils.struct_layout("thinker")
structs.thinker = thinker
	.ptrto("prev", thinker)
	.ptrto("next", thinker)
	.ptr  ("function")
	.ptrto("cnext", thinker)
	.ptrto("cprev", thinker)
	.u32  ("references")
	.build()

--damage_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/r_defs.h#L117-L122
structs.damage = utils.struct_layout("damage")
  .s16  ("amount")
  .u8   ("leakrate")
  .u8   ("interval")
  .build()

-- degenmobj_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/r_defs.h#L83-L87
structs.degenmobj = utils.struct_layout("degenmobj")
	.embed("thinker", structs.thinker)
	.s32  ("x")
	.s32  ("y")
	.s32  ("z")
	.build()

-- excmd_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/d_ticcmd.h#L39-L44
structs.excmd = utils.struct_layout("excmd")
	.u8   ("actions")
	.u8   ("save_slot")
	.u8   ("load_slot")
	.s16  ("look")
	.build()

-- inventory_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/d_player.h#L77-L81
structs.inventory = utils.struct_layout("inventory")
	.s32  ("type") -- artitype_t
	.s32  ("count")
	.build();

-- mapthing_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/doomdata.h#L315-L328
structs.mapthing = utils.struct_layout("mapthing")
	.s16  ("tid")
	.s32  ("x")
	.s32  ("y")
	.s32  ("height")
	.s16  ("angle")
	.s16  ("type")
	.s32  ("options")
	.s32  ("special")
	.array("special_args", "s32", 5)
	.s32  ("gravity")
	.s32  ("health")
	.float("alpha")
	.build()

-- mobjinfo_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/info.h#L6525-L6589
structs.mobjinfo = utils.struct_layout("mobjinfo")
	.s32  ("doomednum")
	.s32  ("spawnstate")
	.s32  ("spawnhealth")
	.s32  ("seestate")
	.s32  ("seesound")
	.s32  ("reactiontime")
	.s32  ("attacksound")
	.s32  ("painstate")
	.s32  ("painchance")
	.s32  ("painsound")
	.s32  ("meleestate")
	.s32  ("missilestate")
	.s32  ("deathstate")
	.s32  ("xdeathstate")
	.s32  ("deathsound")
	.s32  ("speed")
	.s32  ("radius")
	.s32  ("height")
	.s32  ("mass")
	.s32  ("damage")
	.s32  ("activesound")
	.s64  ("flags") -- mobjflags
	.s32  ("raisestate")
	.s32  ("droppeditem") -- mobjtype_t
	-- heretic
	.s32  ("crashstate")
	.s64  ("flags2")
	-- mbf21
	.s32  ("infighting_group")
	.s32  ("projectile_group")
	.s32  ("splash_group")
	.s32  ("ripsound")
	.s32  ("altspeed")
	.s32  ("meleerange")
	-- misc
	.s32  ("bloodcolor")
	.s32  ("visibility")
	.build()

-- msecnode_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/r_defs.h#L373-L382
local msecnode = utils.struct_layout("msecnode")
structs.msecnode = msecnode
	.ptrto("m_sector", sector)
	.ptrto("m_thing", mobj)
	.ptrto("m_tprev", msecnode)
	.ptrto("m_tnext", msecnode)
	.ptrto("m_sprev", msecnode)
	.ptrto("m_snext", msecnode)
	.bool ("visited")
	.build()

-- specialval_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/p_mobj.h#L252-L256
structs.specialval = utils.struct_layout("specialval")
	.s32  ("i")
	.ptrto("m", mobj)
	.build()

-- state_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/info.h#L5757-L5767
structs.state = utils.struct_layout("state")
	.s32  ("sprite") -- spritenum_t
	.s64  ("frame")
	.s64  ("tics")
	.ptr  ("action")
	.s32  ("nextstate")
	.s64  ("misc1")
	.s64  ("misc2")
	.array("args", "s64", 8)
	.s32  ("flags")
	.build()

-- pspdef_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/p_pspr.h#L73-L79
structs.pspdef = utils.struct_layout("pspdef")
	.ptrto("state", structs.state)
	.s32  ("tics")
	.s32  ("sx")
	.s32  ("sy")
	.build()

-- rng_t
structs.rng = utils.struct_layout("rng")
	.array("seed", "u32", 65)
	.s32  ("rndindex")
	.s32  ("prndindex")
	.build()

-- subsector_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/r_defs.h#L422-L431
structs.subsector = utils.struct_layout("subsector")
	.ptrto("sector", sector)
	.s32  ("numlines")
	.s32  ("firstline")
	.ptr  ("poly") -- polyobj_t
	.build()

-- https://github.com/TASEmulators/dsda-doom/blob/3c31ede63018e32687e9f20e91884b65cac3bc79/prboom2/src/p_spec.c#L5283-L5287
structs.taggedline = utils.struct_layout("taggedline")
	.ptrto("line", line)
	.s32  ("lineTag")
	.build()

-- ticcmd_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/d_ticcmd.h#L52-L65
structs.ticcmd = utils.struct_layout("ticcmd")
	.s8   ("forwardmove")
	.s8   ("sidemove")
	.s16  ("angleturn")
	.u8   ("buttons")
	.u8   ("lookfly")
	.u8   ("arti")
	.embed("ex", structs.excmd)
	.build()

-- vertex_t https://github.com/TASEmulators/dsda-doom/blob/623068c33f6bf21239c6c6941f221011b08b6bb9/prboom2/src/r_defs.h#L70-L80
structs.vertex = utils.struct_layout("vertex")
	.s32  ("x")
	.s32  ("y")
	.s32  ("px")
	.s32  ("py")
	.func ("coords", function(self)
		local x, y = utils.read_packed("ii", self._address, self._domain)
		return x, y
	end)
	.build ()

-- seg_t https://github.com/TASEmulators/dsda-doom/blob/3c31ede63018e32687e9f20e91884b65cac3bc79/prboom2/src/r_defs.h#L387-L401
structs.seg = utils.struct_layout("seg")
	.ptrto("v1", structs.vertex)
	.ptrto("v2", structs.vertex)
	.ptr  ("sidedef") -- side_t
	.ptrto("linedef", line)
	.ptrto("frontsector", sector)
	.ptrto("backsector", sector)
	.s32  ("offset")
	.u32  ("angle")
	.u32  ("pangle")
	.u32  ("halflength")
	.build()

-- polyobj_t https://github.com/TASEmulators/dsda-doom/blob/3c31ede63018e32687e9f20e91884b65cac3bc79/prboom2/src/r_defs.h#L589-L607
structs.polyobj = utils.struct_layout("polyobj")
	.s32  ("numsegs")
	.ptr  ("segs_ptr") -- seg_t**
	.embed("startSpot", structs.degenmobj)
	.ptr  ("originalPts_ptr") -- vertex_t*
	.ptr  ("prevPts_ptr") -- vertex_t*
	.u32  ("angle")
	.s32  ("tag")
	.array("bbox", "s32", 4)
	.s32  ("validcount")
	.s32  ("validcount2")
	.bool ("crush")
	.bool ("hurt")
	.s32  ("seqType")
	.s32  ("size")
	.ptr  ("specialdata")
	.ptrto("subsector", structs.subsector)
	.prop ("segs", function (self)
		return utils.pointer_array(self.segs_ptr, "System Bus", self.numsegs, structs.seg.from_pointer)
	end)
	.build()


-- player_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/d_player.h#L143-L267
structs.player = utils.domain_struct_layout("player", structs.PADDED_SIZE.PLAYER, "Players", structs.MAX_PLAYERS)
	.ptrto("mo", mobj)
	.s32  ("playerstate") -- playerstate_t
	.embed("cmd", structs.ticcmd)
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
	.ptrto("attacker", mobj)
	.s32  ("extralight")
	.s32  ("fixedcolormap")
	.s32  ("colormap")
	.array("psprites", "embed", 2, structs.pspdef)
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
	.array("inventory", "embed", 33, structs.inventory)
	.s32  ("readyArtifact") -- artitype_t
	.s32  ("artifactCount")
	.s32  ("inventorySlotNum")
	.s32  ("flamecount")
	.s32  ("chickenTics")
	.s32  ("chickenPeck")
	.ptrto("rain1", mobj)
	.ptrto("rain2", mobj)
	-- hexen
	.s32  ("pclass") -- pclass_t
	.s32  ("morphTics")
	.s32  ("pieces")
	.s16  ("yellowMessage")
	.s32  ("poisoncount")
	.ptrto("poisoner", mobj)
	.u32  ("jumpTics")
	.u32  ("worldTimer")
	-- zdoom
	.s32  ("hazardcount")
	.u8   ("hazardinterval")
	.build()

-- mobj_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/p_mobj.h#L277-L413
structs.mobj = mobj
	.embed("thinker", structs.thinker)
	.s32  ("x")
	.s32  ("y")
	.s32  ("z")
	.ptrto("snext", mobj)
	.ptr  ("sprev") -- pointer to pointer to mobj
	.u32  ("angle")
	.s32  ("sprite") -- spritenum_t
	.s32  ("frame")
	.ptrto("bnext", mobj)
	.ptr  ("bprev") -- pointer to pointer to mobj
	.ptrto("subsector", structs.subsector)
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
	.ptrto("info", structs.mobjinfo)
	.s32  ("tics")
	.ptrto("state", structs.state)
	.s64  ("flags") -- mobjflags
	.s32  ("intflags")
	.s32  ("health")
	.s16  ("movedir")
	.s16  ("movecount")
	.s16  ("strafecount")
	.ptrto("target", mobj)
	.s16  ("reactiontime")
	.s16  ("threshold")
	.s16  ("pursuecount")
	.s16  ("gear")
	.ptrto("player", structs.player)
	.s16  ("lastlook")
	.embed("spawnpoint", structs.mapthing)
	.ptrto("tracer", mobj)
	.ptrto("lastenemy", mobj)
	.s32  ("friction")
	.s32  ("movefactor")
	.ptrto("touching_sectorlist", structs.msecnode) -- start of msecnode.m_tnext linked list
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
	.embed("special1", structs.specialval)
	.embed("special2", structs.specialval)
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
	.func ("iterate_touching_sectorlist", function (self)
		return utils.links(self.touching_sectorlist, "m_tnext", "m_sector")
	end)
	.build()

-- line_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L312-L347
structs.line = line
	.s32  ("iLineID")
	.ptrto("v1", structs.vertex)
	.ptrto("v2", structs.vertex)
	.s32  ("dx")
	.s32  ("dy")
	.float("texel_length")
	.u32  ("flags")
	.s16  ("special")
	.s16  ("tag")
	.array("sidenum", "u16", 2)
	.array("bbox", "s32", 4)
	.s32  ("slopetype") -- slopetype_t
	.ptrto("frontsector", sector)
	.ptrto("backsector", sector)
	.s32  ("validcount")
	.s32  ("validcount2")
	.ptr  ("specialdata")
	.s32  ("r_validcount")
	.u8   ("r_flags")
	.embed("soundorg", structs.degenmobj)
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
	.func ("coords", function(self)
		-- we only care about the low half each pointer, but we read the entire first pointer so we can get both in one read
		local v1, v2 = utils.read_packed("TI", self._address + structs.line.offsets.v1, self._domain)
		local x1, y1 = utils.read_packed("ii", v1 & 0xFFFFFFFF, "System Bus")
		local x2, y2 = utils.read_packed("ii", v2, "System Bus")
		return x1, y1, x2, y2
	end)
	.build()

-- sector_t https://github.com/TASEmulators/dsda-doom/blob/5608ee441410ecae10a17ecdbe1940bd4e1a2856/prboom2/src/r_defs.h#L124-L213
structs.sector = sector
	.s32  ("iSectorID")
	.u32  ("flags")
	.s32  ("floorheight")
	.s32  ("ceilingheight")
	.u8   ("soundtraversed")
	.ptrto("soundtarget", mobj)
	.array("blockbox", "s32", 4)
	.array("bbox", "s32", 4)
	.embed("soundorg", structs.degenmobj)
	.s32  ("validcount")
	.s32  ("gl_validcount")
	.ptrto("thinglist", mobj) -- start of mobj.snext linked list
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
	.ptrto("touching_thinglist", structs.msecnode) -- start of msecnode.m_snext linked list
	.s32  ("linecount")
	.ptr  ("lines_ptr") -- pointer to pointer to line
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
	.embed("damage", structs.damage)
	.s16  ("lightlevel_floor")
	.s16  ("lightlevel_ceiling")
	.u32  ("floor_rotation")
	.u32  ("ceiling_rotation")
	.s32  ("floor_xscale")
	.s32  ("floor_yscale")
	.s32  ("ceiling_xscale")
	.s32  ("ceiling_yscale")
	.func ("iterate_thinglist", function (self)
		return utils.links(self.thinglist, "snext")
	end)
	.func ("iterate_touching_thinglist", function (self)
		return utils.links(self.touching_thinglist, "m_snext", "m_thing")
	end)
	.prop ("lines", function(self)
		return utils.pointer_array(self.lines_ptr, self._domain, self.linecount, structs.line.from_pointer)
	end)
	.build()

assert(structs.line.size   == structs.SIZE.LINE,   "line.size does not match sizeof(line_t)")
assert(structs.mobj.size   == structs.SIZE.MOBJ,   "mobj.size does not match sizeof(mobj_t)")
assert(structs.player.size == structs.SIZE.PLAYER, "player.size does not match sizeof(player_t)")
assert(structs.sector.size == structs.SIZE.SECTOR, "sector.size does not match sizeof(sector_t)")



structs.globals = utils.global_layout()
	.sym  ("s32",   "gameskill")
	.sym  ("s32",   "gameepisode")
	.sym  ("s32",   "gamemap")
	.sym  ("s32",   "displayplayer")
	-- timing
	.sym  ("s32",   "gametic")
	.sym  ("s32",   "leveltime")
	.sym  ("s32",   "totalleveltimes")
	-- stats
	.sym  ("s32",   "levels_completed")
	.sym  ("s32",   "totalkills")
	.sym  ("s32",   "totallive")
	.sym  ("s32",   "totalitems")
	.sym  ("s32",   "totalsecret")
	-- game/map data
	.sym  ("s32",   "compatibility_level")
	.sym  ("bool",  "heretic")
	.sym  ("bool",  "hexen")
	.sym  ("s32",   "gamemode")
	.sym  ("s32",   "gamemission")
	-- game state
	.sym  ("bool",  "automap_active")
	.sym  ("s32" ,  "gameaction")
	.sym  ("s32",   "gamestate")
	.sym  ("bool",  "in_game")
	.sym  ("bool",  "reachedLevelExit")
	.sym  ("bool",  "reachedGameEnd")
	-- rng
	.sym  ("embed", "rng", structs.rng)
	.sym  ("u32",   "rngseed")
	-- arrays
	.sym  ("array", "thinkerclasscap", "embed", 5, structs.thinker)
	.sym  ("array", "playeringame", "bool", structs.MAX_PLAYERS)
	.sym  ("array", "players", "embed", structs.MAX_PLAYERS, structs.player)
	.sym  ("s32",   "thinker_count") -- for mobj_ptrs
	.sym  ("s32",   "numlines")
	.symas("ptr",   "lines", "lines_ptr")
	.sym  ("s32",   "numsectors")
	.symas("ptr",   "sectors", "sectors_ptr")
	.sym  ("s32",   "po_NumPolyobjs")
	.symas("ptr",   "polyobjs", "polyobjs_ptr")
	.sym  ("s32",   "TaggedLineCount")
	.prop ("mobjs", function(self)
		return utils.pointer_array(symbols.mobj_ptrs, "System Bus", self.thinker_count, structs.mobj.from_pointer)
	end)
	.prop ("lines", function(self)
		return utils.array(self.lines_ptr, "System Bus", self.numlines, structs.line.size, structs.line.from_address_unchecked)
	end)
	.prop ("sectors", function(self)
		return utils.array(self.sectors_ptr, "System Bus", self.numsectors, structs.sector.size, structs.sector.from_address_unchecked)
	end)
	.prop ("polyobjs", function(self)
		return utils.array(self.polyobjs_ptr, "System Bus", self.po_NumPolyobjs, structs.polyobj.size, structs.polyobj.from_address)
	end)
	.prop ("TaggedLines", function(self)
		return utils.array(assert(symbols.TaggedLines), "System Bus", self.TaggedLineCount, structs.taggedline.size, structs.taggedline.from_address)
	end)
	.cfunc("iterate_players", function()
		local function next_player(global, i)
			while i < structs.MAX_PLAYERS do
				i = i + 1
				if global.playeringame[i] then
					return i, global.players[i]
				end
			end
		end
		return function(self)
			return next_player, self or structs.globals, 0
		end
	end)
	.func ("iterate_thinkers", function(self, class)
		if not class then class = 5 end -- th_all
		local iterator, state, start = utils.links(self.thinkerclasscap[class], class == 5 and "next" or "cnext")
		return iterator, state, iterator(state, start) -- iterate once already to skip the cap
	end)
	.build()

return structs
