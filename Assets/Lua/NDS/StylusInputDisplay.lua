-- Gives a cross hair UI for the stylus for DS games

local upColor = 'lime'
local downColor = 'red'
local dotColor = 'blue'

client.setwindowsize(client.getwindowsize()) -- assert a sane resolution

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

	local invert = nds.getscreeninvert()
	local wsz = client.getwindowsize()
	local xo = 0
	local yo = 0

	if nds.getscreenlayout() == "Horizontal" then
		if invert then
			xo = xo - 256
		else
			console.writeline("Non-inverted horizontal screens are unsupported, switching to inverted screens")
			nds.setscreeninvert(true)
			xo = xo - 256
		end
	else -- vertical
		if invert then
			yo = yo - 192
		else
			-- don't need to do anything here
		end
	end

	local x = btns['Touch X'] + xo
	local y = btns['Touch Y'] + yo
	local isDown = btns['Touch']

	pts = client.transformPoint(x, y)
	local tx = pts["x"];
	local ty = pts["y"];
	Draw(tx / wsz, ty / wsz, 10000, 10000, isDown)

	emu.yield()
end