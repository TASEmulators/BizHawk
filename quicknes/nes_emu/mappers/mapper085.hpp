// Nes_Emu 0.5.4. http://www.slack.net/~ant/

#include "Nes_Mapper.h"
#include "Nes_Vrc7.h"
#include "blargg_endian.h"
#include <string.h>

#pragma once

/* Copyright (C) 2004-2005 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

struct vrc7_state_t
{
	// written registers
	uint8_t mirroring;
	uint8_t prg_banks [3];
	uint8_t chr_banks [8];
	uint8_t irq_reload;
	uint8_t irq_mode;
	
	// internal state
	uint16_t next_time;
	uint8_t irq_pending;
	uint8_t unused;
	
	vrc7_snapshot_t sound_state;
};
BOOST_STATIC_ASSERT( sizeof (vrc7_state_t) == 20 + sizeof (vrc7_snapshot_t) );

// Vrc7

class Mapper085 : public Nes_Mapper, vrc7_state_t {
public:
	Mapper085()
	{
		vrc7_state_t* state = this;
		register_state( state, sizeof *state );
	}
	
	virtual int channel_count() const { return sound.osc_count; }
	
	virtual void set_channel_buf( int i, Blip_Buffer* b ) { sound.osc_output( i, b ); }
	
	virtual void set_treble( blip_eq_t const& eq ) { sound.treble_eq( eq ); }
	
	virtual void save_state( mapper_state_t & out )
	{
		sound.save_snapshot( &sound_state );
		
		set_le16( &next_time, next_time );
		
		Nes_Mapper::save_state( out );
	}
	
	virtual void load_state( mapper_state_t const& in )
	{
		Nes_Mapper::load_state( in );
		
		next_time = get_le16( &next_time );
		
		sound.load_snapshot( sound_state, in.size );
	}
	
	virtual void reset_state()
	{
		mirroring = 0;
		
		memset( prg_banks, 0, sizeof prg_banks );
		memset( chr_banks, 0, sizeof chr_banks );
		memset( &sound_state, 0, sizeof sound_state );
		
		irq_reload = 0;
		irq_mode = 0;
		irq_pending = false;
		
		next_time = 0;
		sound.reset();
		
		set_prg_bank( 0xE000, bank_8k, last_bank );
		apply_mapping();
	}

	void write_prg_bank( int bank, int data )
	{
		prg_banks [bank] = data;
		set_prg_bank( 0x8000 | ( bank << bank_8k ), bank_8k, data );
	}
	
	void write_chr_bank( int bank, int data )
	{
		//dprintf( "change chr bank %d\n", bank );
		chr_banks [bank] = data;
		set_chr_bank( bank * 0x400, bank_1k, data );
	}
	
	void write_mirroring( int data )
	{
		mirroring = data;
		
		//dprintf( "Change mirroring %d\n", data );
		enable_sram( data & 128, data & 64 );

		if ( data & 2 )
			mirror_single( data & 1 );
		else if ( data & 1 )
			mirror_horiz();
		else
			mirror_vert();
	}

	void apply_mapping()
	{
		size_t i;

		for ( i = 0; i < sizeof prg_banks; i++ )
			write_prg_bank( i, prg_banks [i] );
		
		for ( i = 0; i < sizeof chr_banks; i++ )
			write_chr_bank( i, chr_banks [i] );
		
		write_mirroring( mirroring );
	}
	
	void reset_timer( nes_time_t present )
	{
		next_time = present + unsigned ((0x100 - irq_reload) * timer_period) / 4;
	}
	
	virtual void run_until( nes_time_t end_time )
	{
		if ( irq_mode & 2 )
		{
			while ( next_time < end_time )
			{
				//dprintf( "%d timer expired\n", next_time );
				irq_pending = true;
				reset_timer( next_time );
			}
		}
	}
	
	virtual void end_frame( nes_time_t end_time )
	{
		run_until( end_time );
		
		next_time -= end_time;
		
		sound.end_frame( end_time );
	}
	
	virtual nes_time_t next_irq( nes_time_t present )
	{
		if ( irq_pending )
			return present;
		
		if ( irq_mode & 2 )
			return next_time + 1;
		
		return no_irq;
	}
	
	virtual void write( nes_time_t time, nes_addr_t addr, int data )
	{
		addr |= ( addr & 8 ) << 1;

		if ( addr >= 0xe010 )
		{
			// IRQ
			run_until( time );
			//dprintf( "%d VRC6 IRQ [%d] = %02X\n", time, addr & 3, data );
			switch ( addr & 0xf010 )
			{
				case 0xe010:
					irq_reload = data;
					break;
				
				case 0xf000:
					irq_pending = false;
					irq_mode = data;
					if ( data & 2 )
						reset_timer( time );
					break;
				
				case 0xf010:
					irq_pending = false;
					irq_mode = (irq_mode & ~2) | ((irq_mode << 1) & 2);
					break;
			}
			irq_changed();
		}
		else if ( ( unsigned ) ( addr - 0xa000 ) < 0x4000 )
		{
			write_chr_bank( ((addr >> 4) & 1) | (((addr - 0xa000) >> 11)&~1), data );
		}
		else switch ( addr & 0xf010 )
		{
			case 0x8000: write_prg_bank( 0, data ); break;
			case 0x8010: write_prg_bank( 1, data ); break;
			case 0x9000: write_prg_bank( 2, data ); break;
			
			case 0xe000:
				write_mirroring( data );
				break;

			case 0x9010:
				if ( addr & 0x20 ) sound.write_data( time, data );
				else sound.write_reg( data );
				break;
		}
	}

	Nes_Vrc7 sound;
	enum { timer_period = 113 * 4 + 3 };
};

