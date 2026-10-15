CC = gcc

CFLAGS = -Wall -Wextra -O3 -flto -fvisibility=internal -fPIC -Icommon
LFLAGS = -s -shared
ARCHFLAGS =

SRCS = $(wildcard common/*.c) $(wildcard crc32/*.c) $(wildcard sha1/*.c) bizinterface.c

ifeq ($(OS),Windows_NT)
EXT = dll
else ifeq ($(shell uname -s),Darwin)
EXT = dylib
ARCHFLAGS = -arch x86_64 -mmacosx-version-min=11.0
LFLAGS = -dynamiclib -Wl,-install_name,@rpath/libbizhash.dylib
else
EXT = so
endif

all: libbizhash

libbizhash: $(SRCS)
	$(CC) $(CFLAGS) $(ARCHFLAGS) $(SRCS) -o ../../Assets/dll/libbizhash.$(EXT) $(LFLAGS) $(ARCHFLAGS)
