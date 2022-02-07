CCFLAGS := -Wno-discarded-qualifiers

CXXFLAGS := -DMELONDS_VERSION="" \
	-I./melonDS/src -I./melonDS/src/teakra/include \
	-Wall -Wextra -Werror=int-to-pointer-cast \
	-Wfatal-errors -Wno-unused-parameter -Wno-unused-variable \
	-Wno-unused-but-set-variable -Wno-sign-compare \
	-fno-strict-aliasing -std=c++17

TARGET = melonDS.wbx

CORE_SRCS = \
	AREngine.cpp \
	ARM.cpp \
	ARMInterpreter.cpp \
	ARMInterpreter_ALU.cpp \
	ARMInterpreter_Branch.cpp \
	ARMInterpreter_LoadStore.cpp \
	Config.cpp \
	CP15.cpp \
	CRC32.cpp \
	DMA.cpp \
	DSi.cpp \
	DSi_AES.cpp \
	DSi_Camera.cpp \
	DSi_DSP.cpp \
	DSi_I2C.cpp \
	DSi_NAND.cpp \
	DSi_NDMA.cpp \
	DSi_NWifi.cpp \
	DSi_SD.cpp \
	DSi_SPI_TSC.cpp \
	FATStorage.cpp \
	GBACart.cpp \
	GPU.cpp \
	GPU2D.cpp \
	GPU2D_Soft.cpp \
	GPU3D.cpp \
	GPU3D_Soft.cpp \
	NDS.cpp \
	NDSCart.cpp \
	NDSCart_SRAMManager.cpp \
	RTC.cpp \
	Savestate.cpp \
	SPI.cpp \
	SPU.cpp \
	Wifi.cpp \
	WifiAP.cpp

TEAKRA_SRCS = \
	ahbm.cpp \
	apbp.cpp \
	btdmp.cpp \
	dma.cpp \
	memory_interface.cpp \
	mmio.cpp \
	processor.cpp \
	teakra.cpp \
	timer.cpp

FATFS_SRCS = \
	diskio.c \
	ff.c \
	ffsystem.c \
	ffunicode.c

MISC_SRCS = \
	sha1/sha1.c \
	tiny-AES-c/aes.c \
	xxhash/xxhash.c

SRCS = \
	$(addprefix melonDS/src/,$(CORE_SRCS)) \
	$(addprefix melonDS/src/teakra/src/,$(TEAKRA_SRCS)) \
	$(addprefix melonDS/src/fatfs/,$(FATFS_SRCS)) \
	$(addprefix melonDS/src/,$(MISC_SRCS)) \
	BizConfig.cpp \
	BizInterface.cpp \
	BizPlatform.cpp

include ../common.mak
