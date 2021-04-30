LOCAL_PATH := $(call my-dir)

CORE_DIR := $(LOCAL_PATH)/../..

CFLAGS :=

include $(CORE_DIR)/libretro/Makefile.common

GENERATED_SOURCES := $(filter %_boot.c,$(SOURCES_C))

COREFLAGS := -DINLINE=inline -D__LIBRETRO__ -DGB_INTERNAL $(INCFLAGS) -DSAMEBOY_CORE_VERSION=\"$(VERSION)\" -Wno-multichar -DANDROID

GIT_VERSION := " $(shell git rev-parse --short HEAD || echo unknown)"
ifneq ($(GIT_VERSION)," unknown")
  COREFLAGS += -DGIT_VERSION=\"$(GIT_VERSION)\"
endif

include $(CLEAR_VARS)
LOCAL_MODULE    := retro
LOCAL_SRC_FILES := $(SOURCES_C)
LOCAL_CFLAGS    := -std=c99 $(COREFLAGS) $(CFLAGS)
LOCAL_LDFLAGS   := -Wl,-version-script=$(CORE_DIR)/libretro/link.T
include $(BUILD_SHARED_LIBRARY)

$(CORE_DIR)/libretro/%_boot.c: $(CORE_DIR)/build/bin/BootROMs/%_boot.bin
	echo "/* AUTO-GENERATED */" > $@
	echo "const unsigned char $(notdir $(@:%.c=%))[] = {" >> $@
	hexdump -v -e '/1 "0x%02x, "' $< >> $@
	echo "};" >> $@
	echo "const unsigned $(notdir $(@:%.c=%))_length = sizeof($(notdir $(@:%.c=%)));" >> $@

.INTERMEDIATE: $(GENERATED_SOURCES)
