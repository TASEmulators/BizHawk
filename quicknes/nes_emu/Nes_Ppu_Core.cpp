
// NES PPU register read/write and frame timing

// Nes_Emu 0.5.0. http://www.slack.net/~ant/

#include "Nes_Ppu.h"

#include <string.h>

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

// to do: implement junk in unused bits when reading registers

// to do: put in common or something
template<class T>
inline const T& min( const T& x, const T& y )
{
	if ( x < y )
		return x;
	return y;
}

typedef BOOST::uint8_t byte;

ppu_time_t const ppu_overclock = 3; // PPU clocks for each CPU clock
ppu_time_t const scanline_duration = 341;
ppu_time_t const t_to_v_time = 20 * scanline_duration + 293;
//ppu_time_t const t_to_v_time = 19 * scanline_duration + 330; // 322 - 339 passes test
ppu_time_t const first_scanline_time = 21 * scanline_duration + 128;
ppu_time_t const first_hblank_time = 21 * scanline_duration + 256;

void Nes_Ppu::start_frame()
{
	scanline_time = first_scanline_time;
	hblank_time = first_hblank_time;
	next_time = t_to_v_time / ppu_overclock;
	next_scanline = 0;
	frame_phase = 0;
	query_phase = 0;
	w2003 = 0;
	memset( sprite_scanlines, 64 - max_sprites, sizeof sprite_scanlines );
}

void Nes_Ppu::query_until( nes_time_t time )
{
	// nothing happens until scanline 20
	if ( time > 2271 )
	{
		// clear VBL flag and sprite hit after 20 scanlines
		if ( query_phase < 1 )
		{
			query_phase = 1;
			r2002 &= ~0xc0;
		}
		
		// update sprite hit
		if ( query_phase < 2 && update_sprite_hit( time ) )
			query_phase = 2;
		
		// set VBL flag a few clocks before the end of the frame (cheap hack)
		if ( query_phase < 3 && time > 29777 )
		{
			query_phase = 3;
			r2002 |= 0x80;
		}
	}
}

void Nes_Ppu::end_frame( nes_time_t end_time )
{
	render_until( end_time );
	query_until( end_time );
	// to do: remove (shows number of sprites per line graphically)
	if ( false )
	if ( base_pixels )
	{
		for ( int i = 0; i < image_height; i++ )
		{
			int n = sprite_scanlines [i] - (64 - max_sprites);
			memset( base_pixels + 16 + i * row_bytes, (n <= 8 ? 3 : 6), n );
		}
	}
//  dprintf( "End of frame\n" );
	start_frame();
}

inline byte const* Nes_Ppu::map_chr( int addr ) const
{
	return &chr_rom [map_chr_addr( addr )];
}

// Read/write

inline byte* Nes_Ppu::map_palette( int addr )
{
	if ( (addr & 3) == 0 )
		addr &= 0x0f; // 0x10, 0x14, 0x18, 0x1c map to 0x00, 0x04, 0x08, 0x0c
	
	return &palette [addr & 0x1f];
}

int Nes_Ppu::read( nes_time_t time, unsigned addr )
{
	// Don't catch rendering up to present since status reads don't affect
	// rendering and status is often polled in a tight loop.
	
	switch ( addr & 7 )
	{
		// status
		case 2: {
			second_write = false;
			query_until( time );
			int result = r2002;
			r2002 &= ~0x80;
			return result;
		}
		
		// sprite ram
		case 4: {
			int result = spr_ram [w2003];
			if ( (w2003 & 3) == 2 )
				result &= 0xe3;
			return result;
		}
		
		// video ram
		case 7: {
			render_until( time ); // changes to vram_addr affect rendering
			int result = r2007;
			int a = vram_addr & 0x3fff;
			vram_addr = a + ((w2000 & 4) ? 32 : 1);
			if ( a < 0x2000 )
			{
				r2007 = *map_chr( a );
			}
			else
			{
				r2007 = get_nametable( a ) [a & 0x3ff];
				
				// palette doesn't use read buffer, but it's still filled with nametable contents
				if ( a >= 0x3f00 )
					result = *map_palette( a );
			}
			return result;
		}
	}
	
	return 0;
}

void Nes_Ppu::write( nes_time_t time, unsigned addr, int data )
{
	if ( addr > 0x2007 )
		printf( "Write to mirrored $200x\n" );
	
	int reg = (addr & 7);
	if ( reg == 0 )
	{
		// render only if changes to register could affect it
		int new_temp = (vram_temp & ~0x0c00) | ((data & 3) * 0x400);
		if ( (new_temp - vram_temp) | ((w2000 ^ data) & 0x38) )
			render_until( time );
		
		vram_temp = new_temp;
		w2000 = data;
		return;
	}
	
	render_until( time );
	switch ( reg )
	{
		//case 0: // control (handled above)
		
		case 1: // sprites, bg enable
			w2001 = data;
			break;
		
		case 3: // spr addr
			w2003 = data;
			break;
		
		case 4:
			spr_ram [w2003] = data;
			w2003 = (w2003 + 1) & 0xff;
			break;
		
		case 5:
			if ( second_write ) {
				vram_temp = (vram_temp & ~0x73e0) |
						((data & 0xf8) << 2) | ((data & 7) << 12);
			}
			else {
				pixel_x = data & 7;
				vram_temp = (vram_temp & ~0x001f) | (data >> 3);
			}
			second_write ^= 1;
			break;
		
		case 6: {
			unsigned old_addr = vram_addr;
			if ( second_write )
			{
				vram_addr = vram_temp = (vram_temp & ~0x00ff) | data;
//				if ( time >= 2271 )
//					dprintf( "%d VRAM fine: %d, tile: %d\n",
//							(int) time, int (vram_addr >> 12), int ((vram_addr >> 5) & 31) );
			}
			else
			{
				vram_temp = (vram_temp & ~0xff00) | ((data << 8) & 0x3f00);
			}
			second_write ^= 1;
//			if ( (vram_addr & old_addr) & 0x2000 )
//				dprintf( "%d Toggled A13\n", time );
			break;
		}
		
		case 7:
		{
			int a = vram_addr & 0x3fff;
			vram_addr = a + ((w2000 & 4) ? 32 : 1);
			if ( a < 0x2000 )
			{
				a = map_chr_addr( a );
				BOOST::uint8_t& b = impl->chr_ram [a];
				if ( (b ^ data) & chr_write_mask )
				{
					b = data;
					assert( a < sizeof impl->chr_ram );
					tiles_modified [(unsigned) a / bytes_per_tile] = true;
					any_tiles_modified = true;
				}
			}
			else if ( a < 0x3f00 )
			{
				get_nametable( a ) [a & 0x3ff] = data;
			}
			else
			{
				*map_palette( a ) = data & 0x3f;
			}
			break;
		}
	}
}

