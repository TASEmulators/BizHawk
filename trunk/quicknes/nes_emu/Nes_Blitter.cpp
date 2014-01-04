
#include "Nes_Blitter.h"

#include "blargg_endian.h"

#include "blargg_source.h"

#ifndef NES_BLITTER_OUT_DEPTH
	#define NES_BLITTER_OUT_DEPTH 16
#endif

Nes_Blitter::Nes_Blitter() { ntsc = 0; }

Nes_Blitter::~Nes_Blitter() { free( ntsc ); }

blargg_err_t Nes_Blitter::init()
{
	assert( !ntsc );
	CHECK_ALLOC( ntsc = (nes_ntsc_emph_t*) malloc( sizeof *ntsc ) );
	static setup_t const s = { };
	setup_ = s;
	setup_.ntsc = nes_ntsc_composite;
	return setup( setup_ );
}

blargg_err_t Nes_Blitter::setup( setup_t const& s )
{
	setup_ = s;
	chunk_count = ((Nes_Emu::image_width - setup_.crop.left - setup_.crop.right) + 5) / 6;
	height = Nes_Emu::image_height - setup_.crop.top - setup_.crop.bottom;
	nes_ntsc_init_emph( ntsc, &setup_.ntsc );
	return 0;
}

void Nes_Blitter::blit( Nes_Emu& emu, void* out, long out_pitch )
{
	short const* palette = emu.frame().palette;
	int burst_phase = (setup_.ntsc.merge_fields ? 0 : emu.frame().burst_phase);
	long in_pitch = emu.frame().pitch;
	unsigned char* in = emu.frame().pixels + setup_.crop.top * in_pitch + setup_.crop.left;
	
	for ( int n = height; n; --n )
	{
		unsigned char* line_in = in;
		in += in_pitch;
		
		BOOST::uint32_t* line_out = (BOOST::uint32_t*) out;
		out = (char*) out + out_pitch;
		
		NES_NTSC_BEGIN_ROW( ntsc, burst_phase,
				nes_ntsc_black, nes_ntsc_black, palette [*line_in] );
		
		line_in [256] = 252; // loop reads 3 extra pixels, so set them to black
		line_in [257] = 252;
		line_in [258] = 252;
		line_in++;
		
		burst_phase = (burst_phase + 1) % nes_ntsc_burst_count;
		
		// assemble two 16-bit pixels into a 32-bit int for better performance
		#if BLARGG_BIG_ENDIAN
			#define COMBINE_PIXELS right |= left << 16;
		#else
			#define COMBINE_PIXELS right <<= 16; right |= left;
		#endif
		
		#define OUT_PIXEL( i ) \
			if ( !(i & 1) ) {\
				NES_NTSC_RGB_OUT( (i % 7), left, NES_BLITTER_OUT_DEPTH );\
				if ( i > 1 ) line_out  [(i/2)-1] = right;\
			}\
			else {\
				NES_NTSC_RGB_OUT( (i % 7), right, NES_BLITTER_OUT_DEPTH );\
				COMBINE_PIXELS;\
			}
			
		for ( int n = chunk_count; n; --n )
		{
			unsigned long left, right;
			
			NES_NTSC_COLOR_IN( 0, palette [line_in [0]] );
			OUT_PIXEL( 0 );
			OUT_PIXEL( 1 );
			
			NES_NTSC_COLOR_IN( 1, palette [line_in [1]] );
			OUT_PIXEL( 2 );
			OUT_PIXEL( 3 );
			
			NES_NTSC_COLOR_IN( 2, palette [line_in [2]] );
			OUT_PIXEL( 4 );
			OUT_PIXEL( 5 );
			OUT_PIXEL( 6 );
			
			NES_NTSC_COLOR_IN( 0, palette [line_in [3]] );
			OUT_PIXEL( 7 );
			OUT_PIXEL( 8 );
			
			NES_NTSC_COLOR_IN( 1, palette [line_in [4]] );
			OUT_PIXEL( 9 );
			OUT_PIXEL( 10);
			
			NES_NTSC_COLOR_IN( 2, palette [line_in [5]] );
			line_in += 6;
			OUT_PIXEL( 11 );
			OUT_PIXEL( 12 );
			OUT_PIXEL( 13 );
			
			line_out [6] = right;
			line_out += 7;
		}
	}
}

