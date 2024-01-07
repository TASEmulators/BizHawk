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
 * Mapper 97 - Kaiketsu Yanchamaru
 *
 */

#pragma once

#include "Nes_Mapper.h"
 
// Irem_Tam_S1 

class Mapper097 : public Nes_Mapper {
public:
	Mapper097()
	{
		register_state( &bank, 1 );
	}

	virtual void reset_state()
	{
		bank = ~0;
	}

	virtual void apply_mapping()
	{
		write( 0, 0, bank );
	}

	virtual void write( nes_time_t, nes_addr_t, int data )
	{
		bank = data;
		set_prg_bank( 0x8000, bank_16k, ~0 );
		set_prg_bank( 0xC000, bank_16k, bank & 0x0F );

		switch ( ( bank >> 6 ) & 0x03 )
		{
		case 1: mirror_horiz(); break;
		case 2: mirror_vert(); break;
		case 0:
		case 3: mirror_single( bank & 0x01 ); break;
		}
	}

	uint8_t bank;
};
