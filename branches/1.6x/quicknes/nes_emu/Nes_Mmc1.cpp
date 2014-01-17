
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

#include <string.h>

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

class Mapper_Mmc1 : public Nes_Mapper, mmc1_state_t {
public:
	Mapper_Mmc1()
	{
		mmc1_state_t* state = this;
		register_state( state, sizeof *state );
	}
	
	virtual void reset_state()
	{
		regs [0] = 0x0f;
		regs [1] = 0x00;
		regs [2] = 0x01;
		regs [3] = 0x00;
	}
	
	void register_changed( int );
	
	virtual void apply_mapping()
	{
		enable_sram(); // early MMC1 always had SRAM enabled
		register_changed( 0 );
	}
	
	virtual void write( nes_time_t, nes_addr_t addr, int data )
	{
		if ( !(data & 0x80) )
		{
			buf |= (data & 1) << bit; 
			bit++;
			
			if ( bit >= 5 )
			{
				int reg = addr >> 13 & 3;
				regs [reg] = buf & 0x1f;
				
				bit = 0;
				buf = 0;
				
				register_changed( reg );
			}
		}
		else
		{
			bit = 0;
			buf = 0;
			regs [0] |= 0x0c;
			register_changed( 0 );
		}
	}
};

void Mapper_Mmc1::register_changed( int reg )
{
	// Mirroring
	if ( reg == 0 )
	{
		int mode = regs [0] & 3;
		if ( mode < 2 )
			mirror_single( mode & 1 );
		else if ( mode == 2 )
			mirror_vert();
		else
			mirror_horiz();
	}
	
	// CHR
	if ( reg < 3 && cart().chr_size() > 0 )
	{
		if ( regs [0] & 0x10 )
		{
			set_chr_bank( 0x0000, bank_4k, regs [1] );
			set_chr_bank( 0x1000, bank_4k, regs [2] );
		}
		else
		{
			set_chr_bank( 0, bank_8k, regs [1] >> 1 );
		}
	}
	
	// PRG
	int bank = (regs [1] & 0x10) | (regs [3] & 0x0f);
	if ( !(regs [0] & 0x08) )
	{
		set_prg_bank( 0x8000, bank_32k, bank >> 1 );
	}
	else if ( regs [0] & 0x04 )
	{
		set_prg_bank( 0x8000, bank_16k, bank );
		set_prg_bank( 0xC000, bank_16k, bank | 0x0f );
	}
	else
	{
		set_prg_bank( 0x8000, bank_16k, bank & ~0x0f );
		set_prg_bank( 0xC000, bank_16k, bank );
	}
}

Nes_Mapper* Nes_Mapper::make_mmc1()
{
	return BLARGG_NEW Mapper_Mmc1;
}

