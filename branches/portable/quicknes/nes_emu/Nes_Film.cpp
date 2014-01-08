
// Nes_Emu 0.7.0. http://www.slack.net/~ant/

#include "Nes_Film.h"

#include <string.h>
#include <stdlib.h>

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

nes_tag_t const joypad_data_tag = FOUR_CHAR('JOYP');

Nes_Film::Nes_Film() { clear( 60 * 60 ); }

Nes_Film::~Nes_Film() { }

void Nes_Film::clear( frame_count_t new_period )
{
	period_ = new_period;
	end_ = begin_ = -invalid_frame_count;
	time_offset = 0;
	has_joypad_sync_ = true;
	has_second_joypad = false;
	data.clear( new_period );
}

inline int Nes_Film::calc_block_count( frame_count_t new_end ) const
{
	// usually one block extra than absolutely needed
	return (new_end - time_offset) / data.period() + 2;
}

blargg_err_t Nes_Film::resize( frame_count_t new_end )
{
	blargg_err_t err = data.resize( calc_block_count( new_end ) );
	if ( !err )
		end_ = new_end;
	return err;
}

inline int Nes_Film::calc_block( frame_count_t time, int* index_out ) const
{
	assert( time_offset <= time && time <= end() );
	frame_count_t rel = time - time_offset;
	int block = rel / data.period();
	*index_out = rel - block * data.period();
	return block;
}

Nes_Film::joypad_t Nes_Film::get_joypad( frame_count_t time ) const
{
	int index;
	block_t const& b = data.read( calc_block( time, &index ) );
	joypad_t result = b.joypad0 [index];
	if ( b.joypads [1] )
		result |= b.joypads [1] [index] << 8;
	
	return result;
}

blargg_err_t Nes_Film::set_joypad( frame_count_t time, joypad_t joypad )
{
	int index;
	int block = calc_block( time, &index );
	block_t* b = data.write( block );
	CHECK_ALLOC( b );
	b->joypad0 [index] = joypad & 0xFF;
	
	int joypad2 = joypad >> 8 & 0xFF;
	if ( joypad2 && !b->joypads [1] )
		CHECK_ALLOC( b = data.alloc_joypad2( block ) );
	if ( b->joypads [1] )
	{
		b->joypads [1] [index] = joypad2;
		has_second_joypad = true;
	}
	return 0;
}

blargg_err_t Nes_Film::record_frame( frame_count_t time, joypad_t joypad, Nes_State_** out )
{
	if ( out )
		*out = 0;
	
	if ( !contains( time ) )
	{
		require( blank() );
		clear();
		begin_ = end_ = time;
		time_offset = time - time % period_;
	}
	
	RETURN_ERR( resize( time + 1 ) );
	
	RETURN_ERR( set_joypad( time, joypad ) );
	
	// first check detects stale snapshot left after trimming film
	if ( read_snapshot( time ).timestamp() > time || time == begin_ || time % period_ == 0 )
	{
		Nes_State_* ss = modify_snapshot( time );
		CHECK_ALLOC( ss );
		if ( out )
			*out = ss;
		if ( time != begin_ )
			ss->set_timestamp( invalid_frame_count ); // caller might not take snapshot
	}
	
	return 0;
}

Nes_State_ const* Nes_Film::nearest_snapshot( frame_count_t time ) const
{
	require( contains( time ) );
	
	if ( time > end() )
		time = end();
	
	for ( int i = snapshot_index( time ); i >= 0; i-- )
	{
		Nes_State_ const& ss = snapshots( i );
		if ( ss.timestamp() <= time ) // invalid timestamp will always be greater
			return &ss;
	}
	
	return 0;
}

frame_count_t Nes_Film::constrain( frame_count_t t ) const
{
	if ( t != invalid_frame_count && !blank() )
	{
		if ( t < begin_ ) t = begin_;
		if ( t > end_   ) t = end_;
	}
	return t;
}

