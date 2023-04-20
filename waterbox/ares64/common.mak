NEED_LIBCO := 1

ARES_PATH = $(ROOT_DIR)/ares/ares
NALL_PATH = $(ROOT_DIR)/ares/nall
THIRDPARTY_PATH = $(ROOT_DIR)/ares/thirdparty
ANGRYLION_PATH = $(THIRDPARTY_PATH)/angrylion-rdp/mylittle-nocomment
SLJIT_PATH = $(THIRDPARTY_PATH)/sljit/sljit_src

CCFLAGS := -march=x86-64-v2 -I.$(THIRDPARTY_PATH) -DSLJIT_HAVE_CONFIG_PRE=1 -DSLJIT_HAVE_CONFIG_POST=1

CXXFLAGS := -std=gnu++17 -march=x86-64-v2 \
	-I../libco -I.$(ROOT_DIR)/ares -I.$(ARES_PATH) -I.$(THIRDPARTY_PATH) -I.$(ANGRYLION_PATH) \
	-Werror=int-to-pointer-cast -Wno-unused-but-set-variable -Wno-format-security \
	-Wno-parentheses -Wno-reorder -Wno-unused-variable -Wno-delete-non-virtual-dtor \
	-Wno-sign-compare -Wno-switch -Wno-unused-local-typedefs -Wno-bool-operation \
	-Wno-mismatched-tags -Wno-missing-braces -Wno-overloaded-virtual \
	-Wno-unused-private-field -Wno-sometimes-uninitialized \
	-fno-strict-aliasing -fwrapv \
	-DSLJIT_HAVE_CONFIG_PRE=1 -DSLJIT_HAVE_CONFIG_POST=1 \
	-DWANT_CPU_INTERPRETER=$(WANT_CPU_INTERPRETER)

ifneq (0,$(WANT_CPU_INTERPRETER))
TARGET = ares64_interpreter.wbx
else
TARGET = ares64_recompiler.wbx
endif

SRCS_NALL = \
	$(NALL_PATH)/nall.cpp

SRCS_PROCESSORS = \
	$(ARES_PATH)/component/processor/sm5k/sm5k.cpp

SRCS_ARES = \
	$(ARES_PATH)/ares/ares.cpp \
	$(ARES_PATH)/ares/memory/fixed-allocator.cpp

SRCS_N64 = \
	$(ARES_PATH)/n64/memory/memory.cpp \
	$(ARES_PATH)/n64/system/system.cpp \
	$(ARES_PATH)/n64/cartridge/cartridge.cpp \
	$(ARES_PATH)/n64/cic/cic.cpp \
	$(ARES_PATH)/n64/controller/controller.cpp \
	$(ARES_PATH)/n64/dd/dd.cpp \
	$(ARES_PATH)/n64/mi/mi.cpp \
	$(ARES_PATH)/n64/vi/vi.cpp \
	$(ARES_PATH)/n64/ai/ai.cpp \
	$(ARES_PATH)/n64/pi/pi.cpp \
	$(ARES_PATH)/n64/pif/pif.cpp \
	$(ARES_PATH)/n64/ri/ri.cpp \
	$(ARES_PATH)/n64/si/si.cpp \
	$(ARES_PATH)/n64/rdram/rdram.cpp \
	$(ARES_PATH)/n64/cpu/cpu.cpp \
	$(ARES_PATH)/n64/rsp/rsp.cpp \
	$(ARES_PATH)/n64/rdp/rdp.cpp

SRCS_ANGRYLION = \
	$(ANGRYLION_PATH)/angrylion.cpp \
	$(ANGRYLION_PATH)/n64video.cpp

SRCS_SLJIT = \
	$(SLJIT_PATH)/sljitLir.c \
	$(THIRDPARTY_PATH)/sljitAllocator.cpp

SRCS = $(SRCS_NALL) $(SRCS_PROCESSORS) $(SRCS_ARES) $(SRCS_N64) $(SRCS_ANGRYLION) $(SRCS_SLJIT) $(ROOT_DIR)/BizInterface.cpp
