-- Gives a cross hair UI for the stylus for DS games

local upColor = 'lime'
local downColor = 'red'
local dotColor = 'blue'

function Draw(x, y, maxX, maxY, isDown)
	color = upColor
	if isDown then
		color = downColor
	end

	gui.drawLine(0, y - 1, maxX, y - 1, color, "client")
	gui.drawLine(0, y, maxX, y, color, "client")
	gui.drawLine(0, y + 1, maxX, y + 1, color, "client")
	
	gui.drawLine(x - 1, 0, x - 1, maxY, color, "client")
	gui.drawLine(x, 0, x, maxY, color, "client")
	gui.drawLine(x + 1, 0, x + 1, maxY, color, "client")

	if isDown then
		gui.drawPixel(x - 1, y - 1, dotColor, "client")
		gui.drawPixel(x, y - 1, dotColor, "client")
		gui.drawPixel(x + 1, y - 1, dotColor, "client")
		gui.drawPixel(x - 1, y, dotColor, "client")
		gui.drawPixel(x, y, dotColor, "client")
		gui.drawPixel(x + 1, y, dotColor, "client")
		gui.drawPixel(x - 1, y + 1, dotColor, "client")
		gui.drawPixel(x, y + 1, dotColor, "client")
		gui.drawPixel(x + 1, y + 1, dotColor, "client")
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

	local pts = client.transformPoint(x, y)
	local tx = pts["x"];
	local ty = pts["y"];
	Draw(tx, ty, 10000, 10000, isDown)

	emu.yield()
end
