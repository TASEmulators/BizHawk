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
CXXFLAGS = -Wall -I.. -I../core -O3 -Wfatal-errors -Werror -fomit-frame-pointer -flto $(CXXFLAGS_$(ARCH))

# TODO: include these as options in the Makefile
# -fprofile-generate
# -fprofile-use
TARGET = libquickernes.so
LDFLAGS_32 = -static -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

DEST_64 = ../../Assets/dll
DESTCOPY_64 = ../../output/dll

SRCS = \
	../core/apu/vrc7/emu2413_state.cpp \
	../core/apu/vrc7/emu2413.cpp  \
	../core/apu/vrc7/apu_vrc7.cpp \
	../core/apu/fme7/apu_fme7.cpp \
	../core/apu/namco/apu_namco.cpp \
	../core/apu/effectsBuffer.cpp \
	../core/apu/blipBuffer.cpp \
	../core/apu/NESEffectsBuffer.cpp \
	../core/apu/vrc6/apu_vrc6.cpp \
	../core/apu/oscs.cpp \
	../core/apu/apu.cpp \
	../core/apu/buffer.cpp \
	../core/apu/multiBuffer.cpp \
	../core/emu.cpp \
	../core/mappers/mapper.cpp \
	../core/cpu.cpp \
	../core/ppu/ppu.cpp \
	../core/ppu/ppuRendering.cpp \
	../core/ppu/ppuImpl.cpp \
	../bizinterface.cpp 

OBJS = $(SRCS:.cpp=.o)

all: $(TARGET)

%.o: %.cpp
	$(CXX) -c -o $@ $< $(CXXFLAGS)

$(TARGET) : $(OBJS)
	$(CXX) -o $@ $(LDFLAGS) $(OBJS)

clean:
	$(RM) $(OBJS)
	$(RM) $(TARGET)
	
install:
	$(CP) $(TARGET) $(DEST_$(ARCH))
ifneq ("$(wildcard $(DESTCOPY_$(ARCH)))", "")
	$(CP) $(TARGET) $(DESTCOPY_$(ARCH))
endif
