//
// Jaguar memory and I/O physical (hosted!) memory
//
// by James Hammons
//
// JLH = James Hammons
//
// WHO  WHEN        WHAT
// ---  ----------  -----------------------------------------------------------
// JLH  12/10/2009  Repurposed this file. :-)
//

/*
$FFFFFF => 16,777,215
$A00000 => 10,485,760

Really, just six megabytes short of using the entire address space...
Why not? We could just allocate the entire space and then use the MMU code to do
things like call functions and whatnot...
In other words, read/write would just tuck the value into the host RAM space and
the I/O function would take care of any weird stuff...

Actually: writes would tuck in the value, but reads would have to be handled
correctly since some registers do not fall on the same address as far as reading
goes... Still completely doable though. :-)

N.B.: Jaguar RAM is only 2 megs. ROM is 6 megs max, IO is 128K
*/

#include "memory.h"

uint8_t jagMemSpace[0xF20000];					// The entire memory space of the Jaguar...!

uint8_t * jaguarMainRAM = &jagMemSpace[0x000000];
uint8_t * jaguarMainROM = &jagMemSpace[0x800000];
uint8_t * cdRAM         = &jagMemSpace[0xDFFF00];
uint8_t * gpuRAM        = &jagMemSpace[0xF03000];
uint8_t * dspRAM        = &jagMemSpace[0xF1B000];

#if 0
union Word
{
	uint16_t word;
	struct {
		// This changes depending on endianness...
#ifdef __BIG_ENDIAN__
		uint8_t hi, lo;							// Big endian
#else
		uint8_t lo, hi;							// Little endian
#endif
	};
};
#endif

#if 0
union DWord
{
	uint32_t dword;
	struct
	{
#ifdef __BIG_ENDIAN__
		uint16_t hiw, low;
#else
		uint16_t low, hiw;
#endif
	};
};
#endif

#if 0
static void test(void)
{
	Word reg;
	reg.word = 0x1234;
	reg.lo = 0xFF;
	reg.hi = 0xEE;

	DWord reg2;
	reg2.hiw = 0xFFFE;
	reg2.low = 0x3322;
	reg2.low.lo = 0x11;
}
#endif

// OR, we could do like so:
#if 0
#ifdef __BIG_ENDIAN__
#define DWORD_BYTE_HWORD_H 1
#define DWORD_BYTE_HWORD_L 2
#define DWORD_BYTE_LWORD_H 3
#define DWORD_BYTE_LWORD_L 4
#else
#define DWORD_BYTE_HWORD_H 4
#define DWORD_BYTE_HWORD_L 3
#define DWORD_BYTE_LWORD_H 2
#define DWORD_BYTE_LWORD_L 1
#endif
// But this starts to get cumbersome after a while... Is union really better?

//More union stuff...
unsigned long ByteSwap1 (unsigned long nLongNumber)
{
   union u {unsigned long vi; unsigned char c[sizeof(unsigned long)];};
   union v {unsigned long ni; unsigned char d[sizeof(unsigned long)];};
   union u un;
   union v vn;
   un.vi = nLongNumber;
   vn.d[0]=un.c[3];
   vn.d[1]=un.c[2];
   vn.d[2]=un.c[1];
   vn.d[3]=un.c[0];
   return (vn.ni);
}
#endif

