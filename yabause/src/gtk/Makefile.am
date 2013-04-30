SUBDIRS = doc
Applicationsdir = $(datadir)/applications
pixmapdir = $(datadir)/pixmaps
pixmap_DATA = yabause.png
bin_PROGRAMS = yabause
EXTRA_DIST = yabause.png

yabause_SOURCES = gtkglwidget.c gtkglwidget.h main.c settings.c menu.c gtk-compat.c gtk-compat.h \
	yuifileentry.c yuifileentry.h yuirange.c yuirange.h yuiinputentry.c yuiinputentry.h yuipage.c yuipage.h \
	yuiresolution.c yuiresolution.h yuiwindow.c yuiwindow.h yuivdp1.c yuivdp1.h yuivdp2.c yuivdp2.h \
	yuiscsp.c yuiscsp.h pergtk.c pergtk.h yuiscreenshot.c yuiscreenshot.h \
	yuish.c yuish.h yuitransfer.c yuitransfer.h yuim68k.c yuim68k.h yuiscudsp.c yuiscudsp.h yuimem.c yuimem.h \
	yuiviewer.c yuiviewer.h settings.h \
	yuicheckbutton.c yuicheckbutton.h
yabause_CFLAGS = $(YAB_CFLAGS)
yabause_LDADD = ../libyabause.a $(YAB_LIBS)
yabause_CPPFLAGS = -DYTSDIR=\"$(datadir)/$(PACKAGE)/yts\"

if ARCH_IS_LINUX
if USE_DYNAREC
if CPU_IS_X64
yabause_CFLAGS += -DSH2_DYNAREC=1
endif
if CPU_IS_X86
yabause_CFLAGS += -DSH2_DYNAREC=1
endif
if CPU_IS_ARM
yabause_CFLAGS += -DSH2_DYNAREC=1 -mcpu=cortex-a8 -mfpu=vfp -mfloat-abi=softfp
endif
endif
endif
