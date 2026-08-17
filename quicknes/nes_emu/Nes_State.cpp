
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_State.h"

#include <stdlib.h>
#include <string.h>

#include "blargg_endian.h"
#include "Nes_Emu.h"
#include "Nes_Mapper.h"

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

int mem_differs( void const* p, int cmp, unsigned long s )
{
	unsigned char const* cp = (unsigned char*) p;
	while ( s-- )
	{
		if ( *cp++ != cmp )
			return 1;
	}
	return 0;
}

Nes_State::Nes_State()
{
	Nes_State_::cpu         = &this->cpu;
	Nes_State_::joypad      = &this->joypad;
	Nes_State_::apu         = &this->apu;
	Nes_State_::ppu         = &this->ppu;
	Nes_State_::mapper      = &this->mapper;
	Nes_State_::ram         = this->ram;
	Nes_State_::sram        = this->sram;
	Nes_State_::spr_ram     = this->spr_ram;
	Nes_State_::nametable   = this->nametable;
	Nes_State_::chr         = this->chr;
}

void Nes_State_::clear()
{
	memset( &nes, 0, sizeof nes );
	nes.frame_count = static_cast<unsigned>(invalid_frame_count);
	
	nes_valid      = false;
	cpu_valid      = false;
	joypad_valid   = false;
	apu_valid      = false;
	ppu_valid      = false;
	mapper_valid   = false;
	ram_valid      = false;
	sram_size      = 0;
	spr_ram_valid  = false;
	nametable_size = 0;
	chr_size       = 0;
}

// write

blargg_err_t Nes_State_Writer::end( Nes_Emu const& emu )
{
	Nes_State* state = BLARGG_NEW Nes_State;
	CHECK_ALLOC( state );
	emu.save_state( state );
	blargg_err_t err = end( *state );
	delete state;
	return err;
}

blargg_err_t Nes_State_Writer::end( Nes_State const& ss )
{
	RETURN_ERR( ss.write_blocks( *this ) );
	return Nes_File_Writer::end();
}

blargg_err_t Nes_State::write( Auto_File_Writer out ) const
{
	Nes_State_Writer writer;
	RETURN_ERR( writer.begin( out ) );
	return writer.end( *this );
}

