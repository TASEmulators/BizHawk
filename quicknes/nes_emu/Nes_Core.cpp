
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Core.h"

#include <string.h>
#include "Nes_Mapper.h"
#include "Nes_State.h"

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

extern const char unsupported_mapper [] = "Unsupported mapper";

bool const wait_states_enabled = true;
bool const single_instruction_mode = false; // for debugging irq/nmi timing issues

const int unmapped_fill = Nes_Cpu::page_wrap_opcode;

unsigned const low_ram_size = 0x800;
unsigned const low_ram_end  = 0x2000;
unsigned const sram_end     = 0x8000;

Nes_Core::Nes_Core() : ppu( this )
{
	cart = NULL;
	impl = NULL;
	mapper = NULL;
	memset( &nes, 0, sizeof nes );
	memset( &joypad, 0, sizeof joypad );
}

blargg_err_t Nes_Core::init()
{
	if ( !impl )
	{
		CHECK_ALLOC( impl = BLARGG_NEW impl_t );
		impl->apu.dmc_reader( read_dmc, this );
		impl->apu.irq_notifier( apu_irq_changed, this );
	}
	
	return 0;
}

void Nes_Core::close()
{
	// check that nothing modified unmapped page
	#ifndef NDEBUG
		//if ( cart && mem_differs( impl->unmapped_page, unmapped_fill, sizeof impl->unmapped_page ) )
		//  dprintf( "Unmapped code page was written to\n" );
	#endif
	
	cart = NULL;
	delete mapper;
	mapper = NULL;
	
	ppu.close_chr();
	
	disable_rendering();
}

blargg_err_t Nes_Core::open( Nes_Cart const* new_cart )
{
	close();
	
	RETURN_ERR( init() );
	
	mapper = Nes_Mapper::create( new_cart, this );
	if ( !mapper ) 
		return unsupported_mapper;
	
	RETURN_ERR( ppu.open_chr( new_cart->chr(), new_cart->chr_size() ) );
	
	cart = new_cart;
	memset( impl->unmapped_page, unmapped_fill, sizeof impl->unmapped_page );
	reset( true, true );
	return 0;
}

Nes_Core::~Nes_Core()
{
	close();
	delete impl;
}

void Nes_Core::save_state( Nes_State_* out ) const
{
	out->clear();
	
	out->nes = nes;
	out->nes_valid = true;
	
	*out->cpu = cpu::r;
	out->cpu_valid = true;
	
	*out->joypad = joypad;
	out->joypad_valid = true;
	
	impl->apu.save_state( out->apu );
	out->apu_valid = true;
	
	ppu.save_state( out );
	
	memcpy( out->ram, cpu::low_mem, out->ram_size );
	out->ram_valid = true;
	
	out->sram_size = 0;
	if ( sram_present )
	{
		out->sram_size = sizeof impl->sram;
		memcpy( out->sram, impl->sram, out->sram_size );
	}
	
	out->mapper->size = 0;
	mapper->save_state( *out->mapper );
	out->mapper_valid = true;
}

void Nes_Core::save_state( Nes_State* out ) const
{
	save_state( reinterpret_cast<Nes_State_*>(out) );
}

void Nes_Core::load_state( Nes_State_ const& in )
{
	require( cart );
	
	disable_rendering();
	error_count = 0;
	
	if ( in.nes_valid )
		nes = in.nes;
	
	// always use frame count
	ppu.burst_phase = 0; // avoids shimmer when seeking to same time over and over
	nes.frame_count = in.nes.frame_count;
	if ( (frame_count_t) nes.frame_count == invalid_frame_count )
		nes.frame_count = 0;
	
	if ( in.cpu_valid )
		cpu::r = *in.cpu;
	
	if ( in.joypad_valid )
		joypad = *in.joypad;
	
	if ( in.apu_valid )
	{
		impl->apu.load_state( *in.apu );
		// prevent apu from running extra at beginning of frame
		impl->apu.end_frame( -(int) nes.timestamp / ppu_overclock );
	}
	else
	{
		impl->apu.reset();
	}
	
	ppu.load_state( in );
	
	if ( in.ram_valid )
		memcpy( cpu::low_mem, in.ram, in.ram_size );
	
	sram_present = false;
	if ( in.sram_size )
	{
		sram_present = true;
		memcpy( impl->sram, in.sram, min( (int) in.sram_size, (int) sizeof impl->sram ) );
		enable_sram( true ); // mapper can override (read-only, unmapped, etc.)
	}
	
	if ( in.mapper_valid ) // restore last since it might reconfigure things
		mapper->load_state( *in.mapper );
}

