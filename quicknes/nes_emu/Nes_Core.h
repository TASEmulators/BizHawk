
// Internal NES emulator

// Nes_Emu 0.7.0

#ifndef NES_CORE_H
#define NES_CORE_H

#include "blargg_common.h"
#include "Nes_Apu.h"
#include "Nes_Cpu.h"
#include "Nes_Ppu.h"
class Nes_Mapper;
class Nes_Cart;
class Nes_State;

class Nes_Core : private Nes_Cpu {
	typedef Nes_Cpu cpu;
public:
	Nes_Core();
	~Nes_Core();
	
	blargg_err_t init();
	blargg_err_t open( Nes_Cart const* );
	void reset( bool full_reset = true, bool erase_battery_ram = false );
	blip_time_t emulate_frame();
	void close();
	
	void save_state( Nes_State* ) const;
	void save_state( Nes_State_* ) const;
	void load_state( Nes_State_ const& );
	
	void irq_changed();
	void event_changed();
	
public: private: friend class Nes_Emu;
	
	struct impl_t
	{
		enum { sram_size = 0x2000 };
		BOOST::uint8_t sram [sram_size];
		Nes_Apu apu;
		
		// extra byte allows CPU to always read operand of instruction, which
		// might go past end of data
		BOOST::uint8_t unmapped_page [::Nes_Cpu::page_size + 1];
	};
	impl_t* impl; // keep large arrays separate
	unsigned long error_count;
	bool sram_present;

public:
	uint32_t current_joypad [2];
	int joypad_read_count;
	Nes_Cart const* cart;
	Nes_Mapper* mapper;
	nes_state_t nes;
	Nes_Ppu ppu;

private:
	// noncopyable
	Nes_Core( const Nes_Core& );
	Nes_Core& operator = ( const Nes_Core& );
	
	// Timing
	nes_time_t ppu_2002_time;
	void disable_rendering() { clock_ = 0; }
	nes_time_t earliest_irq( nes_time_t present );
	nes_time_t ppu_frame_length( nes_time_t present );
	nes_time_t earliest_event( nes_time_t present );
	
	// APU and Joypad
	joypad_state_t joypad;
	int  read_io( nes_addr_t );
	void write_io( nes_addr_t, int data );
	static int  read_dmc( void* emu, nes_addr_t );
	static void apu_irq_changed( void* emu );
	
	// CPU
	unsigned sram_readable;
	unsigned sram_writable;
	unsigned lrom_readable;
	nes_time_t clock_;
	nes_time_t cpu_time_offset;
	nes_time_t emulate_frame_();
	nes_addr_t read_vector( nes_addr_t );
	void vector_interrupt( nes_addr_t );
	static void log_unmapped( nes_addr_t addr, int data = -1 );
	void cpu_set_irq_time( nes_time_t t ) { cpu::set_irq_time_( t - 1 - cpu_time_offset ); }
	void cpu_set_end_time( nes_time_t t ) { cpu::set_end_time_( t - 1 - cpu_time_offset ); }
	nes_time_t cpu_time() const { return clock_ + 1; }
	void cpu_adjust_time( int offset );
	
public: private: friend class Nes_Ppu;
	void set_ppu_2002_time( nes_time_t t ) { ppu_2002_time = t - 1 - cpu_time_offset; }
	
public: private: friend class Nes_Mapper;
	void enable_prg_6000();
	void enable_sram( bool enabled, bool read_only = false );
	nes_time_t clock() const { return clock_; }
	void add_mapper_intercept( nes_addr_t start, unsigned size, bool read, bool write );
	
public: private: friend class Nes_Cpu;
	int  cpu_read_ppu( nes_addr_t, nes_time_t );
	int  cpu_read( nes_addr_t, nes_time_t );
	void cpu_write( nes_addr_t, int data, nes_time_t );
	void cpu_write_2007( int data );
	
private:
	unsigned char data_reader_mapped [page_count + 1]; // extra entry for overflow
	unsigned char data_writer_mapped [page_count + 1];
};

int mem_differs( void const* p, int cmp, unsigned long s );

#endif

