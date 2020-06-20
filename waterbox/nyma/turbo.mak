include common.mak

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,pce)) \
	$(call cppdir,hw_sound/pce_psg) \
	$(call cppdir,hw_misc/arcade_card) \
	$(call cppdir,hw_video/huc6270) \
	$(CD_SRCS) \
	pce.cpp

PER_FILE_FLAGS_mednafen/src/pce/input.cpp := -DINPUT_Read=ZZINPUT_Read

include ../common.mak
