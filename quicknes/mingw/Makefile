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

CXXFLAGS_32 = -march=pentium4 -mtune=core2
CXXFLAGS_64 =
CXXFLAGS = -Wall -DDISABLE_AUTO_FILE -D__LIBRETRO__ -DNDEBUG -I.. -O3 -Wno-multichar -fno-exceptions -fomit-frame-pointer -flto $(CXXFLAGS_$(ARCH)) 
# TODO: include these as options in the Makefile
# -fprofile-generate
# -fprofile-use
TARGET = libquicknes.dll
LDFLAGS_32 = -static -static-libgcc -static-libstdc++
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

DEST_32 = ../../output/dll
DEST_64 = ../../output64/dll

SRCS = \
	../nes_emu/abstract_file.cpp \
	../nes_emu/apu_state.cpp \
	../nes_emu/Blip_Buffer.cpp \
	../nes_emu/Effects_Buffer.cpp \
	../nes_emu/Mapper_Fme7.cpp \
	../nes_emu/Mapper_Mmc5.cpp \
	../nes_emu/Mapper_Namco106.cpp \
	../nes_emu/Mapper_Vrc6.cpp \
	../nes_emu/misc_mappers.cpp \
	../nes_emu/Multi_Buffer.cpp \
	../nes_emu/Nes_Apu.cpp \
	../nes_emu/Nes_Buffer.cpp \
	../nes_emu/Nes_Cart.cpp \
	../nes_emu/Nes_Core.cpp \
	../nes_emu/Nes_Cpu.cpp \
	../nes_emu/nes_data.cpp \
	../nes_emu/Nes_Effects_Buffer.cpp \
	../nes_emu/Nes_Emu.cpp \
	../nes_emu/Nes_File.cpp \
	../nes_emu/Nes_Fme7_Apu.cpp \
	../nes_emu/Nes_Mapper.cpp \
	../nes_emu/nes_mappers.cpp \
	../nes_emu/Nes_Mmc1.cpp \
	../nes_emu/Nes_Mmc3.cpp \
	../nes_emu/Nes_Namco_Apu.cpp \
	../nes_emu/Nes_Oscs.cpp \
	../nes_emu/Nes_Ppu.cpp \
	../nes_emu/Nes_Ppu_Impl.cpp \
	../nes_emu/Nes_Ppu_Rendering.cpp \
	../nes_emu/Nes_State.cpp \
	../nes_emu/nes_util.cpp \
	../nes_emu/Nes_Vrc6_Apu.cpp \
	../nes_emu/Mmc24.cpp \
	../bizinterface.cpp \
	../fex/Data_Reader.cpp \
	../fex/blargg_errors.cpp \
	../fex/blargg_common.cpp

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
