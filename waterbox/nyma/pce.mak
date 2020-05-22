include common.mak

# $(call cppdir,hw_video/huc6270)
# $(call cppdir,hw_sound/pce_psg)

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,pce)) \
	$(filter-out %CDAFReader_SF.cpp,$(call cppdir,cdrom)) \
	$(call cdir,tremor) \
	$(call cdir,mpcdec) \
	mednafen/src/mthreading/MThreading_POSIX.cpp \
	$(call cppdir,hw_sound/pce_psg) \
	$(call cppdir,hw_misc/arcade_card) \
	$(call cppdir,hw_video/huc6270) \
	pce.cpp

include ../common.mak
