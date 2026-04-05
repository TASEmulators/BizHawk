-- feos, kalimag, 2025-2026
---@diagnostic disable

--#region MODULES

enums   = require("dsda.enums")
structs = require("dsda.structs")
symbols = require("dsda.symbols")

--#endregion


--#region CONSTANTS

SETTINGS_FILENAME  = "doom.settings.lua"
MAP_CLICK_BLOCK    = "P1 Fire" -- prevent this input while clicking on map buttons
FRACBITS           = 16
FRACUNIT           = 1 << FRACBITS
ANGLE_90           = 0x40000000
MINIMAL_ZOOM       = 0.0001 -- stuff breaks if it's smaller
ZOOM_FACTOR        = 0.01
WHEEL_ZOOM_FACTOR  = 10
DRAG_FACTOR        = 10
PAN_FACTOR         = 10
CHAR_WIDTH         = 10
CHAR_HEIGHT        = 16
PADDING_WIDTH      = 240
PRANDOM_ALL_IN_ONE = 49
GRID_SIZE          = 128
FADEOUT_TIMER      = 20
MAXIMUM_INTERCEPTS = 128
-- initially 12 but we have 64-bit architecture + pointer alignment.
-- when intercept overflow is emulated, the size of 12 is still used internally
-- to corrupt the same target values as in vanilla
INTERCEPT_SIZE         = 16
INTERCEPT_SIZE_VANILLA = 12

--#endregion


--#region ENUMS

---@enum game_state
GameState = {
	LEVEL        = 0,
	INTERMISSION = 1,
	FINALE       = 2,
	DEMOSCREEN   = 3
}
---@enum tracked_type
TrackedType = {
	THING  = 1,
	LINE   = 2,
	SECTOR = 3
}
---@enum angle_type
AngleType = {
	LONGTICS = 16384,
    FINE     = 2048,
    DEGREES  = 90,
    BYTE     = 64
}
---@enum line_log_type
LineLogType = {
	NONE   = 0,
	PLAYER = 1,
	ALL    = 2
}
---@enum intercepts_state
InterceptsState = {
	NONE     = 0,
	PRINT    = 1,
	OVERFLOW = 2
}
---@enum text_pos_y
TextPosY = {
	PLAYER = 42,
	THING  = 236,
	LINE   = 334,
	SECTOR = 416
}
---@enum scroller
Scroller = {
	LEFT  = "< ",
	RIGHT = " >",
	NONE  = "  "
}
---@alias line_t structs.line
---@alias codec_method
---| "encode" Onscreen to in-game
---| "decode" In-game to onscreen
---@alias entity_name
---| "thing"
---| "line"
---| "sector"

--#endregion


--#region CLASSES

--- Closure object for entities we can track. Involves showing them on the screen and saving them to config.
TrackedEntity = {}
---@class (exact) tracked_entity
---@field TrackedList table[] List of individual objects that we're tracking
---@field IDs integer[] List IDs of a given entity type that are currently present in the level
---@field Current integer ID of the currently displayed object
---@field Min integer Lowest tracked ID, for scrolling
---@field Max integer Highest tracked ID, for scrolling
---@field Name string Entity type name to show to user in dialogs

--- Constructor
---@param name entity_name Entity type name to show to user in dialogs
---@return tracked_entity
function TrackedEntity.new(name)
	local self       = {}
	self.TrackedList = {}
	self.IDs         = {}
	self.Current     = nil
	self.Min         = math.maxinteger
	self.Max         = math.mininteger
	self.Name        = name
	--- Removes all tracked objects of this type
	-- TODO: appears in annotations as just nil in calls
	function self.clear()
		self.TrackedList = {}
		self.Current     = nil
		self.Min         = math.maxinteger
		self.Max         = math.mininteger
		settings_write()
	end
	return self
end

--- Just a list of things we display for players
---@class (exact) player
---@field x number
---@field y number
---@field z number
---@field distx number
---@field disty number
---@field distz number
---@field momx number
---@field momy number
---@field distmoved number
---@field dirmoved number
---@field angle integer

--- Players can't be fully deduced from `mobj` list because they will all have -1 `index`, but they have separate predetermined slots in memory, so we display player indices deduced from those the way we display `mobj` indices.
---@class (exact) players
---@field List player[] List of actual player objects
---@field Current integer Index of the currently displayed player
---@field Min integer Lowest present index
---@field Max integer Highest present index

--- Vanilla variables that intercepts overflow would corrupt. `playerstarts` is a `mapthing_t` list in vanilla; nested one in upstream but we're not displaying that detail. Index augend is taken from the original, and index addend exists to indicate lua's 1-based lists.
---@class (exact) intercept_overrun
---@field name string Full name of the vanilla variable we're corrupting
---@field value integer Value that the vanilla variable has at the moment

---@class (exact) intercept_overrun_info
---@field offset string
---@field value string
---@field variable string

--- `intercept_t` plus some extra info for the user.
---@class intercept_info
---@field frac string `frac` is the most complicated part of `intercept_t` struct. When an intercept is checked, traceline length is normalized to [0, 1], and the point where it crosses something denotes the fraction of that length, meaning how soon the traceline hits it. Negative value means behind the origin, and more than 1 means outside the trace range. Then for all the intercepts in the list, those fractions are compared and the shortest one wins. So to manipulate what value is used for memory corruption, we just need to adjust the distance between trace origin and something it hits, keeping in mind intercept at which index/offset matches our target address. Internally represented as 32-bit integer, basically means percentage of traceline length if we multiply the value by 100.
---@field isaline string Whether intercept is with a line or a thing. Internally represented as 32-bit integer.
---@field offset string How far into corrupted memory we are. Informational addition.
---@field pointer integer `d` field of `intercept_t`. Not super relevant outside vanilla executable. Current codebase makes it a 64-bit integer and when intercept overruns are emulated it's just truncated to 32 bits.
---@field block integer Blockmap block where the intercept happened.
---@field id integer `iLineID` or `index`, depending on `isaline`.

--- Used for specific things like point coordinates, but also for anything that can have x/y values
---@class (exact) vertex
---@field x number
---@field y number

--- Depending on the situation this object is more useful than a tuple of indiviaual coords
---@class (exact) line
---@field v1 vertex Starting point of the line
---@field v2 vertex End point of the line

--- Dialog info for removal of tracked entity
---@class (exact) confirmation
---@field type tracked_type
---@field id integer

---@class (exact) current_prompt
---@field msg string Message to show in the dialog that adds tracked entity
---@field value integer Currently typed value
---@field fun fun(id: integer) Callback that actually adds tracked entity

--- Which thing types to display on the map and how
---@class (exact) map_pref
---@field color luacolor
---@field radius_min_zoom number
---@field text_min_zoom number

--- Thing angle display via rotating triangle
---@class triangle
---@field a vertex
---@field b vertex
---@field c vertex
---@field center vertex

--#endregion


-- shortcuts
text     = gui.text
box      = gui.drawBox
drawline = gui.drawLine


--#region TOP LEVEL VARIABLES

Defaults = {
	Zoom           = 1,
	PanX           = 0,
	PanY           = 0,
	ShowMap        = false,
	ShowGrid       = false,
	Follow         = false,
	Hilite         = false,
	InterceptLimit = MAXIMUM_INTERCEPTS,
	---@type angle_type
	Angle = AngleType.BYTE,
}

