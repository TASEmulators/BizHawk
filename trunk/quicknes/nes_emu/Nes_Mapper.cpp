
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

#include <string.h>
#include "Nes_Core.h"

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

Nes_Mapper::Nes_Mapper()
{
	emu_ = NULL;
	static char c;
	state = &c; // TODO: state must not be null?
	state_size = 0;
}

Nes_Mapper::~Nes_Mapper()
{
}

// Sets mirroring, maps first 8K CHR in, first and last 16K of PRG,
// intercepts writes to upper half of memory, and clears registered state.
void Nes_Mapper::default_reset_state()
{
	int mirroring = cart_->mirroring();
	if ( mirroring & 8 )
		mirror_full();
	else if ( mirroring & 1 )
		mirror_vert();
	else
		mirror_horiz();
	
	set_chr_bank( 0, bank_8k, 0 );
	
	set_prg_bank( 0x8000, bank_16k, 0 );
	set_prg_bank( 0xC000, bank_16k, last_bank );
	
	intercept_writes( 0x8000, 0x8000 );
	
	memset( state, 0, state_size );
}

void Nes_Mapper::reset()
{
	default_reset_state();
	reset_state();
	apply_mapping();
}

void mapper_state_t::write( const void* p, unsigned long s )
{
	require( s <= max_mapper_state_size );
	require( !size );
	size = s;
	memcpy( data, p, s );
}

int mapper_state_t::read( void* p, unsigned long s ) const
{
	if ( (long) s > size )
		s = size;
	memcpy( p, data, s );
	return s;
}

void Nes_Mapper::save_state( mapper_state_t& out )
{
	out.write( state, state_size );
}

void Nes_Mapper::load_state( mapper_state_t const& in )
{
	default_reset_state();
	read_state( in );
	apply_mapping();
}

void Nes_Mapper::read_state( mapper_state_t const& in )
{
	memset( state, 0, state_size );
	in.read( state, state_size );
	apply_mapping();
}

// Timing

void Nes_Mapper::irq_changed() { emu_->irq_changed(); }
	
nes_time_t Nes_Mapper::next_irq( nes_time_t ) { return no_irq; }

void Nes_Mapper::a12_clocked() { }

void Nes_Mapper::run_until( nes_time_t ) { }

void Nes_Mapper::end_frame( nes_time_t ) { }

bool Nes_Mapper::ppu_enabled() const { return emu().ppu.w2001 & 0x08; }

// Sound

int Nes_Mapper::channel_count() const { return 0; }

void Nes_Mapper::set_channel_buf( int, Blip_Buffer* ) { require( false ); }

void Nes_Mapper::set_treble( blip_eq_t const& ) { }

// Memory mapping

void Nes_Mapper::set_prg_bank( nes_addr_t addr, bank_size_t bs, int bank )
{
	require( addr >= 0x2000 ); // can't remap low-memory
	
	int bank_size = 1 << bs;
	require( addr % bank_size == 0 ); // must be aligned
	
	int bank_count = cart_->prg_size() >> bs;
	if ( bank < 0 )
		bank += bank_count;
	
	if ( bank >= bank_count )
	{
		check( !(cart_->prg_size() & (cart_->prg_size() - 1)) ); // ensure PRG size is power of 2
		bank %= bank_count;
	}
	
	emu().map_code( addr, bank_size, cart_->prg() + (bank << bs) );
	
	if ( unsigned (addr - 0x6000) < 0x2000 )
		emu().enable_prg_6000();
}

void Nes_Mapper::set_chr_bank( nes_addr_t addr, bank_size_t bs, int bank )
{
	emu().ppu.render_until( emu().clock() ); 
	emu().ppu.set_chr_bank( addr, 1 << bs, bank << bs );
}

void Nes_Mapper::set_chr_bank_ex( nes_addr_t addr, bank_size_t bs, int bank )
{
	emu().ppu.render_until( emu().clock() ); 
	emu().ppu.set_chr_bank_ex( addr, 1 << bs, bank << bs );
}

void Nes_Mapper::mirror_manual( int page0, int page1, int page2, int page3 )
{
	emu().ppu.render_bg_until( emu().clock() ); 
	emu().ppu.set_nt_banks( page0, page1, page2, page3 );
}

#ifndef NDEBUG
int Nes_Mapper::handle_bus_conflict( nes_addr_t addr, int data )
{
	if ( emu().Nes_Cpu::get_code( addr ) [0] != data )
		dprintf( "Mapper write had bus conflict\n" );
	return data;
}
#endif

// Mapper registration

int const max_mappers = 32;
Nes_Mapper::mapping_t Nes_Mapper::mappers [max_mappers] =
{
	{ 0, Nes_Mapper::make_nrom },
	{ 1, Nes_Mapper::make_mmc1 },
	{ 2, Nes_Mapper::make_unrom },
	{ 3, Nes_Mapper::make_cnrom },
	{ 4, Nes_Mapper::make_mmc3 },
	{ 7, Nes_Mapper::make_aorom }
};
static int mapper_count = 6; // to do: keep synchronized with pre-supplied mappers above

Nes_Mapper::creator_func_t Nes_Mapper::get_mapper_creator( int code )
{
	for ( int i = 0; i < mapper_count; i++ )
	{
		if ( mappers [i].code == code )
			return mappers [i].func;
	}
	return NULL;
}

void Nes_Mapper::register_mapper( int code, creator_func_t func )
{
	// Catch attempted registration of a different creation function for same mapper code
	require( !get_mapper_creator( code ) || get_mapper_creator( code ) == func );
	require( mapper_count < max_mappers ); // fixed liming on number of registered mappers
	
	mapping_t& m = mappers [mapper_count++];
	m.code = code;
	m.func = func;
}

Nes_Mapper* Nes_Mapper::create( Nes_Cart const* cart, Nes_Core* emu )
{
	Nes_Mapper::creator_func_t func = get_mapper_creator( cart->mapper_code() );
	if ( !func )
		return NULL;
	
	// to do: out of memory will be reported as unsupported mapper
	Nes_Mapper* mapper = func();
	if ( mapper )
	{
		mapper->cart_ = cart;
		mapper->emu_ = emu;
	}
	return mapper;
}

