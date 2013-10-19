EXTRA_DIST = c68k.h c68kmac.inc gen68k.h gen68k.inc
noinst_PROGRAMS = gen68k
gen68k_SOURCES = c68kexec.c c68k.c gen68k.c
gen68k_CFLAGS = -DC68K_GEN
gen68k_LDFLAGS = -DC68K_GEN

all-local: gen68k$(EXEEXT)
	./gen68k$(EXEEXT)

clean-local:
	-rm *_*.inc