Init           = true
Globals        = structs.globals
MobjType       = enums.mobjtype
SpriteNumber   = enums.doom.spritenum
MobjFlags      = enums.mobjflags
ScreenWidth    = client.screenwidth()
ScreenHeight   = client.screenheight()
BlockmapWidth  = 0
InterceptPtr   = 0
InterceptLog   = false
InterceptShow  = false
RNGLog         = false
Framecount     = 0
LastFramecount = -1
Input          = nil
Lines          = nil
PlayerTypes    = nil
EnemyTypes     = nil
MissileTypes   = nil
MiscTypes      = nil
InertTypes     = nil
LastEpisode    = nil
LastMap        = nil
LastInput      = nil
LastBMWidth    = nil
LastBMOrigin   = nil
LastBMEnd      = nil
---@type current_prompt
CurrentPrompt = nil
---@type confirmation
Confirmation = nil
---@type intercepts_state
InterceptsInfo = InterceptsState.NONE
---@type line_log_type
LineUseLog = LineLogType.NONE
---@type line_log_type
LineCrossLog = LineLogType.NONE


-- SAVED TO CONFIG
Zoom           = nil
Follow         = nil
Hilite         = nil
ShowMap        = nil
ShowGrid       = nil
---@type angle_type
Angle          = nil
InterceptLimit = nil
--- View offset
---@type vertex
Pan = {
	x = Defaults.PanX,
	y = Defaults.PanY
}
---@type tracked_entity[]
Tracked = {
	[TrackedType.THING ] = TrackedEntity.new("thing" ),
	[TrackedType.LINE  ] = TrackedEntity.new("line"  ),
	[TrackedType.SECTOR] = TrackedEntity.new("sector"),
}


-- TABLES
Config      = {}
PRandomInfo = {}
DivLines    = {}
MapBlocks   = {}
GUITexts    = {}

---@type players
Players = {}
--- Intercept objects per block
---@type table<number, intercept_info[]>
Intercepts = {}
---@type table<number, intercept_overrun_info[]>
InterceptsOverruns = {}
-- map object positions bounds
OB = {
	top    = math.maxinteger,
	left   = math.maxinteger,
	bottom = math.mininteger,
	right  = math.mininteger
}
BlockmapOrigin = {
	x = 0,
	y = 0
}
BlockmapEnd = { 
	x = 0,
	y = 0
}
LastScreenSize = {
	w = client.screenwidth(),
	h = client.screenheight()
}
LastMouse = {
	x     = 0,
	y     = 0,
	wheel = 0,
	left  = false
}
-- map colors (0xAARRGGBB or "name")
---@type table<string, map_pref>
MapPrefs = {
	player      = { color = 0xff60d0ff, radius_min_zoom = 0.00, text_min_zoom = 0.50, },
	enemy       = { color = 0xffff0000, radius_min_zoom = 0.00, text_min_zoom = 0.75, },
	enemy_idle  = { color = 0xffaa0000, radius_min_zoom = 0.00, text_min_zoom = 1.00, },
	missile     = { color = 0xffff8000, radius_min_zoom = 0.00, text_min_zoom = 1.00, },
	shootable   = { color = 0xffaaaaaa, radius_min_zoom = 0.00, text_min_zoom = 1.00, },
	countitem   = { color = 0xffffff00, radius_min_zoom = 0.00, text_min_zoom = 1.50, },
	item        = { color = 0xff00ff00, radius_min_zoom = 0.00, text_min_zoom = 1.50, },
	highlight   = { color = 0xffff00ff, radius_min_zoom = 0.00, text_min_zoom = 0.20, },
	grid        = { color = 0xff808080, radius_min_zoom = 0.00, text_min_zoom = 0.00, },
	solid       = { color = 0xff505050, radius_min_zoom = 0.75, text_min_zoom = false,},
--	corpse      = { color = 0xaaaaaaaa, radius_min_zoom = 0.00, text_min_zoom = 0.75, },
--	misc        = { color = 0xffa0a0a0, radius_min_zoom = 0.75, text_min_zoom = 1.00, },
--	inert       = { color = 0x80808080, radius_min_zoom = 0.75, text_min_zoom = false,},
}

--#endregion


gui.use_surface("client")
client.SetClientExtraPadding(PADDING_WIDTH, 0, 0, 0)

--#region TOGGLES

function follow_toggle()
	Follow = not Follow
end

function hilite_toggle()
	Hilite = not Hilite
end

function map_show()
	ShowMap = not ShowMap
end

function grid_show()
	ShowGrid = not ShowGrid
end

--- After intercept overflow, shows which variables got corrupted and their resulting values. The list is consctructed on the fly so we could read from memory directly.
---@param limit integer
---@return intercept_overrun_info[] # Table index indicates offset, usable for comparing with offsets of individual intercepts after overflow
local function fetch_intercept_overruns(limit)
	---@type intercept_overrun_info[]
	local ret = {}
	---@type intercept_overrun[]
	local list = {
		[ 12] = { name = "line_opening.lowfloor",   value = Globals.line_opening.lowfloor     },
		[ 16] = { name = "line_opening.bottom",     value = Globals.line_opening.bottom       },
		[ 20] = { name = "line_opening.top",        value = Globals.line_opening.top          },
		[ 24] = { name = "line_opening.range",      value = Globals.line_opening.range        },
		[160] = { name = "bulletslope",             value = Globals.bulletslope               },
		[176] = { name = "playerstarts[0].x",       value = Globals.playerstarts[0+1].x       },
		[178] = { name = "playerstarts[0].y",       value = Globals.playerstarts[0+1].y       },
		[180] = { name = "playerstarts[0].angle",   value = Globals.playerstarts[0+1].angle   },
		[182] = { name = "playerstarts[0].type",    value = Globals.playerstarts[0+1].type    },
		[184] = { name = "playerstarts[0].options", value = Globals.playerstarts[0+1].options },
		[186] = { name = "playerstarts[1].x",       value = Globals.playerstarts[1+1].x       },
		[188] = { name = "playerstarts[1].y",       value = Globals.playerstarts[1+1].y       },
		[190] = { name = "playerstarts[1].angle",   value = Globals.playerstarts[1+1].angle   },
		[192] = { name = "playerstarts[1].type",    value = Globals.playerstarts[1+1].type    },
		[194] = { name = "playerstarts[1].options", value = Globals.playerstarts[1+1].options },
		[196] = { name = "playerstarts[2].x",       value = Globals.playerstarts[2+1].x       },
		[198] = { name = "playerstarts[2].y",       value = Globals.playerstarts[2+1].y       },
		[200] = { name = "playerstarts[2].angle",   value = Globals.playerstarts[2+1].angle   },
		[202] = { name = "playerstarts[2].type",    value = Globals.playerstarts[2+1].type    },
		[204] = { name = "playerstarts[2].options", value = Globals.playerstarts[2+1].options },
		[206] = { name = "playerstarts[3].x",       value = Globals.playerstarts[3+1].x       },
		[208] = { name = "playerstarts[3].y",       value = Globals.playerstarts[3+1].y       },
		[210] = { name = "playerstarts[3].angle",   value = Globals.playerstarts[3+1].angle   },
		[212] = { name = "playerstarts[3].type",    value = Globals.playerstarts[3+1].type    },
		[214] = { name = "playerstarts[3].options", value = Globals.playerstarts[3+1].options },
		[220] = { name = "bmapwidth",               value = Globals.bmapwidth                 },
		[228] = { name = "bmaporgx",                value = Globals.bmaporgx                  },
		[232] = { name = "bmaporgy",                value = Globals.bmaporgy                  },
		[230] = { name = "bmapheight",              value = Globals.bmapheight                }
	}
	
	-- walk through the list to check how far corruption went for this particular intercept
	for i = 0, 230 do
		local source = list[i]
		if source and i <= limit then
			---@type intercept_overrun_info
			local item = {
				offset   = string.format("%d bytes", i),
				value    = string.format("0x%X", source.value & 0xffffffff),
				variable = source.name
			}
			table.insert(ret, item)
		end
	end
	
	return ret
