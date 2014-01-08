
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Recorder.h"

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

int const joypad_sync_value = 0xFF; // joypad data on frames it's never read

Nes_Recorder::Nes_Recorder()
{
	cache = 0;
	film_ = 0;
	frames = 0;
	resync_enabled = false;
	disable_reverse( 1 ); // sets cache_period_ and cache_size
	reverse_allowed = true;
	buffer_height_ = Nes_Ppu::buffer_height * frames_size + 2;
}

Nes_Recorder::~Nes_Recorder()
{
	free( frames );
	delete [] cache;
}

blargg_err_t Nes_Recorder::init_()
{
	RETURN_ERR( base::init_() );
	
	cache = new Nes_State [cache_size];
	
	int frame_count = reverse_allowed ? frames_size : 1;
	CHECK_ALLOC( frames = (saved_frame_t*) calloc( sizeof *frames, frame_count ) );
	for ( int i = 0; i < frame_count; i++ )
		frames [i].top = (long) i * Nes_Ppu::buffer_height + 1;
	
	return 0;
}

void Nes_Recorder::clear_cache()
{
	ready_to_resync = false;
	reverse_enabled = false;
	for ( int i = 0; i < cache_size; i++ )
		cache [i].set_timestamp( invalid_frame_count );
}

void Nes_Recorder::set_film( Nes_Film* new_film, frame_count_t time )
{
	require( new_film );
	film_ = new_film;
	clear_cache();
	if ( !film_->blank() )
	{
		tell_ = film_->constrain( time );
		check( tell_ == time ); // catch seeks outside film
		seek_( tell_ );
	}
}

void Nes_Recorder::reset( bool full_reset, bool erase_battery_ram )
{
	base::reset( full_reset, erase_battery_ram );
	tell_ = 0;
	film_->clear();
	clear_cache();
}

void Nes_Recorder::loading_state( Nes_State const& in )
{
	reset();
	tell_ = in.timestamp();
}

// Frame emulation

inline int Nes_Recorder::cache_index( frame_count_t t ) const
{
	return (t / cache_period_) % cache_size;
}

void Nes_Recorder::emulate_frame_( Nes_Film::joypad_t joypad )
{
	if ( base::timestamp() % cache_period_ == 0 )
		save_state( &cache [cache_index( base::timestamp() )] );
	
	if ( base::emulate_frame( joypad & 0xFF, (joypad >> 8) & 0xFF ) ) { }
}

void Nes_Recorder::replay_frame_( Nes_Film::joypad_t joypad )
{
	if ( base::timestamp() % film_->period() == 0 )
	{
		if ( film_->read_snapshot( base::timestamp() ).timestamp() == invalid_frame_count )
		{
			Nes_State_* ss = film_->modify_snapshot( base::timestamp() );
			if ( ss )
				save_state( ss );
			else
				check( false ); // out of memory simply causes lack of caching
		}
	}
	
	emulate_frame_( joypad );
}

int Nes_Recorder::replay_frame()
{
	frame_count_t start_time = base::timestamp();
	int joypad = film_->get_joypad( start_time );
	if ( !film_->has_joypad_sync() )
	{
		replay_frame_( joypad );
	}
	else if ( (joypad & 0xFF) != joypad_sync_value )
	{
		// joypad should be read
		replay_frame_( joypad );
		if ( !joypad_read_count() )
		{
			// emulator has fallen behind
			dprintf( "Fell behind joypad data \n" );
			base::set_timestamp( start_time );
		}
	}
	else
	{
		// get joypad for next frame in case emulator gets ahead
		if ( film_->contains_frame( start_time + 1 ) )
		{
			joypad = film_->get_joypad( start_time + 1 );
			if ( (joypad & 0xFF) == joypad_sync_value )
				joypad = 0; // next frame shouldn't read joypad either, so just give it nothing
		}

		// joypad should not be read
		replay_frame_( joypad );
		if ( joypad_read_count() )
		{
			// emulator is ahead
			dprintf( "Ahead of joypad data \n" );
			base::set_timestamp( film_->constrain( base::timestamp() + 1 ) );
		}
	}
	return base::timestamp() - start_time;
}

// Film handling

