/* Copyright (C) 2018.
 * This module is free software; you
 * can redistribute it and/or modify it under the terms of the GNU Lesser
 * General Public License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version. This
 * module is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
 * more details. You should have received a copy of the GNU Lesser General
 * Public License along with this module; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 *
 * This mapper was added by retrowertz for Libretro port of QuickNES.
 *
 * 100-in-1 Contra Function 16
 */

#pragma once

#include "Nes_Mapper.h"

struct Mapper015_state_t
{
	uint8_t prg_bank [ 4 ];
	uint8_t mirroring;
};

BOOST_STATIC_ASSERT( sizeof (Mapper015_state_t) == 5 );

// K-1029, K-1030P

class Mapper015 : public Nes_Mapper, Mapper015_state_t {
public:
	Mapper015()
	{
		i = 0;
		Mapper015_state_t* state = this;
		register_state( state, sizeof *state );
	}

	virtual void reset_state()
	{
		write( 0, 0x8000, 0 );
	}

	virtual void apply_mapping()
	{
		enable_sram();
		set_chr_bank ( 0, bank_8k, 0 );
		for ( i = 0; i < sizeof prg_bank; i++ )
			set_prg_bank ( 0x8000 + ( i * 0x2000 ), bank_8k, prg_bank [i] );
		switch ( mirroring )
		{
		case 0: mirror_vert();  break;
		case 1: mirror_horiz(); break;
		}
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		uint8_t bank  = ( data & 0x3F ) << 1;
		uint8_t sbank = ( data >> 7 ) & 1;

		mirroring = ( data >> 6 ) & 1;
		switch ( addr & 3 )
		{
		case 0:
			for ( i = 0; i < sizeof prg_bank; i++ )
				prg_bank [ i ] = bank + i;
			apply_mapping();
			break;
		case 2:
			for ( i = 0; i < sizeof prg_bank; i++ )
				prg_bank [ i ] = bank | sbank;
			apply_mapping();
			break;
		case 1:
		case 3:
			for ( i = 0; i < sizeof prg_bank; i++ )
			{
				if ( i >= 2 && !( addr & 2 ) )
					bank = 0x7E;
				prg_bank [ i ] = bank + ( i & 1 );
			}
			apply_mapping();
			break;
		}
	}
	
	unsigned long int i;
};