blargg_err_t Nes_State_::write_blocks( Nes_File_Writer& out ) const
{
	if ( nes_valid )
	{
		nes_state_t s = nes;
		s.timestamp *= 5;
		RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( cpu_valid )
	{
		cpu_state_t s;
		memset( &s, 0, sizeof s );
		s.pc = cpu->pc;
		s.s = cpu->sp;
		s.a = cpu->a;
		s.x = cpu->x;
		s.y = cpu->y;
		s.p = cpu->status;
		RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( ppu_valid )
	{
		ppu_state_t s = *ppu;
		RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( apu_valid )
	{
		apu_state_t s = *apu;
		RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( joypad_valid )
	{
		joypad_state_t s = *joypad;
		RETURN_ERR( write_nes_state( out, s ) );
	}
	
	if ( mapper_valid )
		RETURN_ERR( out.write_block( FOUR_CHAR('MAPR'), mapper->data, mapper->size ) );
	
	if ( ram_valid )
		RETURN_ERR( out.write_block( FOUR_CHAR('LRAM'), ram, ram_size ) );
	
	if ( spr_ram_valid )
		RETURN_ERR( out.write_block( FOUR_CHAR('SPRT'), spr_ram, spr_ram_size ) );
	
	if ( nametable_size )
	{
		check( nametable_size == 0x800 || nametable_size == 0x1000 );
		RETURN_ERR( out.write_block_header( FOUR_CHAR('NTAB'), nametable_size ) );
		RETURN_ERR( out.write( nametable, 0x800 ) );
		if ( nametable_size > 0x800 )
			RETURN_ERR( out.write( chr, 0x800 ) );
	}
	
	if ( chr_size )
		RETURN_ERR( out.write_block( FOUR_CHAR('CHRR'), chr, chr_size ) );
	
#ifdef __LIBRETRO__ // Maintain constant save state size.
	if ( sram_size )
		RETURN_ERR( out.write_block( FOUR_CHAR('SRAM'), sram, sram_size ) );
#else
	// only save sram if it's been modified
	if ( sram_size && mem_differs( sram, 0xff, sram_size ) )
		RETURN_ERR( out.write_block( FOUR_CHAR('SRAM'), sram, sram_size ) );
#endif
	
	return 0;
}

// read

Nes_State_Reader::Nes_State_Reader() { state_ = 0; owned = 0; }

Nes_State_Reader::~Nes_State_Reader() { delete owned; }

blargg_err_t Nes_State_Reader::begin( Auto_File_Reader dr, Nes_State* out )
{
	state_ = out;
	if ( !out )
		CHECK_ALLOC( state_ = owned = BLARGG_NEW Nes_State );
	
	RETURN_ERR( Nes_File_Reader::begin( dr ) );
	if ( block_tag() != state_file_tag )
		return "Not a state snapshot file";
	return 0;
}

blargg_err_t Nes_State::read( Auto_File_Reader in )
{
	Nes_State_Reader reader;
	RETURN_ERR( reader.begin( in, this ) );
	while ( !reader.done() )
		RETURN_ERR( reader.next_block() );
	
	return 0;
}

blargg_err_t Nes_State_Reader::next_block()
{
	if ( depth() != 0 )
		return Nes_File_Reader::next_block();
	return state_->read_blocks( *this );
}

void Nes_State_::set_nes_state( nes_state_t const& s )
{
	nes = s;
	nes.timestamp /= 5;
	nes_valid = true;
}

blargg_err_t Nes_State_::read_blocks( Nes_File_Reader& in )
{
	while ( true )
	{
		RETURN_ERR( in.next_block() );
		switch ( in.block_tag() )
		{
		case nes_state_t::tag:
			memset( &nes, 0, sizeof nes );
			RETURN_ERR( read_nes_state( in, &nes ) );
			set_nes_state( nes );
			break;
		
		case cpu_state_t::tag: {
			cpu_state_t s;
			memset( &s, 0, sizeof s );
			RETURN_ERR( read_nes_state( in, &s ) );
			cpu->pc = s.pc;
			cpu->sp = s.s;
			cpu->a = s.a;
			cpu->x = s.x;
			cpu->y = s.y;
			cpu->status = s.p;
			cpu_valid = true;
			break;
		}
		
		case ppu_state_t::tag:
			memset( ppu, 0, sizeof *ppu );
			RETURN_ERR( read_nes_state( in, ppu ) );
			ppu_valid = true;
			break;
		
		case apu_state_t::tag:
			memset( apu, 0, sizeof *apu );
			RETURN_ERR( read_nes_state( in, apu ) );
			apu_valid = true;
			break;
		
		case joypad_state_t::tag:
			memset( joypad, 0, sizeof *joypad );
			RETURN_ERR( read_nes_state( in, joypad ) );
			joypad_valid = true;
			break;
		
		case FOUR_CHAR('MAPR'):
			mapper->size = in.remain();
			RETURN_ERR( in.read_block_data( mapper->data, sizeof mapper->data ) );
			mapper_valid = true;
			break;
		
		case FOUR_CHAR('SPRT'):
			spr_ram_valid = true;
			RETURN_ERR( in.read_block_data( spr_ram, spr_ram_size ) );
			break;
			
		case FOUR_CHAR('NTAB'):
			nametable_size = in.remain();
			check( nametable_size == 0x800 || nametable_size == 0x1000 );
			RETURN_ERR( in.read( nametable, 0x800 ) );
			if ( nametable_size > 0x800 )
				RETURN_ERR( in.read( chr, 0x800 ) );
			break;
			
		case FOUR_CHAR('LRAM'):
			ram_valid = true;
			RETURN_ERR( in.read_block_data( ram, ram_size ) );
			break;
			
		case FOUR_CHAR('CHRR'):
			chr_size = in.remain();
			RETURN_ERR( in.read_block_data( chr, chr_max ) );
			break;
			
		case FOUR_CHAR('SRAM'):
			sram_size = in.remain();
			RETURN_ERR( in.read_block_data( sram, sram_max ) );
			break; 
		
		default:
			return 0;
		}
	}
}

