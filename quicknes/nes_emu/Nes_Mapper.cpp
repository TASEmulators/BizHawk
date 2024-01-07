
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

/*
  New mapping distribution by Sergio Martin (eien86)
  https://github.com/SergioMartin86/jaffarPlus
*/
#include "mappers/mapper000.hpp"
#include "mappers/mapper001.hpp"
#include "mappers/mapper002.hpp"
#include "mappers/mapper003.hpp"
#include "mappers/mapper004.hpp"
#include "mappers/mapper005.hpp"
#include "mappers/mapper007.hpp"
#include "mappers/mapper009.hpp"
#include "mappers/mapper010.hpp"
#include "mappers/mapper011.hpp"
#include "mappers/mapper015.hpp"
#include "mappers/mapper019.hpp"
#include "mappers/mapper021.hpp"
#include "mappers/mapper022.hpp"
#include "mappers/mapper023.hpp"
#include "mappers/mapper024.hpp"
#include "mappers/mapper025.hpp"
#include "mappers/mapper026.hpp"
#include "mappers/mapper030.hpp"
#include "mappers/mapper032.hpp"
#include "mappers/mapper033.hpp"
#include "mappers/mapper034.hpp"
#include "mappers/mapper060.hpp"
#include "mappers/mapper066.hpp"
#include "mappers/mapper069.hpp"
#include "mappers/mapper070.hpp"
#include "mappers/mapper071.hpp"
#include "mappers/mapper073.hpp"
#include "mappers/mapper075.hpp"
#include "mappers/mapper078.hpp"
#include "mappers/mapper079.hpp"
#include "mappers/mapper085.hpp"
#include "mappers/mapper086.hpp"
#include "mappers/mapper087.hpp"
#include "mappers/mapper088.hpp"
#include "mappers/mapper089.hpp"
#include "mappers/mapper093.hpp"
#include "mappers/mapper094.hpp"
#include "mappers/mapper097.hpp"
#include "mappers/mapper113.hpp"
#include "mappers/mapper140.hpp"
#include "mappers/mapper152.hpp"
#include "mappers/mapper154.hpp"
#include "mappers/mapper156.hpp"
#include "mappers/mapper180.hpp"
#include "mappers/mapper184.hpp"
#include "mappers/mapper190.hpp"
#include "mappers/mapper193.hpp"
#include "mappers/mapper206.hpp"
#include "mappers/mapper207.hpp"
#include "mappers/mapper232.hpp"
#include "mappers/mapper240.hpp"
#include "mappers/mapper241.hpp"
#include "mappers/mapper244.hpp"
#include "mappers/mapper246.hpp"

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

Nes_Mapper* Nes_Mapper::create( Nes_Cart const* cart, Nes_Core* emu )
{
  // Getting cartdrige mapper code
  auto mapperCode = cart->mapper_code();
	
  // Storage for the mapper, NULL by default	
  Nes_Mapper* mapper = NULL;

  // Now checking if the detected mapper code is supported
  if (mapperCode ==   0) mapper = new Mapper000();
  if (mapperCode ==   1) mapper = new Mapper001();
  if (mapperCode ==   2) mapper = new Mapper002();
  if (mapperCode ==   3) mapper = new Mapper003();
  if (mapperCode ==   4) mapper = new Mapper004();
  if (mapperCode ==   5) mapper = new Mapper005();
  if (mapperCode ==   7) mapper = new Mapper007();
  if (mapperCode ==   9) mapper = new Mapper009();
  if (mapperCode ==  10) mapper = new Mapper010();
  if (mapperCode ==  11) mapper = new Mapper011();
  if (mapperCode ==  15) mapper = new Mapper015();
  if (mapperCode ==  19) mapper = new Mapper019();
  if (mapperCode ==  21) mapper = new Mapper021();
  if (mapperCode ==  22) mapper = new Mapper022();
  if (mapperCode ==  23) mapper = new Mapper023();
  if (mapperCode ==  24) mapper = new Mapper024();
  if (mapperCode ==  25) mapper = new Mapper025();
  if (mapperCode ==  26) mapper = new Mapper026();
  if (mapperCode ==  30) mapper = new Mapper030();
  if (mapperCode ==  32) mapper = new Mapper032();
  if (mapperCode ==  33) mapper = new Mapper033();
  if (mapperCode ==  34) mapper = new Mapper034();
  if (mapperCode ==  60) mapper = new Mapper060();
  if (mapperCode ==  66) mapper = new Mapper066();
  if (mapperCode ==  69) mapper = new Mapper069();
  if (mapperCode ==  70) mapper = new Mapper070();
  if (mapperCode ==  71) mapper = new Mapper071();
  if (mapperCode ==  73) mapper = new Mapper073();
  if (mapperCode ==  75) mapper = new Mapper075();
  if (mapperCode ==  78) mapper = new Mapper078();
  if (mapperCode ==  79) mapper = new Mapper079();
  if (mapperCode ==  85) mapper = new Mapper085();
  if (mapperCode ==  86) mapper = new Mapper086();
  if (mapperCode ==  87) mapper = new Mapper087();
  if (mapperCode ==  88) mapper = new Mapper088();
  if (mapperCode ==  89) mapper = new Mapper089();
  if (mapperCode ==  93) mapper = new Mapper093();
  if (mapperCode ==  94) mapper = new Mapper094();
  if (mapperCode ==  97) mapper = new Mapper097();
  if (mapperCode == 113) mapper = new Mapper113();
  if (mapperCode == 140) mapper = new Mapper140();
  if (mapperCode == 152) mapper = new Mapper152();
  if (mapperCode == 154) mapper = new Mapper154();
  if (mapperCode == 156) mapper = new Mapper156();
  if (mapperCode == 180) mapper = new Mapper180();
  if (mapperCode == 184) mapper = new Mapper184();
  if (mapperCode == 190) mapper = new Mapper190();
  if (mapperCode == 193) mapper = new Mapper193();
  if (mapperCode == 206) mapper = new Mapper206();
  if (mapperCode == 207) mapper = new Mapper207();
  if (mapperCode == 232) mapper = new Mapper232();
  if (mapperCode == 240) mapper = new Mapper240();
  if (mapperCode == 241) mapper = new Mapper241();
  if (mapperCode == 244) mapper = new Mapper244();
  if (mapperCode == 246) mapper = new Mapper246();

  // If no mapper was found, return null (error) now 
  if (mapper == NULL)
  {
	fprintf(stderr, "Could not find mapper for code: %u\n", mapperCode);
    return NULL;
  } 

  // Assigning backwards pointers to cartdrige and emulator now
  mapper->cart_ = cart;
  mapper->emu_ = emu;

  // Returning successfully created mapper
  return mapper;
}