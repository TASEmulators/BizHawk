NEED_LIBCO := 1

CCFLAGS := -I. \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer \
	-falign-functions=16 \
	-DX86_64 -DLSB_FIRST -DIS_LIB -DHAVE_UNISTD_H -D_BSD_SOURCE

MEM:=mem.c
NET:=net.c
TERMINAL:=terminal.c
CONFIGOBJS=config.c util.c paths.c
Z80OBJS=z80inst.c z80_to_x86.c
M68KOBJS= 68kinst.c m68k_core.c m68k_core_x86.c
TRANSOBJS=gen.c backend.c $(MEM) arena.c tern.c gen_x86.c backend_x86.c
AUDIOOBJS=ym2612.c psg.c wave.c vgm.c event_log.c render_audio.c
LIBZOBJS:=zlib/adler32.c zlib/compress.c zlib/crc32.c zlib/deflate.c zlib/gzclose.c zlib/gzlib.c zlib/gzread.c\
	zlib/gzwrite.c zlib/infback.c zlib/inffast.c zlib/inflate.c zlib/inftrees.c zlib/trees.c zlib/uncompr.c zlib/zutil.c

SRCS=bizhawk.c system.c genesis.c debug.c gdb_remote.c vdp.c io.c romdb.c hash.c xband.c realtec.c \
	i2c.c nor.c sega_mapper.c multi_game.c megawifi.c $(NET) serialize.c $(TERMINAL) $(CONFIGOBJS) gst.c \
	sms.c $(Z80OBJS) $(M68KOBJS) $(TRANSOBJS) $(AUDIOOBJS) saves.c jcart.c gen_player.c $(LIBZOBJS) rom.db.c

TARGET := libblastem.wbx

%.db.c : %.db
	sed $< -e 's/"/\\"/g' -e 's/^\(.*\)$$/"\1\\n"/' -e'1s/^\(.*\)$$/const char $(shell echo $< | tr '.' '_')_data[] = \1/' -e '$$s/^\(.*\)$$/\1;/' > $@

include ../common.mak