end

--- When the amount of intercepts per block exceeds user defined value, or if the overflow has happened, we print that and let the user dump all their contents and info to console.
---@param block integer
local function intercept_logger(block)
	if InterceptLog then
		local origin = Globals.intercepts
		local count  = math.floor((InterceptPtr - origin) / INTERCEPT_SIZE)
		
		if count > InterceptLimit then
			local text
			local i = 1 -- intercept #0 gets printed last so we start with 1 instead
			
			if count > MAXIMUM_INTERCEPTS then
				text = string.format(
					"tic %d, block %d, %d intercepts INTERCEPT OVERFLOW",
					Globals.gametic, block, count
				)
				block = -block -- custom way to indicate overflow
				InterceptsInfo = InterceptsState.OVERFLOW
			
				if not InterceptsOverruns[math.abs(block)] then
					InterceptsOverruns[math.abs(block)] = {}
				end
				InterceptsOverruns[math.abs(block)] = fetch_intercept_overruns(
					(count - MAXIMUM_INTERCEPTS)
					* INTERCEPT_SIZE_VANILLA
					+ INTERCEPT_SIZE_VANILLA
				)
				
				client.pause()
			else
				text = string.format(
					"tic %d, block %d, %d intercepts",
					Globals.gametic, block, count
				)
				InterceptsInfo = InterceptsState.PRINT
			end
			
			print(text)
			
			if not Intercepts[math.abs(block)] then
				Intercepts[math.abs(block)] = {}
			end
			
			for address = origin, InterceptPtr - INTERCEPT_SIZE, INTERCEPT_SIZE do
				local intercept = structs.intercept.from_pointer(address)
				---@type intercept_info
				local object = {
					frac    = string.format("0x%08x", intercept.frac),
					isaline = string.format("0x%08x", intercept.isaline),
					offset  = string.format("%d bytes", 
						(i-1-MAXIMUM_INTERCEPTS)
						*INTERCEPT_SIZE_VANILLA),
					pointer = intercept.d,
					block   = math.abs(block)
				}
				
				if tonumber(intercept.isaline) == 1 then
					object.id = structs.line.from_pointer(object.pointer).iLineID
				else
					object.id = structs.mobj.from_pointer(object.pointer).index
				end

				if InterceptsInfo ~= InterceptsState.OVERFLOW then
					object.offset = "N/A"
				end
				
				object.pointer = string.format("0x%08X", object.pointer)
				
				-- we insert the same interecepts over and over for every new call,
				-- because we can't know when they'll end,
				-- and we may be asked to do this before it actually overflows.
				-- so we can't just sit and wait for an overflow and only
				-- then build the list. there won't be thousands of them anyway.
				Intercepts[math.abs(block)][i] = object
				
				i = i + 1
			end
		end
	end
end

--- Very complicated thing that handles tracelines display and intercepts display and logging. Installs the hook while anything is enabled. When an intercept is added by the game, we read `trace` from memory which is the thing creating intercepts, and we add all those tracelines to a table that we then display once per frame.
local function hook_intercepts()
	local name = "Intercepts"
	
	if InterceptLog or InterceptShow then
		doom.on_intercept(function(block)
			local intercept_p = Globals.intercept_p
			
			if ShowMap and InterceptShow then
				-- fetch traceline while at it
				local divline = Globals.trace
				local key = string.format(
					"%d %d %d %d",
					divline.x,  divline.y,
					divline.x + divline.dx,
					divline.y + divline.dy
				)
				DivLines[key] = Framecount + FADEOUT_TIMER
			end
			
			if intercept_p ~= InterceptPtr then
				-- new intercept was just added
				InterceptPtr = intercept_p

				intercept_logger(block)
				
				if ShowMap and ShowGrid and InterceptShow then
					MapBlocks[block] = Framecount + FADEOUT_TIMER
				end
			end
			
		end, name)
	else
		event.unregisterbyname(name)
	end
end

function intercept_log()
	if Globals.compatibility_level < 7 then
		InterceptLog = not InterceptLog
		hook_intercepts()
		
		if InterceptLog then
			print("Logging intercepts beyond " .. InterceptLimit .. "...")
		end
	else
		print("Boom fixed intercept overflow! No point in logging it.")
	end
end

function intercept_show()
	InterceptShow = not InterceptShow
	hook_intercepts()
end

function prandom_log()
	RNGLog = not RNGLog
	local name = "PRandom"
	
	if RNGLog then
		doom.on_prandom(function(info)
			if not RNGLog then return end
			
			local seed = ""
			
			if Globals.compatibility_level >= 7 then
				seed = string.format("%010u",
					Globals.rng.seed[PRANDOM_ALL_IN_ONE+1]
				)
			else
				seed = string.format("%03d",
					memory.readbyte(memory.read_u32_le(symbols.rndtable) + Globals.rng.rndindex)
				)
			end
			
			table.insert(PRandomInfo, string.format(
				"%d (%d): #%03d %s %s",
				Globals.gametic, #PRandomInfo+1, Globals.rng.rndindex, seed, info
			))
		 end, name)
	else
		event.unregisterbyname(name)
	end
end

--- Does the actual logging for cross/use event
---@param event string How to call the event in the log
---@param line integer Pointer to line that we got from the hook
---@param thing integer Pointer to mobj that we got from the hook
local function line_event(event, line, thing)
	line  = line  - 0x36f00000000
	thing = thing - 0x36f00000000
	
	for i, player in pairs(Players.List) do
		if player.thinker == thing then
			thing = "player " .. i
			break
		end
	end
	
	if type(thing) ~= "string" -- thing is not player
	and ((LineUseLog   == LineLogType.ALL and event == "USED")
	or   (LineCrossLog == LineLogType.ALL and event == "CROSSED"))
	then
		local mobj = structs.mobj.from_pointer(thing)
		thing = "thing " .. mobj.index
	end
	
	if type(thing) == "string" then
		print(string.format(
			"tic %d: line %d %s by %s",
			Globals.gametic - 1,
			memory.read_s32_le(line, "System Bus"),
			event, thing
		))
	end
end

--- Decides what to log exactly for cross/use events and installs the hook accordingly. When nothing is to be logged, the hook is removed.
---@param isUse boolean Indicates event type we're cycling though
function cycle_log_types(isUse)
	if isUse then
		local name = "Use"
		LineUseLog = (LineUseLog + 1) % (LineLogType.ALL + 1)
		event.unregisterbyname(name)
		
		if LineUseLog ~= LineLogType.NONE then
			doom.on_use(function(line, thing)
				if LineUseLog ~= LineLogType.NONE
				then line_event("USED", line, thing)
				end
			end, name)
		end
	else
		local name = "Cross"
		LineCrossLog = (LineCrossLog + 1) % (LineLogType.ALL + 1)
		event.unregisterbyname(name)
		
		if LineCrossLog ~= LineLogType.NONE then
			doom.on_cross(function(line, thing)
				if LineCrossLog ~= LineLogType.NONE
				then line_event("CROSSED", line, thing)
				end
			end, name)
		end
	end
end

--#endregion


--#region GAME/SCREEN CODECS

--- Converts in-game coordinate into onscreen
---@param coord number
---@return integer
local function decode_x(coord)
	return math.floor(((coord / FRACUNIT) + Pan.x) * Zoom)
end

--- Converts in-game coordinate into onscreen
---@param coord number
---@return integer
local function decode_y(coord)
	return math.floor(((-coord / FRACUNIT) + Pan.y) * Zoom)
end

