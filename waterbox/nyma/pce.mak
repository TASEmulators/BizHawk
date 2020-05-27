include common.mak

# $(filter-out %CDAFReader_SF.cpp,$(call cppdir,cdrom))
# $(call cdir,tremor)
# $(call cdir,mpcdec)
# mednafen/src/mthreading/MThreading_POSIX.cpp

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,pce)) \
	$(call cppdir,hw_sound/pce_psg) \
	$(call cppdir,hw_misc/arcade_card) \
	$(call cppdir,hw_video/huc6270) \
	mednafen/src/cdrom/CDInterface.cpp \
	mednafen/src/cdrom/scsicd.cpp \
	mednafen/src/cdrom/CDUtility.cpp \
	mednafen/src/cdrom/lec.cpp \
	mednafen/src/cdrom/recover-raw.cpp \
	mednafen/src/cdrom/l-ec.cpp \
	mednafen/src/cdrom/crc32.cpp \
	mednafen/src/cdrom/galois.cpp \
	cdrom.cpp \
	pce.cpp

PER_FILE_FLAGS_mednafen/src/pce/input.cpp := -DINPUT_Read=ZZINPUT_Read

include ../common.mak
