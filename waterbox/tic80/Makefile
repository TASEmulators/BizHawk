INCLUDE_FLAGS = -I./include/ -I./src/ -I./src/core/ -I./vendor/blip-buf/ -I./vendor/duktape/src/ \
	-I./vendor/fennel/ -I./vendor/giflib/ -I./vendor/lpeg/ -I./vendor/lua/ -I./vendor/moonscript/ \
	-I./vendor/squirrel/include/ -I./vendor/squirrel/sqstdlib/ -I./vendor/squirrel/squirrel/ \
	-I./vendor/wasm3/source/ -I./vendor/wren/src/include/ -I./vendor/wren/src/optional/ \
	-I./vendor/wren/src/vm/ -I./vendor/wren/src/vm/ -I./vendor/zlib/

CCFLAGS := -std=gnu11 -fno-strict-aliasing -fwrapv -DLUA_COMPAT_5_2 -DBUILD_DEPRECATED $(INCLUDE_FLAGS) \
	-Werror=int-to-pointer-cast -Wno-missing-braces -Wno-unused-variable -Wno-unused-but-set-variable -Wno-string-plus-int \
	-Wno-pointer-sign -Wno-unused-function -Wno-format -Wno-extern-initializer -Wno-deprecated-non-prototype

CXXFLAGS := -std=c++11 $(INCLUDE_FLAGS) -Wno-unused-but-set-variable -fno-strict-aliasing -fwrapv

TARGET = tic80.wbx

SRCS = $(shell find ./ -type f -name '*.c') $(shell find ./ -type f -name '*.cpp')

include ../common.mak