--- Converts onscreen coordinate into in-game
---@param coord number
---@return integer
local function encode_x(coord)
	return math.floor(((coord / Zoom) - Pan.x) * FRACUNIT)
end

--- Converts onscreen coordinate into in-game
---@param coord number
---@return integer
local function encode_y(coord)
	return -math.floor(((coord / Zoom) - Pan.y) * FRACUNIT)
end

--- Converter between screen-to-game and game-to-screen coordinates/vertex/line. Return type matches passed type.
---@overload fun(method: codec_method, l: line): line
---@overload fun(method: codec_method, v: vertex): vertex
---@overload fun(method: codec_method, v1: vertex, v2: vertex): vertex, vertex
---@overload fun(method: codec_method, coord: number): integer
---@param method codec_method
---@param arg1 number
---@param arg2 number
---@param arg3 number
---@param arg4 number
---@return integer
---@return integer
---@return integer
---@return integer
local function codec(method, arg1, arg2, arg3, arg4)
	local func_x, func_y
	
	if method == "encode" then
		func_x = encode_x
		func_y = encode_y
	elseif method == "decode" then
		func_x = decode_x
		func_y = decode_y
	end
	
	-- all 4 args passed
	if arg1 and arg2 and arg3 and arg4 then
	
		-- line as 4 coords
		return func_x(arg1), func_y(arg2), func_x(arg3), func_y(arg4)
	
	-- only 2 args passed
	elseif arg1 and arg2 and not (arg3 or arg4) then
		if type(arg1) == "table" and type(arg2) == "table"
		and arg1.x and arg1.y and arg2.x and arg2.y then
		
			-- line as 2 vertices
			return
				{ x = func_x(arg1.x), y = func_y(arg1.y) },
				{ x = func_x(arg2.x), y = func_y(arg2.y) }
		else
		
			-- vertex as 2 coords
			return func_x(arg1), func_y(arg2)
		end
	
	-- only 1 arg passed
	elseif arg1 and not (arg2 or arg3 or arg4) and type(arg1) == "table" then
		if arg1.v1 and arg1.v2 then
			if type(arg1.v1) == "table" and type(arg1.v2) == "table"
			and arg1.v1.x and arg1.v1.y and arg1.v2.x and arg1.v2.y then
			
				-- line
				return {
					v1 = { x = func_x(arg1.v1.x), y = func_y(arg1.v1.y) },
					v2 = { x = func_x(arg1.v2.x), y = func_y(arg1.v2.y) }
				}
			end
		elseif arg1.x and arg1.y then
			
			-- vertex
			return { x = func_x(arg1.x), y = func_y(arg1.y) }
		end
	end
end

--- Converts in-game coordinates into onscreen. Return type matches passed type.
---@overload fun(l: line): line
---@overload fun(v: vertex): vertex
---@overload fun(v1: vertex, v2: vertex): vertex, vertex
---@overload fun(coord: number): integer
---@param x1 number
---@param y1 number
---@param x2 number
---@param y2 number
---@return integer x1
---@return integer y1
---@return integer x2
---@return integer y2
function game_to_screen(x1, y1, x2, y2)
	return codec("decode", x1, y1, x2, y2)
end

--- Converts onscreen coordinates into in-game. Return type matches passed type.
---@overload fun(l: line): line
---@overload fun(v: vertex): vertex
---@overload fun(v1: vertex, v2: vertex): vertex, vertex
---@overload fun(coord: number): integer
---@param x1 number
---@param y1 number
---@param x2 number
---@param y2 number
---@return integer x1
---@return integer y1
---@return integer x2
---@return integer y2
function screen_to_game(x1, y1, x2, y2)
	return codec("encode", x1, y1, x2, y2)
end

--#endregion


--#region TYPE CONVERTERS

---@param x number
---@param y number
---@return vertex
function tuple_to_vertex(x, y)
	return { x = x, y = y }
end

---@param v vertex
---@return integer x
---@return integer y
function vertex_to_tuple(v)
	return table.unpack(v)
end

---@param x1 number
---@param y1 number
---@param x2 number
---@param y2 number
---@return line
function tuple_to_line(x1, y1, x2, y2)
	return {
		v1 = { x = x1, y = y1 },
		v2 = { x = x2, y = y2 }
	}
end

---@param l line
---@return integer x1
---@return integer y1
---@return integer x2
---@return integer y2
function line_to_tuple(l)
	return table.unpack(l.v1), table.unpack(l.v2)
end

--#endregion


--#region MATH

--- Returns 2 passed numbers in order from smaller to bigger. Expands them further apart by 200, because this is meant to be used by `reset_view()` and to have a bit more space around visible objects.
---@param smaller number
---@param bigger number
---@return number smaller
---@return number bigger
local function maybe_swap(smaller, bigger)
	if smaller > bigger then
		return bigger, smaller
	end
	return smaller - 100, bigger + 100
end

---@param var number
---@param minimum number
---@param maximum number
---@return boolean
function in_range(var, minimum, maximum)
	return var >= minimum and var <= maximum
end

---@param point vertex
---@param v1 vertex
---@param v2 vertex
---@return boolean
local function check_side(point, v1, v2)
	return ((v2.y - v1.y) / (v2.x - v1.x)) * (point.x - v1.x) + v1.y < point.y
end

--- Distance to point projecton on infinite line
---@param point vertex
---@param v1 vertex
---@param v2 vertex
---@return number # Sign indicates which side the point is on
function distance_to_line(point, v1, v2)
	local PAx = v1.x - point.x
	local PAy = v1.y - point.y
	local ABx = v2.x - v1.x
	local ABy = v2.y - v1.y
	local t = -PAx * ABx + -PAy * ABy
	t = t / (ABx * ABx + ABy * ABy)
	local PXx = PAx + t * ABx;
	local PXy = PAy + t * ABy;
	local dist = math.sqrt(PXx * PXx + PXy * PXy)

	if check_side(point, v1, v2) then
		return -dist
	end

	return dist
end

local function dist_sq(p1, p2)
    return (p1.x - p2.x)^2 + (p1.y - p2.y)^2
end

--- Distance to closest point of the segment
---@param point vertex
---@param v1 vertex
---@param v2 vertex
---@return number # Sign indicates which side the point is on
function distance_to_segment(point, v1, v2)
	local ab_sq = dist_sq(v1, v2)
	if ab_sq == 0 then return math.sqrt(dist_sq(point, v1)) end
	local t =
		((point.x - v1.x) * (v2.x - v1.x) +
		 (point.y - v1.y) * (v2.y - v1.y)) / ab_sq
	t = math.max(0, math.min(1, t))
	local closestPoint = {
		x = v1.x + t * (v2.x - v1.x),
		y = v1.y + t * (v2.y - v1.y)
	}
	local dist = math.sqrt(dist_sq(point, closestPoint))

	if check_side(point, v1, v2) then
		return -dist
	end

	return dist
end

--- Rotate triangle around its center
---@param t triangle
---@param angle integer
---@return triangle
function rotate_triangle(t, angle)
	local rad = (angle * math.pi) / 180.0;
	local newt = { a = {}, b = {}, c = {}, center = t.center }
	newt.a.x = (t.a.x-t.center.x)*math.cos(rad)-(t.a.y-t.center.y)*math.sin(rad)+t.center.x
	newt.a.y = (t.a.x-t.center.x)*math.sin(rad)+(t.a.y-t.center.y)*math.cos(rad)+t.center.y
	newt.b.x = (t.b.x-t.center.x)*math.cos(rad)-(t.b.y-t.center.y)*math.sin(rad)+t.center.x
	newt.b.y = (t.b.x-t.center.x)*math.sin(rad)+(t.b.y-t.center.y)*math.cos(rad)+t.center.y
	newt.c.x = (t.c.x-t.center.x)*math.cos(rad)-(t.c.y-t.center.y)*math.sin(rad)+t.center.x
	newt.c.y = (t.c.x-t.center.x)*math.sin(rad)+(t.c.y-t.center.y)*math.cos(rad)+t.center.y
	return newt
