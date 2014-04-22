while true do
	if (movie.isloaded()) then
		fps = movie.getfps();
		frames = movie.length();
		tseconds = (frames / fps)
		secondsraw = tseconds % 60;
		shift = 10 ^ 2;
		seconds = math.floor((secondsraw * shift) + 0.5) / shift;
		secondsstr = string.format("%.2f", seconds)
		if (seconds < 10) then
			secondsstr = "0" .. secondsstr;
		end
	
		minutes = (math.floor(tseconds / 60)) % 60;
		minutesstr = minutes;
		if (minutes < 10) then
			minutesstr = "0" .. minutesstr;
		end

		hours = (math.floor(tseconds / 60 / 60)) % 24;
		
		time = minutesstr .. ":" .. secondsstr;
		if (hours > 0) then
			time = "0" .. hours .. ":" .. time;
		end
		gui.text(0, 0, time, null, null, 1);
	end
	emu.frameadvance();
end