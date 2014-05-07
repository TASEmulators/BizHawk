
// Timing and behavior of PPU

// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Ppu.h"

#include <string.h>
#include "Nes_State.h"
#include "Nes_Mapper.h"
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

// to do: remove unnecessary run_until() calls

#include "blargg_source.h"

// Timing

ppu_time_t const scanline_len = Nes_Ppu::scanline_len;

// if non-zero, report sprite max at fixed time rather than calculating it
nes_time_t const fixed_sprite_max_time = 0; // 1 * ((21 + 164) * scanline_len + 100) / ppu_overclock;
int const sprite_max_cpu_offset = 2420 + 3;

ppu_time_t const t_to_v_time = 20 * scanline_len + 302;
ppu_time_t const even_odd_time = 20 * scanline_len + 328;

ppu_time_t const first_scanline_time = 21 * scanline_len + 60; // this can be varied
ppu_time_t const first_hblank_time = 21 * scanline_len + 252;

ppu_time_t const earliest_sprite_max = sprite_max_cpu_offset * ppu_overclock;
ppu_time_t const earliest_sprite_hit = 21 * scanline_len + 339; // needs to be 22 * scanline_len when fixed_sprite_max_time is set

nes_time_t const vbl_end_time = 2272;
ppu_time_t const max_frame_length = 262 * scanline_len;
//ppu_time_t const max_frame_length = 320 * scanline_len; // longer frame for testing movie resync
nes_time_t const earliest_vbl_end_time = max_frame_length / ppu_overclock - 10;

// Scanline rendering

void Nes_Ppu::render_bg_until_( nes_time_t cpu_time )
{
	ppu_time_t time = ppu_time( cpu_time );
	ppu_time_t const frame_duration = scanline_len * 261;
	if ( time > frame_duration )
		time = frame_duration;
	
	// one-time events
	if ( frame_phase <= 1 )
	{
		if ( frame_phase < 1 )
		{
			// vtemp->vaddr
			frame_phase = 1;
			if ( w2001 & 0x08 )
				vram_addr = vram_temp;
		}
		
		// variable-length scanline
		if ( time <= even_odd_time )
		{
			next_bg_time = nes_time( even_odd_time );
			return;
		}
		frame_phase = 2;
		if ( !(w2001 & 0x08) || emu.nes.frame_count & 1 )
		{
			if ( --frame_length_extra < 0 )
			{
				frame_length_extra = 2;
				frame_length_++;
			}
			burst_phase--;
		}
		burst_phase = (burst_phase + 2) % 3;
	}
	
	// scanlines
	if ( scanline_time < time )
	{
		int count = (time - scanline_time + scanline_len) / scanline_len;
		
		// hblank before next scanline
		if ( hblank_time < scanline_time )
		{
			hblank_time += scanline_len;
			run_hblank( 1 );
		}
		
		scanline_time += count * scanline_len;
		
		hblank_time += scanline_len * (count - 1);
		int saved_vaddr = vram_addr;
		
		int start = scanline_count;
		scanline_count += count;
		draw_background( start, count );
		
		vram_addr = saved_vaddr; // to do: this is cheap
		run_hblank( count - 1 );
	}
	
	// hblank after current scanline
	ppu_time_t next_ppu_time = hblank_time;
	if ( hblank_time < time )
	{
		hblank_time += scanline_len;
		run_hblank( 1 );
		next_ppu_time = scanline_time; // scanline will run next
	}
	assert( time <= hblank_time );
	
	// either hblank or scanline comes next
	next_bg_time = nes_time( next_ppu_time );
}

void Nes_Ppu::render_until_( nes_time_t time )
{
	// render bg scanlines then render sprite scanlines up to wherever bg was rendered to
	
	render_bg_until( time );
	next_sprites_time = nes_time( scanline_time );
	if ( host_pixels )
	{
		int start = next_sprites_scanline;
		int count = scanline_count - start;
		if ( count > 0 )
		{
			next_sprites_scanline += count;
			draw_sprites( start, count );
		}
	}
}

