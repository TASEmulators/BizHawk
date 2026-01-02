
-- MODULES

enums   = require("dsda.enums")
structs = require("dsda.structs")
symbols = require("dsda.symbols")


-- CONSTANTS

FRACBITS          = 16
FRACUNIT          = 1 << FRACBITS
ANGLE_90          = 0x40000000
ANGLES            = 64 -- byte type for now
MINIMAL_ZOOM      = 0.0001 -- ???
ZOOM_FACTOR       = 0.01
WHEEL_ZOOM_FACTOR = 10
DRAG_FACTOR       = 10
PAN_FACTOR        = 10
CHAR_WIDTH        = 10
CHAR_HEIGHT       = 16
PADDING_WIDTH     = 240
MAP_CLICK_BLOCK   = "P1 Fire" -- prevent this input while clicking on map buttons

-- closure object
TrackedEntity = {}
function TrackedEntity.new(name)
	local self       = {}
	self.TrackedList = {}
	self.IDs         = {}
	self.Current     = nil
	self.Min         = math.maxinteger
	self.Max         = math.mininteger
	self.Name        = name
	return self
end

-- shortcuts

text     = gui.text
box      = gui.drawBox
drawline = gui.drawLine


-- TOP LEVEL VARIABLES

Zoom           = 1
Follow         = false
Hilite         = false
Init           = true
LastFramecount = -1
ScreenWidth    = client.screenwidth()
ScreenHeight   = client.screenheight()

-- tables

TrackedType = {
	THING  = 0,
	LINE   = 1,
	SECTOR = 2
}

