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
 * 3/24/18
 *
 * Unrom-512
 * 
 * NOTE: 
 * No flash and one screen mirroring support.
 * Tested only on Troll Burner and Mystic Origins demo.
 */

#pragma once

#include "Nes_Mapper.h"

// Unrom512 

class Mapper030 : public Nes_Mapper {
public:
	Mapper030() { }

	void reset_state() { }

	void apply_mapping() { }

	void write( nes_time_t, nes_addr_t addr, int data )
	{
		if ( ( addr & 0xF000 ) >= 0x8000 )
		{
			set_prg_bank(0x8000, bank_16k, data & 0x1F);
			set_chr_bank(0x0000, bank_8k, (data >> 5) & 0x3);
		}
	}
};

