-- feos, kalimag, 2025
dofile("doom.misc.lua")


-- ACTUAL WORK

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
	
	if Tracked[TrackedType.SECTOR].Current then
		local entity = Tracked[TrackedType.SECTOR]
		local sector = entity.TrackedList[entity.Current]
		texts.sector = string.format(
			"SECTOR %d  spec: %d\nflo: %.2f  ceil: %.2f",
			sector.iSectorID,
			sector.special,
			sector.floorheight   / FRACUNIT,
			sector.ceilingheight / FRACUNIT)
	end
	
	if Tracked[TrackedType.LINE].Current then
		local entity         = Tracked[TrackedType.LINE]
		local line           = entity.TrackedList[entity.Current]
		local x1, y1, x2, y2 = cached_line_coords(line)
		local distance       = distance_from_line(
			{ x = player.x,      y = player.y      },
			{ x = x1 / FRACUNIT, y = y1 / FRACUNIT },
			{ x = x2 / FRACUNIT, y = y2 / FRACUNIT }
		)
		texts.line = string.format(
			"LINEDEF %d  dist: %.0f\nv1 x: %5d  y: %5d\nv2 x: %5d  y: %5d",
			line.iLineID, distance,
			math.floor(x1 / FRACUNIT),
			math.floor(y1 / FRACUNIT),
			math.floor(x2 / FRACUNIT),
			math.floor(y2 / FRACUNIT))
	end
	
	if Tracked[TrackedType.THING].Current then
		local entity = Tracked[TrackedType.THING]
		local mobj   = entity.TrackedList[entity.Current]
		texts.thing  = string.format(
			"THING %d (%s)\nx:    %.5f\ny:    %.5f\nz:    %.2f" ..
			"  rad:  %.0f\ntics: %d     hp:   %d\nrt:   %d     thre: %d",
			mobj.index, MobjType[mobj.type],
			mobj.x      / FRACUNIT,
			mobj.y      / FRACUNIT,
			mobj.z      / FRACUNIT,
			mobj.radius / FRACUNIT,
			mobj.tics,
			mobj.health,
			mobj.reactiontime,
			mobj.threshold)
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
		local type   = mobj.type
		local index  = mobj.index
		local radius_color, text_color = get_mobj_color(mobj, type)
		
		-- players have index -1, things to be removed have -2
		if index >= 0 then
			entity.IDs[index] = true
		end
		
		if radius_color or text_color then -- not hidden
			local pos = tuple_to_vertex(game_to_screen(mobj.x, mobj.y))

			if  in_range(pos.x, 0, ScreenWidth)
			and in_range(pos.y, 0, ScreenHeight)
			then
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


-- MAIN LOOP

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


-- CALLBACKS

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