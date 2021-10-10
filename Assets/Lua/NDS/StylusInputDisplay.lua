-- Gives a cross hair UI for the stylus for DS games

local upColor = 'lime'
local downColor = 'red'
local dotColor = 'blue'

function Draw(x, y, maxX, maxY, isDown)
	color = upColor
	if isDown then
		color = downColor
	end

	gui.drawLine(0, y - 1, maxX, y - 1, color)
	gui.drawLine(0, y, maxX, y, color)
	gui.drawLine(0, y + 1, maxX, y + 1, color)
	
	gui.drawLine(x - 1, 0, x - 1, maxY, color)
	gui.drawLine(x, 0, x, maxY, color)
	gui.drawLine(x + 1, 0, x + 1, maxY, color)

	if isDown then
		gui.drawPixel(x - 1, y - 1, dotColor)
		gui.drawPixel(x, y - 1, dotColor)
		gui.drawPixel(x + 1, y - 1, dotColor)
		gui.drawPixel(x - 1, y, dotColor)
		gui.drawPixel(x, y, dotColor)
		gui.drawPixel(x + 1, y, dotColor)
		gui.drawPixel(x - 1, y + 1, dotColor)
		gui.drawPixel(x, y + 1, dotColor)
		gui.drawPixel(x + 1, y + 1, dotColor)
	end
end

while true do
	if emu.getsystemid() ~= "NDS" then
		console.log('This script is for Nintendo DS only')
		break
	end
	
	local btns = joypad.get()
	
	if movie.mode() == "PLAY" and emu.framecount() > 0 then
		btns = movie.getinput(emu.framecount() - 1)
	end

	local x = btns['Touch X']
	local y = btns['Touch Y']
	local isDown = btns['Touch']

	pts = client.transformPoint(x, y)
	local tx = pts["x"];
	local ty = pts["y"];
	local wx = client.screenwidth() / 256
	local wy = client.screenheight() / 384
	Draw(tx / wx, ty / wy, 10000, 10000, isDown)

	emu.yield()
end