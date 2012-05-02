--Mike Tyson's Punch Out!!
--Shows Oppoenent & Mac's Health and damage amounts.
--adelikat


local EHP = 0x0398  -- Enemy HP address
local EHPx= 178
local EHPy= 14
local EnemyHP = 0
local lastEHP = 0

local MHP = 0x0391 -- Mac HP address
local MHPx = 122
local MHPy = 14
local MacHP = 0
local lastMHP = 0

local OppKnockedDown = 0x0005 -- Oppoenent is on the canvas flag
local OppDown		 -- Stores contents of 0x0005
local OppDx = 130
local OppDy = 70
local OppWillGetUpWith = 0x039E -- Health that the oppoenent will get up with
local OppWillGet			  -- Stores contents of 0x039E

local OppHitFlag = 0x03E0
local OppHit
local OppHitTimer = 0
local OppHitToDisplay = 0

OHitValuex = 100
OHitValuey = 100

--*****************************************************************************
function IsOppDown()
--*****************************************************************************
	OppDown = mainmemory.read_u8(OppKnockedDown)
	if OppDown > 0 then
		return true
	end
      return false
end

--*****************************************************************************
function OppIsHit()
--*****************************************************************************
      OppHit = mainmemory.read_u8(OppHitFlag)
	if OppHit > 0 then
		return true
	end
      return false
end



--*****************************************************************************
while true do
--*****************************************************************************
    EnemyHP = mainmemory.read_u8(EHP)
	gui.text(0,0,"Opponent: " .. EnemyHP, null, null, "topright")

    MacHP = mainmemory.read_u8(MHP)
    gui.text(0,12,"Mac: " .. MacHP, null, null, "topright")

    if IsOppDown() then
	    OppWillGet = mainmemory.read_u8(OppWillGetUpWith)
	    gui.text(0, 12, "Next health: " .. OppWillGet, null, null, "bottomright")
    end

    if OppIsHit() then
	    OppHitToDisplay = lastEHP - EnemyHP
	    OppHitTimer = 60

    end

    if OppHitTimer > 0 then
	    gui.text(0, 0, "Damage: " .. OppHitToDisplay, null, null, "bottomright")
    end

    
    if OppHitTimer > 0 then
        OppHitTimer = OppHitTimer - 1
    end

    emu.frameadvance()
    lastEHP = EnemyHP
    lastMHP = MacHP 
end
