CFLAGS=-O3
ifeq ($(OS),Windows_NT)
    LIBS=-lrt `sdl2-config --libs`
    CFLAGS+=-w 
else
    LIBS=-lrt -lSDL2 -pthread
endif

all: libpizza.a
	gcc $(CFLAGS) pizza.c -I lib lib/libpizza.a -o emu-pizza $(LIBS)

libpizza.a:
	make -C lib

clean: 
	rm -f *.o
	make -C cpu clean
	make -C system clean
