
// Film data manager that keeps data compressed in memory

// Nes_Emu 0.7.0

#ifndef NES_FILM_DATA_H
#define NES_FILM_DATA_H

#include "Nes_State.h"
#include "Nes_Film_Packer.h"

class Nes_Film_Data {
public:
	Nes_Film_Data();
	~Nes_Film_Data();
	
	void clear( frame_count_t period );
	frame_count_t period() const { return period_; }
	blargg_err_t resize( int new_count );
	void trim( int begin, int end );
	
	enum { block_size = 8 }; // 16 helps compression but doubles temp buffer size
	typedef int index_t;
	struct block_t
	{
		BOOST::uint8_t* joypads [2];
		
		Nes_State_ states [block_size];
		
		Nes_Cpu::registers_t    cpu [block_size];
		joypad_state_t          joypad [block_size];
		apu_state_t             apu [block_size];
		ppu_state_t             ppu [block_size];
		mapper_state_t          mapper [block_size];
		BOOST::uint8_t spr_ram [block_size] [Nes_State_::spr_ram_size];
		BOOST::uint8_t ram [block_size] [Nes_State_::ram_size];
		BOOST::uint8_t nametable [block_size] [Nes_State_::nametable_max];
		BOOST::uint32_t garbage0;
		BOOST::uint8_t chr [block_size] [Nes_State_::chr_max];
		BOOST::uint32_t garbage1;
		BOOST::uint8_t sram [block_size] [Nes_State_::sram_max];
		BOOST::uint32_t garbage2;
		// garbage values prevent matches in compressor from being longer than 256K
		BOOST::uint8_t joypad0 [60 * 60L * 60L];
	};
	block_t const& read( index_t ) const;
	block_t* write( index_t ); // NULL if out of memory
	block_t* alloc_joypad2( index_t i ) { return write( i ); }
	void joypad_only( bool );
	
private:
	struct comp_block_t
	{
		long size;
		long offset;
		BOOST::uint8_t data [1024 * 1024L];
	};
	comp_block_t** blocks;
	int block_count;
	frame_count_t period_;
	block_t* active;
	mutable int active_index;
	mutable bool active_dirty;
	long joypad_only_;
	Nes_Film_Packer* packer;
	
	void debug_packer() const;
	void flush_active() const;
	void init_active() const;
	void init_states() const;
	void invalidate_active();
	void access( index_t ) const;
	// must be multiple of 4 for packer
	long active_size() const { return (offsetof (block_t,joypad0) + 3) & ~3; }
};

inline Nes_Film_Data::block_t const& Nes_Film_Data::read( int i ) const
{
	//assert( blocks [i] ); // catch reads of uninitialized blocks
	if ( i != active_index )
		access( i );
	return *active;
}

#endif

