
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Film_Data.h"

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

Nes_Film_Data::Nes_Film_Data()
{
	blocks = 0;
	active = 0;
	block_count = 0;
	period_ = 0;
	packer = 0;
	
	BOOST_STATIC_ASSERT( sizeof active->cpu [0] % 4 == 0 );
	BOOST_STATIC_ASSERT( sizeof active->joypad [0] % 4 == 0 );
	BOOST_STATIC_ASSERT( sizeof active->apu [0] % 4 == 0 );
	BOOST_STATIC_ASSERT( sizeof active->ppu [0] % 4 == 0 );
	BOOST_STATIC_ASSERT( sizeof active->mapper [0] % 4 == 0 );
	BOOST_STATIC_ASSERT( sizeof active->states [0] % 4 == 0 );
	//BOOST_STATIC_ASSERT( offsetof (block_t,joypad0) % 4 == 0 );  // XXX
}

#ifndef NDEBUG

static void write_file( void const* in, long size, const char* path )
{
	FILE* out = fopen( path, "wb" );
	if ( out )
	{
		fwrite( in, size, 1, out );
		fclose( out );
	}
}

void Nes_Film_Data::debug_packer() const
{
	comp_block_t* b = blocks [active_index];
	static byte* temp = new byte [active_size() * 2];
	for ( int i = 0; i < active_size() * 2; i++ )
		temp [i] = i;
	long vs = packer->unpack( b->data, b->size, temp );
	if ( vs != active_size() - b->offset )
	{
		dprintf( "Unpacked size differs\n" );
		write_file( (byte*) active + b->offset, vs, "original" );
		write_file( temp, vs, "error" );
		assert( false );
	}
	if ( memcmp( (byte*) active + b->offset, temp, vs ) )
	{
		dprintf( "Unpacked content differs\n" );
		write_file( (byte*) active + b->offset, vs, "original" );
		write_file( temp, vs, "error" );
		assert( false );
	}
	
	if ( 0 )
	{
		long total = 0;
		for ( int i = 0; i < block_count; i++ )
			if ( blocks [i] )
				total += blocks [i]->size;
		//dprintf( "Compression: %ld%%\n", total * 100 / (block_count * (active_size() - period_)) );
		dprintf( "Memory: %ld+%ldK\n", total / 1024, active_size() * 2 / 1024 + 16 );
		//dprintf( "Memory: %ldK\n", (total + active_size() * 2) / 1024 + 16 );
	}
}
#endif

void Nes_Film_Data::flush_active() const
{
	if ( active_dirty )
	{
		assert( (unsigned) active_index < (unsigned) block_count );
		
		active_dirty = false;
		comp_block_t* b = blocks [active_index];
		assert( b && !b->size ); // should have been reallocated in write()
		check( b->offset == joypad_only_ );
		b->size = packer->pack( (byte*) active + b->offset, active_size() - b->offset, b->data );
		assert( b->size <= packer->worst_case( active_size() - b->offset ) );
		
		// shrink allocation
        void* mem = realloc( b, offsetof (comp_block_t, data) + b->size * sizeof(b->data[0]) );
		if ( mem )
			blocks [active_index] = (comp_block_t*) mem;
		else
			check( false ); // shrink shouldn't fail, but fine if it does
		
		#ifndef NDEBUG
			debug_packer();
		#endif
	}
	active_index = -1;
}

void Nes_Film_Data::init_states() const
{
	memset( active->states, 0, sizeof active->states );
	block_t* b = active;
	b->garbage0 = 1;
	b->garbage1 = 2;
	b->garbage2 = 3;
	for ( int j = 0; j < block_size; j++ )
	{
		Nes_State_& s = b->states [j];
		s.cpu       = &b->cpu [j];
		s.joypad    = &b->joypad [j];
		s.apu       = &b->apu [j];
		s.ppu       = &b->ppu [j];
		s.mapper    = &b->mapper [j];
		s.ram       = b->ram [j];
		s.sram      = b->sram [j];
		s.spr_ram   = b->spr_ram [j];
		s.nametable = b->nametable [j];
		s.chr       = b->chr [j];
		s.set_timestamp( invalid_frame_count );
	}
}

