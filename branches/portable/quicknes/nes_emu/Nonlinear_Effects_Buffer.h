
// Effects_Buffer with non-linear sound

// Nes_Emu 0.5.6. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NONLINEAR_EFFECTS_BUFFER_H
#define NONLINEAR_EFFECTS_BUFFER_H

#include "Nonlinear_Buffer.h"
#include "Effects_Buffer.h"

// Effects_Buffer uses several buffers and outputs stereo sample pairs.
class Nonlinear_Effects_Buffer : public Effects_Buffer {
public:
	Nonlinear_Effects_Buffer();
	~Nonlinear_Effects_Buffer();
	
	// Enable/disable non-linear output
	void enable_nonlinearity( Nes_Apu&, bool = true );
	
	// See Effects_Buffer.h for reference
	void config( const config_t& );
	void clear();
	channel_t channel( int );
	long read_samples( blip_sample_t*, long );
	
// End of public interface
private:
	Nes_Nonlinearizer nonlinearizer;
};

#endif

