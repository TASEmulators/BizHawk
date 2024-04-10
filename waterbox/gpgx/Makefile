GPGX_DIR := Genesis-Plus-GX

CCFLAGS := 	-Iutil \
    -I${GPGX_DIR}/core \
	-I${GPGX_DIR}/core/m68k \
	-I${GPGX_DIR}/core/z80 \
	-I${GPGX_DIR}/core/input_hw \
	-I${GPGX_DIR}/core/cart_hw \
	-I${GPGX_DIR}/core/cart_hw/svp \
	-I${GPGX_DIR}/core/sound \
	-I${GPGX_DIR}/core/ntsc \
	-I${GPGX_DIR}/core/cd_hw \
	-Icinterface \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer -Wfatal-errors \
	-DUSE_BIZHAWK_CALLBACKS \
	-DHAVE_YM3438_CORE \
	-DHAVE_OPLL_CORE \
	-DUSE_RAM_DEEPFREEZE \
	-DLSB_FIRST \
	-DUSE_32BPP_RENDERING \
	-DINLINE=static\ __inline__ \
	-fcommon

LDFLAGS :=

TARGET := gpgx.wbx

SRCS = $(shell find $(GPGX_DIR)/core -type f -name '*.c') cinterface/cinterface.c cinterface/cddImpl.c

include ../common.mak
