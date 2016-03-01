CXX = g++
RM = rm
CP = cp

MACHINE = $(shell $(CXX) -dumpmachine)
ifneq (,$(findstring i686,$(MACHINE)))
	ARCH = 32
else ifneq (,$(findstring x86_64,$(MACHINE)))
	ARCH = 64
else
	$(error Unknown arch)
endif

CXXFLAGS = -Wall -Iinclude -Isrc -O3 -std=c++11 -fno-exceptions -flto
TARGET = libgambatte.dll
LDFLAGS_32 = -static -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

DEST_32 = ../output/dll
DEST_64 = ../output64/dll

SRCS = \
	src/cinterface.cpp \
	src/cpu.cpp \
	src/gambatte.cpp \
	src/initstate.cpp \
	src/interrupter.cpp \
	src/interruptrequester.cpp \
	src/memory.cpp \
	src/mem/cartridge.cpp \
	src/mem/memptrs.cpp \
	src/mem/rtc.cpp \
	src/newstate.cpp \
	src/sound.cpp \
	src/sound/channel1.cpp \
	src/sound/channel2.cpp \
	src/sound/channel3.cpp \
	src/sound/channel4.cpp \
	src/sound/duty_unit.cpp \
	src/sound/envelope_unit.cpp \
	src/sound/length_counter.cpp \
	src/tima.cpp \
	src/video.cpp \
	src/video/lyc_irq.cpp \
	src/video/ly_counter.cpp \
	src/video/next_m0_time.cpp \
	src/video/ppu.cpp \
	src/video/sprite_mapper.cpp

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
