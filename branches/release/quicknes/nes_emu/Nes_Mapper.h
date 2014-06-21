
// NES mapper interface

// Nes_Emu 0.7.0

#ifndef NES_MAPPER
#define NES_MAPPER

#include "Nes_Cart.h"
#include "Nes_Cpu.h"
#include "nes_data.h"
#include "Nes_Core.h"
class Blip_Buffer;
class blip_eq_t;
class Nes_Core;

class Nes_Mapper {
public:
	// Register function that creates mapper for given code.
	typedef Nes_Mapper* (*creator_func_t)();
	static void register_mapper( int code, creator_func_t );
	
	// Register optional mappers included with Nes_Emu
	void register_optional_mappers();
	
	// Create mapper appropriate for cartridge. Returns NULL if it uses unsupported mapper.
	static Nes_Mapper* create( Nes_Cart const*, Nes_Core* );
	
	virtual ~Nes_Mapper();
	
	// Reset mapper to power-up state.
	virtual void reset();
	
	// Save snapshot of mapper state. Default saves registered state.
	virtual void save_state( mapper_state_t& );
	
	// Resets mapper, loads state, then applies it
	virtual void load_state( mapper_state_t const& );

// I/O

	// Read from memory
	virtual int read( nes_time_t, nes_addr_t );
	
	// Write to memory
	virtual void write( nes_time_t, nes_addr_t, int data ) = 0;
	
	// Write to memory below 0x8000 (returns false if mapper didn't handle write)
	virtual bool write_intercepted( nes_time_t, nes_addr_t, int data );
	
// Timing
	
	// Time returned when current mapper state won't ever cause an IRQ
	enum { no_irq = LONG_MAX / 2 };
	
	// Time next IRQ will occur at
	virtual nes_time_t next_irq( nes_time_t present );
	
	// Run mapper until given time
	virtual void run_until( nes_time_t );
	
	// End video frame of given length
	virtual void end_frame( nes_time_t length );
	
// Sound
	
	// Number of sound channels
	virtual int channel_count() const;
	
	// Set sound buffer for channel to output to, or NULL to silence channel.
	virtual void set_channel_buf( int index, Blip_Buffer* );
	
	// Set treble equalization
	virtual void set_treble( blip_eq_t const& );
	
// Misc
	
	// Called when bit 12 of PPU's VRAM address changes from 0 to 1 due to
	// $2006 and $2007 accesses (but not due to PPU scanline rendering).
	virtual void a12_clocked();
	
protected:
	// Services provided for derived mapper classes
	Nes_Mapper();
	
	// Register state data to automatically save and load. Be sure the binary
	// layout is suitable for use in a file, including any byte-order issues.
	// Automatically cleared to zero by default reset().
	void register_state( void*, unsigned );
	
	// Enable 8K of RAM at 0x6000-0x7FFF, optionally read-only.
	void enable_sram( bool enabled = true, bool read_only = false );
	
	// Cause CPU writes within given address range to call mapper's write() function.
	// Might map a larger address range, which the mapper can ignore and pass to
	// Nes_Mapper::write(). The range 0x8000-0xffff is always intercepted by the mapper.
	void intercept_writes( nes_addr_t addr, unsigned size );
	
	// Cause CPU reads within given address range to call mapper's read() function.
	// Might map a larger address range, which the mapper can ignore and pass to
	// Nes_Mapper::read(). CPU opcode/operand reads and low-memory reads always
	// go directly to memory and cannot be intercepted.
	void intercept_reads( nes_addr_t addr, unsigned size );
	
	// Bank sizes for mapping
	enum bank_size_t { // 1 << bank_Xk = X * 1024
		bank_1k  = 10,
		bank_2k  = 11,
		bank_4k  = 12,
		bank_8k  = 13,
		bank_16k = 14,
		bank_32k = 15
	};
	
	// Index of last PRG/CHR bank. Last_bank selects last bank, last_bank - 1
	// selects next-to-last bank, etc.
	enum { last_bank = -1 };
	
	// Map 'size' bytes from 'PRG + bank * size' to CPU address space starting at 'addr'
	void set_prg_bank( nes_addr_t addr, bank_size_t size, int bank );
	
