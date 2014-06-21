snes_objects := snes-interface snes-system snes-controller
snes_objects += snes-cartridge snes-cheat
snes_objects += snes-memory snes-cpucore snes-smpcore
snes_objects += snes-cpu snes-smp snes-dsp snes-ppu
snes_objects += snes-icd2 snes-nss snes-superfx snes-sa1
snes_objects += snes-necdsp snes-hitachidsp snes-armdsp
snes_objects += snes-bsx snes-srtc snes-sdd1 snes-spc7110
snes_objects += snes-obc1 snes-sufamiturbo
snes_objects += snes-msu1 snes-link
objects += $(snes_objects)

ifeq ($(profile),accuracy)
  flags += -DPROFILE_ACCURACY
  snescpu := $(snes)/cpu
  snessmp := $(snes)/smp
  snesdsp := $(snes)/dsp
  snesppu := $(snes)/ppu
else ifeq ($(profile),compatibility)
  flags += -DPROFILE_COMPATIBILITY
  snescpu := $(snes)/cpu
  snessmp := $(snes)/smp
  snesdsp := $(snes)/alt/dsp
  snesppu := $(snes)/alt/ppu-compatibility
else ifeq ($(profile),performance)
  flags += -DPROFILE_PERFORMANCE
  snescpu := $(snes)/alt/cpu
  snessmp := $(snes)/alt/smp
  snesdsp := $(snes)/alt/dsp
  snesppu := $(snes)/alt/ppu-performance
endif

obj/snes-interface.o : $(snes)/interface/interface.cpp $(call rwildcard,$(snes)/interface)
obj/snes-system.o    : $(snes)/system/system.cpp $(call rwildcard,$(snes)/system/)
obj/snes-controller.o: $(snes)/controller/controller.cpp $(call rwildcard,$(snes)/controller/)
obj/snes-memory.o    : $(snes)/memory/memory.cpp $(call rwildcard,$(snes)/memory/)
obj/snes-cpucore.o   : $(snes)/cpu/core/core.cpp $(call rwildcard,$(snes)/cpu/core/)
obj/snes-smpcore.o   : $(snes)/smp/core/core.cpp $(call rwildcard,$(snes)/smp/core/)
obj/snes-cpu.o       : $(snescpu)/cpu.cpp $(call rwildcard,$(snescpu)/)
obj/snes-smp.o       : $(snessmp)/smp.cpp $(call rwildcard,$(snessmp)/)
obj/snes-dsp.o       : $(snesdsp)/dsp.cpp $(call rwildcard,$(snesdsp)/)
obj/snes-ppu.o       : $(snesppu)/ppu.cpp $(call rwildcard,$(snesppu)/)
obj/snes-cartridge.o : $(snes)/cartridge/cartridge.cpp $(snes)/cartridge/*
obj/snes-cheat.o     : $(snes)/cheat/cheat.cpp $(snes)/cheat/*

obj/snes-nss.o        : $(snes)/chip/nss/nss.cpp $(call rwildcard,$(snes)/chip/nss/)
obj/snes-icd2.o       : $(snes)/chip/icd2/icd2.cpp $(call rwildcard,$(snes)/chip/icd2/)
obj/snes-superfx.o    : $(snes)/chip/superfx/superfx.cpp $(call rwildcard,$(snes)/chip/superfx/)
obj/snes-sa1.o        : $(snes)/chip/sa1/sa1.cpp $(call rwildcard,$(snes)/chip/sa1/)
obj/snes-necdsp.o     : $(snes)/chip/necdsp/necdsp.cpp $(call rwildcard,$(snes)/chip/necdsp/)
obj/snes-hitachidsp.o : $(snes)/chip/hitachidsp/hitachidsp.cpp $(call rwildcard,$(snes)/chip/hitachidsp/)
obj/snes-armdsp.o     : $(snes)/chip/armdsp/armdsp.cpp $(call rwildcard,$(snes)/chip/armdsp/)
obj/snes-bsx.o        : $(snes)/chip/bsx/bsx.cpp $(call rwildcard,$(snes)/chip/bsx/)
obj/snes-srtc.o       : $(snes)/chip/srtc/srtc.cpp $(snes)/chip/srtc/*
obj/snes-sdd1.o       : $(snes)/chip/sdd1/sdd1.cpp $(snes)/chip/sdd1/*
obj/snes-spc7110.o    : $(snes)/chip/spc7110/spc7110.cpp $(snes)/chip/spc7110/*
obj/snes-obc1.o       : $(snes)/chip/obc1/obc1.cpp $(snes)/chip/obc1/*
obj/snes-sufamiturbo.o: $(snes)/chip/sufamiturbo/sufamiturbo.cpp $(snes)/chip/sufamiturbo/*
obj/snes-msu1.o       : $(snes)/chip/msu1/msu1.cpp $(snes)/chip/msu1/*
obj/snes-link.o       : $(snes)/chip/link/link.cpp $(snes)/chip/link/*
