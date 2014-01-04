
// Nes_Emu 0.5.6. http://www.slack.net/~ant/

#include "Nes_Rewinder.h"

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

// to do: fade out at transitions between forward and reverse

#include BLARGG_SOURCE_BEGIN

// If true, always keep recent frame images in graphics buffer. Reduces overall
// performance by about 33% on my machine, due to the frame buffer not staying in the cache.
bool const quick_reverse = false;

Nes_Rewinder::Nes_Rewinder( frame_count_t snapshot_period ) : recorder( snapshot_period )
{
	pixels = NULL;
	frames = NULL;
	reverse_enabled = false;
}

Nes_Rewinder::~Nes_Rewinder()
{
	delete [] frames;
}

blargg_err_t Nes_Rewinder::init()
{
	if ( !frames )
	{
		BLARGG_RETURN_ERR( recorder::init() );
		
		frames = BLARGG_NEW frame_t [frames_size];
		BLARGG_CHECK_ALLOC( frames );
	}
	
	return blargg_success;
}

blargg_err_t Nes_Rewinder::load_ines_rom( Data_Reader& in, Data_Reader* ips )
{
	if ( !frames )
		BLARGG_RETURN_ERR( init() );
	
	return recorder::load_ines_rom( in, ips );
}

long Nes_Rewinder::samples_avail() const
{
	return frames [current_frame].sample_count;
}

inline void copy_reverse( const blip_sample_t* in, int count, blip_sample_t* out, int step )
{
	in += count;
	while ( count > 0 )
	{
		count -= step;
		in -= step;
		*out = *in;
		out += step;
	}
}

long Nes_Rewinder::read_samples( short* out, long out_size )
{
	int count = samples_avail();
	if ( count )
	{
		if ( count > out_size )
		{
			count = out_size;
			assert( false ); // has no provision for reading partial buffer
		}
		
		if ( !reverse_enabled )
		{
			memcpy( out, frames [current_frame].samples, count * sizeof *out );
		}
		else
		{
			int step = samples_per_frame();
			for ( int i = step; i-- > 0; )
				copy_reverse( frames [current_frame].samples + i, count, out + i, step );
		}
		
		if ( fade_sound_in )
		{
			fade_sound_in = false;
			fade_samples_( out, count, 1 );
		}
		
		if ( frames [current_frame].fade_out )
		{
			fade_sound_in = true;
			fade_samples_( out, count, -1 );
		}
	}
	return count;
}

void Nes_Rewinder::seek( frame_count_t time )
{
	if ( time != tell() )
	{
		clear_reverse();
		recorder::seek_( time );
	}
}

inline void Nes_Rewinder::set_output( int index )
{
	recorder::set_pixels( pixels + index * frame_height * row_bytes, row_bytes );
}

void Nes_Rewinder::frame_rendered( int index, bool using_buffer )
{
	frame_t& frame = frames [index];
	if ( recorder::frames_emulated() > 0 )
	{
		frame.palette_size = recorder::palette_size();
		for ( int i = frame.palette_size; i--; )
			frame.palette [i] = recorder::palette_entry( i );
		frame.sample_count = recorder::read_samples( frame.samples, frame.max_samples );
	}
	else if ( pixels && using_buffer )
	{
		int old_index = (index + frames_size - 1) % frames_size;
		
		memcpy( &frame, &frames [old_index], sizeof frame );
		
		// to do: handle case where row_bytes is a lot greater than buffer_width
		memcpy( pixels + index * frame_height * row_bytes,
				pixels + old_index * frame_height * row_bytes,
				row_bytes * frame_height );
	}
	frame.fade_out = false;
}

blargg_err_t Nes_Rewinder::next_frame( int joypad, int joypad2 )
{
	if ( reverse_enabled )
	{
		if ( !get_film().empty() ) // if empty then we can't seek
			recorder::seek_( reversed_time );
		clear_reverse();
	}
	
	current_frame = 0;
	if ( quick_reverse )
	{
		current_frame = recorder::tell() % frames_size;
		if ( buffer_scrambled )
			buffer_scrambled--;
	}
	set_output( current_frame );
	
	BLARGG_RETURN_ERR( recorder::next_frame( joypad, joypad2 ) );
	frame_rendered( current_frame, quick_reverse );
	
	return blargg_success;
}

frame_count_t Nes_Rewinder::tell() const
{
	return reverse_enabled ? reversed_time : recorder::tell();
}

void Nes_Rewinder::clear_cache()
{
	recorder::clear_cache();
	clear_reverse();
}

