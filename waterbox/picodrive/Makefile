CCFLAGS := -I. \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer \
	-falign-functions=16 \
	-DLSB_FIRST -DNDEBUG -DEMU_F68K -D_USE_CZ80

TARGET := picodrive.wbx
SRCS = $(shell find $(ROOT_DIR) -type f -name '*.c')
include ../common.mak
