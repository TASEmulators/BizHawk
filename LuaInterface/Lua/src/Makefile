SRC=lapi.c lauxlib.c lbaselib.c lcode.c ldblib.c ldebug.c ldo.c ldump.c lfunc.c lgc.c linit.c liolib.c llex.c lmathlib.c lmem.c loadlib.c lobject.c lopcodes.c loslib.c lparser.c lstate.c lstring.c lstrlib.c ltable.c ltablib.c ltm.c lundump.c lvm.c lzio.c print.c LuaDLL.cpp

CC=clang
CXX=clang++

OBJ=$(addprefix build/,$(SRC:=.o))

all: build/libLuaWrap.dylib

build/%.cpp.o: %.cpp
	$(CXX) -m32 -std=c++11 -c -I$(dir $<) -o $@ $<

build/Lua511_wrap.cxx.o: wrapper/Lua511_wrap.cxx
	$(CXX) -m32 -std=c++11 -c -I$(dir $<) -o $@ $<

build/%.c.o: %.c
	$(CC) -m32 -c -o $@ $<

build/libLuaWrap.dylib: $(OBJ) build/Lua511_wrap.cxx.o
	$(CXX) -m32 -shared -o $@ $^

clean:
	rm -f build/*

