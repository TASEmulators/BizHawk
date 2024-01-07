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
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301, USA.
 *
 * This mapper was added by retrowertz for Libretro port of QuickNES.
 *
 * Mapper 140 - Jaleco's JF-11 and JF-14
 *
 */

#pragma once

#include "Nes_Mapper.h"

// Jaleco_JF11 

class Mapper140 : public Nes_Mapper {
public:
	Mapper140()
	{
		register_state( &regs, 1 );
	}

	virtual void reset_state()
	{
		intercept_writes( 0x6000, 1 );
	}

	virtual void apply_mapping()
	{
		write_intercepted(0, 0x6000, regs );
	}

	bool write_intercepted( nes_time_t time, nes_addr_t addr, int data )
	{
		if ( addr < 0x6000 || addr > 0x7FFF )
			return false;

		regs = data;
		set_prg_bank( 0x8000, bank_32k, data >> 4);
		set_chr_bank( 0, bank_8k, data );

		return true;
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data ) { }

	uint8_t regs;
};
