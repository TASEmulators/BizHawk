include common.mak

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,pcfx)) \
	$(call cppdir,hw_cpu/v810) \
	$(filter-out %CDAFReader_SF.cpp,$(call cppdir,cdrom)) \
	$(call cppdir,hw_video/huc6270) \
	$(call cppdir,hw_sound/pce_psg) \
	$(call cdir,tremor) \
	$(call cdir,mpcdec) \
	mednafen/src/mthreading/MThreading_POSIX.cpp

include ../common.mak
