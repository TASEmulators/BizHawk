# special makefile for MAME, which simply calls MAME's own makefile with special arguments for waterboxing

WATERBOX_DIR := $(realpath ..)
ROOT_DIR := $(realpath .)
OUTPUTDLL_DIR := $(realpath $(WATERBOX_DIR)/../Assets/dll)
OUTPUTDLLCOPY_DIR := $(realpath $(WATERBOX_DIR)/../output/dll)
OUT_DIR := $(ROOT_DIR)/obj
OBJ_DIR := $(OUT_DIR)/release
DOBJ_DIR := $(OUT_DIR)/debug
EMULIBC_OBJS := $(WATERBOX_DIR)/emulibc/obj/release/emulibc.c.o \
	$(shell find $(WATERBOX_DIR)/libco/obj/release -type f -name '*.o')
EMULIBC_DOBJS := $(WATERBOX_DIR)/emulibc/obj/debug/emulibc.c.o \
	$(shell find $(WATERBOX_DIR)/libco/obj/debug -type f -name '*.o')
SYSROOT := $(WATERBOX_DIR)/sysroot
LINKSCRIPT := $(WATERBOX_DIR)/linkscript.T

CC := $(SYSROOT)/bin/musl-gcc
COMMONFLAGS := -fvisibility=hidden -I$(WATERBOX_DIR)/emulibc -I$(WATERBOX_DIR)/libco \
	-Wall -mcmodel=large -mstack-protector-guard=global -no-pie -fno-pic -fno-pie \
	-fcf-protection=none -MD -MP -Wno-format-overflow -Wfatal-errors -DWATERBOX
CCFLAGS := $(COMMONFLAGS)
CXXFLAGS := $(COMMONFLAGS) -I$(SYSROOT)/include/c++/v1 -fno-use-cxa-atexit
LDFLAGS := -static -Wl,--eh-frame-hdr -T $(LINKSCRIPT)

EXTRA_LIBS := -lc++ -lc++abi -lunwind -L $(SYSROOT)/lib/linux -lclang_rt.builtins-x86_64

TARGET := libmamearcade.wbx

.DEFAULT_GOAL := release

TARGET_RELEASE := $(OBJ_DIR)/$(TARGET)
TARGET_DEBUG := $(DOBJ_DIR)/$(TARGET)

.PHONY: release debug install install-debug

release: $(TARGET_RELEASE)
debug: $(TARGET_DEBUG)

$(TARGET_RELEASE): $(EMULIBC_OBJS) $(LINKSCRIPT)
	@mkdir -p $(OBJ_DIR)
	@$(MAKE) SUBTARGET=arcade MAIN_SHARED_LIB=1 OPTIMIZE=s PTR64=1 PLATFORM=x86_64 REGENIE=1 DEPRECATED=0 NOWERROR=1 \
		NO_USE_PORTAUDIO=1 NO_USE_PULSEAUDIO=1 DONT_USE_NETWORK=1 NO_USE_MIDI=1 NO_OPENGL=1 NO_X11=1 NO_USE_XINPUT=1 USE_BUNDLED_LIB_SDL2=1 \
		BIN_DIR="$(OUT_DIR)" OVERRIDE_CC="$(CC)" OVERRIDE_CXX="$(CC)" OVERRIDE_LD="$(CC)" ARCHOPTS_C="$(CCFLAGS)" \
		ARCHOPTS_CXX="$(CXXFLAGS)" LDOPTS="$(LDFLAGS) $(CCFLAGS) $(EMULIBC_OBJS) $(EXTRA_LIBS)" linux_x64 -C $(ROOT_DIR)/mame
	@mv -f $(OUT_DIR)/libmamearcade.so $(TARGET_RELEASE)
	@strip --strip-all --keep-symbol=mame_launch --keep-symbol=mame_set_sound_callback --keep-symbol=mame_set_log_callback \
	--keep-symbol=mame_lua_execute --keep-symbol=mame_lua_get_int --keep-symbol=mame_lua_get_double \
	--keep-symbol=mame_lua_get_bool --keep-symbol=mame_lua_get_string --keep-symbol=mame_lua_free_string \
	--keep-symbol=mame_coswitch --keep-symbol=mame_read_byte --keep-symbol=co_clean --keep-symbol=ecl_seal $(TARGET_RELEASE)

$(TARGET_DEBUG): $(EMULIBC_DOBJS) $(LINKSCRIPT)
	@mkdir -p $(DOBJ_DIR)
	@$(MAKE) SUBTARGET=arcade MAIN_SHARED_LIB=1 OPTIMIZE=0 DEBUG=1 SYMBOLS=1 SYMLEVEL=3 PTR64=1 PLATFORM=x86_64 REGENIE=1 DEPRECATED=0 NOWERROR=1 VERBOSE=1 \
		NO_USE_PORTAUDIO=1 DONT_USE_NETWORK=1 NO_USE_MIDI=1 NO_OPENGL=1 NO_X11=1 NO_USE_XINPUT=1 USE_BUNDLED_LIB_SDL2=1 \
		BIN_DIR="$(OUT_DIR)" OVERRIDE_CC="$(CC)" OVERRIDE_CXX="$(CC)" OVERRIDE_LD="$(CC)" ARCHOPTS_C="$(CCFLAGS)" \
		ARCHOPTS_CXX="$(CXXFLAGS)" LDOPTS="$(LDFLAGS) $(CCFLAGS) $(EMULIBC_OBJS) $(EXTRA_LIBS)" linux_x64 -C $(ROOT_DIR)/mame
	@mv -f $(OUT_DIR)/libmamearcade.so $(TARGET_DEBUG)
	@strip --strip-all --keep-symbol=mame_launch --keep-symbol=mame_set_sound_callback --keep-symbol=mame_set_log_callback \
	--keep-symbol=mame_lua_execute --keep-symbol=mame_lua_get_int --keep-symbol=mame_lua_get_double \
	--keep-symbol=mame_lua_get_bool --keep-symbol=mame_lua_get_string --keep-symbol=mame_lua_free_string \
	--keep-symbol=mame_coswitch --keep-symbol=mame_read_byte --keep-symbol=co_clean --keep-symbol=ecl_seal $(TARGET_DEBUG)

install: $(TARGET_RELEASE)
	@cp -f $< $(OUTPUTDLL_DIR)
	@zstd --stdout --ultra -22 --threads=0 $< > $(OUTPUTDLL_DIR)/$(TARGET).zst
	@cp $(OUTPUTDLL_DIR)/$(TARGET).zst $(OUTPUTDLLCOPY_DIR)/$(TARGET).zst || true
	@echo Release build of $(TARGET) installed.

install-debug: $(TARGET_DEBUG)
	@cp -f $< $(OUTPUTDLL_DIR)
	@zstd --stdout --ultra -22 --threads=0 $< > $(OUTPUTDLL_DIR)/$(TARGET).zst
	@cp $(OUTPUTDLL_DIR)/$(TARGET).zst $(OUTPUTDLLCOPY_DIR)/$(TARGET).zst || true
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

-include $(OBJS:%o=%d)
-include $(DOBJS:%o=%d)
