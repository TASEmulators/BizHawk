-- Gives a cross hair UI for the stylus for DS games

local upColor = 'lime'
local downColor = 'red'
local dotColor = 'blue'

client.setwindowsize(client.getwindowsize()) -- assert a sane resolution
console.writeline("Window size must be at an integer scale for this script to work, this will be asserted now and for screen layout changes")
console.writeline("WARNING! Using a higher than supported window size for your monitor may cause this script to fail! (e.g. 1980x1080 should not go past 2x window size)")
console.writeline("Keep the window size in the bounds of your resolution!")

prevScreenLayout = nds.getscreenlayout()

function ResolveSettingIssuesIfNeeded()
	local screenLayout = nds.getscreenlayout()
	if screenLayout == "Horizontal" and not nds.getscreeninvert() then
		console.writeline("Non-inverted horizontal screens are unsupported, switching to inverted screens")
		nds.setscreeninvert(true)
	end
	if nds.getscreengap() ~= 0 then
		console.writeline("Non-zero screen gap is unsupported, setting screen gap to 0 and asserting window size")
		nds.setscreengap(0)
		client.setwindowsize(client.getwindowsize())
	end
	if prevScreenLayout ~= screenLayout then
		console.writeline("screen layout changed, asserting window size")
		client.setwindowsize(client.getwindowsize())
		prevScreenLayout = screenLayout
	end
end

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

	ResolveSettingIssuesIfNeeded()
	
	local btns = joypad.get()
	
	if movie.mode() == "PLAY" and emu.framecount() > 0 then
		btns = movie.getinput(emu.framecount() - 1)
	end

	local xo = 0
	local yo = 0

	if nds.getscreenlayout() == "Horizontal" then
		xo = -256
	elseif nds.getscreeninvert() then
		yo = -192
	end

	local x = btns['Touch X'] + xo
	local y = btns['Touch Y'] + yo
	local isDown = btns['Touch']

	local pts = client.transformPoint(x, y)
	local tx = pts["x"];
	local ty = pts["y"];
	local wsz = client.getwindowsize()
	Draw(tx / wsz, ty / wsz, 10000, 10000, isDown)

	emu.yield()
end