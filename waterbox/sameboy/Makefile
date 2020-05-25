FLAGS := -Wall -Werror=int-to-pointer-cast \
	-Wno-multichar \
	-fomit-frame-pointer -DGB_INTERNAL -D_GNU_SOURCE

CCFLAGS := $(FLAGS) \
	-std=gnu99 \
	-DLSB_FIRST -Werror=pointer-to-int-cast -Werror=implicit-function-declaration

CXXFLAGS := $(FLAGS) -DSPC_NO_COPY_STATE_FUNCS -std=c++0x -fno-exceptions -fno-rtti

TARGET := sameboy.wbx

C_SRCS = $(shell find $(ROOT_DIR) -type f -name '*.c')
CPP_SRCS = $(shell find $(ROOT_DIR) -type f -name '*.cpp')
SRCS = $(C_SRCS) $(CPP_SRCS)

include ../common.mak
