
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "nes_data.h"

#include "blargg_endian.h"

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

#define SWAP_BE( n )    (void) (set_be( &(n), (n) ))
#define SWAP_LE( n )    (void) (set_le( &(n), (n) ))

void nes_block_t::swap()
{
	SWAP_BE( tag );
	SWAP_LE( size );
}

void nes_state_t::swap()
{
	SWAP_LE( timestamp );
	SWAP_LE( frame_count );
}

void cpu_state_t::swap()
{
	SWAP_LE( pc );
}

void ppu_state_t::swap()
{
	SWAP_LE( vram_addr );
	SWAP_LE( vram_temp );
	SWAP_LE( decay_low );
	SWAP_LE( decay_high );
}

void apu_state_t::swap()
{
	SWAP_LE( apu.frame_delay );
	SWAP_LE( square1.delay );
	SWAP_LE( square2.delay );
	SWAP_LE( triangle.delay );
	SWAP_LE( noise.delay );
	SWAP_LE( noise.shift_reg );
	SWAP_LE( dmc.delay );
	SWAP_LE( dmc.remain );
	SWAP_LE( dmc.addr );
}

void joypad_state_t::swap()
{
	SWAP_LE( joypad_latches [0] );
	SWAP_LE( joypad_latches [1] );
}

void movie_info_t::swap()
{
	SWAP_LE( begin );
	SWAP_LE( length );
	SWAP_LE( period );
	SWAP_LE( extra );
}