end

--#endregion


--#region AUTOMAP

---@param divider integer
function pan_left(divider)
	Pan.x = Pan.x + PAN_FACTOR/Zoom/(divider or 2)
end

---@param divider integer
function pan_right(divider)
	Pan.x = Pan.x - PAN_FACTOR/Zoom/(divider or 2)
end

---@param divider integer
function pan_up(divider)
	Pan.y = Pan.y + PAN_FACTOR/Zoom/(divider or 2)
end

---@param divider integer
function pan_down(divider)
	Pan.y = Pan.y - PAN_FACTOR/Zoom/(divider or 2)
end

---@param factor integer
---@param mouseCenter boolean
local function zoom(factor, mouseCenter)
	local mouse
	local mousePos
	local zoomCenter
	local direction = 1
	factor = factor or 1
	
	if Follow then mouseCenter = false end
	
	if factor < 0 then
		direction = -1
		factor = -factor
	end
	
	if mouseCenter then
		mouse      = input.getmouse()
		mousePos   = client.transformPoint(mouse.X, mouse.Y)
		zoomCenter = screen_to_game(mousePos)
	else
		zoomCenter = screen_to_game({
			x = ScreenWidth /2,
			y = ScreenHeight/2
		})
	end
	
	for i=0, factor do
		local newZoom = Zoom + Zoom * ZOOM_FACTOR * direction
		if newZoom < MINIMAL_ZOOM then return end
		Zoom = newZoom
	end
	
	zoomCenter.x = (encode_x(mouseCenter and mousePos.x or ScreenWidth /2)-zoomCenter.x)
	zoomCenter.y = (encode_y(mouseCenter and mousePos.y or ScreenHeight/2)-zoomCenter.y)
	Pan.x = Pan.x + zoomCenter.x / FRACUNIT
	Pan.y = Pan.y - zoomCenter.y / FRACUNIT
end

function update_zoom()
	local mousePos   = client.transformPoint(Mouse.X, Mouse.Y)
	local mouseWheel = math.floor(Mouse.Wheel/120)
	local deltaX     = mousePos.x - LastMouse.x
	local deltaY     = mousePos.y - LastMouse.y
	local deltaWheel = mouseWheel - LastMouse.wheel
	
	if deltaWheel ~= 0 and not Init then
		if mousePos.x > PADDING_WIDTH then
			zoom(deltaWheel * WHEEL_ZOOM_FACTOR, true)
		elseif in_range(mousePos.y, TextPosY.PLAYER, TextPosY.THING) then
			scroll_list(Players, -deltaWheel)
		elseif in_range(mousePos.y, TextPosY.THING, TextPosY.LINE) then
			scroll_list(Tracked[TrackedType.THING], -deltaWheel)
		elseif in_range(mousePos.y, TextPosY.LINE, TextPosY.SECTOR) then
			scroll_list(Tracked[TrackedType.LINE], -deltaWheel)
		elseif in_range(mousePos.y, TextPosY.SECTOR, TextPosY.SECTOR+64) then
			scroll_list(Tracked[TrackedType.SECTOR], -deltaWheel)
		end
	end
	
	if input.get()["Space"] then
		if deltaX ~= 0 then pan_left(DRAG_FACTOR/deltaX) end
		if deltaY ~= 0 then pan_up  (DRAG_FACTOR/deltaY) end
	end
	
	LastMouse.x     = mousePos.x
	LastMouse.y     = mousePos.y
	LastMouse.wheel = mouseWheel
	
	if Follow and Globals.gamestate == GameState.LEVEL then
		local player       = Players.List[Players.Current]
		local screenCenter = screen_to_game({
			x = (ScreenWidth+PADDING_WIDTH)/2,
			y = ScreenHeight/2
		})
		
		screenCenter.x = screenCenter.x / FRACUNIT - player.x
		screenCenter.y = screenCenter.y / FRACUNIT - player.y
		Pan.x = Pan.x + screenCenter.x
		Pan.y = Pan.y - screenCenter.y
	end

	if Config.Zoom then Init = false end
	if not Init    then return       end
	
	if  OB.top    ~= math.maxinteger
	and OB.left   ~= math.maxinteger
	and OB.right  ~= math.mininteger
	and OB.bottom ~= math.mininteger
	and not emu.islagged()
	then
		OB.left, OB.right  = maybe_swap(OB.left, OB.right)
		OB.top,  OB.bottom = maybe_swap(OB.top,  OB.bottom)
		local span         = { x = OB.right-OB.left,                   y = OB.bottom-OB.top    }
		local scale        = { x = (ScreenWidth-PADDING_WIDTH)/span.x, y = ScreenHeight/span.y }
		      Zoom         = math.min(scale.x, scale.y)
		local spanCenter   = { x = OB.left+span.x/2,                   y = OB.top+span.y/2     }
		local sreenCenter  = { x = (ScreenWidth+PADDING_WIDTH)/Zoom/2, y = ScreenHeight/Zoom/2 }
		
		if not Follow then
			Pan.x = -math.floor(spanCenter.x - sreenCenter.x)
			Pan.y = -math.floor(spanCenter.y - sreenCenter.y)
		end
		
		Init = false
	end
end

function reset_view()
	OB = {
		top    = math.maxinteger,
		left   = math.maxinteger,
		bottom = math.mininteger,
		right  = math.mininteger
	}
	Init        = true
	Config.Zoom = nil
	update_zoom()
end

--#endregion


--#region UTIL

--- Converts an object into a table-like string that can be saved to file and parsed back into the same object
---@param o any Object to print out
---@param indent? string Current indentation for recursion
---@return string
function dump(o, indent)
	local offset = ""
	if not indent then
		offset = ""
		indent = 0
	end
	for i = 1, indent do
		offset = offset .. "\t"
	end
	if type(o) == 'table' then
		local s = '{'
		for k,v in pairs(o) do
			if type(k) ~= 'number' then k = '"'..k..'"' end
			if type(v) == 'string' then v = '"'..v..'"' end
			s = string.format("%s\n%s\t[%s] = %s,", s, offset, k, dump(v, indent+1))
		end
		return s .. '\n' .. offset .. '}'
	else
		if type(o) == 'number' then o = string.format("%d", o) end
		return tostring(o)
	end
end

---@param tab table
---@return table
function to_lookup(tab)
	local lookup = {}
	for k, v in pairs(tab) do
		lookup[v] = k
	end
	return lookup
end

---@param str string
---@return integer lines How many lines we detected
---@return integer chars How many chars the longest line is
local function get_line_count(str)
	local count   = 1
	local longest = 0
	local size    = 0
	for i = 1, #str do
		local c = str:sub(i, i)
		if c == '\n' then
			count = count + 1
			if size > longest then
				longest = size
			end
			size = -1
		end
		size = size + 1
	end
	if size > longest then longest = size end
	return count, longest
end

--- Checks if a key has just been pressed by user. Keys that remain pressed from before are ignored.
---@param key string User input key to check
---@return boolean
function check_press(key)
	return Input[key] and not LastInput[key]
end

--- Refreshes things when a map changes
function check_map_change()
	local episode = Globals.gameepisode
	local map     = Globals.gamemap
	
	if Globals.gamestate ~= GameState.LEVEL
	or (LastEpisode and LastMap and (episode ~= LastEpisode or map ~= LastMap)) then
		clear_cache()
		reset_view()
	end

	LastEpisode, LastMap = episode, map
