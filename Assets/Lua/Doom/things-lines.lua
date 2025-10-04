-- feos, 2025

local dsda = require("dsda-data")

-- CONSTANTS
local NULL_OBJECT       = 0x88888888 -- no object at that index
local OUT_OF_BOUNDS     = 0xFFFFFFFF -- no such index
local MINIMAL_ZOOM      = 0.0001     -- ???
local ZOOM_FACTOR       = 0.02
local WHEEL_ZOOM_FACTOR = 4
local DRAG_FACTOR       = 10
local PAN_FACTOR        = 10
local CHAR_WIDTH        = 10
local CHAR_HEIGHT       = 16
local NEGATIVE_MAXIMUM  = 1 << 63
local POSITIVE_MAXIMUM  = ~NEGATIVE_MAXIMUM
local MAP_CLICK_BLOCK   = "P1 Fire" -- prevent this input while clicking on map buttons

-- Map colors (0xAARRGGBB or "name" or false)
local ENEMY_COLOR       = 0xFFFF0000
local ENEMY_WAIT_COLOR  = 0xFFCC0000
local CORPSE_COLOR      = false -- 0x80CC0000
local MISSILE_COLOR     = 0xFFFF8000
local PLAYER_COLOR      = 0xFF0080FF
local COUNTITEM_COLOR   = false -- 0xA0FFFFFF
local ITEM_COLOR        = false -- 0xA0FFFFFF
local MISC_COLOR        = false -- 0xFFFFFFFF
local INERT_COLOR       = false -- 0x80808080

-- shortcuts
local rl       = memory.read_u32_le
local rw       = memory.read_u16_le
local rb       = memory.read_u8
local rls      = memory.read_s32_le
local rws      = memory.read_s16_le
local rbs      = memory.read_s8
local text     = gui.text
local box      = gui.drawBox
local drawline = gui.drawLine
--local text = gui.pixelText -- INSANELY SLOW

local PlayerOffsets = dsda.player.offsets -- player member offsets in bytes
local MobjOffsets   = dsda.mobj.offsets   -- mobj member offsets in bytes
local LineOffsets   = dsda.line.offsets   -- line member offsets in bytes
local MobjType      = dsda.mobjtype
local SpriteNumber  = dsda.doom.spritenum
local MobjFlags     = dsda.mobjflags

-- TOP LEVEL VARIABLES
local Zoom      = 1
local Init      = true
-- tables
-- view offset
local Pan = {
	x = 0,
	y = 0
}
-- object positions bounds
local OB = {
	top    = POSITIVE_MAXIMUM,
	left   = POSITIVE_MAXIMUM,
	bottom = NEGATIVE_MAXIMUM,
	right  = NEGATIVE_MAXIMUM
}
local LastScreenSize = {
	w = client.screenwidth(),
	h = client.screenheight()
}
local LastMouse = {
	x     = 0,
	y     = 0,
	wheel = 0
}
-- forward declarations
local Lines         = {}

--gui.defaultPixelFont("fceux")
gui.use_surface("client")

local function mapify_x(coord)
	return math.floor(((coord / 0xffff)+Pan.x)*Zoom)
end

local function mapify_y(coord)
	return math.floor(((coord / 0xffff)+Pan.y)*Zoom)
end

local function in_range(var, minimum, maximum)
	return var >= minimum and var <= maximum
end

local function reset_view()
	Init = true
	update_zoom()
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

local function zoom_out(times)
	for i=0, (times or 1) do
		local newZoom = Zoom * (1 - ZOOM_FACTOR)
		if newZoom < MINIMAL_ZOOM then return end
		Zoom = newZoom
		pan_left(1)
		pan_up(1.4)
	end
end

local function zoom_in(times)
	for i=0, (times or 1) do
		Zoom = Zoom * (1 + ZOOM_FACTOR)
		pan_right(1)
		pan_down(1.4)
	end
end

function maybe_swap(smaller, bigger)
	if smaller > bigger then
		return bigger, smaller
	end
	return smaller, bigger
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

