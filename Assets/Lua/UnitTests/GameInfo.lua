console.writeline("Game Info library test")
console.writeline("Rom Name: " .. gameinfo.getromname());
console.writeline("Rom Hash: " .. gameinfo.getromhash());
console.writeline("Display Type: " .. emu.getdisplaytype());
console.writeline("In Database?: " .. tostring(gameinfo.indatabase()));
console.writeline("Rom Status: " .. gameinfo.getstatus());
console.writeline("Is Status Bad?: " .. tostring(gameinfo.isstatusbad()));
console.writeline("Board Type: " .. gameinfo.getboardtype());
console.writeline("Game Options: ")
console.writeline(gameinfo.getoptions());