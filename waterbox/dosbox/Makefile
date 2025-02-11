CORE_DIR = libretro-uae
TARGET   = uae.wbx
EMU      = $(CORE_DIR)/sources/src
LIBRETRO = $(CORE_DIR)/libretro
DEPS_DIR = $(CORE_DIR)/deps
RETRODEP = $(CORE_DIR)/retrodep
LIBRETRO_COMM_DIR = $(CORE_DIR)/libretro-common

SOURCES_C += bizhawk.c

INCFLAGS = \
	-I$(EMU) \
	-I$(EMU)/include \
	-I$(CORE_DIR) \
	-I$(RETRODEP) \
	-I$(DEPS_DIR)/7zip \
	-I$(DEPS_DIR)/libmpeg2/include \
	-I$(LIBRETRO) \
	-I$(LIBRETRO_COMM_DIR)/include \
	-I$(LIBRETRO_COMM_DIR)/include/compat/zlib

LDFLAGS +=

CCFLAGS += \
	-std=gnu99 \
	-w -ferror-limit=0

CCFLAGS += $(INCFLAGS) \
	-D__LIBRETRO__ \
	-DINLINE="inline" \
	-MMD \
	-DHAVE_ZLIB \
	-DHAVE_7ZIP \
	-D_7ZIP_ST \
	-DWATERBOX \
	-DWITH_MPEG2

#CCFLAGS += \
	-DPICASSO96_SUPPORTED \

CXXFLAGS += $(INCFLAGS) -DUAE -MMD

SOURCES_C += \
    $(LIBRETRO)/libretro-core.c \
	$(LIBRETRO)/libretro-mapper.c \
	$(LIBRETRO)/libretro-dc.c \
	$(LIBRETRO)/libretro-glue.c \
	$(LIBRETRO)/libretro-vkbd.c \
	$(LIBRETRO)/libretro-graph.c \
	$(DEPS_DIR)/libz/unzip.c \
	$(DEPS_DIR)/libz/ioapi.c