end

--#endregion


--#region IO

--- Deserializer
local function settings_read()
	local file, err = loadfile(SETTINGS_FILENAME, "t", Config)
	if file then
		file()
	else
	--	print(err)
	end
	
	-- ANGLE TYPE
	Angle = Config.Angle or Defaults.Angle
	
	-- INTERCEPTS
	InterceptLimit = Config.InterceptLimit or Defaults.InterceptLimit
	
	-- MAP STATE
	if not Config.Zoom
	or not Config.PanX
	or not Config.PanY
	or Config.Follow == nil
	then
		reset_view()
	end
	Zoom     = Config.Zoom     or Defaults.Zoom
	Pan.x    = Config.PanX     or Defaults.PanX
	Pan.y    = Config.PanY     or Defaults.PanY
	ShowMap  = Config.ShowMap  or Defaults.ShowMap
	ShowGrid = Config.ShowGrid or Defaults.ShowGrid
	Follow   = Config.Follow   or Defaults.Follow
	Hilite   = Config.Hilite   or Defaults.Hilite
	
	-- TRACKED ENTITIES
	if not Config.tracked then return end
	for _,ttype in pairs(TrackedType) do
		local entity   = Tracked[ttype]
		local source   = Config.tracked[entity.Name]
		entity.Current = source.Current
		entity.Min     = math.maxinteger
		entity.Max     = math.mininteger

		if entity.Current then
			if ttype == TrackedType.LINE then
				for _,v in pairs(source.TrackedList) do
					for _, line in pairs(Globals.lines) do
						if v == line.iLineID then
							entity.TrackedList[v] = line

							if v < entity.Min then entity.Min = v end
							if v > entity.Max then entity.Max = v end
						end
					end
				end
			elseif ttype == TrackedType.SECTOR then
				for _,v in pairs(source.TrackedList) do
					for _, sector in pairs(Globals.sectors) do
						if v == sector.iSectorID then
							entity.TrackedList[v] = sector

							if v < entity.Min then entity.Min = v end
							if v > entity.Max then entity.Max = v end
						end
					end
				end
			elseif ttype == TrackedType.THING then
				for _,v in pairs(source.TrackedList) do
					for _, mobj in pairs(Globals.mobjs:readbulk()) do
						if v == mobj.index then
							entity.TrackedList[v] = mobj

							if v < entity.Min then entity.Min = v end
							if v > entity.Max then entity.Max = v end
						end
					end
				end
			end

			if not entity.TrackedList[entity.Current]
			and entity.Min ~= math.maxinteger
			then
				entity.Current = entity.Min
			end

			if entity.Min == math.maxinteger then
				entity.Current = nil
			end
		end
	end
end

--- Serializer
function settings_write()
	local file, err = io.open(SETTINGS_FILENAME, "w")
	if file then
		-- ANGLE TYPE
		file:write("-- available angle types:\n")
		for k,v in pairs(AngleType) do
			file:write(string.format("-- %5d (%s)\n", v, k))
		end
		file:write("Angle = " .. Angle .. "\n")
		file:write("\n")
		
		-- INTERCEPTS
		file:write("-- 128 intercepts is when intercept overflow happens in vanilla\n")
		file:write("-- but you can set it to lower values to make the script report them too\n")
		file:write("InterceptLimit = " .. InterceptLimit .. "\n")
		file:write("\n")
		
		-- MAP STATE
		file:write(    "Zoom = " ..          Zoom      .. "\n")
		file:write(    "PanX = " ..          Pan.x     .. "\n")
		file:write(    "PanY = " ..          Pan.y     .. "\n")
		file:write(  "Follow = " .. tostring(Follow)   .. "\n")
		file:write(  "Hilite = " .. tostring(Hilite)   .. "\n")
		file:write( "ShowMap = " .. tostring(ShowMap)  .. "\n")
		file:write("ShowGrid = " .. tostring(ShowGrid) .. "\n")
		file:write("\n")
		
		-- TRACKED ENTITIES
		file:write("-- keys in TrackedList are internal and meaningless, values hold actual IDs of tracked entities\n")
		local tracked = {}
		
		for _,t in pairs(TrackedType) do
			local entity        = Tracked[t]
			local name          = entity.Name
			tracked[name]       = {}
			local setting       = tracked[name]
			setting.TrackedList = {}
			setting.Current     = entity.Current
			
			for k,_ in pairs(entity.TrackedList) do
				table.insert(setting.TrackedList, k)
			end
			
			table.sort(setting.TrackedList)
		end
		
		file:write("tracked = " .. dump(tracked) .. "\n")
		file:write("\n")
	else
		print(err)
	end
	file:close()
end

--#endregion


--#region CACHE

---@param line line_t
---@return number x1
---@return number y1
---@return number x2
---@return number y2
function cached_line_coords(line)
	if line._polyobj then
		local validcount = line.validcount
		if validcount ~= line._validcount then
			line._validcount = validcount
			local x1, y1 = line.v1:coords()
			local x2, y2 = line.v2:coords()
			line._coords = { x1, y1, x2, y2 }
		end
	end
	return table.unpack(line._coords)
end

function init_cache()
	if Lines then return end

	local polyobj_lines = {}
	local polyobjs = Globals.polyobjs
	if polyobjs then
		for _, polyobj in ipairs(polyobjs) do
			for _, seg in ipairs(polyobj.segs:readbulk()) do
				polyobj_lines[seg.linedef.iLineID] = true
			end
		end
	end

	local tagged_lines = {}
	for _, taggedline in ipairs(Globals.TaggedLines) do
		tagged_lines[taggedline.line.iLineID] = true
	end

	Lines = {}
	for _, line in pairs(Globals.lines) do
		-- selectively cache certain properties. by assigning them manually the read function won't be called again

		local lineId = line.iLineID
		Tracked[TrackedType.LINE].IDs[lineId] = true

		-- assumption: lines can't become special, except for script command CmdSetLineSpecial
		-- exclude lines that have a line id set (and therefore can be targeted by scripts)
		if line.special == 0 and not tagged_lines[lineId] then
			line.special = 0
		end

		if polyobj_lines[lineId] then -- polyobj lines move, so we can't chache their coordinates. this is a hexen+ thing
			line._polyobj = true
			-- assumption: the vertex pointers never change (even if the vertex coordinates do)
			line.v1 = line.v1
			line.v2 = line.v2
		else
			line._coords = { line:coords() }
		end

		table.insert(Lines, line)
	end
	
	for _, sector in pairs(Globals.sectors) do
		Tracked[TrackedType.SECTOR].IDs[sector.iSectorID] = true
	end
	
	BlockmapWidth  = Globals.bmapwidth
	BlockmapOrigin = {
		x = Globals.bmaporgx,
		y = Globals.bmaporgy
	}
	BlockmapEnd = { 
		x = BlockmapOrigin.x + Globals.bmapwidth  * GRID_SIZE * FRACUNIT,
		y = BlockmapOrigin.y + Globals.bmapheight * GRID_SIZE * FRACUNIT
	}
	LastBMWidth  = BlockmapWidth
	LastBMOrigin = BlockmapOrigin
	LastBMEnd    = BlockmapEnd
	
	settings_read()
end

function clear_cache()
	Lines              = nil
	DivLines           = {}
	MapBlocks          = {}
	Intercepts         = {}
	InterceptsOverruns = {}
	Tracked            = {
		[TrackedType.THING ] = TrackedEntity.new("thing" ),
		[TrackedType.LINE  ] = TrackedEntity.new("line"  ),
		[TrackedType.SECTOR] = TrackedEntity.new("sector")
	}
