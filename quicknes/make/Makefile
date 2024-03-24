CXX = g++
RM = rm
CP = cp

MACHINE = $(shell $(CXX) -dumpmachine)
ifneq (,$(findstring i686,$(MACHINE)))
	$(error 32 bit build no longer supported)
else ifneq (,$(findstring x86_64,$(MACHINE)))
	ARCH = 64
else
	$(error Unknown arch)
endif

CXXFLAGS_32 = -march=pentium4 -mtune=core2
CXXFLAGS_64 =
CXXFLAGS = -Wall -I../core/source/quickerNES/core/ -I../core/extern/jaffarCommon/include -D__INLINE__=inline -O3 -Wfatal-errors -Werror -fomit-frame-pointer -flto -D_GNU_SOURCE -D_QUICKERNES_DETECT_JOYPAD_READS -D_QUICKERNES_ENABLE_TRACEBACK_SUPPORT $(CXXFLAGS_$(ARCH))

# TODO: include these as options in the Makefile
# -fprofile-generate
# -fprofile-use

UNAME := $(shell uname)
ifeq ($(UNAME), Linux)
	TARGET = libquicknes.so
else
	TARGET = libquicknes.dll
endif

LDFLAGS_32 = -static -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

DEST_64 = ../../Assets/dll
DESTCOPY_64 = ../../output/dll

SRCS = \
	../core/source/quickerNES/core/apu/vrc7/emu2413_state.cpp \
	../core/source/quickerNES/core/apu/vrc7/emu2413.cpp  \
	../core/source/quickerNES/core/apu/vrc7/apu_vrc7.cpp \
	../core/source/quickerNES/core/apu/fme7/apu_fme7.cpp \
	../core/source/quickerNES/core/apu/namco/apu_namco.cpp \
	../core/source/quickerNES/core/apu/effectsBuffer.cpp \
	../core/source/quickerNES/core/apu/blipBuffer.cpp \
	../core/source/quickerNES/core/apu/NESEffectsBuffer.cpp \
	../core/source/quickerNES/core/apu/vrc6/apu_vrc6.cpp \
	../core/source/quickerNES/core/apu/oscs.cpp \
	../core/source/quickerNES/core/apu/apu.cpp \
	../core/source/quickerNES/core/apu/buffer.cpp \
	../core/source/quickerNES/core/apu/multiBuffer.cpp \
	../core/source/quickerNES/core/emu.cpp \
	../core/source/quickerNES/core/mappers/mapper.cpp \
	../core/source/quickerNES/core/cpu.cpp \
	../core/source/quickerNES/core/ppu/ppu.cpp \
	../core/source/quickerNES/core/ppu/ppuRendering.cpp \
	../core/source/quickerNES/core/ppu/ppuImpl.cpp \
	../bizinterface.cpp 

OBJS = $(SRCS:.cpp=.o)

all: $(TARGET)

%.o: %.cpp
	$(CXX) -c -o $@ $< $(CXXFLAGS)

$(TARGET) : $(OBJS)
	$(CXX) -o $@ $(LDFLAGS) $(OBJS)

clean:
	$(RM) -f $(OBJS)
	$(RM) -f $(TARGET)
	
install:
	$(CP) $(TARGET) $(DEST_$(ARCH))
ifneq ("$(wildcard $(DESTCOPY_$(ARCH)))", "")
	$(CP) $(TARGET) $(DESTCOPY_$(ARCH))
endif