SOURCES_C += \
	$(EMU)/a2065.c \
	$(EMU)/a2091.c \
	$(EMU)/akiko.c \
	$(EMU)/amax.c \
	$(EMU)/ar.c \
	$(EMU)/arcadia.c \
	$(EMU)/aros.rom.c \
	$(EMU)/audio.c \
	$(EMU)/autoconf.c \
	$(EMU)/blitfunc.c \
	$(EMU)/blittable.c \
	$(EMU)/blitter.c \
	$(EMU)/blkdev.c \
	$(EMU)/blkdev_cdimage.c \
	$(EMU)/bsdsocket.c \
	$(EMU)/calc.c \
	$(EMU)/caps/caps.c \
	$(EMU)/caps/uae_dlopen.c \
	$(EMU)/casablanca.c \
	$(EMU)/cd32_fmv.c \
	$(EMU)/cd32_fmv_genlock.c \
	$(EMU)/cda_play.c \
	$(EMU)/cdrom.c \
	$(EMU)/cdtv.c \
	$(EMU)/cdtvcr.c \
	$(EMU)/cfgfile.c \
	$(EMU)/cia.c \
	$(EMU)/cpuboard.c \
	$(EMU)/cpudefs.c \
	$(EMU)/cpuemu_0.c \
	$(EMU)/cpuemu_11.c \
	$(EMU)/cpuemu_13.c \
	$(EMU)/cpuemu_20.c \
	$(EMU)/cpuemu_21.c \
	$(EMU)/cpuemu_22.c \
	$(EMU)/cpuemu_23.c \
	$(EMU)/cpuemu_24.c \
	$(EMU)/cpuemu_31.c \
	$(EMU)/cpuemu_32.c \
	$(EMU)/cpuemu_33.c \
	$(EMU)/cpuemu_34.c \
	$(EMU)/cpuemu_35.c \
	$(EMU)/cpuemu_40.c \
	$(EMU)/cpuemu_50.c \
	$(EMU)/cpummu.c \
	$(EMU)/cpummu30.c \
	$(EMU)/cpustbl.c \
	$(EMU)/crc32.c \
	$(EMU)/custom.c \
	$(EMU)/debug.c \
	$(EMU)/debugmem.c \
	$(EMU)/devices.c \
	$(EMU)/disasm.c \
	$(EMU)/disk.c \
	$(EMU)/diskutil.c \
	$(EMU)/dongle.c \
	$(EMU)/drawing.c \
	$(EMU)/driveclick.c \
	$(EMU)/ethernet.c \
	$(EMU)/events.c \
	$(EMU)/expansion.c \
	$(EMU)/fdi2raw.c\
	$(EMU)/filesys.c \
	$(EMU)/filesys_unix.c \
	$(EMU)/flashrom.c \
	$(EMU)/fpp.c \
	$(EMU)/fpp_native.c \
	$(EMU)/fpp_softfloat.c \
	$(EMU)/fsdb.c \
	$(EMU)/fsdb_unix.c \
	$(EMU)/fsusage.c \
	$(EMU)/gayle.c \
	$(EMU)/gfxboard.c \
	$(EMU)/gfxlib.c \
	$(EMU)/gfxutil.c \
	$(EMU)/hardfile.c \
	$(EMU)/hardfile_unix.c \
	$(EMU)/hrtmon.rom.c \
	$(EMU)/ide.c \
	$(EMU)/idecontrollers.c \
	$(EMU)/identify.c \
	$(EMU)/ini.c \
	$(EMU)/inputdevice.c \
	$(EMU)/isofs.c \
	$(EMU)/keybuf.c \
	$(EMU)/main.c \
	$(EMU)/memory.c \
	$(EMU)/misc.c \
	$(EMU)/missing.c \
	$(EMU)/native2amiga.c \
	$(EMU)/ncr_scsi.c \
	$(EMU)/ncr9x_scsi.c \
	$(EMU)/newcpu.c \
	$(EMU)/newcpu_common.c \
	$(EMU)/pci.c \
	$(EMU)/picasso96.c \
	$(EMU)/readcpu.c \
	$(EMU)/rommgr.c \
	$(EMU)/rtc.c \
	$(EMU)/sampler.c \
	$(EMU)/sana2.c \
	$(EMU)/savestate.c \
	$(EMU)/scsi.c \
	$(EMU)/scsiemul.c \
	$(EMU)/scsitape.c \
	$(EMU)/sndboard.c \
	$(EMU)/specialmonitors.c \
	$(EMU)/statusline.c \
	$(EMU)/test_card.c\
	$(EMU)/traps.c\
	$(EMU)/uaelib.c \
	$(EMU)/uaenet.c \
	$(EMU)/uaeresource.c \
	$(EMU)/uaeserial.c \
	$(EMU)/writelog.c \
	$(EMU)/x86.c \
	$(EMU)/zfile.c \
	$(EMU)/zfile_archive.c \
	$(EMU)/softfloat/softfloat.c \
	$(EMU)/softfloat/softfloat_decimal.c \
	$(EMU)/softfloat/softfloat_fpsp.c

SOURCES_C += \
	$(RETRODEP)/gui.c \
	$(RETRODEP)/main.c \
	$(RETRODEP)/mman.c \
	$(RETRODEP)/parser.c \
	$(RETRODEP)/serial_host.c \
	$(RETRODEP)/machdep/support.c \
	$(RETRODEP)/sounddep/sound.c \
	$(RETRODEP)/stubs/inputrecord.c \
	$(RETRODEP)/threaddep/thread.c

SOURCES_C += \
    $(EMU)/archivers/dms/crc_csum.c \
	$(EMU)/archivers/dms/getbits.c \
	$(EMU)/archivers/dms/maketbl.c \
	$(EMU)/archivers/dms/pfile.c \
	$(EMU)/archivers/dms/tables.c \
	$(EMU)/archivers/dms/u_deep.c \
	$(EMU)/archivers/dms/u_heavy.c \
	$(EMU)/archivers/dms/u_init.c  \
	$(EMU)/archivers/dms/u_medium.c \
	$(EMU)/archivers/dms/u_quick.c \
	$(EMU)/archivers/dms/u_rle.c \
	$(EMU)/archivers/lha/crcio.c \
	$(EMU)/archivers/lha/dhuf.c \
	$(EMU)/archivers/lha/header.c \
	$(EMU)/archivers/lha/huf.c \
	$(EMU)/archivers/lha/larc.c \
	$(EMU)/archivers/lha/lhamaketbl.c \
	$(EMU)/archivers/lha/lharc.c \
	$(EMU)/archivers/lha/shuf.c \
	$(EMU)/archivers/lha/slide.c \
	$(EMU)/archivers/lha/uae_lha.c \
	$(EMU)/archivers/lha/util.c \
	$(EMU)/archivers/mp2/kjmp2.c

