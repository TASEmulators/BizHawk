--RBI Baseball script
--Written by adelikat
--Shows stats and information on screen and can even change a batter or pitcher's hand

local PitchingScreenAddr = 0x001A;
local PitchingScreen;

local p1PitchHealthAddr = 0x060D;
local p1PitchHealth;

local p2PitchHealthAddr = 0x061D;
local p2PitchHealth;

local p1OutsAddr = 0x0665;
local p1Outs;

local pitchtypeAddr = 0x0112;
local pitchtype;

local P1currHitterPowerAddr = 0x062B --2 byte
local P1currSpeedAddr = 0x062D
local P1currContactAddr = 0x062A

local P2currHitterPowerAddr = 0x063B --2 byte
local P2currSpeedAddr = 0x063D
local P2currContactAddr = 0x063A

local topinningAddr = 0x0115

--Extra Ram map notes
--0627 = P1 Batter Lefy or Righty (even or odd)
--0628 = P1 Bat average 150 + this address
--0629 = P1 # of Home Runs
--0638 = P2 Bat average 2 150 + this address
--0639 = P2 # of Home Runs
--0637 = P2 Batter Lefy or Righty (even or odd)

--060x = P1 pitcher, 061x = P2 pitcher

--0607 = 
--Right digit =	P1 Pitcher Lefty or Right (even or odd)
--Left digit = P1 Pitcher drop rating

--0609 = P1 Sinker ball speed
--060A = P1 Regular ball Speed
--060B = P1 Fastball Speed
--060C = P1 Pitcher Curve rating left digit is curve left, right is curve right

--0114 = Current Inning
--0115 = 10 if bottom of inning, 0 if on top, controls which player can control the batter

--0728 & 0279 - In charge of inning music

--TODO
--A hotkeys for boosting/lowering current pitcher (p1 or 2) health
--fix pitcher L/R switching to not kill the left digit (drop rating) in the process
--Do integer division on curve rating
--Outs display is wrong
--Music on/off toggle

while true do

mainmemory.write_u8(0x0726, 0)	--Turn of inning music
mainmemory.write_u8(0x0727, 0)
mainmemory.write_u8(0x0728, 0)
mainmemory.write_u8(0x0729, 0)

inningtb = memory.readbyte(topinningAddr);
i = input.get();

--Switch P1 batter hands
if (i.K == true) then
	if (inningtb == 0x10) then	
		mainmemory.write_u8(0x0607, 0)
	end
	if (inningtb == 0) then
		mainmemory.write_u8(0x0627, 0)
	end
end
	
if (i.L == true) then
	if (inningtb == 0x10) then	
		mainmemory.write_u8(0x0607, 1)
	end
	if (inningtb == 0) then
		mainmemory.write_u8(0x0627, 1)
	end
end

--Switch P2 batter hands
if (i.H == true) then
	if (inningtb == 0x0) then	
		mainmemory.write_u8(0x0617, 0)
	end
	if (inningtb == 0x10) then
		mainmemory.write_u8(0x0637, 0)
	end
end
	
if (i.J == true) then
	if (inningtb == 0x0) then	
		mainmemory.write_u8(0x0617, 1)
	end
	if (inningtb == 0x10) then
		mainmemory.write_u8(0x0637, 1)
	end
end
	
PitchingScreen = memory.readbyte(PitchingScreenAddr);

-------------------------------------------------------
if (PitchingScreen == 0x003E) then
gui.text(186,24, "Toggle Hand\nH/J");
gui.text(1,24, "Toggle Hand\nK/L");

pitchtype = memory.readbyte(pitchtypeAddr);

--What the pitcher will pitch
if (pitchtype == 0) then
	gui.text(100,1,"Sinker!!");
end
if (pitchtype == 2) then
	gui.text(100,1,"Fast Ball")
end
if (pitchtype == 1) then
	gui.text(100,1,"Regular Pitch")
end

--Top of Inning
if (inningtb == 0) then
	gui.text(186,1,"Health  " .. memory.readbyte(0x061D))
	gui.text(186,128,"Drop    " .. memory.readbyte(0x0617) % 16)
	gui.text(186,136,"CurveL  " .. memory.readbyte(0x061C) / 16)
	gui.text(186,144,"CurveR  " .. memory.readbyte(0x061C) % 16)
	gui.text(186,152,"Fast SP " .. memory.readbyte(0x061B))
	gui.text(186,160,"Reg  SP " .. memory.readbyte(0x061A))
	gui.text(186,168,"Sink SP " .. memory.readbyte(0x0619))
	
	P1currPower = memory.readbyte(P1currHitterPowerAddr) + (memory.readbyte(P1currHitterPowerAddr+1) * 256);
	gui.text(1,176, "Power: " .. P1currPower);
	P1currSpeed = memory.readbyte(P1currSpeedAddr);
	gui.text(1,168, "Speed: " .. P1currSpeed);
	P1currCt = memory.readbyte(P1currContactAddr);
	gui.text(1,160, "Contact: " .. P1currCt);
end

--Bottom of Inning
if (inningtb == 0x10) then
	gui.text(1,1,"Health  " .. memory.readbyte(0x060D))
	gui.text(1,128,"Drop    " .. memory.readbyte(0x0607) % 16)
	gui.text(1,136,"CurveL  " .. memory.readbyte(0x060C) / 16)
	gui.text(1,144,"CurveR  " .. memory.readbyte(0x060C) % 16)
	gui.text(1,152,"Fast SP " .. memory.readbyte(0x060B))
	gui.text(1,160,"Reg  SP " .. memory.readbyte(0x060A))
	gui.text(1,168,"Sink SP " .. memory.readbyte(0x0609))

	P2currPower = memory.readbyte(P2currHitterPowerAddr) + (memory.readbyte(P2currHitterPowerAddr+1) * 256);
	gui.text(188,176, "Power: " .. P2currPower);
	P2currSpeed = memory.readbyte(P2currSpeedAddr);
	gui.text(188,168, "Speed: " .. P2currSpeed);
	P2currCt = memory.readbyte(P2currContactAddr);
	gui.text(188,160, "Contact: " .. P2currCt);
end

end
-------------------------------------------------------

if (PitchingScreen == 0x0036) then

p1Outs = memory.readbyte(p1OutsAddr);
gui.text(1,1, "Outs " .. p1Outs);

end
-------------------------------------------------------

emu.frameadvance()

end