	// Map 'size' bytes from 'CHR + bank * size' to PPU address space starting at 'addr'
	void set_chr_bank( nes_addr_t addr, bank_size_t size, int bank );
	void set_chr_bank_ex( nes_addr_t addr, bank_size_t size, int bank ); // mmc24 only
	
	// Set PPU mirroring. All mappings implemented using mirror_manual().
	void mirror_manual( int page0, int page1, int page2, int page3 );
	void mirror_single( int page );
	void mirror_horiz( int page = 0 );
	void mirror_vert( int page = 0 );
	void mirror_full();
	
	// True if PPU rendering is enabled. Some mappers watch PPU memory accesses to determine
	// when scanlines occur, and can only do this when rendering is enabled.
	bool ppu_enabled() const;
	
	// Cartridge being emulated
	Nes_Cart const& cart() const { return *cart_; }
	
	// Must be called when next_irq()'s return value is earlier than previous,
	// current CPU run can be stopped earlier. Best to call whenever time may
	// have changed (no performance impact if called even when time didn't change).
	void irq_changed();
	
	// Handle data written to mapper that doesn't handle bus conflict arising due to
	// PRG also reading data. Returns data that mapper should act as if were
	// written. Currently always returns 'data' and just checks that data written is
	// the same as byte in PRG at same address and writes debug message if it doesn't.
	int handle_bus_conflict( nes_addr_t addr, int data );
	
	// Reference to emulator that uses this mapper.
	Nes_Core& emu() const { return *emu_; }
	
protected:
	// Services derived classes provide
	
	// Read state from snapshot. Default reads data into registered state, then calls
	// apply_mapping().
	virtual void read_state( mapper_state_t const& );
	
	// Apply current mapping state to hardware. Called after reading mapper state
	// from a snapshot.
	virtual void apply_mapping() = 0;
	
	// Called by default reset() before apply_mapping() is called.
	virtual void reset_state() { }
	
	// End of general interface
private:
	Nes_Core* emu_;
	void* state;
	unsigned state_size;
	Nes_Cart const* cart_;
	
	void default_reset_state();
	
	struct mapping_t {
		int code;
		Nes_Mapper::creator_func_t func;
	};
	static mapping_t mappers [];
	static creator_func_t get_mapper_creator( int code );
	
	// built-in mappers
	static Nes_Mapper* make_nrom();
	static Nes_Mapper* make_unrom();
	static Nes_Mapper* make_aorom();
	static Nes_Mapper* make_cnrom();
	static Nes_Mapper* make_mmc1();
	static Nes_Mapper* make_mmc3();
};

template<class T>
struct register_mapper {
	/*void*/ register_mapper( int code ) { Nes_Mapper::register_mapper( code, create ); }
	static Nes_Mapper* create() { return BLARGG_NEW T; }
};

#ifdef NDEBUG
inline int Nes_Mapper::handle_bus_conflict( nes_addr_t addr, int data ) { return data; }
#endif

inline void Nes_Mapper::mirror_horiz(  int p ) { mirror_manual( p, p, p ^ 1, p ^ 1 ); }
inline void Nes_Mapper::mirror_vert(   int p ) { mirror_manual( p, p ^ 1, p, p ^ 1 ); }
inline void Nes_Mapper::mirror_single( int p ) { mirror_manual( p, p, p, p ); }
inline void Nes_Mapper::mirror_full()          { mirror_manual( 0, 1, 2, 3 ); }

inline void Nes_Mapper::register_state( void* p, unsigned s )
{
	assert( s <= max_mapper_state_size );
	state = p;
	state_size = s;
}

inline bool Nes_Mapper::write_intercepted( nes_time_t, nes_addr_t, int ) { return false; }

inline int Nes_Mapper::read( nes_time_t, nes_addr_t ) { return -1; } // signal to caller

inline void Nes_Mapper::intercept_reads( nes_addr_t addr, unsigned size )
{
	emu().add_mapper_intercept( addr, size, true, false );
}

inline void Nes_Mapper::intercept_writes( nes_addr_t addr, unsigned size )
{
	emu().add_mapper_intercept( addr, size, false, true );
}

inline void Nes_Mapper::enable_sram( bool enabled, bool read_only )
{
	emu_->enable_sram( enabled, read_only );
}

#endif

