GPGX_DIR := Genesis-Plus-GX

CCFLAGS := 	-Iutil \
    -I${GPGX_DIR}/core \
	-I${GPGX_DIR}/core/m68k \
	-I${GPGX_DIR}/core/z80 \
	-I${GPGX_DIR}/core/input_hw \
	-I${GPGX_DIR}/core/cart_hw \
	-I${GPGX_DIR}/core/cart_hw/svp \
	-I${GPGX_DIR}/core/sound \
	-I${GPGX_DIR}/core/ntsc \
	-I${GPGX_DIR}/core/cd_hw \
	-I${GPGX_DIR}/core/debug \
	-Icinterface \
	-Iutil \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer -Wfatal-errors \
	-DUSE_BIZHAWK_CALLBACKS \
	-DHAVE_YM3438_CORE \
	-DHAVE_OPLL_CORE \
	-DUSE_RAM_DEEPFREEZE \
	-DLSB_FIRST \
	-DUSE_32BPP_RENDERING \
	-DHOOK_CPU \
	-DINLINE=static\ __inline__ \
	-DcdStream=cdStream \
	-fcommon

LDFLAGS :=

TARGET := gpgx.wbx

SRCS = $(GPGX_DIR)/core/sound/sound.c \
	   $(GPGX_DIR)/core/sound/ym3438.c \
	   $(GPGX_DIR)/core/sound/opll.c \
	   $(GPGX_DIR)/core/sound/eq.c \
	   $(GPGX_DIR)/core/sound/ym2413.c \
	   $(GPGX_DIR)/core/sound/blip_buf.c \
	   $(GPGX_DIR)/core/sound/psg.c \
	   $(GPGX_DIR)/core/sound/ym2612.c \
	   $(GPGX_DIR)/core/membnk.c \
	   $(GPGX_DIR)/core/vdp_ctrl.c \
	   $(GPGX_DIR)/core/z80/z80.c \
	   $(GPGX_DIR)/core/io_ctrl.c \
	   $(GPGX_DIR)/core/ntsc/sms_ntsc.c \
	   $(GPGX_DIR)/core/ntsc/md_ntsc.c \
	   $(GPGX_DIR)/core/m68k/s68kcpu.c \
	   $(GPGX_DIR)/core/m68k/m68kcpu.c \
	   $(GPGX_DIR)/core/memz80.c \
	   $(GPGX_DIR)/core/genesis.c \
	   $(GPGX_DIR)/core/vdp_render.c \
	   $(GPGX_DIR)/core/system.c \
	   $(GPGX_DIR)/core/cd_hw/cd_cart.c \
	   $(GPGX_DIR)/core/cd_hw/cdc.c \
	   $(GPGX_DIR)/core/cd_hw/pcm.c \
	   $(GPGX_DIR)/core/cd_hw/gfx.c \
	   $(GPGX_DIR)/core/cd_hw/scd.c \
	   $(GPGX_DIR)/core/cd_hw/cdd.c \
	   $(GPGX_DIR)/core/input_hw/sportspad.c \
	   $(GPGX_DIR)/core/input_hw/activator.c \
	   $(GPGX_DIR)/core/input_hw/mouse.c \
	   $(GPGX_DIR)/core/input_hw/paddle.c \
	   $(GPGX_DIR)/core/input_hw/gamepad.c \
	   $(GPGX_DIR)/core/input_hw/input.c \
	   $(GPGX_DIR)/core/input_hw/terebi_oekaki.c \
	   $(GPGX_DIR)/core/input_hw/teamplayer.c \
	   $(GPGX_DIR)/core/input_hw/graphic_board.c \
	   $(GPGX_DIR)/core/input_hw/lightgun.c \
	   $(GPGX_DIR)/core/input_hw/xe_1ap.c \
	   $(GPGX_DIR)/core/mem68k.c \
	   $(GPGX_DIR)/core/cart_hw/svp/ssp16.c \
	   $(GPGX_DIR)/core/cart_hw/svp/svp.c \
	   $(GPGX_DIR)/core/cart_hw/areplay.c \
	   $(GPGX_DIR)/core/cart_hw/eeprom_spi.c \
	   $(GPGX_DIR)/core/cart_hw/sram.c \
	   $(GPGX_DIR)/core/cart_hw/eeprom_93c.c \
	   $(GPGX_DIR)/core/cart_hw/sms_cart.c \
	   $(GPGX_DIR)/core/cart_hw/eeprom_i2c.c \
	   $(GPGX_DIR)/core/cart_hw/ggenie.c \
	   $(GPGX_DIR)/core/cart_hw/md_cart.c \
	   $(GPGX_DIR)/core/cart_hw/megasd.c \
	   $(GPGX_DIR)/core/debug/cpuhook.c \
	   $(GPGX_DIR)/core/loadrom.c \
	   cinterface/cdStreamImpl.c \
	   cinterface/cinterface.c \
	   util/scrc32.c

include ../common.mak