end

--#endregion


--#region GUI

--- Displays next or previous entity from a given list. Nothing happens if there's nowhere to scroll. Depends on some metadata being present in the entity, it's not just a flat list!
---@param entity tracked_entity | players Source of the list to scroll through
---@param delta integer Direction and amount to scroll by
function scroll_list(entity, delta)
	if not entity.Current then return end
	
	local list  = entity.TrackedList or entity.List
	local limit = entity.Max
	local step  = 1
	
	if delta < 0 then
		limit = entity.Min
		step  = -1
		delta = -delta
	end
	
	if entity.Current == limit then return end
	
	for i = entity.Current+step, limit, step do
		if list[i] then delta = delta - 1 end
		if delta == 0 then
			entity.Current = i
			return
		end
	end
end

--- Shows clickable button on the screen that executes a given function. Only initial click is detected, holding it does nothing.
---@param x integer Onscreen coordinate X
---@param y integer Onscreen coordinate Y
---@param name string Text to appear on the button
---@param func function Function to execute when the button is pressed
function make_button(x, y, name, func)
	local lineCount,
	      longest    = get_line_count(name)
	local boxWidth   = CHAR_WIDTH
	local boxHeight  = CHAR_HEIGHT
	local textWidth  = longest  *CHAR_WIDTH
	local textHeight = lineCount*CHAR_HEIGHT
	local colors     = { 0x66bbddff, 0xaabbddff, 0xaa88aaff }
	local colorIndex = 1
	local padding    = 6
	
	-- delete button
	if name == " X " then
		colors = { 0x88ff8888, 0xffff0000, 0xffff0000 }
	end
	
	if x < 0 then x = ScreenWidth  + x end
	if y < 0 then y = ScreenHeight + y end
	
	if textWidth  + padding > boxWidth  then boxWidth  = textWidth  + padding end
	if textHeight + padding > boxHeight then boxHeight = textHeight + padding end
	
	local textX    = x + boxWidth /2 - textWidth /2
	local textY    = y + boxHeight/2 - textHeight/2 - boxHeight
	local mousePos = client.transformPoint(Mouse.X, Mouse.Y)
	
	if  in_range(mousePos.x, x,           x+boxWidth)
	and in_range(mousePos.y, y-boxHeight, y         )
	and not (freeze_gui() and name ~= "Confirm" and name ~= "Cancel")
	then
		if Mouse.Left then
			suppress_click_input()
			colorIndex = 3
			
			if not LastMouse.left then
				func()
			end
		else
			colorIndex = 2
		end
	end
	
	box(x, y, x+boxWidth, y-boxHeight, 0xaaffffff, colors[colorIndex])
	text(textX, textY, name, colors[colorIndex] | 0xff000000) -- full alpha
end

--- Shows a dialog that blocks everything else and expects confirmation or cancelation
---@param message string
---@return boolean # Whether the user confirmed or canceled
function show_dialog(message)
	local ret
	local lineCount,
	      longest    = get_line_count(message)
	local textWidth  = longest      *CHAR_WIDTH
	local textHeight = (lineCount+2)*CHAR_HEIGHT
	local boxWidth   = CHAR_WIDTH
	local boxHeight  = CHAR_HEIGHT
	local padding    = 50
	
	if textWidth  + padding > boxWidth  then boxWidth  = textWidth  + padding end
	if textHeight + padding > boxHeight then boxHeight = textHeight + padding end
	
	local x     = ScreenWidth  /2 - textWidth /2
	local y     = ScreenHeight /2 - textHeight/2
	local textX = x + boxWidth /2 - textWidth /2
	local textY = y + boxHeight/2 - textHeight/2
	local edge  = { x = x+boxWidth, y = y+boxHeight }
	
	box(x, y, edge.x, edge.y, 0xaaffffff, 0xaabbddff)
	text(textX, textY, message, 0xffffffff)
	
	make_button(edge.x-padding*3.8, edge.y-CHAR_HEIGHT, "Confirm", function() ret = true  end)
	make_button(edge.x-padding*2,   edge.y-CHAR_HEIGHT, "Cancel",  function() ret = false end)
	
	return ret
end

---@return boolean # Whether GUI is currently frozen by a dialog that requires user input
function freeze_gui()
	return CurrentPrompt ~= nil
	or     Confirmation  ~= nil
end

--#endregion


--#region MISC

function print_help()
	local help = "Script for Doom engine games by feos and kalimag.\n\n"..

	"Shows player info. If you have several players in-game you can hover on player info and scroll the mouse wheel to show info for different players.\n"..
	"Shows current tic, in-game time, and RNG index along with value at that index.\n\n"..

	"Shows Automap consisting of linedefs, sectors, and things (toggled via the 'Map' button).\n"..
	"Shows blockmap grid (toggled via the 'Grid' button).\n"..
	"The 'Reset View' button zooms and pans the automap to make all things visible.\n"..
	"The 'Follow' button makes the camera stick to the player that is currently selected on the left panel.\n"..
	"The 'Hilite' buttons enables highlight/selection mode where you can hover on sectors, linedefs, and things to see their info on the left, in corresponding colors.\n"..
	"Use Mouse Wheel to zoom in and out, and mouse movement with the 'Space' key held down to pan.\n\n"..

	"Buttons for adding things, lines, and sectors allow to track those entities. If more than one object of a given type is tracked, you can hoven on them and scroll through them with the mouse wheel. They can also be removed from the tracked list by hitting the red cross button that appears on hover. \n\n"..

	"Tracked entity lists and Automap config are saved when the script is stopped or restarted or when you add or remove tracked entities. To manually edit the config file, disable the script, edit the 'doom.settings.lua' file if it exists, then start the script again. Editing it while the script is running will overwrite your edits later. Config also stored Angle type which has no other way to change it.\n\n"..

	"You can log which thing has used or crossed linedefs, with options being None, Player, and All\n\n"..

	"You can log various info every time a P_Random() call happens with a class that changes the RNG value. Log prints tic count when the call happened, which call it is per that tic, the RNG index, the value at that index, and the call stack (while file and line called P_Random() within which function).\n\n"..

	"You can display intercepts on the Automap: tracelines and blockmap blocks where intercepts happen. Red color means an intercept happened on this tic, then the color fades to grey.\n\n"..

	"And the most complicated feature is intercept logging. It will inform you when the intercept count exceeded a user-defined limit, which is 128 by default but can be changed in config (the 'InterceptLimit' value). If the intercept count exceeded that value, a button will appear that will print info on all the intercepts, as well as info on all the vanilla addresses they corrupted, if any. Boom fixed intercept overflow, so in Boom complevel this feature is disabled.\n\n"

	print(help)
end

--- Doom uses left mouse clock for firing and if we're running unpaused we don't want clicking script buttons to trigger fire. Currently only blocks first player fire. TODO: block for all players
function suppress_click_input()
	if MAP_CLICK_BLOCK and MAP_CLICK_BLOCK ~= "" then
		joypad.set({ [MAP_CLICK_BLOCK] = false })
	end
end

--- Default map view is when all things are on the screen, which dictates zoom and pan. This function sets that up by checking positions of all things. It also creates a lookup list of IDs for when we add things to the tracker.
function init_mobj_bounds()
	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local x      = mobj.x / FRACUNIT
		local y      = mobj.y / FRACUNIT * -1
		local index  = mobj.index
		local entity = Tracked[TrackedType.THING]
		if x < OB.left   then OB.left   = x end
		if x > OB.right  then OB.right  = x end
		if y < OB.top    then OB.top    = y end
		if y > OB.bottom then OB.bottom = y end
		
		-- players have index -1, things to be removed have -2
		if index >= 0 then
			entity.IDs[index] = true
		end
	end
