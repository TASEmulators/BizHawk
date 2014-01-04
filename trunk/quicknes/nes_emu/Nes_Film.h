
// Film to record NES movies on using Nes_Recorder

// Nes_Emu 0.7.0

#ifndef NES_FILM_H
#define NES_FILM_H

#include "blargg_common.h"
#include "Nes_Film_Data.h"

// See below for custom reader and writer classes that allow user data in movie files
class Nes_Film_Writer;
class Nes_Film_Reader;

class Nes_Film {
public:
	Nes_Film();
	~Nes_Film();
	
	// Clear film to blankness
	void clear() { clear( period() ); }
	
	// Time of first recorded snapshot
	frame_count_t begin() const { return begin_; }
	
	// Time of *end* of last recorded frame
	frame_count_t end() const { return end_; }
	
	// Number of frames in recording
	frame_count_t length() const { return end() - begin(); }
	
	// Trim to subset of recording. OK if new_begin == new_end, which does
	// not make film blank, merely of zero length.
	void trim( frame_count_t new_begin, frame_count_t new_end );
	
	// Write entire recording to file
	blargg_err_t write( Auto_File_Writer ) const;
	
	// Read entire recording from file
	blargg_err_t read( Auto_File_Reader );
	
// Additional features

	// Write trimmed recording to file, with snapshots approximately every 'period' frames
	blargg_err_t write( Auto_File_Writer, frame_count_t begin, frame_count_t end,
			frame_count_t period = 0 ) const;
	
	// Clear film and set how often snapshots are taken. One snapshot is kept for
	// every 'period' frames of recording. A lower period makes seeking faster but
	// uses more memory.
	void clear( frame_count_t new_period );
	
	// Average number of frames between snapshots
	frame_count_t period() const { return period_; }
	
	// True if film has just been cleared
	bool blank() const { return end_ < 0; }
	
	// True if timestamp is within recording. Always false if film is blank.
	bool contains( frame_count_t t ) const { return begin() <= t && t <= end(); }
	
	// True if recording contains frame beginning at timestamp
	bool contains_frame( frame_count_t t ) const { return begin() <= t && t < end(); }
	
	// Constrain timestamp to recorded range, or return unchanged if film is blank
	frame_count_t constrain( frame_count_t ) const;
	
// Raw access for use by Nes_Recorder
	
	// True if joypad entries are 0xFF on frames that the joypad isn't read
	bool has_joypad_sync() const { return has_joypad_sync_; }
	
	// Snapshot that might have current timestamp
	Nes_State_ const& read_snapshot( frame_count_t ) const;
	
	// Snapshot that current timestamp maps to. NULL if out of memory.
	Nes_State_* modify_snapshot( frame_count_t );
	
	// Pointer to nearest snapshot at or before timestamp, or NULL if none
	Nes_State_ const* nearest_snapshot( frame_count_t ) const;
	
	typedef unsigned long joypad_t;
	
	// Get joypad data for frame beginning at timestamp
	joypad_t get_joypad( frame_count_t ) const;
	
	// Change joypad data for frame beginning at timestamp. Frame must already have
	// been recorded normally.
	blargg_err_t set_joypad( frame_count_t, joypad_t );
	
	// Record new frame beginning at timestamp using joypad data. Returns
	// pointer where snapshot should be saved to, or NULL if a snapshot isn't
	// needed for this timestamp. Removes anything recorded after frame.
	blargg_err_t record_frame( frame_count_t, joypad_t joypad, Nes_State_** out = 0 );
	
private:
	// noncopyable
	Nes_Film( Nes_Film const& );
	Nes_Film& operator = ( Nes_Film const& );
	
	typedef Nes_Film_Data::block_t block_t;
	Nes_Film_Data data;
	frame_count_t begin_;
	frame_count_t end_;
	frame_count_t period_;
	frame_count_t time_offset;
	bool has_joypad_sync_;
	bool has_second_joypad;
	
	int calc_block( frame_count_t time, int* index_out ) const;
	int snapshot_index( frame_count_t ) const;
	Nes_State_ const& snapshots( int ) const;
	int calc_block_count( frame_count_t new_end ) const;
	blargg_err_t resize( frame_count_t new_end );
	bool contains_range( frame_count_t first, frame_count_t last ) const;
	
	blargg_err_t write_blocks( Nes_File_Writer&, frame_count_t first,
			frame_count_t last, frame_count_t period ) const;
	blargg_err_t begin_read( movie_info_t const& );
	blargg_err_t read_joypad( Nes_File_Reader&, int index );
	blargg_err_t read_snapshot( Nes_File_Reader& );
	
	friend class Nes_Film_Reader;
	friend class Nes_Film_Writer;
	friend class Nes_Film_Joypad_Scanner;
};

// Allows user data blocks to be written with film
class Nes_Film_Writer : public Nes_File_Writer {
public:
	// Begin writing movie file
	blargg_err_t begin( Auto_File_Writer );
	
	// End writing movie file. Optionally specify custom period and subset of
	// recording to write.
	blargg_err_t end( Nes_Film const& );
	blargg_err_t end( Nes_Film const&, frame_count_t first, frame_count_t last,
			frame_count_t period );
};

// Allows film information to be checked before loading film, and for handling
// of user data blocks.
class Nes_Film_Reader : public Nes_File_Reader {
public:
	Nes_Film_Reader();
	~Nes_Film_Reader();
	
	// Begin reading from movie file. Does not modify film until later (see below).
	blargg_err_t begin( Auto_File_Reader, Nes_Film* out );
	
	// Go to next custom block in file
	blargg_err_t next_block();
	
	// Information about film (see nes_state.h for fields). Returns zero
	// until information is encountered in file. Once return value becomes
	// non-zero, next call to next_block() will read movie into film.
	// Until that time, film is not modified or examined at all.
	movie_info_t const* info() const { return info_ptr; }
	
	// to do: allow reading subset of recording from file (for example, last 5 minutes)
	
private:
	Nes_Film* film;
	bool film_initialized;
	movie_info_t info_;
	movie_info_t* info_ptr;
	int joypad_count;
	
	blargg_err_t customize();
	blargg_err_t next_block_();
};

inline blargg_err_t Nes_Film_Writer::begin( Auto_File_Writer dw )
{
	return Nes_File_Writer::begin( dw, movie_file_tag );
}

inline blargg_err_t Nes_Film::write( Auto_File_Writer out ) const
{
	return write( out, begin(), end(), period() );
}

inline blargg_err_t Nes_Film_Writer::end( Nes_Film const& film )
{
	return end( film, film.begin(), film.end(), film.period() );
}

inline int Nes_Film::snapshot_index( frame_count_t time ) const
{
	return (time - time_offset) / period_;
}

inline Nes_State_ const& Nes_Film::snapshots( int i ) const
{
	return data.read( (unsigned) i / data.block_size ).states [(unsigned) i % data.block_size];
}

inline Nes_State_ const& Nes_Film::read_snapshot( frame_count_t time ) const
{
	return snapshots( snapshot_index( time ) );
}

inline Nes_State_* Nes_Film::modify_snapshot( frame_count_t time )
{
	int i = snapshot_index( time );
	block_t* b = data.write( (unsigned) i / data.block_size );
	if ( b )
		return &b->states [(unsigned) i % data.block_size];
	return 0;
}

#endif

