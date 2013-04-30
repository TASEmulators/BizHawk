EXTRA_DIST = icache-funcs-2450.patch me-sectstart.c me-sectend.c \
             psp-logo.png sh2-core.i
CLEANFILES = me.elf me.prx me-exp.c me-test.elf me-test.prx melib.S \
             yabause.elf yabause.prx yabause.sfo EBOOT.PBP

# yabause.{elf,prx} are only intermediate targets; the final target is
# EBOOT.PBP (but we keep the ELF/PRX files around for testing purposes).
if BUILD_ME_TEST
me_test=me-test.prx
else
me_test=
endif
all-local: EBOOT.PBP me.prx $(me_test)
.SECONDARY: yabause.elf yabause.prx me.elf me-test.elf

########

noinst_PROGRAMS = yabause.elf me.elf me-test.elf

yabause_elf_SOURCES = config.c control.c display.c filesel.c font.c gu.c \
                      init.c localtime.c main.c menu.c misc.c osk.c profile.c \
                      psp-cd.c psp-m68k.c psp-per.c psp-sh2.c psp-sound.c \
                      psp-video.c psp-video-bitmap.c psp-video-rotate.c \
                      psp-video-tilemap.c psp-video-tweaks.c \
                      rtl.c rtl-mips.c rtlexec.c rtlinsn.c rtlopt.c rtlunit.c \
                      satopt-sh2.c \
                      sh2.c sh2-interpret.c sh2-opcodeinfo.c sh2-optimize.c \
                      sys.c texcache.c threads.c timing.c yui.c \
                      me-utility.c \
                      \
                      common.h config.h control.h display.h filesel.h font.h \
                      gu.h init.h localtime.h menu.h misc.h osk.h profile.h \
                      psp-cd.h psp-m68k.h psp-per.h psp-sh2.h psp-sound.h \
                      psp-video.h psp-video-internal.h \
                      rtl.h rtl-internal.h rtl-mips.h \
                      satopt-sh2.h \
                      sh2.h sh2-internal.h \
                      sys.h texcache.h timing.h \
                      me-utility.h
yabause_elf_DEPENDENCIES = me-sectstart.$(OBJEXT) me-sectend.$(OBJEXT)
nodist_yabause_elf_SOURCES = melib.S 
yabause_elf_CFLAGS = $(YAB_CFLAGS) -Wpointer-arith -Wshadow
yabause_elf_LDADD = ../libyabause.a $(YAB_LIBS) -lpspsdk
yabause.elf: $(yabause_elf_OBJECTS) $(yabause_elf_DEPENDENCIES) ../libyabause.a
	$(yabause_elf_LINK) \
		me-sectstart.$(OBJEXT) \
		$(yabause_elf_OBJECTS) \
		$(yabause_elf_LDADD) \
		$(LIBS) \
		me-sectend.$(OBJEXT)

me_elf_SOURCES = me.c me.h
nodist_me_elf_SOURCES = me-exp.c
me_elf_CFLAGS = -Wpointer-arith -Wshadow
me_elf_LDFLAGS = -nostartfiles
me_elf_LIBS = -lpspkernel
me.elf: $(me_elf_OBJECTS)
	$(me_elf_LINK) $(me_elf_OBJECTS) $(me_elf_LDADD) $(me_elf_LIBS)

me_test_elf_SOURCES = me-test.c me-utility.c me-utility.h
nodist_me_test_elf_SOURCES = melib.S
me_test_elf_CFLAGS = -Wpointer-arith -Wshadow
me_test_elf_LIBS = -lc -lpspuser
me-test.elf: $(me_test_elf_OBJECTS)
	$(me_test_elf_LINK) $(me_test_elf_OBJECTS) $(me_test_elf_LDADD) $(me_test_elf_LIBS)

########

.elf.prx:
	psp-fixup-imports $<
	psp-prxgen $< $@

me-exp.c: me.exp
	psp-build-exports -b $< >$@
melib.S: me.exp
	psp-build-exports -s $<

yabause.sfo:
	mksfoex -d MEMSIZE=1 "Yabause $(PACKAGE_VERSION)" $@

EBOOT.PBP: psp-logo.png yabause.prx yabause.sfo
	pack-pbp $@ yabause.sfo $< NULL NULL NULL NULL yabause.prx NULL
