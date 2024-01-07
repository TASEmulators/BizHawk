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
 * VRC-1 Konami
 */

#pragma once

#include "Nes_Mapper.h"

struct vrc1_state_t
{
	uint8_t prg_banks [ 3 ];
	uint8_t chr_banks [ 2 ];
	uint8_t chr_banks_hi [ 2 ];
	uint8_t mirroring;
};

BOOST_STATIC_ASSERT( sizeof ( vrc1_state_t ) == 8 );

// VRC1 

class Mapper075 : public Nes_Mapper, vrc1_state_t {
public:
	Mapper075()
	{
		vrc1_state_t * state = this;
		register_state( state, sizeof * state );
	}

	void reset_state()
	{
	}

	void apply_mapping()
	{
		update_prg_banks();
		update_chr_banks();
		update_mirroring();
	}

	void write( nes_time_t, nes_addr_t addr, int data )
	{
		switch ( addr & 0xF000 )
		{
			case 0x8000:
				prg_banks [ 0 ] = data & 0xF;
				update_prg_banks();
				break;
			case 0x9000:
				mirroring = data & 1;
				chr_banks_hi [ 0 ] = ( data & 2 ) << 3;
				chr_banks_hi [ 1 ] = ( data & 4 ) << 2;
				update_chr_banks();
				update_mirroring();
				break;
			case 0xa000:
				prg_banks [ 1 ] = data & 0xF;
				update_prg_banks();
				break;
			case 0xc000:
				prg_banks [ 2 ] = data & 0xF;
				update_prg_banks();
				break;
			case 0xe000:
				chr_banks [ 0 ] = data & 0xF;
				update_chr_banks();
				break;
			case 0xf000:
				chr_banks [ 1 ] = data & 0xF;
				update_chr_banks();
				break;
		}
	}

	void update_prg_banks()
	{
		set_prg_bank( 0x8000, bank_8k, prg_banks [ 0 ] );
		set_prg_bank( 0xa000, bank_8k, prg_banks [ 1 ] );
		set_prg_bank( 0xc000, bank_8k, prg_banks [ 2 ] );
	}

	void update_chr_banks()
	{
		set_chr_bank( 0x0000, bank_4k, chr_banks [ 0 ] | chr_banks_hi [ 0 ] );
		set_chr_bank( 0x1000, bank_4k, chr_banks [ 1 ] | chr_banks_hi [ 1 ] );
	}

	void update_mirroring()
	{
		switch ( mirroring & 1 )
		{
			case 1: mirror_horiz(); break;
			case 0: mirror_vert(); break;
		}
	}
};