Nes_State_ const* Nes_Recorder::nearest_snapshot( frame_count_t time ) const
{
	Nes_State_ const* ss = film_->nearest_snapshot( time );
	if ( ss )
	{
		// check cache for any snapshots more recent than film_'s
		for ( frame_count_t t = time - time % cache_period_;
				ss->timestamp() < t; t -= cache_period_ )
		{
			Nes_State_ const& cache_ss = cache [cache_index( t )];
			if ( cache_ss.timestamp() == t )
				return &cache_ss;
		}
	}
	return ss;
}

void Nes_Recorder::seek_( frame_count_t time )
{
	Nes_State_ const* ss = nearest_snapshot( time );
	if ( !film_->contains( time ) || !ss )
	{
		require( false ); // tried to seek outside recording
		return;
	}
	
	base::load_state( *ss );
	frame_ = 0; // don't render graphics
	frame_count_t max_iter = (time - base::timestamp()) * 2; // don't seek forever
	while ( base::timestamp() < time && max_iter-- )
		replay_frame();
}

void Nes_Recorder::seek( frame_count_t time )
{
	check( film_->contains( time ) );
	time = film_->constrain( time );
	if ( time != tell() )
	{
		reverse_enabled = false;
		seek_( time );
		tell_ = base::timestamp();
	}
}

inline frame_count_t Nes_Recorder::advancing_frame()
{
	if ( reverse_enabled )
	{
		reverse_enabled = false;
		if ( !film_->blank() )
			seek_( tell_ );
	}
	frame_ = &frames [0];
	tell_ = base::timestamp();
	return tell_++;
}

blargg_err_t Nes_Recorder::emulate_frame( int joypad, int joypad2 )
{
	frame_count_t time = advancing_frame();
	require( film_->blank() || film_->contains( time ) );
	
	Nes_Film::joypad_t joypads = joypad2 * 0x100 + joypad;
	
	Nes_State_* ss = 0;
	RETURN_ERR( film_->record_frame( time, joypads, &ss ) );
	if ( ss )
		save_state( ss );
	
	emulate_frame_( joypads );
	
	if ( film_->has_joypad_sync() && !joypad_read_count() )
		RETURN_ERR( film_->set_joypad( time, joypad_sync_value ) );
	
	// avoid stale cache snapshot after trimming film
	if ( base::timestamp() % cache_period_ == 0 )
		cache [cache_index( base::timestamp() )].set_timestamp( invalid_frame_count );
	
	return 0;
}

void Nes_Recorder::next_frame()
{
	if ( tell() >= film_->end() )
	{
		check( false ); // tried to go past end
		return;
	}
	frame_count_t time = advancing_frame();
	assert( base::timestamp() == time || base::timestamp() == time + 1 );
	
	// ready_to_resync avoids endless resyncing if joypad isn't getting read when
	// it should, thus the timestamp never incrementing.
	if ( ready_to_resync )
	{
		Nes_State_ const& ss = film_->read_snapshot( time );
		if ( ss.timestamp() == time )
		{
		// todo: remove
		#if !defined (NDEBUG) && 1
			dprintf( "Resynced \n" );
			
			static Nes_State* temp = BLARGG_NEW Nes_State [2];
			static char* temp2 = new char [sizeof *temp];
			if ( temp && temp2 )
			{
				save_state( temp );
				memcpy( temp2, temp, sizeof *temp );
				long a = temp->apu.noise.shift_reg;
				long b = temp->apu.apu.w40xx [0x11];
				long c = temp->apu.dmc.bits;
				base::load_state( ss );
				save_state( temp );
				save_state( &temp [1] );
				
				// shift register and dac are not maintained
				temp->apu.noise.shift_reg = a;
				temp->apu.apu.w40xx [0x11] = b;
				temp->apu.dmc.bits = c;
				
				if ( memcmp( temp2, temp, sizeof *temp ) )
				{
					check( !"Film sync corrected error" );
					Std_File_Writer out;
					(void) !out.open( "state0" );
					(void) !temp [0].write( out );
					(void) !out.open( "state1" );
					(void) !temp [1].write( out );
					//(void) !out.open( "state2" );
					//(void) !ss.write( out );
				}
			}
			
			if ( 0 )
		#endif
			base::load_state( ss );
		}
	}
	
	int count = replay_frame();
	tell_ = base::timestamp();
	
	// examination of count prevents endless resync if frame is getting doubled
	ready_to_resync = false;
	if ( count && resync_enabled && film_->read_snapshot( tell_ ).timestamp() == tell_ )
	{
		fade_sound_out = true;
		ready_to_resync = true;
	}
}

