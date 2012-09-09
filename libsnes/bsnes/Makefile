include nall/Makefile

nes := nes
snes := snes
gameboy := gameboy
profile := accuracy
target := ui

# options += console

# compiler
c       := $(compiler) -std=gnu99 $(BIZWINCFLAGS)
cpp     := $(subst cc,++,$(compiler)) -std=gnu++0x
flags   := -I. -O3 -fomit-frame-pointer
link    :=
objects := libco

ifneq ($(BIZWINCFLAGS),)
  flags := $(BIZWINCFLAGS)
endif

# profile-guided optimization mode
# pgo := instrument
# pgo := optimize

ifeq ($(pgo),instrument)
  flags += -fprofile-generate
  link += -lgcov
else ifeq ($(pgo),optimize)
  flags += -fprofile-use
endif

# platform
ifeq ($(platform),x)
  link += -s -ldl -lX11 -lXext
else ifeq ($(platform),osx)
else ifeq ($(platform),win)
  link += $(if $(findstring console,$(options)),-mconsole,-mwindows)
  link += -mthreads -s -luuid -lkernel32 -luser32 -lgdi32 -lcomctl32 -lcomdlg32 -lshell32 -lole32
  link += -Wl,-enable-auto-import -Wl,-enable-runtime-pseudo-reloc
else
  unknown_platform: help;
endif

ui := target-$(target)

# implicit rules
compile = \
  $(strip \
    $(if $(filter %.c,$<), \
      $(c) $(flags) $1 -c $< -o $@, \
      $(if $(filter %.cpp,$<), \
        $(cpp) $(flags) $1 -c $< -o $@ \
      ) \
    ) \
  )

%.o: $<; $(call compile)

all: build;

obj/libco.o: libco/libco.c libco/*

include $(ui)/Makefile
flags := $(flags) $(foreach o,$(call strupper,$(options)),-D$o)

# targets
clean:
	-@$(call delete,obj/*.o)
	-@$(call delete,obj/*.a)
	-@$(call delete,obj/*.so)
	-@$(call delete,obj/*.dylib)
	-@$(call delete,obj/*.dll)
	-@$(call delete,*.res)
	-@$(call delete,*.pgd)
	-@$(call delete,*.pgc)
	-@$(call delete,*.ilk)
	-@$(call delete,*.pdb)
	-@$(call delete,*.manifest)

sync:
	if [ -d ./libco ]; then rm -r ./libco; fi
	if [ -d ./nall ]; then rm -r ./nall; fi
	if [ -d ./ruby ]; then rm -r ./ruby; fi
	if [ -d ./phoenix ]; then rm -r ./phoenix; fi
	cp -r ../libco ./libco
	cp -r ../nall ./nall
	cp -r ../ruby ./ruby
	cp -r ../phoenix ./phoenix
	rm -r libco/doc
	rm -r libco/test
	rm -r nall/test
	rm -r ruby/_test
	rm -r phoenix/nall
	rm -r phoenix/test

archive-all:
	tar -cjf bsnes.tar.bz2 base data gameboy libco nall nes obj out phoenix ruby snes target-debugger target-libsnes target-ui Makefile cc.bat purge.bat

help:;
