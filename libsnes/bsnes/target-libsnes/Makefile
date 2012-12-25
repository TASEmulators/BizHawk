include $(snes)/Makefile
include $(gameboy)/Makefile
output := libsnes

ifeq ($(platform),x)
  flags += -fPIC
else ifeq ($(platform),osx)
  flags += -fPIC
else ifeq ($(platform),win)
endif

#rules
objects := $(objects) libsnes libsnes_pwrap
objects := $(patsubst %,obj/%.o,$(objects))

obj/libsnes.o: $(ui)/libsnes.cpp $(ui)/*
obj/libsnes_pwrap.o: $(ui)/libsnes_pwrap.cpp $(ui)/*

#targets
build: $(objects)
ifeq ($(platform),x)
	ar rcs out/libsnes.a $(objects)
	$(cpp) -o out/libsnes.so -shared -Wl,-soname,libsnes.so.1 $(objects)
else ifeq ($(platform),osx)
	ar rcs out/libsnes.a $(objects)
	$(cpp) -o out/libsnes.dylib -install_name @executable_path/../Libraries/libsnes.dylib -shared -dynamiclib $(objects)
else ifeq ($(platform),win)
	$(cpp) -o out/libsneshawk-$(profile).exe -Wl $(objects) $(TARGET_LIBSNES_LIBDEPS)
endif

install:
ifeq ($(platform),x)
	install -D -m 755 out/libsnes.a $(DESTDIR)$(prefix)/lib/libsnes.a
	install -D -m 755 out/libsnes.so $(DESTDIR)$(prefix)/lib/libsnes.so
	ldconfig -n $(DESTDIR)$(prefix)/lib
else ifeq ($(platform),osx)
	cp out/libsnes.dylib /usr/local/lib/libsnes.dylib
endif

uninstall:
ifeq ($(platform),x)
	rm $(DESTDIR)$(prefix)/lib/libsnes.a
	rm $(DESTDIR)$(prefix)/lib/libsnes.so
else ifeq ($(platform),osx)
	rm /usr/local/lib/libsnes.dylib
endif