// Extra features

void Nes_Recorder::record_keyframe()
{
	if ( !film_->blank() )
	{
		Nes_State_* ss = film_->modify_snapshot( base::timestamp() );
		if ( !ss )
		{
			check( false ); // out of memory simply causes lack of key frame adjustment
		}
		// first snapshot can only be replaced if key frame is at beginning of film
		else if ( ss->timestamp() > film_->begin() || base::timestamp() == film_->begin() )
		{
			if ( ss->timestamp() != base::timestamp() )
				save_state( ss );
		}
	}
}

frame_count_t Nes_Recorder::nearby_keyframe( frame_count_t time ) const
{
	// TODO: reimplement using direct snapshot and cache access
	check( film_->contains( time ) );
	
	// don't adjust time if seeking to beginning or end
	if ( film_->begin() < time && time < film_->end() )
	{
		// rounded time must be within about a minute of requested time
		int const half_threshold = 45 * frame_rate;
		
		// find nearest snapshots before and after requested time
		frame_count_t after = invalid_frame_count;
		frame_count_t before = time + half_threshold;
		do
		{
			after = before;
			Nes_State_ const* ss = nearest_snapshot( film_->constrain( before - 1 ) );
			if ( !ss )
			{
				require( false ); // tried to seek outside recording
				return time;
			}
			before = ss->timestamp();
		}
		while ( time < before );
		
		// determine closest and use if within threshold
		frame_count_t closest = after;
		if ( time - before < after - time )
			closest = before;
		int delta = time - closest;
		if ( max( delta, -delta ) < half_threshold )
			time = closest;
		if ( time < film_->begin() )
			time = film_->begin();
	}
	return time;
}

void Nes_Recorder::skip( int delta )
{
	if ( delta ) // rounding code can't handle zero
	{
		// round to nearest cache timestamp (even if not in cache)
		frame_count_t current = tell();
		frame_count_t time = current + delta + cache_period_ / 2;
		time -= time % cache_period_;
		if ( delta < 0 )
		{
			if ( time >= current )
				time -= cache_period_;
		}
		else if ( time <= current )
		{
			time += cache_period_;
		}
		seek( film_->constrain( time ) );
	}
}

// Reverse handling

// Get index of frame at given timestamp in reverse frames
inline int Nes_Recorder::reverse_index( frame_count_t time ) const
{
	int index = time % (frames_size * 2);
	if ( index >= frames_size ) // opposite direction on odd runs
		index = frames_size * 2 - 1 - index;
	return index;
}

// Generate frame at given timestamp into proper position in reverse frames
void Nes_Recorder::reverse_fill( frame_count_t time )
{
	if ( time >= film_->begin() )
	{
		// todo: can cause excessive seeking when joypad data loses sync
		if ( base::timestamp() != time )
			seek_( time );
		
		saved_frame_t* frame = &frames [reverse_index( time )];
		frame_ = frame;
		if ( time % frames_size == frames_size - 1 )
			fade_sound_out = true;
		replay_frame();
		frame->sample_count = base::read_samples( frame->samples, frame->max_samples );
	}
}

void Nes_Recorder::prev_frame()
{
	if ( tell() <= film_->begin() || !reverse_allowed )
	{
		check( false ); // tried to go before beginning
		return;
	}
	
	int offset = tell_ % frames_size;
	frame_count_t aligned = tell_ - offset;
	if ( reverse_enabled )
	{
		reverse_fill( aligned - 1 - offset );
	}
	else
	{
		reverse_enabled = true;
		for ( int i = 0; i < frames_size; i++ )
		{
			frame_count_t time = aligned + i;
			if ( i >= offset )
				time -= offset + frames_size; // restore some of previous second
			reverse_fill( time );
		}
	}
	
	tell_--;
	frame_ = &frames [reverse_index( tell_ )];
}

long Nes_Recorder::read_samples( short* out, long count )
{
	require( count >= frame().sample_count );
	if ( !reverse_enabled )
		return base::read_samples( out, count );
	
	// copy samples in reverse without reversing left and right channels
	// to do: optimize?
	count = frame().sample_count;
	blip_sample_t const* in = STATIC_CAST(saved_frame_t const&,frame()).samples;
	int step = frame().chan_count - 1;
	for ( int i = 0; i < count; i++ )
		out [count - 1 - (i ^ step)] = in [i];
	
	return count;
}

