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
 *
 * Mapper 33 - Taito TC0190
 * 
 */

#pragma once

#include "Nes_Mapper.h"

struct tc0190_state_t
{
	uint8_t preg [ 2 ];
	uint8_t creg [ 6 ];
	uint8_t mirr;
};

BOOST_STATIC_ASSERT( sizeof ( tc0190_state_t ) == 9 );

// TaitoTC0190

class Mapper033 : public Nes_Mapper, tc0190_state_t {
public:
	Mapper033()
	{
		tc0190_state_t *state = this;
		register_state( state, sizeof *state );
	}

	virtual void reset_state()
	{ }

	virtual void apply_mapping()
	{
		for ( int i = 0; i < 2; i++ )
		{
			set_prg_bank ( 0x8000 + ( i << 13 ), bank_8k, preg [ i ] );
			set_chr_bank ( 0x0000 + ( i << 11 ), bank_2k, creg [ i ] );
		}

		for ( int i = 0; i < 4; i++ )
			set_chr_bank ( 0x1000 + ( i << 10 ), bank_1k, creg [ 2 + i ] );

		if ( mirr ) mirror_horiz();
		else mirror_vert();
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		switch ( addr & 0xA003 )
		{
		case 0x8000:
			preg [ 0 ] = data & 0x3F;
			mirr = data >> 6;
			set_prg_bank ( 0x8000, bank_8k, preg [ 0 ] );
			if ( mirr ) mirror_horiz();
			else mirror_vert();
			break;
		case 0x8001:
			preg [ 1 ] = data & 0x3F;
			set_prg_bank ( 0xA000, bank_8k, preg [ 1 ] );
			break;
		case 0x8002: case 0x8003:
			addr &= 0x01;
			creg [ addr ] = data;
			set_chr_bank ( addr << 11, bank_2k, creg [ addr ] );
			break;
		case 0xA000: case 0xA001:
		case 0xA002: case 0xA003:
			addr &= 0x03;
			creg [ 2 + addr ] = data;
			set_chr_bank ( 0x1000 | ( addr << 10 ), bank_1k, creg [ 2 + addr ] );
			break;
		}
	}
};
