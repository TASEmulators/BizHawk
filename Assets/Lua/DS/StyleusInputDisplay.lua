-- Gives a cross hair UI for the stylus for DS games

local upColor = 'white'
local downColor = 'green'
local dotColor = 'red'

function Draw(x, y, maxX, maxY, isDown)
	color = upColor
	if isDown then
		color = downColor
	end

	gui.drawLine(0, y, maxX, y, color)
	gui.drawLine(x, 0, x, maxY, color)

	if isDown then
		gui.drawPixel(x, y, dotColor)
	end
end

while true do
	if emu.getsystemid() ~= "NDS" then
		console.log('This script is for Nintendo DS only')
		break
	end
	
	local touchStart = ds.touchScreenStart()
	if touchStart then
		local bufferX = client.bufferwidth()
		local bufferY = client.bufferheight()

		local btns = joypad.get()
		local x = btns['TouchX']
		local y = btns['TouchY']
		local isDown = btns['Touch']

		-- A bit of a hack to ensure it is not drawing while mouse moving
		-- on the top screen
		if y == 0 then
			x = 0
		end

		x = x + touchStart.X
		y = y + touchStart.Y

		Draw(x, y, bufferX, bufferY, isDown)
	end

	emu.frameadvance()
end