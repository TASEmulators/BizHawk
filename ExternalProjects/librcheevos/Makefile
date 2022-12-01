ROOT_DIR := $(realpath .)
OUTPUTDLL_DIR := $(realpath $(ROOT_DIR)/../../Assets/dll)
OUTPUTDLLCOPY_DIR := $(realpath $(ROOT_DIR)/../../output/dll)

OUT_DIR := $(ROOT_DIR)/obj
OBJ_DIR := $(OUT_DIR)/release
DOBJ_DIR := $(OUT_DIR)/debug

CC := gcc
CCFLAGS := -I$(ROOT_DIR)/rcheevos/include -Wall -Wextra -std=c89 \
	-Wno-implicit-fallthrough -Wno-missing-field-initializers \
	-Wno-unused-parameter -Wno-maybe-uninitialized -DRC_DISABLE_LUA

ifeq ($(OS),Windows_NT)
TARGET := librcheevos.dll
else
TARGET := librcheevos.so
endif

# rc_libretro.c wants libretro.h which is expected by the builder to provide (and that file is huge)
# not needed for our purposes anyways, so don't bother
SRCS := $(filter-out %rc_libretro.c,$(shell find $(ROOT_DIR)/rcheevos/src -type f -name '*.c'))

LDFLAGS := -shared
CCFLAGS_DEBUG := -O0 -g
CCFLAGS_RELEASE := -O3 -flto
LDFLAGS_DEBUG :=
LDFLAGS_RELEASE := -s

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
