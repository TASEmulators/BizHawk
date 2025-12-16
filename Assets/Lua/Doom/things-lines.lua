-- feos, kalimag, 2025

local enums   = require("dsda.enums")
local structs = require("dsda.structs")
local symbols = require("dsda.symbols")


-- CONSTANTS

local MINIMAL_ZOOM      = 0.0001 -- ???
local ZOOM_FACTOR       = 0.01
local WHEEL_ZOOM_FACTOR = 10
local DRAG_FACTOR       = 10
local PAN_FACTOR        = 10
local CHAR_WIDTH        = 10
local CHAR_HEIGHT       = 16
local MAP_CLICK_BLOCK   = "P1 Fire" -- prevent this input while clicking on map buttons

-- Map colors (0xAARRGGBB or "name")
local MapPrefs = {
	player      = { color = 0xFF60A0FF, radius_min_zoom = 0.00, text_min_zoom = 0.20, },
	enemy       = { color = 0xFFFF0000, radius_min_zoom = 0.00, text_min_zoom = 0.25, },
	enemy_idle  = { color = 0xFFAA0000, radius_min_zoom = 0.00, text_min_zoom = 0.25, },
--	corpse      = { color = 0xAAAAAAAA, radius_min_zoom = 0.00, text_min_zoom = 0.75, },
	missile     = { color = 0xFFFF8000, radius_min_zoom = 0.00, text_min_zoom = 0.25, },
	shootable   = { color = 0xFFAAAAAA, radius_min_zoom = 0.00, text_min_zoom = 0.50, },
	countitem   = { color = 0xFFFFFF00, radius_min_zoom = 0.00, text_min_zoom = 1.50, },
	item        = { color = 0xFF00FF00, radius_min_zoom = 0.00, text_min_zoom = 1.50, },
--	misc        = { color = 0xFFA0A0A0, radius_min_zoom = 0.75, text_min_zoom = 1.00, },
	solid       = { color = 0xFF505050, radius_min_zoom = 0.75, text_min_zoom = false, },
--	inert       = { color = 0x80808080, radius_min_zoom = 0.75, text_min_zoom = false, },
	highlight   = { color = 0xFFFF00FF, radius_min_zoom = 0.00, text_min_zoom = 0.20, },
}

-- shortcuts

local text     = gui.text
local box      = gui.drawBox
local drawline = gui.drawLine
--local text = gui.pixelText -- INSANELY SLOW

local Globals      = structs.globals
local MobjType     = enums.mobjtype
local SpriteNumber = enums.doom.spritenum
local MobjFlags    = enums.mobjflags


-- TOP LEVEL VARIABLES

local Zoom = 1
local Follow = false
local Init = true

-- tables

-- view offset
local Pan = {
	x = 0,
	y = 0
}
-- object positions bounds
local OB = {
	top    = math.maxinteger,
	left   = math.maxinteger,
	bottom = math.mininteger,
	right  = math.mininteger
}
local LastScreenSize = {
	w = client.screenwidth(),
	h = client.screenheight()
}
local LastMouse = {
	x     = 0,
	y     = 0,
	wheel = 0,
	left  = false
}
local LastFramecount = -1
local LastEpisode
local LastMap

-- forward declarations

local Lines
local PlayerTypes
local EnemyTypes
local MissileTypes
local MiscTypes
local InertTypes

--gui.defaultPixelFont("fceux")
gui.use_surface("client")
client.SetClientExtraPadding(240, 0, 0, 0)

-- TYPE CONVERTERS

local function tuple_to_vertex(xx, yy)
	return { x = xx, y = yy }
end

local function vertex_to_tuple(v)
	return table.unpack(v)
end

local function tuple_to_line(x1, y1, x2, y2)
	return {
		v1 = { x = x1, y = y1 },
		v2 = { x = x2, y = y2 }
	}
end

local function line_to_tuple(l)
	return table.unpack(l.v1), table.unpack(l.v2)
end

-- GAME/SCREEN CODECS

local function decode_x(coord)
	return math.floor(((coord / 0xffff) + Pan.x) * Zoom)
end

local function decode_y(coord)
	return math.floor(((-coord / 0xffff) + Pan.y) * Zoom)
end

local function encode_x(coord)
	return math.floor(((coord / Zoom) - Pan.x) * 0xffff)
end

local function encode_y(coord)
	return -math.floor(((coord / Zoom) - Pan.y) * 0xffff)
end