SOURCES_C += \
	$(DEPS_DIR)/libmpeg2/src/convert/rgb.c \
	$(DEPS_DIR)/libmpeg2/src/cpu_accel.c \
	$(DEPS_DIR)/libmpeg2/src/cpu_state.c \
	$(DEPS_DIR)/libmpeg2/src/alloc.c \
	$(DEPS_DIR)/libmpeg2/src/decode.c \
	$(DEPS_DIR)/libmpeg2/src/header.c \
	$(DEPS_DIR)/libmpeg2/src/idct.c \
	$(DEPS_DIR)/libmpeg2/src/motion_comp.c \
	$(DEPS_DIR)/libmpeg2/src/slice.c

SOURCES_C += \
	$(DEPS_DIR)/libz/adler32.c \
	$(DEPS_DIR)/libz/crc32.c \
	$(DEPS_DIR)/libz/deflate.c \
	$(DEPS_DIR)/libz/gzclose.c \
	$(DEPS_DIR)/libz/gzlib.c \
	$(DEPS_DIR)/libz/gzread.c \
	$(DEPS_DIR)/libz/gzwrite.c \
	$(DEPS_DIR)/libz/inffast.c \
	$(DEPS_DIR)/libz/inflate.c \
	$(DEPS_DIR)/libz/inftrees.c \
	$(DEPS_DIR)/libz/trees.c \
	$(DEPS_DIR)/libz/zutil.c

SOURCES_C += \
	$(DEPS_DIR)/7zip/7zArcIn.c \
	$(DEPS_DIR)/7zip/7zBuf.c \
	$(DEPS_DIR)/7zip/7zCrc.c \
	$(DEPS_DIR)/7zip/7zCrcOpt.c \
	$(DEPS_DIR)/7zip/7zDec.c \
	$(DEPS_DIR)/7zip/7zFile.c \
	$(DEPS_DIR)/7zip/7zStream.c \
	$(DEPS_DIR)/7zip/Bcj2.c \
	$(DEPS_DIR)/7zip/Bra.c \
	$(DEPS_DIR)/7zip/Bra86.c \
	$(DEPS_DIR)/7zip/BraIA64.c \
	$(DEPS_DIR)/7zip/CpuArch.c \
	$(DEPS_DIR)/7zip/Delta.c \
	$(DEPS_DIR)/7zip/Lzma2Dec.c \
	$(DEPS_DIR)/7zip/LzmaDec.c

SOURCES_C += \
	$(LIBRETRO_COMM_DIR)/compat/compat_strl.c \
	$(LIBRETRO_COMM_DIR)/compat/compat_strcasestr.c \
	$(LIBRETRO_COMM_DIR)/compat/fopen_utf8.c \
	$(LIBRETRO_COMM_DIR)/encodings/encoding_utf.c \
	$(LIBRETRO_COMM_DIR)/file/file_path.c \
	$(LIBRETRO_COMM_DIR)/file/file_path_io.c \
	$(LIBRETRO_COMM_DIR)/file/retro_dirent.c \
	$(LIBRETRO_COMM_DIR)/streams/file_stream.c \
	$(LIBRETRO_COMM_DIR)/streams/file_stream_transforms.c \
	$(LIBRETRO_COMM_DIR)/string/stdstring.c \
	$(LIBRETRO_COMM_DIR)/time/rtime.c \
	$(LIBRETRO_COMM_DIR)/vfs/vfs_implementation.c

SRCS = $(SOURCES_C)

include ../common.mak