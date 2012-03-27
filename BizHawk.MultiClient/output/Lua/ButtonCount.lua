--Written by Brandon Evans

--You can change the position of the text here.
local x = 0
local y = 36

local holds = 0
local pressed = {}
local presses = 0
local states = {}

function table.copy(t)
	local t2 = {}
	for k, v in pairs(t) do
		t2[k] = v
	end
	return t2
end

function counts()
	--Display the counts of the holds and the presses.
	gui.text(x, y, 'Holds: ' .. holds)
	gui.text(x, y + 14, 'Presses: ' .. presses)
end

function load(slot)
	--As Lua starts counting from 1, and there may be a slot 0, increment.
	slot = slot + 1
	if not states[slot] or not states[slot].holds then
		gui.text(x, y + 28, 'No data loaded from slot ' .. tostring(slot - 1))
		counts()
		return
	end
	--Load the data if there is any available for this slot.
	holds = states[slot].holds
	pressed = table.copy(states[slot].pressed)
	presses = states[slot].presses
	gui.text(x, y + 28, 'Data loaded from slot ' .. tostring(slot - 1))
	counts()
end

function parse()
	--If there is an open, read-only TAS file, parse it for the initial data.
	if not movie.isloaded() then
		return false
	end
	local fh = io.open(movie.filename())
	if not fh or movie.mode() == 'record' or movie.filename():match(
	'.%.(%w+)$'
	) ~= 'tas' then
		return false
	end
	local frame = -1
	local last = {}
	local match = {
		'Up', 'Down', 'Left', 'Right', 'Start', 'Select', 'B', 'A'
	}
	--Parse up until two frames before the current one.
	while frame ~= emu.framecount() - 2 do
		line = fh:read()
		if not line then
			break
		end
		--This is only a frame if it starts with a vertical bar.
		if string.sub(line, 0, 1) == '|' then
			frame = frame + 1
			local player = -1
			--Split up the sections by a vertical bar.
			for section in string.gmatch(line, '[^|]+') do
				player = player + 1
				--Only deal with actual players.
				if player ~= 0 then
					local button = 0
					--Run through all the buttons.
					for text in string.gmatch(section, '.') do
						button = button + 1
						local name = 'P' .. player .. ' ' .. match[button]
						local pressed = false
						--Check if this button is pressed.
						if text ~= ' ' and text ~= '.' then
							holds = holds + 1
							--If the button was not previously pressed,
							--increment.
							if not last[name] then
								presses = presses + 1
							end
							pressed = true
						end
						----Mark this button as pressed or not pressed.
						last[name] = pressed
					end
				end
			end
		end
	end
	return true
end

function save(slot)
	--As Lua starts counting from 1, and there may be a slot 0, increment.
	slot = slot + 1
	while table.maxn(states) < slot do
		table.insert(states, {})
	end
	--Mark the current data as the data for this slot.
	states[slot].holds = holds
	states[slot].buttons = table.copy(buttons)
	states[slot].presses = presses
	gui.text(x, y + 28, 'Data saved to slot ' .. tostring(slot - 1))
	counts()
end

if parse() then
	gui.text(x, y + 28, 'Movie parsed for data')
else
	gui.text(x, y + 28, 'No movie parsed for data')
end
if savestate.registerload then
	savestate.registerload(load)
	savestate.registersave(save)
end

while true do
	--If this is the first frame, reset the data.
	if emu.framecount() == 0 then
		holds = 0
		pressed = {}
		presses = 0
	end
	local buttons = joypad.get()
	--Run through all of the pressed buttons.
	for button, value in pairs(buttons) do
		if value then
			holds = holds + 1
			--If in the previous frame the button was not pressed, increment.
			if not pressed[button] then
				presses = presses + 1
			end
		end
		--Mark this button as pressed or not pressed.
		pressed[button] = value
	end
	counts()
	emu.frameadvance()
end