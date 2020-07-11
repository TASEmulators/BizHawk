all: test.txt

.PHONY: all

test: test.c Makefile
	gcc test.c -o test -Wall -O3 -fcf-protection=none

test.txt: test Makefile
	./test > test.txt
