ROOT_DIR := $(realpath .)
CORE_DIR := $(realpath $(ROOT_DIR)/libsameboy/Core)
OUTPUTDLL_DIR := $(realpath $(ROOT_DIR)/../../Assets/dll)
OUTPUTDLLCOPY_DIR := $(realpath $(ROOT_DIR)/../../output/dll)

OUT_DIR := $(ROOT_DIR)/obj
OBJ_DIR := $(OUT_DIR)/release
DOBJ_DIR := $(OUT_DIR)/debug

CC := gcc
CCFLAGS := -I$(CORE_DIR) -Wall -Wextra -std=gnu11 -Wno-strict-aliasing \
	-Wno-multichar -Wno-implicit-fallthrough -Wno-sign-compare -Wno-unused-parameter \
	-Wno-int-in-bool-context -Wno-missing-field-initializers -Wno-overflow -Wno-unused-result \
	-D_GNU_SOURCE -D_USE_MATH_DEFINES -DNDEBUG -DGB_INTERNAL -DGB_DISABLE_DEBUGGER \
	-DGB_DISABLE_CHEATS -DGB_DISABLE_TIMEKEEPING -DGB_DISABLE_REWIND -DGB_VERSION=

ifeq ($(OS),Windows_NT)
TARGET := libsameboy.dll
else
TARGET := libsameboy.so
CCFLAGS := $(CCFLAGS) -fPIC
endif

SRCS := \
	$(CORE_DIR)/apu.c \
	$(CORE_DIR)/random.c \
	$(CORE_DIR)/camera.c \
	$(CORE_DIR)/rumble.c \
	$(CORE_DIR)/save_state.c \
	$(CORE_DIR)/display.c \
	$(CORE_DIR)/sgb.c \
	$(CORE_DIR)/gb.c \
	$(CORE_DIR)/sm83_cpu.c \
	$(CORE_DIR)/mbc.c \
	$(CORE_DIR)/memory.c \
	$(CORE_DIR)/timing.c \
	$(CORE_DIR)/printer.c \
	$(CORE_DIR)/joypad.c \
	$(ROOT_DIR)/BizInterface.c \
	$(ROOT_DIR)/blip_buf.c

LDFLAGS := -shared -Wno-attributes
CCFLAGS_DEBUG := -O0 -g
CCFLAGS_RELEASE := -O3 -flto
LDFLAGS_DEBUG :=
LDFLAGS_RELEASE := -s -Wno-lto-type-mismatch

_OBJS := $(addsuffix .o,$(realpath $(SRCS)))
OBJS := $(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))
DOBJS := $(patsubst $(ROOT_DIR)%,$(DOBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.c.o: %.c
	@echo cc $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS) $(CCFLAGS_RELEASE)
$(DOBJ_DIR)/%.c.o: %.c
	@echo cc $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS) $(CCFLAGS_DEBUG)

.DEFAULT_GOAL := install

TARGET_RELEASE := $(OBJ_DIR)/$(TARGET)
TARGET_DEBUG := $(DOBJ_DIR)/$(TARGET)

.PHONY: release debug install install-debug

release: $(TARGET_RELEASE)
debug: $(TARGET_DEBUG)

$(TARGET_RELEASE): $(OBJS)
	@echo ld $@
	@$(CC) -o $@ $(LDFLAGS) $(LDFLAGS_RELEASE) $(CCFLAGS) $(CCFLAGS_RELEASE) $(OBJS)
$(TARGET_DEBUG): $(DOBJS)
	@echo ld $@
	@$(CC) -o $@ $(LDFLAGS) $(LDFLAGS_DEBUG) $(CCFLAGS) $(CCFLAGS_DEBUG) $(DOBJS)

install: $(TARGET_RELEASE)
	@cp -f $< $(OUTPUTDLL_DIR)
	@cp $(OUTPUTDLL_DIR)/$(TARGET) $(OUTPUTDLLCOPY_DIR)/$(TARGET) || true
	@echo Release build of $(TARGET) installed.

install-debug: $(TARGET_DEBUG)
	@cp -f $< $(OUTPUTDLL_DIR)
	@cp $(OUTPUTDLL_DIR)/$(TARGET) $(OUTPUTDLLCOPY_DIR)/$(TARGET) || true
	@echo Debug build of $(TARGET) installed.

.PHONY: clean clean-release clean-debug
clean:
	rm -rf $(OUT_DIR)
clean-release:
	rm -rf $(OUT_DIR)/release
clean-debug:
	rm -rf $(OUT_DIR)/debug

-include $(OBJS:%o=%d)
-include $(DOBJS:%o=%d)
