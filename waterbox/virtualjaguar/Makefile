COMMON_FLAGS := -fno-strict-aliasing -fwrapv -I./src/ -I./src/m68000 \
	-Werror=int-to-pointer-cast -Wno-unused-variable -Wno-cpp \
	-Wno-unused-but-set-variable -Wno-return-type -Wno-misleading-indentation \
	-Wno-parentheses -Wno-unused-label -Wfatal-errors

CCFLAGS := -std=c11 $(COMMON_FLAGS) -Wno-implicit-function-declaration

CXXFLAGS := -std=c++17 $(COMMON_FLAGS)

TARGET = virtualjaguar.wbx

SRCS = $(shell find ./ -type f -name '*.c') $(shell find ./ -type f -name '*.cpp')

include ../common.mak
