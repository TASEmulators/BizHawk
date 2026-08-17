
// Optional less-common simple mappers

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
	
	virtual void write( nes_time_t, nes_addr_t, int data )
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
	
	virtual void write( nes_time_t, nes_addr_t, int data )
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
	
	virtual void write( nes_time_t, nes_addr_t, int data )
	{
		int changed = bank ^ data;
		bank = data;
		
		if ( changed & 0x0f )
			set_prg_bank( 0x8000, bank_32k, bank & 0x0f );
		
		if ( changed & 0xf0 )
			set_chr_bank( 0, bank_8k, bank >> 4 );
	}
};

// Jaleco/Konami

class Mapper_87 : public Nes_Mapper {
	byte bank;
public:
	Mapper_87()
	{
		register_state( &bank, 1 );
	}
	
	void apply_mapping()
	{
		intercept_writes( 0x6000, 1 );
		write( 0, 0x6000, bank );
	}
	
	bool write_intercepted( nes_time_t, nes_addr_t addr, int data )
	{
		if ( addr != 0x6000 )
			return false;
		
		bank = data;
		set_chr_bank( 0, bank_8k, data >> 1 );
		return true;
	}
	
	void write( nes_time_t, nes_addr_t, int ) { }
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

class Mapper_78 : public Nes_Mapper {
	// lower 8 bits are the reg at 8000:ffff
	// next two bits are autodetecting type
	// 0 = unknown 1 = cosmo carrier 2 = holy diver
	int reg;
	void writeinternal(int data, int changed)
	{
		reg &= 0x300;
		reg |= data;

		if (changed & 0x07)
			set_prg_bank(0x8000, bank_16k, reg & 0x07);
		if (changed & 0xf0)
			set_chr_bank(0x0000, bank_8k, (reg >> 4) & 0x0f);
		if (changed & 0x08)
		{
			// set mirroring based on memorized board type
			if (reg & 0x100)
			{
				mirror_single((reg >> 3) & 1);
			}
			else if (reg & 0x200)
			{
				if (reg & 0x08)
					mirror_vert();
				else
					mirror_horiz();
			}
			else
			{
				// if you don't set something here, holy diver dumps with 4sc set will
				// savestate as 4k NTRAM.  then when you later set H\V mapping, state size mismatch.
				mirror_single(1);
			}
		}
	}

public:
	Mapper_78()
	{
		register_state(&reg, 4);
	}

	virtual void reset_state()
	{
		reg = 0;
	}

	virtual void apply_mapping()
	{
		writeinternal(reg, 0xff);
	}

	virtual void write( nes_time_t, nes_addr_t addr, int data)
	{
		// heuristic: if the first write ever to the register is 0,
		// we're on holy diver, otherwise, carrier.  it works for these two games...
		if (!(reg & 0x300))
		{
			reg |= data ? 0x100 : 0x200;
			writeinternal(data, 0xff);
		}
		else
		{
			writeinternal(data, reg ^ data);
		}
	}
};

class Mapper_212 : public Nes_Mapper {
	// something about this doesn't work right?
	// TODO: fix me
	int reg;
	void writeinternal(int data, int changed)
	{
		reg = data;

		if (changed & 0x0007)
		{
			set_chr_bank(0x0000, bank_8k, reg & 0x0007);
		}
		if (changed & 0x0008)
		{
			if (reg & 0x0008)
			{
				mirror_horiz();
			}
			else
			{
				mirror_vert();
			}
		}
		if (changed & 0x4007)
		{
			if (reg & 0x4000)
			{
				set_prg_bank(0x8000, bank_32k, reg >> 1 & 0x0003);
			}
			else
			{
				set_prg_bank(0x8000, bank_16k, reg & 0x0007);
				set_prg_bank(0xc000, bank_16k, reg & 0x0007);
			}
		}
	}

public:
	Mapper_212()
	{
		register_state(&reg, 4);
	}

	virtual void reset_state()
	{
		reg = 0xffff;
	}

	virtual void apply_mapping()
	{
		//intercept_reads(0xe000, 0x2000); // some sort of bus conflict or anti-piracy bs
		writeinternal(reg, 0xffff);
	}
	/*
	// as written.  this will never be hit.  what's going on?
	virtual int read( nes_time_t time, nes_addr_t addr )
	{
		int ret = Nes_Mapper::read(time, addr);
		if ((addr & 0xe010) == 0x6000)
			ret |= 0x80;
		return ret;
	}*/

	virtual void write( nes_time_t, nes_addr_t addr, int data)
	{
		writeinternal(addr, addr ^ reg);
	}
};

void register_misc_mappers();
void register_misc_mappers()
{
	register_mapper<Mapper_Color_Dreams>( 11 );
	register_mapper<Mapper_Nina1>( 34 );
	register_mapper<Mapper_Gnrom>( 66 );
	register_mapper<Mapper_Camerica>( 71 );
	register_mapper<Mapper_87>( 87 );
	register_mapper<Mapper_Quattro>( 232 );

	register_mapper<Mapper_78>( 78 );
	// register_mapper<Mapper_212>( 212 );
}

