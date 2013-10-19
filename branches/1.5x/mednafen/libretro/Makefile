DEBUG = 0
FRONTEND_SUPPORTS_RGB565 = 1

PREV_ROOT_DIR := ../
MEDNAFEN_DIR := ../src

ifeq ($(platform),)
platform = unix
ifeq ($(shell uname -a),)
   platform = win
else ifneq ($(findstring Darwin,$(shell uname -a)),)
   platform = osx
else ifneq ($(findstring MINGW,$(shell uname -a)),)
   platform = win
endif
endif

# If you have a system with 1GB RAM or more - cache the whole 
# CD in order to prevent file access delays/hiccups
CACHE_CD = 0

#if no core specified, just pick psx for now
ifeq ($(core),)
   core = psx
endif

NEED_MINIFEN = 1

ifeq ($(core), psx)
   core = psx
   PTHREAD_FLAGS = -pthread
   NEED_CD = 1
   NEED_THREADING = 1
   NEED_BPP = 32
   NEED_BLIP = 1
   NEED_DEINTERLACER = 1
   CORE_DEFINE := -DWANT_PSX_EMU
   CORE_DIR := $(MEDNAFEN_DIR)/psx
   CORE_SOURCES := $(CORE_DIR)/psx.cpp \
	$(CORE_DIR)/irq.cpp \
	$(CORE_DIR)/timer.cpp \
	$(CORE_DIR)/dma.cpp \
	$(CORE_DIR)/frontio.cpp \
	$(CORE_DIR)/sio.cpp \
	$(CORE_DIR)/cpu.cpp \
	$(CORE_DIR)/gte.cpp \
	$(CORE_DIR)/dis.cpp \
	$(CORE_DIR)/cdc.cpp \
	$(CORE_DIR)/spu.cpp \
	$(CORE_DIR)/gpu.cpp \
	$(CORE_DIR)/mdec.cpp \
	$(CORE_DIR)/input/gamepad.cpp \
	$(CORE_DIR)/input/dualanalog.cpp \
	$(CORE_DIR)/input/dualshock.cpp \
	$(CORE_DIR)/input/justifier.cpp \
	$(CORE_DIR)/input/guncon.cpp \
	$(CORE_DIR)/input/negcon.cpp \
	$(CORE_DIR)/input/memcard.cpp \
	$(CORE_DIR)/input/multitap.cpp \
	$(CORE_DIR)/input/mouse.cpp
TARGET_NAME := mednafen_psx_libretro
else ifeq ($(core), pce-fast)
   core = pce_fast
   PTHREAD_FLAGS = -pthread
   NEED_BPP = 16
   NEED_BLIP = 1
   NEED_CD = 1
   NEED_CRC32 = 1
   NEED_THREADING = 1
   CORE_DEFINE := -DWANT_PCE_FAST_EMU
   CORE_DIR := $(MEDNAFEN_DIR)/pce_fast-0924

CORE_SOURCES := $(CORE_DIR)/huc.cpp \
	$(CORE_DIR)/pce_huc6280.cpp \
	$(CORE_DIR)/input.cpp \
	$(CORE_DIR)/pce.cpp \
	$(CORE_DIR)/tsushin.cpp \
	$(CORE_DIR)/input/gamepad.cpp \
	$(CORE_DIR)/input/mouse.cpp \
	$(CORE_DIR)/input/tsushinkb.cpp \
	$(CORE_DIR)/vdc.cpp
TARGET_NAME := mednafen_pce_fast_libretro

HW_CPU_SOURCES += $(MEDNAFEN_DIR)/hw_cpu/huc6280/huc6280.cpp
HW_MISC_SOURCES += $(MEDNAFEN_DIR)/hw_misc/arcade_card/arcade_card.cpp
HW_SOUND_SOURCES += $(MEDNAFEN_DIR)/hw_sound/pce_psg/pce_psg.cpp
HW_VIDEO_SOURCES += $(MEDNAFEN_DIR)/hw_video/huc6270/vdc.cpp
CDROM_SOURCES += $(MEDNAFEN_DIR)/cdrom/pcecd.cpp
else ifeq ($(core), wswan)
   core = wswan
   NEED_BPP = 16
   NEED_BLIP = 1
   CORE_DEFINE := -DWANT_WSWAN_EMU
   CORE_DIR := $(MEDNAFEN_DIR)/wswan

