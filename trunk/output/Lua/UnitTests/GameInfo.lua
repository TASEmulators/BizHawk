console.log("Game Info library test")
console.log("Rom Name: " .. gameinfo.getromname());
console.log("Rom Hash: " .. gameinfo.getromhash());
console.log("Display Type: " .. emu.getdisplaytype());
console.log("In Database?: " .. tostring(gameinfo.getindatabase()));
console.log("Rom Status: " .. gameinfo.getstatus());
console.log("Is Status Bad?: " .. tostring(gameinfo.getisstatusbad()));
console.log("Board Type: " .. gameinfo.getboardtype());
console.log("Game Options: ")
console.log(gameinfo.getoptions());