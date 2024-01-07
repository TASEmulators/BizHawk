/* Copyright notice for this file:
 *  Copyright (C) 2004-2006 Shay Green
 *  Copyright (C) 2007 CaH4e3
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
 * General code is from FCEUX https://sourceforge.net/p/fceultra/code/HEAD/tree/fceu/trunk/src/boards/vrc2and4.cpp
 * IRQ portion is from existing VRC6/VRC7 by Shay Green
 * This mapper was ported by retrowertz for Libretro port of QuickNES.
 * 3-19-2018
 *
 * VRC-2/VRC-4 Konami
 */

#pragma once
#include "Nes_Mapper.h"

struct vrc2_state_t
{
	uint8_t prg_banks [ 2 ];
	uint8_t chr_banks [ 8 ];
	uint8_t mirroring;
	uint8_t prg_swap;
	uint8_t irq_latch;
	uint8_t irq_control;

	// internal state
	uint16_t next_time;
	uint8_t irq_pending;
};

BOOST_STATIC_ASSERT( sizeof ( vrc2_state_t ) == 18 );

template <bool type_a, bool type_b>
class Mapper_VRC2_4 : public Nes_Mapper, vrc2_state_t {
public:
	Mapper_VRC2_4()
	{
		if (type_a && type_b)			// mapper 21
		{
			is22 = 0;
			reg1mask = 0x42;
			reg2mask = 0x84;
		}
		else if (!type_a && type_b)		// mapper 22
		{
			is22 = 1;
			reg1mask = 2;
			reg2mask = 1;
		}
		else if (!type_a && !type_b)	// mapper 23
		{
			is22 = 0;
			reg1mask = 0x15;
			reg2mask = 0x2a;
		}
		else if (type_a && !type_b)		// mapper 25
		{
			is22 = 0;
			reg1mask = 0xa;
			reg2mask = 0x5;
		}
		vrc2_state_t * state = this;
		register_state( state, sizeof * state );
	}

	void reset_state()
	{
	}

	void apply_mapping()
	{
		if ( !is22 ) enable_sram();
		update_prg();
		update_chr();
		set_mirroring();
	}

	void reset_timer( nes_time_t present )
	{
		next_time = present + unsigned ( ( 0x100 - irq_latch ) * timer_period ) / 4;
	}

	virtual void run_until( nes_time_t end_time )
	{
		if ( irq_control & 2 )
		{
			while ( next_time < end_time )
			{
				// printf( "%d timer expired\n", next_time );
				irq_pending = true;
				reset_timer( next_time );
			}
		}
	}

	virtual void end_frame( nes_time_t end_time )
	{
		run_until( end_time );

		// to do: next_time might go negative if IRQ is disabled
		next_time -= end_time;
	}

	virtual nes_time_t next_irq( nes_time_t present )
	{
		if ( irq_pending )
			return present;

		if ( irq_control & 2 )
			return next_time + 1;

		return no_irq;
	}

	void write_irq( nes_time_t time, nes_addr_t addr, int data );

	void write( nes_time_t time, nes_addr_t addr, int data )
	{
		addr = ( addr & 0xF000 ) | !!( addr & reg2mask ) << 1 | !!( addr & reg1mask );

		if( addr >= 0xB000 && addr <= 0xE003)
		{
			unsigned banknumber = ( ( addr >> 1 ) & 1 ) | ( ( addr - 0xB000 ) >> 11 );
			unsigned offset = ( addr & 1 ) << 2;

			chr_banks [ banknumber ] &= ( 0xF0 ) >> offset;
			chr_banks [ banknumber ] |= ( data & 0xF ) << offset;
			chr_banks [ banknumber ] |= ( offset ? ( ( data & 0x10 ) << 4 ) : 0 );
			update_chr();
		}
		else
		{
			switch ( addr & 0xF003 )
			{
				case 0x8000:
				case 0x8001:
				case 0x8002:
				case 0x8003:
					prg_banks [ 0 ] = data & 0x1F;
					update_prg();
					break;
				case 0xA000:
				case 0xA001:
				case 0xA002:
				case 0xA003:
					prg_banks [ 1 ] = data & 0x1F;
					update_prg();
					break;
				case 0x9000:
				case 0x9001:
					mirroring = data;
					set_mirroring();
					break;
				case 0x9002:
				case 0x9003:
					prg_swap = data;
					update_prg();
					break;
				case 0xF000:
				case 0xF001:
				case 0xF002:
				case 0xF003:
					write_irq( time, addr, data );
					break;
			}
		}
	}

	unsigned is22, reg1mask, reg2mask;
	enum { timer_period = 113 * 4 + 3 };

private:
	void set_mirroring()
	{
		switch ( mirroring & 3 )
		{
		case 0: mirror_vert(); break;
		case 1: mirror_horiz(); break;
		case 2:
		case 3: mirror_single( mirroring & 1 ); break;
		}
	}

	void update_prg()
	{
		if ( prg_swap & 2 )
		{
			set_prg_bank( 0x8000, bank_8k, ( 0xFE ) );
			set_prg_bank( 0xC000, bank_8k, prg_banks [ 0 ] );
		}
		else
		{
			set_prg_bank( 0x8000, bank_8k, prg_banks [ 0 ] );
			set_prg_bank( 0xC000, bank_8k, ( 0xFE ) );
		}

		set_prg_bank( 0xA000, bank_8k, prg_banks [ 1 ] );
		set_prg_bank( 0xE000, bank_8k, ( 0xFF ) );
	}

	void update_chr()
	{
		for ( int i = 0; i < (int) sizeof chr_banks; i++ )
			set_chr_bank( i * 0x400, bank_1k, chr_banks [ i ] >> is22 );
	}
};

template <bool type_a, bool type_b>
void Mapper_VRC2_4<type_a, type_b>::write_irq( nes_time_t time,
	nes_addr_t addr, int data )
{
	// IRQ
	run_until( time );
	//printf("%6d VRC2_4 [%d] A:%04x V:%02x\n", time, addr & 3, addr, data);
	switch ( addr & 3 )
	{
		case 0:
			irq_latch = ( irq_latch & 0xF0 ) | ( data & 0xF );
			break;
		case 1:
			irq_latch = ( irq_latch & 0x0F ) | ( ( data & 0xF ) << 4 );
			break;
		case 2:
			irq_pending = false;
			irq_control = data & 3;
			if ( data & 2 ) reset_timer( time );
			break;
		case 3:
			irq_pending = false;
			irq_control = ( irq_control & ~2 ) | ( ( irq_control << 1 ) & 2 );
			break;
	}
	irq_changed();
}


typedef Mapper_VRC2_4<true,true> Mapper021;
