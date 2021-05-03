NEED_LIBCO := 1

CXXFLAGS := -DHOOKS -DGAMEBOY \
	-D_GNU_SOURCE \
	-Werror=int-to-pointer-cast \
	-I../libco -I./bsnes \
	-Wno-parentheses -Wno-sign-compare \
	-Wno-unused-variable -Wno-unused-function \
	-fno-threadsafe-statics \
	-std=c++17 \
	-Wno-multichar -DGB_INTERNAL -DGB_DISABLE_DEBUGGER -DGB_DISABLE_CHEATS \
	-fno-strict-aliasing -fwrapv -Wno-trigraphs -Wno-switch -Wno-reorder -Wno-misleading-indentation

CCFLAGS := -std=c11 -DGB_INTERNAL -DGB_DISABLE_DEBUGGER -DGB_DISABLE_CHEATS -D_GNU_SOURCE -Wno-multichar \
	-Werror -Wall -Wno-unknown-warning -Wno-unknown-warning-option \
	-Wno-nonnull -Wno-unused-result -Wno-strict-aliasing -Wno-multichar -Wno-int-in-bool-context -fno-strict-aliasing

TARGET = bsnes.wbx

SRCS_PROCESSORS = \
	$(ROOT_DIR)/bsnes/processor/spc700/spc700.cpp \
	$(ROOT_DIR)/bsnes/processor/wdc65816/wdc65816.cpp \
	$(ROOT_DIR)/bsnes/processor/arm7tdmi/arm7tdmi.cpp

SRCS_EMULATOR = \
	$(ROOT_DIR)/bsnes/emulator/emulator.cpp \
	$(ROOT_DIR)/bsnes/filter/filter.cpp

SRCS_GB = \
	$(ROOT_DIR)/bsnes/gb/Core/apu.c \
	$(ROOT_DIR)/bsnes/gb/Core/camera.c \
	$(ROOT_DIR)/bsnes/gb/Core/rumble.c \
	$(ROOT_DIR)/bsnes/gb/Core/display.c \
	$(ROOT_DIR)/bsnes/gb/Core/gb.c \
	$(ROOT_DIR)/bsnes/gb/Core/joypad.c \
	$(ROOT_DIR)/bsnes/gb/Core/mbc.c \
	$(ROOT_DIR)/bsnes/gb/Core/memory.c \
	$(ROOT_DIR)/bsnes/gb/Core/printer.c \
	$(ROOT_DIR)/bsnes/gb/Core/random.c \
	$(ROOT_DIR)/bsnes/gb/Core/rewind.c \
	$(ROOT_DIR)/bsnes/gb/Core/save_state.c \
	$(ROOT_DIR)/bsnes/gb/Core/sgb.c \
	$(ROOT_DIR)/bsnes/gb/Core/sm83_cpu.c \
	$(ROOT_DIR)/bsnes/gb/Core/symbol_hash.c \
	$(ROOT_DIR)/bsnes/gb/Core/timing.c

SRCS_SFC = \
	$(ROOT_DIR)/bsnes/sfc/interface/interface.cpp \
	$(ROOT_DIR)/bsnes/sfc/system/system.cpp \
	$(ROOT_DIR)/bsnes/sfc/controller/controller.cpp \
	$(ROOT_DIR)/bsnes/sfc/cartridge/cartridge.cpp \
	$(ROOT_DIR)/bsnes/sfc/memory/memory.cpp \
	$(ROOT_DIR)/bsnes/sfc/cpu/cpu.cpp \
	$(ROOT_DIR)/bsnes/sfc/smp/smp.cpp \
	$(ROOT_DIR)/bsnes/sfc/dsp/dsp.cpp \
	$(ROOT_DIR)/bsnes/sfc/ppu/ppu.cpp \
	$(ROOT_DIR)/bsnes/sfc/ppu-fast/ppu.cpp \
	$(ROOT_DIR)/bsnes/sfc/expansion/expansion.cpp \
	$(ROOT_DIR)/bsnes/sfc/coprocessor/coprocessor.cpp \
	$(ROOT_DIR)/bsnes/sfc/slot/slot.cpp \
	$(ROOT_DIR)/bsnes/target-bsnescore/bsnescore.cpp \
	$(ROOT_DIR)/bsnes/target-bsnescore/bsnescore_pwrap.cpp

SRCS = $(SRCS_PROCESSORS) $(SRCS_EMULATOR) $(SRCS_GB) $(SRCS_SFC)

include ../common.mak