end

--- 
function get_mobj_pref(mobj, mobjtype)
	if HighlightTypes[mobjtype] then return MapPrefs.highlight end
	if InertTypes    [mobjtype] then return MapPrefs.inert     end
	if MiscTypes     [mobjtype] then return MapPrefs.misc      end
	if MissileTypes  [mobjtype] then return MapPrefs.missile   end
	if PlayerTypes   [mobjtype] then return MapPrefs.player    end

	local flags = mobj.flags
	if flags & (MobjFlags.PICKUP | MobjFlags.FRIEND) ~= 0 then return MapPrefs.player end
	if flags & MobjFlags.COUNTKILL ~= 0 or EnemyTypes[mobjtype] then
		if flags & MobjFlags.CORPSE ~= 0 then return MapPrefs.corpse end
		if mobj.state.action == symbols.A_Look then return MapPrefs.enemy_idle end
		return MapPrefs.enemy
	end
	if flags & MobjFlags.COUNTITEM ~= 0 then return MapPrefs.countitem end
	if flags & MobjFlags.SPECIAL   ~= 0 then return MapPrefs.item      end
	if flags & MobjFlags.MISSILE   ~= 0 then return MapPrefs.missile   end
	if flags & MobjFlags.SHOOTABLE ~= 0 then return MapPrefs.shootable end
	if flags & MobjFlags.SOLID     ~= 0 then return MapPrefs.solid     end
	return MapPrefs.inert
end

function get_mobj_color(mobj, mobjtype)
	local pref = get_mobj_pref(mobj, mobjtype)
	if not pref then return end
	local color = pref.color
	if not color or color < 0x01000000 then return end
	local radius_min_zoom = pref.radius_min_zoom or math.huge
	local text_min_zoom   = pref.text_min_zoom   or math.huge
	local radius_color    = Zoom >= radius_min_zoom and color or nil
	local text_color      = Zoom >= text_min_zoom   and color or nil
	return radius_color, text_color
end

function add_entity(type)
	if CurrentPrompt then return end
	
	local adder
	local entity = Tracked[type]
	local lookup = entity.IDs
	local array  = entity.TrackedList
	local name   = entity.Name
	
	if type == TrackedType.LINE then
		adder = function(id)
			for _, line in pairs(Globals.lines) do
				if id == line.iLineID then
					array[id] = line
					return
				end
			end
		end
	elseif type == TrackedType.SECTOR then
		adder = function(id)
			for _, sector in pairs(Globals.sectors) do
				if id == sector.iSectorID then
					array[id] = sector
					return
				end
			end
		end
	elseif type == TrackedType.THING then
		adder = function(id)
			for _, mobj in pairs(Globals.mobjs:readbulk()) do
				if id == mobj.index then
					array[id] = mobj
					return
				end
			end
		end
	end
	
	CurrentPrompt = {
		msg = name,
		fun = function(id)
			if not lookup[id] then
				print(string.format(
					"\nERROR: Can't add %s %d because it doesn't exist!\n", name, id
				))
				return
			end
			
			if array[id] then
				print(string.format(
					"\nERROR: Can't add %s %d because it's already there!\n", name, id
				))
				return
			end
			
			if id < entity.Min then entity.Min = id end
			if id > entity.Max then entity.Max = id end
			
			adder(id)
			entity.Current = id
			print(string.format("Added %s %d", name, id))
			settings_write()
		end,
		value = nil
	}
end

--#endregion


--#region LOOKUPS

-- Additional types that are not identifiable by flags alone
HighlightTypes = to_lookup({

})
PlayerTypes = to_lookup({
	MobjType.PLAYER,
	MobjType.HERETIC_PLAYER,
	MobjType.HERETIC_CHICPLAYER,
	MobjType.HEXEN_PLAYER_FIGHTER,
	MobjType.HEXEN_PLAYER_CLERIC,
	MobjType.HEXEN_PLAYER_MAGE,
	MobjType.HEXEN_PIGPLAYER,

})
EnemyTypes = to_lookup({
	MobjType.SKULL,
})
MissileTypes = to_lookup({
	MobjType.HEXEN_THROWINGBOMB,
	MobjType.HERETIC_FIREBOMB, MobjType.HEXEN_FIREBOMB,
	MobjType.HEXEN_POISONBAG, MobjType.HEXEN_POISONCLOUD,
	MobjType.HEXEN_DRAGON_FX2,
	MobjType.HEXEN_SUMMON_FX,
})
MiscTypes = to_lookup({
	MobjType.BOSSTARGET,
	MobjType.TELEPORTMAN, MobjType.HERETIC_TELEPORTMAN, MobjType.HEXEN_TELEPORTMAN,
	MobjType.PUSH, MobjType.PULL,
	MobjType.HERETIC_PODGENERATOR,
	MobjType.HEXEN_MAPSPOT, MobjType.HEXEN_MAPSPOTGRAVITY,
	MobjType.HEXEN_THRUSTFLOOR_UP, MobjType.HEXEN_THRUSTFLOOR_DOWN,
	MobjType.HEXEN_QUAKE_FOCUS,
	MobjType.HEXEN_ZPOISONSHROOM,
})
InertTypes = to_lookup({
	MobjType.HERETIC_BLOODSPLATTER,
	MobjType.HERETIC_FEATHER,
	MobjType.HERETIC_PODGOO,
	MobjType.HERETIC_SPLASH,
	MobjType.HERETIC_SLUDGECHUNK,
	MobjType.HERETIC_TELEGLITTER, MobjType.HERETIC_TELEGLITTER2,
	MobjType.HEXEN_BLOODSPLATTER,
	MobjType.HEXEN_CORPSEBLOODDRIP,
	MobjType.HEXEN_LEAF1, MobjType.HEXEN_LEAF2,
	MobjType.HEXEN_SPLASH,
	MobjType.HEXEN_SLUDGECHUNK,
	MobjType.HEXEN_WATER_DRIP,
	MobjType.HEXEN_DIRT1, MobjType.HEXEN_DIRT2, MobjType.HEXEN_DIRT3,
	MobjType.HEXEN_DIRT4, MobjType.HEXEN_DIRT5, MobjType.HEXEN_DIRT6,
	MobjType.HEXEN_FIREDEMON_FX1, MobjType.HEXEN_FIREDEMON_FX2, MobjType.HEXEN_FIREDEMON_FX3,
	MobjType.HEXEN_FIREDEMON_FX4, MobjType.HEXEN_FIREDEMON_FX5,
	MobjType.HEXEN_ICEGUY_WISP1, MobjType.HEXEN_ICEGUY_WISP2,
	MobjType.HEXEN_KORAX_SPIRIT1, MobjType.HEXEN_KORAX_SPIRIT2, MobjType.HEXEN_KORAX_SPIRIT3,
	MobjType.HEXEN_KORAX_SPIRIT4, MobjType.HEXEN_KORAX_SPIRIT5, MobjType.HEXEN_KORAX_SPIRIT6,
	MobjType.HEXEN_POTTERYBIT1,
	MobjType.HEXEN_SGSHARD0, MobjType.HEXEN_SGSHARD1, MobjType.HEXEN_SGSHARD2,
	MobjType.HEXEN_SGSHARD3, MobjType.HEXEN_SGSHARD4, MobjType.HEXEN_SGSHARD5,
	MobjType.HEXEN_SGSHARD6, MobjType.HEXEN_SGSHARD7, MobjType.HEXEN_SGSHARD8,
	MobjType.HEXEN_SGSHARD9,
	MobjType.HEXEN_WRAITHFX3,
})

--#endregion