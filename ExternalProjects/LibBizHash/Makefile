CC = gcc

CFLAGS = -Wall -Wextra -O3 -flto -s -fvisibility=internal -fPIC
LFLAGS = -shared

SRCS = $(wildcard ./*.c) $(wildcard x86/*.c)

ifeq ($(OS),Windows_NT)
EXT = dll
else
EXT = so
endif

all: libbizhash

libbizhash: $(SRCS)
	$(CC) $(CFLAGS) $(SRCS) -o ../../Assets/dll/libbizhash.$(EXT) $(LFLAGS)
