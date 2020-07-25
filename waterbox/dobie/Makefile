CXXFLAGS := \
	-Wall -Werror=int-to-pointer-cast \
	-std=c++14 -fomit-frame-pointer -fno-rtti \
	-Wno-reorder \
	-Idobiestation/ext/libdeflate
CCFLAGS := \
	-Wall -Wundef -Wpedantic -Wdeclaration-after-statement -Wmissing-prototypes -Wstrict-prototypes -Wvla \
	-D_ANSI_SOURCE \
	-Idobiestation/ext/libdeflate \
	-Idobiestation/ext/libdeflate/common

TARGET = dobie.wbx

CORE_SRCS = \
	audio/utils.cpp \
	ee/bios_hle.cpp \
	ee/cop0.cpp \
	ee/cop1.cpp \
	ee/cop2.cpp \
	ee/dmac.cpp \
	ee/ee_jit.cpp \
	ee/ee_jit64.cpp \
	ee/ee_jit64_cop2.cpp \
	ee/ee_jit64_fpu.cpp \
	ee/ee_jit64_fpu_avx.cpp \
	ee/ee_jit64_gpr.cpp \
	ee/ee_jit64_mmi.cpp \
	ee/ee_jittrans.cpp \
	ee/emotion.cpp \
	ee/emotion_fpu.cpp \
	ee/emotion_mmi.cpp \
	ee/emotion_special.cpp \
	ee/emotion_vu0.cpp \
	ee/emotionasm.cpp \
	ee/emotiondisasm.cpp \
	ee/emotioninterpreter.cpp \
	ee/intc.cpp \
	ee/ipu/chromtable.cpp \
	ee/ipu/codedblockpattern.cpp \
	ee/ipu/dct_coeff.cpp \
	ee/ipu/dct_coeff_table0.cpp \
	ee/ipu/dct_coeff_table1.cpp \
	ee/ipu/ipu.cpp \
	ee/ipu/ipu_fifo.cpp \
	ee/ipu/lumtable.cpp \
	ee/ipu/mac_addr_inc.cpp \
	ee/ipu/mac_b_pic.cpp \
	ee/ipu/mac_i_pic.cpp \
	ee/ipu/mac_p_pic.cpp \
	ee/ipu/motioncode.cpp \
	ee/ipu/vlc_table.cpp \
	ee/timers.cpp \
	ee/vif.cpp \
	ee/vu.cpp \
	ee/vu_disasm.cpp \
	ee/vu_interpreter.cpp \
	ee/vu_jit.cpp \
	ee/vu_jit64.cpp \
	ee/vu_jittrans.cpp \
	emulator.cpp \
	errors.cpp \
	gif.cpp \
	gs.cpp \
	gscontext.cpp \
	gsmem.cpp \
	gsregisters.cpp \
	gsthread.cpp \
	iop/cdvd/bincuereader.cpp \
	iop/cdvd/cdvd.cpp \
	iop/cdvd/cso_reader.cpp \
	iop/cdvd/iso_reader.cpp \
	iop/firewire.cpp \
	iop/gamepad.cpp \
	iop/iop.cpp \
	iop/iop_cop0.cpp \
	iop/iop_dma.cpp \
	iop/iop_intc.cpp \
	iop/iop_interpreter.cpp \
	iop/iop_timers.cpp \
	iop/memcard.cpp \
	iop/sio2.cpp \
	iop/spu/spu.cpp \
	iop/spu/spu_adpcm.cpp \
	iop/spu/spu_envelope.cpp \
	iop/spu/spu_tables.cpp \
	jitcommon/emitter64.cpp \
	jitcommon/ir_block.cpp \
	jitcommon/ir_instr.cpp \
	jitcommon/jitcache.cpp \
	scheduler.cpp \
	serialize.cpp \
	sif.cpp \
	tests/iop/alu.cpp

DEFLATE_SRCS = \
	lib/aligned_malloc.c \
	lib/deflate_decompress.c \
	lib/x86/cpu_features.c

SRCS = \
	$(addprefix dobiestation/src/core/,$(CORE_SRCS)) \
	$(addprefix dobiestation/ext/libdeflate/,$(DEFLATE_SRCS)) \
	cinterface.cpp

include ../common.mak
