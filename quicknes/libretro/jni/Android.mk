LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

ifeq ($(TARGET_ARCH),arm)
LOCAL_CFLAGS += -DANDROID_ARM
LOCAL_ARM_MODE := arm
endif

ifeq ($(TARGET_ARCH),x86)
LOCAL_CFLAGS +=  -DANDROID_X86
endif

ifeq ($(TARGET_ARCH),mips)
LOCAL_CFLAGS += -DANDROID_MIPS
endif

LOCAL_MODULE    := libretro

EMU_DIR := ../../nes_emu
LIBRETRO_DIR := ../

CXXSRCS := \
	$(EMU_DIR)/abstract_file.cpp \
	$(EMU_DIR)/apu_state.cpp \
	$(EMU_DIR)/Blip_Buffer.cpp \
	$(EMU_DIR)/Effects_Buffer.cpp \
	$(EMU_DIR)/Mapper_Fme7.cpp \
	$(EMU_DIR)/Mapper_Mmc5.cpp \
	$(EMU_DIR)/Mapper_Namco106.cpp \
	$(EMU_DIR)/Mapper_Vrc6.cpp \
	$(EMU_DIR)/misc_mappers.cpp \
	$(EMU_DIR)/Multi_Buffer.cpp \
	$(EMU_DIR)/Nes_Apu.cpp \
	$(EMU_DIR)/Nes_Buffer.cpp \
	$(EMU_DIR)/Nes_Cart.cpp \
	$(EMU_DIR)/Nes_Core.cpp \
	$(EMU_DIR)/Nes_Cpu.cpp \
	$(EMU_DIR)/nes_data.cpp \
	$(EMU_DIR)/Nes_Effects_Buffer.cpp \
	$(EMU_DIR)/Nes_Emu.cpp \
	$(EMU_DIR)/Nes_File.cpp \
	$(EMU_DIR)/Nes_Film.cpp \
	$(EMU_DIR)/Nes_Film_Data.cpp \
	$(EMU_DIR)/Nes_Film_Packer.cpp \
	$(EMU_DIR)/Nes_Fme7_Apu.cpp \
	$(EMU_DIR)/Nes_Mapper.cpp \
	$(EMU_DIR)/nes_mappers.cpp \
	$(EMU_DIR)/Nes_Mmc1.cpp \
	$(EMU_DIR)/Nes_Mmc3.cpp \
	$(EMU_DIR)/Nes_Namco_Apu.cpp \
	$(EMU_DIR)/Nes_Oscs.cpp \
	$(EMU_DIR)/Nes_Ppu.cpp \
	$(EMU_DIR)/Nes_Ppu_Impl.cpp \
	$(EMU_DIR)/Nes_Ppu_Rendering.cpp \
	$(EMU_DIR)/Nes_Recorder.cpp \
	$(EMU_DIR)/Nes_State.cpp \
	$(EMU_DIR)/nes_util.cpp \
	$(EMU_DIR)/Nes_Vrc6_Apu.cpp \
	$(LIBRETRO_DIR)/libretro.cpp

LIBSRCS := \
	$(LIBRETRO_DIR)/../fex/Data_Reader.cpp \
	$(LIBRETRO_DIR)/../fex/blargg_errors.cpp \
	$(LIBRETRO_DIR)/../fex/blargg_common.cpp

LOCAL_SRC_FILES    =  $(CXXSRCS) $(LIBSRCS)
LOCAL_CXXFLAGS = -DANDROID -D__LIBRETRO__ -Wall -Wno-multichar -Wno-unused-variable -Wno-sign-compare -DNDEBUG \
	-DSTD_AUTO_FILE_WRITER=Std_File_Writer \
	-DSTD_AUTO_FILE_READER=Std_File_Reader \
	-DSTD_AUTO_FILE_COMP_READER=Std_File_Reader \
	-DSTD_AUTO_FILE_COMP_WRITER=Std_File_Writer
LOCAL_C_INCLUDES = $(LIBRETRO_DIR) $(EMU_DIR) $(EMU_DIR)/..

include $(BUILD_SHARED_LIBRARY)
