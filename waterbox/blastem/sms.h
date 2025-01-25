#ifndef SMS_H_
#define SMS_H_

#include "system.h"
#include "vdp.h"
#include "psg.h"
#ifdef NEW_CORE
#include "z80.h"
#else
#include "z80_to_x86.h"
#endif
#include "io.h"

#define SMS_RAM_SIZE (8*1024)
#define SMS_CART_RAM_SIZE (32*1024)

typedef struct {
	system_header header;
	z80_context   *z80;
	vdp_context   *vdp;
	psg_context   *psg;
	sega_io       io;
	uint8_t       *rom;
	uint32_t      rom_size;
	uint32_t      master_clock;
	uint32_t      normal_clock;
	uint8_t       should_return;
	uint8_t       ram[SMS_RAM_SIZE];
	uint8_t       bank_regs[4];
	uint8_t       cart_ram[SMS_CART_RAM_SIZE];
} sms_context;

sms_context *alloc_configure_sms(system_media *media, uint32_t opts, uint8_t force_region);

#endif //SMS_H_
