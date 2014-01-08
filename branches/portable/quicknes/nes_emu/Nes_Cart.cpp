
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Cart.h"

#include <stdlib.h>
#include <string.h>

/* Copyright (C) 2004-2006 Shay Green. This module is free software; you
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

char const Nes_Cart::not_ines_file [] = "Not an iNES file";

Nes_Cart::Nes_Cart()
{
	prg_ = NULL;
	chr_ = NULL;
	clear();
}

Nes_Cart::~Nes_Cart()
{
	clear();
}

void Nes_Cart::clear()
{
	free( prg_ );
	prg_ = NULL;
	
	free( chr_ );
	chr_ = NULL;
	
	prg_size_ = 0;
	chr_size_ = 0;
	mapper = 0;
}

long Nes_Cart::round_to_bank_size( long n )
{
	n += bank_size - 1;
	return n - n % bank_size;
}

blargg_err_t Nes_Cart::resize_prg( long size )
{
	if ( size != prg_size_ )
	{
		// padding allows CPU to always read operands of instruction, which
		// might go past end of data
		void* p = realloc( prg_, round_to_bank_size( size ) + 2 );
		CHECK_ALLOC( p || !size );
		prg_ = (byte*) p;
		prg_size_ = size;
	}
	return 0;
}

blargg_err_t Nes_Cart::resize_chr( long size )
{
	if ( size != chr_size_ )
	{
		void* p = realloc( chr_, round_to_bank_size( size ) );
		CHECK_ALLOC( p || !size );
		chr_ = (byte*) p;
		chr_size_ = size;
	}
	return 0;
}

// iNES reading

struct ines_header_t {
	BOOST::uint8_t signature [4];
	BOOST::uint8_t prg_count; // number of 16K PRG banks
	BOOST::uint8_t chr_count; // number of 8K CHR banks
	BOOST::uint8_t flags;     // MMMM FTBV Mapper low, Four-screen, Trainer, Battery, V mirror
	BOOST::uint8_t flags2;    // MMMM --XX Mapper high 4 bits
	BOOST::uint8_t zero [8];  // if zero [7] is non-zero, treat flags2 as zero
};
BOOST_STATIC_ASSERT( sizeof (ines_header_t) == 16 );

blargg_err_t Nes_Cart::load_ines( Auto_File_Reader in )
{
	RETURN_ERR( in.open() );
	
	ines_header_t h;
	RETURN_ERR( in->read( &h, sizeof h ) );
	
	if ( 0 != memcmp( h.signature, "NES\x1A", 4 ) )
		return not_ines_file;
	
	if ( h.zero [7] ) // handle header defaced by a fucking idiot's handle
		h.flags2 = 0;
	
	set_mapper( h.flags, h.flags2 );
	
	if ( h.flags & 0x04 ) // skip trainer
		RETURN_ERR( in->skip( 512 ) );
	
	RETURN_ERR( resize_prg( h.prg_count * 16 * 1024L ) );
	RETURN_ERR( resize_chr( h.chr_count * 8 * 1024L ) );
	
	RETURN_ERR( in->read( prg(), prg_size() ) );
	RETURN_ERR( in->read( chr(), chr_size() ) );
	
	return 0;
}

// IPS patching

// IPS patch file format (integers are big-endian):
// 5    "PATCH"
// n    blocks
//
// normal block:
// 3    offset
// 2    size
// n    data
//
// repeated byte block:
// 3    offset
// 2    0
// 2    size
// 1    fill value
//
// end block (optional):
// 3    "EOF"
//
// A block can append data to the file by specifying an offset at the end of
// the current file data.

typedef BOOST::uint8_t byte;
static blargg_err_t apply_ips_patch( Data_Reader& patch, byte** file, long* file_size )
{
	byte signature [5];
	RETURN_ERR( patch.read( signature, sizeof signature ) );
	if ( memcmp( signature, "PATCH", sizeof signature ) )
		return "Not an IPS patch file";
	
	while ( patch.remain() )
	{
		// read offset
		byte buf [6];
		RETURN_ERR( patch.read( buf, 3 ) );
		long offset = buf [0] * 0x10000 + buf [1] * 0x100 + buf [2];
		if ( offset == 0x454F46 ) // 'EOF'
			break;
		
		// read size
		RETURN_ERR( patch.read( buf, 2 ) );
		long size = buf [0] * 0x100 + buf [1];
		
		// size = 0 signals a run of identical bytes
		int fill = -1;
		if ( size == 0 )
		{
			RETURN_ERR( patch.read( buf, 3 ) );
			size = buf [0] * 0x100 + buf [1];
			fill = buf [2];
		}
		
		// expand file if new data is at exact end of file
		if ( offset == *file_size )
		{
			*file_size = offset + size;
			void* p = realloc( *file, *file_size );
			CHECK_ALLOC( p );
			*file = (byte*) p;
		}
		
		//dprintf( "Patch offset: 0x%04X, size: 0x%04X\n", (int) offset, (int) size );
		
		if ( offset < 0 || *file_size < offset + size )
			return "IPS tried to patch past end of file";
		
		// read/fill data
		if ( fill < 0 )
			RETURN_ERR( patch.read( *file + offset, size ) );
		else
			memset( *file + offset, fill, size );
	}
	
	return 0;
}

blargg_err_t Nes_Cart::load_patched_ines( Auto_File_Reader in, Auto_File_Reader patch )
{
	RETURN_ERR( in.open() );
	RETURN_ERR( patch.open() );
	
	// read file into memory
	long size = in->remain();
	byte* ines = (byte*) malloc( size );
	CHECK_ALLOC( ines );
	const char* err = in->read( ines, size );
	
	// apply patch
	if ( !err )
		err = apply_ips_patch( *patch, &ines, &size );
	
	// load patched file
	if ( !err )
	{
		Mem_File_Reader patched( ines, size );
		err = load_ines( patched );
	}
	
	free( ines );
	
	return err;
}

blargg_err_t Nes_Cart::apply_ips_to_prg( Auto_File_Reader patch )
{
	RETURN_ERR( patch.open() );

	long size = prg_size();

	byte* prg_copy = (byte*) malloc( size );
	CHECK_ALLOC( prg_copy );
	memcpy( prg_copy, prg(), size );

	const char* err = apply_ips_patch( *patch, &prg_copy, &size );

	if ( !err )
	{
		resize_prg( size );
		memcpy( prg(), prg_copy, size );
	}

	free( prg_copy );

	return err;
}

blargg_err_t Nes_Cart::apply_ips_to_chr( Auto_File_Reader patch )
{
	RETURN_ERR( patch.open() );

	long size = chr_size();

	byte* chr_copy = (byte*) malloc( size );
	CHECK_ALLOC( chr_copy );
	memcpy( chr_copy, chr(), size );

	const char* err = apply_ips_patch( *patch, &chr_copy, &size );

	if ( !err )
	{
		resize_chr( size );
		memcpy( chr(), chr_copy, size );
	}

	free( chr_copy );

	return err;
}