void Nes_Core::enable_prg_6000()
{
	sram_writable = 0;
	sram_readable = 0;
	lrom_readable = 0x8000;
}

void Nes_Core::enable_sram( bool b, bool read_only )
{
	sram_writable = 0;
	if ( b )
	{
		if ( !sram_present )
		{
			sram_present = true;
			memset( impl->sram, 0xFF, impl->sram_size );
		}
		sram_readable = sram_end;
		if ( !read_only )
			sram_writable = sram_end;
		cpu::map_code( 0x6000, impl->sram_size, impl->sram );
	}
	else
	{
		sram_readable = 0;
		for ( int i = 0; i < impl->sram_size; i += cpu::page_size )
			cpu::map_code( 0x6000 + i, cpu::page_size, impl->unmapped_page );
	}
}

// Unmapped memory

#if !defined (NDEBUG) && 0
static nes_addr_t last_unmapped_addr;
#endif

void Nes_Core::log_unmapped( nes_addr_t addr, int data )
{
	#if !defined (NDEBUG) && 0
		if ( last_unmapped_addr != addr )
		{
			last_unmapped_addr = addr;
			if ( data < 0 )
				dprintf( "Read unmapped %04X\n", addr );
			else
				dprintf( "Write unmapped %04X <- %02X\n", addr, data );
		}
	#endif
}

inline void Nes_Core::cpu_adjust_time( int n )
{
	ppu_2002_time   -= n;
	cpu_time_offset += n;
	cpu::reduce_limit( n );
}

// I/O and sound

int Nes_Core::read_dmc( void* data, nes_addr_t addr )
{
	Nes_Core* emu = (Nes_Core*) data;
	int result = *emu->cpu::get_code( addr );
	if ( wait_states_enabled )
		emu->cpu_adjust_time( 4 );
	return result;
}

void Nes_Core::apu_irq_changed( void* emu )
{
	((Nes_Core*) emu)->irq_changed();
}

void Nes_Core::write_io( nes_addr_t addr, int data )
{
	// sprite dma
	if ( addr == 0x4014 )
	{
		ppu.dma_sprites( clock(), cpu::get_code( data * 0x100 ) );
		cpu_adjust_time( 513 );
		return;
	}
	
	// joypad strobe
	if ( addr == 0x4016 )
	{
		// if strobe goes low, latch data
		if ( joypad.w4016 & 1 & ~data )
		{
			joypad_read_count++;
			joypad.joypad_latches [0] = current_joypad [0];
			joypad.joypad_latches [1] = current_joypad [1];
		}
		joypad.w4016 = data;
		return;
	}
	
	// apu
	if ( unsigned (addr - impl->apu.start_addr) <= impl->apu.end_addr - impl->apu.start_addr )
	{
		impl->apu.write_register( clock(), addr, data );
		if ( wait_states_enabled )
		{
			if ( addr == 0x4010 || (addr == 0x4015 && (data & 0x10)) )
			{
				impl->apu.run_until( clock() + 1 );
				event_changed();
			}
		}
		return;
	}
	
	#ifndef NDEBUG
		log_unmapped( addr, data );
	#endif
}

int Nes_Core::read_io( nes_addr_t addr )
{
	if ( (addr & 0xFFFE) == 0x4016 )
	{
		// to do: to aid with recording, doesn't emulate transparent latch,
		// so a game that held strobe at 1 and read $4016 or $4017 would not get
		// the current A status as occurs on a NES
		int32_t result = joypad.joypad_latches [addr & 1];
		if ( !(joypad.w4016 & 1) )
			joypad.joypad_latches [addr & 1] = result >> 1; // ASR is intentional
		return result & 1;
	}
	
	if ( addr == Nes_Apu::status_addr )
		return impl->apu.read_status( clock() );
	
	#ifndef NDEBUG
		log_unmapped( addr );
	#endif
	
	return addr >> 8; // simulate open bus
}

