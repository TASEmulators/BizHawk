
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

// NROM

class Mapper_Nrom : public Nes_Mapper {
public:
	Mapper_Nrom() { }
	
	virtual void apply_mapping() { }
	
	virtual void write( nes_time_t, nes_addr_t, int )
	{
		// empty
	}
};

Nes_Mapper* Nes_Mapper::make_nrom() { return new Mapper_Nrom; }

// UNROM

class Mapper_Unrom : public Nes_Mapper {
	byte bank;
public:
	Mapper_Unrom()
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

Nes_Mapper* Nes_Mapper::make_unrom() { return new Mapper_Unrom; }

// AOROM

class Mapper_Aorom : public Nes_Mapper {
	byte bank;
public:
	Mapper_Aorom()
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
	
Nes_Mapper* Nes_Mapper::make_aorom() { return new Mapper_Aorom; }

// CNROM

class Mapper_Cnrom : public Nes_Mapper {
	byte bank;
public:
	Mapper_Cnrom()
	{
		register_state( &bank, 1 );
	}
	
	virtual void apply_mapping()
	{
		set_chr_bank( 0, bank_8k, bank & 7 );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		bank = handle_bus_conflict( addr, data );
		set_chr_bank( 0, bank_8k, bank & 7 );
	}
};

Nes_Mapper* Nes_Mapper::make_cnrom() { return new Mapper_Cnrom; }

