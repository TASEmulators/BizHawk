CC = gcc

ARCH = 64
#MACHINE = $(shell $(CC) -dumpmachine)
#ifneq (,$(findstring i686,$(MACHINE)))
#    ARCH = 32
#else ifneq (,$(findstring x86_64,$(MACHINE)))
#    ARCH = 64
#else
#    $(error Unknown arch)
#endif

CCFLAGS:=-Icore -Iutil -Icore/m68k -Icore/z80 -Icore/input_hw \
	-Icore/cart_hw -Icore/cart_hw/svp -Icore/sound -Icore/ntsc -Icore/cd_hw \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer -fvisibility=hidden \
	-DLSB_FIRST -DUSE_32BPP_RENDERING -DINLINE=static\ __inline__ \
	-ffreestanding -nostdinc -nostdlib \
	-I../libc/includes -I../libc/internals \
	-mcmodel=large -O2

TARGET = gpgx.elf

LDFLAGS_32 =
LDFLAGS_64 = -static
LDFLAGS = $(LDFLAGS_$(ARCH)) $(CCFLAGS)

#DEST_32 = ../../../../../output/dll
#DEST_64 = ../../../../../output64/dll

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c')
OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.c=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

all: $(TARGET)

.PHONY: clean all

$(TARGET) : $(OBJS) lscript
	@$(CC) -o $@ $(LDFLAGS) -T lscript $(CCFLAGS) $(OBJS) ../libc/emulibc.a

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET)

#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
