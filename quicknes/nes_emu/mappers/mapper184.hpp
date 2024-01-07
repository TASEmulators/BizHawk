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
 * Mapper 184 - Sunsoft-1
 */

#pragma once

#include "Nes_Mapper.h"

// Sunsoft1

class Mapper184 : public Nes_Mapper {
public:
	Mapper184()
	{
		register_state( &regs, 1 );
	}

	virtual void reset_state()
	{}

	virtual void apply_mapping()
	{
		set_prg_bank( 0x8000, bank_32k, 0 );
		intercept_writes( 0x6000, 1 );
		write_intercepted( 0, 0x6000, regs );
	}

	virtual bool write_intercepted( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr != 0x6000 )
			return false;

		regs = data;
		set_chr_bank( 0x0000, bank_4k, data & 0x07 );
		set_chr_bank( 0x1000, bank_4k, ( data >> 4 ) & 0x07 );

		return true;
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{}

	uint8_t regs;
};

