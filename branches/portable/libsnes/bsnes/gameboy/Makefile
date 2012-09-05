options += gameboy

gameboy_objects := gameboy-interface gameboy-system gameboy-scheduler
gameboy_objects += gameboy-memory gameboy-cartridge
gameboy_objects += gameboy-cpu gameboy-apu gameboy-lcd
gameboy_objects += gameboy-cheat gameboy-video
objects += $(gameboy_objects)

obj/gameboy-interface.o: $(gameboy)/interface/interface.cpp $(call rwildcard,$(gameboy)/interface/)
obj/gameboy-system.o: $(gameboy)/system/system.cpp $(call rwildcard,$(gameboy)/system/)
obj/gameboy-scheduler.o: $(gameboy)/scheduler/scheduler.cpp $(call rwildcard,$(gameboy)/scheduler/)
obj/gameboy-cartridge.o: $(gameboy)/cartridge/cartridge.cpp $(call rwildcard,$(gameboy)/cartridge/)
obj/gameboy-memory.o: $(gameboy)/memory/memory.cpp $(call rwildcard,$(gameboy)/memory/)
obj/gameboy-cpu.o: $(gameboy)/cpu/cpu.cpp $(call rwildcard,$(gameboy)/cpu/)
obj/gameboy-apu.o: $(gameboy)/apu/apu.cpp $(call rwildcard,$(gameboy)/apu/)
obj/gameboy-lcd.o: $(gameboy)/lcd/lcd.cpp $(call rwildcard,$(gameboy)/lcd/)
obj/gameboy-cheat.o: $(gameboy)/cheat/cheat.cpp $(call rwildcard,$(gameboy)/cheat/)
obj/gameboy-video.o: $(gameboy)/video/video.cpp $(call rwildcard,$(gameboy)/video/)
