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
 * Mapper 93 - Sunsoft-2
 */

#pragma once

#include "Nes_Mapper.h"

// Sunsoft2a 

class Mapper093 : public Nes_Mapper {
public:
	Mapper093()
	{
		register_state( &regs, 1 );
	}

	virtual void reset_state()
	{}

	virtual void apply_mapping()
	{
		set_prg_bank( 0xC000, bank_16k, last_bank );
		write( 0, 0x8000, regs );
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		regs = handle_bus_conflict( addr, data );

		set_chr_bank( 0x0000, bank_8k, data & 0x0F );
		set_prg_bank( 0x8000, bank_16k, ( data >> 4 ) & 0x07 );
	}

	uint8_t regs;
};