local function get_mobj_color(mobj)
	local flags = mobj.flags
	if flags & (MobjFlags.PICKUP | MobjFlags.FRIEND) ~= 0 then return PLAYER_COLOR end
	if flags & (MobjFlags.COUNTKILL | MobjFlags.SKULLFLY) ~= 0 then
		if flags & MobjFlags.CORPSE ~= 0 then return CORPSE_COLOR end
		if flags & MobjFlags.AMBUSH ~= 0 or mobj.target == 0 then return ENEMY_WAIT_COLOR end
		return ENEMY_COLOR
	end
	if flags & MobjFlags.COUNTITEM ~= 0 then return COUNTITEM_COLOR end
	if flags & MobjFlags.SPECIAL ~= 0 then return ITEM_COLOR end
	if flags & MobjFlags.MISSILE ~= 0 then return MISSILE_COLOR end
	if flags & MobjFlags.SHOOTABLE ~= 0 then return MISC_COLOR end
	return INERT_COLOR
end

local function iterate_players()
	local playercount       = 0
	local total_killcount   = 0
	local total_itemcount   = 0
	local total_secretcount = 0
	local stats             = "      HP Armr Kill Item Secr\n"
	for addr, player in pairs(dsda.player.items) do
		playercount       = playercount + 1
		local killcount   = player.killcount
		local itemcount   = player.itemcount
		local secretcount = player.secretcount

		total_killcount   = total_killcount   + killcount
		total_itemcount   = total_itemcount   + itemcount
		total_secretcount = total_secretcount + secretcount

		stats = string.format("%s P%i %4i %4i %4i %4i %4i\n",
			stats, playercount, player.health, player.armorpoints1, killcount, itemcount, secretcount)
	end
	if playercount > 1 then
		stats = string.format("%s %-12s %4i %4i %4i\n", stats, "All", total_killcount, total_itemcount, total_secretcount)
	end
	gui.text(0, 0, stats, nil, "topright")
end

local function iterate()
	if Init then return end

	for addr, mobj in pairs(dsda.mobj.items) do
		local color  = get_mobj_color(mobj)
		if color then -- not hidden
			local pos    = { x = mapify_x(mobj.x), y = mapify_y(-mobj.y) }

			if  in_range(pos.x, 0, client.screenwidth())
			and in_range(pos.y, 0, client.screenheight())
			then
				local type   = mobj.type
				local radius = math.floor ((mobj.radius >> 16) * Zoom)
				--[[--
				local z      = mobj.z
				local index  = mobj.index
				local tics   = mobj.tics
				local health = mobj.health
				local sprite = SpriteNumber[mobj.sprite]
				--]]--

				type = MobjType[type]
				text(pos.x, pos.y, string.format("%s", type),  color)
				box(pos.x - radius, pos.y - radius, pos.x + radius, pos.y + radius, color)
			end
		end
	end
	
	for _, line in ipairs(Lines) do
		-- Line positions need to be updated for polyobjects
		-- No way to tell if a line is part of a polyobject, but they update validcount
		-- when moving so this is a decent way of cutting down on memory reads
		local validcount = line.validcount
		if validcount ~= line._validcount then
			line._validcount = validcount
			line._v1 = { x =  line.v1_x,
		                 y = -line.v1_y, }
			line._v2 = { x =  line.v2_x,
		                 y = -line.v2_y, }
		end
		local v1, v2  = line._v1, line._v2
		local special = line.special

		local color
		if special ~= 0 then color = 0xffcc00ff end

		drawline(
			mapify_x(v1.x),
			mapify_y(v1.y),
			mapify_x(v2.x),
			mapify_y(v2.y),
			color or 0xffcccccc)
	end
end

local function init_objects()
	for addr, mobj in pairs(dsda.mobj.items) do
		local x    = mobj.x / 0xffff
		local y    = mobj.y / 0xffff * -1
		if x < OB.left   then OB.left   = x end
		if x > OB.right  then OB.right  = x end
		if y < OB.top    then OB.top    = y end
		if y > OB.bottom then OB.bottom = y end
	end

	Lines = {}
	for addr, line in pairs(dsda.line.items) do
		table.insert(Lines, line)
	end
end

