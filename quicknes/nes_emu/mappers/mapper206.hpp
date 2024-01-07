/* Copyright notice for this file:
 * Copyright (C) 2018
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 *
 * This mapper was added by retrowertz for Libretro port of QuickNES.
 * 3/24/18
 *
 * Mapper  88
 * Mapper 154
 * Mapper 206
 */

#pragma once

#include "Nes_Mapper.h"

struct namco_34xx_state_t
{
	uint8_t bank [ 8 ];
	uint8_t mirr;
	uint8_t mode;
};

BOOST_STATIC_ASSERT( sizeof (namco_34xx_state_t) == 10 );

// Namco_34xx

class Mapper206 : public Nes_Mapper, namco_34xx_state_t {
public:
	Mapper206()
	{
		namco_34xx_state_t *state = this;
		register_state( state, sizeof *state );
	}

	virtual void reset_state()
	{ }

	virtual void apply_mapping()
	{
		set_chr_bank( 0x0000, bank_2k, bank [ 0 ] );
		set_chr_bank( 0x0800, bank_2k, bank [ 1 ] );
		for ( int i = 0; i < 4; i++ )
			set_chr_bank( 0x1000 + ( i << 10 ), bank_1k, bank [ i + 2 ] );

		set_prg_bank( 0x8000, bank_8k, bank [ 6 ] );
		set_prg_bank( 0xA000, bank_8k, bank [ 7 ] );
		set_prg_bank( 0xC000, bank_8k, ~1 );
		set_prg_bank( 0xE000, bank_8k, ~0 );
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		switch ( addr & 0xE001 )
		{
		case 0x8000:
			mode = data;
			break;
		case 0x8001:
			mode &= 0x07;
			switch ( mode )
			{
			case 0: case 1:
				bank [ mode ] = data >> 1;
				set_chr_bank( 0x0000 + ( mode << 11 ), bank_2k, bank [ mode ] );
				break;
			case 2: case 3: case 4: case 5:
				bank [ mode ] = data;
				set_chr_bank( 0x1000 + ( ( mode - 2 ) << 10 ), bank_1k, bank [ mode ] );
				break;
			case 6: case 7:
				bank [ mode ] = data;
				set_prg_bank( 0x8000 + ( ( mode - 6 ) << 13 ), bank_8k, bank [ mode ] );
				break;
			}
			break;
		}
	}
};