inline bool Nes_Film::contains_range( frame_count_t first, frame_count_t last ) const
{
	return begin_ <= first && first <= last && last <= end_;
}

void Nes_Film::trim( frame_count_t first, frame_count_t last )
{
	check( begin() <= first && first <= last && last <= end() );
	
	// TODO: this routine was broken; check thoroughly
	
	if ( first > begin_ )
		begin_ = first;
	
	// preserve first snapshot, which might be before beginning
	int first_block = (begin_ - time_offset) / data.period();
	if ( first_block > 0 )
	{
		// TODO: pathological thrashing still possible
		Nes_State_ const* ss = nearest_snapshot( begin_ );
		if ( ss )
			first_block = (ss->timestamp() - time_offset) / data.period();
		time_offset += first_block * data.period();
	}
	
	if ( begin_ <= last && last < end_ )
		end_ = last;
	data.trim( first_block, calc_block_count( end_ ) );
	// be sure snapshot for beginning was preserved
	assert( nearest_snapshot( begin_ ) );
}

// Nes_Film_Joypad_Scanner

// Simplifies scanning joypad data
class Nes_Film_Joypad_Scanner {
public:
	// Begin scanning range and set public members for first block
	Nes_Film_Joypad_Scanner( frame_count_t first, frame_count_t last, Nes_Film const& );
	
	int block;  // block index
	int offset; // offset in data
	int count;  // number of bytes
	frame_count_t remain; // number of bytes remaining to scan
	
	// Pointer to temporary buffer of 'block_period' bytes. Cleared
	// to zero before first use.
	unsigned char* buf();
	
	// Go to next block. False if no more blocks.
	bool next();
	
	~Nes_Film_Joypad_Scanner();
private:
	Nes_Film& film;
	unsigned char* buf_;
	void recalc_count();
};

inline unsigned char* Nes_Film_Joypad_Scanner::buf()
{
	if ( !buf_ )
		buf_ = (unsigned char*) calloc( 1, film.data.period() );
	return buf_;
}

inline void Nes_Film_Joypad_Scanner::recalc_count()
{
	count = film.data.period() - offset;
	if ( count > remain )
		count = remain;
}

Nes_Film_Joypad_Scanner::Nes_Film_Joypad_Scanner( frame_count_t first, frame_count_t last,
		Nes_Film const& f ) : film( *(Nes_Film*) &f )
{
	buf_ = 0;
	remain = last - first;
	block = film.calc_block( first, &offset );
	recalc_count();
	film.data.joypad_only( true );
}

Nes_Film_Joypad_Scanner::~Nes_Film_Joypad_Scanner()
{
	film.data.joypad_only( false );
	free( buf_ );
}

bool Nes_Film_Joypad_Scanner::next()
{
	block++;
	offset = 0;
	remain -= count;
	if ( remain <= 0 )
		return false;
	recalc_count();
	return true;
}

// Nes_Film_Writer

blargg_err_t Nes_Film_Writer::end( Nes_Film const& film, frame_count_t first,
		frame_count_t last, frame_count_t period )
{
	RETURN_ERR( film.write_blocks( *this, first, last, period ) );
	return Nes_File_Writer::end();
}

blargg_err_t Nes_Film::write( Auto_File_Writer out, frame_count_t first,
		frame_count_t last, frame_count_t period ) const
{
	Nes_Film_Writer writer;
	RETURN_ERR( writer.begin( out ) );
	return writer.end( *this, first, last, (period ? period : this->period()) );
}

static blargg_err_t write_state( Nes_State_ const& ss, Nes_File_Writer& out )
{
	RETURN_ERR( out.begin_group( state_file_tag ) );
	RETURN_ERR( ss.write_blocks( out ) );
	return out.end_group();
}