//Not sure if this is a good approach yet...
//should be if we use proper aliasing, and htonl and friends...
#if 1
uint32_t & butch     = *((uint32_t *)&jagMemSpace[0xDFFF00]);	// base of Butch == interrupt control register, R/W
uint32_t & dscntrl   = *((uint32_t *)&jagMemSpace[0xDFFF04]);	// DSA control register, R/W
uint16_t & ds_data   = *((uint16_t *)&jagMemSpace[0xDFFF0A]);	// DSA TX/RX data, R/W
uint32_t & i2cntrl   = *((uint32_t *)&jagMemSpace[0xDFFF10]);	// i2s bus control register, R/W
uint32_t & sbcntrl   = *((uint32_t *)&jagMemSpace[0xDFFF14]);	// CD subcode control register, R/W
uint32_t & subdata   = *((uint32_t *)&jagMemSpace[0xDFFF18]);	// Subcode data register A
uint32_t & subdatb   = *((uint32_t *)&jagMemSpace[0xDFFF1C]);	// Subcode data register B
uint32_t & sb_time   = *((uint32_t *)&jagMemSpace[0xDFFF20]);	// Subcode time and compare enable (D24)
uint32_t & fifo_data = *((uint32_t *)&jagMemSpace[0xDFFF24]);	// i2s FIFO data
uint32_t & i2sdat2   = *((uint32_t *)&jagMemSpace[0xDFFF28]);	// i2s FIFO data (old)
uint32_t & unknown   = *((uint32_t *)&jagMemSpace[0xDFFF2C]);	// Seems to be some sort of I2S interface
#else
uint32_t butch, dscntrl, ds_data, i2cntrl, sbcntrl, subdata, subdatb, sb_time, fifo_data, i2sdat2, unknown;
#endif

//#warning "Need to separate out this stuff (or do we???)"
//if we use a contiguous memory space, we don't need this shit...
//err, maybe we do, let's not be so hasty now... :-)

//#define ENDIANSAFE(x) htonl(x)

// The nice thing about doing it this way is that on big endian machines, htons/l
// compile to nothing and on Intel machines, it compiles down to a single bswap instruction.
// So endianness issues go away nicely without a lot of drama. :-D

#define BSWAP16(x) (htons(x))
#define BSWAP32(x) (htonl(x))
//this isn't endian safe...
#define BSWAP64(x) ((htonl(x & 0xFFFFFFFF) << 32) | htonl(x >> 32))
// Actually, we use ESAFExx() macros instead of this, and we use GCC to check the endianness...
// Actually, considering that "byteswap.h" doesn't exist elsewhere, the above
// is probably our best bet here. Just need to rename them to ESAFExx().

// Look at <endian.h> and see if that header is portable or not.

