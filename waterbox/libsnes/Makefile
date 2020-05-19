NEED_LIBCO := 1

#-DPROFILE_PERFORMANCE
CCFLAGS := -DHOOKS -DBIZHAWK -DPROFILE_COMPATIBILITY -DGAMEBOY \
	-D_GNU_SOURCE \
	-Werror=int-to-pointer-cast \
	-I../libco -I./bsnes \
	-Wno-parentheses -Wno-sign-compare \
	-Wno-unused-variable -Wno-unused-function \
	-fno-threadsafe-statics \
	-std=c++0x

TARGET = libsnes.wbx

SRCS_PERF = \
	$(ROOT_DIR)/bsnes/snes/alt/cpu/cpu.cpp \
	$(ROOT_DIR)/bsnes/snes/alt/ppu-performance/ppu.cpp \
	$(ROOT_DIR)/bsnes/snes/alt/smp/smp.cpp
SRCS_COMPAT = \
	$(ROOT_DIR)/bsnes/snes/alt/ppu-compatibility/ppu.cpp \
	$(ROOT_DIR)/bsnes/snes/cpu/cpu.cpp \
	$(ROOT_DIR)/bsnes/snes/smp/smp.cpp
SRCS_ALL = \
	$(ROOT_DIR)/bsnes/base/base.cpp \
	$(ROOT_DIR)/bsnes/gameboy/apu/apu.cpp \
	$(ROOT_DIR)/bsnes/gameboy/cartridge/cartridge.cpp \
	$(ROOT_DIR)/bsnes/gameboy/cpu/cpu.cpp \
	$(ROOT_DIR)/bsnes/snes/alt/dsp/dsp.cpp \
	$(ROOT_DIR)/bsnes/gameboy/interface/interface.cpp \
	$(ROOT_DIR)/bsnes/gameboy/lcd/lcd.cpp \
	$(ROOT_DIR)/bsnes/gameboy/memory/memory.cpp \
	$(ROOT_DIR)/bsnes/gameboy/scheduler/scheduler.cpp \
	$(ROOT_DIR)/bsnes/gameboy/system/system.cpp \
	$(ROOT_DIR)/bsnes/gameboy/video/video.cpp \
	$(ROOT_DIR)/bsnes/snes/cartridge/cartridge.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/armdsp/armdsp.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/bsx/bsx.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/hitachidsp/hitachidsp.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/icd2/icd2.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/link/link.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/msu1/msu1.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/necdsp/necdsp.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/nss/nss.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/obc1/obc1.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/sa1/sa1.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/sdd1/sdd1.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/spc7110/spc7110.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/srtc/srtc.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/sufamiturbo/sufamiturbo.cpp \
	$(ROOT_DIR)/bsnes/snes/chip/superfx/superfx.cpp \
	$(ROOT_DIR)/bsnes/snes/config/config.cpp \
	$(ROOT_DIR)/bsnes/snes/controller/controller.cpp \
	$(ROOT_DIR)/bsnes/snes/cpu/core/core.cpp \
	$(ROOT_DIR)/bsnes/snes/interface/interface.cpp \
	$(ROOT_DIR)/bsnes/snes/memory/memory.cpp \
	$(ROOT_DIR)/bsnes/snes/smp/core/core.cpp \
	$(ROOT_DIR)/bsnes/snes/system/system.cpp \
	$(ROOT_DIR)/bsnes/target-libsnes/libsnes.cpp \
	$(ROOT_DIR)/bsnes/target-libsnes/libsnes_pwrap.cpp
SRCS = $(SRCS_ALL) $(SRCS_COMPAT)

include ../common.mak
