ifeq (,$(findstring MINGW,$(shell uname)))
$(error This must be built with mingw)
endif

winguard.dll: winguard.c
	gcc -O2 -s -o $@ $< -shared

install: winguard.dll
	cp $< ../../output/dll
clean:
	rm winguard.dll

.PHONY: install clean
