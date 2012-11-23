# this was modified at one point so that it would compile in our bizhawk project, but isn't in
# use at the moment.  tread carefully

ifeq ($(platform),)
platform = unix
ifeq ($(shell uname -a),)
   platform = win
else ifneq ($(findstring MINGW,$(shell uname -a)),)
   platform = win
else ifneq ($(findstring Darwin,$(shell uname -a)),)
   platform = osx
else ifneq ($(findstring win,$(shell uname -a)),)
   platform = win
endif
endif

ifeq ($(platform), unix)
   TARGET := libmeteor.so
   fpic := -fPIC
   SHARED := -shared -Wl,--version-script=link.T -Wl,--no-undefined
else ifeq ($(platform), osx)
   TARGET := libmeteor.dylib
   fpic := -fPIC
   SHARED := -dynamiclib
else
   TARGET := libmeteor.dll
   CXX = g++
#   SHARED := -shared -static-libgcc -static-libstdc++ -s -Wl,--version-script=link.T -Wl,--no-undefined
   SHARED := -shared -static-libgcc -static-libstdc++ -s -Wl,--no-undefined
   CXXFLAGS += -DNO_MEMMEM
endif

#CXXFLAGS += -Wall -pedantic -I. -I../ameteor/include -pipe -D__LIBRETRO__ -Wno-parentheses -fno-exceptions -fno-rtti
CXXFLAGS += -Wall -pedantic -I. -Iinclude -pipe -D__LIBRETRO__ -DX86_ASM -Wno-parentheses -fno-exceptions -fno-rtti

ifeq ($(DEBUG), 1)
   CFLAGS += -O0 -g
   CXXFLAGS += -O0 -g
else
   CFLAGS += -O3
   CXXFLAGS += -O3
endif

#SRCDIR := ../ameteor/source
SRCDIR := ./source

SOURCES := \
	$(SRCDIR)/audio/dsound.cpp \
	$(SRCDIR)/audio/sound1.cpp \
	$(SRCDIR)/audio/sound2.cpp \
	$(SRCDIR)/audio/sound4.cpp \
	$(SRCDIR)/audio/speaker.cpp \
	$(SRCDIR)/disassembler/argimmediate.cpp \
	$(SRCDIR)/disassembler/argmulregisters.cpp \
	$(SRCDIR)/disassembler/argpsr.cpp \
	$(SRCDIR)/disassembler/argregister.cpp \
	$(SRCDIR)/disassembler/argrelative.cpp \
	$(SRCDIR)/disassembler/argshift.cpp \
	$(SRCDIR)/disassembler/arguimmediate.cpp \
	$(SRCDIR)/disassembler/arguments.cpp \
	$(SRCDIR)/disassembler/instruction.cpp \
	$(SRCDIR)/graphics/bglayer.cpp \
	$(SRCDIR)/graphics/object.cpp \
	$(SRCDIR)/graphics/objects.cpp \
	$(SRCDIR)/graphics/renderer.cpp \
	$(SRCDIR)/graphics/screen.cpp \
	$(SRCDIR)/ameteor.cpp \
	$(SRCDIR)/bios.cpp \
	$(SRCDIR)/clock.cpp \
	$(SRCDIR)/cpu.cpp \
	$(SRCDIR)/debug.cpp \
	$(SRCDIR)/dma.cpp \
	$(SRCDIR)/eeprom.cpp \
	$(SRCDIR)/flash.cpp \
	$(SRCDIR)/cartmem.cpp \
	$(SRCDIR)/interpreter.cpp \
	$(SRCDIR)/interpreter_arm.cpp \
	$(SRCDIR)/interpreter_thumb.cpp \
	$(SRCDIR)/io.cpp \
	$(SRCDIR)/keypad.cpp \
	$(SRCDIR)/lcd.cpp \
	$(SRCDIR)/memory.cpp \
	$(SRCDIR)/sound.cpp \
	$(SRCDIR)/sram.cpp \
	$(SRCDIR)/timer.cpp \
	cinterface.cpp
#	video.cpp \
#	audio.cpp \
#	input.cpp \
#	libretro.cpp

OBJ := $(SOURCES:.cpp=.o)

all: $(TARGET)

$(TARGET): $(OBJ)
	@$(CXX) -o $@ $^ $(SHARED) $(LDFLAGS) $(LIBS)
	@echo LD $(notdir $@)

%.o: %.cpp
	@$(CXX) -o $@ -c $< $(CXXFLAGS) $(fpic)
	@echo CXX $(notdir $<)

clean:
	rm -f $(TARGET)
	rm -f $(OBJ)

.PHONY: clean

