
// NES 6502 CPU emulator

// Nes_Emu 0.7.0

#ifndef NES_CPU_H
#define NES_CPU_H

#include "blargg_common.h"

typedef long     nes_time_t; // clock cycle count
typedef unsigned nes_addr_t; // 16-bit address

class Nes_Cpu {
public:
	typedef BOOST::uint8_t uint8_t;
	
	// Clear registers, unmap memory, and map code pages to unmapped_page.
	void reset( void const* unmapped_page = 0 );
	
	// Map code memory (memory accessed via the program counter). Start and size
	// must be multiple of page_size.
	enum { page_bits = 11 };
	enum { page_count = 0x10000 >> page_bits };
	enum { page_size = 1L << page_bits };
	void map_code( nes_addr_t start, unsigned size, void const* code );
	
	// Access memory as the emulated CPU does.
	int  read( nes_addr_t );
	void write( nes_addr_t, int data );
	uint8_t* get_code( nes_addr_t ); // non-const to allow debugger to modify code
	
	// Push a byte on the stack
	void push_byte( int );
	
	// NES 6502 registers. *Not* kept updated during a call to run().
	struct registers_t {
		long pc; // more than 16 bits to allow overflow detection
		BOOST::uint8_t a;
		BOOST::uint8_t x;
		BOOST::uint8_t y;
		BOOST::uint8_t status;
		BOOST::uint8_t sp;
	};
	//registers_t r;
	
	// Reasons that run() returns
	enum result_t {
		result_cycles,  // Requested number of cycles (or more) were executed
		result_sei,     // I flag just set and IRQ time would generate IRQ now
		result_cli,     // I flag just cleared but IRQ should occur *after* next instr
		result_badop    // unimplemented/illegal instruction
	};
	
	result_t run( nes_time_t end_time );
	
	nes_time_t time() const             { return clock_count; }
	void reduce_limit( int offset );
	void set_end_time_( nes_time_t t );
	void set_irq_time_( nes_time_t t );
	unsigned long error_count() const   { return error_count_; }
	
	// If PC exceeds 0xFFFF and encounters page_wrap_opcode, it will be silently wrapped.
	enum { page_wrap_opcode = 0xF2 };
	
	// One of the many opcodes that are undefined and stop CPU emulation.
	enum { bad_opcode = 0xD2 };

	void set_tracecb(void (*cb)(unsigned int *dest));
	
private:
	uint8_t const* code_map [page_count + 1];
	nes_time_t clock_limit;
	nes_time_t clock_count;
	nes_time_t irq_time_;
	nes_time_t end_time_;
	unsigned long error_count_;
	
	enum { irq_inhibit = 0x04 };
	void set_code_page( int, uint8_t const* );
	void update_clock_limit();

	void (*tracecb)(unsigned int *dest);
	
public:
	registers_t r;
	
	// low_mem is a full page size so it can be mapped with code_map
	uint8_t low_mem [page_size > 0x800 ? page_size : 0x800];
};

inline BOOST::uint8_t* Nes_Cpu::get_code( nes_addr_t addr )
{
	return (uint8_t*) code_map [addr >> page_bits] + addr;
}
	
inline void Nes_Cpu::update_clock_limit()
{
	nes_time_t t = end_time_;
	if ( t > irq_time_ && !(r.status & irq_inhibit) )
		t = irq_time_;
	clock_limit = t;
}

inline void Nes_Cpu::set_end_time_( nes_time_t t )
{
	end_time_ = t;
	update_clock_limit();
}

inline void Nes_Cpu::set_irq_time_( nes_time_t t )
{
	irq_time_ = t;
	update_clock_limit();
}

inline void Nes_Cpu::reduce_limit( int offset )
{
	clock_limit -= offset;
	end_time_   -= offset;
	irq_time_   -= offset;
}

inline void Nes_Cpu::push_byte( int data )
{
	int sp = r.sp;
	r.sp = (sp - 1) & 0xFF;
	low_mem [0x100 + sp] = data;
}

#endif