// Frame events

inline void Nes_Ppu::end_vblank()
{
	// clear VBL, sprite hit, and max sprites flags first time after 20 scanlines
	r2002 &= end_vbl_mask;
	end_vbl_mask = ~0;
}

inline void Nes_Ppu::run_end_frame( nes_time_t time )
{
	if ( !frame_ended )
	{
		// update frame_length
		render_bg_until( time );

		// set VBL when end of frame is reached
		nes_time_t len = frame_length();
		if ( time >= len )
		{
			r2002 |= 0x80;
			frame_ended = true;
			if ( w2000 & 0x80 )
				nmi_time_ = len + 2 - (frame_length_extra >> 1);
		}
	}
}

// Sprite max

inline void Nes_Ppu::invalidate_sprite_max_()
{
	next_sprite_max_run = earliest_sprite_max / ppu_overclock;
	sprite_max_set_time = 0;
}

void Nes_Ppu::run_sprite_max_( nes_time_t cpu_time )
{
	end_vblank(); // might get run outside $2002 handler
	
	// 577.0 / 0x10000 ~= 1.0 / 113.581, close enough to accurately calculate which scanline it is
	int start_scanline = next_sprite_max_scanline;
	next_sprite_max_scanline = unsigned ((cpu_time - sprite_max_cpu_offset) * 577) / 0x10000u;
	assert( next_sprite_max_scanline >= 0 && next_sprite_max_scanline <= last_sprite_max_scanline );
	
	if ( !sprite_max_set_time )
	{
		if ( !(w2001 & 0x18) )
			return;
		
		long t = recalc_sprite_max( start_scanline );
		sprite_max_set_time = indefinite_time;
		if ( t > 0 )
			sprite_max_set_time = t / 3 + sprite_max_cpu_offset;
		next_sprite_max_run = sprite_max_set_time;
		//dprintf( "sprite_max_set_time: %d\n", sprite_max_set_time );
	}
	
	if ( cpu_time > sprite_max_set_time )
	{
		r2002 |= 0x20;
		//dprintf( "Sprite max flag set: %d\n", sprite_max_set_time );
		next_sprite_max_run = indefinite_time;
	}
}

inline void Nes_Ppu::run_sprite_max( nes_time_t t )
{
	if ( !fixed_sprite_max_time && t > next_sprite_max_run )
		run_sprite_max_( t );
}

inline void Nes_Ppu::invalidate_sprite_max( nes_time_t t )
{
	if ( !fixed_sprite_max_time && !(r2002 & 0x20) )
	{
		run_sprite_max( t );
		invalidate_sprite_max_();
	}
}

// Sprite 0 hit

inline int Nes_Ppu_Impl::first_opaque_sprite_line() /*const*/
{
	// advance earliest time if sprite has blank lines at beginning
	byte const* p = map_chr( sprite_tile_index( spr_ram ) * 16 );
	int twice = w2000 >> 5 & 1; // loop twice if double height is set
	int line = 0;
	do
	{
		for ( int n = 8; n--; p++ )
		{
			if ( p [0] | p [8] )
				return line;
			line++;
		}
		
		p += 8;
	}
	while ( !--twice );
	return line;
}

