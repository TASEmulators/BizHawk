-- feos, kalimag, 2025

local enums   = require("dsda.enums")
local structs = require("dsda.structs")
local symbols = require("dsda.symbols")


-- CONSTANTS

local FRACBITS          <const> = 16
local FRACUNIT          <const> = 1 << FRACBITS
local ANGLE_90          <const> = 0x40000000
local ANGLES            <const> = 64 -- byte type for now
local MINIMAL_ZOOM      <const> = 0.0001 -- ???
local ZOOM_FACTOR       <const> = 0.01
local WHEEL_ZOOM_FACTOR <const> = 10
local DRAG_FACTOR       <const> = 10
local PAN_FACTOR        <const> = 10
local CHAR_WIDTH        <const> = 10
local CHAR_HEIGHT       <const> = 16
local PADDING_WIDTH     <const> = 240
local MAP_CLICK_BLOCK   <const> = "P1 Fire" -- prevent this input while clicking on map buttons

local TrackedType = {
	THING  = 0,
	LINE   = 1,
	SECTOR = 2
}

-- Map colors (0xAARRGGBB or "name")
local MapPrefs = {
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

-- shortcuts

local text     = gui.text
local box      = gui.drawBox
local drawline = gui.drawLine

local Globals      = structs.globals
local MobjType     = enums.mobjtype
local SpriteNumber = enums.doom.spritenum
local MobjFlags    = enums.mobjflags


-- TOP LEVEL VARIABLES

local Zoom           = 1
local Follow         = false
local Hilite         = false
local Init           = true
local LastFramecount = -1
local ScreenWidth    = client.screenwidth()
local ScreenHeight   = client.screenheight()

-- tables

local Players = {}
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
local Tracked = {
	[TrackedType.THING] = {
		TrackedList = {},
		IDs         = {},
		Current     = -1,
		Name        = "thing"
	},
	[TrackedType.LINE] = {
		TrackedList = {},
		IDs         = {},
		Current     = -1,
		Name        = "line"
	},
	[TrackedType.SECTOR] = {
		TrackedList = {},
		IDs         = {},
		Current     = -1,
		Name        = "sector"
	}
}

-- forward declarations

local Input
local Lines
local PlayerTypes
local EnemyTypes
local MissileTypes
local MiscTypes
local InertTypes
local CurrentPrompt
local LastEpisode
local LastMap
local LastInput

--gui.defaultPixelFont("fceux")
gui.use_surface("client")
client.SetClientExtraPadding(PADDING_WIDTH, 0, 0, 0)

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
	return math.floor(((coord / FRACUNIT) + Pan.x) * Zoom)
end

local function decode_y(coord)
	return math.floor(((-coord / FRACUNIT) + Pan.y) * Zoom)
end

local function encode_x(coord)
	return math.floor(((coord / Zoom) - Pan.x) * FRACUNIT)
end

local function encode_y(coord)
	return -math.floor(((coord / Zoom) - Pan.y) * FRACUNIT)
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

local function follow_toggle()
	if Mouse.Left and not LastMouse.left then
		Follow = not Follow
	end
end

local function hilite_toggle()
	if Mouse.Left and not LastMouse.left then
		Hilite = not Hilite
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
	--[[--
	local playercount       = 0
	local total_killcount   = 0
	local total_itemcount   = 0
	local total_secretcount = 0
	local stats             = "      HP Armr Kill Item Secr\n"
	--]]--
	for i, player in Globals:iterate_players() do
		Players[i] = {
			x     = player.mo.x     / FRACUNIT,
			y     = player.mo.y     / FRACUNIT,
			z     = player.mo.z     / FRACUNIT,
			prevx = player.mo.PrevX / FRACUNIT,
			prevy = player.mo.PrevY / FRACUNIT,
			prevz = player.mo.PrevZ / FRACUNIT,
			momx  = player.mo.momx  / FRACUNIT,
			momy  = player.mo.momy  / FRACUNIT,
			angle = math.floor(player.mo.angle * (ANGLES / ANGLE_90))
		}
		
		Players[i].distx      = Players[i].x - Players[i].prevx
		Players[i].disty      = Players[i].y - Players[i].prevy
		Players[i].distz      = Players[i].z - Players[i].prevz		
		Players[i].distmoved  = math.sqrt(
			Players[i].distx * Players[i].distx +
			Players[i].disty * Players[i].disty)
		
		if Players[i].distx == 0 and Players[i].disty == 0 then
			Players[i].dirmoved = 0
		else
			local angle = math.atan(Players[i].distx / Players[i].disty) * 180 / math.pi - 90
			if Players[i].disty >= 0
			then Players[i].dirmoved = -angle
			else Players[i].dirmoved = -angle + 180
			end
		end
		--[[--
		playercount       = playercount + 1
		local killcount   = player.killcount
		local itemcount   = player.itemcount
		local secretcount = player.secretcount

		total_killcount   = total_killcount   + killcount
		total_itemcount   = total_itemcount   + itemcount
		total_secretcount = total_secretcount + secretcount

		stats = string.format("%s P%i %4i %4i %4i %4i %4i\n",
			stats, i, player.health, player.armorpoints1, killcount, itemcount, secretcount)
		--]]--
	end
	--[[--
	if playercount > 1 then
		stats = string.format("%s %-12s %4i %4i %4i\n", stats, "All", total_killcount, total_itemcount, total_secretcount)
	end
	text(0, 0, stats, nil, "topright")
	--]]--
end

local function iterate()
	if Init then return end

	init_cache()
	
	local closest_line
	local selected_sector
	local texts         = {}
	local player        = select(2, next(Players)) -- first present player only for now
	local mousePos      = client.transformPoint(Mouse.X, Mouse.Y)
	local gameMousePos  = screen_to_game(mousePos)
	local shortest_dist = math.maxinteger
					
	texts.player = string.format(
		"    X: %.6f\n    Y: %.6f\n    Z: %.2f\n" ..
		"distX: %.6f\ndistY: %.6f\ndistZ: %.2f\n" ..
		" momX: %.6f\n momY: %.6f\n" ..
		"distM: %.6f\n dirM: %.6f\nangle: %d",
		player.x,
		player.y,
		player.z,
		player.distx,
		player.disty,
		player.distz,
		player.momx,
		player.momy,
		player.distmoved,
		player.dirmoved,
		player.angle
	)
	
	for _, sector in pairs(Globals.sectors) do
		local index   = sector.iSectorID
		local entity  = Tracked[TrackedType.SECTOR]
		local list    = entity.TrackedList
		
		if #list > 0 then
			local id = list[entity.Current]
			
			if id == index then
				texts.sector = string.format(
					"SECTOR %d  spec: %d\nflo: %.2f  ceil: %.2f",
					index,
					sector.special,
					sector.floorheight   / FRACUNIT,
					sector.ceilingheight / FRACUNIT)
			end
		end
	end
	
	for i, line in ipairs(Lines) do
		local x1, y1, x2, y2 = game_to_screen(cached_line_coords(line))
		local color   = 0xffffffff
		local special = line.special
		local index   = line.iLineID
		local entity  = Tracked[TrackedType.LINE]
		local list    = entity.TrackedList

		if special ~= 0 then color = 0xffff00ff end

		drawline(x1, y1, x2, y2, color) -- no speedup from doing range check
		x1, y1, x2, y2 = cached_line_coords(line)
		
		if #list > 0 then
			local id       = list[entity.Current]
			local distance = distance_from_line(
				{ x = player.x,      y = player.y      },
				{ x = x1 / FRACUNIT, y = y1 / FRACUNIT },
				{ x = x2 / FRACUNIT, y = y2 / FRACUNIT }
			)
			
			if id == index then
				texts.line = string.format(
					"LINEDEF %d  dist: %.0f\nv1 x: %5d  y: %5d\nv2 x: %5d  y: %5d",
					index, distance,
					math.floor(x1 / FRACUNIT),
					math.floor(y1 / FRACUNIT),
					math.floor(x2 / FRACUNIT),
					math.floor(y2 / FRACUNIT))
			end
		end
		
		if Hilite then
			local dist = distance_from_line(
				gameMousePos,
				tuple_to_vertex(x1, y1),
				tuple_to_vertex(x2, y2))
			
			if math.abs(dist) < shortest_dist then
				shortest_dist = math.abs(dist)
				closest_line = line
			end
		end
	end
	
	if mousePos.x > PADDING_WIDTH and not CurrentPrompt then
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
				drawline(x1, y1, x2, y2, 0xff00ffff)
				texts.sector = texts.sector or string.format(
					"SECTOR %d  spec: %d\nflo: %.2f  ceil: %.2f",
					selected_sector.iSectorID,
					selected_sector.special,
					selected_sector.floorheight   / FRACUNIT,
					selected_sector.ceilingheight / FRACUNIT)
			end
		end
		
		if closest_line then
			local x1, y1, x2, y2 = cached_line_coords(closest_line)
			local distance = distance_from_line(
				{ x = player.x,      y = player.y      },
				{ x = x1 / FRACUNIT, y = y1 / FRACUNIT },
				{ x = x2 / FRACUNIT, y = y2 / FRACUNIT }
			)
			
			x1, y1, x2, y2 = game_to_screen(x1, y1, x2, y2)		
			drawline(x1, y1, x2, y2, 0xffff8800)
			texts.line = texts.line or string.format(
				"LINEDEF %d  dist: %.0f\nv1 x: %5d  y: %5d\nv2 x: %5d  y: %5d",
				closest_line.iLineID, distance,
				math.floor(closest_line.v1.x / FRACUNIT),
				math.floor(closest_line.v1.y / FRACUNIT),
				math.floor(closest_line.v2.x / FRACUNIT),
				math.floor(closest_line.v2.y / FRACUNIT))
		end
	end

	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local entity = Tracked[TrackedType.THING]
		local list   = entity.TrackedList
		local type   = mobj.type
		local index  = mobj.index
		local radius_color, text_color = get_mobj_color(mobj, type)
		
		-- players have index -1, things to be removed have -2
		if index >= 0 then
			entity.IDs[index] = true
		end
		
		if #list > 0 then
			local id = list[entity.Current]
			
			if id == index then
				texts.thing = string.format(
					"THING %d (%s)\nx:    %.5f\ny:    %.5f\nz:    %.2f" ..
					"  rad:  %.0f\ntics: %d     hp:   %d\nrt:   %d     thre: %d",
					mobj.index, MobjType[type],
					mobj.x      / FRACUNIT,
					mobj.y      / FRACUNIT,
					mobj.z      / FRACUNIT,
					mobj.radius / FRACUNIT,
					mobj.tics,
					mobj.health,
					mobj.reactiontime,
					mobj.threshold)
			end
		end
		
		if radius_color or text_color then -- not hidden
			local pos = tuple_to_vertex(game_to_screen(mobj.x, mobj.y))

			if  in_range(pos.x, 0, ScreenWidth)
			and in_range(pos.y, 0, ScreenHeight)
			then
				local type   = mobj.type
				local radius = mobj.radius
				local screen_radius = math.floor((radius / FRACUNIT) * Zoom)
				
				if  Hilite
				and in_range(mousePos.x, pos.x - screen_radius, pos.x + screen_radius)
				and in_range(mousePos.y, pos.y - screen_radius, pos.y + screen_radius)
				and mousePos.x > PADDING_WIDTH and not CurrentPrompt
				then
					radius_color = "white"
					text_color   = "white"
					
					texts.thing = texts.thing or string.format(
						"THING %d (%s)\nx:    %.5f\ny:    %.5f\nz:    %.2f" ..
						"  rad:  %.0f\ntics: %d     hp:   %d\nrt:   %d     thre: %d",
						mobj.index, MobjType[type],
						mobj.x      / FRACUNIT,
						mobj.y      / FRACUNIT,
						mobj.z      / FRACUNIT,
						mobj.radius / FRACUNIT,
						mobj.tics,
						mobj.health,
						mobj.reactiontime,
						mobj.threshold)
				end
				
				if radius_color then
					box(
						pos.x - screen_radius, 
						pos.y - screen_radius,
						pos.x + screen_radius,
						pos.y + screen_radius,
						radius_color)
				end
				
				if text_color then
					text(
						pos.x - screen_radius + 1,
						pos.y - screen_radius,
						string.format("%d", index),
						text_color)
				end
			end
		end
	end
	
	box ( 0,  0, PADDING_WIDTH, ScreenHeight, 0xb0000000, 0xb0000000)	
	text(10, 42, texts.player, MapPrefs.player.color)
	
	if texts.thing  then text(10, 222, texts.thing             ) end
	if texts.line   then text(10, 320, texts.line,   0xffff8800) end
	if texts.sector then text(10, 370, texts.sector, 0xff00ffff) end
	
	texts.thing = nil
end

local function init_mobj_bounds()
	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local x = mobj.x / FRACUNIT
		local y = mobj.y / FRACUNIT * -1
		if x < OB.left   then OB.left   = x end
		if x > OB.right  then OB.right  = x end
		if y < OB.top    then OB.top    = y end
		if y > OB.bottom then OB.bottom = y end
	end
end

local function update_zoom()
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
	Lines     = nil
	reset_view()
	Tracked[TrackedType.THING ].TrackedList = {}
	Tracked[TrackedType.THING ].IDs         = {}
	Tracked[TrackedType.THING ].Current     = -1
	Tracked[TrackedType.LINE  ].TrackedList = {}
	Tracked[TrackedType.LINE  ].IDs         = {}
	Tracked[TrackedType.LINE  ].Current     = -1
	Tracked[TrackedType.SECTOR].TrackedList = {}
	Tracked[TrackedType.SECTOR].IDs         = {}
	Tracked[TrackedType.SECTOR].Current     = -1
end

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

local function make_button(x, y, name, func)
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
	and not CurrentPrompt then
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

local function check_press(key)
	return Input[key] and not LastInput[key]
end

local function input_prompt()
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

local function add_entity(type)
	if CurrentPrompt then return end
	
	local entity = Tracked[type]
	local lookup = entity.IDs
	local array  = entity.TrackedList
	local name   = entity.Name
	
	CurrentPrompt = {
		msg = type,
		fun = function(id)
			if not lookup[id] then
				print(string.format(
					"\nERROR: Can't add %s %d because it doesn't exist!\n", name, id
				))
				return
			end
			
			--[[
			we either look for items longer with big tracked lists, or we add by index
			and potentially waste memory if there are thousands of entities in a map
			because gaps will also be there. since people are unlikely to track hundreds
			of items, relying on traversing the whole list every time is probably fine.
			--]]
			for i = 1, #array do
				if array[i] == id then
					print(string.format(
						"\nERROR: Can't add %s %d because it's already there!\n", name, id
					))
					return
				end
			end
			table.insert(array, id)
			Tracked[type].Current = #array
			print(string.format("Added %s %d", name, id))
		end,
		value = nil
	}
end

local function make_buttons()
	make_button(-115,  30, "Add Sector", function() add_entity(TrackedType.SECTOR) end)
	make_button(-210,  30, "Add Line",   function() add_entity(TrackedType.LINE  ) end)
	make_button(-315,  30, "Add Thing",  function() add_entity(TrackedType.THING ) end)
	make_button(  10, -40, "+",          function() zoom      ( 1                ) end)
	make_button(  10, -10, "-",          function() zoom      (-1                ) end)
	make_button(  40, -24, "<",          pan_left  )
	make_button(  64, -40, "^",          pan_up    )
	make_button(  64, -10, "v",          pan_down  )
	make_button(  88, -24, ">",          pan_right )
	make_button( 118, -40, "Reset View", reset_view)
	make_button( 118, -10,
		string.format("Follow %s",    Follow and "ON " or "OFF"), follow_toggle)
	make_button(-460, 30,
		string.format("Highlight %s", Hilite and "ON " or "OFF"), hilite_toggle)
	
	if CurrentPrompt then
		input_prompt()
	end
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
	ScreenWidth  = client.screenwidth()
	ScreenHeight = client.screenheight()
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
	ScreenWidth      = client.screenwidth()
	ScreenHeight     = client.screenheight()

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
		iterate_players()
		iterate()
		LastMouse.left = Mouse.Left
	end

	LastScreenSize.w = ScreenWidth
	LastScreenSize.h = ScreenHeight
	LastFramecount   = framecount

	emu.yield()
end