
// NES ROM data loaded into memory

// Nes_Emu 0.5.6. Copyright (C) 2004-2005 Shay Green. GNU LGPL license.

#ifndef NES_ROM_H
#define NES_ROM_H

#include "blargg_common.h"
#include "abstract_file.h"

class Nes_Rom {
	typedef BOOST::uint8_t byte;
public:
	Nes_Rom();
	~Nes_Rom();
	
	// Load iNES ROM file
	blargg_err_t load_ines_rom( Data_Reader& );
	
	// Load iNES ROM file and apply IPS patch
	blargg_err_t load_patched_ines_rom( Data_Reader&, Data_Reader& ips_patch );
	
	// to do: support UNIF?
	
	// True if a ROM is currently loaded
	bool loaded() const { return prg_ != NULL; }
	
	// Free any loaded ROM data
	void reset();
	
	// True if ROM claims to have battery-backed memory
	bool has_battery_ram() const;
	
	// Size of PRG data
	long prg_size() const { return prg_size_; }
	
	// Size of CHR data
	long chr_size() const { return chr_size_; }
	
	// Change size of PRG (code) data
	blargg_err_t resize_prg( long );
	
	// Change size of CHR (graphics) data 
	blargg_err_t resize_chr( long );
	
	// Set mapper and information bytes. LSB and MSB are the standard iNES header
	// bytes at offsets 6 and 7.
	void set_mapper( int mapper_lsb, int mapper_msb );
	
	// Initial mirroring setup
	int mirroring() const { return mapper & 0x09; }
	
	// iNES mapper code
	int mapper_code() const;
	
	// Pointer to beginning of PRG data
	byte      * prg()       { return prg_; }
	byte const* prg() const { return prg_; }
	
	// Pointer to beginning of CHR data
	byte      * chr()       { return chr_; }
	byte const* chr() const { return chr_; }
	
	// End of public interface
private:
	enum { bank_size = 8 * 1024L }; // bank sizes must be a multiple of this
	byte* prg_;
	byte* chr_;
	long prg_size_;
	long chr_size_;
	unsigned mapper;
	long round_to_bank_size( long n );
};

inline bool Nes_Rom::has_battery_ram() const { return mapper & 0x02; }

inline void Nes_Rom::set_mapper( int mapper_lsb, int mapper_msb )
{
	mapper = mapper_msb * 0x100 + mapper_lsb;
}

inline int Nes_Rom::mapper_code() const { return ((mapper >> 8) & 0xf0) | ((mapper >> 4) & 0x0f); }

#endif

