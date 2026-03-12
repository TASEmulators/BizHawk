CCFLAGS := -Wno-incompatible-pointer-types-discards-qualifiers \
	-Wno-pointer-sign -Wno-unused-function -Wno-unused-variable

CXXFLAGS := -DOGLRENDERER_ENABLED -DJIT_ENABLED -DWATERBOX \
	-DMELONDS_GL_HEADER=\"BizPlatform/BizOGL.h\" \
	-I. -I./melonDS/src -I./melonDS/src/teakra/include \
	-I./BizPlatform/faad2/include \
	-Wall -Wextra -Werror=int-to-pointer-cast \
	-Wfatal-errors -Wno-unused-parameter -Wno-unused-variable \
	-Wno-unused-but-set-variable -Wno-unused-function \
	-Wno-sign-compare -Wno-deprecated-declarations \
	-Wno-missing-braces -Wno-bitwise-instead-of-logical \
	-Wno-unused-private-field -Wno-logical-op-parentheses \
	-Wno-mismatched-tags -Wno-reorder-ctor -Wno-invalid-offsetof \
	-Wno-bitwise-op-parentheses -Wno-vla-cxx-extension \
	-fno-strict-aliasing -fwrapv -std=c++17

TARGET = melonDS.wbx

CORE_SRCS = \
	ARCodeFile.cpp \
	ARDatabaseDAT.cpp \
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
	DSi_I2S.cpp \
	DSi_NAND.cpp \
	DSi_NDMA.cpp \
	DSi_NWifi.cpp \
	DSi_SD.cpp \
	DSi_SPI_TSC.cpp \
	FATIO.cpp \
	FATStorage.cpp \
	FreeBIOS.cpp \
	GBACart.cpp \
	GBACartMotionPak.cpp \
	GPU.cpp \
	GPU2D.cpp \
	GPU2D_Soft.cpp \
	GPU3D.cpp \
	GPU3D_Soft.cpp \
	GPU3D_Texcache.cpp \
	Mic.cpp \
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
	frontend/ScreenLayout.cpp

CORE_JIT_SRCS = \
	ARM_InstrInfo.cpp \
	ARMJIT.cpp \
	ARMJIT_Memory.cpp \
	ARMJIT_Global.cpp \
	dolphin/CommonFuncs.cpp

CORE_JIT_X64_SRCS = \
	dolphin/x64ABI.cpp \
	dolphin/x64CPUDetect.cpp \
	dolphin/x64Emitter.cpp \
	ARMJIT_x64/ARMJIT_Compiler.cpp \
	ARMJIT_x64/ARMJIT_ALU.cpp \
	ARMJIT_x64/ARMJIT_LoadStore.cpp \
	ARMJIT_x64/ARMJIT_Branch.cpp \
	ARMJIT_x64/ARMJIT_Linkage.S

CORE_DSP_HLE_SRCS = \
	AACUcode.cpp \
	G711Ucode.cpp \
	GraphicsUcode.cpp \
	UcodeBase.cpp

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
	blip-buf/blip_buf.c

BIZPLATFORM_SRCS = \
	BizAAC.cpp \
	BizFile.cpp \
	BizLog.cpp \
	BizMic.cpp \
	BizOGL.cpp \
	BizPlatformStubs.cpp \
	BizSaveManager.cpp \
	BizThread.cpp

LIBFAAD_SRCS = \
	bits.c \
	cfft.c \
	common.c \
	decoder.c \
	drc.c \
	error.c \
	filtbank.c \
	huffman.c \
	is.c \
	mdct.c \
	mp4.c \
	ms.c \
	output.c \
	pns.c \
	pulse.c \
	specrec.c \
	syntax.c \
	tns.c

CCFLAGS += \
	-I./BizPlatform/faad2/include \
	-I./BizPlatform/faad2/libfaad \
	-DFAAD2_VERSION=2.11.2 \
	-DPACKAGE_VERSION=\"2.11.2\" \
	-DHAVE_INTTYPES_H=1 \
	-DHAVE_LRINTF=1 \
	-DHAVE_STDINT_H=1 \
	-DHAVE_STRING_H=1 \
	-DHAVE_STRINGS_H=1 \
	-DHAVE_SYS_STAT_H=1 \
	-DHAVE_SYS_TIME_H=1 \
	-DHAVE_SYS_TYPES_H=1 \
	-DHAVE_UNISTD_H=1 \
	-DLC_ONLY_DECODER -DDISABLE_SBR

SRCS = \
	$(addprefix melonDS/src/,$(CORE_SRCS)) \
	$(addprefix melonDS/src/,$(CORE_GL_SRCS)) \
	$(addprefix melonDS/src/,$(CORE_JIT_SRCS)) \
	$(addprefix melonDS/src/,$(CORE_JIT_X64_SRCS)) \
	$(addprefix melonDS/src/DSP_HLE/,$(CORE_DSP_HLE_SRCS)) \
	$(addprefix melonDS/src/teakra/src/,$(TEAKRA_SRCS)) \
	$(addprefix melonDS/src/fatfs/,$(FATFS_SRCS)) \
	$(addprefix melonDS/src/,$(MISC_SRCS)) \
	$(addprefix BizPlatform/,$(BIZPLATFORM_SRCS)) \
	$(addprefix BizPlatform/faad2/libfaad/,$(LIBFAAD_SRCS)) \
	BizConsoleCreator.cpp \
	BizDebugging.cpp \
	BizGLPresenter.cpp \
	BizInterface.cpp \
	dthumb.c

include ../common.mak