// CPU

const int irq_inhibit_mask = 0x04;

nes_addr_t Nes_Core::read_vector( nes_addr_t addr )
{
	byte const* p = cpu::get_code( addr );
	return p [1] * 0x100 + p [0];
}

void Nes_Core::reset( bool full_reset, bool erase_battery_ram )
{
	require( cart );
	
	if ( full_reset )
	{
		cpu::reset( impl->unmapped_page );
		cpu_time_offset = -1;
		clock_ = 0;
		
		// Low RAM
		memset( cpu::low_mem, 0xFF, low_ram_size );
		cpu::low_mem [8] = 0xf7;
		cpu::low_mem [9] = 0xef;
		cpu::low_mem [10] = 0xdf;
		cpu::low_mem [15] = 0xbf;
		
		// SRAM
		lrom_readable = 0;
		sram_present = true;
		enable_sram( false );
		if ( !cart->has_battery_ram() || erase_battery_ram )
			memset( impl->sram, 0xFF, impl->sram_size );
		
		joypad.joypad_latches [0] = 0;
		joypad.joypad_latches [1] = 0;
		
		nes.frame_count = 0;
	}
	
	// to do: emulate partial reset
	
	ppu.reset( full_reset );
	impl->apu.reset();
	
	mapper->reset();
	
	cpu::r.pc = read_vector( 0xFFFC );
	cpu::r.sp = 0xfd;
	cpu::r.a = 0;
	cpu::r.x = 0;
	cpu::r.y = 0;
	cpu::r.status = irq_inhibit_mask;
	nes.timestamp = 0;
	error_count = 0;
}

void Nes_Core::vector_interrupt( nes_addr_t vector )
{
	cpu::push_byte( cpu::r.pc >> 8 );
	cpu::push_byte( cpu::r.pc & 0xFF );
	cpu::push_byte( cpu::r.status | 0x20 ); // reserved bit is set
	
	cpu_adjust_time( 7 );
	cpu::r.status |= irq_inhibit_mask;
	cpu::r.pc = read_vector( vector );
}

inline nes_time_t Nes_Core::earliest_irq( nes_time_t present )
{
	return min( impl->apu.earliest_irq( present ), mapper->next_irq( present ) );
}

void Nes_Core::irq_changed()
{
	cpu_set_irq_time( earliest_irq( cpu_time() ) );
}

inline nes_time_t Nes_Core::ppu_frame_length( nes_time_t present )
{
	nes_time_t t = ppu.frame_length();
	if ( t > present )
		return t;
	
	ppu.render_bg_until( clock() ); // to do: why this call to clock() rather than using present?
	return ppu.frame_length();
}

inline nes_time_t Nes_Core::earliest_event( nes_time_t present )
{
	// PPU frame
	nes_time_t t = ppu_frame_length( present );
	
	// DMC
	if ( wait_states_enabled )
		t = min( t, impl->apu.next_dmc_read_time() + 1 );
	
	// NMI
	t = min( t, ppu.nmi_time() );
	
	if ( single_instruction_mode )
		t = min( t, present + 1 );
	
	return t;
}

void Nes_Core::event_changed()
{
	cpu_set_end_time( earliest_event( cpu_time() ) );
}

#undef NES_EMU_CPU_HOOK
#ifndef NES_EMU_CPU_HOOK
	#define NES_EMU_CPU_HOOK( cpu, end_time ) cpu::run( end_time )
#endif