CORE_SOURCES := $(CORE_DIR)/gfx.cpp \
	$(CORE_DIR)/main.cpp \
	$(CORE_DIR)/memory.cpp \
	$(CORE_DIR)/v30mz.cpp \
	$(CORE_DIR)/sound.cpp \
	$(CORE_DIR)/tcache.cpp \
	$(CORE_DIR)/interrupt.cpp \
	$(CORE_DIR)/eeprom.cpp \
	$(CORE_DIR)/rtc.cpp
TARGET_NAME := mednafen_wswan_libretro
endif

ifeq ($(NEED_RESAMPLER), 1)
FLAGS += -DNEED_RESAMPLER
RESAMPLER_SOURCES += $(MEDNAFEN_DIR)/sound/Fir_Resampler.cpp
ifeq ($(NEED_BLIP), 1)
RESAMPLER_SOURCES += $(MEDNAFEN_DIR)/sound/Blip_Buffer_orig.cpp
endif
else
ifeq ($(NEED_BLIP), 1)
RESAMPLER_SOURCES += $(MEDNAFEN_DIR)/sound/Blip_Buffer.cpp
endif
endif

CORE_INCDIR := -I$(CORE_DIR)

ifeq ($(platform), unix)
   TARGET := $(TARGET_NAME).so
   fpic := -fPIC
   SHARED := -shared -Wl,--no-undefined -Wl,--version-script=link.T
   ENDIANNESS_DEFINES := -DLSB_FIRST
   ifneq ($(shell uname -p | grep -E '((i.|x)86|amd64)'),)
      IS_X86 = 1
   endif
   LDFLAGS += $(PTHREAD_FLAGS)
   FLAGS += $(PTHREAD_FLAGS)
else ifeq ($(platform), osx)
   TARGET := $(TARGET_NAME).dylib
   fpic := -fPIC
   SHARED := -dynamiclib
   ENDIANNESS_DEFINES := -DLSB_FIRST
   LDFLAGS += $(PTHREAD_FLAGS)
   FLAGS += $(PTHREAD_FLAGS)
else ifeq ($(platform), ps3)
   TARGET := $(TARGET_NAME)_ps3.a
   CC = $(CELL_SDK)/host-win32/ppu/bin/ppu-lv2-gcc.exe
   CXX = $(CELL_SDK)/host-win32/ppu/bin/ppu-lv2-g++.exe
   AR = $(CELL_SDK)/host-win32/ppu/bin/ppu-lv2-ar.exe
   ENDIANNESS_DEFINES := -DMSB_FIRST -DBYTE_ORDER=BIG_ENDIAN
else ifeq ($(platform), sncps3)
   TARGET := $(TARGET_NAME)_ps3.a
   CC = $(CELL_SDK)/host-win32/sn/bin/ps3ppusnc.exe
   CXX = $(CELL_SDK)/host-win32/sn/bin/ps3ppusnc.exe
   AR = $(CELL_SDK)/host-win32/sn/bin/ps3snarl.exe
   ENDIANNESS_DEFINES := -DMSB_FIRST -DBYTE_ORDER=BIG_ENDIAN

   CXXFLAGS += -Xc+=exceptions
else ifeq ($(platform), psl1ght)
   TARGET := $(TARGET_NAME)_psl1ght.a
   CC = $(PS3DEV)/ppu/bin/ppu-gcc$(EXE_EXT)
   CC = $(PS3DEV)/ppu/bin/ppu-g++$(EXE_EXT)
   AR = $(PS3DEV)/ppu/bin/ppu-ar$(EXE_EXT)
   ENDIANNESS_DEFINES := -DMSB_FIRST -DBYTE_ORDER=BIG_ENDIAN
else ifeq ($(platform), xenon)
   TARGET := $(TARGET_NAME)_xenon360.a
   CC = xenon-gcc$(EXE_EXT)
   CXX = xenon-g++$(EXE_EXT)
   AR = xenon-ar$(EXE_EXT)
   ENDIANNESS_DEFINES += -D__LIBXENON__ -m32 -D__ppc__ -DMSB_FIRST -DBYTE_ORDER=BIG_ENDIAN
   LIBS := $(PTHREAD_FLAGS)
