
// NES non-linear audio output

// Nes_Emu 0.5.0. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NES_NONLINEARIZER_H
#define NES_NONLINEARIZER_H

#include "Multi_Buffer.h"

class Nes_Nonlinearizer : public Multi_Buffer {
public:
	Nes_Nonlinearizer();
	~Nes_Nonlinearizer();
	
	// Enable non-linear output
	void enable_nonlinearity( bool = true );
	
	// See Multi_Buffer.h
	blargg_err_t sample_rate( long rate, int msec = blip_default_length );
	Multi_Buffer::sample_rate;
	void clock_rate( long );
	void bass_freq( int );
	void clear();
	channel_t channel( int );
	void end_frame( blip_time_t, bool unused = true );
	long samples_avail() const;
	long read_samples( blip_sample_t*, long );
	
	// End of public interface
private:
	enum { shift = 5 };
	enum { half = 0x8000 >> shift };
	enum { entry_mask = half * 2 - 1 };
	Blip_Buffer buf;
	Blip_Buffer tnd;
	long accum;
	BOOST::uint16_t table [half * 2];
	
	void make_nonlinear( long );
};

#endif

