
// NES emulator with movie recording and playback in both directions

// Nes_Emu 0.7.0

#ifndef NES_RECORDER_H
#define NES_RECORDER_H

#include "Nes_Emu.h"
#include "Nes_Film.h"

class Nes_Recorder : public Nes_Emu {
public:
	// Set new film to use for recording and playback. If film isn't empty, seeks to
	// its beginning (or to optional specified fime). Film *must* be loaded before
	// using the recorder for emulation.
	void set_film( Nes_Film*, frame_count_t );
	void set_film( Nes_Film* f ) { set_film( f, f->begin() ); }
	
	// Currently loaded film
	Nes_Film& film() const { return *film_; }
	
	// Time references are in terms of timestamps at a single moment, rather than
	// the frames that occur between two timestamps. All timestamps must be within
	// the current recording. The emulator is always at a particular timestamp in the
	// recording; a frame exists only in the graphics buffer and sample buffer. Shown
	// below are timestamps and the frame that occurs between 1 and 2.
	// |---|///|---|---
	// 0   1   2   3    timestamps of snapshots between frames
	
	// Current timestamp
	frame_count_t tell() const;
	
	// Seek to new timestamp. Time is constrained to recording if it falls outside.
	void seek( frame_count_t );
	
	// Record new frame at current timestamp and remove anything previously
	// recorded after it.
	blargg_err_t emulate_frame( int joypad, int joypad2 = 0 );
	
	// Increment timestamp and generate frame for that duration. Does nothing
	// if already at end of recording.
	void next_frame();
	
	// Decrement timestamp and generate frame for that duration. Does nothing
	// if already at beginning of recording. Performance is close to that of
	// next_frame().
	void prev_frame();
	
// Additional features
	
	// Disable reverse support and optionally use less-frequent cache snapshots, in order
	// to drastically reduce the required height of the graphics buffer and memory usage.
	// If used, must be called one time *before* any use of emulator.
	void disable_reverse( int cache_period_secs = 5 );
	
	// Call when current film has been significantly changed (loaded from file).
	// Doesn't need to be called if film was merely trimmed.
	void film_changed();
	
	// Attempt to add keyframe to film at current timestamp, which will make future
	// seeking to this point faster after film is later loaded.
	void record_keyframe();
	
	// Get time of nearby key frame within +/- 45 seconds, otherwise return time unchanged.
	// Seeking to times of key frames is much faster than an arbitrary time. Time
	// is constrained to film if it falls outside.
	frame_count_t nearby_keyframe( frame_count_t ) const;
	
	// Quickly skip forwards or backwards, staying within recording. May skip slightly
	// more or less than delta, but will always skip at least some if delta is non-zero
	// (i.e. repeated skip( 1 ) calls will eventually reach the end of the recording).
	void skip( int delta );
	
	// When enabled, emulator is synchronized at each snapshot in film. This
	// corrects any emulation differences compared to when the film was made. Might
	// be necessary when playing movies made in an older version of this emulator
	// or from other emulators.
	void enable_resync( bool b = true ) { resync_enabled = b; }
	
	// Height of graphics buffer needed when running forward (next_frame() or
	// emulate_frame(), but not prev_frame())
	enum { forward_buffer_height = Nes_Ppu::buffer_height };
	
public:
	Nes_Recorder();
	virtual ~Nes_Recorder();
	virtual blargg_err_t init_();
	virtual void reset( bool = true, bool = false );
	void load_state( Nes_State const& s ) { Nes_Emu::load_state( s ); }
	blargg_err_t load_state( Auto_File_Reader r ) { return Nes_Emu::load_state( r ); }
	virtual long read_samples( short* out, long count );
private:
	typedef Nes_Emu base;
	
	// snapshots
	Nes_State* cache;
	int cache_size;
	int cache_period_;
	Nes_Film* film_;
	void clear_cache();
	int cache_index( frame_count_t ) const;
	Nes_State_ const* nearest_snapshot( frame_count_t ) const;
	
	// film
	frame_count_t tell_;
	bool resync_enabled;
	bool ready_to_resync;
	void emulate_frame_( Nes_Film::joypad_t );
	void replay_frame_( Nes_Film::joypad_t );
	int replay_frame();
	void seek_( frame_count_t );
	frame_count_t advancing_frame();
	void loading_state( Nes_State const& );
	
	// reverse handling
	enum { frames_size = frame_rate };
	struct saved_frame_t : Nes_Emu::frame_t {
		enum { max_samples = 2048 };
		blip_sample_t samples [max_samples];
	};
	saved_frame_t* frames;
	bool reverse_enabled;
	bool reverse_allowed;
	void reverse_fill( frame_count_t );
	int reverse_index( frame_count_t ) const; // index for given frame
};

inline frame_count_t Nes_Recorder::tell() const { return film_->constrain( tell_ ); }

inline void Nes_Recorder::film_changed() { set_film( &film(), film().constrain( tell() ) ); }

inline void Nes_Recorder::disable_reverse( int new_cache_period )
{
	reverse_allowed = false;
	buffer_height_ = Nes_Ppu::buffer_height;
	cache_period_ = new_cache_period * frame_rate;
	cache_size = 2 * 60 * frame_rate / cache_period_ + 7; // +7 reduces contention
}

#endif

