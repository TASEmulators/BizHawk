
// Sunsoft FME-7 mapper

// Nes_Emu 0.7.0. http://www.slack.net/~ant/libs/

#include "Nes_Mapper.h"

#include "blargg_endian.h"
#include "Nes_Fme7_Apu.h"

/* Copyright (C) 2005 Chris Moeller */
/* Copyright (C) 2005-2006 Shay Green. This module is free software; you
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

struct fme7_state_t
{
	// first 16 bytes in register order
	BOOST::uint8_t regs [13];
	BOOST::uint8_t irq_mode;
	BOOST::uint16_t irq_count;
	
	BOOST::uint8_t command;
	BOOST::uint8_t irq_pending;
	fme7_apu_state_t sound_state; // only used when saving/restoring state
	
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (fme7_state_t) == 18 + sizeof (fme7_apu_state_t) );

void fme7_state_t::swap()
{
	set_le16( &irq_count, irq_count );
	for ( unsigned i = 0; i < sizeof sound_state.delays / sizeof sound_state.delays [0]; i++ )
		set_le16( &sound_state.delays [i], sound_state.delays [i] );
}

class Mapper_Fme7 : public Nes_Mapper, fme7_state_t {
	nes_time_t last_time;
	Nes_Fme7_Apu sound;
public:
	Mapper_Fme7()
	{
		fme7_state_t* state = this;
		register_state( state, sizeof *state );
	}
	
	virtual int channel_count() const { return sound.osc_count; }

	virtual void set_channel_buf( int i, Blip_Buffer* b ) { sound.osc_output( i, b ); }

	virtual void set_treble( blip_eq_t const& eq ) { sound.treble_eq( eq ); }
	
	virtual void reset_state()
	{
		regs [8] = 0x40; // wram disabled
		irq_count = 0xFFFF;
		sound.reset();
	}
	
	virtual void save_state( mapper_state_t& out )
	{
		sound.save_state( &sound_state );
		fme7_state_t::swap();
		Nes_Mapper::save_state( out );
		fme7_state_t::swap(); // to do: kind of hacky to swap in place
	}
	
	virtual void read_state( mapper_state_t const& in )
	{
		Nes_Mapper::read_state( in );
		fme7_state_t::swap();
		sound.load_state( sound_state );
	}
	
	void write_register( int index, int data );
	
	virtual void apply_mapping()
	{
		last_time = 0;
		for ( int i = 0; i < (int) sizeof regs; i++ )
			write_register( i, regs [i] );
	}

	virtual void run_until( nes_time_t end_time )
	{
		int new_count = irq_count - (end_time - last_time);
		last_time = end_time;
		
		if ( new_count <= 0 && (irq_mode & 0x81) == 0x81 )
			irq_pending = true;
		
		if ( irq_mode & 0x01 )
			irq_count = new_count & 0xFFFF;
	}
	
	virtual nes_time_t next_irq( nes_time_t )
	{
		if ( irq_pending )
			return 0;
		
		if ( (irq_mode & 0x81) == 0x81 )
			return last_time + irq_count + 1;
		
		return no_irq;
	}
	
	virtual void end_frame( nes_time_t end_time )
	{
		if ( end_time > last_time )
			run_until( end_time );
		
		last_time -= end_time;
		assert( last_time >= 0 );
		
		sound.end_frame( end_time );
	}
	
	void write_irq( nes_time_t, int index, int data );
	
	virtual void write( nes_time_t time, nes_addr_t addr, int data )
	{
		switch ( addr & 0xE000 )
		{
		case 0x8000:
			command = data & 0x0F;
			break;
		
		case 0xA000:
			if ( command < 0x0D )
				write_register( command, data );
			else
				write_irq( time, command, data );
			break;
		
		case 0xC000:
			sound.write_latch( data );
			break;
		
		case 0xE000:
			sound.write_data( time, data );
			break;
		}
	}
};

void Mapper_Fme7::write_irq( nes_time_t time, int index, int data )
{
	run_until( time );
	switch ( index )
	{
	case 0x0D:
		irq_mode = data;
		irq_pending = false;
		irq_changed();
		break;

	case 0x0E:
		irq_count = (irq_count & 0xFF00) | data;
		break;

	case 0x0F:
		irq_count = data << 8 | (irq_count & 0xFF);
		break;
	}
	
}

void Mapper_Fme7::write_register( int index, int data )
{
	regs [index] = data;
	int prg_bank = index - 0x09;
	if ( (unsigned) prg_bank < 3 ) // most common
	{
		set_prg_bank( 0x8000 | (prg_bank << bank_8k), bank_8k, data );
	}
	else if ( index == 0x08 )
	{
		enable_sram( (data & 0xC0) == 0xC0 );
		if ( !(data & 0xC0) )
			set_prg_bank( 0x6000, bank_8k, data & 0x3F );
	}
	else if ( index < 0x08 )
	{
		set_chr_bank( index * 0x400, bank_1k, data );
	}
	else
	{
		assert( index == 0x0C );
		if ( data & 2 )
			mirror_single( data & 1 );
		else if ( data & 1 )
			mirror_horiz();
		else
			mirror_vert();
	}
}

void register_fme7_mapper();
void register_fme7_mapper()
{
	register_mapper<Mapper_Fme7>( 69 );
}