-- return value matches passed value (line/vertex tuple/table)
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

local function game_to_screen(arg1, arg2, arg3, arg4)
	return codec("decode", arg1, arg2, arg3, arg4)
end

local function screen_to_game(arg1, arg2, arg3, arg4)
	return codec("encode", arg1, arg2, arg3, arg4)
end


local function in_range(var, minimum, maximum)
	return var >= minimum and var <= maximum
end

local function pan_left(divider)
	Pan.x = Pan.x + PAN_FACTOR/Zoom/(divider or 2)
end

local function pan_right(divider)
	Pan.x = Pan.x - PAN_FACTOR/Zoom/(divider or 2)
end

local function pan_up(divider)
	Pan.y = Pan.y + PAN_FACTOR/Zoom/(divider or 2)
end

local function pan_down(divider)
	Pan.y = Pan.y - PAN_FACTOR/Zoom/(divider or 2)
end

local function zoom(times, mouseCenter)
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
			x = client.screenwidth ()/2,
			y = client.screenheight()/2
		})
	end
	
	for i=0, times do
		local newZoom = Zoom + Zoom * ZOOM_FACTOR * direction
		if newZoom < MINIMAL_ZOOM then return end
		Zoom = newZoom
	end
	
	zoomCenter.x = (encode_x(mouseCenter and mousePos.x or client.screenwidth ()/2)-zoomCenter.x)
	zoomCenter.y = (encode_y(mouseCenter and mousePos.y or client.screenheight()/2)-zoomCenter.y)
	Pan.x = Pan.x + zoomCenter.x / 0xffff
	Pan.y = Pan.y - zoomCenter.y / 0xffff
end

local function follow_toggle()
	if Mouse.Left and not LastMouse.left then
		Follow = not Follow
	end
end

-- helper to get squared distance (avoids sqrt for comparison)
function dist_sq(p1, p2)
    return (p1.x - p2.x)^2 + (p1.y - p2.y)^2
end

local function distance_from_line(p, a, b)
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

local function suppress_click_input()
	if MAP_CLICK_BLOCK and MAP_CLICK_BLOCK ~= "" then
		joypad.set({ [MAP_CLICK_BLOCK] = false })
	end
end

local function maybe_swap(smaller, bigger)
	if smaller > bigger then
		return bigger, smaller
	end
	return smaller - 100, bigger + 100
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

local function to_lookup(table)
	local lookup = {}
	for k, v in pairs(table) do
		lookup[v] = k
	end
	return lookup
end

local function get_mobj_pref(mobj, mobjtype)
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

local function get_mobj_color(mobj, mobjtype)
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

local function init_cache()
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
end

local function cached_line_coords(line)
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

local function iterate_players()
	local playercount       = 0
	local total_killcount   = 0
	local total_itemcount   = 0
	local total_secretcount = 0
	local stats             = "      HP Armr Kill Item Secr\n"
	for i, player in Globals:iterate_players() do
		playercount       = playercount + 1
		local killcount   = player.killcount
		local itemcount   = player.itemcount
		local secretcount = player.secretcount

		total_killcount   = total_killcount   + killcount
		total_itemcount   = total_itemcount   + itemcount
		total_secretcount = total_secretcount + secretcount

		stats = string.format("%s P%i %4i %4i %4i %4i %4i\n",
			stats, i, player.health, player.armorpoints1, killcount, itemcount, secretcount)
	end
	if playercount > 1 then
		stats = string.format("%s %-12s %4i %4i %4i\n", stats, "All", total_killcount, total_itemcount, total_secretcount)
	end
	gui.text(0, 0, stats, nil, "topright")
end

