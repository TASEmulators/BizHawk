# common parts of all waterbox cores

WATERBOX_DIR := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOT_DIR := $(shell dirname $(realpath $(lastword $(filter-out $(lastword $(MAKEFILE_LIST)),$(MAKEFILE_LIST)))))
OUTPUTDLL_DIR := $(realpath $(WATERBOX_DIR)/../Assets/dll)
OUTPUTDLLCOPY_DIR := $(realpath $(WATERBOX_DIR)/../output/dll)
ifeq ($(OUT_DIR),)
OUT_DIR := $(ROOT_DIR)/obj
endif
OBJ_DIR := $(OUT_DIR)/release
DOBJ_DIR := $(OUT_DIR)/debug
EMULIBC_OBJS := $(WATERBOX_DIR)/emulibc/obj/release/emulibc.c.o
EMULIBC_DOBJS := $(WATERBOX_DIR)/emulibc/obj/debug/emulibc.c.o
SYSROOT := $(WATERBOX_DIR)/sysroot
ifdef NEED_LIBCO
EMULIBC_OBJS := $(EMULIBC_OBJS) $(shell find $(WATERBOX_DIR)/libco/obj/release -type f -name '*.o')
EMULIBC_DOBJS := $(EMULIBC_DOBJS) $(shell find $(WATERBOX_DIR)/libco/obj/debug -type f -name '*.o')
endif
LINKSCRIPT := $(WATERBOX_DIR)/linkscript.T

print-%: ;
	@echo $* = $($*)

#LD_PLUGIN := $(shell gcc --print-file-name=liblto_plugin.so)

ifneq (,$(wildcard $(SYSROOT)/bin/musl-clang))
CC := $(SYSROOT)/bin/musl-clang
else ifneq (,$(wildcard $(SYSROOT)/bin/musl-gcc))
CC := $(SYSROOT)/bin/musl-gcc
else
$(error Compiler not found in sysroot)
endif
COMMONFLAGS := -fvisibility=hidden -I$(WATERBOX_DIR)/emulibc -Wall -mcmodel=large \
	-mstack-protector-guard=global -fno-pic -fno-pie -fcf-protection=none \
	-MD -MP
CCFLAGS := $(COMMONFLAGS) $(CCFLAGS)
LDFLAGS := $(LDFLAGS) -static -no-pie -Wl,--eh-frame-hdr,-O2 -T $(LINKSCRIPT) #-Wl,--plugin,$(LD_PLUGIN)
CCFLAGS_DEBUG := -O0 -g
CCFLAGS_RELEASE := -O3 -flto -DNDEBUG
CCFLAGS_RELEASE_ASONLY := -O3
LDFLAGS_DEBUG :=
LDFLAGS_RELEASE :=
CXXFLAGS := $(COMMONFLAGS) $(CXXFLAGS) -I$(SYSROOT)/include/c++/v1 -fno-use-cxa-atexit -fvisibility-inlines-hidden
CXXFLAGS_DEBUG := -O0 -g
CXXFLAGS_RELEASE := -O3 -flto -DNDEBUG
CXXFLAGS_RELEASE_ASONLY := -O3

EXTRA_LIBS := -L $(SYSROOT)/lib/linux -lclang_rt.builtins-x86_64 $(EXTRA_LIBS)
CPP_EXTRA_LIBS := -lc++ -lc++abi -lunwind $(EXTRA_LIBS)

ifneq ($(filter %.cpp,$(SRCS)),)
EXTRA_LIBS += $(CPP_EXTRA_LIBS)
endif

ifneq ($(filter %.cxx,$(SRCS)),)
EXTRA_LIBS += $(CPP_EXTRA_LIBS)
endif

_OBJS := $(addsuffix .o,$(abspath $(SRCS)))
OBJS := $(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))
DOBJS := $(patsubst $(ROOT_DIR)%,$(DOBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.c.o: %.c
	@echo cc $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS) $(CCFLAGS_RELEASE) $(PER_FILE_FLAGS_$<)
$(OBJ_DIR)/%.cpp.o: %.cpp
	@echo cxx $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CXXFLAGS) $(CXXFLAGS_RELEASE) $(PER_FILE_FLAGS_$<)
