include common.mak

SRCS += \
	$(filter-out %debug.cpp,$(call cppdir,psx)) \
	mednafen/src/cheat_formats/psx.cpp \
	mednafen/src/resampler/resample.c \
	$(CD_SRCS) \
	mednafen/src/PSFLoader.cpp \
	shock.cpp

include ../common.mak