local function iterate()
	if Init then return end

	init_cache()
	
	local closest_line
	local selected_sector
	local mousePos      = client.transformPoint(Mouse.X, Mouse.Y)
	local gameMousePos  = screen_to_game(mousePos)
	local screenwidth   = client.screenwidth()
	local screenheight  = client.screenheight()
	local shortest_dist = math.maxinteger

	for i, line in ipairs(Lines) do
		local color = 0xffffffff
		local x1, y1, x2, y2 = game_to_screen(cached_line_coords(line))
		local special = line.special

		if special ~= 0 then color = 0xffff00ff end

		drawline(x1, y1, x2, y2, color) -- no speedup from doing range check
		
		x1, y1, x2, y2 = cached_line_coords(line)
		
		local dist = distance_from_line(
			gameMousePos,
			tuple_to_vertex(x1, y1),
			tuple_to_vertex(x2, y2))
		
		if math.abs(dist) < shortest_dist then
			shortest_dist = math.abs(dist)
			closest_line = line
		end
	end
	
	if closest_line then
		local x1, y1, x2, y2 = game_to_screen(cached_line_coords(closest_line))
		local side =
			(mousePos.y - y1) * (x2 - x1) -
			(mousePos.x - x1) * (y2 - y1)
		
		if side <= 0 then
			if closest_line.backsector then
				selected_sector = closest_line.backsector
			end
		else
			if closest_line.frontsector then
				selected_sector = closest_line.frontsector
			end
		end
	end
	
	if selected_sector then
		for _, line in ipairs(selected_sector.lines) do
			-- cached_line_coords gives some length error?
			local x1, y1, x2, y2 = game_to_screen(line:coords())
			gui.drawLine(x1, y1, x2, y2, 0xff00ffff)
		end
	end
	
	if closest_line then
		local x1, y1, x2, y2 = game_to_screen(cached_line_coords(closest_line))
		drawline(x1, y1, x2, y2, 0xffff8800)
	end

	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local type = mobj.type
		local radius_color, text_color = get_mobj_color(mobj, type)
		if radius_color or text_color then -- not hidden
			local pos = tuple_to_vertex(game_to_screen(mobj.x, mobj.y))

			if  in_range(pos.x, 0, screenwidth)
			and in_range(pos.y, 0, screenheight)
			then
				local type   = mobj.type
				local radius = math.floor((mobj.radius / 0xffff) * Zoom)
				local index  = mobj.index
				--[[--
				local z      = mobj.z
				local tics   = mobj.tics
				local health = mobj.health
				local sprite = SpriteNumber[mobj.sprite]
				--]]--
				
				if  in_range(mousePos.x, pos.x - radius, pos.x + radius)
				and in_range(mousePos.y, pos.y - radius, pos.y + radius)
				then
					radius_color = "white"
					text_color   = "white"
				end
				
				if radius_color then
					box(pos.x - radius, pos.y - radius, pos.x + radius, pos.y + radius, radius_color)
				end
				
				if text_color then
				--	type = MobjType[type]
					text(
						pos.x - radius + 1,
						pos.y - radius,
						string.format("%d", index),  text_color)
				end
			end
		end
	end
	
--	text(50,10,shortest_dist/0xffff)
end

local function init_mobj_bounds()
	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local x = mobj.x / 0xffff
		local y = mobj.y / 0xffff * -1
		if x < OB.left   then OB.left   = x end
		if x > OB.right  then OB.right  = x end
		if y < OB.top    then OB.top    = y end
		if y > OB.bottom then OB.bottom = y end
	end
end

local function get_player1_xy()
	for _, player in Globals.iterate_players() do
		return { x = player.mo.x, y = player.mo.y }
	end
end

local function update_zoom()
	local screenwidth  = client.screenwidth()
	local screenheight = client.screenheight()
	local mousePos     = client.transformPoint(Mouse.X, Mouse.Y)
	local mouseWheel   = math.floor(Mouse.Wheel/120)
	local deltaX       = mousePos.x - LastMouse.x
	local deltaY       = mousePos.y - LastMouse.y
	local deltaWheel   = mouseWheel - LastMouse.wheel
	
	if deltaWheel ~= 0 then zoom(deltaWheel * WHEEL_ZOOM_FACTOR, true) end
	
	if input.get()["Space"] then
		if deltaX ~= 0 then pan_left(DRAG_FACTOR/deltaX) end
		if deltaY ~= 0 then pan_up  (DRAG_FACTOR/deltaY) end
		suppress_click_input()
	end
	
	LastMouse.x     = mousePos.x
	LastMouse.y     = mousePos.y
	LastMouse.wheel = mouseWheel
	
	if Follow and Globals.gamestate == 0 then
		local playerPos = get_player1_xy()
		local screenCenter = screen_to_game({
			x = screenwidth /2,
			y = screenheight/2
		})
		
		screenCenter.x = screenCenter.x - playerPos.x
		screenCenter.y = screenCenter.y - playerPos.y
		Pan.x = Pan.x + screenCenter.x / 0xffff
		Pan.y = Pan.y - screenCenter.y / 0xffff
	end
	
	if not Init
	and LastScreenSize.w == screenwidth
	and LastScreenSize.h == screenheight
	then return end
	
	if  OB.top    ~= math.maxinteger
	and OB.left   ~= math.maxinteger
	and OB.right  ~= math.mininteger
	and OB.bottom ~= math.mininteger
	and not emu.islagged()
	then
		OB.left, OB.right  = maybe_swap(OB.left, OB.right)
		OB.top,  OB.bottom = maybe_swap(OB.top,  OB.bottom)
		local span         = { x = OB.right-OB.left,   y = OB.bottom-OB.top    }
		local scale        = { x = screenwidth/span.x, y = screenheight/span.y }
		      Zoom         = math.min(scale.x, scale.y)
		local spanCenter   = { x = OB.left+span.x/2,   y = OB.top+span.y/2     }
		local sreenCenter  = { x = screenwidth/Zoom/2, y = screenheight/Zoom/2 }
		
		if not Follow then
			Pan.x = -math.floor(spanCenter.x - sreenCenter.x)
			Pan.y = -math.floor(spanCenter.y - sreenCenter.y)
		end
		
		Init = false
	end
