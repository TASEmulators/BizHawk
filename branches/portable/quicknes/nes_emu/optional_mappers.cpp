
// Optional less-common simple mappers

// Nes_Emu 0.5.6. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

/* Copyright (C) 2004-2005 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include BLARGG_SOURCE_BEGIN

// Nina-1 (Deadly Towers only)

class Mapper_Nina1 : public Nes_Mapper {
	byte bank;
public:
	Mapper_Nina1()
	{
		register_state( &bank, 1 );
	}
	
	virtual void apply_mapping()
	{
		write( 0, 0, bank );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		bank = data;
		set_prg_bank( 0x8000, bank_32k, bank );
	}
};

// GNROM

class Mapper_Gnrom : public Nes_Mapper {
	byte bank;
public:
	Mapper_Gnrom()
	{
		register_state( &bank, 1 );
	}
	
	virtual void apply_mapping()
	{
		int b = bank;
		bank = ~b;
		write( 0, 0, b );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		int changed = bank ^ data;
		bank = data;
		
		if ( changed & 0x30 )
			set_prg_bank( 0x8000, bank_32k, bank >> 4 & 3 );
		
		if ( changed & 0x03 )
			set_chr_bank( 0, bank_8k, bank & 3 );
	}
};

// Color Dreams

class Mapper_Color_Dreams : public Nes_Mapper {
	byte bank;
public:
	Mapper_Color_Dreams()
	{
		register_state( &bank, 1 );
	}
	
	virtual void apply_mapping()
	{
		int b = bank;
		bank = ~b;
		write( 0, 0, b );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		int changed = bank ^ data;
		bank = data;
		
		if ( changed & 0x0f )
			set_prg_bank( 0x8000, bank_32k, bank & 0x0f );
		
		if ( changed & 0xf0 )
			set_chr_bank( 0, bank_8k, bank >> 4 );
	}
};

// Camerica

class Mapper_Camerica : public Nes_Mapper {
	byte regs [3];
public:
	Mapper_Camerica()
	{
		register_state( regs, sizeof regs );
	}
	
	virtual void apply_mapping()
	{
		write( 0, 0xc000, regs [0] );
		if ( regs [1] & 0x80 )
			write( 0, 0x9000, regs [1] );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr >= 0xc000 )
		{
			regs [0] = data;
			set_prg_bank( 0x8000, bank_16k, data );
		}
		else if ( (addr & 0xf000) == 0x9000 )
		{
			regs [1] = 0x80 | data;
			mirror_single( (data >> 4) & 1 );
		}
	}
};

// Quattro

class Mapper_Quattro : public Nes_Mapper {
	byte regs [2];
public:
	Mapper_Quattro()
	{
		register_state( regs, sizeof regs );
	}
	
	virtual void reset_state()
	{
		regs [0] = 0;
		regs [1] = 3;
	}
	
	virtual void apply_mapping()
	{
		int bank = regs [0] >> 1 & 0x0c;
		set_prg_bank( 0x8000, bank_16k, bank + (regs [1] & 3) );
		set_prg_bank( 0xC000, bank_16k, bank + 3 );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr < 0xc000 )
			regs [0] = data;
		else
			regs [1] = data;
		Mapper_Quattro::apply_mapping();
	}
};

void register_misc_mappers();
void register_misc_mappers()
{
	register_mapper<Mapper_Color_Dreams>( 11 );
	register_mapper<Mapper_Nina1>( 34 );
	register_mapper<Mapper_Gnrom>( 66 );
	register_mapper<Mapper_Camerica>( 71 );
	register_mapper<Mapper_Quattro>( 232 );
}

void Nes_Mapper::register_optional_mappers()
{
	register_misc_mappers();
	
	extern void register_vrc6_mapper();
	register_vrc6_mapper();
	
	extern void register_mmc5_mapper();
	register_mmc5_mapper();
	
	extern void register_fme07_mapper();
	register_fme07_mapper();
	
	extern void register_namco106_mapper();
	register_namco106_mapper();
}