void Nes_Ppu::update_sprite_hit( nes_time_t cpu_time )
{
	ppu_time_t earliest = earliest_sprite_hit + spr_ram [0] * scanline_len + spr_ram [3];
	//ppu_time_t latest = earliest + sprite_height() * scanline_len;
	
	earliest += first_opaque_sprite_line() * scanline_len;
	
	ppu_time_t time = ppu_time( cpu_time );
	next_sprite_hit_check = indefinite_time;
	
	if ( false )
		if ( earliest < time )
		{
			r2002 |= 0x40;
			return;
		}
	
	if ( time < earliest )
	{
		next_sprite_hit_check = nes_time( earliest );
		return;
	}
	
	// within possible range; render scanline and compare pixels
	int count_needed = 2 + (time - earliest_sprite_hit - spr_ram [3]) / scanline_len;
	if ( count_needed > 240 )
		count_needed = 240;
	while ( scanline_count < count_needed )
		render_bg_until( max( cpu_time, next_bg_time + 1 ) );
	
	if ( sprite_hit_found < 0 )
		return; // sprite won't hit
	
	if ( !sprite_hit_found )
	{
		// check again next scanline
		next_sprite_hit_check = nes_time( earliest_sprite_hit + spr_ram [3] +
				(scanline_count - 1) * scanline_len );
	}
	else
	{
		// hit found
		ppu_time_t hit_time = earliest_sprite_hit + sprite_hit_found - scanline_len;
		
		if ( time < hit_time )
		{
			next_sprite_hit_check = nes_time( hit_time );
			return;
		}
		
		//dprintf( "Sprite hit x: %d, y: %d, scanline_count: %d\n",
		//      sprite_hit_found % 341, sprite_hit_found / 341, scanline_count );
		
		r2002 |= 0x40;
	}
}

// $2002

inline void Nes_Ppu::query_until( nes_time_t time )
{
	end_vblank();
	
	// sprite hit
	if ( time > next_sprite_hit_check )
		update_sprite_hit( time );
	
	// sprite max
	if ( !fixed_sprite_max_time )
		run_sprite_max( time );
	else if ( time >= fixed_sprite_max_time )
		r2002 |= (w2001 << 1 & 0x20) | (w2001 << 2 & 0x20);
}

int Nes_Ppu::read_2002( nes_time_t time )
{
	nes_time_t next = next_status_event;
	next_status_event = vbl_end_time;
	int extra_clock = extra_clocks ? (extra_clocks - 1) >> 2 & 1 : 0;
	if ( time > next && time > vbl_end_time + extra_clock )
	{
		query_until( time );
		
		next_status_event = next_sprite_hit_check;
		nes_time_t const next_max = fixed_sprite_max_time ?
				fixed_sprite_max_time : next_sprite_max_run;
		if ( next_status_event > next_max )
			next_status_event = next_max;

		if ( time > earliest_open_bus_decay() )
		{
			next_status_event = earliest_open_bus_decay();
			update_open_bus( time );
		}
		
		if ( time > earliest_vbl_end_time )
		{
			if ( next_status_event > earliest_vbl_end_time )
				next_status_event = earliest_vbl_end_time;
			run_end_frame( time );
			
			// special vbl behavior when read is just before or at clock when it's set
			if ( extra_clocks != 1 )
			{
				if ( time == frame_length() )
				{
					nmi_time_ = indefinite_time;
					//dprintf( "Suppressed NMI\n" );
				}
			}
			else if ( time == frame_length() - 1 )
			{
				r2002 &= ~0x80;
				frame_ended = true;
				nmi_time_ = indefinite_time;
				//dprintf( "Suppressed NMI\n" );
			}
		}
	}
	emu.set_ppu_2002_time( next_status_event );
	
	int result = r2002;
	second_write = false;
	r2002 = result & ~0x80;
	poke_open_bus( time, result, 0xE0 );
	update_open_bus( time );
	return ( result & 0xE0 ) | ( open_bus & 0x1F );
}

void Nes_Ppu::dma_sprites( nes_time_t time, void const* in )
{
	//dprintf( "%d sprites written\n", time );
	render_until( time );
	
	invalidate_sprite_max( time );
	// catch anything trying to dma while rendering is enabled
	check( time + 513 <= vbl_end_time || !(w2001 & 0x18) );
	
	memcpy( spr_ram + w2003, in, 0x100 - w2003 );
	memcpy( spr_ram, (char*) in + 0x100 - w2003, w2003 );
}

// Read

inline int Nes_Ppu_Impl::read_2007( int addr )
{
	int result = r2007;
	if ( addr < 0x2000 )
	{
		r2007 = *map_chr( addr );
	}
	else
	{
		r2007 = get_nametable( addr ) [addr & 0x3ff];
		if ( addr >= 0x3f00 )
		{
			return palette [map_palette( addr )] | ( open_bus & 0xC0 );
		}
	}
	return result;
}