Tracked = {
	[TrackedType.THING ] = TrackedEntity.new("thing" ),
	[TrackedType.LINE  ] = TrackedEntity.new("line"  ),
	[TrackedType.SECTOR] = TrackedEntity.new("sector")
}
Players = {}
-- view offset
Pan = {
	x = 0,
	y = 0
}
-- map object positions bounds
OB = {
	top    = math.maxinteger,
	left   = math.maxinteger,
	bottom = math.mininteger,
	right  = math.mininteger
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

-- forward declarations

Input         = nil
Lines         = nil
PlayerTypes   = nil
EnemyTypes    = nil
MissileTypes  = nil
MiscTypes     = nil
InertTypes    = nil
CurrentPrompt = nil
LastEpisode   = nil
LastMap       = nil
LastInput     = nil

--gui.defaultPixelFont("fceux")
gui.use_surface("client")
client.SetClientExtraPadding(PADDING_WIDTH, 0, 0, 0)

-- GAME/SCREEN CODECS

function decode_x(coord)
	return math.floor(((coord / FRACUNIT) + Pan.x) * Zoom)
end

function decode_y(coord)
	return math.floor(((-coord / FRACUNIT) + Pan.y) * Zoom)
end

function encode_x(coord)
	return math.floor(((coord / Zoom) - Pan.x) * FRACUNIT)
end

function encode_y(coord)
	return -math.floor(((coord / Zoom) - Pan.y) * FRACUNIT)
end

-- return value matches passed value (line/vertex tuple/table)
function codec(method, arg1, arg2, arg3, arg4)
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

function game_to_screen(arg1, arg2, arg3, arg4)
	return codec("decode", arg1, arg2, arg3, arg4)
end

function screen_to_game(arg1, arg2, arg3, arg4)
	return codec("encode", arg1, arg2, arg3, arg4)
end



function pan_left(divider)
	Pan.x = Pan.x + PAN_FACTOR/Zoom/(divider or 2)
end

function pan_right(divider)
	Pan.x = Pan.x - PAN_FACTOR/Zoom/(divider or 2)
end

function pan_up(divider)
	Pan.y = Pan.y + PAN_FACTOR/Zoom/(divider or 2)
end

function pan_down(divider)
	Pan.y = Pan.y - PAN_FACTOR/Zoom/(divider or 2)
end

function zoom(times, mouseCenter)
	local mouse
	local mousePos
	local zoomCenter
	local direction = 1
	times = times or 1
	
	if Follow then mouseCenter = false end
	
	if times < 0 then
		direction = -1
		times = -times
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
	
	for i=0, times do
		local newZoom = Zoom + Zoom * ZOOM_FACTOR * direction
		if newZoom < MINIMAL_ZOOM then return end
		Zoom = newZoom
	end
	
	zoomCenter.x = (encode_x(mouseCenter and mousePos.x or ScreenWidth /2)-zoomCenter.x)
	zoomCenter.y = (encode_y(mouseCenter and mousePos.y or ScreenHeight/2)-zoomCenter.y)
	Pan.x = Pan.x + zoomCenter.x / FRACUNIT
	Pan.y = Pan.y - zoomCenter.y / FRACUNIT
end



function to_lookup(table)
	local lookup = {}
	for k, v in pairs(table) do
		lookup[v] = k
	end
	return lookup
end

function get_line_count(str)
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


-- MATH

function maybe_swap(smaller, bigger)
	if smaller > bigger then
		return bigger, smaller
	end
	return smaller - 100, bigger + 100
end

function in_range(var, minimum, maximum)
	return var >= minimum and var <= maximum
end

-- helper to get squared distance (avoids sqrt for comparison)
function dist_sq(p1, p2)
    return (p1.x - p2.x)^2 + (p1.y - p2.y)^2
end

function distance_from_line(p, a, b)
	local ab_sq = dist_sq(a, b)
	
	if ab_sq == 0 then return math.sqrt(dist_sq(p, a)) end -- A and B are the same point

	-- project point P onto the line AB
	-- t = ((P-A) . (B-A)) / |B-A|^2
	local t =
		((p.x - a.x) * (b.x - a.x) +
		 (p.y - a.y) * (b.y - a.y)) / ab_sq
	
	-- clamp t to [0, 1] to stay within the segment
	t = math.max(0, math.min(1, t))

	-- find the closest point on the segment (D)
	local closestPoint = {
		x = a.x + t * (b.x - a.x),
		y = a.y + t * (b.y - a.y)
	}

	-- return the distance from P to the closest point D
	local dist = math.sqrt(dist_sq(p, closestPoint))
		
	if ((b.y - a.y) / (b.x - a.x)) * (p.x - a.x) + a.y < p.y then return -dist end
	
	return dist
end


-- TYPE CONVERTERS

function tuple_to_vertex(xx, yy)
	return { x = xx, y = yy }
end

function vertex_to_tuple(v)
	return table.unpack(v)
end

function tuple_to_line(x1, y1, x2, y2)
	return {
		v1 = { x = x1, y = y1 },
		v2 = { x = x2, y = y2 }
	}
end

function line_to_tuple(l)
	return table.unpack(l.v1), table.unpack(l.v2)
end


-- Map colors (0xAARRGGBB or "name")
MapPrefs = {
	player      = { color = 0xff60d0ff, radius_min_zoom = 0.00, text_min_zoom = 0.20, },
	enemy       = { color = 0xffff0000, radius_min_zoom = 0.00, text_min_zoom = 0.25, },
	enemy_idle  = { color = 0xffaa0000, radius_min_zoom = 0.00, text_min_zoom = 0.25, },
--	corpse      = { color = 0xaaaaaaaa, radius_min_zoom = 0.00, text_min_zoom = 0.75, },
	missile     = { color = 0xffff8000, radius_min_zoom = 0.00, text_min_zoom = 0.25, },
	shootable   = { color = 0xffaaaaaa, radius_min_zoom = 0.00, text_min_zoom = 0.50, },
	countitem   = { color = 0xffffff00, radius_min_zoom = 0.00, text_min_zoom = 1.50, },
	item        = { color = 0xff00ff00, radius_min_zoom = 0.00, text_min_zoom = 1.50, },
--	misc        = { color = 0xffa0a0a0, radius_min_zoom = 0.75, text_min_zoom = 1.00, },
	solid       = { color = 0xff505050, radius_min_zoom = 0.75, text_min_zoom = false, },
--	inert       = { color = 0x80808080, radius_min_zoom = 0.75, text_min_zoom = false, },
	highlight   = { color = 0xffff00ff, radius_min_zoom = 0.00, text_min_zoom = 0.20, },
}

Globals      = structs.globals
MobjType     = enums.mobjtype
SpriteNumber = enums.doom.spritenum
MobjFlags    = enums.mobjflags


function init_mobj_bounds()
	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local x = mobj.x / FRACUNIT
		local y = mobj.y / FRACUNIT * -1
		if x < OB.left   then OB.left   = x end
		if x > OB.right  then OB.right  = x end
		if y < OB.top    then OB.top    = y end
		if y > OB.bottom then OB.bottom = y end
	end
end

function follow_toggle()
	if Mouse.Left and not LastMouse.left then
		Follow = not Follow
	end
end

function hilite_toggle()
	if Mouse.Left and not LastMouse.left then
		Hilite = not Hilite
	end
end

function suppress_click_input()
	if MAP_CLICK_BLOCK and MAP_CLICK_BLOCK ~= "" then
		joypad.set({ [MAP_CLICK_BLOCK] = false })
	end
end

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

function update_zoom()
	local mousePos     = client.transformPoint(Mouse.X, Mouse.Y)
	local mouseWheel   = math.floor(Mouse.Wheel/120)
	local deltaX       = mousePos.x - LastMouse.x
	local deltaY       = mousePos.y - LastMouse.y
	local deltaWheel   = mouseWheel - LastMouse.wheel
	
	if deltaWheel ~= 0 then zoom(deltaWheel * WHEEL_ZOOM_FACTOR, true) end
	
	if input.get()["Space"] then
		if deltaX ~= 0 then pan_left(DRAG_FACTOR/deltaX) end
		if deltaY ~= 0 then pan_up  (DRAG_FACTOR/deltaY) end
	end
	
	LastMouse.x     = mousePos.x
	LastMouse.y     = mousePos.y
	LastMouse.wheel = mouseWheel
	
	if Follow and Globals.gamestate == 0 then
		local player = select(2, next(Players))
		local screenCenter = screen_to_game({
			x = (ScreenWidth+PADDING_WIDTH)/2,
			y = ScreenHeight/2
		})
		
		screenCenter.x = screenCenter.x / FRACUNIT - player.x
		screenCenter.y = screenCenter.y / FRACUNIT - player.y
		Pan.x = Pan.x + screenCenter.x
		Pan.y = Pan.y - screenCenter.y
	end
	
	if not Init
	and LastScreenSize.w == ScreenWidth
	and LastScreenSize.h == ScreenHeight
	then return end
	
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
	Init = true
	update_zoom()
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
end

function clear_cache()
	Lines = nil
	reset_view()
	Tracked = {
		[TrackedType.THING ] = TrackedEntity.new("thing" ),
		[TrackedType.LINE  ] = TrackedEntity.new("line"  ),
		[TrackedType.SECTOR] = TrackedEntity.new("sector")
	}
end

function make_button(x, y, name, func)
	local lineCount, longest = get_line_count(name)
	local boxWidth   = CHAR_WIDTH
	local boxHeight  = CHAR_HEIGHT
	local textWidth  = longest  *CHAR_WIDTH
	local textHeight = lineCount*CHAR_HEIGHT
	local colors     = { 0x66bbddff, 0xaabbddff, 0xaa88aaff }
	local colorIndex = 1
	local padding    = 10
	
	if x < 0 then x = ScreenWidth  + x end
	if y < 0 then y = ScreenHeight + y end
	
	if textWidth  + padding > boxWidth  then boxWidth  = textWidth  + padding end
	if textHeight + padding > boxHeight then boxHeight = textHeight + padding end
	
	local textX    = x + boxWidth /2 - textWidth /2
	local textY    = y + boxHeight/2 - textHeight/2 - boxHeight
	local mousePos = client.transformPoint(Mouse.X, Mouse.Y)
	
	if  in_range(mousePos.x, x,           x+boxWidth)
	and in_range(mousePos.y, y-boxHeight, y         )
	and not CurrentPrompt
	and not (Follow
	and (func == pan_left
	or   func == pan_up
	or   func == pan_down
	or   func == pan_right))
	then
		if Mouse.Left then
			suppress_click_input()
			colorIndex = 3
			func()
		else
			colorIndex = 2
		end
	end
	
	box(x, y, x+boxWidth, y-boxHeight, 0xaaffffff, colors[colorIndex])
	text(textX, textY, name, colors[colorIndex] | 0xff000000) -- full alpha
end

function check_press(key)
	return Input[key] and not LastInput[key]
end

function input_prompt()
	Input       = input.get()
	local value = tostring(CurrentPrompt.value or "")
	
	if check_press("Escape") then
		CurrentPrompt = nil
		return
	elseif check_press("Backspace") then
		value = value:sub(1, -2)
	elseif (check_press("Enter") or check_press("KeypadEnter")) and value ~= "" then
		CurrentPrompt.fun(tonumber(value))
		CurrentPrompt = nil
		return
	else
		for i = 0, 9 do
			local digit  = tostring(i)
			local number = "Number" .. digit
			local keypad = "Keypad" .. digit
			if (check_press(number)
			or  check_press(keypad))
			then value = value .. digit
			end
		end
	end
	
	local boxWidth   = CHAR_WIDTH
	local boxHeight  = CHAR_HEIGHT
	local message    = string.format(
		"Enter %s ID from\nlevel editor.\n\n" ..
		"Hit \"Enter\" to send,\n" ..
		"\"Backspace\" to erase,\n" ..
		"or \"Escape\" to cancel.\n\n%s_",
		CurrentPrompt.msg, value)
	local lineCount, longest = get_line_count(message)
	local textWidth  = longest  *CHAR_WIDTH
	local textHeight = lineCount*CHAR_HEIGHT
	local padding    = 50
	
	if textWidth  + padding > boxWidth  then boxWidth  = textWidth  + padding end
	if textHeight + padding > boxHeight then boxHeight = textHeight + padding end
	
	local x     = ScreenWidth /2 - textWidth /2
	local y     = ScreenHeight/2 - textHeight/2
	local textX = x + boxWidth /2 - textWidth /2
	local textY = y + boxHeight/2 - textHeight/2
	
	box(x, y, x+boxWidth, y+boxHeight, 0xaaffffff, 0xaabbddff)
	text(textX, textY, message, 0xffffffff)
	
	if value ~= "" then
		CurrentPrompt.value = tonumber(value)
	else
		CurrentPrompt.value = nil
	end
	
	LastInput = Input
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
		end,
		value = nil
	}
end


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