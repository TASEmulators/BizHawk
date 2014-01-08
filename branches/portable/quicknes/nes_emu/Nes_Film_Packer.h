
// Fast save state compressor/decompressor for reducing Nes_Film memory usage

// Nes_Emu 0.7.0

#ifndef NES_FILM_PACKER_H
#define NES_FILM_PACKER_H

#include "blargg_common.h"

class Nes_Film_Packer {
public:
	void prepare( void const* begin, long size );
	
	// Worst-case output size for given input size
	long worst_case( long in_size ) const { return in_size + 8; }
	
	typedef unsigned char byte;
	long pack( byte const* in, long size, byte* packed_out );
	
	long unpack( byte const* packed_in, long packed_size, byte* out );
private:
	enum { dict_bits = 12 };
	enum { dict_size = 1 << dict_bits };
	enum { dict_shift = 32 - dict_bits };
	
	typedef BOOST::uint32_t uint32_t;
	uint32_t const* dict [dict_size];
};

#endif

