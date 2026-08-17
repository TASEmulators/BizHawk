
// Experimental utilities for NES emulator

// Nes_Emu 0.7.0

#ifndef NES_UTIL_H
#define NES_UTIL_H

#include "blargg_common.h"
class Nes_Emu;
class Nes_Cart;

class Joypad_Filter {
public:
	Joypad_Filter();
	
	// Control filtering of simultaneous directions. Enabled by default.
	void enable_filtering( bool = true );
	
	// Prevents simultaneous left+right and up+down to avoid problems in some games.
	// Also turns bits 8 and 9 into turbo A and B.
	int process( int joypad );
	
	// Set A and B turbo rates, where 1.0 is maximum and 0.0 disables them
	void set_a_rate( double r ) { rates [0] = (int) (r * 0x100); }
	void set_b_rate( double r ) { rates [1] = (int) (r * 0x100); }
	
	// Call after each emulated frame for which Nes_Emu::frame().joypad_read_count
	// is non-zero.
	void clock_turbo();
	
private:
	int prev;
	int mask;
	int times [2];
	int rates [2];
};

struct game_genie_patch_t
{
	unsigned addr;    // always 0x8000 or greater
	int change_to;
	int compare_with; // if -1, always change byte
	
	// Decode Game Genie code
	blargg_err_t decode( const char* in );
	
	// Apply patch to cartridge data. Might not work for some codes, since this really
	// requires emulator support. Returns number of bytes changed, where 0
	// means patch wasn't for that cartridge.
	int apply( Nes_Cart& ) const;
};

class Cheat_Value_Finder {
public:
	Cheat_Value_Finder();
	
	// Start scanning emulator's memory for values that are constantly changing.
	void start( Nes_Emu* );
	
	// Rescan memory and eliminate any changed bytes from later matching.
	// Should be called many times after begin_scan() and before begin_matching().
	void rescan();
	
	// Start search for any bytes which changed by difference between original and
	// changed values.
	void search( int original, int changed );
	
	// Get next match and return its delta from changed value (closer to 0
	// is more likely to be a match), or no_match if there are no more matches.
	// Optionally returns address of matched byte.
	enum { no_match = 0x100 };
	int next_match( int* addr = NULL );
	
	// Change current match to new value. Returns previous value.
	int change_value( int new_value );
	
private:
	typedef BOOST::uint8_t byte;
	Nes_Emu* emu;
	int original_value;
	int changed_value;
	int pos;
	enum { low_mem_size = 0x800 };
	byte original [low_mem_size];
	byte changed  [low_mem_size];
};

#endif

