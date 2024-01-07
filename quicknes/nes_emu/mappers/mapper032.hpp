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
 * Mapper 32 - Irem's G-101
 *
 */

#pragma once

#include "Nes_Mapper.h"

struct mapper32_state_t
{
	uint8_t chr_bank [ 8 ];
	uint8_t prg_bank [ 2 ];
	uint8_t prg_mode;
	uint8_t mirr;
};

BOOST_STATIC_ASSERT( sizeof ( mapper32_state_t ) == 12 );

// Irem_G101

class Mapper032 : public Nes_Mapper, mapper32_state_t {
public:
	Mapper032()
	{
		mapper32_state_t * state = this;
		register_state( state, sizeof * state );
	}

	virtual void reset_state()
	{
		prg_bank [ 0 ] = ~1;
		prg_bank [ 1 ] = ~0;
		enable_sram();
	}

	virtual void apply_mapping()
	{
		if ( prg_mode == 0 )
		{
			set_prg_bank ( 0x8000, bank_8k, prg_bank [ 0 ] );
			set_prg_bank ( 0xA000, bank_8k, prg_bank [ 1 ] );
			set_prg_bank ( 0xC000, bank_8k, ~1 );
			set_prg_bank ( 0xE000, bank_8k, ~0 );
		}
		else
		{
			set_prg_bank ( 0xC000, bank_8k, prg_bank [ 0 ] );
			set_prg_bank ( 0xA000, bank_8k, prg_bank [ 1 ] );
			set_prg_bank ( 0x8000, bank_8k, ~1 );
			set_prg_bank ( 0xE000, bank_8k, ~0 );
		}

		for ( unsigned long int i = 0; i < sizeof chr_bank; i++)
			set_chr_bank( ( i << 10 ), bank_1k, chr_bank [ i ] );

		switch ( mirr )
		{
		case 0: mirror_vert(); break;
		case 1: mirror_horiz(); break;
		}
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		switch ( addr & 0xF000 )
		{
		case 0x8000:
			prg_bank [ 0 ] = data;
			switch ( prg_mode )
			{
			case 0: set_prg_bank ( 0x8000, bank_8k, data ); break;
			case 1: set_prg_bank ( 0xC000, bank_8k, data ); break;
			}
			break;
		case 0x9000:
			mirr = data & 1;
			prg_mode = ( data >> 1 ) & 1;
			switch ( data & 1 )
			{
			case 0: mirror_vert(); break;
			case 1: mirror_horiz(); break;
			}
			break;
		case 0xA000:
			prg_bank [ 1 ] = data;
			set_prg_bank ( 0xA000, bank_8k, data );
			break;
		}

		switch ( addr & 0xF007 )
		{
		case 0xB000: case 0xB001: case 0xB002: case 0xB003:
		case 0xB004: case 0xB005: case 0xB006: case 0xB007:
			chr_bank [ addr & 0x07 ] = data;
			set_chr_bank( ( addr & 0x07 ) << 10, bank_1k, data );
			break;
		}
	}
};

