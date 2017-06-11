CC = x86_64-nt64-midipix-g++

	# -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast
	#-std=c99 -fomit-frame-pointer -fvisibility=hidden
	#-DPROFILE_PERFORMANCE
	#-fno-exceptions -fno-rtti 
CCFLAGS:= -DHOOKS -DBIZHAWK -DPROFILE_COMPATIBILITY -DGAMEBOY \
	-D_GNU_SOURCE \
	-Werror=pointer-to-int-cast -Werror=int-to-pointer-cast \
	-I../emulibc -I../libco -I./bsnes \
	-Wall -Werror=implicit-function-declaration \
	-Wno-parentheses -Wno-sign-compare \
	-Wno-unused-variable -Wno-unused-function \
	-fvisibility=hidden \
	-fno-threadsafe-statics \
	-std=c++0x \
	-O3 -flto

TARGET = libsnes.wbx

LDFLAGS = -Wl,--dynamicbase,--export-all-symbols

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))

SRCS_PERF:= \
	$(ROOT_DIR)/bsnes/snes/alt/cpu/cpu.cpp \
	$(ROOT_DIR)/bsnes/snes/alt/ppu-performance/ppu.cpp \
	$(ROOT_DIR)/bsnes/snes/alt/smp/smp.cpp
SRCS_COMPAT:= \
	$(ROOT_DIR)/bsnes/snes/alt/ppu-compatibility/ppu.cpp \
	$(ROOT_DIR)/bsnes/snes/cpu/cpu.cpp \
	$(ROOT_DIR)/bsnes/snes/smp/smp.cpp
SRCS_ALL:= \
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
SRCS:=$(SRCS_ALL) $(SRCS_COMPAT)

OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.cpp=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: $(ROOT_DIR)/%.cpp
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

all: $(TARGET)

.PHONY: clean all

$(TARGET).in: $(OBJS)
	@$(CC) -o $@ $(LDFLAGS) $(CCFLAGS) $(OBJS) ../emulibc/libemuhost.so ../libco/libco.so

$(TARGET): $(TARGET).in
	strip $< -o $@ -R /4 -R /14 -R /29 -R /41 -R /55 -R /67 -R /78 -R /89 -R /104
#	cp $< $@

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET).in
	rm -f $(TARGET)

print-%:
	@echo $* = $($*)
#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
