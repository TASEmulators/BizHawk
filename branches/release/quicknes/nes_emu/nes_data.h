
// NES data file block formats

// Nes_Emu 0.7.0

#ifndef NES_DATA_H
#define NES_DATA_H

#include "blargg_common.h"
#include "apu_state.h"

typedef long nes_tag_t;

#if 'ABCD' == '\101\102\103\104'
#define FOUR_CHAR( c ) (\
	((c) / '\1\0\0\0' % 0x100 * 0x01000000L) +\
	((c) / '\0\1\0\0' % 0x100 * 0x00010000L) +\
	((c) / '\0\0\1\0' % 0x100 * 0x00000100L) +\
	((c) / '\0\0\0\1' % 0x100 * 0x00000001L)\
)
#else
#if 'ABCD' == 0x41424344
#define FOUR_CHAR( c ) c
#else
#define FOUR_CHAR( c ) (\
	((c) / 0x01000000 % 0x100 * 0x00000001) +\
	((c) / 0x00010000 % 0x100 * 0x00000100) +\
	((c) / 0x00000100 % 0x100 * 0x00010000) +\
	((c) / 0x00000001 % 0x100 * 0x01000000)\
)
#endif
#endif

typedef BOOST::uint8_t byte;

// Binary format of save state blocks. All multi-byte values are stored in little-endian.

nes_tag_t const state_file_tag = FOUR_CHAR('NESS');

nes_tag_t const movie_file_tag = FOUR_CHAR('NMOV');

// Name of cartridge file in 8-bit characters (UTF-8 preferred) with ".nes" etc *removed*,
// no NUL termination. Yes: "Castlevania (U)". No: "Strider (U).nes".
nes_tag_t const cart_name_tag = FOUR_CHAR('romn');

// CRC-32 of cartridge's PRG and CHR data combined
nes_tag_t const cart_checksum_tag = FOUR_CHAR('csum');

struct nes_block_t
{
	BOOST::uint32_t tag; // ** stored in big-endian
	BOOST::uint32_t size;
	
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (nes_block_t) == 8 );

unsigned long const group_begin_size = 0xffffffff; // group block has this size
nes_tag_t const group_end_tag = FOUR_CHAR('gend'); // group end block has this tag

struct movie_info_t
{
	BOOST::uint32_t begin;
	BOOST::uint32_t length;
	BOOST::uint16_t period;
	BOOST::uint16_t extra;
	byte joypad_count;
	byte has_joypad_sync;
	byte unused [2];
	
	enum { tag = FOUR_CHAR('INFO') };
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (movie_info_t) == 16 );

struct nes_state_t
{
	BOOST::uint16_t timestamp; // CPU clocks * 15 (for NTSC)
	byte pal;
	byte unused [1];
	BOOST::uint32_t frame_count; // number of frames emulated since power-up
	
	enum { tag = FOUR_CHAR('TIME') };
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (nes_state_t) == 8 );

struct joypad_state_t
{
	uint32_t joypad_latches [2]; // joypad 1 & 2 shift registers
	byte w4016;             // strobe
	byte unused [3];
	
	enum { tag = FOUR_CHAR('CTRL') };
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (joypad_state_t) == 12 );

// Increase this (and let me know) if your mapper requires more state. This only
// sets the size of the in-memory buffer; it doesn't affect the file format at all.
unsigned const max_mapper_state_size = 256;
struct mapper_state_t
{
	int size;
	union {
		double align;
		byte data [max_mapper_state_size];
	};
	
	void write( const void* p, unsigned long s );
	int read( void* p, unsigned long s ) const;
};

struct cpu_state_t
{
	BOOST::uint16_t pc;
	byte s;
	byte p;
	byte a;
	byte x;
	byte y;
	byte unused [1];
	
	enum { tag = FOUR_CHAR('CPUR') };
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (cpu_state_t) == 8 );

struct ppu_state_t
{
	byte w2000;                 // control
	byte w2001;                 // control
	byte r2002;                 // status
	byte w2003;                 // sprite ram addr
	byte r2007;                 // vram read buffer
	byte second_write;          // next write to $2005/$2006 is second since last $2002 read
	BOOST::uint16_t vram_addr;  // loopy_v
	BOOST::uint16_t vram_temp;  // loopy_t
	byte pixel_x;               // fine-scroll (0-7)
	byte unused;
	byte palette [0x20];        // entries $10, $14, $18, $1c should be ignored
	BOOST::uint16_t decay_low;
	BOOST::uint16_t decay_high;
	byte open_bus;
	byte unused2[3];
	
	enum { tag = FOUR_CHAR('PPUR') };
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (ppu_state_t) == 20 + 0x20 );

struct mmc1_state_t
{
	byte regs [4]; // current registers (5 bits each)
	byte bit;      // number of bits in buffer (0 to 4)
	byte buf;      // currently buffered bits (new bits added to bottom)
};
BOOST_STATIC_ASSERT( sizeof (mmc1_state_t) == 6 );

struct mmc3_state_t
{
	byte banks [8]; // last writes to $8001 indexed by (mode & 7)
	byte mode;      // $8000
	byte mirror;    // $a000
	byte sram_mode; // $a001
	byte irq_ctr;   // internal counter
	byte irq_latch; // $c000
	byte irq_enabled;// last write was to 0) $e000, 1) $e001
	byte irq_flag;
};
BOOST_STATIC_ASSERT( sizeof (mmc3_state_t) == 15 );

#endif