uint16_t & memcon1   = *((uint16_t *)&jagMemSpace[0xF00000]);
uint16_t & memcon2   = *((uint16_t *)&jagMemSpace[0xF00002]);
uint16_t & hc        = *((uint16_t *)&jagMemSpace[0xF00004]);
uint16_t & vc        = *((uint16_t *)&jagMemSpace[0xF00006]);
uint16_t & lph       = *((uint16_t *)&jagMemSpace[0xF00008]);
uint16_t & lpv       = *((uint16_t *)&jagMemSpace[0xF0000A]);
uint64_t & obData    = *((uint64_t *)&jagMemSpace[0xF00010]);
uint32_t & olp       = *((uint32_t *)&jagMemSpace[0xF00020]);
uint16_t & obf       = *((uint16_t *)&jagMemSpace[0xF00026]);
uint16_t & vmode     = *((uint16_t *)&jagMemSpace[0xF00028]);
uint16_t & bord1     = *((uint16_t *)&jagMemSpace[0xF0002A]);
uint16_t & bord2     = *((uint16_t *)&jagMemSpace[0xF0002C]);
uint16_t & hp        = *((uint16_t *)&jagMemSpace[0xF0002E]);
uint16_t & hbb       = *((uint16_t *)&jagMemSpace[0xF00030]);
uint16_t & hbe       = *((uint16_t *)&jagMemSpace[0xF00032]);
uint16_t & hs        = *((uint16_t *)&jagMemSpace[0xF00034]);
uint16_t & hvs       = *((uint16_t *)&jagMemSpace[0xF00036]);
uint16_t & hdb1      = *((uint16_t *)&jagMemSpace[0xF00038]);
uint16_t & hdb2      = *((uint16_t *)&jagMemSpace[0xF0003A]);
uint16_t & hde       = *((uint16_t *)&jagMemSpace[0xF0003C]);
uint16_t & vp        = *((uint16_t *)&jagMemSpace[0xF0003E]);
uint16_t & vbb       = *((uint16_t *)&jagMemSpace[0xF00040]);
uint16_t & vbe       = *((uint16_t *)&jagMemSpace[0xF00042]);
uint16_t & vs        = *((uint16_t *)&jagMemSpace[0xF00044]);
uint16_t & vdb       = *((uint16_t *)&jagMemSpace[0xF00046]);
uint16_t & vde       = *((uint16_t *)&jagMemSpace[0xF00048]);
uint16_t & veb       = *((uint16_t *)&jagMemSpace[0xF0004A]);
uint16_t & vee       = *((uint16_t *)&jagMemSpace[0xF0004C]);
uint16_t & vi        = *((uint16_t *)&jagMemSpace[0xF0004E]);
uint16_t & pit0      = *((uint16_t *)&jagMemSpace[0xF00050]);
uint16_t & pit1      = *((uint16_t *)&jagMemSpace[0xF00052]);
uint16_t & heq       = *((uint16_t *)&jagMemSpace[0xF00054]);
uint32_t & bg        = *((uint32_t *)&jagMemSpace[0xF00058]);
uint16_t & int1      = *((uint16_t *)&jagMemSpace[0xF000E0]);
uint16_t & int2      = *((uint16_t *)&jagMemSpace[0xF000E2]);
uint8_t  * clut      =   (uint8_t *) &jagMemSpace[0xF00400];
uint8_t  * lbuf      =   (uint8_t *) &jagMemSpace[0xF00800];
uint32_t & g_flags   = *((uint32_t *)&jagMemSpace[0xF02100]);
uint32_t & g_mtxc    = *((uint32_t *)&jagMemSpace[0xF02104]);
uint32_t & g_mtxa    = *((uint32_t *)&jagMemSpace[0xF02108]);
uint32_t & g_end     = *((uint32_t *)&jagMemSpace[0xF0210C]);
uint32_t & g_pc      = *((uint32_t *)&jagMemSpace[0xF02110]);
uint32_t & g_ctrl    = *((uint32_t *)&jagMemSpace[0xF02114]);
uint32_t & g_hidata  = *((uint32_t *)&jagMemSpace[0xF02118]);
uint32_t & g_divctrl = *((uint32_t *)&jagMemSpace[0xF0211C]);
uint32_t g_remain;								// Dual register with $F0211C
uint32_t & a1_base   = *((uint32_t *)&jagMemSpace[0xF02200]);
uint32_t & a1_flags  = *((uint32_t *)&jagMemSpace[0xF02204]);
uint32_t & a1_clip   = *((uint32_t *)&jagMemSpace[0xF02208]);
uint32_t & a1_pixel  = *((uint32_t *)&jagMemSpace[0xF0220C]);
uint32_t & a1_step   = *((uint32_t *)&jagMemSpace[0xF02210]);
uint32_t & a1_fstep  = *((uint32_t *)&jagMemSpace[0xF02214]);
uint32_t & a1_fpixel = *((uint32_t *)&jagMemSpace[0xF02218]);
uint32_t & a1_inc    = *((uint32_t *)&jagMemSpace[0xF0221C]);
uint32_t & a1_finc   = *((uint32_t *)&jagMemSpace[0xF02220]);
uint32_t & a2_base   = *((uint32_t *)&jagMemSpace[0xF02224]);
uint32_t & a2_flags  = *((uint32_t *)&jagMemSpace[0xF02228]);
uint32_t & a2_mask   = *((uint32_t *)&jagMemSpace[0xF0222C]);
uint32_t & a2_pixel  = *((uint32_t *)&jagMemSpace[0xF02230]);
uint32_t & a2_step   = *((uint32_t *)&jagMemSpace[0xF02234]);
uint32_t & b_cmd     = *((uint32_t *)&jagMemSpace[0xF02238]);
uint32_t & b_count   = *((uint32_t *)&jagMemSpace[0xF0223C]);
uint64_t & b_srcd    = *((uint64_t *)&jagMemSpace[0xF02240]);
uint64_t & b_dstd    = *((uint64_t *)&jagMemSpace[0xF02248]);
uint64_t & b_dstz    = *((uint64_t *)&jagMemSpace[0xF02250]);
uint64_t & b_srcz1   = *((uint64_t *)&jagMemSpace[0xF02258]);
uint64_t & b_srcz2   = *((uint64_t *)&jagMemSpace[0xF02260]);
uint64_t & b_patd    = *((uint64_t *)&jagMemSpace[0xF02268]);
uint32_t & b_iinc    = *((uint32_t *)&jagMemSpace[0xF02270]);
uint32_t & b_zinc    = *((uint32_t *)&jagMemSpace[0xF02274]);
uint32_t & b_stop    = *((uint32_t *)&jagMemSpace[0xF02278]);
uint32_t & b_i3      = *((uint32_t *)&jagMemSpace[0xF0227C]);
uint32_t & b_i2      = *((uint32_t *)&jagMemSpace[0xF02280]);
uint32_t & b_i1      = *((uint32_t *)&jagMemSpace[0xF02284]);
uint32_t & b_i0      = *((uint32_t *)&jagMemSpace[0xF02288]);
uint32_t & b_z3      = *((uint32_t *)&jagMemSpace[0xF0228C]);
uint32_t & b_z2      = *((uint32_t *)&jagMemSpace[0xF02290]);
uint32_t & b_z1      = *((uint32_t *)&jagMemSpace[0xF02294]);
uint32_t & b_z0      = *((uint32_t *)&jagMemSpace[0xF02298]);
uint16_t & jpit1     = *((uint16_t *)&jagMemSpace[0xF10000]);
uint16_t & jpit2     = *((uint16_t *)&jagMemSpace[0xF10002]);
uint16_t & jpit3     = *((uint16_t *)&jagMemSpace[0xF10004]);
uint16_t & jpit4     = *((uint16_t *)&jagMemSpace[0xF10006]);
uint16_t & clk1      = *((uint16_t *)&jagMemSpace[0xF10010]);
uint16_t & clk2      = *((uint16_t *)&jagMemSpace[0xF10012]);
uint16_t & clk3      = *((uint16_t *)&jagMemSpace[0xF10014]);
uint16_t & j_int     = *((uint16_t *)&jagMemSpace[0xF10020]);
uint16_t & asidata   = *((uint16_t *)&jagMemSpace[0xF10030]);
uint16_t & asictrl   = *((uint16_t *)&jagMemSpace[0xF10032]);
uint16_t asistat;									// Dual register with $F10032
uint16_t & asiclk    = *((uint16_t *)&jagMemSpace[0xF10034]);
uint16_t & joystick  = *((uint16_t *)&jagMemSpace[0xF14000]);
uint16_t & joybuts   = *((uint16_t *)&jagMemSpace[0xF14002]);
uint32_t & d_flags   = *((uint32_t *)&jagMemSpace[0xF1A100]);
uint32_t & d_mtxc    = *((uint32_t *)&jagMemSpace[0xF1A104]);
uint32_t & d_mtxa    = *((uint32_t *)&jagMemSpace[0xF1A108]);
uint32_t & d_end     = *((uint32_t *)&jagMemSpace[0xF1A10C]);
uint32_t & d_pc      = *((uint32_t *)&jagMemSpace[0xF1A110]);
uint32_t & d_ctrl    = *((uint32_t *)&jagMemSpace[0xF1A114]);
uint32_t & d_mod     = *((uint32_t *)&jagMemSpace[0xF1A118]);
uint32_t & d_divctrl = *((uint32_t *)&jagMemSpace[0xF1A11C]);
uint32_t d_remain;								// Dual register with $F0211C
uint32_t & d_machi   = *((uint32_t *)&jagMemSpace[0xF1A120]);
uint16_t & ltxd      = *((uint16_t *)&jagMemSpace[0xF1A148]);
uint16_t lrxd;									// Dual register with $F1A148
uint16_t & rtxd      = *((uint16_t *)&jagMemSpace[0xF1A14C]);
uint16_t rrxd;									// Dual register with $F1A14C
uint8_t  & sclk      = *((uint8_t *) &jagMemSpace[0xF1A150]);
uint8_t sstat;									// Dual register with $F1A150
uint32_t & smode     = *((uint32_t *)&jagMemSpace[0xF1A154]);

// Memory debugging identifiers

const char * whoName[10] =
	{ "Unknown", "Jaguar", "DSP", "GPU", "TOM", "JERRY", "M68K", "Blitter", "OP", "Debugger" };