void Nes_Rewinder::clear_reverse()
{
	buffer_scrambled = frames_size;
	reverse_enabled = false;
	reverse_unmirrored = 0;
	reverse_pivot = 0;
	reversed_time = 0;
}

void Nes_Rewinder::play_frame_( int index )
{
	if ( negative_seek > 0 )
	{
		negative_seek--;
	}
	else
	{
		set_output( index );
		recorder::play_frame_();
		frame_rendered( index, true );
	}
}

void Nes_Rewinder::seek_clamped( frame_count_t time )
{
	negative_seek = movie_begin() - time;
	if ( negative_seek > 0 )
		time = movie_begin();
	recorder::seek_( time );
}

void Nes_Rewinder::enter_reverse()
{
	reversed_time = recorder::tell() - 1;
	
	reverse_pivot = reversed_time % frames_size;
	frame_count_t first_frame = reversed_time - reverse_pivot;
	if ( buffer_scrambled )
	{
		// buffer hasn't been filled with a clean second of frames since last seek
		
		dprintf( "Refilling reverse buffer, pivot: %d\n", (int) reverse_pivot );
		
		// fill beginning
		seek_clamped( first_frame );
		for ( int i = 0; i <= reverse_pivot; i++ )
			play_frame_( i );
		frames [0].fade_out = true;
		
		// fill end
		seek_clamped( first_frame - frames_size + reverse_pivot + 1 );
		for ( int i = reverse_pivot + 1; i < frames_size; i++ )
			play_frame_( i );
	}
	
	if ( reverse_pivot + 1 < frames_size )
		frames [reverse_pivot + 1].fade_out = true;
	
	reverse_unmirrored = 2; // unmirrored for first two passes, then alternating
	reverse_pivot = -reverse_pivot; // don't pivot yet
	
	seek_clamped( first_frame - frames_size );
	
	// Buffer is now filled. Current second is at beginning and previous at end,
	// and in this example reversed_time is 24 and reverse_pivot is 4:
	// 20 21 22 23 24 25 16 17 18 19
	
	// As fragment of current second is played backwards, it will be replaced with
	// beginning of previous second:
	
	//  <---------------
	// 20 21 22 23 24 25 16 17 18 19    frame 25
	// 20 21 22 23 24 10 16 17 18 19    frame 24
	// 20 21 22 23 11 10 16 17 18 19    frame 23
	// 20 21 22 12 11 10 16 17 18 19    frame 22
	// 20 21 13 12 11 10 16 17 18 19    frame 21
	// 20 14 13 12 11 10 16 17 18 19    frame 20
	// 15 14 13 12 11 10 16 17 18 19    frame 19
	// Then filling will keep replacing buffer contents in a converging fashion:
	//                    <---------
	// 15 14 13 12 11 10 16 17 18  0    frame 19
	// 15 14 13 12 11 10 16 17  1  0    frame 18
	// 15 14 13 12 11 10 16  2  1  0    frame 17
	// 15 14 13 12 11 10  3  2  1  0    frame 16
	//  -------------->
	//  4 14 13 12 11 10  3  2  1  0    frame 15
	//  4  5 13 12 11 10  3  2  1  0    frame 14
	//  4  5  6 12 11 10  3  2  1  0    frame 13
	//  4  5  6  7 11 10  3  2  1  0    frame 12
	//  4  5  6  7  8 10  3  2  1  0    frame 11
	//  4  5  6  7  8  9  3  2  1  0    frame 10
	//  <---------------
	// etc.
}

void Nes_Rewinder::prev_frame()
{
	if ( tell() <= movie_begin() )
	{
		require( false ); // tried to go before beginning of movie
		return;
	}
	
	if ( !reverse_enabled )
	{
		reverse_enabled = true;
		enter_reverse();
	}
	else
	{
		play_frame_( current_frame );
		
		reversed_time--;
		if ( reversed_time % frames_size == frames_size - 1 )
		{
			if ( reverse_pivot < 0 )
				reverse_pivot = -reverse_pivot;
			
			if ( --reverse_unmirrored < 0 )
				reverse_unmirrored = 1;
			
			seek_clamped( reversed_time - frames_size * 2 + 1 );
		}
	}
	
	// determine index of frame in buffer
	int raw_index = reversed_time % frames_size;
	int index = raw_index;
	if ( !reverse_unmirrored )
		index = frames_size - 1 - index;
	if ( index <= reverse_pivot )
		index = reverse_pivot - index;
	current_frame = index;
	
	if ( raw_index == 0 ) // previous frame will be from previous restoration
		frames [index].fade_out = true;
}

