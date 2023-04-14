# special makefile for MAME, which simply calls MAME's own makefile with special arguments for waterboxing

WATERBOX_DIR := $(realpath ..)
ROOT_DIR := $(realpath .)
OUTPUTDLL_DIR := $(realpath $(WATERBOX_DIR)/../Assets/dll)
OUTPUTDLLCOPY_DIR := $(realpath $(WATERBOX_DIR)/../output/dll)
OUT_DIR := $(ROOT_DIR)/obj
OBJ_DIR := $(OUT_DIR)/release
DOBJ_DIR := $(OUT_DIR)/debug

TARGET := libmamearcade.wbx

.DEFAULT_GOAL := release

TARGET_RELEASE := $(OBJ_DIR)/$(TARGET)
TARGET_DEBUG := $(DOBJ_DIR)/$(TARGET)

.PHONY: release debug install install-debug

release: $(TARGET_RELEASE)
debug: $(TARGET_DEBUG)

$(TARGET_RELEASE):
	@$(MAKE) SUBTARGET=arcade WATERBOX=1 OPTIMIZE=s DEPRECATED=0 NOWERROR=1 \
		WBX_DIR=$(WATERBOX_DIR) BUILDDIR=$(OBJ_DIR) -C $(ROOT_DIR)/mame
	@mv -f $(ROOT_DIR)/mame/*.wbx $(TARGET_RELEASE)
	@strip --strip-all -wK "mame_*" -K "co_clean" -K "ecl_seal" -K "__wbxsysinfo" $(TARGET_RELEASE)

$(TARGET_DEBUG):
	@$(MAKE) SUBTARGET=arcade WATERBOX=1 OPTIMIZE=g DEBUG=1 SYMBOLS=1 PROFILER=0 DEPRECATED=0 NOWERROR=1 \
		WBX_DIR=$(WATERBOX_DIR) BUILDDIR=$(DOBJ_DIR) -C $(ROOT_DIR)/mame
	@mv -f $(ROOT_DIR)/mame/*.wbx $(TARGET_DEBUG)

install: $(TARGET_RELEASE)
	@cp -f $< $(OUTPUTDLL_DIR)
	@zstd --stdout --ultra -22 --threads=0 $< > $(OUTPUTDLL_DIR)/$(TARGET).zst
	@cp $(OUTPUTDLL_DIR)/$(TARGET).zst $(OUTPUTDLLCOPY_DIR)/$(TARGET).zst 2> /dev/null || true
	@echo Release build of $(TARGET) installed.

install-debug: $(TARGET_DEBUG)
	@cp -f $< $(OUTPUTDLL_DIR)
	@zstd --stdout --ultra -22 --threads=0 $< > $(OUTPUTDLL_DIR)/$(TARGET).zst
	@cp $(OUTPUTDLL_DIR)/$(TARGET).zst $(OUTPUTDLLCOPY_DIR)/$(TARGET).zst 2> /dev/null || true
	@echo Debug build of $(TARGET) installed.

.PHONY: clean clean-release clean-debug
clean:
	@$(MAKE) clean -C $(ROOT_DIR)/mame
	rm -rf $(OUT_DIR)
clean-release:
	@$(MAKE) clean -C $(ROOT_DIR)/mame
	rm -rf $(OUT_DIR)/release
clean-debug:
	@$(MAKE) clean -C $(ROOT_DIR)/mame
	rm -rf $(OUT_DIR)/debug
