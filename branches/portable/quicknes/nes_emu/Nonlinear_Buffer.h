
// NES non-linear audio output handling.

// Nes_Emu 0.5.6. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NONLINEAR_BUFFER_H
#define NONLINEAR_BUFFER_H

#include "Multi_Buffer.h"
class Nes_Apu;

// Use to make samples non-linear in Blip_Buffer used for triangle, noise, and DMC only
class Nes_Nonlinearizer {
public:
	Nes_Nonlinearizer();
	
	// Must be called when buffer is cleared
	void clear() { accum = 0x8000; }
	
	// Enable/disable non-linear output
	void enable( Nes_Apu&, bool = true );
	
	// Make at most 'count' samples in buffer non-linear and return number
	// of samples modified. This many samples must then be read out of the buffer.
	long make_nonlinear( Blip_Buffer&, long count );
	
private:
	enum { shift = 5 };
	enum { half = 0x8000 >> shift };
	enum { entry_mask = half * 2 - 1 };
	BOOST::uint16_t table [half * 2];
	long accum;
	bool nonlinear;
};

class Nonlinear_Buffer : public Multi_Buffer {
public:
	Nonlinear_Buffer();
	~Nonlinear_Buffer();
	
	// Enable/disable non-linear output
	void enable_nonlinearity( Nes_Apu&, bool = true );
	
	// Blip_Buffer to output other sound chips to
	Blip_Buffer* buffer() { return &buf; }
	
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
	
private:
	Blip_Buffer buf;
	Blip_Buffer tnd;
	Nes_Nonlinearizer nonlinearizer;
};

#endif

