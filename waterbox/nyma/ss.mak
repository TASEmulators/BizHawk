include common.mak

SRCS += \
	$(filter-out %gen_dsp.cpp,$(call cppdir,ss)) \
	$(filter-out %gen.cpp,$(call cppdir,hw_cpu/m68k)) \
	mednafen/src/resampler/resample.c \
	$(CD_SRCS) \
	mednafen/src/PSFLoader.cpp mednafen/src/SSFLoader.cpp \
	ss.cpp

include ../common.mak