else ifeq ($(platform), ngc)
   TARGET := $(TARGET_NAME)_ngc.a
   CC = $(DEVKITPPC)/bin/powerpc-eabi-gcc$(EXE_EXT)
   CXX = $(DEVKITPPC)/bin/powerpc-eabi-g++$(EXE_EXT)
   AR = $(DEVKITPPC)/bin/powerpc-eabi-ar$(EXE_EXT)
   ENDIANNESS_DEFINES += -DGEKKO -DHW_DOL -mrvl -mcpu=750 -meabi -mhard-float -DMSB_FIRST -DBYTE_ORDER=BIG_ENDIAN

   EXTRA_INCLUDES := -I$(DEVKITPRO)/libogc/include

else ifeq ($(platform), wii)
   TARGET := $(TARGET_NAME)_wii.a
   CC = $(DEVKITPPC)/bin/powerpc-eabi-gcc$(EXE_EXT)
   CXX = $(DEVKITPPC)/bin/powerpc-eabi-g++$(EXE_EXT)
   AR = $(DEVKITPPC)/bin/powerpc-eabi-ar$(EXE_EXT)
   ENDIANNESS_DEFINES += -DGEKKO -DHW_RVL -mrvl -mcpu=750 -meabi -mhard-float -DMSB_FIRST -DBYTE_ORDER=BIG_ENDIAN

   EXTRA_INCLUDES := -I$(DEVKITPRO)/libogc/include
else
   TARGET := retro.dll
   CC = i686-pc-mingw32-gcc
   CXX = i686-pc-mingw32-g++
   SHARED := -shared -Wl,--no-undefined -Wl,--version-script=link.T
   LDFLAGS += -static-libgcc -static-libstdc++ -lwinmm
   ENDIANNESS_DEFINES := -DLSB_FIRST
endif

ifeq ($(NEED_THREADING), 1)
THREAD_STUBS += thread.cpp
endif

ifeq ($(NEED_CRC32), 1)
FLAGS += -DHAVE_CRC32
endif

ifeq ($(NEED_DEINTERLACER), 1)
FLAGS += -DNEED_DEINTERLACER
endif

ifeq ($(NEED_CD), 1)
CDROM_SOURCES += $(MEDNAFEN_DIR)/cdrom/CDAccess.cpp \
	$(MEDNAFEN_DIR)/cdrom/CDAccess_Image.cpp \
	$(MEDNAFEN_DIR)/cdrom/CDUtility.cpp \
	$(MEDNAFEN_DIR)/cdrom/lec.cpp \
	$(MEDNAFEN_DIR)/cdrom/SimpleFIFO.cpp \
	$(MEDNAFEN_DIR)/cdrom/audioreader.cpp \
	$(MEDNAFEN_DIR)/cdrom/galois.cpp \
	$(MEDNAFEN_DIR)/cdrom/pcecd.cpp \
	$(MEDNAFEN_DIR)/cdrom/scsicd.cpp \
	$(MEDNAFEN_DIR)/cdrom/recover-raw.cpp \
	$(MEDNAFEN_DIR)/cdrom/l-ec.cpp \
	$(MEDNAFEN_DIR)/cdrom/cdromif.cpp \
	$(MEDNAFEN_DIR)/cdrom/cd_crc32.cpp

