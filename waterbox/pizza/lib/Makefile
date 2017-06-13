SRCS=$(wildcard *.c)
CFLAGS=-I.. -c -pthread -O3 -Wall

all: 
	gcc $(CFLAGS) $(SRCS)
	ar rcs libpizza.a *.o

clean: 
	rm -f *.o
