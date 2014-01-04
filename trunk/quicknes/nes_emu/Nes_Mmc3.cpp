
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Mapper.h"

#include <string.h>
#include "Nes_Core.h"

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

// 264 or less breaks Gargoyle's Quest II
// 267 or less breaks Magician
int const irq_fine_tune = 268;
nes_time_t const first_scanline = 20 * Nes_Ppu::scanline_len + irq_fine_tune;
nes_time_t const last_scanline = first_scanline + 240 * Nes_Ppu::scanline_len;

class Mapper_Mmc3 : public Nes_Mapper, mmc3_state_t {
	nes_time_t next_time;
	int counter_just_clocked; // used only for debugging
public:
	Mapper_Mmc3()
	{
		mmc3_state_t* state = this;
		register_state( state, sizeof *state );
	}
	
	virtual void reset_state()
	{
		memcpy( banks, "\0\2\4\5\6\7\0\1", sizeof banks );
		
		counter_just_clocked = 0;
		next_time = 0;
		mirror = 1;
		if ( cart().mirroring() & 1 )
		{
			mirror = 0;
			//dprintf( "cart specified vertical mirroring\n" );
		}
	}
	
	void update_chr_banks();
	void update_prg_banks();
	void write_irq( nes_addr_t addr, int data );
	void write( nes_time_t, nes_addr_t, int );
	
	void start_frame() { next_time = first_scanline; }
	
	virtual void apply_mapping()
	{
		write( 0, 0xA000, mirror );
		write( 0, 0xA001, sram_mode );
		update_chr_banks();
		update_prg_banks();
		start_frame();
	}
	
	void clock_counter()
	{
		if ( counter_just_clocked )
			counter_just_clocked--;
		
		if ( !irq_ctr-- )
		{
			irq_ctr = irq_latch;
			//if ( !irq_latch )
				//dprintf( "MMC3 IRQ counter reloaded with 0\n" );
		}
		
		//dprintf( "%6d MMC3 IRQ clocked\n", time / ppu_overclock );
		if ( irq_ctr == 0 )
		{
			//if ( irq_enabled && !irq_flag )
				//dprintf( "%6d MMC3 IRQ triggered: %f\n", time / ppu_overclock, time / scanline_len.0 - 20 );
			irq_flag = irq_enabled;
		}
	}
	
	virtual void run_until( nes_time_t );
	
	virtual void a12_clocked()
	{
		clock_counter();
		if ( irq_enabled )
			irq_changed();
	}
	
	virtual void end_frame( nes_time_t end_time )
	{
		run_until( end_time );
		start_frame();
	}
	
	virtual nes_time_t next_irq( nes_time_t present )
	{
		run_until( present );
		
		if ( !irq_enabled )
			return no_irq;
		
		if ( irq_flag )
			return 0;
		
		if ( !ppu_enabled() )
			return no_irq;
		
		int remain = irq_ctr - 1;
		if ( remain < 0 )
			remain = irq_latch;
		
		assert( remain >= 0 );
		
		long time = remain * 341L + next_time;
		if ( time > last_scanline )
			return no_irq;
		
		return time / ppu_overclock + 1;
	}
};

void Mapper_Mmc3::run_until( nes_time_t end_time )
{
	bool bg_enabled = ppu_enabled();
	
	end_time *= ppu_overclock;
	while ( next_time < end_time && next_time <= last_scanline )
	{
		if ( bg_enabled )
			clock_counter();
		next_time += Nes_Ppu::scanline_len;
	}
}

void Mapper_Mmc3::update_chr_banks()
{
	int chr_xor = (mode >> 7 & 1) * 0x1000;
	set_chr_bank( 0x0000 ^ chr_xor, bank_2k, banks [0] >> 1 );
	set_chr_bank( 0x0800 ^ chr_xor, bank_2k, banks [1] >> 1 );
	set_chr_bank( 0x1000 ^ chr_xor, bank_1k, banks [2] );
	set_chr_bank( 0x1400 ^ chr_xor, bank_1k, banks [3] );
	set_chr_bank( 0x1800 ^ chr_xor, bank_1k, banks [4] );
	set_chr_bank( 0x1c00 ^ chr_xor, bank_1k, banks [5] );
}

void Mapper_Mmc3::update_prg_banks()
{
	set_prg_bank( 0xA000, bank_8k, banks [7] );
	nes_addr_t addr = 0x8000 + 0x4000 * (mode >> 6 & 1);
	set_prg_bank( addr, bank_8k, banks [6] );
	set_prg_bank( addr ^ 0x4000, bank_8k, last_bank - 1 );
}

void Mapper_Mmc3::write_irq( nes_addr_t addr, int data )
{
	switch ( addr & 0xE001 )
	{
	case 0xC000:
		irq_latch = data;
		break;
	
	case 0xC001:
		if ( counter_just_clocked == 1 )
			dprintf( "MMC3 IRQ counter pathological behavior triggered\n" );
		counter_just_clocked = 2;
		irq_ctr = 0;
		break;
	
	case 0xE000:
		irq_flag = false;
		irq_enabled = false;
		break;
	
	case 0xE001:
		irq_enabled = true;
		break;
	}
	if ( irq_enabled )
		irq_changed();
}

void Mapper_Mmc3::write( nes_time_t time, nes_addr_t addr, int data )
{
	check( !(addr & ~0xe001) ); // writes to mirrored registers are rare
	//dprintf( "%6d %02X->%04X\n", time, data, addr );
	
	switch ( addr & 0xE001 )
	{
	case 0x8000: {
		int changed = mode ^ data;
		mode = data;
		// avoid unnecessary bank updates
		if ( changed & 0x80 )
			update_chr_banks();
		if ( changed & 0x40 )
			update_prg_banks();
		break;
	}
	
	case 0x8001: {
		int bank = mode & 7;
		banks [bank] = data;
		if ( bank < 6 )
			update_chr_banks();
		else
			update_prg_banks();
		break;
	}
	
	case 0xA000:
		mirror = data;
		if ( !(cart().mirroring() & 0x08) )
		{
			if ( mirror & 1 )
				mirror_horiz();
			else
				mirror_vert();
		}
		break;
	
	case 0xA001:
		sram_mode = data;
		//dprintf( "%02X->%04X\n", data, addr );
		
		// Startropics 1 & 2 use MMC6 and always enable low 512 bytes of SRAM
		if ( (data & 0x3F) == 0x30 )
			enable_sram( true );
		else
			enable_sram( data & 0x80, data & 0x40 );
		break;
	
	default:
		run_until( time );
		write_irq( addr, data );
		break;
	}
}

Nes_Mapper* Nes_Mapper::make_mmc3()
{
	return BLARGG_NEW Mapper_Mmc3;
}