// Frame rendering

// returns true when sprite hit checking is done for the frame (hit flag won't change any more)
bool Nes_Ppu::update_sprite_hit( nes_time_t cpu_time )
{
	// earliest time of hit empirically determined by testing on NES
	ppu_time_t const delay = 21 * scanline_duration + 333;
	if ( cpu_time < delay / ppu_overclock )
		return false;
	
	long time = cpu_time * ppu_overclock - delay;
	long low_bound = spr_ram [0] * scanline_duration + spr_ram [3];
	if ( time < low_bound )
		return false;
	
	int tile = spr_ram [1] + ((w2000 << 5) & 0x100);
	int height = 1;
	if ( w2000 & 0x20 )
	{
		height = 2;
		tile = (tile & 1) * 0x100 + (tile & 0xfe);
	}
	byte const* data = map_chr( tile * bytes_per_tile );
	for ( int n = height; n--; )
	{
		for ( int n = 8; n--; )
		{
			if ( time < low_bound )
				return false;
			
			if ( data [0] | data [8] )
			{
				r2002 |= 0x40;
				return true;
			}
			
			data++;
			low_bound += scanline_duration;
		}
		
		data += 8;
	}
	
	return true;
}

void Nes_Ppu::run_hblank( int n )
{
	hblank_time += scanline_duration * n;
	if ( w2001 & 0x08 )
	{
//		vram_addr = (vram_addr & ~0x41f) | (vram_temp & 0x41f);
		long addr = vram_addr + n * 0x1000;
		if ( addr >= 0x8000 )
		{
			addr &= 0x7fff;
			
			int const mask = 0x3e0;
			int a = (addr + 0x20) & mask;
			if ( a == 30 * 0x20 )
			{
				a &= 0x1f;
				addr ^= 0x800;
			}
			addr = (addr & ~mask) | (a & mask);
		}
		assert( addr < 0x8000 );
		vram_addr = addr;
	}
}

void Nes_Ppu::render_until_( nes_time_t cpu_time )
{
	ppu_time_t time = cpu_time * ppu_overclock;
	ppu_time_t const frame_duration = scanline_duration * 261;
	if ( time > frame_duration )
		time = frame_duration;
	
	if ( frame_phase == 0 )
	{
		frame_phase = 1;
		if ( w2001 & 0x08 )
			vram_addr = vram_temp;
//      else
//          dprintf( "PPU off\n" );
	}
	
	if ( hblank_time < scanline_time && hblank_time < time )
		run_hblank( 1 );
	
	int count = 0;
	while ( scanline_time < time )
	{
		scanline_time += scanline_duration;
		count++;
	}
	
	if ( count )
	{
		int start = next_scanline;
		int end = start + count;
		assert( end <= image_height );
		next_scanline = end;
		
		if ( base_pixels )
		{
			if ( start == 0 )
			{
				memcpy( host_palette, palette, 32 );
				int bg = palette [0];
				for ( int i = 0; i < 32; i += 4 )
					host_palette [i] = bg;
				memcpy( host_palette + 32, host_palette, 32 );
			}
			
			if ( w2001 & 0x18 && any_tiles_modified )
			{
				any_tiles_modified = false;
				update_tiles( 0 );
			}
			
			if ( w2001 & 0x08 )
			{
				draw_background( start, end );
			}
			else
			{
				run_hblank( end - start - 1 );
				black_background( start, end );
			}
			
			// when clipping just sprites, save left strip then restore after drawing sprites
			int const obj_mask = 0x04;
			int const bg_mask = 0x02;
			int clip_mode = ~w2001 & (obj_mask | bg_mask);
			
			if ( clip_mode == obj_mask )
				save_left( start, end );
			else if ( clip_mode == bg_mask )
				clip_left( start, end );
			
			if ( w2001 & 0x10 )
				draw_sprites( start, end );
			
			if ( clip_mode == obj_mask )
				restore_left( start, end );
			else if ( clip_mode == (obj_mask | bg_mask) )
				clip_left( start, end );
		}
		else
		{
			run_hblank( end - start - 1 );
		}
	}
	
	if ( hblank_time < time )
		run_hblank( 1 );
	assert( time <= hblank_time );
	
	next_time = min( scanline_time, hblank_time ) / ppu_overclock;
}

