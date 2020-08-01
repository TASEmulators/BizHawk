--[[
Input visualizing based in FCEUX!
Supports many consoles, only standard controllers are supported.
Position, colors, stack and controller enabling are configurable!
Planned for future: Controllers with analog stick(s) (e.g N64)

Features:
- Only standard controllers (so no N64 and PS1 analogs :c)
- A bit configurable (default colors are also based in FCEUX).
- Distinguish between GB and DualGB (same for GG and NGP).
- And also shows link cable state.
- Detect which controllers are plugged.
- WonderSwan rotation also rotates display.
- Left-Right and Top-Down modes! (or Stack Horizontal / Vertical)
- Lets you choose which controllers to display.
- Also, if you enable an unplugged controller, it will NOT display.
- Can also display movie loaded input (BizHawk 2.3.3+).

Thanks to adelikat for giving support for reading player specific inputs and -
also gave very good feedback.
Thanks to EZGames69 for so many corrections, ideas and testing.

Made by Cyorter (2020)
]]

emu.frameadvance()
console.clear()

--------------------------------------------------------------------------------

-- Configurations --

-- Position (0 and 0 is top-left)
-- Pro tip: You can also drag it while playing (drag slow!).

xpos = 8
ypos = 8

-- Colors (AARRGGBB/RGB32)

cyp = 0xC0FDDBCF -- Button Yup Pressed (FCEUX = 0xC0FDDBCF)
cnp = 0xC0000000 -- Button Not Pressed (FCEUX = 0xC0000000)
cbg = 0xC0000E60 -- Background (FCEUX = 0xC0000E60)

-- Stack Horizontally / Vertically
-- Top-Down mode (if disabled it is Left-Right)

topdown = false

--                             Display  controls
--         01   02   03   04   05   06   07   08   09   10   11   12
enable = {true,true,true,true,true,true,true,true,true,true,true,true}

--------------------------------------------------------------------------------

-- Console Interface --
-- DO NOT CHANGE THIS, IS USED FOR DETECTION!

do
	pluged = {joypad.get(1)['Up'] ~= nil,
			joypad.get( 2)['Up'] ~= nil,
			joypad.get( 3)['Up'] ~= nil,
			joypad.get( 4)['Up'] ~= nil,
			joypad.get( 5)['Up'] ~= nil,
			joypad.get( 6)['Up'] ~= nil,
			joypad.get( 7)['Up'] ~= nil,
			joypad.get( 8)['Up'] ~= nil,
			joypad.get( 9)['Up'] ~= nil,
			joypad.get(10)['Up'] ~= nil,
			joypad.get(11)['Up'] ~= nil,
			joypad.get(12)['Up'] ~= nil}

	sys = emu.getsystemid()
	if sys == 'Game Gear' then sys = 'GG' end -- DualGG is weird

	-- Here I abuse a technique that is equivalent to a ternary operator.
	-- (sys == 'PCE' and 5) or (sys == 'NES' and 4) or 2
	-- In Lua is equivalent to C ternary operator:
	-- sys == "PCE" ? 5 : sys == "NES" ? 4 : 2;
	-- Is used to know how many players does a console have.

	players = ((sys == 'NES'  or sys == 'N64') and 4) or ((sys == 'PCE' or sys == 'PCECD' or sys == 'SGX' or sys == 'SNES') and 5) or ((sys == 'PSX' or sys == 'GEN') and 8) or (sys == 'SAT' and 12) or 2

	-- Detect hand-held, this time is not as abused as above.
	portable = sys == 'GB' or sys == 'GBC' or sys == 'Lynx' or sys == 'GBA' or sys == 'NGP' or sys == 'GG' or sys == 'VB' or sys == 'WSWAN'

	-- Add "Dual" in system's name if there are 2 players, no matter if linked or not.
	if portable and pluged[2] then print('Dual ' .. sys)
	else print(sys) end

	if (not portable) then
		-- Prints which controllers are connected.
		for h = 1,players,1 do print(string.format('Player %u: ',h) .. (pluged[h] and 'Connected!' or 'Unconnected!')) end
	end

	if sys == 'NES' and pluged[3] and not pluged[4] then print('\nWARNING! Invalid FourScore use.') end
	if sys == 'SAT' then print('\nWARNING! joypad.get() has problems with Players 10,11,12!') end
	-- Reason: joypad.get() is buggy when using more than 10 controllers
end

print('\nReminder: Enable Capure OSD when Dumping!\n')

--------------------------------------------------------------------------------

size = {{['A26']=10, ['C64']=24, ['NES']=40, ['PCE']=40, ['PCECD']=40, ['SGX']=40, ['GB']=16, ['GBC']=16, ['VB']=38, ['WSWAN']=28, ['SNES']=46, ['GEN']=46, ['GBA']=34, ['Coleco']=16, ['SAT']=46, ['A78']=20, ['SG']=20, ['SMS']=36, ['Lynx']=32, ['GG']=30, ['NGP']=29, ['PCFX']=50, ['PSX']=56},{['A26']=16, ['C64']=16, ['NES']=16, ['PCE']=16, ['PCECD']=16, ['SGX']=16, ['GB']=24, ['GBC']=24, ['VB']=28, ['WSWAN']=28, ['SNES']=20, ['GEN']=20, ['GBA']=16, ['Coleco']=38, ['SAT']=20, ['A78']=20, ['SG']=20, ['SMS']=16, ['Lynx']=15, ['GG']=22, ['NGP']=14, ['PCFX']=22, ['PSX']=34}}

event.onframestart(function()
	x = input.getmouse()['X']
	y = input.getmouse()['Y']
	b = input.getmouse()['Left'] or input.getmouse()['Right']
	if (x > xpos and x < (xpos + size[1][sys]) and y > ypos and y < (ypos + size[2][sys]) and b) then
		xpos = x - size[1][sys] / 2
		ypos = y - size[2][sys] / 2
	end
end
)

--------------------------------------------------------------------------------