nes_time_t Nes_Core::emulate_frame_()
{
	Nes_Cpu::result_t last_result = cpu::result_cycles;
	int extra_instructions = 0;
	while ( true )
	{
		// Add DMC wait-states to CPU time
		if ( wait_states_enabled )
		{
			impl->apu.run_until( cpu_time() );
			clock_ = cpu_time_offset;
		}
		
		nes_time_t present = cpu_time();
		if ( present >= ppu_frame_length( present ) )
		{
			if ( ppu.nmi_time() <= present )
			{
				// NMI will occur next, so delayed CLI and SEI don't need to be handled.
				// If NMI will occur normally ($2000.7 and $2002.7 set), let it occur
				// next frame, otherwise vector it now.
				
				if ( !(ppu.w2000 & 0x80 & ppu.r2002) )
				{
					dprintf( "vectored NMI at end of frame\n" );
					vector_interrupt( 0xFFFA );
					present += 7;
				}
				return present;
			}
			
			if ( extra_instructions > 2 )
			{
				check( last_result != cpu::result_sei && last_result != cpu::result_cli );
				check( ppu.nmi_time() >= 0x10000 || (ppu.w2000 & 0x80 & ppu.r2002) );
				return present;
			}
			
			if ( last_result != cpu::result_cli && last_result != cpu::result_sei &&
					(ppu.nmi_time() >= 0x10000 || (ppu.w2000 & 0x80 & ppu.r2002)) )
				return present;
			
			dprintf( "Executing extra instructions for frame\n" );
			extra_instructions++; // execute one more instruction
		}
		
		// NMI
		if ( present >= ppu.nmi_time() )
		{
			ppu.acknowledge_nmi();
			vector_interrupt( 0xFFFA );
			last_result = cpu::result_cycles; // most recent sei/cli won't be delayed now
		}
		
		// IRQ
		nes_time_t irq_time = earliest_irq( present );
		cpu_set_irq_time( irq_time );
		if ( present >= irq_time && (!(cpu::r.status & irq_inhibit_mask) ||
				last_result == cpu::result_sei) )
		{
			if ( last_result != cpu::result_cli )
			{
				//dprintf( "%6d IRQ vectored\n", present );
				mapper->run_until( present );
				vector_interrupt( 0xFFFE );
			}
			else
			{
				// CLI delays IRQ
				cpu_set_irq_time( present + 1 );
				check( false ); // rare event
			}
		}
		
		// CPU
		nes_time_t end_time = earliest_event( present );
		if ( extra_instructions )
			end_time = present + 1;
		unsigned long cpu_error_count = cpu::error_count();
		last_result = NES_EMU_CPU_HOOK( cpu, end_time - cpu_time_offset - 1 );
		cpu_adjust_time( cpu::time() );
		clock_ = cpu_time_offset;
		error_count += cpu::error_count() - cpu_error_count;
	}
}

nes_time_t Nes_Core::emulate_frame()
{
	require( cart );
	
	joypad_read_count = 0;
	
	cpu_time_offset = ppu.begin_frame( nes.timestamp ) - 1;
	ppu_2002_time = 0;
	clock_ = cpu_time_offset;
	
	check( cpu_time() == (int) nes.timestamp / ppu_overclock );
	check( 1 && impl->apu.last_time == cpu_time() );
	
	// TODO: clean this fucking mess up
	impl->apu.run_until_( emulate_frame_() );
	clock_ = cpu_time_offset;
	impl->apu.run_until_( cpu_time() );
	check( 2 && clock_ == cpu_time_offset );
	check( 3 && impl->apu.last_time == cpu_time() );
	
	nes_time_t ppu_frame_length = ppu.frame_length();
	nes_time_t length = cpu_time();
	nes.timestamp = ppu.end_frame( length );
	mapper->end_frame( length );
	impl->apu.end_frame( ppu_frame_length );
	check( 4 && cpu_time() == length );
	
	check( 5 && impl->apu.last_time == length - ppu_frame_length );
	
	disable_rendering();
	nes.frame_count++;
	
	return ppu_frame_length;
}

void Nes_Core::add_mapper_intercept( nes_addr_t addr, unsigned size, bool read, bool write )
{
	require( addr >= 0x4000 );
	require( addr + size <= 0x10000 );
	int end = (addr + size + (page_size - 1)) >> page_bits;
	for ( int page = addr >> page_bits; page < end; page++ )
	{
		data_reader_mapped [page] |= read;
		data_writer_mapped [page] |= write;
	}
}

