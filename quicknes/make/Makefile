CXX = g++
RM = rm
CP = cp

CXXFLAGS = -I../core/source/quickerNES/core/ -I../core/extern/jaffarCommon/include -Wall -Wfatal-errors -Werror \
	-std=c++20 -O3 -fomit-frame-pointer -flto -fvisibility=internal -fvisibility-inlines-hidden \
	-D_GNU_SOURCE -D__INLINE__=inline -D_QUICKERNES_DETECT_JOYPAD_READS -D_QUICKERNES_ENABLE_TRACEBACK_SUPPORT -D_QUICKERNES_SUPPORT_ARKANOID_INPUTS

# TODO: include these as options in the Makefile
# -fprofile-generate
# -fprofile-use

UNAME := $(shell uname)
ifeq ($(UNAME), Linux)
	TARGET = libquicknes.so
else
	TARGET = libquicknes.dll
endif

LDFLAGS = -shared -s $(CXXFLAGS)

DEST = ../../Assets/dll
DESTCOPY = ../../output/dll

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
	$(CP) $(TARGET) $(DEST)
ifneq ("$(wildcard $(DESTCOPY))", "")
	$(CP) $(TARGET) $(DESTCOPY)
endif
