
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Ppu_Rendering.h"

#include <string.h>
#include <stddef.h>

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

#ifdef BLARGG_ENABLE_OPTIMIZER
	#include BLARGG_ENABLE_OPTIMIZER
#endif

#ifdef __MWERKS__
	static unsigned zero = 0; // helps CodeWarrior optimizer when added to constants
#else
	const  unsigned zero = 0; // compile-time constant on other compilers
#endif

// Nes_Ppu_Impl

inline Nes_Ppu_Impl::cached_tile_t const&
		Nes_Ppu_Impl::get_sprite_tile( byte const* sprite ) /*const*/
{
	cached_tile_t* tiles = tile_cache;
	if ( sprite [2] & 0x40 )
		tiles = flipped_tiles;
	int index = sprite_tile_index( sprite );
	
	// use index directly, since cached tile is same size as native tile
	BOOST_STATIC_ASSERT( sizeof (cached_tile_t) == bytes_per_tile );
	return *(Nes_Ppu_Impl::cached_tile_t*)
			((byte*) tiles + map_chr_addr( index * bytes_per_tile ));
}

inline Nes_Ppu_Impl::cached_tile_t const& Nes_Ppu_Impl::get_bg_tile( int index ) /*const*/
{
	// use index directly, since cached tile is same size as native tile
	BOOST_STATIC_ASSERT( sizeof (cached_tile_t) == bytes_per_tile );
	return *(Nes_Ppu_Impl::cached_tile_t*)
			((byte*) tile_cache + map_chr_addr( index * bytes_per_tile ));
}

// Fill

void Nes_Ppu_Rendering::fill_background( int count )
{
	ptrdiff_t const next_line = scanline_row_bytes - image_width;
	uint32_t* pixels = (uint32_t*) scanline_pixels;
	
	unsigned long fill = palette_offset;
	if ( (vram_addr & 0x3f00) == 0x3f00 )
	{
		// PPU uses current palette entry if addr is within palette ram
		int color = vram_addr & 0x1f;
		if ( !(color & 3) )
			color &= 0x0f;
		fill += color * 0x01010101;
	}
	
	for ( int n = count; n--; )
	{
		for ( int n = image_width / 16; n--; )
		{
			pixels [0] = fill;
			pixels [1] = fill;
			pixels [2] = fill;
			pixels [3] = fill;
			pixels += 4;
		}
		pixels = (uint32_t*) ((byte*) pixels + next_line);
	}
}

void Nes_Ppu_Rendering::clip_left( int count )
{
	ptrdiff_t next_line = scanline_row_bytes;
	byte* p = scanline_pixels;
	unsigned long fill = palette_offset;
	
	for ( int n = count; n--; )
	{
		((uint32_t*) p) [0] = fill;
		((uint32_t*) p) [1] = fill;
		p += next_line;
	}
}

void Nes_Ppu_Rendering::save_left( int count )
{
	ptrdiff_t next_line = scanline_row_bytes;
	byte* in = scanline_pixels;
	uint32_t* out = impl->clip_buf;
	
	for ( int n = count; n--; )
	{
		unsigned long in0 = ((uint32_t*) in) [0];
		unsigned long in1 = ((uint32_t*) in) [1];
		in += next_line;
		out [0] = in0;
		out [1] = in1;
		out += 2;
	}
}

void Nes_Ppu_Rendering::restore_left( int count )
{
	ptrdiff_t next_line = scanline_row_bytes;
	byte* out = scanline_pixels;
	uint32_t* in = impl->clip_buf;
	
	for ( int n = count; n--; )
	{
		unsigned long in0 = in [0];
		unsigned long in1 = in [1];
		in += 2;
		((uint32_t*) out) [0] = in0;
		((uint32_t*) out) [1] = in1;
		out += next_line;
	}
}

// Background

