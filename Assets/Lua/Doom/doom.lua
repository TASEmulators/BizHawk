-- feos, kalimag, 2025-2026
---@diagnostic disable

dofile("doom.misc.lua")


--#region ACTUAL WORK

local function iterate_players()
	for i, player in Globals:iterate_players() do
		Players[i] = {
			thinker = player.mo.thinker._address,
			x       = player.mo.x     / FRACUNIT,
			y       = player.mo.y     / FRACUNIT,
			z       = player.mo.z     / FRACUNIT,
			prevx   = player.mo.PrevX / FRACUNIT,
			prevy   = player.mo.PrevY / FRACUNIT,
			prevz   = player.mo.PrevZ / FRACUNIT,
			momx    = player.mo.momx  / FRACUNIT,
			momy    = player.mo.momy  / FRACUNIT,
			angle   = math.floor(player.mo.angle * (Angle / ANGLE_90))
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

		if not Players.Current then Players.Current = i end
		if not Players.Min     then Players.Min     = i end
		Players.Max = i
	end
end

local function thing_handler()
	if not ShowMap then return end
	
	local mousePos = client.transformPoint(Mouse.X, Mouse.Y)
	
	for _, mobj in pairs(Globals.mobjs:readbulk()) do
		local type   = mobj.type
		local index  = mobj.index
		local angle  = mobj.angle * (AngleType.DEGREES / ANGLE_90) - AngleType.DEGREES
		local radius = math.floor((mobj.radius / FRACUNIT) * Zoom)
		local name   = MobjType[type]
		local radius_color, text_color = get_mobj_color(mobj, type)
		
		if radius_color or text_color then -- not hidden
			local pos      = tuple_to_vertex(game_to_screen(mobj.x, mobj.y))
			local triangle = rotate_triangle({
				a = { x = pos.x - radius / 2, y = pos.y          },
				b = { x = pos.x,              y = pos.y - radius },
				c = { x = pos.x + radius / 2, y = pos.y          },
				center = pos,
			}, -angle)

			drawline(triangle.a.x, triangle.a.y, triangle.b.x, triangle.b.y, radius_color)
			drawline(triangle.b.x, triangle.b.y, triangle.c.x, triangle.c.y, radius_color)
		--	drawline(triangle.c.x, triangle.c.y, triangle.a.x, triangle.a.y, radius_color)

			if name == "PLAYER" then
				for i, player in pairs(Players) do
					if player.thinker == mobj.thinker._address then
						name  = name .. " " .. i
						index = i -- override local index for players
						break
					end
				end
			end

			if  in_range(pos.x, 0, ScreenWidth)
			and in_range(pos.y, 0, ScreenHeight)
			then
				
				if  Hilite
				and in_range(mousePos.x, pos.x - radius, pos.x + radius)
				and in_range(mousePos.y, pos.y - radius, pos.y + radius)
				and mousePos.x > PADDING_WIDTH and not freeze_gui()
				then
					radius_color = "white"
					text_color   = "white"
					
					GUITexts.thing = string.format(
						"  THING %d (%s)\n   x: %.5f\n   y: %.5f\n   z: %.2f" ..
						"   rad: %.0f\ntics: %d     hp:   %d\n  rt: %d     thre: %d",
						mobj.index, -- original index display
						name,
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
						pos.x - radius, 
						pos.y - radius,
						pos.x + radius,
						pos.y + radius,
						radius_color)
				end
				
				if text_color and index >= 0 then
					text(
						pos.x - radius + 1,
						pos.y - radius,
						string.format("%d", index),
						text_color)
				end
			end
		end
	end
end

local function line_handler()
	if not ShowMap then return end
	
	local closestLine, selectedSector
	local player       = Players[Players.Current]
	local mousePos     = client.transformPoint(Mouse.X, Mouse.Y)
	local gameMousePos = screen_to_game(mousePos)
	local shortestDist = math.maxinteger
	
	for _, line in ipairs(Lines) do
		local x1, y1, x2, y2 = game_to_screen(cached_line_coords(line))
		local color   = 0xffffffff
		local special = line.special
		local index   = line.iLineID
		local entity  = Tracked[TrackedType.LINE]
		local list    = entity.TrackedList

		if special ~= 0 then color = 0xffff00ff end

		drawline(x1, y1, x2, y2, color) -- no speedup from doing range check
		x1, y1, x2, y2 = cached_line_coords(line)
		
		if Hilite then
			local dist = math.abs(distance_to_segment(
				gameMousePos,
				tuple_to_vertex(x1, y1),
				tuple_to_vertex(x2, y2)))
			
			if dist < shortestDist then
				shortestDist = dist
				closestLine  = line
			end
		end
	end
	
	if mousePos.x > PADDING_WIDTH and not freeze_gui() then
		if closestLine then
			local x1, y1, x2, y2 = game_to_screen(cached_line_coords(closestLine))
			local side =
				(mousePos.y - y1) * (x2 - x1) -
				(mousePos.x - x1) * (y2 - y1)
			
			if side <= 0 then
				if closestLine.backsector then
					selectedSector = closestLine.backsector
				end
			else
				if closestLine.frontsector then
					selectedSector = closestLine.frontsector
				end
			end
		end
		
		if selectedSector then
			for _, line in ipairs(selectedSector.lines) do
				-- cached_line_coords gives some length error?
				local x1, y1, x2, y2 = game_to_screen(line:coords())
				drawline(x1, y1, x2, y2, 0xff00ffff)
				GUITexts.sector = string.format(
					"  SECTOR %d\nspecial: %d\n  floor: %.2f\nceiling: %.2f",
					selectedSector.iSectorID,
					selectedSector.special,
					selectedSector.floorheight   / FRACUNIT,
					selectedSector.ceilingheight / FRACUNIT)
			end
		end
		
		if closestLine then
			local x1, y1, x2, y2 = cached_line_coords(closestLine)
			local v1              = { x = x1 / FRACUNIT, y = y1 / FRACUNIT }
			local v2              = { x = x2 / FRACUNIT, y = y2 / FRACUNIT }
			local distanceLine    = distance_to_line   ({ x = player.x, y = player.y }, v1, v2)
			local distanceSegment = distance_to_segment({ x = player.x, y = player.y }, v1, v2)
			
			x1, y1, x2, y2 = game_to_screen(x1, y1, x2, y2)		
			drawline(x1, y1, x2, y2, 0xffff8800)
			GUITexts.line = string.format(
				"  LINEDEF %d\n"..
				"dist(line): %f\ndist( seg): %f\n"..
				"v1 x: %5d  y: %5d\nv2 x: %5d  y: %5d",
				closestLine.iLineID,
				distanceLine, distanceSegment,
				math.floor(closestLine.v1.x / FRACUNIT),
				math.floor(closestLine.v1.y / FRACUNIT),
				math.floor(closestLine.v2.x / FRACUNIT),
				math.floor(closestLine.v2.y / FRACUNIT))
		end
	end
end

local function tracked_handler()
	local player   = Players[Players.Current]
	local mousePos = client.transformPoint(Mouse.X, Mouse.Y)
	
	if Tracked[TrackedType.THING].Current then
		local entity = Tracked[TrackedType.THING]
		local min    = entity.Current == entity.Min and Scroller.NONE or Scroller.LEFT
		local max    = entity.Current == entity.Max and Scroller.NONE or Scroller.RIGHT
		local mobj   = entity.TrackedList[entity.Current]
		
		if mousePos.x <= PADDING_WIDTH
		and in_range(mousePos.y, TextPosY.THING, TextPosY.LINE-1) then
			local delete = false
			box(0, TextPosY.THING, PADDING_WIDTH, TextPosY.LINE-1, 0xffffffff, 0x88ffffff)
			make_button(PADDING_WIDTH-36, TextPosY.THING+22, " X ", function() delete = true end)
			
			if input.get()["Delete"] or delete then
				Confirmation = {
					type = TrackedType.THING,
					id   = mobj.index
				}
			end
		end
		
		GUITexts.thing  = string.format(
			"%sTHING %d (%s)%s\n   x: %.5f\n   y: %.5f\n   z: %.2f" ..
			"   rad: %.0f\ntics: %d    hp:   %d\n  rt: %d    thre: %d",
			min, mobj.index, MobjType[mobj.type], max,
			mobj.x      / FRACUNIT,
			mobj.y      / FRACUNIT,
			mobj.z      / FRACUNIT,
			mobj.radius / FRACUNIT,
			mobj.tics,
			mobj.health,
			mobj.reactiontime,
			mobj.threshold)
	end
	
	if Tracked[TrackedType.LINE].Current then
		local entity          = Tracked[TrackedType.LINE]
		local min             = entity.Current == entity.Min and Scroller.NONE or Scroller.LEFT
		local max             = entity.Current == entity.Max and Scroller.NONE or Scroller.RIGHT
		local line            = entity.TrackedList[entity.Current]
		local x1, y1, x2, y2  = line:coords()
		local v1              = { x = x1 / FRACUNIT, y = y1 / FRACUNIT }
		local v2              = { x = x2 / FRACUNIT, y = y2 / FRACUNIT }
		local distanceLine    = distance_to_line   ({ x = player.x, y = player.y }, v1, v2)
		local distanceSegment = distance_to_segment({ x = player.x, y = player.y }, v1, v2)
		
		if mousePos.x <= PADDING_WIDTH
		and in_range(mousePos.y, TextPosY.LINE, TextPosY.SECTOR-1) then
			local delete = false
			box(0, TextPosY.LINE, PADDING_WIDTH, TextPosY.SECTOR-1, 0xffffffff, 0x88ffffff)
			make_button(PADDING_WIDTH-36, TextPosY.LINE+22, " X ", function() delete = true end)
			
			if input.get()["Delete"] or delete then
				Confirmation = {
					type = TrackedType.LINE,
					id   = line.iLineID
				}
			end
		end
		
		GUITexts.line = string.format(
			"%sLINEDEF %d%s\n"..
			"dist(line): %f\ndist( seg): %f\n"..
			"v1 x: %5d  y: %5d\nv2 x: %5d  y: %5d",
			min, line.iLineID, max,
			distanceLine, distanceSegment,
			math.floor(x1 / FRACUNIT),
			math.floor(y1 / FRACUNIT),
			math.floor(x2 / FRACUNIT),
			math.floor(y2 / FRACUNIT))
	end
	
	if Tracked[TrackedType.SECTOR].Current then
		local entity = Tracked[TrackedType.SECTOR]
		local min    = entity.Current == entity.Min and Scroller.NONE or Scroller.LEFT
		local max    = entity.Current == entity.Max and Scroller.NONE or Scroller.RIGHT
		local sector = entity.TrackedList[entity.Current]
		
		if mousePos.x <= PADDING_WIDTH
		and in_range(mousePos.y, TextPosY.SECTOR, TextPosY.SECTOR+64) then
			local delete = false
			box(0, TextPosY.SECTOR, PADDING_WIDTH, TextPosY.SECTOR+64, 0xffffffff, 0x88ffffff)
			make_button(PADDING_WIDTH-36, TextPosY.SECTOR+22, " X ", function() delete = true end)
			
			if input.get()["Delete"] or delete then
				Confirmation = {
					type = TrackedType.SECTOR,
					id   = sector.iSectorID
				}
			end
		end
		
		GUITexts.sector = string.format(
			"%sSECTOR %d%s\nspecial: %d\n  floor: %.2f\nceiling: %.2f",
			min, sector.iSectorID, max,
			sector.special,
			sector.floorheight   / FRACUNIT,
			sector.ceilingheight / FRACUNIT)
	end
end

local function dialog_handler()
	
	Input = input.get()
	
	if CurrentPrompt then
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
		
		local ret = show_dialog(string.format(
			"Enter %s ID from\nlevel editor.\n\n" ..
			"Hit \"Enter\" to confirm,\n" ..
			"\"Backspace\" to erase,\n" ..
			"or \"Escape\" to cancel.\n" ..
			"Or use the buttons.\n\n%s_\n",
			CurrentPrompt.msg, value
		))

		if ret == true and value ~= "" then
			CurrentPrompt.fun(tonumber(value))
			CurrentPrompt = nil
			return
		elseif ret == false then
			CurrentPrompt = nil
			return
		end
		
		if value ~= "" then
			CurrentPrompt.value = tonumber(value)
		else
			CurrentPrompt.value = nil
		end
	elseif Confirmation then
		local entity = Tracked[Confirmation.type]
		local ret    = show_dialog(string.format(
			"Stop tracking %s %d?\n\n" ..
			"Hit \"Enter\" to confirm,\n" ..
			"or \"Escape\" to cancel.\n" ..
			"Or use the buttons.",
			entity.Name,
			Confirmation.id
		))
		
		if check_press("Escape") or ret == false then
			Confirmation = nil
		elseif (check_press("Enter") or check_press("KeypadEnter")) or ret == true then
			
			if entity.Min == entity.Max then
				-- it was the final entry, clear the whole thing
				entity:clear()
			else
				if entity.Max == Confirmation.id then
					scroll_list(entity, -1)
					entity.Max = entity.Current
				else
					scroll_list(entity, 1)
					
					if entity.Min == Confirmation.id then
						entity.Min = entity.Current
					end
				end
				entity.TrackedList[Confirmation.id] = nil
			end
			
			Confirmation = nil
		end
	end
		
	LastInput = Input
end

local function draw_grid()
	if not (ShowMap and ShowGrid) then return end
	
	BlockmapWidth  = Globals.bmapwidth
	BlockmapOrigin = {
		x = Globals.bmaporgx,
		y = Globals.bmaporgy
	}
	BlockmapEnd = { 
		x = BlockmapOrigin.x + BlockmapWidth      * GRID_SIZE * FRACUNIT,
		y = BlockmapOrigin.y + Globals.bmapheight * GRID_SIZE * FRACUNIT
	}
	
	if BlockmapWidth ~= 0 then
		local size  = GRID_SIZE * FRACUNIT
		local step  = GRID_SIZE * Zoom
		local bmorg = game_to_screen(BlockmapOrigin)
		local bmend = game_to_screen(BlockmapEnd)
		local start = { x = bmorg.x, y = bmend.y }
		local stop  = { x = bmend.x, y = bmorg.y }
		
		for x = start.x, stop.x-1, step do
			drawline(x, start.y, x, stop.y, MapPrefs.grid.color)
		end
		for y = start.y, stop.y-1, step do
			drawline(start.x, y, stop.x, y, MapPrefs.grid.color)
		end
		
		-- due to step being float in screen coords, we can't avoid rounding error
		-- so final 2 lines won't match grid size and sometimes won't even be drawn
		-- so we draw them separately where they "should be"
		-- while embracing the rounding error of all the rest
		-- since drawing them all perfectly is too complicated
		drawline(stop.x, start.y, stop.x, stop.y, MapPrefs.grid.color)
		drawline(start.x, stop.y, stop.x, stop.y, MapPrefs.grid.color)
	end

	-- if overflow corrupted actual blockmap
	-- we still use original values just to show where it happened
	for block,timer in pairs(MapBlocks) do
		local forecolor
		local backcolor
		local delta    = timer - Framecount
		local visblock = block
		
		if block < 0 then -- custom way to indicate overflow
			visblock  = -block
			forecolor = 0xffffff00
			backcolor = 0x33ffff00
		elseif delta == FADEOUT_TIMER - 1 then
			forecolor = 0xffff0000
			backcolor = 0x33ff0000
		else
			forecolor = 0x88aaaaaa
			-- dynamically reduced alpha
			backcolor = (math.floor(0x88 / FADEOUT_TIMER * delta) << 24) | 0x888888
		end
		
		if delta > 0 then
			-- positioning precision is a bit higher than of the overall grid
			-- so it may look off at some zoom levels
			local origin = game_to_screen(LastBMOrigin)
			local x      = origin.x +            visblock % LastBMWidth  * GRID_SIZE * Zoom
			local y      = origin.y - math.floor(visblock / LastBMWidth) * GRID_SIZE * Zoom
			
			-- if overflow happened, only show its block(s)
			if InterceptsInfo < InterceptsState.OVERFLOW then
				box(x, y, x + GRID_SIZE * Zoom, y - GRID_SIZE * Zoom, forecolor, backcolor)
			elseif block < 0 then
				text(x, y - GRID_SIZE * Zoom, visblock, forecolor)
				box(x, y, x + GRID_SIZE * Zoom, y - GRID_SIZE * Zoom, forecolor, backcolor)
			end
		else
			MapBlocks[block] = nil
		end
	end
end

local function draw_tracelines()
	if not ShowMap then return end
	
	for key,timer in pairs(DivLines) do
		local color
		local i     = 0
		local line  = {}
		local delta = timer - Framecount
		
		for token in string.gmatch(key, "([^%s]+)") do
		   line[i] = tonumber(token)
		   i = i + 1
		end
		
		-- full red or grey with dynamically reduced alpha
		if delta == FADEOUT_TIMER - 1
		then color = 0xffff0000
		else color = (math.floor(0xff / FADEOUT_TIMER * delta) << 24) | 0x888888
		end
		
		if delta > 0 then
			local x1, y1, x2, y2 = game_to_screen(line[0], line[1], line[2], line[3])
			drawline(x1, y1, x2, y2, color)
		else
			DivLines[key] = nil
		end
	end
end

local function iterate()
	if Init then return end
	
	local mousePos  = client.transformPoint(Mouse.X, Mouse.Y)
	local player    = Players[Players.Current]
	local rngindex  = Globals.rng.rndindex
	local min       = Players.Current == Players.Min and Scroller.NONE or Scroller.LEFT
	local max       = Players.Current == Players.Max and Scroller.NONE or Scroller.RIGHT
	GUITexts        = {}
	GUITexts.player = string.format(
		"     %sPLAYER %d%s\n" ..
		"    X: %.6f\n    Y: %.6f\n    Z: %.2f\n" ..
		"distX: %.6f\ndistY: %.6f\ndistZ: %.2f\n" ..
		" momX: %.6f\n momY: %.6f\n" ..
		"distM: %.6f\n dirM: %.6f\nangle: %d\n",
		min, Players.Current, max,
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
	
	draw_grid()
	tracked_handler()
	thing_handler()
	line_handler()
	draw_tracelines()

	if mousePos.x <= PADDING_WIDTH
	and in_range(mousePos.y, TextPosY.PLAYER, TextPosY.THING-1) then
		box(0, TextPosY.PLAYER, PADDING_WIDTH, TextPosY.THING-1, 0xffffffff, 0x88ffffff)
	end
	
	box(0, 0, PADDING_WIDTH, ScreenHeight, 0xb0000000, 0xb0000000)
	text(10, TextPosY.PLAYER, GUITexts.player, MapPrefs.player.color)
	text(
		PADDING_WIDTH,
		ScreenHeight - 32 * ScreenHeight / 200 - 50, -- just above hud
		string.format(
			" tic: %d\n" ..
			"time: %.2f\n" .. -- xdre limits to centiseconds
			" rng: #%03d %d",
			Globals.gametic - 1,
			Globals.leveltime / 35,
			rngindex,
			memory.readbyte(memory.read_u32_le(symbols.rndtable) + rngindex, "System Bus")
	))
	if GUITexts.thing  then text(10, TextPosY.THING,  GUITexts.thing             ) end
	if GUITexts.line   then text(10, TextPosY.LINE,   GUITexts.line,   0xffff8800) end
	if GUITexts.sector then text(10, TextPosY.SECTOR, GUITexts.sector, 0xff00ffff) end
end

local function make_buttons()
	local w = PADDING_WIDTH -- horizontal offset
	local h = 26            -- button height
	
	make_button(w+5, h*1, "Add Thing ", function() add_entity(TrackedType.THING ) end)
	make_button(w+5, h*2, "Add Line  ", function() add_entity(TrackedType.LINE  ) end)
	make_button(w+5, h*3, "Add Sector", function() add_entity(TrackedType.SECTOR) end)
	
	local useName = "  NONE"
	if     LineUseLog == LineLogType.PLAYER then useName = "PLAYER"
	elseif LineUseLog == LineLogType.ALL    then useName = "   ALL"
	end
	
	local crossName = "  NONE"
	if     LineCrossLog == LineLogType.PLAYER then crossName = "PLAYER"
	elseif LineCrossLog == LineLogType.ALL    then crossName = "   ALL"
	end
	
	make_button(-170, h*1, "Log Use   "   ..useName,    function() cycle_log_types(true ) end)
	make_button(-170, h*2, "Log Cross "   ..crossName,  function() cycle_log_types(false) end)
	make_button(-170, h*3, "Log P_Random "..(RNGLog        and " ON" or "OFF"),   prandom_log)
	make_button(-170, h*4, "Log  Interc. "..(InterceptLog  and " ON" or "OFF"), intercept_log)
	make_button(-170, h*5, "Show Interc. "..(InterceptShow and " ON" or "OFF"),intercept_show)
	make_button(-110, h*6, "Map    "      ..(ShowMap       and " ON" or "OFF"),      map_show)
	make_button(-110, h*7, "Grid   "      ..(ShowGrid      and " ON" or "OFF"),     grid_show)
	make_button(-110, h*8, "Hilite "      ..(Hilite        and " ON" or "OFF"), hilite_toggle)
	make_button(-110, h*9, "Follow "      ..(Follow        and " ON" or "OFF"), follow_toggle)
	make_button(-110, h*10,"Reset View",                                           reset_view)
	
	if InterceptsInfo > InterceptsState.NONE then
		make_button(w+5, h*10, "Print Intercepts", function()
			print("")
			print("Intercepts in blocks =")
			print(dump(Intercepts))
			Intercepts = {}
			
			if InterceptsInfo == InterceptsState.OVERFLOW then
				print("")
				print("InterceptsOverruns in blocks =")
				print(dump(InterceptsOverruns))
				InterceptsOverruns = {}
			end
			
			InterceptsInfo = InterceptsState.NONE
		end)
	end

	dialog_handler()
end

--#endregion


--#region CALLBACKS

event.onframestart(function()
	PRandomInfo = {}
	
	if freeze_gui() then
		suppress_click_input()
	end
	
	if client.ispaused() then return end -- frameadvance while paused
	
	-- do this before frame start to suppress mouse click input
	ScreenWidth  = client.screenwidth()
	ScreenHeight = client.screenheight()
	make_buttons()
	update_zoom()
end)

event.onframeend(function()
	Framecount = emu.framecount()
	for _,v in pairs(PRandomInfo) do
		print(v)
	end
end)

event.onexit(function()
	gui.clearGraphics()
	gui.cleartext()
	settings_write()
end)

event.onloadstate(check_map_change)

tastudio.onbranchload(check_map_change)

--#endregion


--#region MAIN LOOP

while true do
	Mouse        = input.getmouse()
	ScreenWidth  = client.screenwidth()
	ScreenHeight = client.screenheight()
	local paused = client.ispaused()

	check_map_change()

	if Init then init_mobj_bounds() end

	-- clear cache after rewind, turbo etc.
	-- this is only necessary to invalidate line specials, the rest is handled by map change detection above
--	if Framecount ~= LastFramecount and Framecount ~= LastFramecount + 1 then
	if Globals.gamestate ~= 0 then
		clear_cache()
	end
	
	init_cache()

	if Globals.gamestate == 0 then
		iterate_players()
	end

	if paused then
		-- OSD text is not automatically cleared while paused
		gui.cleartext()
		gui.clearGraphics()
		
		-- while onframestart isn't called
		make_buttons()
		update_zoom()
	end

	-- workaround: prevent multiple execution per frame because of emu.yield(), except when paused
	if (Framecount ~= LastFramecount or paused)
	and Globals.gamestate == 0
	and emu.framecount() > 0
	then
		iterate()
		LastMouse.left = Mouse.Left
	end

	LastScreenSize.w = ScreenWidth
	LastScreenSize.h = ScreenHeight
	LastFramecount   = Framecount

	emu.yield()
end

--#endregion