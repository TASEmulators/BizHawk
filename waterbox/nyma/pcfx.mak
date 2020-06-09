include common.mak

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,pcfx)) \
	$(call cppdir,hw_cpu/v810) \
	$(call cppdir,hw_video/huc6270) \
	$(call cppdir,hw_sound/pce_psg) \
	$(CD_SRCS) \
	pcfx.cpp

include ../common.mak
