EXTRA_DIST = cd.h yabause.vcproj yabause_logo.bmp logo.ico yabause.sln gen68k.vcproj \
	68klib.vcproj inc.rules Installer
dist-hook:
	rm -rf `find $(distdir)/Installer -name CVS`
bin_PROGRAMS = yabause
yabause_SOURCES = \
	aviout.c bup-manager.c cheats.c cheats.h custctl.c custctl.h disasm.c \
	disasm.h dx.h hexedit.c hexedit.h hotkey.cpp hotkey.h resource.h resource.rc perdx.c \
        perdx.h ram_search.cpp ram_search.h ramwatch.cpp ramwatch.h snddx.c snddx.h yui.cpp yuiwin.h \
	settings/settings.c settings/settings.h \
        settings/settings-basic.c settings/settings-input.c settings/settings-log.c \
	settings/settings-netlink.c settings/settings-sound.c settings/settings-video.c \
	cpudebug/debug-68k.c cpudebug/debug-scsp.c cpudebug/debug-scu.c \
	cpudebug/debug-sh2.c cpudebug/debug-smpc.c cpudebug/debug-vdp1.c \
	cpudebug/debug-vdp2.c cpudebug/yuidebug.c cpudebug/yuidebug.h
	
yabause_LDFLAGS = -mwindows -mthreads
yabause_LDADD = ../libyabause.a resource.o -lmingw32 -lopengl32 -lglut32 -lkernel32 -lgdi32 -lcomctl32 -lcomdlg32 -lwinmm -ldxguid -ldinput8 -ldsound -ldxerr8 -lvfw32
yabause_CPPFLAGS = -DUNICODE -D_UNICODE -I@srcdir@/settings -I@srcdir@/cpudebug
resource.o: resource.rc resource.h
	$(WINDRES) $< -o $@ -I$(srcdir) $(DEFS)