blargg_err_t Nes_Film::write_blocks( Nes_File_Writer& out, frame_count_t first,
		frame_count_t last, frame_count_t period ) const
{
	require( contains_range( first, last ) );
	require( nearest_snapshot( first ) );
	frame_count_t first_snapshot = nearest_snapshot( first )->timestamp();
	assert( first_snapshot <= first );
	
	// write info block
	movie_info_t info;
	memset( &info, 0, sizeof info );
	info.begin = first;
	info.length = last - first;
	info.extra = first - first_snapshot;
	info.period = period;
	info.has_joypad_sync = has_joypad_sync_;
	info.joypad_count = 1;
	if ( has_second_joypad )
	{
		// Scan second joypad data for any blocks containing non-zero data
		Nes_Film_Joypad_Scanner joypad( first, last, *this );
		do
		{
			block_t const& b = data.read( joypad.block );
			if ( b.joypads [1] &&
					mem_differs( &b.joypads [1] [joypad.offset], 0, joypad.count ) )
			{
				info.joypad_count = 2;
				break;
			}
		}
		while ( joypad.next() );
	}
	RETURN_ERR( write_nes_state( out, info ) );
	
	// write joypad data
	for ( int i = 0; i < info.joypad_count; i++ )
	{
		Nes_Film_Joypad_Scanner joypad( first_snapshot, last, *this );
		RETURN_ERR( out.write_block_header( joypad_data_tag, joypad.remain ) );
		do
		{
			block_t const& b = data.read( joypad.block );
			byte const* data = b.joypads [i];
			if ( !data )
				CHECK_ALLOC( data = joypad.buf() );
			RETURN_ERR( out.write( &data [joypad.offset], joypad.count ) );
		}
		while ( joypad.next() );
	}
	
	// write first state
	int index = snapshot_index( first_snapshot );
	assert( snapshots( index ).timestamp() == first_snapshot );
	RETURN_ERR( write_state( snapshots( index ), out ) );
	
	// write snapshots that fall within output periods
	// TODO: thorougly verify this tricky algorithm
	//dprintf( "last: %6d\n", last );
	int last_index = snapshot_index( last );
	frame_count_t time = first_snapshot + period;
	for ( ; ++index <= last_index; )
	{
		Nes_State_ const& ss = snapshots( index );
		frame_count_t t = ss.timestamp();
		if ( t != invalid_frame_count )
		{
			while ( time + period <= t )
				time += period;
			
			if ( t >= time - period )
			{
				time += period;
				//dprintf( "time: %6d\n", t );
				RETURN_ERR( write_state( ss, out ) );
			}
		}
	}
	
	return 0;
}

// Nes_Film_Reader

blargg_err_t Nes_Film::read( Auto_File_Reader in )
{
	Nes_Film_Reader reader;
	RETURN_ERR( reader.begin( in, this ) );
	while ( !reader.done() )
		RETURN_ERR( reader.next_block() );
	return 0;
}

Nes_Film_Reader::Nes_Film_Reader()
{
	film = 0;
	info_ptr = 0;
	joypad_count = 0;
	film_initialized = false;
	memset( &info_, 0, sizeof info_ );
}

Nes_Film_Reader::~Nes_Film_Reader() { }

blargg_err_t Nes_Film_Reader::begin( Auto_File_Reader dr, Nes_Film* nf )
{
	film = nf;
	RETURN_ERR( Nes_File_Reader::begin( dr ) );
	if ( block_tag() != movie_file_tag )
		return "Not a movie file";
	return 0;
}

blargg_err_t Nes_Film::begin_read( movie_info_t const& info )
{
	begin_ = info.begin - info.extra;
	end_   = info.begin + info.length;
	time_offset = begin_ - begin_ % period_;
	has_joypad_sync_ = info.has_joypad_sync;
	assert( begin_ <= end_ );
	return resize( end_ );
}

