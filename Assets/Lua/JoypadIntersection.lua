---------------------------------------------------------
-- Small script for only allowing input on P1 controller
-- if both P1 and P2 holds down a specific input
--
-- Note that this script only works on systems which
-- has two or more joypads (such as NES) and not on
-- systems with just one joypad (such as Gameboy)
--
-- Author: Gikkman
---------------------------------------------------------

-- Pre-made array for resetting the P1 joypad
local reset = joypad.get(1)
for k,v in pairs(reset) do
    reset[k] = ''
end

event.onframestart( function()
    local p1 = joypad.get(1)
    local p2 = joypad.get(2)
    local consolidated = intersection(p1, p2)
    
    gui.drawText(0,10, 'P1: ' .. dump(p1))
    gui.drawText(0,25, 'P2: ' .. dump(p2))
    
    joypad.set(consolidated, 1)
end )
 
event.onframeend( function()	
    joypad.set(reset, 1)
end )
 
-- Get intersection of P1 and P2 joypads
function intersection(p1, p2)
    local ret = {}
    for k,v in pairs(p1) do
        ret[k] = p1[k] and p2[k]
    end
    return ret
end
 
-- Print all pressed buttons
function dump(o)
  local s = ''
  for k,v in pairs(o) do
     if v then s = s .. tostring(k) .. ' ' end
  end
  return s
end
 

--------------------------------------
--             Main loop            --
--------------------------------------
while true do
    emu.frameadvance()
end
