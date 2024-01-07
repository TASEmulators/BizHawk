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

// AOROM

class Mapper007 : public Nes_Mapper {
	uint8_t bank;
public:
	Mapper007()
	{
		register_state( &bank, 1 );
	}
	
	virtual void apply_mapping()
	{
		int b = bank;
		bank = ~b; // force update
		write( 0, 0, b );
	}
	
	virtual void write( nes_time_t, nes_addr_t, int data )
	{
		int changed = bank ^ data;
		bank = data;
		
		if ( changed & 0x10 )
			mirror_single( bank >> 4 & 1 );
		
		if ( changed & 0x0f )
			set_prg_bank( 0x8000, bank_32k, bank & 7 );
	}
};
	

