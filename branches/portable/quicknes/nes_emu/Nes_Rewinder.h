
// NES recorder with smooth rewind (backwards playback)

// Nes_Emu 0.5.6. Copyright (C) 2004-2005 Shay Green. GNU LGPL license.

#ifndef NES_REWINDER_H
#define NES_REWINDER_H

#include "Nes_Recorder.h"

class Nes_Rewinder : public Nes_Recorder {
	typedef Nes_Recorder recorder;
	enum { frames_size = frames_per_second };
public:
	explicit Nes_Rewinder( frame_count_t snapshot_period = 0 );
	~Nes_Rewinder();
	
	// Nes_Rewinder adds a single feature to Nes_Recorder: the ability to generate
	// consecutive previous frames with similar performance to normal forward
	// generation.
	
	// Emulate frame *ending* at current timestamp. On exit, timestamp = timestamp - 1.
	void prev_frame();
	
	// Recently-generated frames are stored in a caller-supplied graphics buffer,
	// which is many times taller than a single frame image.
	enum { frame_height = Nes_Recorder::buffer_height };
	enum { buffer_height = frame_height * frames_size };

	// Y coordinate of image for current frame in graphics buffer
	int get_buffer_y();
	
	// Documented in Nes_Emu.h and Nes_Recorder.h
	blargg_err_t load_ines_rom( Data_Reader&, Data_Reader* = NULL );
	void set_pixels( void*, long bytes_per_row );
	blargg_err_t next_frame( int joypad, int joypad2 = 0 );
	frame_count_t tell() const;
	void seek( frame_count_t );
	long samples_avail() const;
	long read_samples( short* out, long max_samples );
	int palette_size() const { return frames [current_frame].palette_size; }
	int palette_entry( int i ) const { return frames [current_frame].palette [i]; }
	blargg_err_t init();
	
	// End of public interface
private:
	
	BOOST::uint8_t* pixels;
	long row_bytes;
	
	// frame cache
	struct frame_t
	{
		int sample_count;
		bool fade_out;
		int palette_size;
		byte palette [max_palette_size];
		enum { max_samples = 2048 };
		blip_sample_t samples [max_samples];
	};
	frame_t* frames;
	int current_frame;
	bool fade_sound_in;
	void set_output( int index );
	void frame_rendered( int index, bool using_buffer );
	void clear_cache(); // Nes_Recorder override
	
	// clamped seek
	int negative_seek; // if positive, number of frames to ignore before playing
	void play_frame_( int index );
	void seek_clamped( frame_count_t ); // allows timestamp to be before beginning
	
	// reversed frame mapping
	frame_count_t reversed_time;// current time is different than emulator time in reverse
	int buffer_scrambled;   // number of frames remaining before buffer isn't scrambled
	bool reverse_enabled;
	int reverse_unmirrored; // number of buffers until buffer order is mirrored
	int reverse_pivot;      // pivot point in buffer
	void clear_reverse();
	void enter_reverse();
};

inline int Nes_Rewinder::get_buffer_y()
{
	return current_frame * frame_height + image_top;
}

inline void Nes_Rewinder::set_pixels( void* p, long rb )
{
	pixels = (BOOST::uint8_t*) p;
	row_bytes = rb;
}

#endif