while sys == 'A26' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 20
			else
				x = xpos + (h - 1) * 14
				y = ypos
			end

			gui.drawRectangle(x+0,y+ 0,11,15,cbg,cbg)
			gui.drawRectangle(x+7,y+ 2,1, 3,c['Up']     and cyp or cnp,c['Up']     and cyp or cnp)
			gui.drawRectangle(x+7,y+10,1, 3,c['Down']   and cyp or cnp,c['Down']   and cyp or cnp)
			gui.drawRectangle(x+5,y+ 6,1, 3,c['Left']   and cyp or cnp,c['Left']   and cyp or cnp)
			gui.drawRectangle(x+9,y+ 6,1, 3,c['Right']  and cyp or cnp,c['Right']  and cyp or cnp)
			gui.drawRectangle(x+1,y+ 2,1, 3,c['Button'] and cyp or cnp,c['Button'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'C64' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 20
			else
				x = xpos + (h - 1) * 28
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+0,23,15,cbg,cbg)

			gui.drawRectangle(x+14,y+ 2,3,3,c['Up']     and cyp or cnp,c['Up']     and cyp or cnp)
			gui.drawRectangle(x+14,y+10,3,3,c['Down']   and cyp or cnp,c['Down']   and cyp or cnp)
			gui.drawRectangle(x+10,y+ 6,3,3,c['Left']   and cyp or cnp,c['Left']   and cyp or cnp)
			gui.drawRectangle(x+18,y+ 6,3,3,c['Right']  and cyp or cnp,c['Right']  and cyp or cnp)

			gui.drawEllipse(x+2,y+2,3, 3,c['Button'] and cyp or cnp,c['Button'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'NES' do
	for h = 1,4,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 20
			else
				x = xpos + (h - 1) * 44
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+0,39,15,cbg,cbg)

			gui.drawRectangle(x+ 6,y+ 2, 3, 3,c['Up']     and cyp or cnp,c['Up']     and cyp or cnp)
			gui.drawRectangle(x+ 6,y+10, 3, 3,c['Down']   and cyp or cnp,c['Down']   and cyp or cnp)
			gui.drawRectangle(x+ 2,y+ 6, 3, 3,c['Left']   and cyp or cnp,c['Left']   and cyp or cnp)
			gui.drawRectangle(x+10,y+ 6, 3, 3,c['Right']  and cyp or cnp,c['Right']  and cyp or cnp)
			gui.drawRectangle(x+16,y+10, 3, 1,c['Select'] and cyp or cnp,c['Select'] and cyp or cnp)
			gui.drawRectangle(x+22,y+10, 3, 1,c['Start']  and cyp or cnp,c['Start']  and cyp or cnp)

			gui.drawEllipse(x+28,y+8, 3, 3,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
			gui.drawEllipse(x+34,y+8, 3, 3,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'PCE' or sys == 'PCECD' or sys == 'SGX' do
	for h = 1,5,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 20
			else
				x = xpos + (h - 1) * 44
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+0,39,15,cbg,cbg)

			gui.drawRectangle(x+ 6,y+ 2, 3, 3,c['Up']     and cyp or cnp,c['Up']     and cyp or cnp)
			gui.drawRectangle(x+ 6,y+10, 3, 3,c['Down']   and cyp or cnp,c['Down']   and cyp or cnp)
			gui.drawRectangle(x+ 2,y+ 6, 3, 3,c['Left']   and cyp or cnp,c['Left']   and cyp or cnp)
			gui.drawRectangle(x+10,y+ 6, 3, 3,c['Right']  and cyp or cnp,c['Right']  and cyp or cnp)
			gui.drawRectangle(x+16,y+10, 3, 1,c['Select'] and cyp or cnp,c['Select'] and cyp or cnp)
			gui.drawRectangle(x+22,y+10, 3, 1,c['Run']    and cyp or cnp,c['Run']    and cyp or cnp)

			gui.drawEllipse(x+28,y+8, 3, 3,c['B2'] and cyp or cnp,c['B2'] and cyp or cnp)
			gui.drawEllipse(x+34,y+8, 3, 3,c['B1'] and cyp or cnp,c['B1'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

gambatte = joypad.get()['Up'] ~= nil
link = joypad.get()['P2 Up'] ~= nil
prev = false
while sys == 'GB' or sys == 'GBC' do
	if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1)
	else c = joypad.get() end

	if not prev and c['Toggle Cable'] then
		link = not link
	end
	prev = c['Toggle Cable']

	x = xpos
	y = ypos

	if gambatte then
		gui.drawRectangle(x+ 0,y+1,15,21,cbg,cbg)
		gui.drawLine(x+1,y+ 0,x+14,y+ 0,cbg)
		gui.drawLine(x+1,y+23,x+14,y+23,cbg)
		gui.drawLine(x+3,y+24,x+12,y+24,cbg)

		gui.drawRectangle(x+2,y+2,11,8,cnp,cnp)

		gui.drawRectangle(x+3,y+13, 1, 1,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
		gui.drawRectangle(x+3,y+17, 1, 1,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
		gui.drawRectangle(x+1,y+15, 1, 1,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
		gui.drawRectangle(x+5,y+15, 1, 1,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)

		gui.drawRectangle(x+10,y+16, 1, 1,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
		gui.drawRectangle(x+13,y+15, 1, 1,c['A'] and cyp or cnp,c['A'] and cyp or cnp)

		gui.drawLine(x+ 6,y+21,x+ 7,y+21,c['Select'] and cyp or cnp)
		gui.drawLine(x+ 9,y+21,x+10,y+21,c['Start']  and cyp or cnp)
	else
	if enable[1] and pluged[1] then
		gui.drawRectangle(x+ 0,y+4,15,21,cbg,cbg)
		gui.drawLine(x+1,y+ 3,x+14,y+ 3,cbg)
		gui.drawLine(x+1,y+26,x+14,y+26,cbg)
		gui.drawLine(x+3,y+27,x+12,y+27,cbg)

		gui.drawRectangle(x+2,y+5,11,8,cnp,cnp)

		gui.drawRectangle(x+3,y+16, 1, 1,c['P1 Up']    and cyp or cnp,c['P1 Up']    and cyp or cnp)
		gui.drawRectangle(x+3,y+20, 1, 1,c['P1 Down']  and cyp or cnp,c['P1 Down']  and cyp or cnp)
		gui.drawRectangle(x+1,y+18, 1, 1,c['P1 Left']  and cyp or cnp,c['P1 Left']  and cyp or cnp)
		gui.drawRectangle(x+5,y+18, 1, 1,c['P1 Right'] and cyp or cnp,c['P1 Right'] and cyp or cnp)

		gui.drawRectangle(x+10,y+19, 1, 1,c['P1 B'] and cyp or cnp,c['P1 B'] and cyp or cnp)
		gui.drawRectangle(x+13,y+18, 1, 1,c['P1 A'] and cyp or cnp,c['P1 A'] and cyp or cnp)

		gui.drawLine(x+ 6,y+24,x+ 7,y+24,c['P1 Select'] and cyp or cnp)
		gui.drawLine(x+ 9,y+24,x+10,y+24,c['P1 Start']  and cyp or cnp)
	end
	
	if enable[2] and pluged[2] then 
		gui.drawRectangle(x+20,y+4,15,21,cbg,cbg)
		gui.drawLine(x+21,y+ 3,x+34,y+ 3,cbg)
		gui.drawLine(x+21,y+26,x+34,y+26,cbg)
		gui.drawLine(x+23,y+27,x+32,y+27,cbg)

		gui.drawRectangle(x+22,y+5,11,8,cnp,cnp)

		gui.drawRectangle(x+23,y+16, 1, 1,c['P2 Up']    and cyp or cnp,c['P2 Up']    and cyp or cnp)
		gui.drawRectangle(x+23,y+20, 1, 1,c['P2 Down']  and cyp or cnp,c['P2 Down']  and cyp or cnp)
		gui.drawRectangle(x+21,y+18, 1, 1,c['P2 Left']  and cyp or cnp,c['P2 Left']  and cyp or cnp)
		gui.drawRectangle(x+25,y+18, 1, 1,c['P2 Right'] and cyp or cnp,c['P2 Right'] and cyp or cnp)

		gui.drawRectangle(x+30,y+19, 1, 1,c['P2 B'] and cyp or cnp,c['P2 B'] and cyp or cnp)
		gui.drawRectangle(x+33,y+18, 1, 1,c['P2 A'] and cyp or cnp,c['P2 A'] and cyp or cnp)

		gui.drawLine(x+26,y+24,x+27,y+24,c['P2 Select'] and cyp or cnp)
		gui.drawLine(x+29,y+24,x+30,y+24,c['P2 Start']  and cyp or cnp)
	end
	end

	if link then
		gui.drawRectangle(x+16,y+8,1,1,cbg,cbg)
		gui.drawRectangle(x+36,y+8,1,1,cbg,cbg)
		gui.drawLine(x+17,y+1,x+17,y+7,cbg)
		gui.drawLine(x+38,y+1,x+38,y+8,cbg)
		gui.drawLine(x+18,y+2,x+18,y+8,cbg)
		gui.drawLine(x+37,y+2,x+37,y+7,cbg)
		gui.drawRectangle(x+18,y+0,19,1,cbg,cbg)
	end

	emu.frameadvance()
end

while sys == 'VB' do
	if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1)
	else c = joypad.get() end

	x = xpos
	y = ypos

	gui.drawLine(x+ 4,y+ 0,x+ 9,y+ 0,cbg)
	gui.drawLine(x+30,y+ 0,x+35,y+ 0,cbg)
	gui.drawLine(x+ 3,y+ 1,x+11,y+ 1,cbg)
	gui.drawLine(x+28,y+ 1,x+36,y+ 1,cbg)
	gui.drawLine(x+ 2,y+ 2,x+13,y+ 2,cbg)
	gui.drawLine(x+26,y+ 2,x+37,y+ 2,cbg)
	gui.drawLine(x+ 1,y+ 3,x+15,y+ 3,cbg)
	gui.drawLine(x+24,y+ 3,x+38,y+ 3,cbg)
	gui.drawLine(x+ 0,y+ 4,x+17,y+ 4,cbg)
	gui.drawLine(x+22,y+ 4,x+39,y+ 4,cbg)
	gui.drawRectangle(x+ 0,y+ 5,39,3,cbg,cbg)
	gui.drawRectangle(x+ 1,y+ 9,37,1,cbg,cbg)
	gui.drawLine(x+ 2,y+11,x+11,y+11,cbg)
	gui.drawRectangle(x+ 3,y+12, 7,1,cbg,cbg)
	gui.drawRectangle(x+ 2,y+14, 7,2,cbg,cbg)
	gui.drawRectangle(x+ 1,y+17, 7,2,cbg,cbg)
	gui.drawLine(x+ 0,y+20,x+ 8,y+20,cbg)
	gui.drawRectangle(x+ 0,y+21, 7,3,cbg,cbg)
	gui.drawLine(x+ 0,y+25,x+ 6,y+25,cbg)
	gui.drawLine(x+ 1,y+26,x+ 6,y+26,cbg)
	gui.drawLine(x+ 2,y+27,x+ 5,y+27,cbg)
	gui.drawLine(x+28,y+11,x+37,y+11,cbg)
	gui.drawRectangle(x+29,y+12, 7,1,cbg,cbg)
	gui.drawRectangle(x+30,y+14, 7,2,cbg,cbg)
	gui.drawRectangle(x+31,y+17, 7,2,cbg,cbg)
	gui.drawLine(x+31,y+20,x+39,y+20,cbg)
	gui.drawRectangle(x+32,y+21, 7,3,cbg,cbg)
	gui.drawLine(x+33,y+25,x+39,y+25,cbg)
	gui.drawLine(x+33,y+26,x+38,y+26,cbg)
	gui.drawLine(x+34,y+27,x+37,y+27,cbg)
	gui.drawLine(x+13,y+11,x+26,y+11,cbg)
	gui.drawRectangle(x+14,y+12,11,7,cbg,cbg)

	gui.drawRectangle(x+ 6,y+3,1,1,c['L_Up']    and cyp or cnp,c['L_Up']    and cyp or cnp)
	gui.drawRectangle(x+ 6,y+7,1,1,c['L_Down']  and cyp or cnp,c['L_Down']  and cyp or cnp)
	gui.drawRectangle(x+ 4,y+5,1,1,c['L_Left']  and cyp or cnp,c['L_Left']  and cyp or cnp)
	gui.drawRectangle(x+ 8,y+5,1,1,c['L_Right'] and cyp or cnp,c['L_Right'] and cyp or cnp)
	gui.drawRectangle(x+32,y+3,1,1,c['R_Up']    and cyp or cnp,c['R_Up']    and cyp or cnp)
	gui.drawRectangle(x+32,y+7,1,1,c['R_Down']  and cyp or cnp,c['R_Down']  and cyp or cnp)
	gui.drawRectangle(x+30,y+5,1,1,c['R_Left']  and cyp or cnp,c['R_Left']  and cyp or cnp)
	gui.drawRectangle(x+34,y+5,1,1,c['R_Right'] and cyp or cnp,c['R_Right'] and cyp or cnp)
	gui.drawRectangle(x+ 6,y+5,1,1,c['L']       and cyp or cnp,c['L']       and cyp or cnp)
	gui.drawRectangle(x+32,y+5,1,1,c['R']       and cyp or cnp,c['R']       and cyp or cnp)

	gui.drawRectangle(x+12,y+6,1,1,c['Select'] and cyp or cnp,c['Select'] and cyp or cnp)
	gui.drawRectangle(x+15,y+8,1,1,c['Start']  and cyp or cnp,c['Start']  and cyp or cnp)
	gui.drawRectangle(x+23,y+8,1,1,c['B']      and cyp or cnp,c['B']      and cyp or cnp)
	gui.drawRectangle(x+26,y+6,1,1,c['A']      and cyp or cnp,c['A']      and cyp or cnp)

	emu.frameadvance()
end

wswanr = client.screenwidth() < client.screenheight()
prev = false
while sys == 'WSWAN' do
	if movie.mode() == 'PLAY' then
		c = movie.getinput(emu.framecount() - 1, wswanr and 2 or 1)
		c['Rotate'] = movie.getinput(emu.framecount() - 1)['Rotate']
	else c = joypad.get(wswanr and 2 or 1); c['Rotate'] = joypad.get()['Rotate'] end

	if not prev and c['Rotate'] then
		wswanr = not wswanr -- Rotate!
	end
	prev = c['Rotate']

	x = xpos
	y = ypos

	if wswanr then
		gui.drawRectangle(x+ 0,y+ 2,17,27,cbg,cbg)
		gui.drawLine(x+2,y+0,x+16,y+0,cbg)
		gui.drawLine(x+1,y+1,x+17,y+1,cbg)
		gui.drawLine(x+1,y+30,x+16,y+30,cbg)
		gui.drawLine(x+2,y+31,x+14,y+31,cbg)
		gui.drawLine(x+18,y+2,x+18,y+27,cbg)

		gui.drawRectangle(x+3,y+7,11,16,cnp,cnp)

		gui.drawRectangle(x+ 4,y+25, 1, 1,c['Y2']     and cyp or cnp,c['Y2']     and cyp or cnp)
		gui.drawRectangle(x+ 4,y+29, 1, 1,c['Y4']     and cyp or cnp,c['Y4']     and cyp or cnp)
		gui.drawRectangle(x+ 2,y+27, 1, 1,c['Y1']     and cyp or cnp,c['Y1']     and cyp or cnp)
		gui.drawRectangle(x+ 6,y+27, 1, 1,c['Y3']     and cyp or cnp,c['Y3']     and cyp or cnp)
		gui.drawRectangle(x+12,y+25, 1, 1,c['X2']     and cyp or cnp,c['X2']     and cyp or cnp)
		gui.drawRectangle(x+12,y+29, 1, 1,c['X4']     and cyp or cnp,c['X4']     and cyp or cnp)
		gui.drawRectangle(x+10,y+27, 1, 1,c['X1']     and cyp or cnp,c['X1']     and cyp or cnp)
		gui.drawRectangle(x+14,y+27, 1, 1,c['X3']     and cyp or cnp,c['X3']     and cyp or cnp)
		gui.drawLine(x+17,y+17,x+17,y+18,c['Start']  and cyp or cnp)
		gui.drawLine(x+17,y+20,x+17,y+21,c['Rotate'] and cyp or cnp)

		gui.drawRectangle(x+15,y+ 4, 1, 1,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
		gui.drawRectangle(x+13,y+ 2, 1, 1,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
	else
		gui.drawRectangle(x+ 2,y+ 0,27,17,cbg,cbg)
		gui.drawLine(x+0,y+2,x+0,y+14,cbg)
		gui.drawLine(x+1,y+1,x+1,y+16,cbg)
		gui.drawLine(x+30,y+1,x+30,y+17,cbg)
		gui.drawLine(x+31,y+2,x+31,y+16,cbg)
		gui.drawLine(x+4,y+18,x+29,y+18,cbg)

		gui.drawRectangle(x+8,y+3,16,11,cnp,cnp)

		gui.drawRectangle(x+ 3,y+ 2, 1, 1,c['Y1']     and cyp or cnp,c['Y1']     and cyp or cnp)
		gui.drawRectangle(x+ 3,y+ 6, 1, 1,c['Y3']     and cyp or cnp,c['Y3']     and cyp or cnp)
		gui.drawRectangle(x+ 1,y+ 4, 1, 1,c['Y4']     and cyp or cnp,c['Y4']     and cyp or cnp)
		gui.drawRectangle(x+ 5,y+ 4, 1, 1,c['Y2']     and cyp or cnp,c['Y2']     and cyp or cnp)
		gui.drawRectangle(x+ 3,y+10, 1, 1,c['X1']     and cyp or cnp,c['X1']     and cyp or cnp)
		gui.drawRectangle(x+ 3,y+14, 1, 1,c['X3']     and cyp or cnp,c['X3']     and cyp or cnp)
		gui.drawRectangle(x+ 1,y+12, 1, 1,c['X4']     and cyp or cnp,c['X4']     and cyp or cnp)
		gui.drawRectangle(x+ 5,y+12, 1, 1,c['X2']     and cyp or cnp,c['X2']     and cyp or cnp)
		gui.drawLine(x+13,y+17,x+14,y+17,c['Start']  and cyp or cnp)
		gui.drawLine(x+10,y+17,x+11,y+17,c['Rotate'] and cyp or cnp)

		gui.drawRectangle(x+26,y+15, 1, 1,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
		gui.drawRectangle(x+28,y+13, 1, 1,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
	end

	emu.frameadvance()
end

while sys == 'SNES' do
	for h = 1,5,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 24
			else
				x = xpos + (h - 1) * 50
				y = ypos
			end

			gui.drawRectangle(x+ 4,y+ 0,37,15,cbg,cbg)
			gui.drawRectangle(x+ 0,y+ 4, 3,11,cbg,cbg)
			gui.drawRectangle(x+42,y+ 4, 3,11,cbg,cbg)
			gui.drawRectangle(x+ 4,y+16,11, 3,cbg,cbg)
			gui.drawRectangle(x+30,y+16,11, 3,cbg,cbg)
			gui.drawRectangle(x+ 2,y+ 2, 1, 1,cbg,cbg)
			gui.drawRectangle(x+ 2,y+16, 1, 1,cbg,cbg)
			gui.drawRectangle(x+16,y+16, 1, 1,cbg,cbg)
			gui.drawRectangle(x+28,y+16, 1, 1,cbg,cbg)
			gui.drawRectangle(x+42,y+16, 1, 1,cbg,cbg)
			gui.drawRectangle(x+42,y+ 2, 1, 1,cbg,cbg)

			gui.drawRectangle(x+ 8,y+ 4, 3, 3,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 8,y+12, 3, 3,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 4,y+ 8, 3, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+12,y+ 8, 3, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)
			gui.drawRectangle(x+ 8,y+ 0, 3, 1,c['L']     and cyp or cnp,c['L']     and cyp or cnp)
			gui.drawRectangle(x+34,y+ 0, 3, 1,c['R']     and cyp or cnp,c['R']     and cyp or cnp)
	
			gui.drawEllipse(x+30,y+ 8, 3, 3,c['Y'] and cyp or cnp,c['Y'] and cyp or cnp)
			gui.drawEllipse(x+34,y+12, 3, 3,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
			gui.drawEllipse(x+38,y+ 8, 3, 3,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
			gui.drawEllipse(x+34,y+ 4, 3, 3,c['X'] and cyp or cnp,c['X'] and cyp or cnp)

			gui.drawLine(x+18,y+13,x+21,y+10,c['Select'] and cyp or cnp)
			gui.drawLine(x+19,y+13,x+22,y+10,c['Select'] and cyp or cnp)
			gui.drawLine(x+23,y+13,x+26,y+10,c['Start']  and cyp or cnp)
			gui.drawLine(x+24,y+13,x+27,y+10,c['Start']  and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'GEN' do
	for h = 1,8,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 24
			else
				x = xpos + (h - 1) % 4 * 50
				y = ypos + math.floor((h - 1) / 4) * 24
			end

			gui.drawLine(x+ 8,y+ 0,x+37,y+ 0,cbg)
			gui.drawLine(x+ 4,y+ 1,x+41,y+ 1,cbg)
			gui.drawRectangle(x+ 4,y+ 2,37,11,cbg,cbg)

			gui.drawRectangle(x+ 0,y+ 4,3,8,cbg,cbg)
			gui.drawRectangle(x+42,y+ 4,3,8,cbg,cbg)
			gui.drawRectangle(x+ 1,y+14,10,1,cbg,cbg)
			gui.drawRectangle(x+ 3,y+16,7,1,cbg,cbg)
			gui.drawRectangle(x+ 5,y+18,4,1,cbg,cbg)
			gui.drawRectangle(x+34,y+14,10,1,cbg,cbg)
			gui.drawRectangle(x+35,y+16,7,1,cbg,cbg)
			gui.drawRectangle(x+36,y+18,4,1,cbg,cbg)
			gui.drawRectangle(x+ 2,y+ 2,1,1,cbg,cbg)
			gui.drawRectangle(x+42,y+ 2,1,1,cbg,cbg)

			gui.drawLine(x+ 1,y+13,x+ 3,y+13,cbg)
			gui.drawLine(x+42,y+13,x+44,y+13,cbg)

			gui.drawRectangle(x+ 8,y+ 2, 3, 3,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 8,y+10, 3, 3,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 4,y+ 6, 3, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+12,y+ 6, 3, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)
			gui.drawRectangle(x+25,y+ 6, 1, 1,c['X']     and cyp or cnp,c['X']     and cyp or cnp)
			gui.drawRectangle(x+31,y+ 4, 1, 1,c['Y']     and cyp or cnp,c['Y']     and cyp or cnp)
			gui.drawRectangle(x+37,y+ 2, 1, 1,c['Z']     and cyp or cnp,c['Z']     and cyp or cnp)
			gui.drawRectangle(x+19,y+ 5, 3, 1,c['Start'] and cyp or cnp,c['Start'] and cyp or cnp)
			-- gui.drawRectangle(x+19,y+ 9, 3, 1,c['Mode']  and cyp or cnp,c['Mode']  and cyp or cnp)

			gui.drawEllipse(x+26,y+ 9, 3, 3,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
			gui.drawEllipse(x+32,y+ 7, 3, 3,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
			gui.drawEllipse(x+38,y+ 5, 3, 3,c['C'] and cyp or cnp,c['C'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'GBA' do
	if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1)
	else c = joypad.get() end

	x = xpos
	y = ypos

	gui.drawRectangle(x+0,y+5,35,8,cbg,cbg)
	gui.drawLine(x+1,y+4,x+34,y+4,cbg)
	gui.drawLine(x+2,y+3,x+33,y+3,cbg)
	gui.drawLine(x+3,y+2,x+32,y+2,cbg)
	gui.drawLine(x+7,y+1,x+28,y+1,cbg)
	gui.drawLine(x+9,y+0,x+26,y+0,cbg)
	gui.drawLine(x+1,y+14,x+34,y+14,cbg)
	gui.drawLine(x+5,y+15,x+30,y+15,cbg)
	gui.drawLine(x+9,y+16,x+26,y+16,cbg)

	gui.drawRectangle(x+9,y+3,17,10,cnp,cnp)

	gui.drawRectangle(x+ 3,y+5,1,1,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
	gui.drawRectangle(x+ 3,y+9,1,1,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
	gui.drawRectangle(x+ 1,y+7,1,1,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
	gui.drawRectangle(x+ 5,y+7,1,1,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)

	gui.drawRectangle(x+29,y+ 8, 1, 1,c['B']      and cyp or cnp,c['B']      and cyp or cnp)
	gui.drawRectangle(x+32,y+ 6, 1, 1,c['A']      and cyp or cnp,c['A']      and cyp or cnp)
	gui.drawRectangle(x+ 6,y+10, 1, 1,c['Start']  and cyp or cnp,c['Start']  and cyp or cnp)
	gui.drawRectangle(x+ 6,y+13, 1, 1,c['Select'] and cyp or cnp,c['Select'] and cyp or cnp)

	gui.drawLine(x+1,y+1,x+6,y+1,c['L'] and cyp or cnp)
	gui.drawLine(x+0,y+2,x+2,y+2,c['L'] and cyp or cnp)
	gui.drawLine(x+0,y+3,x+1,y+3,c['L'] and cyp or cnp)
	gui.drawPixel(x+0,y+4,c['L'] and cyp or cnp)
	gui.drawLine(x+29,y+1,x+34,y+1,c['R'] and cyp or cnp)
	gui.drawLine(x+33,y+2,x+35,y+2,c['R'] and cyp or cnp)
	gui.drawLine(x+34,y+3,x+35,y+3,c['R'] and cyp or cnp)
	gui.drawPixel(x+35,y+4,c['R'] and cyp or cnp)

	emu.frameadvance()
end

while sys == 'Coleco' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 42
			else
				x = xpos + (h - 1) * 22
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+ 0,15,37,cbg,cbg)

			gui.drawRectangle(x+ 6,y+ 0, 3, 3,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 6,y+ 8, 3, 3,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 2,y+ 4, 3, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+10,y+ 4, 3, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)
			gui.drawRectangle(x+ 0,y+12, 1, 3,c['L']     and cyp or cnp,c['L']     and cyp or cnp)
			gui.drawRectangle(x+14,y+12, 1, 3,c['R']     and cyp or cnp,c['R']     and cyp or cnp)
			gui.drawRectangle(x+ 1,y+18, 3, 3,c['Key 1'] and cyp or cnp,c['Key 1'] and cyp or cnp)
			gui.drawRectangle(x+ 6,y+18, 3, 3,c['Key 2'] and cyp or cnp,c['Key 2'] and cyp or cnp)
			gui.drawRectangle(x+11,y+18, 3, 3,c['Key 3'] and cyp or cnp,c['Key 3'] and cyp or cnp)
			gui.drawRectangle(x+ 1,y+23, 3, 3,c['Key 4'] and cyp or cnp,c['Key 4'] and cyp or cnp)
			gui.drawRectangle(x+ 6,y+23, 3, 3,c['Key 5'] and cyp or cnp,c['Key 5'] and cyp or cnp)
			gui.drawRectangle(x+11,y+23, 3, 3,c['Key 6'] and cyp or cnp,c['Key 6'] and cyp or cnp)
			gui.drawRectangle(x+ 1,y+28, 3, 3,c['Key 7'] and cyp or cnp,c['Key 7'] and cyp or cnp)
			gui.drawRectangle(x+ 6,y+28, 3, 3,c['Key 8'] and cyp or cnp,c['Key 8'] and cyp or cnp)
			gui.drawRectangle(x+11,y+28, 3, 3,c['Key 9'] and cyp or cnp,c['Key 9'] and cyp or cnp)
			gui.drawRectangle(x+ 1,y+33, 3, 3,c['Star']  and cyp or cnp,c['Star']  and cyp or cnp)
			gui.drawRectangle(x+ 6,y+33, 3, 3,c['Key 0'] and cyp or cnp,c['Key 0'] and cyp or cnp)
			gui.drawRectangle(x+11,y+33, 3, 3,c['Pound'] and cyp or cnp,c['Pound'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

-- Note: joypad.get() is buggy when giving numbers higher than 0-9!
while sys == 'SAT' do
	for h = 1,12,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 24
			else
				x = xpos + (h - 1) * 50
				y = ypos
			end

			gui.drawRectangle(x+ 2,y+ 4,41,12,cbg,cbg)
			gui.drawRectangle(x+ 3,y+ 2,39, 1,cbg,cbg)
			gui.drawLine(x+ 1,y+ 6,x+ 1,y+16,cbg)
			gui.drawLine(x+ 0,y+10,x+ 0,y+16,cbg)
			gui.drawLine(x+44,y+ 6,x+44,y+16,cbg)
			gui.drawLine(x+45,y+10,x+45,y+16,cbg)
			gui.drawLine(x+ 1,y+17,x+19,y+17,cbg)
			gui.drawLine(x+26,y+17,x+44,y+17,cbg)
			gui.drawLine(x+ 2,y+18,x+15,y+18,cbg)
			gui.drawLine(x+30,y+18,x+43,y+18,cbg)
			gui.drawLine(x+ 4,y+19,x+ 9,y+19,cbg)
			gui.drawLine(x+36,y+19,x+41,y+19,cbg)
			gui.drawLine(x+ 4,y+ 1,x+19,y+ 1,cbg)
			gui.drawLine(x+ 6,y+ 0,x+15,y+ 0,cbg)
			gui.drawLine(x+26,y+ 1,x+41,y+ 1,cbg)
			gui.drawLine(x+30,y+ 0,x+39,y+ 0,cbg)

			gui.drawRectangle(x+ 8,y+ 4, 3, 3,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 8,y+12, 3, 3,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 4,y+ 8, 3, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+12,y+ 8, 3, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)
			gui.drawRectangle(x+24,y+ 8, 1, 1,c['X']     and cyp or cnp,c['X']     and cyp or cnp)
			gui.drawRectangle(x+31,y+ 6, 1, 1,c['Y']     and cyp or cnp,c['Y']     and cyp or cnp)
			gui.drawRectangle(x+37,y+ 4, 1, 1,c['Z']     and cyp or cnp,c['Z']     and cyp or cnp)
			gui.drawRectangle(x+19,y+12, 3, 1,c['Start'] and cyp or cnp,c['Start'] and cyp or cnp)
			gui.drawRectangle(x+ 8,y+ 0, 3, 1,c['L']     and cyp or cnp,c['L']     and cyp or cnp)
			gui.drawRectangle(x+34,y+ 0, 3, 1,c['R']     and cyp or cnp,c['R']     and cyp or cnp)

			gui.drawEllipse(x+26,y+11, 3, 3,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
			gui.drawEllipse(x+32,y+ 9, 3, 3,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
			gui.drawEllipse(x+38,y+ 7, 3, 3,c['C'] and cyp or cnp,c['C'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'A78' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if c['Button'] ~= nil then c['Trigger'] = c['Button'] end

			if topdown then
				x = xpos
				y = ypos + (h - 1) * 24
			else
				x = xpos + (h - 1) * 24
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+ 0,19,19,cbg,cbg)

			gui.drawRectangle(x+ 8,y+ 6, 3, 3,c['Up']        and cyp or cnp,c['Up']        and cyp or cnp)
			gui.drawRectangle(x+ 8,y+14, 3, 3,c['Down']      and cyp or cnp,c['Down']      and cyp or cnp)
			gui.drawRectangle(x+ 4,y+10, 3, 3,c['Left']      and cyp or cnp,c['Left']      and cyp or cnp)
			gui.drawRectangle(x+12,y+10, 3, 3,c['Right']     and cyp or cnp,c['Right']     and cyp or cnp)
			gui.drawRectangle(x+ 0,y+ 0, 3, 5,c['Trigger']   and cyp or cnp,c['Trigger']   and cyp or cnp)
			gui.drawRectangle(x+16,y+ 0, 3, 5,c['Trigger 2'] and cyp or cnp,c['Trigger 2'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'SG' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 24
			else
				x = xpos + (h - 1) * 24
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+ 0,19,19,cbg,cbg)

			gui.drawRectangle(x+ 8,y+ 6, 3, 3,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 8,y+14, 3, 3,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 4,y+10, 3, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+12,y+10, 3, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)
			gui.drawRectangle(x+ 0,y+ 0, 3, 5,c['B1']    and cyp or cnp,c['B1']    and cyp or cnp)
			gui.drawRectangle(x+16,y+ 0, 3, 5,c['B2']    and cyp or cnp,c['B2']    and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'SMS' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 20
			else
				x = xpos + (h - 1) * 40
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+ 0,35,15,cbg,cbg)

			gui.drawRectangle(x+ 6,y+ 2, 3, 3,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 6,y+10, 3, 3,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 2,y+ 6, 3, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+10,y+ 6, 3, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)

			gui.drawEllipse(x+24,y+7, 3, 3,c['B1'] and cyp or cnp,c['B1'] and cyp or cnp)
			gui.drawEllipse(x+30,y+7, 3, 3,c['B2'] and cyp or cnp,c['B2'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end

while sys == 'Lynx' do
	if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1)
	else c = joypad.get() end

	x = xpos
	y = ypos

	gui.drawRectangle(x+ 0,y+ 3,31, 8,cbg,cbg)
	gui.drawRectangle(x+ 1,y+ 0, 5, 2,cbg,cbg)
	gui.drawRectangle(x+ 1,y+12, 5, 2,cbg,cbg)
	gui.drawRectangle(x+25,y+ 0, 5, 2,cbg,cbg)
	gui.drawRectangle(x+25,y+12, 5, 2,cbg,cbg)
	gui.drawRectangle(x+ 7,y+ 1,17, 1,cbg,cbg)
	gui.drawRectangle(x+ 7,y+12,17, 1,cbg,cbg)

	gui.drawRectangle(x+11,y+4,9,6,cnp,cnp)

	gui.drawRectangle(x+ 3,y+5, 1, 1,c['Up']       and cyp or cnp,c['Up']       and cyp or cnp)
	gui.drawRectangle(x+ 3,y+9, 1, 1,c['Down']     and cyp or cnp,c['Down']     and cyp or cnp)
	gui.drawRectangle(x+ 1,y+7, 1, 1,c['Left']     and cyp or cnp,c['Left']     and cyp or cnp)
	gui.drawRectangle(x+ 5,y+7, 1, 1,c['Right']    and cyp or cnp,c['Right']    and cyp or cnp)
	gui.drawLine(x+22,y+5,x+23,y+5,c['Option 1'] and cyp or cnp)
	gui.drawLine(x+22,y+7,x+23,y+7,c['Pause']    and cyp or cnp)
	gui.drawLine(x+22,y+9,x+23,y+9,c['Option 2'] and cyp or cnp)

	gui.drawRectangle(x+25,y+ 2, 1, 1,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
	gui.drawRectangle(x+28,y+ 1, 1, 1,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
	gui.drawRectangle(x+25,y+11, 1, 1,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
	gui.drawRectangle(x+28,y+12, 1, 1,c['A'] and cyp or cnp,c['A'] and cyp or cnp)

	emu.frameadvance()
end

link = joypad.get()['P2 Up'] ~= nil
prev = false
while sys == 'GG' do
	if movie.mode() == 'PLAY' then tc = movie.getinput(emu.framecount() - 1, h)['Toggle Cable']
	else tc = joypad.get()['Toggle Cable'] end

	if not prev and tc then
		link = not link
	end
	prev = tc

	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			x = xpos + (h - 1) * 34
			y = ypos

			gui.drawRectangle(x+ 0,y+6,29,11,cbg,cbg)
			gui.drawLine(x+ 2,y+4,x+27,y+4,cbg)
			gui.drawLine(x+ 1,y+5,x+28,y+5,cbg)
			gui.drawLine(x+ 1,y+18,x+28,y+18,cbg)
			gui.drawLine(x+ 2,y+19,x+27,y+19,cbg)

			gui.drawRectangle(x+ 9,y+6,11,8,cnp,cnp)

			gui.drawRectangle(x+ 3,y+ 9, 1, 1,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+ 3,y+13, 1, 1,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 1,y+11, 1, 1,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+ 5,y+11, 1, 1,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)

			gui.drawLine(x+23,y+ 8,x+24,y+ 7,c['Start'] and cyp or cnp)
			gui.drawPixel(x+24,y+ 8,c['Start'] and cyp or cnp)

			gui.drawRectangle(x+24,y+13, 1, 1,c['B1'] and cyp or cnp,c['B1'] and cyp or cnp)
			gui.drawRectangle(x+26,y+11, 1, 1,c['B2'] and cyp or cnp,c['B2'] and cyp or cnp)
		end
	end

	if link then
		gui.drawRectangle(xpos+ 7,ypos+2,1,1,cbg,cbg)
		gui.drawRectangle(xpos+41,ypos+2,1,1,cbg,cbg)
		gui.drawLine(xpos+7,ypos+1,xpos+42,ypos+1,cbg)
		gui.drawLine(xpos+8,ypos+0,xpos+41,ypos+0,cbg)
	end

	emu.frameadvance()
end

link = joypad.get()['P2 Up'] ~= nil -- Dual NeoPop for when?
prev = false
while sys == 'NGP' do
	if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1)
	else c = joypad.get() end

	if not prev and c['Toggle Cable'] then
		link = not link
	end
	prev = c['Toggle Cable']

	x = xpos
	y = ypos

	if not pluged[2] then
		gui.drawRectangle(x+ 0,y+1,28,11,cbg,cbg)
		gui.drawLine(x+1,y+0,x+27,y+0,cbg)
		gui.drawLine(x+1,y+13,x+27,y+13,cbg)
		gui.drawLine(x+8,y+14,x+21,y+14,cbg)

		gui.drawRectangle(x+9,y+2,11,9,cnp,cnp)

		gui.drawRectangle(x+ 3,y+4, 1, 1,c['Up']     and cyp or cnp,c['Up']     and cyp or cnp)
		gui.drawRectangle(x+ 3,y+8, 1, 1,c['Down']   and cyp or cnp,c['Down']   and cyp or cnp)
		gui.drawRectangle(x+ 1,y+6, 1, 1,c['Left']   and cyp or cnp,c['Left']   and cyp or cnp)
		gui.drawRectangle(x+ 5,y+6, 1, 1,c['Right']  and cyp or cnp,c['Right']  and cyp or cnp)
		gui.drawRectangle(x+26,y+1, 1, 1,c['Option'] and cyp or cnp,c['Option'] and cyp or cnp)

		gui.drawRectangle(x+23,y+7, 1, 1,c['A'] and cyp or cnp,c['A'] and cyp or cnp)
		gui.drawRectangle(x+26,y+5, 1, 1,c['B'] and cyp or cnp,c['B'] and cyp or cnp)
	else
	if enable[1] then
		gui.drawRectangle(x+ 0,y+5,28,11,cbg,cbg)
		gui.drawLine(x+1,y+4,x+27,y+4,cbg)
		gui.drawLine(x+1,y+17,x+27,y+17,cbg)
		gui.drawLine(x+8,y+18,x+21,y+18,cbg)

		gui.drawRectangle(x+9,y+6,11,9,cnp,cnp)

		gui.drawRectangle(x+ 3,y+ 8, 1, 1,c['P1 Up']     and cyp or cnp,c['P1 Up']     and cyp or cnp)
		gui.drawRectangle(x+ 3,y+12, 1, 1,c['P1 Down']   and cyp or cnp,c['P1 Down']   and cyp or cnp)
		gui.drawRectangle(x+ 1,y+10, 1, 1,c['P1 Left']   and cyp or cnp,c['P1 Left']   and cyp or cnp)
		gui.drawRectangle(x+ 5,y+10, 1, 1,c['P1 Right']  and cyp or cnp,c['P1 Right']  and cyp or cnp)
		gui.drawRectangle(x+26,y+ 5, 1, 1,c['P1 Option'] and cyp or cnp,c['P1 Option'] and cyp or cnp)

		gui.drawRectangle(x+23,y+11, 1, 1,c['P1 A'] and cyp or cnp,c['P1 A'] and cyp or cnp)
		gui.drawRectangle(x+26,y+ 9, 1, 1,c['P1 B'] and cyp or cnp,c['P1 B'] and cyp or cnp)
	end
	
	if enable[2] then
		gui.drawRectangle(x+33,y+5,28,11,cbg,cbg)
		gui.drawLine(x+34,y+4,x+60,y+4,cbg)
		gui.drawLine(x+34,y+17,x+60,y+17,cbg)
		gui.drawLine(x+41,y+18,x+54,y+18,cbg)

		gui.drawRectangle(x+42,y+6,11,9,cnp,cnp)

		gui.drawRectangle(x+36,y+ 8, 1, 1,c['P2 Up']     and cyp or cnp,c['P2 Up']     and cyp or cnp)
		gui.drawRectangle(x+36,y+12, 1, 1,c['P2 Down']   and cyp or cnp,c['P2 Down']   and cyp or cnp)
		gui.drawRectangle(x+34,y+10, 1, 1,c['P2 Left']   and cyp or cnp,c['P2 Left']   and cyp or cnp)
		gui.drawRectangle(x+38,y+10, 1, 1,c['P2 Right']  and cyp or cnp,c['P2 Right']  and cyp or cnp)
		gui.drawRectangle(x+59,y+ 5, 1, 1,c['P2 Option'] and cyp or cnp,c['P2 Option'] and cyp or cnp)

		gui.drawRectangle(x+56,y+11, 1, 1,c['P2 A'] and cyp or cnp,c['P2 A'] and cyp or cnp)
		gui.drawRectangle(x+59,y+ 9, 1, 1,c['P2 B'] and cyp or cnp,c['P2 B'] and cyp or cnp)
	end
	end

	if link then
		gui.drawRectangle(x+21,y+2,1,1,cbg,cbg)
		gui.drawRectangle(x+54,y+2,1,1,cbg,cbg)
		gui.drawLine(x+21,y+1,x+55,y+1,cbg)
		gui.drawLine(x+22,y+0,x+54,y+0,cbg)
	end

	emu.frameadvance()
end

Mode = {{false,false},{false,false}}
Prev = {{false,false},{false,false}}
while sys == 'PCFX' do
	for h = 1,2,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if not Prev[h][1] and c['Mode 1'] then Mode[h][1] = not Mode[h][1] end
		if not Prev[h][2] and c['Mode 2'] then Mode[h][2] = not Mode[h][2] end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 26
			else
				x = xpos + (h - 1) * 54
				y = ypos
			end

			gui.drawRectangle(x+ 0,y+ 6,49,11,cbg,cbg)
			gui.drawRectangle(x+ 2,y+ 4,45, 1,cbg,cbg)
			gui.drawRectangle(x+ 4,y+ 2,41, 1,cbg,cbg)
			gui.drawRectangle(x+ 2,y+18,15, 1,cbg,cbg)
			gui.drawRectangle(x+ 4,y+20,11, 1,cbg,cbg)
			gui.drawRectangle(x+32,y+18,15, 1,cbg,cbg)
			gui.drawRectangle(x+34,y+20,11, 1,cbg,cbg)
			gui.drawLine(x+11,y+1,x+38,y+1,cbg)
			gui.drawLine(x+18,y+0,x+31,y+0,cbg)

			gui.drawRectangle(x+ 8,y+ 6, 3, 3,c['Up']     and cyp or cnp,c['Up']     and cyp or cnp)
			gui.drawRectangle(x+ 8,y+14, 3, 3,c['Down']   and cyp or cnp,c['Down']   and cyp or cnp)
			gui.drawRectangle(x+ 4,y+10, 3, 3,c['Left']   and cyp or cnp,c['Left']   and cyp or cnp)
			gui.drawRectangle(x+12,y+10, 3, 3,c['Right']  and cyp or cnp,c['Right']  and cyp or cnp)
			gui.drawRectangle(x+18,y+14, 3, 1,c['Select'] and cyp or cnp,c['Select'] and cyp or cnp)
			gui.drawRectangle(x+24,y+14, 3, 1,c['Run']    and cyp or cnp,c['Run']    and cyp or cnp)
			gui.drawRectangle(x+21,y+ 7, 1, 1,Mode[h][1]  and cnp or cyp,Mode[h][1]  and cnp or cyp)
			gui.drawRectangle(x+23,y+ 7, 1, 1,Mode[h][1]  and cyp or cnp,Mode[h][1]  and cyp or cnp)
			gui.drawRectangle(x+21,y+11, 1, 1,Mode[h][2]  and cnp or cyp,Mode[h][2]  and cnp or cyp)
			gui.drawRectangle(x+23,y+11, 1, 1,Mode[h][2]  and cyp or cnp,Mode[h][2]  and cyp or cnp)

			gui.drawEllipse(x+30,y+13, 3, 3,c['III'] and cyp or cnp,c['III'] and cyp or cnp)
			gui.drawEllipse(x+36,y+12, 3, 3,c['II']  and cyp or cnp,c['II']  and cyp or cnp)
			gui.drawEllipse(x+42,y+11, 3, 3,c['I']   and cyp or cnp,c['I']   and cyp or cnp)
			gui.drawEllipse(x+29,y+ 8, 3, 3,c['IV']  and cyp or cnp,c['IV']  and cyp or cnp)
			gui.drawEllipse(x+35,y+ 7, 3, 3,c['V']   and cyp or cnp,c['V']   and cyp or cnp)
			gui.drawEllipse(x+41,y+ 6, 3, 3,c['VI']  and cyp or cnp,c['VI']  and cyp or cnp)
		end

	Prev[h] = {c['Mode 1'],c['Mode 2']}

	end

	emu.frameadvance()
end

while sys == 'PSX' do
	for h = 1,8,1 do
		if movie.mode() == 'PLAY' then c = movie.getinput(emu.framecount() - 1, h)
		else c = joypad.get(h) end

		if enable[h] and pluged[h] then
			if topdown then
				x = xpos
				y = ypos + (h - 1) * 38
			else
				x = xpos + (h - 1) * 60
				y = ypos
			end

			gui.drawRectangle(x+ 2,y+7,51,11,cbg,cbg)
			gui.drawRectangle(x+ 6,y+0,11, 3,cbg,cbg)
			gui.drawRectangle(x+38,y+0,11, 3,cbg,cbg)
			gui.drawLine(x+ 5,y+ 4,x+19,y+ 4,cbg)
			gui.drawLine(x+36,y+ 4,x+50,y+ 4,cbg)
			gui.drawLine(x+ 4,y+ 5,x+51,y+ 5,cbg)
			gui.drawLine(x+ 3,y+ 6,x+52,y+ 6,cbg)
			gui.drawLine(x+ 1,y+19,x+54,y+19,cbg)

			gui.drawLine(x+ 1,y+20,x+19,y+20,cbg)
			gui.drawLine(x+ 1,y+21,x+17,y+21,cbg)
			gui.drawLine(x+ 1,y+22,x+15,y+22,cbg)
			gui.drawRectangle(x+ 0,y+23,14, 2,cbg,cbg)
			gui.drawRectangle(x+ 0,y+26,13, 1,cbg,cbg)
			gui.drawRectangle(x+ 0,y+28,12, 1,cbg,cbg)
			gui.drawLine(x+ 1,y+30,x+11,y+30,cbg)
			gui.drawLine(x+ 2,y+31,x+10,y+31,cbg)
			gui.drawLine(x+ 3,y+32,x+ 9,y+32,cbg)
			gui.drawLine(x+ 5,y+33,x+ 8,y+33,cbg)

			gui.drawLine(x+36,y+20,x+54,y+20,cbg)
			gui.drawLine(x+38,y+21,x+54,y+21,cbg)
			gui.drawLine(x+40,y+22,x+54,y+22,cbg)
			gui.drawRectangle(x+41,y+23,14, 2,cbg,cbg)
			gui.drawRectangle(x+42,y+26,13, 1,cbg,cbg)
			gui.drawRectangle(x+43,y+28,12, 1,cbg,cbg)
			gui.drawLine(x+44,y+30,x+54,y+30,cbg)
			gui.drawLine(x+45,y+31,x+53,y+31,cbg)
			gui.drawLine(x+46,y+32,x+52,y+32,cbg)
			gui.drawLine(x+47,y+33,x+50,y+33,cbg)

			gui.drawRectangle(x+10,y+ 7, 3, 2,c['Up']    and cyp or cnp,c['Up']    and cyp or cnp)
			gui.drawRectangle(x+10,y+16, 3, 2,c['Down']  and cyp or cnp,c['Down']  and cyp or cnp)
			gui.drawRectangle(x+ 6,y+11, 2, 3,c['Left']  and cyp or cnp,c['Left']  and cyp or cnp)
			gui.drawRectangle(x+15,y+11, 2, 3,c['Right'] and cyp or cnp,c['Right'] and cyp or cnp)
			gui.drawLine(x+11,y+10,x+12,y+10,c['Up']    and cyp or cnp)
			gui.drawLine(x+11,y+15,x+12,y+15,c['Down']  and cyp or cnp)
			gui.drawLine(x+ 9,y+12,x+ 9,y+13,c['Left']  and cyp or cnp)
			gui.drawLine(x+14,y+12,x+14,y+13,c['Right'] and cyp or cnp)

			gui.drawEllipse(x+42,y+ 7, 3, 3,c['Triangle'] and cyp or cnp,c['Triangle'] and cyp or cnp)
			gui.drawEllipse(x+38,y+11, 3, 3,c['Square']   and cyp or cnp,c['Square']   and cyp or cnp)
			gui.drawEllipse(x+46,y+11, 3, 3,c['Circle']   and cyp or cnp,c['Circle']   and cyp or cnp)
			gui.drawEllipse(x+42,y+15, 3, 3,c['Cross']    and cyp or cnp,c['Cross']    and cyp or cnp)

			gui.drawRectangle(x+23,y+15,3,1,c['Select'] and cyp or cnp)
			gui.drawRectangle(x+29,y+15,3,1,c['Start'] and cyp or cnp)
			gui.drawRectangle(x+10,y+ 0,3,1,c['L2'] and cyp or cnp)
			gui.drawRectangle(x+10,y+ 3,3,1,c['L1'] and cyp or cnp)
			gui.drawRectangle(x+42,y+ 0,3,1,c['R2'] and cyp or cnp)
			gui.drawRectangle(x+42,y+ 3,3,1,c['R1'] and cyp or cnp)
		end
	end

	emu.frameadvance()
end