CCFLAGS := -Icore -Iutil -Icore/m68k -Icore/z80 -Icore/input_hw \
	-Icore/cart_hw -Icore/cart_hw/svp -Icore/sound -Icore/ntsc -Icore/cd_hw \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer \
	-DLSB_FIRST -DUSE_32BPP_RENDERING -DINLINE=static\ __inline__

LDFLAGS :=

TARGET := gpgx.wbx

SRCS = $(shell find $(ROOT_DIR) -type f -name '*.c')

include ../common.mak