void Nes_Ppu_Rendering::draw_background_( int remain )
{
	// Draws 'remain' background scanlines. Does not modify vram_addr.
	
	int vram_addr = this->vram_addr & 0x7fff;
	byte* row_pixels = scanline_pixels - pixel_x;
	int left_clip = (w2001 >> 1 & 1) ^ 1;
	row_pixels += left_clip * 8;
	do
	{
		// scanlines until next row
		int height = 8 - (vram_addr >> 12);
		if ( height > remain )
			height = remain;
		
		// handle hscroll change before next scanline
		int hscroll_changed = (vram_addr ^ vram_temp) & 0x41f;
		int addr = vram_addr;
		if ( hscroll_changed )
		{
			vram_addr ^= hscroll_changed;
			height = 1; // hscroll will change after first line
		}
		remain -= height;
		
		// increment address for next row
		vram_addr += height << 12;
		assert( vram_addr < 0x10000 );
		if ( vram_addr & 0x8000 )
		{
			int y = (vram_addr + 0x20) & 0x3e0;
			vram_addr &= 0x7fff & ~0x3e0;
			if ( y == 30 * 0x20 )
				y = 0x800; // toggle vertical nametable
			vram_addr ^= y;
		}
		
		// nametable change usually occurs in middle of row
		byte const* nametable = get_nametable( addr );
		byte const* nametable2 = get_nametable( addr ^ 0x400 );
		int count2 = addr & 31;
		int count = 32 - count2 - left_clip;

		// this conditional is commented out because of mmc2\4
		// normally, the extra row of pixels is only fetched when pixel_ x is not 0, which makes sense
		// but here, we need a correct fetch pattern to pick up 0xfd\0xfe tiles off the edge of the display
		
		// this doesn't cause any problems with buffer overflow because the framebuffer we're rendering to is
		// already guarded (width = 272)
		// this doesn't give us a fully correct ppu fetch pattern, but it's close enough for punch out

		//if ( pixel_x )
			count2++;
		
		byte const* attr_table = &nametable [0x3c0 | (addr >> 4 & 0x38)];
		int bg_bank = (w2000 << 4) & 0x100;
		addr += left_clip;
		
		// output pixels
		ptrdiff_t const row_bytes = scanline_row_bytes;
		byte* pixels = row_pixels;
		row_pixels += height * row_bytes;
		
		unsigned long const mask = 0x03030303 + zero;
		unsigned long const attrib_factor = 0x04040404 + zero;
		
		if ( height == 8 )
		{
			// unclipped
			assert( (addr >> 12) == 0 );
			addr &= 0x03ff;
			int const fine_y = 0;
			int const clipped = false;
			#include "Nes_Ppu_Bg.h"
		}
		else
		{
			// clipped
			int const fine_y = addr >> 12;
			addr &= 0x03ff;
			height -= fine_y & 1;
			int const clipped = true;
			#include "Nes_Ppu_Bg.h"
		}
	}
	while ( remain );
}

// Sprites

void Nes_Ppu_Rendering::draw_sprites_( int begin, int end )
{
	// Draws sprites on scanlines begin through end - 1. Handles clipping.
	
	int const sprite_height = this->sprite_height();
	int end_minus_one = end - 1;
	int begin_minus_one = begin - 1;
	int index = 0;
	do
	{
		byte const* sprite = &spr_ram [index];
		index += 4;
		
		// find if sprite is visible
		int top_minus_one = sprite [0];
		int visible = end_minus_one - top_minus_one;
		if ( visible <= 0 )
			continue; // off bottom
		
		// quickly determine whether sprite is unclipped
		int neg_vis = visible - sprite_height;
		int neg_skip = top_minus_one - begin_minus_one;
		if ( (neg_skip | neg_vis) >= 0 ) // neg_skip >= 0 && neg_vis >= 0
		{
			// unclipped
			#ifndef NDEBUG
				int top = sprite [0] + 1;
				assert( (top + sprite_height) > begin && top < end );
				assert( begin <= top && top + sprite_height <= end );
			#endif
			
			int const skip = 0;
			int visible = sprite_height;
			
			#define CLIPPED 0
			#include "Nes_Ppu_Sprites.h"
		}
		else
		{
			// clipped
			if ( neg_vis > 0 )
				visible -= neg_vis;
			
			if ( neg_skip > 0 )
				neg_skip = 0;
			visible += neg_skip;
			
			if ( visible <= 0 )
				continue; // off top
			
			// visible and clipped
			#ifndef NDEBUG
				int top = sprite [0] + 1;
				assert( (top + sprite_height) > begin && top < end );
				assert( top < begin || top + sprite_height > end );
			#endif
			
			int skip = -neg_skip;
			
			//dprintf( "begin: %d, end: %d, top: %d, skip: %d, visible: %d\n",
			//      begin, end, top_minus_one + 1, skip, visible );
			
			#define CLIPPED 1
			#include "Nes_Ppu_Sprites.h"
		}
	}
	while ( index < 0x100 );
}

