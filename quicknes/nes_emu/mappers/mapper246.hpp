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
 * Mapper 246 - Feng Shen Bang (Asia) (Ja) (Unl)
 *
 */

#pragma once

#include "Nes_Mapper.h"

// https://www.nesdev.org/wiki/INES_Mapper246

class Mapper246 : public Nes_Mapper {
public:
	Mapper246()
	{
		register_state( regs, sizeof regs );
	}

	virtual void reset_state()
	{
		regs [ 3 ] = ~0;
	}

	virtual void apply_mapping()
	{
		enable_sram();
		intercept_writes( 0x6000, 0x07 );
		for ( size_t i = 0; i < sizeof regs; i++ )
			write_intercepted( 0, 0x6000 + i, regs [ i ] );
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{ }

	virtual bool write_intercepted( nes_time_t, nes_addr_t addr, int data )
	{
		int bank = addr & 0x07;

		if ( addr < 0x6000 || addr > 0x67FF )
			return false;

		regs [ bank ] = data;
		if ( bank < 4 )
		{
			set_prg_bank( 0x8000 + ( bank << 13 ), bank_8k, data );
			return true;
		}

		set_chr_bank( 0x0000 + ( ( bank & 0x03 ) << 11 ) , bank_2k, data );

		return true;
	}

	uint8_t regs [ 8 ];
};

