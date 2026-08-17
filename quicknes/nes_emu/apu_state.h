
// NES APU state snapshot support

// Nes_Snd_Emu 0.1.7

#ifndef APU_STATE_H
#define APU_STATE_H

#include "blargg_common.h"

struct apu_state_t
{
	typedef BOOST::uint8_t byte;
	
	typedef byte env_t [3];
	/*struct env_t {
		byte delay;
		byte env;
		byte written;
	};*/
	
	struct apu_t {
		byte w40xx [0x14]; // $4000-$4013
		byte w4015; // enables
		byte w4017; // mode
		BOOST::uint16_t frame_delay;
		byte frame_step;
		byte irq_flag;
	} apu;
	
	struct square_t {
		BOOST::uint16_t delay;
		env_t env;
		byte length_counter;
		byte phase;
		byte swp_delay;
		byte swp_reset;
		byte unused2 [1];
	};
	
	square_t square1;
	square_t square2;
	
	struct triangle_t {
		BOOST::uint16_t delay;
		byte length_counter;
		byte phase;
		byte linear_counter;
		byte linear_mode;
	} triangle;
	
	struct noise_t {
		BOOST::uint16_t delay;
		env_t env;
		byte length_counter;
		BOOST::uint16_t shift_reg;
	} noise;
	
	struct dmc_t {
		BOOST::uint16_t delay;
		BOOST::uint16_t remain;
		BOOST::uint16_t addr;
		byte buf;
		byte bits_remain;
		byte bits;
		byte buf_full;
		byte silence;
		byte irq_flag;
	} dmc;
	
	//byte length_counters [4];
	
	enum { tag = 0x41505552 }; // 'APUR'
	void swap();
};
BOOST_STATIC_ASSERT( sizeof (apu_state_t) == 72 );

#endif