MPC_SRC := $(wildcard $(MEDNAFEN_DIR)/mpcdec/*.c)
TREMOR_SRC := $(wildcard $(MEDNAFEN_DIR)/tremor/*.c)
FLAGS += -DNEED_CD
endif

ifeq ($(NEED_MINIFEN), 1)
MINIFEN_SOURCES += minifen/minifen.cpp minifen/minifen_state.cpp
FLAGS += -DNEED_MINIFEN
else
MINIFEN_SOURCES += $(MEDNAFEN_DIR)/mednafen.cpp $(MEDNAFEN_DIR)/state.cpp
endif


MEDNAFEN_SOURCES := $(MEDNAFEN_DIR)/error.cpp \
	$(MEDNAFEN_DIR)/math_ops.cpp \
	$(MEDNAFEN_DIR)/settings.cpp \
	$(MEDNAFEN_DIR)/general.cpp \
	$(MEDNAFEN_DIR)/FileWrapper.cpp \
	$(MEDNAFEN_DIR)/FileStream.cpp \
	$(MEDNAFEN_DIR)/MemoryStream.cpp \
	$(MEDNAFEN_DIR)/Stream.cpp \
	$(MEDNAFEN_DIR)/endian.cpp \
	$(CDROM_SOURCES) \
	$(MEDNAFEN_DIR)/mempatcher.cpp \
	$(MEDNAFEN_DIR)/video/video.cpp \
	$(MEDNAFEN_DIR)/video/Deinterlacer.cpp \
	$(MEDNAFEN_DIR)/video/surface.cpp \
	$(RESAMPLER_SOURCES) \
	$(MEDNAFEN_DIR)/sound/Stereo_Buffer.cpp \
	$(MEDNAFEN_DIR)/file.cpp \
	$(MEDNAFEN_DIR)/okiadpcm.cpp \
	$(MEDNAFEN_DIR)/md5.cpp


LIBRETRO_SOURCES := libretro.cpp stubs.cpp $(THREAD_STUBS)

SOURCES_C := $(MEDNAFEN_DIR)/trio/trio.c \
	$(MPC_SRC) \
	$(TREMOR_SRC) \
	$(LIBRETRO_SOURCES_C) \
	$(MEDNAFEN_DIR)/trio/trionan.c \
	$(MEDNAFEN_DIR)/trio/triostr.c

SOURCES := $(MINIFEN_SOURCES) $(LIBRETRO_SOURCES) $(CORE_SOURCES) $(MEDNAFEN_SOURCES) $(HW_CPU_SOURCES) $(HW_MISC_SOURCES) $(HW_SOUND_SOURCES) $(HW_VIDEO_SOURCES)


OBJECTS := $(SOURCES:.cpp=.o) $(SOURCES_C:.c=.o)

all: $(TARGET)

ifeq ($(DEBUG),0)
   FLAGS += -O3 -ffast-math -funroll-loops 
else
   FLAGS += -O0 -g
endif

LDFLAGS += $(fpic) $(SHARED)
FLAGS += -Wall $(fpic) -fno-strict-overflow
FLAGS += -I. -I$(PREV_ROOT_DIR)/include -I$(PREV_ROOT_DIR)/intl -I$(CORE_INCDIR)  -I$(MEDNAFEN_DIR)

WARNINGS := -Wall \
	-Wno-narrowing \
	-Wno-unused-but-set-variable \
	-Wno-sign-compare \
	-Wno-unused-variable \
	-Wno-unused-function \
	-Wno-uninitialized \
	-Wno-unused-result \
	-Wno-strict-aliasing \
	-Wno-overflow

FLAGS += $(ENDIANNESS_DEFINES) -DSIZEOF_DOUBLE=8 $(WARNINGS) \
			-DMEDNAFEN_VERSION=\"0.9.26\" -DPACKAGE=\"mednafen\" -DMEDNAFEN_VERSION_NUMERIC=926 -DPSS_STYLE=1 -DMPC_FIXED_POINT -DARCH_X86 $(CORE_DEFINE) -DSTDC_HEADERS -D__STDC_LIMIT_MACROS -D__LIBRETRO__ -DNDEBUG -DHAVE_MKDIR $(EXTRA_INCLUDES)

ifeq ($(CACHE_CD), 1)
FLAGS += -D__LIBRETRO_CACHE_CD__
endif

ifeq ($(NEED_BPP), 16)
FLAGS += -DWANT_16BPP
endif

ifeq ($(FRONTEND_SUPPORTS_RGB565), 1)
FLAGS += -DFRONTEND_SUPPORTS_RGB565
endif

ifeq ($(NEED_BPP), 32)
FLAGS += -DWANT_32BPP
endif


CXXFLAGS += $(FLAGS)
CFLAGS += $(FLAGS) -std=gnu99

$(TARGET): $(OBJECTS)
	$(CXX) -o $@ $^ $(LDFLAGS)

%.o: %.cpp
	$(CXX) -c -o $@ $< $(CXXFLAGS)

%.o: %.c
	$(CC) -c -o $@ $< $(CFLAGS)

clean:
	rm -f $(TARGET) $(OBJECTS)

.PHONY: clean