$(OBJ_DIR)/%.cxx.o: %.cxx
	@echo cxx $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CXXFLAGS) $(CXXFLAGS_RELEASE) $(PER_FILE_FLAGS_$<)
$(DOBJ_DIR)/%.c.o: %.c
	@echo cc $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS) $(CCFLAGS_DEBUG) $(PER_FILE_FLAGS_$<)
$(DOBJ_DIR)/%.cpp.o: %.cpp
	@echo cxx $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CXXFLAGS) $(CXXFLAGS_DEBUG) $(PER_FILE_FLAGS_$<)
$(DOBJ_DIR)/%.cxx.o: %.cxx
	@echo cxx $<
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CXXFLAGS) $(CXXFLAGS_DEBUG) $(PER_FILE_FLAGS_$<)
$(OBJ_DIR)/%.c.s: %.c
	@echo cc -S $<
	@mkdir -p $(@D)
	@$(CC) -c -S -o $@ $< $(CCFLAGS) $(CCFLAGS_RELEASE_ASONLY) $(PER_FILE_FLAGS_$<)
$(OBJ_DIR)/%.cpp.s: %.cpp
	@echo cxx -S $<
	@mkdir -p $(@D)
	@$(CC) -c -S -o $@ $< $(CXXFLAGS) $(CXXFLAGS_RELEASE_ASONLY) $(PER_FILE_FLAGS_$<)
$(OBJ_DIR)/%.cxx.s: %.cxx
	@echo cxx -S $<
	@mkdir -p $(@D)
	@$(CC) -c -S -o $@ $< $(CXXFLAGS) $(CXXFLAGS_RELEASE_ASONLY) $(PER_FILE_FLAGS_$<)

ifndef NO_WBX_TARGETS

.DEFAULT_GOAL := release

TARGET_RELEASE := $(OBJ_DIR)/$(TARGET)
TARGET_DEBUG := $(DOBJ_DIR)/$(TARGET)

.PHONY: release debug install install-debug

release: $(TARGET_RELEASE)
debug: $(TARGET_DEBUG)

$(TARGET_RELEASE): $(OBJS) $(EMULIBC_OBJS) $(LINKSCRIPT)
	@echo ld $@
	@$(CC) -o $@ $(LDFLAGS) $(LDFLAGS_RELEASE) $(CCFLAGS) $(CCFLAGS_RELEASE) $(OBJS) $(EMULIBC_OBJS) $(EXTRA_LIBS)
$(TARGET_DEBUG): $(DOBJS) $(EMULIBC_DOBJS) $(LINKSCRIPT)
	@echo ld $@
	@$(CC) -o $@ $(LDFLAGS) $(LDFLAGS_DEBUG) $(CCFLAGS) $(CCFLAGS_DEBUG) $(DOBJS) $(EMULIBC_DOBJS) $(EXTRA_LIBS)

install: $(TARGET_RELEASE)
	@cp -f $< $(OUTPUTDLL_DIR)
	@zstd --stdout --ultra -22 --threads=0 $< > $(OUTPUTDLL_DIR)/$(TARGET).zst
	@cp $(OUTPUTDLL_DIR)/$(TARGET).zst $(OUTPUTDLLCOPY_DIR)/$(TARGET).zst 2> /dev/null || true
	@echo Release build of $(TARGET) installed.

install-debug: $(TARGET_DEBUG)
	@cp -f $< $(OUTPUTDLL_DIR)
	@zstd --stdout -1 --threads=0 $< > $(OUTPUTDLL_DIR)/$(TARGET).zst
	@cp $(OUTPUTDLL_DIR)/$(TARGET).zst $(OUTPUTDLLCOPY_DIR)/$(TARGET).zst 2> /dev/null || true
	@echo Debug build of $(TARGET) installed.

else

# add fake rules that match the WBX_TARGETS case to ease use of all-cores.mak

.DEFAULT_GOAL = all

.PHONY: all release debug install install-debug
release debug install install-debug: all

all: $(OBJS) $(DOBJS)

endif

.PHONY: clean clean-release clean-debug
clean:
	rm -rf $(OUT_DIR)
clean-release:
	rm -rf $(OUT_DIR)/release
clean-debug:
	rm -rf $(OUT_DIR)/debug

-include $(OBJS:%o=%d)
-include $(DOBJS:%o=%d)
