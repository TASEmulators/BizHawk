# initial configuration used in all waterbox makefiles
WATERBOX_DIR := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOT_DIR := $(shell dirname $(realpath $(lastword $(filter-out $(lastword $(MAKEFILE_LIST)), $(MAKEFILE_LIST)))))
OUTPUTDLL_DIR := $(shell realpath $(WATERBOX_DIR)/../output/dll)
OBJ_DIR := $(ROOT_DIR)/obj
EMULIBC_OBJS := $(WATERBOX_DIR)/emulibc/obj/emulibc.o

print-%: ;
	@echo $* = $($*)

.DEFAULT_GOAL := all

CC := $(WATERBOX_DIR)/musl/waterbox-sysroot/bin/musl-gcc
CCFLAGS := -fPIE -fvisibility=hidden -I$(WATERBOX_DIR)/emulibc -fno-exceptions -Wall
LDFLAGS := -static -Wl,--oformat=pe-x86-64
CXXFLAGS := ($CCFLAGS) -fno-rtti
