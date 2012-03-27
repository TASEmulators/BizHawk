--Written by Brandon Evans

--You can change the position of the text here.
local x = 0
local y = 36

local holds
local pressed
local presses
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
	console.output('Data loaded from slot ' .. tostring(slot - 1))
	counts()
end

function record(buttons)
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
end

function frames()
	--If there is an open, read-only TAS file, parse it for the initial data.
	if not movie.isloaded() then
		return false
	end
	local frame = -1
	reset()
	--Parse up until two frames before the current one.
	while frame ~= emu.framecount() - 2 do
		record(movie.getinput(frame))
		frame = frame + 1
	end
	return true
end

function reset()
	holds = 0
	pressed = {}
	presses = 0
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
	console.output('Data saved to slot ' .. tostring(slot - 1))
	counts()
end

reset()
if frames() then
	console.output('Data loaded from frames')
else
	console.output('No data loaded from frames')
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
	record(joypad.get())
	counts()
	emu.frameadvance()
end