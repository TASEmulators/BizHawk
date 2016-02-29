console.writeline("Unit test for joypad.set using the controller parameter")
console.writeline("Core Required: NES (or any multi-player core with U,D,L,R,select,start,A,B as buttons)")
console.writeline("Correct behavior:")
console.writeline("No Directional button shoudl be imparied in any way, should operate as if nothing was ever pressed")
console.writeline("A should be off and user can not push buttons to change that")
console.writeline("B should be on and the user can not push buttons to change that")
console.writeline("Select should be on, but pressing it turns it off")
console.writeline("Start should be unaffected")
console.writeline("After frame 600, the console will say 'cleared', and now all buttons should be off and unaffected, as if the script was never run");

buttons = { };

buttons["Up"] = false;
buttons["Down"] = true;
buttons["Left"] = "invert";
buttons["Right"] = null;
joypad.set(buttons, 1);

pushThings = true;	

while true do
	if (pushThings) then
		buttons = { };
		buttons["A"] = false;
		buttons["B"] = true;
		buttons["Select"] = "invert";
		buttons["Start"] = null;
		joypad.set(buttons, 1);
	end
	
	if (emu.framecount() == 600) then
		pushThings = false;
		turnoff = { };
		turnoff["A"] = null;
		turnoff["B"] = null;
		turnoff["Select"] = null;
		turnoff["Start"] = null;
		
		joypad.set(turnoff, 1);
		console.writeline("cleared")
	end
	
	emu.frameadvance();
end