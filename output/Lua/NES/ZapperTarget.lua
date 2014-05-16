-- Draws a target on screen representing the x,y coordinates of the Zapper plugged into the 2nd port

while true do
	buttons = joypad.get();
	x = buttons["P2 Zapper X"];
	y = buttons["P2 Zapper Y"];
	fired = buttons["P2 Fire"];

	color = "white";
	if fired then
		color = "red";
	end

	gui.drawLine(x - 4, y + 4, x + 4, y - 4, color);
	gui.drawLine(x + 4, y + 4, x - 4, y - 4, color);
	gui.drawEllipse(x - 5, y - 5, 10, 10, color);
	gui.drawPixel(x, y, "red");
	gui.drawEllipse(x - 1, y - 1, 2, 2, "red");
	emu.frameadvance()
end