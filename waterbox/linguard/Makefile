ifeq (,$(findstring Linux,$(shell uname)))
$(error This must be built with linux)
endif

linguard.so: linguard.c
	gcc -Wall -O2 -s -o $@ $< -shared -fPIC

install: linguard.so
	cp $< ../../output
clean:
	rm linguard.so

.PHONY: install clean
