include common.mak

SRCS += \
	$(filter-out %ppu_mt.cpp %ppu_mtrender.cpp %msu1.cpp,$(call cppdir,snes_faust)) \
	mednafen/src/cheat_formats/snes.cpp \
	mednafen/src/SNSFLoader.cpp mednafen/src/PSFLoader.cpp mednafen/src/SPCReader.cpp \
	cdrom_dummy.cpp \
	faust.cpp

include ../common.mak
