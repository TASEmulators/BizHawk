-- Super Mario Bros. 2 USA - Grids & Contents (Unfinished)
-- Super Mario Bros. 2 (U) (PRG0) [!].nes
-- Written by QFox
-- 31 July 2008

-- shows (proper!) grid and contents. disable grid by setting variable to false
-- shows any non-air grid's tile-id
-- Slow! Will be heavy on lighter systems



local angrybirdo = false; -- makes birdo freak, but can skew other creatures with timing :)
local drawgrid = false; -- draws a green grid

local function box(x1,y1,x2,y2,color)
	-- gui.text(50,50,x1..","..y1.." "..x2..","..y2);
	gui.drawNew()
	if (x1 > 0 and x1 < 0xFF and x2 > 0 and x2 < 0xFF and y1 > 0 and y1 < 239 and y2 > 0 and y2 < 239) then
		gui.drawRectangle(x1,y1,x2,y2,color);
	end;
	gui.drawFinish()
end;
local function text(x,y,str)
	if (x > 0 and x < 0xFF and y > 0 and y < 240) then
		--gui.text(x,y,str);
	end;
end;
local function toHexStr(n)
	local meh = "%X";
	return meh:format(n);
end;

while (true) do
	if (angrybirdo and mainmemory.read_u8(0x0010) > 0x81) then memory.writebyte(0x0010, 0x6D); end; -- birdo fires eggs constantly :p
	
	-- px = horzizontal page of current level
	-- x = page x (relative to current page)
	-- rx = real x (relative to whole level)
	-- sx = screen x (relative to viewport)
	local playerpx = mainmemory.read_u8(0x0014);
	local playerpy = mainmemory.read_u8(0x001E);
	local playerx = mainmemory.read_u8(0x0028);
	local playery = mainmemory.read_u8(0x0032);
	local playerrx = (playerpx*0xFF)+playerx;
	local playerry = (playerpy*0xFF)+playery;
	local playerstate = mainmemory.read_u8(0x0050);
	local screenoffsetx = mainmemory.read_u8(0x04C0);
	local screenoffsety = mainmemory.read_u8(0x00CB);

	local playersx = (playerx - screenoffsetx);
	if (playersx < 0) then playersx = playersx + 0xFF; end;
	
	local playersy = (playery - screenoffsety);
	if (playersy < 0) then playersy = playersy + 0xFF; end;
	
	if (playerstate ~= 0x07) then
		box(playersx, playersy, playersx+16, playersy+16, "green");
	end;

	if (mainmemory.read_u8(0x00D8) == 0) then -- not scrolling vertically
		-- show environment
		-- i have playerrx, which is my real position in this level
		-- i have the level, which is located at 0x6000 (the SRAM)
		-- each tile (denoted by one byte) is 16x16 pixels
		-- each screen is 15 tiles high and about 16 tiles wide
		-- to get the right column, we add our playerrx/16 to 0x6000
		-- to be exact:
		-- 0x6000 + (math.floor(playerrx/16) * 0xF0) + math.mod(playerx,0x0F)
		
		local levelstart = 0x6000; -- start of level layout in RAM
	
	  -- ok, here we have two choices. either this is a horizontal level or
	  -- it is a vertical level. We have no real way of checking this, but
	  -- luckily levels are either horizontal or vertical :)
	  -- so there are three possibilities
	  -- 1: you're in 0:0, no worries
	  -- 2: you're in x:0, you're in a horizontal level
	  -- 3: you're in 0:y, you're in a vertical level
	  
	
		local addleftcrap = math.mod(screenoffsetx,16)*-1; -- works as padding to keep the grid aligned
		local leftsplitrx = (mainmemory.read_u8(0x04BE)*0x100) + (screenoffsetx + addleftcrap); -- column start left. add addleftcrap to iterative stuff to build up
		local addtopcrap = math.mod(screenoffsety,15); -- works as padding to keep the grid aligned
		local columns = math.floor(leftsplitrx/16); -- column x of the level is on the left side of the screen
		
		if (drawgrid) then -- print grid?
			for i=0,15 do
				-- text(addleftcrap+(i*16)-1, 37, toHexStr(columns+i)); -- print colnumber in each column
				for j=0,17 do
					box(addleftcrap+(i*16), addtopcrap+(j*16), addleftcrap+(i*16)+16, addtopcrap+(j*16)+16, "green"); -- draw green box for each cell
				end;
			end;
		end;
	
	-- 42=mushroom if you go sub
	-- 45=small
	-- 44=big
	-- 49=subspace
	-- 6c=pow
	-- 4e=cherry
		
		local topsplitry = (screenoffsety);
	
		-- starting page (might flow into next page). if the number of columns 
		-- is > 16, its a horizontal level, else its a vertical level. in either 
		-- case, the other will not up this value.
		local levelpage = 
				levelstart + 
				((math.floor(columns/16))*0xF0) + 
				(mainmemory.read_u8(0x00CA)*0x100) +
				topsplitry;
		local levelcol = math.mod(columns,16); -- this is our starting column
	
		--text(10,150,toHexStr(topsplitry).." "..toHexStr(levelcol).." "..toHexStr(levelpage+levelcol).." "..toHexStr(leftsplitrx));
	
		for j=0,15 do -- 16 columns
			if (levelcol + j > 15) then -- go to next page
				levelpage = levelpage + 0xF0;
				levelcol = -j;
			end;
			for i=0,14 do -- 15 rows
				local tile = mainmemory.read_u8(levelpage+(levelcol+j)+(i*0x10));
				if (tile ~= 0x40) then
					text(-2+addleftcrap+(j*16),5+(i*16),toHexStr(tile));
				end;
			end;
		end;
	end; -- not scrolling if

	-- print some generic stats
	text(2,10,"x:"..toHexStr(screenoffsetx));
	text(2,25,"y: "..toHexStr(screenoffsety));
	text(230,10,mainmemory.read_u8(0x04C1));
	text(100,10,"Page: "..playerpx..","..playerpy);
	text(playersx,playersy,playerrx.."\n"..playery);
	
	-- draw enemy info
	local startpx = 0x0015;
	local startpy = 0x001F;
	local startx = 0x0029;
	local starty = 0x0033;
	local drawn = 0x0051;
	local type = 0x0090;
	for i=0,9 do
		local estate = mainmemory.read_u8(drawn+i);
		if (estate ~= 0) then
			local ex = mainmemory.read_u8(startx+i);
			local epx = mainmemory.read_u8(startpx+i);
			local ey = mainmemory.read_u8(starty+i);
			local epy = mainmemory.read_u8(startpy+i);
			local erx = (epx*0xFF)+ex;
			local ery = (epy*0xFF)+ey;
			local esx = (ex - screenoffsetx);
			if (esx < 0) then esx = esx + 0xFF; end;
			local esy = (ey - screenoffsety);
			if (esy < 0) then esy = esy + 0xFF; end;

			--text(10, 20+(16*i), i..": "..esx.." "..erx); -- show enemy position list

			-- show enemy information
			if ((erx > playerrx-127) and erx < (playerrx+120)) then
				--text(esx,esy,erx); -- show level x pos above enemy
				local wtf = "%X";
				text(esx,esy,wtf:format(mainmemory.read_u8(type+i))); -- show enemy code
				if (estate == 1 and i < 5) then
					box(esx, esy, esx+16, esy+16, "red");
				else
					box(esx, esy, esx+16, esy+16, "blue");
				end;
			end;
		end;
	end; -- enemy info

	emu.frameadvance();
end;