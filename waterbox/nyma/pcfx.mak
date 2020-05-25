include common.mak

# $(filter-out %CDAFReader_SF.cpp,$(call cppdir,cdrom))
# $(call cdir,tremor)
# $(call cdir,mpcdec)
# mednafen/src/mthreading/MThreading_POSIX.cpp

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,pcfx)) \
	$(call cppdir,hw_cpu/v810) \
	$(call cppdir,hw_video/huc6270) \
	$(call cppdir,hw_sound/pce_psg)

include ../common.mak
