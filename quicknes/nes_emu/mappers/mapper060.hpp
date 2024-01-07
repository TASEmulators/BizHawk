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
 * 4-in-1 Multicart ( Reset-based )
 */

#pragma once

#include "Nes_Mapper.h"

// NROM-128 4-in-1 multicart

class Mapper060 : public Nes_Mapper {
public:
	Mapper060()
	{
		last_game = 2;
		register_state( &game_sel, 1 );
	}

	virtual void reset_state()
	{
		game_sel = last_game;
		game_sel++;
		game_sel &= 3;
	}

	virtual void apply_mapping()
	{
		set_prg_bank ( 0x8000, bank_16k, game_sel );
		set_prg_bank ( 0xC000, bank_16k, game_sel );
		set_chr_bank ( 0, bank_8k, game_sel );
		last_game = game_sel;
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data ) { }

	uint8_t game_sel, last_game;
};
