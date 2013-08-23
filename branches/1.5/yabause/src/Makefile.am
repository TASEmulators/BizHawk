EXTRA_DIST = Makefile.dc Makefile.mng logo.bmp logo.png Makefile.wii logo.svg

if COMPILE_C68K
SUBDIRS = c68k . $(YUI_SUBDIR)
else
SUBDIRS = . $(YUI_SUBDIR)
endif
if YUI_IS_CARBON
YUI_SUBDIR=carbon
endif
if YUI_IS_DREAMCAST
YUI_SUBDIR=dreamcast
endif
if YUI_IS_GTK
YUI_SUBDIR=gtk
endif
if YUI_IS_PSP
YUI_SUBDIR=psp
endif
if YUI_IS_QT
YUI_SUBDIR=qt
endif
if YUI_IS_WII
YUI_SUBDIR=wii
endif
if YUI_IS_WINDOWS
YUI_SUBDIR=windows
endif

noinst_LIBRARIES = libyabause.a
libyabause_a_SOURCES = \
        bios.c bios.h cdbase.c cdbase.h cheat.c cheat.h coffelf.c coffelf.h \
        core.h cs0.c cs0.h cs1.c cs1.h cs2.c cs2.h \
	debug.c debug.h error.c error.h memory.c memory.h \
	m68kcore.c m68kcore.h m68kd.c m68kd.h movie.c movie.h \
	netlink.c netlink.h \
	osdcore.c osdcore.h \
	peripheral.h peripheral.c \
	persdljoy.c persdljoy.h profile.c profile.h \
	scu.c scu.h sh2core.c sh2core.h sh2d.c sh2d.h \
	sh2idle.c sh2idle.h sh2int.c sh2int.h sh2trace.c sh2trace.h \
	smpc.c smpc.h sndal.c sndal.h snddummy.c sndsdl.c sndsdl.h sndwav.c \
	threads.h titan/titan.c titan/titan.h vdp1.c vdp1.h vdp2.c vdp2.h \
	vdp2debug.c vdp2debug.h vidogl.c vidogl.h vidshared.c vidshared.h \
	vidsoft.c vidsoft.h yabause.c yabause.h ygl.h ygl.c yglshader.c yui.h

if USE_SCSP2
libyabause_a_SOURCES += scsp2.c scsp2.h
else
libyabause_a_SOURCES += scsp.c scsp.h
endif

if COMPILE_C68K
libyabause_a_SOURCES += c68k/c68kexec.c c68k/c68k.c c68k/gen68k.c m68kc68k.c m68kc68k.h
c68kexec.o: c68k/c68kexec.c
	$(COMPILE) -Ic68k -O0 $(srcdir)/c68k/c68kexec.c -c -o c68kexec.o
endif

if COMPILE_Q68
libyabause_a_SOURCES += m68kq68.c q68/q68.c q68/q68-core.c q68/q68-disasm.c \
	q68/q68-const.h q68/q68.h q68/q68-internal.h q68/q68-jit.h q68/q68-jit-psp.h q68/q68-jit-x86.h
if Q68_USE_JIT
libyabause_a_SOURCES += q68/q68-jit.c
if CPU_IS_X86
libyabause_a_SOURCES += q68/q68-jit-x86.S
endif
if CPU_IS_X64
libyabause_a_SOURCES += q68/q68-jit-x86.S
endif
if CPU_IS_PSP
libyabause_a_SOURCES += q68/q68-jit-psp.S
endif
endif
endif

if ARCH_IS_FREEBSD
libyabause_a_SOURCES += cd-freebsd.c thr-dummy.c
endif
if ARCH_IS_LINUX
libyabause_a_SOURCES += cd-linux.c perlinuxjoy.c perlinuxjoy.h thr-linux.c
if USE_DYNAREC
if CPU_IS_X64
libyabause_a_SOURCES += sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_x64.s
AM_CFLAGS = -DSH2_DYNAREC=1
endif
if CPU_IS_X86
libyabause_a_SOURCES += sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_x86.s
AM_CFLAGS = -DSH2_DYNAREC=1
endif
if CPU_IS_ARM
libyabause_a_SOURCES += sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_arm.s
AM_CFLAGS = -DSH2_DYNAREC=1 -mcpu=cortex-a8 -mfpu=vfp -mfloat-abi=softfp
endif
endif
endif
if ARCH_IS_MACOSX
libyabause_a_SOURCES += cd-macosx.c macjoy.c macjoy.h permacjoy.c permacjoy.h thr-dummy.c
endif
if ARCH_IS_NETBSD
libyabause_a_SOURCES += cd-netbsd.c thr-dummy.c
endif
if ARCH_IS_WINDOWS
libyabause_a_SOURCES += cd-windows.c thr-dummy.c
endif
if YUI_IS_DREAMCAST
libyabause_a_SOURCES += thr-dummy.c
endif
if YUI_IS_WII
libyabause_a_SOURCES += thr-dummy.c
endif

if TEST_PSP_SH2
libyabause_a_SOURCES += psp/psp-sh2.c psp/rtl.c psp/rtlexec.c psp/rtlinsn.c \
                        psp/rtlopt.c psp/rtlunit.c psp/satopt-sh2.c psp/sh2.c \
                        psp/sh2-interpret.c psp/sh2-opcodeinfo.c \
                        psp/sh2-optimize.c 
endif
