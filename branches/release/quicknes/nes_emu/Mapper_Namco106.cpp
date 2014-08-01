
// Namco 106 mapper

// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

#include "Nes_Namco_Apu.h"

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

// to do: CHR mapping and nametable handling needs work

struct namco106_state_t
{
	BOOST::uint8_t regs [16];
	BOOST::uint16_t irq_ctr;
	BOOST::uint8_t irq_pending;
	BOOST::uint8_t unused1 [1]; 
};
BOOST_STATIC_ASSERT( sizeof (namco106_state_t) == 20 );

class Mapper_Namco106 : public Nes_Mapper, namco106_state_t {
	Nes_Namco_Apu sound;
	nes_time_t last_time;
public:
	Mapper_Namco106()
	{
		namco106_state_t* state = this;
		register_state( state, sizeof *state );
	}
	
	virtual int channel_count() const { return sound.osc_count; }
	
	virtual void set_channel_buf( int i, Blip_Buffer* b ) { sound.osc_output( i, b ); }
	
	virtual void set_treble( blip_eq_t const& eq ) { sound.treble_eq( eq ); }
	
	void reset_state()
	{
		regs [12] = 0;
		regs [13] = 1;
		regs [14] = last_bank - 1;
		sound.reset();
	}
	
	virtual void apply_mapping()
	{
		last_time = 0;
		enable_sram();
		intercept_writes( 0x4800, 1 );
		intercept_reads ( 0x4800, 1 );
		
		intercept_writes( 0x5000, 0x1000 );
		intercept_reads ( 0x5000, 0x1000 );
		
		for ( int i = 0; i < (int) sizeof regs; i++ )
			write( 0, 0x8000 + i * 0x800, regs [i] );
	}
	
	virtual nes_time_t next_irq( nes_time_t time )
	{
		if ( irq_pending )
			return time;
		
		if ( !(irq_ctr & 0x8000) )
			return no_irq;
		
		return 0x10000 - irq_ctr + last_time;
	}
	
	virtual void run_until( nes_time_t end_time )
	{
		long count = irq_ctr + (end_time - last_time);
		if ( irq_ctr & 0x8000 )
		{
			if ( count > 0xffff )
			{
				count = 0xffff;
				irq_pending = true;
			}
		}
		else if ( count > 0x7fff )
		{
			count = 0x7fff;
		}
		
		irq_ctr = count;
		last_time = end_time;
	}
	
	virtual void end_frame( nes_time_t end_time )
	{
		if ( end_time > last_time )
			run_until( end_time );
		last_time -= end_time;
		assert( last_time >= 0 );
		sound.end_frame( end_time );
	}
	
	void write_bank( nes_addr_t, int data );
	void write_irq( nes_time_t, nes_addr_t, int data );
	
	virtual int read( nes_time_t time, nes_addr_t addr )
	{
		if ( addr == 0x4800 )
			return sound.read_data();
		
		if ( addr == 0x5000 )
		{
			irq_pending = false;
			return irq_ctr & 0xff;
		}
		
		if ( addr == 0x5800 )
		{
			irq_pending = false;
			return irq_ctr >> 8;
		}
		
		return Nes_Mapper::read( time, addr );
	}
	
	virtual bool write_intercepted( nes_time_t time, nes_addr_t addr, int data )
	{
		if ( addr == 0x4800 )
		{
			sound.write_data( time, data );
		}
		else if ( addr == 0x5000 )
		{
			irq_ctr = (irq_ctr & 0xff00) | data;
			irq_pending = false;
			irq_changed();
		}
		else if ( addr == 0x5800 )
		{
			irq_ctr = (data << 8) | (irq_ctr & 0xff);
			irq_pending = false;
			irq_changed();
		}
		else
		{
			return false;
		}
		
		return true;
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		int reg = addr >> 11 & 0x0F;
		regs [reg] = data;
		
		int prg_bank = reg - 0x0c;
		if ( (unsigned) prg_bank < 3 )
		{
			if ( prg_bank == 0 && (data & 0x40) )
				mirror_vert();
			set_prg_bank( 0x8000 | (prg_bank << bank_8k), bank_8k, data & 0x3F );
		}
		else if ( reg < 8 )
		{
			set_chr_bank( reg * 0x400, bank_1k, data );
		}
		else if ( reg < 0x0c )
		{
			mirror_manual( regs [8] & 1, regs [9] & 1, regs [10] & 1, regs [11] & 1 );
		}
		else
		{
			sound.write_addr( data );
		}
	}
};

void register_namco106_mapper();
void register_namco106_mapper()
{
	register_mapper<Mapper_Namco106>( 19 );
}

// in the most obscure place in case crappy linker is used
void register_optional_mappers();
void register_optional_mappers()
{
	extern void register_misc_mappers();
	register_misc_mappers();
	
	extern void register_vrc6_mapper();
	register_vrc6_mapper();
	
	//extern void register_mmc5_mapper();
	//register_mmc5_mapper();
	
	extern void register_fme7_mapper();
	register_fme7_mapper();
	
	extern void register_namco106_mapper();
	register_namco106_mapper();

	extern void register_mmc24();
	register_mmc24();
}

