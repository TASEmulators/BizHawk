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
 * Mapper 244 - Decathlon
 *
 */

#pragma once

#include "Nes_Mapper.h"

// https://www.nesdev.org/wiki/INES_Mapper244

struct mapper244_state_t
{
	uint8_t preg;
	uint8_t creg;
};

BOOST_STATIC_ASSERT( sizeof (mapper244_state_t) == 2 );

class Mapper244 : public Nes_Mapper, mapper244_state_t {
public:
	Mapper244()
	{
		mapper244_state_t *state = this;
		register_state( state, sizeof *state );
	}

	virtual void reset_state()
	{ }

	virtual void apply_mapping()
	{
		set_prg_bank( 0x8000, bank_32k, preg );
		set_chr_bank( 0x0000, bank_8k, creg );
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr >= 0x8065 && addr <= 0x80A4 )
		{
			preg = ( addr - 0x8065 ) & 0x03;
			set_prg_bank( 0x8000, bank_32k, preg );
		}

		if ( addr >= 0x80A5 && addr <= 0x80E4 )
		{
			creg = (addr - 0x80A5 ) & 0x07;
			set_chr_bank( 0x0000, bank_8k, creg );
		}
	}
};