blargg_err_t Nes_Film_Reader::customize()
{
	require( info_ptr );
	if ( film_initialized )
		return 0;
	film_initialized = true;
	film->clear();
	return film->begin_read( info_ );
}

blargg_err_t Nes_Film_Reader::next_block()
{
	blargg_err_t err = next_block_();
	if ( err )
		film->clear(); // don't leave film in inconsistent state when reading fails
	return err;
}

blargg_err_t Nes_Film_Reader::next_block_()
{
	for ( ; ; )
	{
		RETURN_ERR( Nes_File_Reader::next_block() );
		switch ( depth() == 0 ? block_tag() : 0 )
		{
		case movie_info_t::tag:
			check( !info_ptr );
			RETURN_ERR( read_nes_state( *this, &info_ ) );
			info_ptr = &info_;
			return 0;
		
		case joypad_data_tag:
			RETURN_ERR( customize() );
			RETURN_ERR( film->read_joypad( *this, joypad_count++ ) );
			break;
		
		case state_file_tag:
			RETURN_ERR( customize() );
			RETURN_ERR( film->read_snapshot( *this ) );
			break;
		
		default:
			if ( done() )
			{
				// at least first snapshot must have been read
				check( film->read_snapshot( film->begin_ ).timestamp() != invalid_frame_count );
				film->begin_ += info_.extra; // bump back to claimed beginning
// todo: remove
#if !defined (NDEBUG) && 0
FILE* out = fopen( "raw_block", "wb" );
int block_count = (film->end() - film->time_offset) / film->data.period() + 1;
//for ( int i = 0; i < block_count; i++ )
int i = (block_count > 1);
	fwrite( &film->data.read( i ), offsetof (Nes_Film_Data::block_t,joypad0 [film->data.period()]), 1, out );
fclose( out );
#endif
			}
			return 0;
		}
	}
}

blargg_err_t Nes_Film::read_joypad( Nes_File_Reader& in, int index )
{
	check( index <= 1 );
	if ( index <= 1 )
	{
		Nes_Film_Joypad_Scanner joypad( begin_, end_, *this );
		do
		{
			block_t* b = data.write( joypad.block );
			CHECK_ALLOC( b );
			byte* p = b->joypads [index];
			if ( !p )
				CHECK_ALLOC( p = joypad.buf() );
			p += joypad.offset;
			RETURN_ERR( in.read( p, joypad.count ) );
			if ( !b->joypads [index] && mem_differs( p, 0, joypad.count ) )
			{
				// non-zero joypad2 data
				CHECK_ALLOC( b = data.alloc_joypad2( joypad.block ) );
				memcpy( &b->joypads [index] [joypad.offset], p, joypad.count );
				has_second_joypad = true;
			}
		}
		while ( joypad.next() );
	}
	
	return 0;
}

blargg_err_t Nes_Film::read_snapshot( Nes_File_Reader& in )
{
	RETURN_ERR( in.enter_group() );
	
	// read snapshot's timestamp
	nes_state_t info;
	memset( &info, 0, sizeof info );
	for ( ; ; )
	{
		RETURN_ERR( in.next_block() );
		if ( in.block_tag() == info.tag )
		{
			RETURN_ERR( read_nes_state( in, &info ) );
			break;
		}
		check( false ); // shouldn't encounter any unknown blocks
	}
	frame_count_t time = info.frame_count;
	
	if ( !contains( time ) )
	{
		check( false );
	}
	else
	{
		// read snapshot only if it's earlier than any existing snapshot in same segment
		Nes_State_* ss = modify_snapshot( time );
		CHECK_ALLOC( ss );
		
		// uninitialized snapshot's time is large positive value so always compares greater
		if ( time < ss->timestamp() )
		{
			// read new snapshot
			ss->clear();
			ss->set_nes_state( info );
			do
			{
				RETURN_ERR( ss->read_blocks( in ) );
			}
			while ( in.block_type() != in.group_end );
		}
	}
	return in.exit_group();
}

