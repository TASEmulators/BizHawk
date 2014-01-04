
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Film_Packer.h"

#include <string.h>

/* Copyright (C) 2006 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include "blargg_source.h"

#ifdef BLARGG_ENABLE_OPTIMIZER
	#include BLARGG_ENABLE_OPTIMIZER
#endif

// - On my 400 MHz PowerPC G3, pack() = 230MB/sec, unpack() = 320MB/sec.
// - All 32-bit accessess are on 4-byte boundaries of the input/output buffers.
// - This would not make a good general-purpose compressor because the match
//   offset is limited to a multiple of 4.

#ifdef __MWERKS__
	static unsigned zero = 0; // helps CodeWarrior optimizer when added to constants
#else
	const  unsigned zero = 0; // compile-time constant on other compilers
#endif

void Nes_Film_Packer::prepare( void const* begin, long size )
{
	uint32_t const* end = (uint32_t*) ((byte*) begin + size - 4);
	uint32_t const** d = dict;
	for ( int n = dict_size; n--; )
		*d++ = end;
	
	uint32_t temp = 0x80000000;
	assert( (BOOST::int32_t) temp < 0 ); // be sure high bit is sign
}

long Nes_Film_Packer::pack( byte const* in_begin, long in_size, byte* out_begin )
{
//memcpy( out_begin, in_begin, in_size ); return in_size;
	
	assert( (in_size & 3) == 0 );
	uint32_t const* const in_end = (uint32_t*) (in_begin + in_size);
	uint32_t const* const end = in_end - 2;
	uint32_t const* in = (uint32_t*) in_begin;
	uint32_t* out = (uint32_t*) out_begin;
	
	unsigned long first = *in++;
	unsigned long offset;
	uint32_t const* match;
	
	unsigned long const factor = 0x100801 + zero;
	
	// spaghetti program flow gives better efficiency
	
	goto begin;
	
	// match loop
	do
	{
		if ( match [-1] != first ) break;
		offset <<= 14;
		if ( *match != *in ) break;
match:
		// count matching words beyond the first two
		unsigned long n = (byte*) end - (byte*) in;
		first = *++in;
		unsigned long m = *++match;
		uint32_t const* start = in;
		for ( n >>= 2; n; --n )
		{
			if ( m != first ) break;
			m = *++match;
			first = *++in;
		}
		
		// encode match offset and length
		unsigned long length = (byte*) in - (byte*) start;
		assert( 0 <= length && length <= 0xFFFF << 2 );
		assert( offset >> 16 <= 0x7FFF );
		offset |= length >> 2;
		
		// check for next match
		unsigned long index = (first * factor) >> dict_shift & (dict_size - 1);
		match = dict [index];
		*out++ = offset; // interleved write of previous match
		offset = (byte*) in - (byte*) match; assert( !(offset & 3) );
		if ( in >= end ) goto match_end;
		++in;
		dict [index] = in;
	}
	while ( offset < 0x20000 );
	
begin:
	// start writing next literal
	out [1] = first;
	uint32_t* literal;
	literal = out;
	out++;
	
	// literal loop
literal:
	first = *in;
	do
	{
		// check for match
		unsigned long index = (first * factor) >> dict_shift & (dict_size - 1);
		*++out = first; // interleved write of current literal
		match = dict [index];
		dict [index] = in + 1;
		if ( in >= end ) goto literal_end;
		offset = (byte*) in - (byte*) match; assert( !(offset & 3) );
		++in;
		if ( match [-1] != first ) goto literal;
		first = *in;
	}
	while ( offset >= 0x20000 || *match != first );
	
	// set length of completed literal
	offset <<= 14;
	*literal = (((byte*) out - (byte*) literal) >> 2) | 0x80000000;
	goto match;
	
match_end:
	// start new literal for remaining data after final match
	literal = out++;
literal_end:
	--out;
	
	// write remaining data to literal
	assert( in < in_end );
	do
	{
		*++out = *in++;
	}
	while ( in < in_end );
	*literal = (((byte*) out - (byte*) literal) >> 2) + 0x80000001;
	
	// mark end with zero word
	*++out = 0x80000000;
	++out;
	
	long out_size = (byte*) out - out_begin;
	assert( (out_size & 3) == 0 );
	return out_size;
}

long Nes_Film_Packer::unpack( byte const* in_begin, long in_size, byte* out_begin )
{
//memcpy( out_begin, in_begin, in_size ); return in_size;

	assert( (in_size & 3) == 0 );
	uint32_t const* in = (uint32_t*) in_begin;
	uint32_t* out = (uint32_t*) out_begin;
	long const literal_offset = 0x7FFFFFFE + zero;
	long count = (BOOST::int32_t) *in++;
	uint32_t const* m;
	
	assert( count < 0 ); // first item should be literal
	goto literal;
	do
	{
		// match
		do
		{
			assert( m - 1 >= (void*) out_begin );
			assert( m - 1 < out );
			unsigned long data = m [-1];
			*out++ = data;
			data = *m;
			if ( (count &= 0xFFFF) != 0 )
			{
				do
				{
					*out++ = data;
					data = *++m;
				}
				while ( --count );
			}
			count = (BOOST::int32_t) *in++;
			*out++ = data;
			m = out - (count >> 16);
		}
		while ( count >= 0 );
		
	literal:
		unsigned long data = *in++;
		*out++ = data;
		data = *in++;
		if ( (count += literal_offset) != 0 )
		{
			do
			{
				*out++ = data;
				data = *in++;
			}
			while ( --count );
		}
		
		count = (BOOST::int32_t) data;
		m = out - (data >> 16);
	}
	while ( count >= 0 );
	
	assert( count == (BOOST::int32_t) 0x80000000 );
	assert( (byte*) in == in_begin + in_size );
	long out_size = (byte*) out - out_begin;
	assert( (out_size & 3) == 0 );
	return out_size;
}