function update_zoom()
	local mouse      = input.getmouse()
	local mousePos   = client.transformPoint(mouse.X, mouse.Y)
	local deltaX     = mousePos.x - LastMouse.x
	local deltaY     = mousePos.y - LastMouse.y
	local newWheel   = math.floor(mouse.Wheel/120)
	local wheelDelta = newWheel - LastMouse.wheel
	
	if     wheelDelta > 0 then zoom_in ( wheelDelta * WHEEL_ZOOM_FACTOR)
	elseif wheelDelta < 0 then zoom_out(-wheelDelta * WHEEL_ZOOM_FACTOR)
	end
	
	if   mouse.Left
	and (input.get()["Shift"]
	or   input.get()["LeftShift"]) then
		if     deltaX > 0 then pan_left ( DRAG_FACTOR/deltaX)
		elseif deltaX < 0 then pan_right(-DRAG_FACTOR/deltaX)
		end
		if     deltaY > 0 then pan_up  ( DRAG_FACTOR/deltaY)
		elseif deltaY < 0 then pan_down(-DRAG_FACTOR/deltaY)
		end
	end
	
	LastMouse.x     = mousePos.x
	LastMouse.y     = mousePos.y
	LastMouse.left  = mouse.Left
	LastMouse.wheel = newWheel
	
	if not Init
	and LastScreenSize.w == client.screenwidth()
	and LastScreenSize.h == client.screenheight()
	then return end
	
	if  OB.top    ~= POSITIVE_MAXIMUM
	and OB.left   ~= POSITIVE_MAXIMUM
	and OB.right  ~= NEGATIVE_MAXIMUM
	and OB.bottom ~= NEGATIVE_MAXIMUM
	and not emu.islagged()
	then
		OB.left, OB.right  = maybe_swap(OB.left, OB.right)
		OB.top,  OB.bottom = maybe_swap(OB.top,  OB.bottom)
		local span        = { x = OB.right-OB.left+200,        y = OB.bottom-OB.top+200         }
		local scale       = { x = client.screenwidth()/span.x, y = client.screenheight()/span.y }
		local spanCenter  = { x = OB.left+span.x/2, y = OB.top+span.y/2 }
		      Zoom        = math.min(scale.x, scale.y)
		local sreenCenter = { x = client.screenwidth()/Zoom/2, y = client.screenheight()/Zoom/2 }
		      Pan.x       = -math.floor(spanCenter.x - sreenCenter.x)
		      Pan.y       = -math.floor(spanCenter.y - sreenCenter.y)
		      Init        = false
	end
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
	local mouse    = input.getmouse()
	local mousePos = client.transformPoint(mouse.X, mouse.Y)
	
	if  in_range(mousePos.x, x,           x+boxWidth)
	and in_range(mousePos.y, y-boxHeight, y         )
	and not input.get()["Shift"]
	and not input.get()["LeftShift"] then
		if mouse.Left then
			if MAP_CLICK_BLOCK and MAP_CLICK_BLOCK ~= "" then
				joypad.set({ [MAP_CLICK_BLOCK] = false })
			end
			colorIndex = 3
			func()
		else colorIndex = 2 end
	end
	
	box(x, y, x+boxWidth, y-boxHeight, 0xaaffffff, colors[colorIndex])
	text(textX, textY, name, colors[colorIndex] | 0xff000000) -- full alpha
end

function make_buttons()
	make_button( 10, client.screenheight()-70, "Zoom\nIn",    zoom_in   )
	make_button( 10, client.screenheight()-10, "Zoom\nOut",   zoom_out  )
	make_button( 80, client.screenheight()-40, "Pan\nLeft",   pan_left  )
	make_button(150, client.screenheight()-70, "Pan \nUp",    pan_up    )
	make_button(150, client.screenheight()-10, "Pan\nDown",   pan_down  )
	make_button(220, client.screenheight()-40, "Pan\nRight",  pan_right )
	make_button(300, client.screenheight()-10, "Reset\nView", reset_view)
end

event.onexit(function()
	gui.clearGraphics()
	gui.cleartext()
end)

while true do
	if Init then init_objects() end
	
	gui.clearGraphics()
	gui.cleartext()
	
	make_buttons()
	iterate()
	iterate_players()
	update_zoom()
	
	--[[--
	text(10, client.screenheight()-170, string.format(
		"Zoom: %.4f\nPanX: %s\nPanY: %s", 
		Zoom, Pan.x, Pan.y), 0xffbbddff)
	--]]--
	
	LastScreenSize.w = client.screenwidth()
	LastScreenSize.h = client.screenheight()
	
	emu.yield()
end