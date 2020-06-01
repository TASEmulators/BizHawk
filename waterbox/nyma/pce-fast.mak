include common.mak

SRCS += \
	$(call cppdir,pce_fast) \
	$(call cppdir,hw_misc/arcade_card) \
	$(CD_SRCS) \
	pce-fast.cpp

PER_FILE_FLAGS_mednafen/src/pce_fast/input.cpp := -DINPUT_Read=ZZINPUT_Read

include ../common.mak
