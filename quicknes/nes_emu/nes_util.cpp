
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "nes_util.h"

#include "Nes_Cart.h"
#include "Nes_Emu.h"
#include <ctype.h>
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

// Joypad_Filter

Joypad_Filter::Joypad_Filter()
{
	prev = 0;
	mask = ~0x50;
	times [0] = 0;
	times [1] = 0;
	set_a_rate( 0.75 );
	set_b_rate( 0.75 );
}

void Joypad_Filter::enable_filtering( bool b )
{
	bool enabled = (mask + 0x10) >> 5 & 1;
	if ( enabled != b )
		mask = b ? ~0x50 : ~0;
}

int Joypad_Filter::process( int joypad )
{
	// prevent left+right and up+down (prefer most recent one pressed)
	int changed = prev ^ joypad;
	int hidden = joypad & ~mask;
	prev = joypad;
	
	int const x_axis = 0xC0;
	int const y_axis = 0x30;
	
	if ( changed & x_axis && hidden & x_axis )
		mask ^= x_axis;
	
	if ( changed & y_axis && hidden & y_axis )
		mask ^= y_axis;
	
	// reset turbo if button just pressed, to avoid delaying button press
	if ( changed & 0x100 ) times [0] = 0;
	if ( changed & 0x200 ) times [1] = 0;
	mask |= changed & 0x300 & joypad;
	
	// mask and combine turbo bits
	joypad &= mask;
	return (joypad >> 8 & 3) | (joypad & ~0x300);
}

void Joypad_Filter::clock_turbo()
{
	for ( int i = 0; i < 2; i++ )
	{
		int t = times [i] + rates [i];
		mask ^= (t & 0x100) << i;
		times [i] = t & 0xFF;
	}
}

// game_genie_patch_t

blargg_err_t game_genie_patch_t::decode( const char* in )
{
	int const code_len = 8;
	unsigned char result [code_len] = { 0 };
	int in_len = strlen( in );
	if ( in_len != 6 && in_len != 8 )
		return "Game Genie code is wrong length";
	for ( int i = 0; i < code_len; i++ )
	{
		char c = 'A';
		if ( i < in_len )
			c = toupper( in [i] );
		
		static char const letters [17] = "AEPOZXLUGKISTVYN";
		char const* p = strchr( (char*) letters, c );
		if ( !p )
			return "Game Genie code had invalid character";
		int n = p - letters;
		
		result [i] |= n >> 1;
		result [(i + 1) % code_len] |= (n << 3) & 0x0f;
	}
	
	addr = result [3]<<12 | result [5]<<8 | result [2]<<4 | result [4];
	change_to = result [1]<<4 | result [0];
	compare_with = -1;
	if ( addr & 0x8000 )
		compare_with = result [7]<<4 | result [6];
	addr |= 0x8000;
	
	return 0;
}

int game_genie_patch_t::apply( Nes_Cart& cart ) const
{
	// determine bank size
	long bank_size = 32 * 1024L; // mappers 0, 2, 3, 7, 11, 34, 71, 87
	switch ( cart.mapper_code() )
	{
	case 1:   // MMC1
	case 71:  // Camerica
	case 232: // Quattro
		bank_size = 16 * 1024L;
		break;
	
	case 4:  // MMC3
	case 5:  // MMC5
	case 24: // VRC6
	case 26: // VRC6
	case 69: // FME7
		bank_size = 8 * 1024L;
		break;
	}
	
	// patch each bank (not very good, since it might patch banks that never occupy
	// that address)
	int mask = (compare_with >= 0 ? ~0 : 0);
	BOOST::uint8_t* p = cart.prg() + addr % bank_size;
	int count = 0;
	for ( int n = cart.prg_size() / bank_size; n--; p += bank_size )
	{
		if ( !((*p ^ compare_with) & mask) )
		{
			*p = change_to;
			count++;
		}
	}
	return count;
}

// Cheat_Value_Finder

Cheat_Value_Finder::Cheat_Value_Finder()
{
	emu = NULL;
}

void Cheat_Value_Finder::start( Nes_Emu* new_emu )
{
	emu = new_emu;
	pos = 0;
	memcpy( original, emu->low_mem(), low_mem_size );
	memset( changed, 0, low_mem_size );
}

void Cheat_Value_Finder::rescan()
{
	byte const* low_mem = emu->low_mem();
	for ( int i = 0; i < low_mem_size; i++ )
		changed [i] |= original [i] ^ low_mem [i];
	memcpy( original, emu->low_mem(), low_mem_size );
}

void Cheat_Value_Finder::search( int new_original, int new_changed )
{
	require( new_original != new_changed );
	original_value = new_original;
	changed_value = new_changed;
	pos = -1;
}

int Cheat_Value_Finder::next_match( int* addr )
{
	byte const* low_mem = emu->low_mem();
	while ( ++pos < low_mem_size )
	{
		if ( !changed [pos] )
		{
			int old = (original [pos] - original_value) & 0xff;
			int cur = (low_mem [pos] - changed_value) & 0xff;
			
			if ( old == cur )
			{
				if ( addr )
					*addr = pos;
				return (char) old; // sign-extend
			}
		}
	}
	
	return no_match;
}

int Cheat_Value_Finder::change_value( int new_value )
{
	require( (unsigned) pos < low_mem_size );
	int result = emu->low_mem() [pos];
	emu->low_mem() [pos] = new_value;
	return result;
}

