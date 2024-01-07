#pragma once

// Common simple mappers

// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

/* Copyright (C) 2004-2006 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include "blargg_source.h"

// UNROM

class Mapper002 : public Nes_Mapper {
	uint8_t bank;
public:
	Mapper002()
	{
		register_state( &bank, 1 );
	}
	
	virtual void apply_mapping()
	{
		enable_sram(); // at least one UNROM game needs sram (Bomberman 2)
		set_prg_bank( 0x8000, bank_16k, bank );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		bank = handle_bus_conflict( addr, data );
		set_prg_bank( 0x8000, bank_16k, bank );
	}
};

