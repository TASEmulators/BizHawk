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
 * Mapper 193 - NTDEC TC-112
 * Fighting Hero (Unl)
 * War in the Gulf & (its Brazilian localization)
 *
 */

#pragma once

#include "Nes_Mapper.h"
 
// NTDEC's TC-112 mapper IC.

class Mapper193 : public Nes_Mapper {
public:
	Mapper193()
	{
		register_state( regs, sizeof regs );
	}

	virtual void reset_state()
	{ }

	virtual void apply_mapping()
	{
		for ( size_t i = 0; i < sizeof regs; i++ )
			write_intercepted( 0, 0x6000 + i, regs [ i ] );
		set_prg_bank( 0xA000, bank_8k, ~2 );
		set_prg_bank( 0xC000, bank_8k, ~1 );
		set_prg_bank( 0xE000, bank_8k, ~0 );
		intercept_writes( 0x6000, 0x03 );
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{ }

	virtual bool write_intercepted( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr < 0x6000 || addr > 0x6003 )
			return false;

		regs [ addr & 0x03 ] = data;
		switch ( addr & 0x03 )
		{
		case 0: set_chr_bank( 0x0000, bank_4k, regs [ 0 ] >> 2 ); break;
		case 1: set_chr_bank( 0x1000, bank_2k, regs [ 1 ] >> 1 ); break;
		case 2: set_chr_bank( 0x1800, bank_2k, regs [ 2 ] >> 1 ); break;
		case 3: set_prg_bank( 0x8000, bank_8k, regs [ 3 ] ); break;
		}

		return true;
	}

	uint8_t regs [ 4 ];
};

