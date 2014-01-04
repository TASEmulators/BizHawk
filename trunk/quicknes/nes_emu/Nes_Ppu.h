
// NES PPU emulator

// Nes_Emu 0.7.0

#ifndef NES_PPU_H
#define NES_PPU_H

#include "Nes_Ppu_Rendering.h"
class Nes_Mapper;
class Nes_Core;

typedef long nes_time_t;
typedef long ppu_time_t; // ppu_time_t = nes_time_t * ppu_overclock

ppu_time_t const ppu_overclock = 3; // PPU clocks for each CPU clock

class Nes_Ppu : public Nes_Ppu_Rendering {
	typedef Nes_Ppu_Rendering base;
public:
	Nes_Ppu( Nes_Core* );
	
	// Begin PPU frame and return beginning CPU timestamp
	nes_time_t begin_frame( ppu_time_t );
	
	nes_time_t nmi_time() { return nmi_time_; }
	void acknowledge_nmi() { nmi_time_ = LONG_MAX / 2 + 1; }
	
	int read_2002( nes_time_t );
	int read( unsigned addr, nes_time_t );
	void write( nes_time_t, unsigned addr, int );
	
	void render_bg_until( nes_time_t );
	void render_until( nes_time_t );
	
	// CPU time that frame will have ended by
	int frame_length() const { return frame_length_; }
	
	// End frame rendering and return PPU timestamp for next frame
	ppu_time_t end_frame( nes_time_t );
	
	// Do direct memory copy to sprite RAM
	void dma_sprites( nes_time_t, void const* in );
	
	int burst_phase;
	
private:
	
	Nes_Core& emu;
	
	enum { indefinite_time = LONG_MAX / 2 + 1 };
	
	void suspend_rendering();
	int read_( unsigned addr, nes_time_t ); // note swapped arguments!
	
	// NES<->PPU time conversion
	int extra_clocks;
	ppu_time_t ppu_time( nes_time_t t ) const { return t * ppu_overclock + extra_clocks; }
	nes_time_t nes_time( ppu_time_t t ) const { return (t - extra_clocks) / ppu_overclock; }
	
	// frame
	nes_time_t nmi_time_;
	int end_vbl_mask;
	int frame_length_;
	int frame_length_extra;
	bool frame_ended;
	void end_vblank();
	void run_end_frame( nes_time_t );
	
	// bg rendering
	nes_time_t next_bg_time;
	ppu_time_t scanline_time;
	ppu_time_t hblank_time;
	int scanline_count;
	int frame_phase;
	void render_bg_until_( nes_time_t );
	void run_scanlines( int count );
	
	// sprite rendering
	ppu_time_t next_sprites_time;
	int next_sprites_scanline;
	void render_until_( nes_time_t );
	
	// $2002 status register
	nes_time_t next_status_event;
	void query_until( nes_time_t );
	
	// sprite hit
	nes_time_t next_sprite_hit_check;
	void update_sprite_hit( nes_time_t );

	// open bus decay
	void update_open_bus( nes_time_t );
	void poke_open_bus( nes_time_t, int, int mask );
	const nes_time_t earliest_open_bus_decay() const;

	// sprite max
	nes_time_t next_sprite_max_run; // doesn't need to run until this time
	nes_time_t sprite_max_set_time; // if 0, needs to be recalculated
	int next_sprite_max_scanline;
	void run_sprite_max_( nes_time_t );
	void run_sprite_max( nes_time_t );
	void invalidate_sprite_max_();
	void invalidate_sprite_max( nes_time_t );
	
	friend int nes_cpu_read_likely_ppu( class Nes_Core*, unsigned, nes_time_t );
};

inline void Nes_Ppu::suspend_rendering()
{
	next_bg_time      = indefinite_time;
	next_sprites_time = indefinite_time;
	extra_clocks = 0;
}

inline Nes_Ppu::Nes_Ppu( Nes_Core* e ) : emu( *e )
{
	burst_phase = 0;
	suspend_rendering();
}

inline void Nes_Ppu::render_until( nes_time_t t )
{
	if ( t > next_sprites_time )
		render_until_( t );
}

inline void Nes_Ppu::render_bg_until( nes_time_t t )
{
	if ( t > next_bg_time )
		render_bg_until_( t );
}

inline void Nes_Ppu::update_open_bus( nes_time_t time )
{
	if ( time >= decay_low ) open_bus &= ~0x1F;
	if ( time >= decay_high ) open_bus &= ~0xE0;
}

#endif