void Nes_Film_Data::access( index_t i ) const
{
	assert( (unsigned) i < (unsigned) block_count );
	if ( active_dirty )
		flush_active();
	active_index = i;
	comp_block_t* b = blocks [i];
	if ( b )
	{
		assert( b->size );
		long size = packer->unpack( b->data, b->size, (byte*) active + b->offset );
		assert( b->offset + size == active_size() );
		if ( b->offset )
			init_states();
	}
	else
	{
		active->joypads [0] = &active->joypad0 [0];
		active->joypads [1] = &active->joypad0 [period_];
		init_states();
		memset( active->joypad0, 0, period_ * 2 );
	}
}

Nes_Film_Data::block_t* Nes_Film_Data::write( int i )
{
	require( (unsigned) i < (unsigned) block_count );
	if ( i != active_index )
		access( i );
	if ( !active_dirty )
	{
		// preallocate now to avoid losing write when flushed later
		long size = packer->worst_case( active_size() - joypad_only_ );
		comp_block_t* new_mem = (comp_block_t*) realloc( blocks [i], size );
		if ( !new_mem )
			return 0;
		new_mem->size = 0;
		new_mem->offset = joypad_only_;
		blocks [i] = new_mem;
		active_dirty = true;
	}
	return active;
}

void Nes_Film_Data::joypad_only( bool b )
{
	flush_active();
	joypad_only_ = b * offsetof (block_t,joypad0);
}

blargg_err_t Nes_Film_Data::resize( int new_count )
{
	if ( new_count < block_count )
	{
		assert( active );
		
		if ( active_index >= new_count )
			flush_active();
		
		for ( int i = new_count; i < block_count; i++ )
			free( blocks [i] );
		
		block_count = new_count;
		void* new_blocks = realloc( blocks, new_count * sizeof *blocks );
		if ( new_blocks || !new_count )
			blocks = (comp_block_t**) new_blocks;
		else
			check( false ); // shrink shouldn't fail, but fine if it does
	}
	else if ( new_count > block_count )
	{
		if ( !packer )
			CHECK_ALLOC( packer = BLARGG_NEW Nes_Film_Packer );
		
		if ( !active )
		{
			assert( period_ );
			active = (block_t*) calloc( active_size(), 1 );
			CHECK_ALLOC( active );
			//init_active(); // TODO: unnecessary since it's called on first access anyway?
			packer->prepare( active, active_size() );
		}
		
		void* new_blocks = realloc( blocks, new_count * sizeof *blocks );
		CHECK_ALLOC( new_blocks );
		blocks = (comp_block_t**) new_blocks;
		
		for ( int i = block_count; i < new_count; i++ )
			blocks [i] = 0;
		
		block_count = new_count;
	}
	return 0;
}

void Nes_Film_Data::clear( frame_count_t period )
{
	active_index = -1;
	active_dirty = false;
	if ( resize( 0 ) )
		check( false ); // shrink should never fail
	joypad_only_ = false;
	period_ = period * block_size;
	free( active );
	active = 0;
}

void Nes_Film_Data::trim( int begin, int new_count )
{
	require( 0 <= begin && begin + new_count <= block_count );
	require( (unsigned) new_count <= (unsigned) block_count );
	if ( (unsigned) new_count < (unsigned) block_count )
	{
		if ( begin || active_index >= new_count )
			flush_active();
		
		if ( begin )
		{
			for ( int i = 0; i < begin; i++ )
				free( blocks [i] );
			memmove( &blocks [0], &blocks [begin], (block_count - begin) * sizeof *blocks );
			block_count -= begin;
		}
		
		if ( resize( new_count ) )
			check( false ); // shrink should never fail
	}
}

Nes_Film_Data::~Nes_Film_Data()
{
	if ( resize( 0 ) )
		check( false ); // shrink should never fail
	free( active );
	delete packer;
}

