#ifndef INSTANCE_H
#define INSTANCE_H

struct FrontEndSettings
{
	int cpuSaveType; // [0auto] 1eeprom 2sram 3flash 4eeprom+sensor 5none
	int	flashSize; // [0x10000] 0x20000
	int	enableRtc; // [false] true
	int mirroringEnable; // [false] true
	int skipBios; // [false] true

	int RTCuseRealTime;
	int RTCyear; // 00..99
	int RTCmonth; // 00..11
	int RTCmday; // 01..31
	int RTCwday; // 00..06
	int RTChour; // 00..23
	int RTCmin; // 00..59
	int RTCsec; // 00..59

};

struct MemoryAreas
{
	void *bios;
	void *iwram;
	void *ewram;
	void *palram;
	void *vram;
	void *oam;
	void *rom;
	void *mmio;
	void *sram;
	uint32_t sram_size;
};

#define FLASH_128K_SZ 0x20000

#define EEPROM_IDLE           0
#define EEPROM_READADDRESS    1
#define EEPROM_READDATA       2
#define EEPROM_READDATA2      3
#define EEPROM_WRITEDATA      4

enum {
	IMAGE_UNKNOWN,
	IMAGE_GBA
};

#define SGCNT0_H 0x82
#define FIFOA_L 0xa0
#define FIFOA_H 0xa2
#define FIFOB_L 0xa4
#define FIFOB_H 0xa6

#define BLIP_BUFFER_ACCURACY 16
#define BLIP_PHASE_BITS 8
#define BLIP_WIDEST_IMPULSE_ 16
#define BLIP_BUFFER_EXTRA_ 18
#define BLIP_RES 256
#define BLIP_RES_MIN_ONE 255
#define BLIP_SAMPLE_BITS 30
#define BLIP_READER_DEFAULT_BASS 9
#define BLIP_DEFAULT_LENGTH 250		/* 1/4th of a second */

#define BUFS_SIZE 3
#define STEREO 2

#define	CLK_MUL	GB_APU_OVERCLOCK
#define CLK_MUL_MUL_2 8
#define CLK_MUL_MUL_4 16
#define CLK_MUL_MUL_6 24
#define CLK_MUL_MUL_8 32
#define CLK_MUL_MUL_15 60
#define CLK_MUL_MUL_32 128
#define DAC_BIAS 7

#define PERIOD_MASK 0x70
#define SHIFT_MASK 0x07

#define PERIOD2_MASK 0x1FFFF

#define BANK40_MASK 0x40
#define BANK_SIZE 32
#define BANK_SIZE_MIN_ONE 31
#define BANK_SIZE_DIV_TWO 16

/* 11-bit frequency in NRx3 and NRx4*/
#define GB_OSC_FREQUENCY() (((regs[4] & 7) << 8) + regs[3])

#define	WAVE_TYPE	0x100
#define NOISE_TYPE	0x200
#define MIXED_TYPE	WAVE_TYPE | NOISE_TYPE
#define TYPE_INDEX_MASK	0xFF

#define BITS_16 0
#define BITS_32 1

#define R13_IRQ  18
#define R14_IRQ  19
#define SPSR_IRQ 20
#define R13_USR  26
#define R14_USR  27
#define R13_SVC  28
#define R14_SVC  29
#define SPSR_SVC 30
#define R13_ABT  31
#define R14_ABT  32
#define SPSR_ABT 33
#define R13_UND  34
#define R14_UND  35
#define SPSR_UND 36
#define R8_FIQ   37
#define R9_FIQ   38
#define R10_FIQ  39
#define R11_FIQ  40
#define R12_FIQ  41
#define R13_FIQ  42
#define R14_FIQ  43
#define SPSR_FIQ 44

typedef struct {
	uint8_t *address;
	uint32_t mask;
} memoryMap;

typedef union {
	struct {
#ifdef LSB_FIRST
		uint8_t B0;
		uint8_t B1;
		uint8_t B2;
		uint8_t B3;
#else
		uint8_t B3;
		uint8_t B2;
		uint8_t B1;
		uint8_t B0;
#endif
	} B;
	struct {
#ifdef LSB_FIRST
		uint16_t W0;
		uint16_t W1;
#else
		uint16_t W1;
		uint16_t W0;
#endif
	} W;
#ifdef LSB_FIRST
	uint32_t I;
#else
	volatile uint32_t I;
#endif
} reg_pair;

typedef struct 
{
	reg_pair reg[45];
	bool busPrefetch;
	bool busPrefetchEnable;
	uint32_t busPrefetchCount;
	uint32_t armNextPC;
} bus_t;

typedef struct
{
	uint8_t paletteRAM[0x400];
	int layerEnable;
	int layerEnableDelay;
	int lcdTicks;
} graphics_t;

/* Begins reading from buffer. Name should be unique to the current block.*/
#define BLIP_READER_BEGIN( name, blip_buffer ) \
        const int32_t * name##_reader_buf = (blip_buffer).buffer_;\
        int32_t name##_reader_accum = (blip_buffer).reader_accum_

/* Advances to next sample*/
#define BLIP_READER_NEXT( name, bass ) \
        (void) (name##_reader_accum += *name##_reader_buf++ - (name##_reader_accum >> (bass)))

/* Ends reading samples from buffer. The number of samples read must now be removed
   using Blip_Buffer::remove_samples(). */
#define BLIP_READER_END( name, blip_buffer ) \
        (void) ((blip_buffer).reader_accum_ = name##_reader_accum)

#define BLIP_READER_ADJ_( name, offset ) (name##_reader_buf += offset)

#define BLIP_READER_NEXT_IDX_( name, idx ) {\
        name##_reader_accum -= name##_reader_accum >> BLIP_READER_DEFAULT_BASS;\
        name##_reader_accum += name##_reader_buf [(idx)];\
}

#define BLIP_READER_NEXT_RAW_IDX_( name, idx ) {\
        name##_reader_accum -= name##_reader_accum >> BLIP_READER_DEFAULT_BASS; \
        name##_reader_accum += *(int32_t const*) ((char const*) name##_reader_buf + (idx)); \
}

#if defined (_M_IX86) || defined (_M_IA64) || defined (__i486__) || \
                defined (__x86_64__) || defined (__ia64__) || defined (__i386__)
        #define BLIP_CLAMP_( in ) in < -0x8000 || 0x7FFF < in
#else
        #define BLIP_CLAMP_( in ) (int16_t) in != in
#endif

/* Clamp sample to int16_t range */
#define BLIP_CLAMP( sample, out ) { if ( BLIP_CLAMP_( (sample) ) ) (out) = ((sample) >> 24) ^ 0x7FFF; }
#define GB_ENV_DAC_ENABLED() (regs[2] & 0xF8)	/* Non-zero if DAC is enabled*/
#define GB_NOISE_PERIOD2_INDEX()	(regs[3] >> 4)
#define GB_NOISE_PERIOD2(base)		(base << GB_NOISE_PERIOD2_INDEX())
#define GB_NOISE_LFSR_MASK()		((regs[3] & 0x08) ? ~0x4040 : ~0x4000)
#define GB_WAVE_DAC_ENABLED() (regs[0] & 0x80)	/* Non-zero if DAC is enabled*/

#define reload_sweep_timer() \
        sweep_delay = (regs [0] & PERIOD_MASK) >> 4; \
        if ( !sweep_delay ) \
                sweep_delay = 8;


#ifdef __LIBRETRO__
#define PIX_BUFFER_SCREEN_WIDTH 256
#else
#define PIX_BUFFER_SCREEN_WIDTH 240
#endif

#endif
