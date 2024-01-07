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
 */

#pragma once

#include "Nes_Mapper.h"

// https://www.nesdev.org/wiki/INES_Mapper240

class Mapper240 : public Nes_Mapper {
public:
	Mapper240()
	{
		register_state( &regs, 1 );
	}

	virtual void reset_state()
	{
	}

	virtual void apply_mapping()
	{
		enable_sram();
		intercept_writes( 0x4020, 1 );
		write_intercepted( 0, 0x4120, regs );
	}

	virtual void write( nes_time_t, nes_addr_t, int data )
	{ }

	virtual bool write_intercepted( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr < 0x4020 || addr > 0x5FFF )
			return false;

		regs = data;
		set_chr_bank( 0x0000, bank_8k, data & 0x0F );
		set_prg_bank( 0x8000, bank_32k, data >> 4 );

		return true;
	}

	uint8_t regs;
};