void Nes_Ppu_Rendering::check_sprite_hit( int begin, int end )
{
	// Checks for sprite 0 hit on scanlines begin through end - 1.
	// Updates sprite_hit_found. Background (but not sprites) must have
	// already been rendered for the scanlines.
	
	// clip
	int top = spr_ram [0] + 1;
	int skip = begin - top;
	if ( skip < 0 )
		skip = 0;
	
	top += skip;
	int visible = end - top;
	if ( visible <= 0 )
		return; // not visible
	
	int height = sprite_height();
	if ( visible >= height )
	{
		visible = height;
		sprite_hit_found = -1; // signal that no more hit checking will take place
	}
	
	// pixels
	ptrdiff_t next_row = this->scanline_row_bytes;
	byte const* bg = this->scanline_pixels + spr_ram [3] + (top - begin) * next_row;
	cache_t const* lines = get_sprite_tile( spr_ram );
	
	// left edge clipping
	int start_x = 0;
	if ( spr_ram [3] < 8 && (w2001 & 0x01e) != 0x1e )
	{
		if ( spr_ram [3] == 0 )
			return; // won't hit
		start_x = 8 - spr_ram [3];
	}
	
	// vertical flip
	int final = skip + visible;
	if ( spr_ram [2] & 0x80 )
	{
		skip += height - 1;
		final = skip - visible;
	}
	
	// check each line
	unsigned long const mask = 0x01010101 + zero;
	do
	{
		// get pixels for line
		unsigned long line = lines [skip >> 1];
		unsigned long hit0 = ((uint32_t*) bg) [0];
		unsigned long hit1 = ((uint32_t*) bg) [1];
		bg += next_row;
		line >>= skip << 1 & 2;
		line |= line >> 1;
		
		// check for hits
		hit0 = ((hit0 >> 1) | hit0) & (line >> 4);
		hit1 = ((hit1 >> 1) | hit1) & line;
		if ( (hit0 | hit1) & mask )
		{
			// write to memory to avoid endian issues
			uint32_t quads [3];
			quads [0] = hit0;
			quads [1] = hit1;
			
			// find which pixel hit
			int x = start_x;
			do
			{
				if ( ((byte*) quads) [x] & 1 )
				{
					x += spr_ram [3];
					if ( x >= 255 )
						break; // ignore right edge
					
					if ( spr_ram [2] & 0x80 )
						skip = height - 1 - skip; // vertical flip
					int y = spr_ram [0] + 1 + skip;
					sprite_hit_found = y * scanline_len + x;
					
					return;
				}
			}
			while ( x++ < 7 );
		}
		if ( skip > final )
			skip -= 2;
		skip++;
	}
	while ( skip != final );
}

// Draw scanlines

inline bool Nes_Ppu_Rendering::sprite_hit_possible( int scanline ) const
{
	return !sprite_hit_found && spr_ram [0] <= scanline && (w2001 & 0x18) == 0x18;
}

void Nes_Ppu_Rendering::draw_scanlines( int start, int count,
		byte* pixels, long pitch, int mode )
{
	assert( start + count <= image_height );
	assert( pixels );
	
	scanline_pixels = pixels + image_left;
	scanline_row_bytes = pitch;
	
	int const obj_mask = 2;
	int const bg_mask = 1;
	int draw_mode = (w2001 >> 3) & 3;
	int clip_mode = (~w2001 >> 1) & draw_mode;
	
	if ( !(draw_mode & bg_mask) )
	{
		// no background
		clip_mode |= bg_mask; // avoid unnecessary save/restore
		if ( mode & bg_mask )
			fill_background( count );
	}
	
	if ( start == 0 && mode & 1 )
		memset( sprite_scanlines, max_sprites - sprite_limit, 240 );
	
	if ( (draw_mode &= mode) )
	{
		// sprites and/or background are being rendered
		
		if ( any_tiles_modified && chr_is_writable )
		{
			any_tiles_modified = false;
			update_tiles( 0 );
		}
		
		if ( draw_mode & bg_mask )
		{
			//dprintf( "bg  %3d-%3d\n", start, start + count - 1 );
			draw_background_( count );
			
			if ( clip_mode == bg_mask )
				clip_left( count );
			
			if ( sprite_hit_possible( start + count ) )
				check_sprite_hit( start, start + count );
		}
		
		if ( draw_mode & obj_mask )
		{
			// when clipping just sprites, save left strip then restore after drawing them
			if ( clip_mode == obj_mask )
				save_left( count );
			
			//dprintf( "obj %3d-%3d\n", start, start + count - 1 );
			
			draw_sprites_( start, start + count );
			
			if ( clip_mode == obj_mask )
				restore_left( count );
			
			if ( clip_mode == (obj_mask | bg_mask) )
				clip_left( count );
		}
	}
	
	scanline_pixels = NULL;
}

void Nes_Ppu_Rendering::draw_background( int start, int count )
{
	// always capture palette at least once per frame
	if ( (start + count >= 240 && !palette_size) || (w2001 & palette_changed) )
	{
		palette_changed = false;
		capture_palette();
	}
	
	if ( host_pixels )
	{
		draw_scanlines( start, count, host_pixels + host_row_bytes * start, host_row_bytes, 1 );
	}
	else if ( sprite_hit_possible( start + count ) )
	{
		// not rendering, but still handle sprite hit using mini graphics buffer
		int y = spr_ram [0] + 1;
		int skip = min( count, max( y - start, 0 ) );
		int visible = min( count - skip, sprite_height() );
		
		assert( skip + visible <= count );
		assert( visible <= mini_offscreen_height );
		
		if ( visible > 0 )
		{
			run_hblank( skip );
			draw_scanlines( start + skip, visible, impl->mini_offscreen, buffer_width, 3 );
		}
	}
}