int Nes_Ppu::read( unsigned addr, nes_time_t time )
{
	if ( addr & ~0x2007 )
		dprintf( "Read from mirrored PPU register 0x%04X\n", addr );
	
	switch ( addr & 7 )
	{
		// status
		case 2: // handled inline
			return read_2002( time );
		
		// sprite ram
		case 4: {
			int result = spr_ram [w2003];
			if ( (w2003 & 3) == 2 )
				result &= 0xe3;
			poke_open_bus( time, result, ~0 );
			return result;
		}
		
		// video ram
		case 7: {
			render_bg_until( time );
			int addr = vram_addr;
			int new_addr = addr + addr_inc;
			vram_addr = new_addr;
			if ( ~addr & new_addr & vaddr_clock_mask )
			{
				emu.mapper->a12_clocked();
				addr = vram_addr - addr_inc; // avoid having to save across func call
			}
			int result = read_2007( addr & 0x3fff );
			poke_open_bus( time, result, ( ( addr & 0x3fff ) >= 0x3f00 ) ? 0x3F : ~0 );
			return result;
		}
		
		default:
			dprintf( "Read from unimplemented PPU register 0x%04X\n", addr );
			break;
	}

	update_open_bus( time );
	
	return open_bus;
}

// Write

void Nes_Ppu::write( nes_time_t time, unsigned addr, int data )
{
	if ( addr & ~0x2007 )
		dprintf( "Wrote to mirrored PPU register 0x%04X\n", addr );
	
	switch ( addr & 7 )
	{
		case 0:{// control
			int changed = w2000 ^ data;
			
			if ( changed & 0x28 )
				render_until( time ); // obj height or pattern addr changed
			else if ( changed & 0x10 )
				render_bg_until( time ); // bg pattern addr changed
			else if ( ((data << 10) ^ vram_temp) & 0x0C00 )
				render_bg_until( time ); // nametable changed
			
			if ( changed & 0x80 )
			{
				if ( time > vbl_end_time + ((extra_clocks - 1) >> 2 & 1) )
					end_vblank(); // to do: clean this up
				
				if ( data & 0x80 & r2002 )
				{
					nmi_time_ = time + 2;
					emu.event_changed();
				}
				if ( time >= earliest_vbl_end_time )
					run_end_frame( time - 1 + (extra_clocks & 1) );
			}
			
			// nametable select
			vram_temp = (vram_temp & ~0x0C00) | ((data & 3) * 0x400);
			
			if ( changed & 0x20 ) // sprite height changed
				invalidate_sprite_max( time );
			w2000 = data;
			addr_inc = data & 4 ? 32 : 1;

			break;
		}
		
		case 1:{// sprites, bg enable
			int changed = w2001 ^ data;
			
			if ( changed & 0xE1 )
			{
				render_until( time + 1 ); // emphasis/monochrome bits changed
				palette_changed = 0x18;
			}
			
			if ( changed & 0x14 )
				render_until( time + 1 ); // sprite enable/clipping changed
			else if ( changed & 0x0A )
				render_bg_until( time + 1 ); // bg enable/clipping changed
			
			if ( changed & 0x08 ) // bg enabled changed
				emu.mapper->run_until( time );
			
			if ( !(w2001 & 0x18) != !(data & 0x18) )
				invalidate_sprite_max( time ); // all rendering just turned on or off
			
			w2001 = data;
			
			if ( changed & 0x08 )
				emu.irq_changed();

			break;
		}
		
		case 3: // spr addr
			w2003 = data;
			poke_open_bus( time, w2003, ~0 );
			break;
		
		case 4:
			//dprintf( "%d sprites written\n", time );
			if ( time > first_scanline_time / ppu_overclock )
			{
				render_until( time );
				invalidate_sprite_max( time );
			}
			spr_ram [w2003] = data;
			w2003 = (w2003 + 1) & 0xff;
			break;
		
		case 5:
			render_bg_until( time );
			if ( (second_write ^= 1) )
			{
				pixel_x = data & 7;
				vram_temp = (vram_temp & ~0x1f) | (data >> 3);
			}
			else
			{
				vram_temp = (vram_temp & ~0x73e0) |
						(data << 12 & 0x7000) | (data << 2 & 0x03e0);
			}
			break;
		
		case 6:
			render_bg_until( time );
			if ( (second_write ^= 1) )
			{
				vram_temp = (vram_temp & 0xff) | (data << 8 & 0x3f00);
			}
			else
			{
				int changed = ~vram_addr & vram_temp;
				vram_addr = vram_temp = (vram_temp & 0xff00) | data;
				if ( changed & vaddr_clock_mask )
					emu.mapper->a12_clocked();
			}
			break;
		
		default:
			dprintf( "Wrote to unimplemented PPU register 0x%04X\n", addr );
			break;
	}

	poke_open_bus( time, data, ~0 );
}

