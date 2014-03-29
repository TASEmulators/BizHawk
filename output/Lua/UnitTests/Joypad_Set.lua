console.log("Unit test for joypad.set")
console.log("Core Required: NES (or any multi-player core with U,D,L,R,select,start,A,B as buttons)")
console.log("Correct behavior:")
console.log("No Directional button shoudl be imparied in any way, should operate as if nothing was ever pressed")
console.log("A should be off and user can not push buttons to change that")
console.log("B should be on and the user can not push buttons to change that")
console.log("Select should be on, but pressing it turns it off")
console.log("Start should be unaffected")
console.log("After frame 600, the console will say 'cleared', and now all buttons should be off and unaffected, as if the script was never run");

buttons = { };

buttons["P1 Up"] = false;
buttons["P1 Down"] = true;
buttons["P1 Left"] = "invert";
buttons["P1 Right"] = null;
joypad.set(buttons);

pushThings = true;	

while true do
	if (pushThings) then
		buttons = { };
		buttons["P1 A"] = false;
		buttons["P1 B"] = true;
		buttons["P1 Select"] = "invert";
		buttons["P1 Start"] = null;
		joypad.set(buttons);
	end
	
	if (emu.framecount() == 600) then
		pushThings = false;
		turnoff = { };
		turnoff["P1 A"] = null;
		turnoff["P1 B"] = null;
		turnoff["P1 Select"] = null;
		turnoff["P1 Start"] = null;
		
		joypad.set(turnoff);
		console.log("cleared")
	end
	
	emu.frameadvance();
end