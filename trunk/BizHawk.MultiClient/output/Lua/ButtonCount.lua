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
	gui.text(x, y, 'Holds: ' .. holds)
	gui.text(x, y + 14, 'Presses: ' .. presses)
end

function frames()
	if not movie.isloaded() then
		console.output('No data loaded from frames')
		return
	end
	reset()
	--Get data from every frame but this one. This frame's data will be decided
	--in real-time.
	for frame = 0, emu.framecount() - 1 do
		record(movie.getinput(frame))
	end
	console.output('Data loaded from frames')
	counts()
end

function load(name)
	if not states[name] then
		frames()
		save(name)
		return
	end
	holds = states[name].holds
	pressed = table.copy(states[name].pressed)
	presses = states[name].presses
	console.output('Data loaded from ' .. name)
	counts()
end

function record(buttons)
	for button, value in pairs(buttons) do
		if value then
			holds = holds + 1
			if not pressed[button] then
				presses = presses + 1
			end
		end
		pressed[button] = value
	end
end

function reset()
	holds = 0
	pressed = {}
	presses = 0
end

function save(name)
	states[name] = {}
	states[name].holds = holds
	states[name].pressed = table.copy(pressed)
	states[name].presses = presses
	console.output('Data saved to ' .. name)
	counts()
end

reset()
frames()

if savestate.registerload then
	savestate.registerload(load)
	savestate.registersave(save)
end

while true do
	--If this is the first frame, reset the data.
	if emu.framecount() == 0 then
		reset()
	end
	record(joypad.get())
	counts()
	emu.frameadvance()
end