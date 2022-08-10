CC = gcc

CFLAGS = -Wall -Wextra -O3 -flto -fvisibility=internal -fPIC -Icommon
LFLAGS = -s -shared

SRCS = $(wildcard common/*.c) $(wildcard crc32/*.c) $(wildcard sha1/*.c) bizinterface.c

ifeq ($(OS),Windows_NT)
EXT = dll
else
EXT = so
endif

all: libbizhash

libbizhash: $(SRCS)
	$(CC) $(CFLAGS) $(SRCS) -o ../../Assets/dll/libbizhash.$(EXT) $(LFLAGS)