end

local function reset_view()
	OB = {
		top    = math.maxinteger,
		left   = math.maxinteger,
		bottom = math.mininteger,
		right  = math.mininteger
	}
	Init = true	
	update_zoom()
end

local function clear_cache()
	Lines = nil
	reset_view()
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
	local mousePos = client.transformPoint(Mouse.X, Mouse.Y)
	
	if  in_range(mousePos.x, x,           x+boxWidth)
	and in_range(mousePos.y, y-boxHeight, y         ) then
		if not (Follow
		and (func == pan_left
		or   func == pan_up
		or   func == pan_down
		or   func == pan_right))
		then
			if Mouse.Left then
				suppress_click_input()
				colorIndex = 3
				func()
			else colorIndex = 2 end
		end
	end
	
	box(x, y, x+boxWidth, y-boxHeight, 0xaaffffff, colors[colorIndex])
	text(textX, textY, name, colors[colorIndex] | 0xff000000) -- full alpha
end

local function make_buttons()
	make_button( 10, client.screenheight()-40, "+", function() zoom( 1) end)
	make_button( 10, client.screenheight()-10, "-", function() zoom(-1) end)
	make_button( 40, client.screenheight()-24, "<", pan_left  )
	make_button( 64, client.screenheight()-40, "^", pan_up    )
	make_button( 64, client.screenheight()-10, "v", pan_down  )
	make_button( 88, client.screenheight()-24, ">", pan_right )
	make_button(118, client.screenheight()-40, "Reset View", reset_view)
	make_button(118, client.screenheight()-10,
		string.format("Follow %s", Follow and "ON " or "OFF"), follow_toggle)
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



event.onframestart(function()
	if client.ispaused() then return end -- frameadvance while paused
	-- do this before frame start to suppress mouse click input
	make_buttons()
	update_zoom()
end)

event.onexit(function()
	gui.clearGraphics()
	gui.cleartext()
end)

event.onloadstate(function()
	clear_cache()
end)

tastudio.onbranchload(function()
	clear_cache()
end)

while true do
	local framecount = emu.framecount()
	local paused     = client.ispaused()
	Mouse            = input.getmouse()

	local episode, map = Globals.gameepisode, Globals.gamemap
	if episode ~= LastEpisode or map ~= LastMap then
		clear_cache()
		LastEpisode, LastMap = episode, map
	end

	if Init then init_mobj_bounds() end

	-- clear cache after rewind, turbo etc.
	-- this is only necessary to invalidate line specials, the rest is handled by map change detection above
	if framecount ~= LastFramecount and framecount ~= LastFramecount + 1 then
		clear_cache()
	end

	if paused then
		-- OSD text is not automatically cleared while paused
		gui.cleartext()
		gui.clearGraphics()
		-- while onframestart isn't called
		make_buttons()
	end

	update_zoom()

	-- workaround: prevent multiple execution per frame because of emu.yield(), except when paused
	if (framecount ~= LastFramecount or paused) and Globals.gamestate == 0 then
		iterate()
		LastMouse.left   = Mouse.Left
	--	iterate_players()
	end

	--[[--
	text(10, client.screenheight()-170, string.format(
		"Zoom: %.4f\nPanX: %s\nPanY: %s", 
		Zoom, Pan.x, Pan.y), 0xffbbddff)
	--]]--

	LastScreenSize.w = client.screenwidth()
	LastScreenSize.h = client.screenheight()
	LastFramecount   = framecount

	emu.yield()
end