// Frame begin/end

nes_time_t Nes_Ppu::begin_frame( ppu_time_t timestamp )
{
	// current time
	int cpu_timestamp = timestamp / ppu_overclock;
	extra_clocks = timestamp - cpu_timestamp * ppu_overclock;
	
	// frame end
	ppu_time_t const frame_end = max_frame_length - 1 - extra_clocks;
	frame_length_ = (frame_end + (ppu_overclock - 1)) / ppu_overclock;
	frame_length_extra = frame_length_ * ppu_overclock - frame_end;
	assert( (unsigned) frame_length_extra < 3 );
	
	// nmi
	nmi_time_ = indefinite_time;
	if ( w2000 & 0x80 & r2002 )
		nmi_time_ = 2 - (extra_clocks >> 1);
	
	// bg rendering
	frame_phase = 0;
	scanline_count = 0;
	hblank_time = first_hblank_time;
	scanline_time = first_scanline_time;
	next_bg_time = nes_time( t_to_v_time );
	
	// sprite rendering
	next_sprites_scanline = 0;
	next_sprites_time = 0;
	
	// status register
	frame_ended = false;
	end_vbl_mask = ~0xE0;
	next_status_event = 0;
	sprite_hit_found = 0;
	next_sprite_hit_check = 0;
	next_sprite_max_scanline = 0;
	invalidate_sprite_max_();

	decay_low += cpu_timestamp;
	decay_high += cpu_timestamp;
	
	base::begin_frame();
	
	//dprintf( "cpu_timestamp: %d\n", cpu_timestamp );
	return cpu_timestamp;
}

ppu_time_t Nes_Ppu::end_frame( nes_time_t end_time )
{
	render_bg_until( end_time );
	render_until( end_time );
	query_until( end_time );
	run_end_frame( end_time );

	update_open_bus( end_time );
	decay_low -= end_time;
	decay_high -= end_time;
	
	// to do: do more PPU RE to get exact behavior
	if ( w2001 & 0x08 )
	{
		unsigned a = vram_addr + 2;
		if ( (vram_addr & 0xff) >= 0xfe )
			a = (vram_addr ^ 0x400) - 0x1e;
		vram_addr = a;
	}
	
	if ( w2001 & 0x10 )
		w2003 = 0;
	
	suspend_rendering();
	
	return (end_time - frame_length_) * ppu_overclock + frame_length_extra;
}

void Nes_Ppu::poke_open_bus( nes_time_t time, int data, int mask )
{
	open_bus = ( open_bus & ~mask ) | ( data & mask );
	if ( mask & 0x1F ) decay_low = time + scanline_len * 100 / ppu_overclock;
	if ( mask & 0xE0 ) decay_high = time + scanline_len * 100 / ppu_overclock;
}

const nes_time_t Nes_Ppu::earliest_open_bus_decay() const
{
	return ( decay_low < decay_high ) ? decay_low : decay_high;
}
