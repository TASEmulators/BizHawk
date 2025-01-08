CCFLAGS := -Wno-incompatible-pointer-types-discards-qualifiers -Wno-pointer-sign

CXXFLAGS := -DOGLRENDERER_ENABLED \
	-I. -I./melonDS/src -I./melonDS/src/teakra/include \
	-Wall -Wextra -Werror=int-to-pointer-cast \
	-Wfatal-errors -Wno-unused-parameter -Wno-unused-variable \
	-Wno-unused-but-set-variable -Wno-unused-function \
	-Wno-sign-compare -Wno-deprecated-declarations \
	-Wno-missing-braces -Wno-bitwise-instead-of-logical \
	-Wno-unused-private-field -Wno-logical-op-parentheses \
	-Wno-mismatched-tags -Wno-reorder-ctor \
	-fno-strict-aliasing -fwrapv -std=c++17

TARGET = melonDS.wbx

CORE_SRCS = \
	ARCodeFile.cpp \
	AREngine.cpp \
	ARM.cpp \
	ARMInterpreter.cpp \
	ARMInterpreter_ALU.cpp \
	ARMInterpreter_Branch.cpp \
	ARMInterpreter_LoadStore.cpp \
	CP15.cpp \
	CRC32.cpp \
	DMA.cpp \
	DMA_Timings.cpp \
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
	FATIO.cpp \
	FATStorage.cpp \
	FreeBIOS.cpp \
	GBACart.cpp \
	GPU.cpp \
	GPU2D.cpp \
	GPU2D_Soft.cpp \
	GPU3D.cpp \
	GPU3D_Soft.cpp \
	GPU3D_Texcache.cpp \
	NDS.cpp \
	NDSCart.cpp \
	NDSCartR4.cpp \
	ROMList.cpp \
	RTC.cpp \
	Savestate.cpp \
	SPI.cpp \
	SPI_Firmware.cpp \
	SPU.cpp \
	Utils.cpp \
	Wifi.cpp \
	WifiAP.cpp

CORE_GL_SRCS = \
	GPU_OpenGL.cpp \
	GPU3D_OpenGL.cpp \
	GPU3D_Compute.cpp \
	GPU3D_TexcacheOpenGL.cpp \
	OpenGLSupport.cpp \
	frontend/Util_Video.cpp

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
	ff.c \
	ffsystem.c \
	ffunicode.c

MISC_SRCS = \
	InvisiblePoolAllocator.cpp \
	sha1/sha1.c \
	tiny-AES-c/aes.c \
	xxhash/xxhash.c \
	blip_buf/blip_buf.c \

BIZPLATFORM_SRCS = \
	BizPlatform/BizFile.cpp \
	BizPlatform/BizLog.cpp \
	BizPlatform/BizOGL.cpp \
	BizPlatform/BizPlatformStubs.cpp \
	BizPlatform/BizSaveManager.cpp \
	BizPlatform/BizThread.cpp \

SRCS = \
	$(addprefix melonDS/src/,$(CORE_SRCS)) \
	$(addprefix melonDS/src/,$(CORE_GL_SRCS)) \
	$(addprefix melonDS/src/teakra/src/,$(TEAKRA_SRCS)) \
	$(addprefix melonDS/src/fatfs/,$(FATFS_SRCS)) \
	$(addprefix melonDS/src/,$(MISC_SRCS)) \
	$(BIZPLATFORM_SRCS) \
	BizConsoleCreator.cpp \
	BizDebugging.cpp \
	BizGLPresenter.cpp \
	BizInterface.cpp \
	dthumb.c

include ../common.mak
