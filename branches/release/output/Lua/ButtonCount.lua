--Written by Brandon Evans

--You can change the position of the text here.
local x = 0
local y = 36

--You can blacklist buttons from being recorded here.
local blacklist = {'Lag', 'Pause', 'Reset'}

local data = {}
local registered = false
local state = {}
local states = {}

function deepcopy(object)
	local lookup_table = {}
	local function _copy(object)
		if type(object) ~= 'table' then
			return object
		elseif lookup_table[object] then
			return lookup_table[object]
		end
		local new_table = {}
		lookup_table[object] = new_table
		for index, value in pairs(object) do
			new_table[_copy(index)] = _copy(value)
		end
		return setmetatable(new_table, getmetatable(object))
	end
	return _copy(object)
end

function counts(obj)
	gui.text(x, y, 'Pressed: ' .. obj.pressed)
	gui.text(x, y + 14, 'Inputted: ' .. obj.inputted)
end

function frames()
	if not movie.isloaded() then
		console.log('No data loaded from frames')
		return
	end
	reset()
	--Get data from every frame but this one. This frame's data will be decided
	--in real-time.
	for frame = 0, emu.framecount() - 1 do
		record(movie.getinput(frame))
	end
	console.log('Data loaded from frames')
	counts(data)
end

function load(name)
	registered = true
	state = {}
	if not states[name] then
		frames()
		save(name)
		return
	end
	data = deepcopy(states[name])
	console.log('Data loaded from ' .. name)
	--Show the data from before the state's frame.
	local previous = states[name].previous
	counts(previous)
end

function record(buttons)
	for button, value in pairs(buttons) do
		local blacklisted = false
		for index, name in pairs(blacklist) do
			if name == button then
				blacklisted = true
			end
		end
		if value and not blacklisted then
			data.inputted = data.inputted + 1
			if not data.buttons[button] then
				data.pressed = data.pressed + 1
			end
		end
		data.buttons[button] = value
	end
end

function reset()
	data.buttons = {}
	data.pressed = 0
	data.inputted = 0
end

function save(name)
	registered = true
	if next(state) == nil then
		data.previous = deepcopy(data)
		--Include the state's frame in the data.
		record(joypad.get())
		state = deepcopy(data)
	end
	states[name] = deepcopy(state)
	console.log('Data saved to ' .. name)
end

reset()
frames()

if event.onloadstate then
	event.onloadstate(load)
	event.onsavestate(save)
end

while true do
	--If this is the first frame, reset the data.
	if emu.framecount() == 0 then
		reset()
	end
	if not registered then
		record(joypad.get())
	end
	registered = false
	state = {}
	counts(data)
	emu.frameadvance()
end