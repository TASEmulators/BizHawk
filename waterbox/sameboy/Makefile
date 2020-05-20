FLAGS := -Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-Wno-multichar \
	-fomit-frame-pointer

CCFLAGS := $(FLAGS) \
	-std=gnu99 \
	-DLSB_FIRST -D_GNU_SOURCE -DGB_INTERNAL

CPPFLAGS := $(FLAGS) -DSPC_NO_COPY_STATE_FUNCS -std=c++0x -D_GNU_SOURCE -DGB_INTERNAL

TARGET := sameboy.wbx

C_SRCS = $(shell find $(ROOT_DIR) -type f -name '*.c')
CPP_SRCS = $(shell find $(ROOT_DIR) -type f -name '*.cpp')
SRCS = $(C_SRCS) $(CPP_SRCS)

include ../common.mak
