
// NES MMC5 mapper, currently only tailored for Castlevania 3 (U)

// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

#include "Nes_Core.h"
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

struct mmc5_state_t
{
	enum { reg_count = 0x30 };
	byte regs [0x30];
	byte irq_enabled;
};
// to do: finalize state format
BOOST_STATIC_ASSERT( sizeof (mmc5_state_t) == 0x31 );

class Mapper_Mmc5 : public Nes_Mapper, mmc5_state_t {
	nes_time_t irq_time;
public:
	Mapper_Mmc5()
	{
		mmc5_state_t* state = this;
		register_state( state, sizeof *state );
	}
	
	virtual void reset_state()
	{
		irq_time = no_irq;
		regs [0x00] = 2;
		regs [0x01] = 3;
		regs [0x14] = 0x7f;
		regs [0x15] = 0x7f;
		regs [0x16] = 0x7f;
		regs [0x17] = 0x7f;
	}
	
	virtual void read_state( mapper_state_t const& in )
	{
		Nes_Mapper::read_state( in );
		irq_time = no_irq;
	}
	
	enum { regs_addr = 0x5100 };
	
	virtual void apply_mapping();
	
	virtual nes_time_t next_irq( nes_time_t )
	{
		if ( irq_enabled & 0x80 )
			return irq_time;
		
		return no_irq;
	}
	
	virtual bool write_intercepted( nes_time_t time, nes_addr_t addr, int data )
	{
		int reg = addr - regs_addr;
		if ( (unsigned) reg < reg_count )
		{
			regs [reg] = data;
			switch ( reg )
			{
			case 0x05:
				mirror_manual( data & 3, data >> 2 & 3,
						data >> 4 & 3, data >> 6 & 3 );
				break;
			
			case 0x15:
				set_prg_bank( 0x8000, bank_16k, data >> 1 & 0x3f );
				break;
			
			case 0x16:
				set_prg_bank( 0xC000, bank_8k, data & 0x7f );
				break;
			
			case 0x17:
				set_prg_bank( 0xE000, bank_8k, data & 0x7f );
				break;
			
			case 0x20:
			case 0x21:
			case 0x22:
			case 0x23:
			case 0x28:
			case 0x29:
			case 0x2a:
			case 0x2b:
				set_chr_bank( ((reg >> 1 & 4) + (reg & 3)) * 0x400, bank_1k, data );
				break;
			}
			check( (regs [0x00] & 3) == 2 );
			check( (regs [0x01] & 3) == 3 );
		}
		else if ( addr == 0x5203 )
		{
			irq_time = no_irq;
			if ( data && data < 240 )
			{
				irq_time = (341 * 21 + 128 + (data * 341)) / 3;
				if ( irq_time < time )
					irq_time = no_irq;
			}
			irq_changed();
		}
		else if ( addr == 0x5204 )
		{
			irq_enabled = data;
			irq_changed();
		}
		else
		{
			return false;
		}
		
		return true;
	}
	
	virtual void write( nes_time_t, nes_addr_t, int ) { }
};

void Mapper_Mmc5::apply_mapping()
{
	static unsigned char list [] = {
		0x05, 0x15, 0x16, 0x17,
		0x20, 0x21, 0x22, 0x23,
		0x28, 0x29, 0x2a, 0x2b
	};
	
	for ( int i = 0; i < (int) sizeof list; i++ )
		write_intercepted( 0, regs_addr + list [i], regs [list [i]] );
	intercept_writes( 0x5100, 0x200 );
}

void register_mmc5_mapper();
void register_mmc5_mapper()
{
	register_mapper<Mapper_Mmc5>( 5 );
}

