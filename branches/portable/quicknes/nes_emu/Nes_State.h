
// NES state snapshot for saving and restoring emulator state

// Nes_Emu 0.7.0

#ifndef NES_STATE_H
#define NES_STATE_H

#include "Nes_File.h"
#include "Nes_Cpu.h"
class Nes_Emu;
class Nes_State;

typedef long frame_count_t;

// Writes state to a file
class Nes_State_Writer : public Nes_File_Writer {
public:
	// Begin writing file
	blargg_err_t begin( Auto_File_Writer );
	
	// Write emulator's current state to file and end
	blargg_err_t end( Nes_Emu const& );
	
	// Write state to file and end
	blargg_err_t end( Nes_State const& );
};

// Reads state from a file
class Nes_State_Reader : public Nes_File_Reader {
public:
	
	// Begin reading state snapshot from file
	blargg_err_t begin( Auto_File_Reader, Nes_State* = 0 );
	
	// Go to next unrecognized block in file
	blargg_err_t next_block();
	
	// State as read from file. Only valid after all blocks have been read.
	Nes_State const& state() const;
	
public:
	Nes_State_Reader();
	~Nes_State_Reader();
private:
	Nes_State* owned;
	Nes_State* state_;
};

class Nes_State_ {
public:
	
	blargg_err_t write_blocks( Nes_File_Writer& ) const;
	void set_nes_state( nes_state_t const& );
	blargg_err_t read_blocks( Nes_File_Reader& );
	
	enum { ram_size = 0x800 };
	enum { sram_max = 0x2000 };
	enum { spr_ram_size = 0x100 };
	enum { nametable_max = 0x800 };
	enum { chr_max = 0x2000 };
	BOOST::uint8_t *ram, *sram, *spr_ram, *nametable, *chr;
	nes_state_t             nes;
	Nes_Cpu::registers_t*   cpu;
	joypad_state_t*         joypad;
	apu_state_t*            apu;
	ppu_state_t*            ppu;
	mapper_state_t*         mapper;
	
	bool nes_valid, cpu_valid, joypad_valid, apu_valid, ppu_valid;
	bool mapper_valid, ram_valid, spr_ram_valid;
	short sram_size, nametable_size, chr_size;
	
	// Invalidate all state
	void clear();
	
	// Change timestamp
	void set_timestamp( frame_count_t );
	
	// Timestamp snapshot was taken at
	frame_count_t timestamp() const;
};

// Snapshot of emulator state
class Nes_State : private Nes_State_ {
public:
	
	Nes_State();
	
#if 0 // What is this?
	Nes_State_::set_timestamp;
	Nes_State_::timestamp;
	Nes_State_::clear;
#endif
	
	// Write snapshot to file
	blargg_err_t write( Auto_File_Writer ) const;
	
	// Read snapshot from file
	blargg_err_t read( Auto_File_Reader );
	
private:
	Nes_Cpu::registers_t    cpu;
	joypad_state_t          joypad;
	apu_state_t             apu;
	ppu_state_t             ppu;
	mapper_state_t          mapper;
	BOOST::uint8_t ram [ram_size];
	BOOST::uint8_t sram [sram_max];
	BOOST::uint8_t spr_ram [spr_ram_size];
	BOOST::uint8_t nametable [nametable_max];
	BOOST::uint8_t chr [chr_max];
	
	friend class Nes_Emu;
	friend class Nes_State_Writer;
	friend class Nes_State_Reader;
	friend class Nes_Recorder;
public:
	blargg_err_t read_sta_file( Auto_File_Reader );
};

frame_count_t const invalid_frame_count = LONG_MAX / 2 + 1; // a large positive value

int mem_differs( void const* in, int compare, unsigned long count );

inline Nes_State const& Nes_State_Reader::state() const
{
	assert( depth() == 0 && block_type() == group_end );
	return *state_;
}

inline blargg_err_t Nes_State_Writer::begin( Auto_File_Writer dw )
{
	return Nes_File_Writer::begin( dw, state_file_tag );
}

inline void Nes_State_::set_timestamp( frame_count_t t ) { nes.frame_count = t; }

inline frame_count_t Nes_State_::timestamp() const { return nes.frame_count; }

#endif

