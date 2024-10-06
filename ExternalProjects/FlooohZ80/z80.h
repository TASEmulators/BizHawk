#pragma once
/*#
	# z80.h

	A cycle-stepped Z80 emulator in a C header.

	Do this:
	~~~~C
	#define CHIPS_IMPL
	~~~~
	before you include this file in *one* C or C++ file to create the
	implementation.

	Optionally provide
	~~~C
	#define CHIPS_ASSERT(x) your_own_asset_macro(x)
	~~~

	## Emulated Pins
	***********************************
	*           +-----------+         *
	* M1    <---|           |---> A0  *
	* MREQ  <---|           |---> A1  *
	* IORQ  <---|           |---> A2  *
	* RD    <---|           |---> ..  *
	* WR    <---|    Z80    |---> A15 *
	* HALT  <---|           |         *
	* WAIT  --->|           |<--> D0  *
	* INT   --->|           |<--> D1  *
	* NMI   --->|           |<--> ... *
	* RFSH  <---|           |<--> D7  *
	*           +-----------+         *
	***********************************

	## Functions

	~~~C
	uint64_t z80_init(z80_t* cpu);
	~~~
		Initializes a new z80_t instance, returns initial pin mask to start
		execution at address 0.

	~~~C
	uint64_t z80_reset(z80_t* cpu)
	~~~
		Resets a z80_t instance, returns pin mask to start execution at
		address 0.

	~~~C
	uint64_t z80_tick(z80_t* cpu, uint64_t pins)
	~~~
		Step the z80_t instance for one clock cycle.

	~~~C
	uint64_t z80_prefetch(z80_t* cpu, uint16_t new_pc)
	~~~
		Call this function to force execution to start at a specific
		PC. Use the returned pin mask as argument into the next z80_tick() call.

	~~~C
	bool z80_opdone(z80_t* cpu)
	~~~
		Helper function to detect whether the z80_t instance has completed
		an instruction.

	## HOWTO

	Initialize a new z80_t instance and start ticking it:
	~~~C
		z80_t cpu;
		uint64_t pins = z80_init(&cpu);
		while (!done) {
			pins = z80_tick(&cpu, pins);
		}
	~~~
	Since there is no memory attached yet, the CPU will simply run whatever opcode
	bytes are present on the data bus (in this case the data bus is zero, so the CPU
	just runs throught the same NOP over and over).

	Next, add some memory and inspect and modify the pin mask to handle memory accesses:
	~~~C
		uint8_t mem[(1<<16)] = {0};
		z80_t cpu;
		uint64_t pins = z80_init(&cpu);
		while (!done) {
			pins = z80_tick(&cpu, pins);
			if (pins & Z80_MREQ) {
				const uint16_t addr = Z80_GET_ADDR(pins);
				if (pins & Z80_RD) {
					uint8_t data = mem[addr];
					Z80_SET_DATA(pins, data);
				}
				else if (pins & Z80_WR) {
					uint8_t data = Z80_GET_DATA(pins);
					mem[addr] = data;
				}
			}
		}
	~~~
	The CPU will now run through the whole address space executing NOPs (because the memory is
	filled with 0s instead of a valid program). If there would be a valid Z80 program at memory
	address 0, this would be executed instead.

	IO requests are handled the same as memory requests, but instead of the MREQ pin, the
	IORQ pin must be checked:
	~~~C
		uint8_t mem[(1<<16)] = {0};
		z80_t cpu;
		uint64_t pins = z80_init(&cpu);
		while (!done) {
			pins = z80_tick(&cpu, pins);
			if (pins & Z80_MREQ) {
				const uint16_t addr = Z80_GET_ADDR(pins);
				if (pins & Z80_RD) {
					uint8_t data = mem[addr];
					Z80_SET_DATA(pins, data);
				}
				else if (pins & Z80_WR) {
					uint8_t data = Z80_GET_DATA(pins);
					mem[addr] = data;
				}
			}
			else if (pins & Z80_IORQ) {
				const uint16_t port = Z80_GET_ADDR(pins);
				if (pins & Z80_RD) {
					// handle IO input request at port
					...
				}
				else if (pins & Z80_WR) {
					// handle IO output request at port
					...
				}
			}
		}
	~~~

	Handle interrupt acknowledge cycles by checking for Z80_IORQ|Z80_M1:
	~~~C
		uint8_t mem[(1<<16)] = {0};
		z80_t cpu;
		uint64_t pins = z80_init(&cpu);
		while (!done) {
			pins = z80_tick(&cpu, pins);
			if (pins & Z80_MREQ) {
				const uint16_t addr = Z80_GET_ADDR(pins);
				if (pins & Z80_RD) {
					uint8_t data = mem[addr];
					Z80_SET_DATA(pins, data);
				}
				else if (pins & Z80_WR) {
					uint8_t data = Z80_GET_DATA(pins);
					mem[addr] = data;
				}
			}
			else if (pins & Z80_IORQ) {
				const uint16_t addr = Z80_GET_ADDR(pins);
				if (pins & Z80_M1) {
					// an interrupt acknowledge cycle, depending on the emulated system,
					// put either an instruction byte, or an interrupt vector on the data bus
					Z80_SET_DATA(pins, opcode_or_intvec);
				}
				else if (pins & Z80_RD) {
					// handle IO input request at port `addr`
					...
				}
				else if (pins & Z80_WR) {
					// handle IO output request at port `addr`
					...
				}
			}
		}
	~~~

	To request an interrupt, or inject a wait state just set the respective pin
	(Z80_INT, Z80_NMI, Z80_WAIT), don't forget to clear the pin again later (the
	details on when those pins are set and cleared depend heavily on the
	emulated system).

	!!! note
		NOTE: The Z80_RES pin is currently not emulated. Instead call the `z80_reset()` function.

	To emulate a whole computer system, add the per-tick code for the rest of the system to the
	basic ticking code above.

	If the emulated system uses the Z80 daisychain interrupt protocol (for instance when using
	the Z80 family chips like the PIO or CTC), tick those chips in interrupt priority order and
	set the Z80_IEIO pin before the highest priority chip in the daisychain is ticked:

	~~~C
		...
		while (!done) {
			pins = z80_tick(&cpu, pins);
			...
			// tick Z80 family chips in 'daisychain order':
			pins |= Z80_IEIO;
			...
			pins = z80ctc_tick(&ctc, pins);
			...
			pins = z80pio_tick(&pio, pins);
			...
			// the Z80_INT pin will now be set if any of the chips wants to issue an interrupt request
		}
	~~~
#*/
/*
	zlib/libpng license

	Copyright (c) 2021 Andre Weissflog
	This software is provided 'as-is', without any express or implied warranty.
	In no event will the authors be held liable for any damages arising from the
	use of this software.
	Permission is granted to anyone to use this software for any purpose,
	including commercial applications, and to alter it and redistribute it
	freely, subject to the following restrictions:
		1. The origin of this software must not be misrepresented; you must not
		claim that you wrote the original software. If you use this software in a
		product, an acknowledgment in the product documentation would be
		appreciated but is not required.
		2. Altered source versions must be plainly marked as such, and must not
		be misrepresented as being the original software.
		3. This notice may not be removed or altered from any source
		distribution.
*/
#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

	// address pins
#define Z80_PIN_A0  (0)
#define Z80_PIN_A1  (1)
#define Z80_PIN_A2  (2)
#define Z80_PIN_A3  (3)
#define Z80_PIN_A4  (4)
#define Z80_PIN_A5  (5)
#define Z80_PIN_A6  (6)
#define Z80_PIN_A7  (7)
#define Z80_PIN_A8  (8)
#define Z80_PIN_A9  (9)
#define Z80_PIN_A10 (10)
#define Z80_PIN_A11 (11)
#define Z80_PIN_A12 (12)
#define Z80_PIN_A13 (13)
#define Z80_PIN_A14 (14)
#define Z80_PIN_A15 (15)

// data pins
#define Z80_PIN_D0  (16)
#define Z80_PIN_D1  (17)
#define Z80_PIN_D2  (18)
#define Z80_PIN_D3  (19)
#define Z80_PIN_D4  (20)
#define Z80_PIN_D5  (21)
#define Z80_PIN_D6  (22)
#define Z80_PIN_D7  (23)

// control pins
#define Z80_PIN_M1    (24)        // machine cycle 1
#define Z80_PIN_MREQ  (25)        // memory request
#define Z80_PIN_IORQ  (26)        // input/output request
#define Z80_PIN_RD    (27)        // read
#define Z80_PIN_WR    (28)        // write
#define Z80_PIN_HALT  (29)        // halt state
#define Z80_PIN_INT   (30)        // interrupt request
#define Z80_PIN_RES   (31)        // reset requested
#define Z80_PIN_NMI   (32)        // non-maskable interrupt
#define Z80_PIN_WAIT  (33)        // wait requested
#define Z80_PIN_RFSH  (34)        // refresh

// virtual pins (for interrupt daisy chain protocol)
#define Z80_PIN_IEIO  (37)      // unified daisy chain 'Interrupt Enable In+Out'
#define Z80_PIN_RETI  (38)      // cpu has decoded a RETI instruction

// pin bit masks
#define Z80_A0    (1ULL<<Z80_PIN_A0)
#define Z80_A1    (1ULL<<Z80_PIN_A1)
#define Z80_A2    (1ULL<<Z80_PIN_A2)
#define Z80_A3    (1ULL<<Z80_PIN_A3)
#define Z80_A4    (1ULL<<Z80_PIN_A4)
#define Z80_A5    (1ULL<<Z80_PIN_A5)
#define Z80_A6    (1ULL<<Z80_PIN_A6)
#define Z80_A7    (1ULL<<Z80_PIN_A7)
#define Z80_A8    (1ULL<<Z80_PIN_A8)
#define Z80_A9    (1ULL<<Z80_PIN_A9)
#define Z80_A10   (1ULL<<Z80_PIN_A10)
#define Z80_A11   (1ULL<<Z80_PIN_A11)
#define Z80_A12   (1ULL<<Z80_PIN_A12)
#define Z80_A13   (1ULL<<Z80_PIN_A13)
#define Z80_A14   (1ULL<<Z80_PIN_A14)
#define Z80_A15   (1ULL<<Z80_PIN_A15)
#define Z80_D0    (1ULL<<Z80_PIN_D0)
#define Z80_D1    (1ULL<<Z80_PIN_D1)
#define Z80_D2    (1ULL<<Z80_PIN_D2)
#define Z80_D3    (1ULL<<Z80_PIN_D3)
#define Z80_D4    (1ULL<<Z80_PIN_D4)
#define Z80_D5    (1ULL<<Z80_PIN_D5)
#define Z80_D6    (1ULL<<Z80_PIN_D6)
#define Z80_D7    (1ULL<<Z80_PIN_D7)
#define Z80_M1    (1ULL<<Z80_PIN_M1)
#define Z80_MREQ  (1ULL<<Z80_PIN_MREQ)
#define Z80_IORQ  (1ULL<<Z80_PIN_IORQ)
#define Z80_RD    (1ULL<<Z80_PIN_RD)
#define Z80_WR    (1ULL<<Z80_PIN_WR)
#define Z80_HALT  (1ULL<<Z80_PIN_HALT)
#define Z80_INT   (1ULL<<Z80_PIN_INT)
#define Z80_RES   (1ULL<<Z80_PIN_RES)
#define Z80_NMI   (1ULL<<Z80_PIN_NMI)
#define Z80_WAIT  (1ULL<<Z80_PIN_WAIT)
#define Z80_RFSH  (1ULL<<Z80_PIN_RFSH)
#define Z80_IEIO  (1ULL<<Z80_PIN_IEIO)
#define Z80_RETI  (1ULL<<Z80_PIN_RETI)

#define Z80_CTRL_PIN_MASK (Z80_M1|Z80_MREQ|Z80_IORQ|Z80_RD|Z80_WR|Z80_RFSH)
#define Z80_PIN_MASK ((1ULL<<40)-1)

// pin access helper macros
#define Z80_MAKE_PINS(ctrl, addr, data) ((ctrl)|((data&0xFF)<<16)|((addr)&0xFFFFULL))
#define Z80_GET_ADDR(p) ((uint16_t)(p))
#define Z80_SET_ADDR(p,a) {p=((p)&~0xFFFF)|((a)&0xFFFF);}
#define Z80_GET_DATA(p) ((uint8_t)((p)>>16))
#define Z80_SET_DATA(p,d) {p=((p)&~0xFF0000ULL)|(((d)<<16)&0xFF0000ULL);}

// status flags
#define Z80_CF (1<<0)           // carry
#define Z80_NF (1<<1)           // add/subtract
#define Z80_VF (1<<2)           // parity/overflow
#define Z80_PF Z80_VF
#define Z80_XF (1<<3)           // undocumented bit 3
#define Z80_HF (1<<4)           // half carry
#define Z80_YF (1<<5)           // undocumented bit 5
#define Z80_ZF (1<<6)           // zero
#define Z80_SF (1<<7)           // sign

// CPU state

#pragma pack(push, 1)
typedef struct {
	uint16_t step;      // the currently active decoder step
	uint16_t addr;      // effective address for (HL),(IX+d),(IY+d)
	uint8_t dlatch;     // temporary store for data bus value
	uint8_t opcode;     // current opcode
	uint8_t hlx_idx;    // index into hlx[] for mapping hl to ix or iy (0: hl, 1: ix, 2: iy)
	bool prefix_active; // true if any prefix currently active (only needed in z80_opdone())
	uint64_t pins;      // last pin state, used for NMI detection
	uint64_t int_bits;  // track INT and NMI state
	union {
		struct { uint8_t pcl; uint8_t pch; };
		uint16_t pc;
	};
	union {
		struct { uint8_t f; uint8_t a; };
		uint16_t af;
	};
	union {
		struct { uint8_t c; uint8_t b; };
		uint16_t bc;
	};
	union {
		struct { uint8_t e; uint8_t d; };
		uint16_t de;
	};
	union {
		struct {
			union { struct { uint8_t l; uint8_t h; }; uint16_t hl; };
			union { struct { uint8_t ixl; uint8_t ixh; }; uint16_t ix; };
			union { struct { uint8_t iyl; uint8_t iyh; }; uint16_t iy; };
		};
		struct { union { struct { uint8_t l; uint8_t h; }; uint16_t hl; }; } hlx[3];
	};
	union { struct { uint8_t wzl; uint8_t wzh; }; uint16_t wz; };
	union { struct { uint8_t spl; uint8_t sph; }; uint16_t sp; };
	union { struct { uint8_t r; uint8_t i; }; uint16_t ir; };
	uint16_t af2, bc2, de2, hl2; // shadow register bank
	uint8_t im;
	bool iff1, iff2;
} z80_t;
#pragma pack(pop)

/*
	typedef struct {
		uint16_t step;      // the currently active decoder step
		uint16_t addr;      // effective address for (HL),(IX+d),(IY+d)
		uint8_t dlatch;     // temporary store for data bus value
		uint8_t opcode;     // current opcode
		uint8_t hlx_idx;    // index into hlx[] for mapping hl to ix or iy (0: hl, 1: ix, 2: iy)
		bool prefix_active; // true if any prefix currently active (only needed in z80_opdone())
		uint64_t pins;      // last pin state, used for NMI detection
		uint64_t int_bits;  // track INT and NMI state
		union { struct { uint8_t pcl; uint8_t pch; }; uint16_t pc; };

		// NOTE: These unions are fine in C, but not C++.
		union { struct { uint8_t f; uint8_t a; }; uint16_t af; };
		union { struct { uint8_t c; uint8_t b; }; uint16_t bc; };
		union { struct { uint8_t e; uint8_t d; }; uint16_t de; };
		union {
			struct {
				union { struct { uint8_t l; uint8_t h; }; uint16_t hl; };
				union { struct { uint8_t ixl; uint8_t ixh; }; uint16_t ix; };
				union { struct { uint8_t iyl; uint8_t iyh; }; uint16_t iy; };
			};
			struct { union { struct { uint8_t l; uint8_t h; }; uint16_t hl; }; } hlx[3];
		};
		union { struct { uint8_t wzl; uint8_t wzh; }; uint16_t wz; };
		union { struct { uint8_t spl; uint8_t sph; }; uint16_t sp; };
		union { struct { uint8_t r; uint8_t i; }; uint16_t ir; };
		uint16_t af2, bc2, de2, hl2; // shadow register bank
		uint8_t im;
		bool iff1, iff2;
	} z80_t;
	*/
	// initialize a new Z80 instance and return initial pin mask
	uint64_t z80_init(z80_t* cpu);
	// immediately put Z80 into reset state
	uint64_t z80_reset(z80_t* cpu);
	// execute one tick, return new pin mask
	uint64_t z80_tick(z80_t* cpu, uint64_t pins);
	// force execution to continue at address 'new_pc'
	uint64_t z80_prefetch(z80_t* cpu, uint16_t new_pc);
	// return true when full instruction has finished
	bool z80_opdone(z80_t* cpu);

	

#ifdef __cplusplus
} // extern C
#endif

//-- IMPLEMENTATION ------------------------------------------------------------
#ifdef CHIPS_IMPL
#include <string.h> // memset
#ifndef CHIPS_ASSERT
#include <assert.h>
#define CHIPS_ASSERT(c) assert(c)
#endif

#if defined(__GNUC__)
#define _Z80_UNREACHABLE __builtin_unreachable()
#elif defined(_MSC_VER)
#define _Z80_UNREACHABLE __assume(0)
#else
#define _Z80_UNREACHABLE
#endif

// values for hlx_idx for mapping HL, IX or IY, used as index into hlx[]
#define _Z80_MAP_HL (0)
#define _Z80_MAP_IX (1)
#define _Z80_MAP_IY (2)

uint64_t z80_init(z80_t* cpu) {
	CHIPS_ASSERT(cpu);
	// initial state as described in 'The Undocumented Z80 Documented'
	memset(cpu, 0, sizeof(z80_t));
	cpu->af = cpu->bc = cpu->de = cpu->hl = 0xFFFF;
	cpu->wz = cpu->sp = cpu->ix = cpu->iy = 0xFFFF;
	cpu->af2 = cpu->bc2 = cpu->de2 = cpu->hl2 = 0xFFFF;
	return z80_prefetch(cpu, 0x0000);
}

uint64_t z80_reset(z80_t* cpu) {
	// reset state as described in 'The Undocumented Z80 Documented'
	memset(cpu, 0, sizeof(z80_t));
	cpu->af = cpu->bc = cpu->de = cpu->hl = 0xFFFF;
	cpu->wz = cpu->sp = cpu->ix = cpu->iy = 0xFFFF;
	cpu->af2 = cpu->bc2 = cpu->de2 = cpu->hl2 = 0xFFFF;
	return z80_prefetch(cpu, 0x0000);
}

bool z80_opdone(z80_t* cpu) {
	// because of the overlapped cycle, the result of the previous
	// instruction is only available in M1/T2
	return ((cpu->pins & (Z80_M1 | Z80_RD)) == (Z80_M1 | Z80_RD)) && !cpu->prefix_active;
}

static inline uint64_t _z80_halt(z80_t* cpu, uint64_t pins) {
	cpu->pc--;
	return pins | Z80_HALT;
}

// sign+zero+parity lookup table
static const uint8_t _z80_szp_flags[256] = {
  0x44,0x00,0x00,0x04,0x00,0x04,0x04,0x00,0x08,0x0c,0x0c,0x08,0x0c,0x08,0x08,0x0c,
  0x00,0x04,0x04,0x00,0x04,0x00,0x00,0x04,0x0c,0x08,0x08,0x0c,0x08,0x0c,0x0c,0x08,
  0x20,0x24,0x24,0x20,0x24,0x20,0x20,0x24,0x2c,0x28,0x28,0x2c,0x28,0x2c,0x2c,0x28,
  0x24,0x20,0x20,0x24,0x20,0x24,0x24,0x20,0x28,0x2c,0x2c,0x28,0x2c,0x28,0x28,0x2c,
  0x00,0x04,0x04,0x00,0x04,0x00,0x00,0x04,0x0c,0x08,0x08,0x0c,0x08,0x0c,0x0c,0x08,
  0x04,0x00,0x00,0x04,0x00,0x04,0x04,0x00,0x08,0x0c,0x0c,0x08,0x0c,0x08,0x08,0x0c,
  0x24,0x20,0x20,0x24,0x20,0x24,0x24,0x20,0x28,0x2c,0x2c,0x28,0x2c,0x28,0x28,0x2c,
  0x20,0x24,0x24,0x20,0x24,0x20,0x20,0x24,0x2c,0x28,0x28,0x2c,0x28,0x2c,0x2c,0x28,
  0x80,0x84,0x84,0x80,0x84,0x80,0x80,0x84,0x8c,0x88,0x88,0x8c,0x88,0x8c,0x8c,0x88,
  0x84,0x80,0x80,0x84,0x80,0x84,0x84,0x80,0x88,0x8c,0x8c,0x88,0x8c,0x88,0x88,0x8c,
  0xa4,0xa0,0xa0,0xa4,0xa0,0xa4,0xa4,0xa0,0xa8,0xac,0xac,0xa8,0xac,0xa8,0xa8,0xac,
  0xa0,0xa4,0xa4,0xa0,0xa4,0xa0,0xa0,0xa4,0xac,0xa8,0xa8,0xac,0xa8,0xac,0xac,0xa8,
  0x84,0x80,0x80,0x84,0x80,0x84,0x84,0x80,0x88,0x8c,0x8c,0x88,0x8c,0x88,0x88,0x8c,
  0x80,0x84,0x84,0x80,0x84,0x80,0x80,0x84,0x8c,0x88,0x88,0x8c,0x88,0x8c,0x8c,0x88,
  0xa0,0xa4,0xa4,0xa0,0xa4,0xa0,0xa0,0xa4,0xac,0xa8,0xa8,0xac,0xa8,0xac,0xac,0xa8,
  0xa4,0xa0,0xa0,0xa4,0xa0,0xa4,0xa4,0xa0,0xa8,0xac,0xac,0xa8,0xac,0xa8,0xa8,0xac,
};

static inline uint8_t _z80_sz_flags(uint8_t val) {
	return (val != 0) ? (val & Z80_SF) : Z80_ZF;
}

static inline uint8_t _z80_szyxch_flags(uint8_t acc, uint8_t val, uint32_t res) {
	return _z80_sz_flags(res) |
		(res & (Z80_YF | Z80_XF)) |
		((res >> 8) & Z80_CF) |
		((acc ^ val ^ res) & Z80_HF);
}

static inline uint8_t _z80_add_flags(uint8_t acc, uint8_t val, uint32_t res) {
	return _z80_szyxch_flags(acc, val, res) | ((((val ^ acc ^ 0x80) & (val ^ res)) >> 5) & Z80_VF);
}

static inline uint8_t _z80_sub_flags(uint8_t acc, uint8_t val, uint32_t res) {
	return Z80_NF | _z80_szyxch_flags(acc, val, res) | ((((val ^ acc) & (res ^ acc)) >> 5) & Z80_VF);
}

static inline uint8_t _z80_cp_flags(uint8_t acc, uint8_t val, uint32_t res) {
	return Z80_NF |
		_z80_sz_flags(res) |
		(val & (Z80_YF | Z80_XF)) |
		((res >> 8) & Z80_CF) |
		((acc ^ val ^ res) & Z80_HF) |
		((((val ^ acc) & (res ^ acc)) >> 5) & Z80_VF);
}

static inline uint8_t _z80_sziff2_flags(z80_t* cpu, uint8_t val) {
	return (cpu->f & Z80_CF) | _z80_sz_flags(val) | (val & (Z80_YF | Z80_XF)) | (cpu->iff2 ? Z80_PF : 0);
}

static inline void _z80_add8(z80_t* cpu, uint8_t val) {
	uint32_t res = cpu->a + val;
	cpu->f = _z80_add_flags(cpu->a, val, res);
	cpu->a = (uint8_t)res;
}

static inline void _z80_adc8(z80_t* cpu, uint8_t val) {
	uint32_t res = cpu->a + val + (cpu->f & Z80_CF);
	cpu->f = _z80_add_flags(cpu->a, val, res);
	cpu->a = (uint8_t)res;
}

static inline void _z80_sub8(z80_t* cpu, uint8_t val) {
	uint32_t res = (uint32_t)((int)cpu->a - (int)val);
	cpu->f = _z80_sub_flags(cpu->a, val, res);
	cpu->a = (uint8_t)res;
}

static inline void _z80_sbc8(z80_t* cpu, uint8_t val) {
	uint32_t res = (uint32_t)((int)cpu->a - (int)val - (cpu->f & Z80_CF));
	cpu->f = _z80_sub_flags(cpu->a, val, res);
	cpu->a = (uint8_t)res;
}

static inline void _z80_and8(z80_t* cpu, uint8_t val) {
	cpu->a &= val;
	cpu->f = _z80_szp_flags[cpu->a] | Z80_HF;
}

static inline void _z80_xor8(z80_t* cpu, uint8_t val) {
	cpu->a ^= val;
	cpu->f = _z80_szp_flags[cpu->a];
}

static inline void _z80_or8(z80_t* cpu, uint8_t val) {
	cpu->a |= val;
	cpu->f = _z80_szp_flags[cpu->a];
}

static inline void _z80_cp8(z80_t* cpu, uint8_t val) {
	uint32_t res = (uint32_t)((int)cpu->a - (int)val);
	cpu->f = _z80_cp_flags(cpu->a, val, res);
}

static inline void _z80_neg8(z80_t* cpu) {
	uint32_t res = (uint32_t)(0 - (int)cpu->a);
	cpu->f = _z80_sub_flags(0, cpu->a, res);
	cpu->a = (uint8_t)res;
}

static inline uint8_t _z80_inc8(z80_t* cpu, uint8_t val) {
	uint8_t res = val + 1;
	uint8_t f = _z80_sz_flags(res) | (res & (Z80_XF | Z80_YF)) | ((res ^ val) & Z80_HF);
	if (res == 0x80) {
		f |= Z80_VF;
	}
	cpu->f = f | (cpu->f & Z80_CF);
	return res;
}

static inline uint8_t _z80_dec8(z80_t* cpu, uint8_t val) {
	uint8_t res = val - 1;
	uint8_t f = Z80_NF | _z80_sz_flags(res) | (res & (Z80_XF | Z80_YF)) | ((res ^ val) & Z80_HF);
	if (res == 0x7F) {
		f |= Z80_VF;
	}
	cpu->f = f | (cpu->f & Z80_CF);
	return res;
}

static inline void _z80_ex_de_hl(z80_t* cpu) {
	uint16_t tmp = cpu->hl;
	cpu->hl = cpu->de;
	cpu->de = tmp;
}

static inline void _z80_ex_af_af2(z80_t* cpu) {
	uint16_t tmp = cpu->af2;
	cpu->af2 = cpu->af;
	cpu->af = tmp;
}

static inline void _z80_exx(z80_t* cpu) {
	uint16_t tmp;
	tmp = cpu->bc; cpu->bc = cpu->bc2; cpu->bc2 = tmp;
	tmp = cpu->de; cpu->de = cpu->de2; cpu->de2 = tmp;
	tmp = cpu->hl; cpu->hl = cpu->hl2; cpu->hl2 = tmp;
}

static inline void _z80_rlca(z80_t* cpu) {
	uint8_t res = (cpu->a << 1) | (cpu->a >> 7);
	cpu->f = ((cpu->a >> 7) & Z80_CF) | (cpu->f & (Z80_SF | Z80_ZF | Z80_PF)) | (res & (Z80_YF | Z80_XF));
	cpu->a = res;
}

static inline void _z80_rrca(z80_t* cpu) {
	uint8_t res = (cpu->a >> 1) | (cpu->a << 7);
	cpu->f = (cpu->a & Z80_CF) | (cpu->f & (Z80_SF | Z80_ZF | Z80_PF)) | (res & (Z80_YF | Z80_XF));
	cpu->a = res;
}

static inline void _z80_rla(z80_t* cpu) {
	uint8_t res = (cpu->a << 1) | (cpu->f & Z80_CF);
	cpu->f = ((cpu->a >> 7) & Z80_CF) | (cpu->f & (Z80_SF | Z80_ZF | Z80_PF)) | (res & (Z80_YF | Z80_XF));
	cpu->a = res;
}

static inline void _z80_rra(z80_t* cpu) {
	uint8_t res = (cpu->a >> 1) | ((cpu->f & Z80_CF) << 7);
	cpu->f = (cpu->a & Z80_CF) | (cpu->f & (Z80_SF | Z80_ZF | Z80_PF)) | (res & (Z80_YF | Z80_XF));
	cpu->a = res;
}

static inline void _z80_daa(z80_t* cpu) {
	uint8_t res = cpu->a;
	if (cpu->f & Z80_NF) {
		if (((cpu->a & 0xF) > 0x9) || (cpu->f & Z80_HF)) {
			res -= 0x06;
		}
		if ((cpu->a > 0x99) || (cpu->f & Z80_CF)) {
			res -= 0x60;
		}
	}
	else {
		if (((cpu->a & 0xF) > 0x9) || (cpu->f & Z80_HF)) {
			res += 0x06;
		}
		if ((cpu->a > 0x99) || (cpu->f & Z80_CF)) {
			res += 0x60;
		}
	}
	cpu->f &= Z80_CF | Z80_NF;
	cpu->f |= (cpu->a > 0x99) ? Z80_CF : 0;
	cpu->f |= (cpu->a ^ res) & Z80_HF;
	cpu->f |= _z80_szp_flags[res];
	cpu->a = res;
}

static inline void _z80_cpl(z80_t* cpu) {
	cpu->a ^= 0xFF;
	cpu->f = (cpu->f & (Z80_SF | Z80_ZF | Z80_PF | Z80_CF)) | Z80_HF | Z80_NF | (cpu->a & (Z80_YF | Z80_XF));
}

static inline void _z80_scf(z80_t* cpu) {
	cpu->f = (cpu->f & (Z80_SF | Z80_ZF | Z80_PF | Z80_CF)) | Z80_CF | (cpu->a & (Z80_YF | Z80_XF));
}

static inline void _z80_ccf(z80_t* cpu) {
	cpu->f = ((cpu->f & (Z80_SF | Z80_ZF | Z80_PF | Z80_CF)) | ((cpu->f & Z80_CF) << 4) | (cpu->a & (Z80_YF | Z80_XF))) ^ Z80_CF;
}

static inline void _z80_add16(z80_t* cpu, uint16_t val) {
	const uint16_t acc = cpu->hlx[cpu->hlx_idx].hl;
	cpu->wz = acc + 1;
	const uint32_t res = acc + val;
	cpu->hlx[cpu->hlx_idx].hl = res;
	cpu->f = (cpu->f & (Z80_SF | Z80_ZF | Z80_VF)) |
		(((acc ^ res ^ val) >> 8) & Z80_HF) |
		((res >> 16) & Z80_CF) |
		((res >> 8) & (Z80_YF | Z80_XF));
}

static inline void _z80_adc16(z80_t* cpu, uint16_t val) {
	// NOTE: adc is ED-prefixed, so they are never rewired to IX/IY
	const uint16_t acc = cpu->hl;
	cpu->wz = acc + 1;
	const uint32_t res = acc + val + (cpu->f & Z80_CF);
	cpu->hl = res;
	cpu->f = (((val ^ acc ^ 0x8000) & (val ^ res) & 0x8000) >> 13) |
		(((acc ^ res ^ val) >> 8) & Z80_HF) |
		((res >> 16) & Z80_CF) |
		((res >> 8) & (Z80_SF | Z80_YF | Z80_XF)) |
		((res & 0xFFFF) ? 0 : Z80_ZF);
}

static inline void _z80_sbc16(z80_t* cpu, uint16_t val) {
	// NOTE: sbc is ED-prefixed, so they are never rewired to IX/IY
	const uint16_t acc = cpu->hl;
	cpu->wz = acc + 1;
	const uint32_t res = acc - val - (cpu->f & Z80_CF);
	cpu->hl = res;
	cpu->f = (Z80_NF | (((val ^ acc) & (acc ^ res) & 0x8000) >> 13)) |
		(((acc ^ res ^ val) >> 8) & Z80_HF) |
		((res >> 16) & Z80_CF) |
		((res >> 8) & (Z80_SF | Z80_YF | Z80_XF)) |
		((res & 0xFFFF) ? 0 : Z80_ZF);
}

static inline bool _z80_ldi_ldd(z80_t* cpu, uint8_t val) {
	const uint8_t res = cpu->a + val;
	cpu->bc -= 1;
	cpu->f = (cpu->f & (Z80_SF | Z80_ZF | Z80_CF)) |
		((res & 2) ? Z80_YF : 0) |
		((res & 8) ? Z80_XF : 0) |
		(cpu->bc ? Z80_VF : 0);
	return cpu->bc != 0;
}

static inline bool _z80_cpi_cpd(z80_t* cpu, uint8_t val) {
	uint32_t res = (uint32_t)((int)cpu->a - (int)val);
	cpu->bc -= 1;
	uint8_t f = (cpu->f & Z80_CF) | Z80_NF | _z80_sz_flags(res);
	if ((res & 0xF) > ((uint32_t)cpu->a & 0xF)) {
		f |= Z80_HF;
		res--;
	}
	if (res & 2) { f |= Z80_YF; }
	if (res & 8) { f |= Z80_XF; }
	if (cpu->bc) { f |= Z80_VF; }
	cpu->f = f;
	return (cpu->bc != 0) && !(f & Z80_ZF);
}

static inline bool _z80_ini_ind(z80_t* cpu, uint8_t val, uint8_t c) {
	const uint8_t b = cpu->b;
	uint8_t f = _z80_sz_flags(b) | (b & (Z80_XF | Z80_YF));
	if (val & Z80_SF) { f |= Z80_NF; }
	uint32_t t = (uint32_t)c + val;
	if (t & 0x100) { f |= Z80_HF | Z80_CF; }
	f |= _z80_szp_flags[((uint8_t)(t & 7)) ^ b] & Z80_PF;
	cpu->f = f;
	return (b != 0);
}

static inline bool _z80_outi_outd(z80_t* cpu, uint8_t val) {
	const uint8_t b = cpu->b;
	uint8_t f = _z80_sz_flags(b) | (b & (Z80_XF | Z80_YF));
	if (val & Z80_SF) { f |= Z80_NF; }
	uint32_t t = (uint32_t)cpu->l + val;
	if (t & 0x0100) { f |= Z80_HF | Z80_CF; }
	f |= _z80_szp_flags[((uint8_t)(t & 7)) ^ b] & Z80_PF;
	cpu->f = f;
	return (b != 0);
}

static inline uint8_t _z80_in(z80_t* cpu, uint8_t val) {
	cpu->f = (cpu->f & Z80_CF) | _z80_szp_flags[val];
	return val;
}

static inline uint8_t _z80_rrd(z80_t* cpu, uint8_t val) {
	const uint8_t l = cpu->a & 0x0F;
	cpu->a = (cpu->a & 0xF0) | (val & 0x0F);
	val = (val >> 4) | (l << 4);
	cpu->f = (cpu->f & Z80_CF) | _z80_szp_flags[cpu->a];
	return val;
}

static inline uint8_t _z80_rld(z80_t* cpu, uint8_t val) {
	const uint8_t l = cpu->a & 0x0F;
	cpu->a = (cpu->a & 0xF0) | (val >> 4);
	val = (val << 4) | l;
	cpu->f = (cpu->f & Z80_CF) | _z80_szp_flags[cpu->a];
	return val;
}

static inline uint8_t _z80_rlc(z80_t* cpu, uint8_t val) {
	uint8_t res = (val << 1) | (val >> 7);
	cpu->f = _z80_szp_flags[res] | ((val >> 7) & Z80_CF);
	return res;
}

static inline uint8_t _z80_rrc(z80_t* cpu, uint8_t val) {
	uint8_t res = (val >> 1) | (val << 7);
	cpu->f = _z80_szp_flags[res] | (val & Z80_CF);
	return res;
}

static inline uint8_t _z80_rl(z80_t* cpu, uint8_t val) {
	uint8_t res = (val << 1) | (cpu->f & Z80_CF);
	cpu->f = _z80_szp_flags[res] | ((val >> 7) & Z80_CF);
	return res;
}

static inline uint8_t _z80_rr(z80_t* cpu, uint8_t val) {
	uint8_t res = (val >> 1) | ((cpu->f & Z80_CF) << 7);
	cpu->f = _z80_szp_flags[res] | (val & Z80_CF);
	return res;
}

static inline uint8_t _z80_sla(z80_t* cpu, uint8_t val) {
	uint8_t res = val << 1;
	cpu->f = _z80_szp_flags[res] | ((val >> 7) & Z80_CF);
	return res;
}

static inline uint8_t _z80_sra(z80_t* cpu, uint8_t val) {
	uint8_t res = (val >> 1) | (val & 0x80);
	cpu->f = _z80_szp_flags[res] | (val & Z80_CF);
	return res;
}

static inline uint8_t _z80_sll(z80_t* cpu, uint8_t val) {
	uint8_t res = (val << 1) | 1;
	cpu->f = _z80_szp_flags[res] | ((val >> 7) & Z80_CF);
	return res;
}

static inline uint8_t _z80_srl(z80_t* cpu, uint8_t val) {
	uint8_t res = val >> 1;
	cpu->f = _z80_szp_flags[res] | (val & Z80_CF);
	return res;
}

static inline uint64_t _z80_set_ab(uint64_t pins, uint16_t ab) {
	return (pins & ~0xFFFF) | ab;
}

static inline uint64_t _z80_set_ab_x(uint64_t pins, uint16_t ab, uint64_t x) {
	return (pins & ~0xFFFF) | ab | x;
}

static inline uint64_t _z80_set_ab_db(uint64_t pins, uint16_t ab, uint8_t db) {
	return (pins & ~0xFFFFFF) | (db << 16) | ab;
}

static inline uint64_t _z80_set_ab_db_x(uint64_t pins, uint16_t ab, uint8_t db, uint64_t x) {
	return (pins & ~0xFFFFFF) | (db << 16) | ab | x;
}

static inline uint8_t _z80_get_db(uint64_t pins) {
	return (uint8_t)(pins >> 16);
}

// CB-prefix block action
static inline bool _z80_cb_action(z80_t* cpu, uint8_t z0, uint8_t z1) {
	const uint8_t x = cpu->opcode >> 6;
	const uint8_t y = (cpu->opcode >> 3) & 7;
	uint8_t val, res;
	switch (z0) {
	case 0: val = cpu->b; break;
	case 1: val = cpu->c; break;
	case 2: val = cpu->d; break;
	case 3: val = cpu->e; break;
	case 4: val = cpu->h; break;
	case 5: val = cpu->l; break;
	case 6: val = cpu->dlatch; break;   // (HL)
	case 7: val = cpu->a; break;
	default: _Z80_UNREACHABLE;
	}
	switch (x) {
	case 0: // rot/shift
		switch (y) {
		case 0: res = _z80_rlc(cpu, val); break;
		case 1: res = _z80_rrc(cpu, val); break;
		case 2: res = _z80_rl(cpu, val); break;
		case 3: res = _z80_rr(cpu, val); break;
		case 4: res = _z80_sla(cpu, val); break;
		case 5: res = _z80_sra(cpu, val); break;
		case 6: res = _z80_sll(cpu, val); break;
		case 7: res = _z80_srl(cpu, val); break;
		default: _Z80_UNREACHABLE;
		}
		break;
	case 1: // bit
		res = val & (1 << y);
		cpu->f = (cpu->f & Z80_CF) | Z80_HF | (res ? (res & Z80_SF) : (Z80_ZF | Z80_PF));
		if (z0 == 6) {
			cpu->f |= (cpu->wz >> 8) & (Z80_YF | Z80_XF);
		}
		else {
			cpu->f |= val & (Z80_YF | Z80_XF);
		}
		break;
	case 2: // res
		res = val & ~(1 << y);
		break;
	case 3: // set
		res = val | (1 << y);
		break;
	default: _Z80_UNREACHABLE;
	}
	// don't write result back for BIT
	if (x != 1) {
		cpu->dlatch = res;
		switch (z1) {
		case 0: cpu->b = res; break;
		case 1: cpu->c = res; break;
		case 2: cpu->d = res; break;
		case 3: cpu->e = res; break;
		case 4: cpu->h = res; break;
		case 5: cpu->l = res; break;
		case 6: break;   // (HL)
		case 7: cpu->a = res; break;
		default: _Z80_UNREACHABLE;
		}
		return true;
	}
	else {
		return false;
	}
}

// compute the effective memory address for DD+CB/FD+CB instructions
static inline void _z80_ddfdcb_addr(z80_t* cpu, uint64_t pins) {
	uint8_t d = _z80_get_db(pins);
	cpu->addr = cpu->hlx[cpu->hlx_idx].hl + (int8_t)d;
	cpu->wz = cpu->addr;
}

// special case opstate table slots
#define _Z80_OPSTATE_SLOT_CB        (0)
#define _Z80_OPSTATE_SLOT_CBHL      (1)
#define _Z80_OPSTATE_SLOT_DDFDCB    (2)
#define _Z80_OPSTATE_SLOT_INT_IM0   (3)
#define _Z80_OPSTATE_SLOT_INT_IM1   (4)
#define _Z80_OPSTATE_SLOT_INT_IM2   (5)
#define _Z80_OPSTATE_SLOT_NMI       (6)
#define _Z80_OPSTATE_NUM_SPECIAL_OPS (7)

#define _Z80_OPSTATE_STEP_INDIRECT (5)          // see case-branch '6'
#define _Z80_OPSTATE_STEP_INDIRECT_IMM8 (13)    // see case-branch '14'

static const uint16_t _z80_optable[256] = {
	  27,  // 00: NOP (M:1 T:4 steps:1)
	  28,  // 01: LD BC,nn (M:3 T:10 steps:7)
	  35,  // 02: LD (BC),A (M:2 T:7 steps:4)
	  39,  // 03: INC BC (M:2 T:6 steps:3)
	  42,  // 04: INC B (M:1 T:4 steps:1)
	  43,  // 05: DEC B (M:1 T:4 steps:1)
	  44,  // 06: LD B,n (M:2 T:7 steps:4)
	  48,  // 07: RLCA (M:1 T:4 steps:1)
	  49,  // 08: EX AF,AF' (M:1 T:4 steps:1)
	  50,  // 09: ADD HL,BC (M:2 T:11 steps:8)
	  58,  // 0A: LD A,(BC) (M:2 T:7 steps:4)
	  62,  // 0B: DEC BC (M:2 T:6 steps:3)
	  65,  // 0C: INC C (M:1 T:4 steps:1)
	  66,  // 0D: DEC C (M:1 T:4 steps:1)
	  67,  // 0E: LD C,n (M:2 T:7 steps:4)
	  71,  // 0F: RRCA (M:1 T:4 steps:1)
	  72,  // 10: DJNZ d (M:4 T:13 steps:10)
	  82,  // 11: LD DE,nn (M:3 T:10 steps:7)
	  89,  // 12: LD (DE),A (M:2 T:7 steps:4)
	  93,  // 13: INC DE (M:2 T:6 steps:3)
	  96,  // 14: INC D (M:1 T:4 steps:1)
	  97,  // 15: DEC D (M:1 T:4 steps:1)
	  98,  // 16: LD D,n (M:2 T:7 steps:4)
	 102,  // 17: RLA (M:1 T:4 steps:1)
	 103,  // 18: JR d (M:3 T:12 steps:9)
	 112,  // 19: ADD HL,DE (M:2 T:11 steps:8)
	 120,  // 1A: LD A,(DE) (M:2 T:7 steps:4)
	 124,  // 1B: DEC DE (M:2 T:6 steps:3)
	 127,  // 1C: INC E (M:1 T:4 steps:1)
	 128,  // 1D: DEC E (M:1 T:4 steps:1)
	 129,  // 1E: LD E,n (M:2 T:7 steps:4)
	 133,  // 1F: RRA (M:1 T:4 steps:1)
	 134,  // 20: JR NZ,d (M:3 T:12 steps:9)
	 143,  // 21: LD HL,nn (M:3 T:10 steps:7)
	 150,  // 22: LD (nn),HL (M:5 T:16 steps:13)
	 163,  // 23: INC HL (M:2 T:6 steps:3)
	 166,  // 24: INC H (M:1 T:4 steps:1)
	 167,  // 25: DEC H (M:1 T:4 steps:1)
	 168,  // 26: LD H,n (M:2 T:7 steps:4)
	 172,  // 27: DAA (M:1 T:4 steps:1)
	 173,  // 28: JR Z,d (M:3 T:12 steps:9)
	 182,  // 29: ADD HL,HL (M:2 T:11 steps:8)
	 190,  // 2A: LD HL,(nn) (M:5 T:16 steps:13)
	 203,  // 2B: DEC HL (M:2 T:6 steps:3)
	 206,  // 2C: INC L (M:1 T:4 steps:1)
	 207,  // 2D: DEC L (M:1 T:4 steps:1)
	 208,  // 2E: LD L,n (M:2 T:7 steps:4)
	 212,  // 2F: CPL (M:1 T:4 steps:1)
	 213,  // 30: JR NC,d (M:3 T:12 steps:9)
	 222,  // 31: LD SP,nn (M:3 T:10 steps:7)
	 229,  // 32: LD (nn),A (M:4 T:13 steps:10)
	 239,  // 33: INC SP (M:2 T:6 steps:3)
	 242,  // 34: INC (HL) (M:3 T:11 steps:8)
	 250,  // 35: DEC (HL) (M:3 T:11 steps:8)
	 258,  // 36: LD (HL),n (M:3 T:10 steps:7)
	 265,  // 37: SCF (M:1 T:4 steps:1)
	 266,  // 38: JR C,d (M:3 T:12 steps:9)
	 275,  // 39: ADD HL,SP (M:2 T:11 steps:8)
	 283,  // 3A: LD A,(nn) (M:4 T:13 steps:10)
	 293,  // 3B: DEC SP (M:2 T:6 steps:3)
	 296,  // 3C: INC A (M:1 T:4 steps:1)
	 297,  // 3D: DEC A (M:1 T:4 steps:1)
	 298,  // 3E: LD A,n (M:2 T:7 steps:4)
	 302,  // 3F: CCF (M:1 T:4 steps:1)
	 303,  // 40: LD B,B (M:1 T:4 steps:1)
	 304,  // 41: LD B,C (M:1 T:4 steps:1)
	 305,  // 42: LD B,D (M:1 T:4 steps:1)
	 306,  // 43: LD B,E (M:1 T:4 steps:1)
	 307,  // 44: LD B,H (M:1 T:4 steps:1)
	 308,  // 45: LD B,L (M:1 T:4 steps:1)
	 309,  // 46: LD B,(HL) (M:2 T:7 steps:4)
	 313,  // 47: LD B,A (M:1 T:4 steps:1)
	 314,  // 48: LD C,B (M:1 T:4 steps:1)
	 315,  // 49: LD C,C (M:1 T:4 steps:1)
	 316,  // 4A: LD C,D (M:1 T:4 steps:1)
	 317,  // 4B: LD C,E (M:1 T:4 steps:1)
	 318,  // 4C: LD C,H (M:1 T:4 steps:1)
	 319,  // 4D: LD C,L (M:1 T:4 steps:1)
	 320,  // 4E: LD C,(HL) (M:2 T:7 steps:4)
	 324,  // 4F: LD C,A (M:1 T:4 steps:1)
	 325,  // 50: LD D,B (M:1 T:4 steps:1)
	 326,  // 51: LD D,C (M:1 T:4 steps:1)
	 327,  // 52: LD D,D (M:1 T:4 steps:1)
	 328,  // 53: LD D,E (M:1 T:4 steps:1)
	 329,  // 54: LD D,H (M:1 T:4 steps:1)
	 330,  // 55: LD D,L (M:1 T:4 steps:1)
	 331,  // 56: LD D,(HL) (M:2 T:7 steps:4)
	 335,  // 57: LD D,A (M:1 T:4 steps:1)
	 336,  // 58: LD E,B (M:1 T:4 steps:1)
	 337,  // 59: LD E,C (M:1 T:4 steps:1)
	 338,  // 5A: LD E,D (M:1 T:4 steps:1)
	 339,  // 5B: LD E,E (M:1 T:4 steps:1)
	 340,  // 5C: LD E,H (M:1 T:4 steps:1)
	 341,  // 5D: LD E,L (M:1 T:4 steps:1)
	 342,  // 5E: LD E,(HL) (M:2 T:7 steps:4)
	 346,  // 5F: LD E,A (M:1 T:4 steps:1)
	 347,  // 60: LD H,B (M:1 T:4 steps:1)
	 348,  // 61: LD H,C (M:1 T:4 steps:1)
	 349,  // 62: LD H,D (M:1 T:4 steps:1)
	 350,  // 63: LD H,E (M:1 T:4 steps:1)
	 351,  // 64: LD H,H (M:1 T:4 steps:1)
	 352,  // 65: LD H,L (M:1 T:4 steps:1)
	 353,  // 66: LD H,(HL) (M:2 T:7 steps:4)
	 357,  // 67: LD H,A (M:1 T:4 steps:1)
	 358,  // 68: LD L,B (M:1 T:4 steps:1)
	 359,  // 69: LD L,C (M:1 T:4 steps:1)
	 360,  // 6A: LD L,D (M:1 T:4 steps:1)
	 361,  // 6B: LD L,E (M:1 T:4 steps:1)
	 362,  // 6C: LD L,H (M:1 T:4 steps:1)
	 363,  // 6D: LD L,L (M:1 T:4 steps:1)
	 364,  // 6E: LD L,(HL) (M:2 T:7 steps:4)
	 368,  // 6F: LD L,A (M:1 T:4 steps:1)
	 369,  // 70: LD (HL),B (M:2 T:7 steps:4)
	 373,  // 71: LD (HL),C (M:2 T:7 steps:4)
	 377,  // 72: LD (HL),D (M:2 T:7 steps:4)
	 381,  // 73: LD (HL),E (M:2 T:7 steps:4)
	 385,  // 74: LD (HL),H (M:2 T:7 steps:4)
	 389,  // 75: LD (HL),L (M:2 T:7 steps:4)
	 393,  // 76: HALT (M:1 T:4 steps:1)
	 394,  // 77: LD (HL),A (M:2 T:7 steps:4)
	 398,  // 78: LD A,B (M:1 T:4 steps:1)
	 399,  // 79: LD A,C (M:1 T:4 steps:1)
	 400,  // 7A: LD A,D (M:1 T:4 steps:1)
	 401,  // 7B: LD A,E (M:1 T:4 steps:1)
	 402,  // 7C: LD A,H (M:1 T:4 steps:1)
	 403,  // 7D: LD A,L (M:1 T:4 steps:1)
	 404,  // 7E: LD A,(HL) (M:2 T:7 steps:4)
	 408,  // 7F: LD A,A (M:1 T:4 steps:1)
	 409,  // 80: ADD B (M:1 T:4 steps:1)
	 410,  // 81: ADD C (M:1 T:4 steps:1)
	 411,  // 82: ADD D (M:1 T:4 steps:1)
	 412,  // 83: ADD E (M:1 T:4 steps:1)
	 413,  // 84: ADD H (M:1 T:4 steps:1)
	 414,  // 85: ADD L (M:1 T:4 steps:1)
	 415,  // 86: ADD (HL) (M:2 T:7 steps:4)
	 419,  // 87: ADD A (M:1 T:4 steps:1)
	 420,  // 88: ADC B (M:1 T:4 steps:1)
	 421,  // 89: ADC C (M:1 T:4 steps:1)
	 422,  // 8A: ADC D (M:1 T:4 steps:1)
	 423,  // 8B: ADC E (M:1 T:4 steps:1)
	 424,  // 8C: ADC H (M:1 T:4 steps:1)
	 425,  // 8D: ADC L (M:1 T:4 steps:1)
	 426,  // 8E: ADC (HL) (M:2 T:7 steps:4)
	 430,  // 8F: ADC A (M:1 T:4 steps:1)
	 431,  // 90: SUB B (M:1 T:4 steps:1)
	 432,  // 91: SUB C (M:1 T:4 steps:1)
	 433,  // 92: SUB D (M:1 T:4 steps:1)
	 434,  // 93: SUB E (M:1 T:4 steps:1)
	 435,  // 94: SUB H (M:1 T:4 steps:1)
	 436,  // 95: SUB L (M:1 T:4 steps:1)
	 437,  // 96: SUB (HL) (M:2 T:7 steps:4)
	 441,  // 97: SUB A (M:1 T:4 steps:1)
	 442,  // 98: SBC B (M:1 T:4 steps:1)
	 443,  // 99: SBC C (M:1 T:4 steps:1)
	 444,  // 9A: SBC D (M:1 T:4 steps:1)
	 445,  // 9B: SBC E (M:1 T:4 steps:1)
	 446,  // 9C: SBC H (M:1 T:4 steps:1)
	 447,  // 9D: SBC L (M:1 T:4 steps:1)
	 448,  // 9E: SBC (HL) (M:2 T:7 steps:4)
	 452,  // 9F: SBC A (M:1 T:4 steps:1)
	 453,  // A0: AND B (M:1 T:4 steps:1)
	 454,  // A1: AND C (M:1 T:4 steps:1)
	 455,  // A2: AND D (M:1 T:4 steps:1)
	 456,  // A3: AND E (M:1 T:4 steps:1)
	 457,  // A4: AND H (M:1 T:4 steps:1)
	 458,  // A5: AND L (M:1 T:4 steps:1)
	 459,  // A6: AND (HL) (M:2 T:7 steps:4)
	 463,  // A7: AND A (M:1 T:4 steps:1)
	 464,  // A8: XOR B (M:1 T:4 steps:1)
	 465,  // A9: XOR C (M:1 T:4 steps:1)
	 466,  // AA: XOR D (M:1 T:4 steps:1)
	 467,  // AB: XOR E (M:1 T:4 steps:1)
	 468,  // AC: XOR H (M:1 T:4 steps:1)
	 469,  // AD: XOR L (M:1 T:4 steps:1)
	 470,  // AE: XOR (HL) (M:2 T:7 steps:4)
	 474,  // AF: XOR A (M:1 T:4 steps:1)
	 475,  // B0: OR B (M:1 T:4 steps:1)
	 476,  // B1: OR C (M:1 T:4 steps:1)
	 477,  // B2: OR D (M:1 T:4 steps:1)
	 478,  // B3: OR E (M:1 T:4 steps:1)
	 479,  // B4: OR H (M:1 T:4 steps:1)
	 480,  // B5: OR L (M:1 T:4 steps:1)
	 481,  // B6: OR (HL) (M:2 T:7 steps:4)
	 485,  // B7: OR A (M:1 T:4 steps:1)
	 486,  // B8: CP B (M:1 T:4 steps:1)
	 487,  // B9: CP C (M:1 T:4 steps:1)
	 488,  // BA: CP D (M:1 T:4 steps:1)
	 489,  // BB: CP E (M:1 T:4 steps:1)
	 490,  // BC: CP H (M:1 T:4 steps:1)
	 491,  // BD: CP L (M:1 T:4 steps:1)
	 492,  // BE: CP (HL) (M:2 T:7 steps:4)
	 496,  // BF: CP A (M:1 T:4 steps:1)
	 497,  // C0: RET NZ (M:4 T:11 steps:8)
	 505,  // C1: POP BC (M:3 T:10 steps:7)
	 512,  // C2: JP NZ,nn (M:3 T:10 steps:7)
	 519,  // C3: JP nn (M:3 T:10 steps:7)
	 526,  // C4: CALL NZ,nn (M:6 T:17 steps:14)
	 540,  // C5: PUSH BC (M:4 T:11 steps:8)
	 548,  // C6: ADD n (M:2 T:7 steps:4)
	 552,  // C7: RST 0h (M:4 T:11 steps:8)
	 560,  // C8: RET Z (M:4 T:11 steps:8)
	 568,  // C9: RET (M:3 T:10 steps:7)
	 575,  // CA: JP Z,nn (M:3 T:10 steps:7)
	 582,  // CB: CB prefix (M:1 T:4 steps:1)
	 583,  // CC: CALL Z,nn (M:6 T:17 steps:14)
	 597,  // CD: CALL nn (M:5 T:17 steps:14)
	 611,  // CE: ADC n (M:2 T:7 steps:4)
	 615,  // CF: RST 8h (M:4 T:11 steps:8)
	 623,  // D0: RET NC (M:4 T:11 steps:8)
	 631,  // D1: POP DE (M:3 T:10 steps:7)
	 638,  // D2: JP NC,nn (M:3 T:10 steps:7)
	 645,  // D3: OUT (n),A (M:3 T:11 steps:8)
	 653,  // D4: CALL NC,nn (M:6 T:17 steps:14)
	 667,  // D5: PUSH DE (M:4 T:11 steps:8)
	 675,  // D6: SUB n (M:2 T:7 steps:4)
	 679,  // D7: RST 10h (M:4 T:11 steps:8)
	 687,  // D8: RET C (M:4 T:11 steps:8)
	 695,  // D9: EXX (M:1 T:4 steps:1)
	 696,  // DA: JP C,nn (M:3 T:10 steps:7)
	 703,  // DB: IN A,(n) (M:3 T:11 steps:8)
	 711,  // DC: CALL C,nn (M:6 T:17 steps:14)
	 725,  // DD: DD prefix (M:1 T:4 steps:1)
	 726,  // DE: SBC n (M:2 T:7 steps:4)
	 730,  // DF: RST 18h (M:4 T:11 steps:8)
	 738,  // E0: RET PO (M:4 T:11 steps:8)
	 746,  // E1: POP HL (M:3 T:10 steps:7)
	 753,  // E2: JP PO,nn (M:3 T:10 steps:7)
	 760,  // E3: EX (SP),HL (M:5 T:19 steps:16)
	 776,  // E4: CALL PO,nn (M:6 T:17 steps:14)
	 790,  // E5: PUSH HL (M:4 T:11 steps:8)
	 798,  // E6: AND n (M:2 T:7 steps:4)
	 802,  // E7: RST 20h (M:4 T:11 steps:8)
	 810,  // E8: RET PE (M:4 T:11 steps:8)
	 818,  // E9: JP HL (M:1 T:4 steps:1)
	 819,  // EA: JP PE,nn (M:3 T:10 steps:7)
	 826,  // EB: EX DE,HL (M:1 T:4 steps:1)
	 827,  // EC: CALL PE,nn (M:6 T:17 steps:14)
	 841,  // ED: ED prefix (M:1 T:4 steps:1)
	 842,  // EE: XOR n (M:2 T:7 steps:4)
	 846,  // EF: RST 28h (M:4 T:11 steps:8)
	 854,  // F0: RET P (M:4 T:11 steps:8)
	 862,  // F1: POP AF (M:3 T:10 steps:7)
	 869,  // F2: JP P,nn (M:3 T:10 steps:7)
	 876,  // F3: DI (M:1 T:4 steps:1)
	 877,  // F4: CALL P,nn (M:6 T:17 steps:14)
	 891,  // F5: PUSH AF (M:4 T:11 steps:8)
	 899,  // F6: OR n (M:2 T:7 steps:4)
	 903,  // F7: RST 30h (M:4 T:11 steps:8)
	 911,  // F8: RET M (M:4 T:11 steps:8)
	 919,  // F9: LD SP,HL (M:2 T:6 steps:3)
	 922,  // FA: JP M,nn (M:3 T:10 steps:7)
	 929,  // FB: EI (M:1 T:4 steps:1)
	 930,  // FC: CALL M,nn (M:6 T:17 steps:14)
	 944,  // FD: FD prefix (M:1 T:4 steps:1)
	 945,  // FE: CP n (M:2 T:7 steps:4)
	 949,  // FF: RST 38h (M:4 T:11 steps:8)
};

static const uint16_t _z80_ddfd_optable[256] = {
	  27,  // 00: NOP (M:1 T:4 steps:1)
	  28,  // 01: LD BC,nn (M:3 T:10 steps:7)
	  35,  // 02: LD (BC),A (M:2 T:7 steps:4)
	  39,  // 03: INC BC (M:2 T:6 steps:3)
	  42,  // 04: INC B (M:1 T:4 steps:1)
	  43,  // 05: DEC B (M:1 T:4 steps:1)
	  44,  // 06: LD B,n (M:2 T:7 steps:4)
	  48,  // 07: RLCA (M:1 T:4 steps:1)
	  49,  // 08: EX AF,AF' (M:1 T:4 steps:1)
	  50,  // 09: ADD HL,BC (M:2 T:11 steps:8)
	  58,  // 0A: LD A,(BC) (M:2 T:7 steps:4)
	  62,  // 0B: DEC BC (M:2 T:6 steps:3)
	  65,  // 0C: INC C (M:1 T:4 steps:1)
	  66,  // 0D: DEC C (M:1 T:4 steps:1)
	  67,  // 0E: LD C,n (M:2 T:7 steps:4)
	  71,  // 0F: RRCA (M:1 T:4 steps:1)
	  72,  // 10: DJNZ d (M:4 T:13 steps:10)
	  82,  // 11: LD DE,nn (M:3 T:10 steps:7)
	  89,  // 12: LD (DE),A (M:2 T:7 steps:4)
	  93,  // 13: INC DE (M:2 T:6 steps:3)
	  96,  // 14: INC D (M:1 T:4 steps:1)
	  97,  // 15: DEC D (M:1 T:4 steps:1)
	  98,  // 16: LD D,n (M:2 T:7 steps:4)
	 102,  // 17: RLA (M:1 T:4 steps:1)
	 103,  // 18: JR d (M:3 T:12 steps:9)
	 112,  // 19: ADD HL,DE (M:2 T:11 steps:8)
	 120,  // 1A: LD A,(DE) (M:2 T:7 steps:4)
	 124,  // 1B: DEC DE (M:2 T:6 steps:3)
	 127,  // 1C: INC E (M:1 T:4 steps:1)
	 128,  // 1D: DEC E (M:1 T:4 steps:1)
	 129,  // 1E: LD E,n (M:2 T:7 steps:4)
	 133,  // 1F: RRA (M:1 T:4 steps:1)
	 134,  // 20: JR NZ,d (M:3 T:12 steps:9)
	 143,  // 21: LD HL,nn (M:3 T:10 steps:7)
	 150,  // 22: LD (nn),HL (M:5 T:16 steps:13)
	 163,  // 23: INC HL (M:2 T:6 steps:3)
	 166,  // 24: INC H (M:1 T:4 steps:1)
	 167,  // 25: DEC H (M:1 T:4 steps:1)
	 168,  // 26: LD H,n (M:2 T:7 steps:4)
	 172,  // 27: DAA (M:1 T:4 steps:1)
	 173,  // 28: JR Z,d (M:3 T:12 steps:9)
	 182,  // 29: ADD HL,HL (M:2 T:11 steps:8)
	 190,  // 2A: LD HL,(nn) (M:5 T:16 steps:13)
	 203,  // 2B: DEC HL (M:2 T:6 steps:3)
	 206,  // 2C: INC L (M:1 T:4 steps:1)
	 207,  // 2D: DEC L (M:1 T:4 steps:1)
	 208,  // 2E: LD L,n (M:2 T:7 steps:4)
	 212,  // 2F: CPL (M:1 T:4 steps:1)
	 213,  // 30: JR NC,d (M:3 T:12 steps:9)
	 222,  // 31: LD SP,nn (M:3 T:10 steps:7)
	 229,  // 32: LD (nn),A (M:4 T:13 steps:10)
	 239,  // 33: INC SP (M:2 T:6 steps:3)
	_Z80_OPSTATE_STEP_INDIRECT,  // 34: INC (HL) (M:3 T:11 steps:8)
	_Z80_OPSTATE_STEP_INDIRECT,  // 35: DEC (HL) (M:3 T:11 steps:8)
	_Z80_OPSTATE_STEP_INDIRECT_IMM8,  // 36: LD (HL),n (M:3 T:10 steps:7)
	 265,  // 37: SCF (M:1 T:4 steps:1)
	 266,  // 38: JR C,d (M:3 T:12 steps:9)
	 275,  // 39: ADD HL,SP (M:2 T:11 steps:8)
	 283,  // 3A: LD A,(nn) (M:4 T:13 steps:10)
	 293,  // 3B: DEC SP (M:2 T:6 steps:3)
	 296,  // 3C: INC A (M:1 T:4 steps:1)
	 297,  // 3D: DEC A (M:1 T:4 steps:1)
	 298,  // 3E: LD A,n (M:2 T:7 steps:4)
	 302,  // 3F: CCF (M:1 T:4 steps:1)
	 303,  // 40: LD B,B (M:1 T:4 steps:1)
	 304,  // 41: LD B,C (M:1 T:4 steps:1)
	 305,  // 42: LD B,D (M:1 T:4 steps:1)
	 306,  // 43: LD B,E (M:1 T:4 steps:1)
	 307,  // 44: LD B,H (M:1 T:4 steps:1)
	 308,  // 45: LD B,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 46: LD B,(HL) (M:2 T:7 steps:4)
	 313,  // 47: LD B,A (M:1 T:4 steps:1)
	 314,  // 48: LD C,B (M:1 T:4 steps:1)
	 315,  // 49: LD C,C (M:1 T:4 steps:1)
	 316,  // 4A: LD C,D (M:1 T:4 steps:1)
	 317,  // 4B: LD C,E (M:1 T:4 steps:1)
	 318,  // 4C: LD C,H (M:1 T:4 steps:1)
	 319,  // 4D: LD C,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 4E: LD C,(HL) (M:2 T:7 steps:4)
	 324,  // 4F: LD C,A (M:1 T:4 steps:1)
	 325,  // 50: LD D,B (M:1 T:4 steps:1)
	 326,  // 51: LD D,C (M:1 T:4 steps:1)
	 327,  // 52: LD D,D (M:1 T:4 steps:1)
	 328,  // 53: LD D,E (M:1 T:4 steps:1)
	 329,  // 54: LD D,H (M:1 T:4 steps:1)
	 330,  // 55: LD D,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 56: LD D,(HL) (M:2 T:7 steps:4)
	 335,  // 57: LD D,A (M:1 T:4 steps:1)
	 336,  // 58: LD E,B (M:1 T:4 steps:1)
	 337,  // 59: LD E,C (M:1 T:4 steps:1)
	 338,  // 5A: LD E,D (M:1 T:4 steps:1)
	 339,  // 5B: LD E,E (M:1 T:4 steps:1)
	 340,  // 5C: LD E,H (M:1 T:4 steps:1)
	 341,  // 5D: LD E,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 5E: LD E,(HL) (M:2 T:7 steps:4)
	 346,  // 5F: LD E,A (M:1 T:4 steps:1)
	 347,  // 60: LD H,B (M:1 T:4 steps:1)
	 348,  // 61: LD H,C (M:1 T:4 steps:1)
	 349,  // 62: LD H,D (M:1 T:4 steps:1)
	 350,  // 63: LD H,E (M:1 T:4 steps:1)
	 351,  // 64: LD H,H (M:1 T:4 steps:1)
	 352,  // 65: LD H,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 66: LD H,(HL) (M:2 T:7 steps:4)
	 357,  // 67: LD H,A (M:1 T:4 steps:1)
	 358,  // 68: LD L,B (M:1 T:4 steps:1)
	 359,  // 69: LD L,C (M:1 T:4 steps:1)
	 360,  // 6A: LD L,D (M:1 T:4 steps:1)
	 361,  // 6B: LD L,E (M:1 T:4 steps:1)
	 362,  // 6C: LD L,H (M:1 T:4 steps:1)
	 363,  // 6D: LD L,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 6E: LD L,(HL) (M:2 T:7 steps:4)
	 368,  // 6F: LD L,A (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 70: LD (HL),B (M:2 T:7 steps:4)
	_Z80_OPSTATE_STEP_INDIRECT,  // 71: LD (HL),C (M:2 T:7 steps:4)
	_Z80_OPSTATE_STEP_INDIRECT,  // 72: LD (HL),D (M:2 T:7 steps:4)
	_Z80_OPSTATE_STEP_INDIRECT,  // 73: LD (HL),E (M:2 T:7 steps:4)
	_Z80_OPSTATE_STEP_INDIRECT,  // 74: LD (HL),H (M:2 T:7 steps:4)
	_Z80_OPSTATE_STEP_INDIRECT,  // 75: LD (HL),L (M:2 T:7 steps:4)
	 393,  // 76: HALT (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 77: LD (HL),A (M:2 T:7 steps:4)
	 398,  // 78: LD A,B (M:1 T:4 steps:1)
	 399,  // 79: LD A,C (M:1 T:4 steps:1)
	 400,  // 7A: LD A,D (M:1 T:4 steps:1)
	 401,  // 7B: LD A,E (M:1 T:4 steps:1)
	 402,  // 7C: LD A,H (M:1 T:4 steps:1)
	 403,  // 7D: LD A,L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 7E: LD A,(HL) (M:2 T:7 steps:4)
	 408,  // 7F: LD A,A (M:1 T:4 steps:1)
	 409,  // 80: ADD B (M:1 T:4 steps:1)
	 410,  // 81: ADD C (M:1 T:4 steps:1)
	 411,  // 82: ADD D (M:1 T:4 steps:1)
	 412,  // 83: ADD E (M:1 T:4 steps:1)
	 413,  // 84: ADD H (M:1 T:4 steps:1)
	 414,  // 85: ADD L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 86: ADD (HL) (M:2 T:7 steps:4)
	 419,  // 87: ADD A (M:1 T:4 steps:1)
	 420,  // 88: ADC B (M:1 T:4 steps:1)
	 421,  // 89: ADC C (M:1 T:4 steps:1)
	 422,  // 8A: ADC D (M:1 T:4 steps:1)
	 423,  // 8B: ADC E (M:1 T:4 steps:1)
	 424,  // 8C: ADC H (M:1 T:4 steps:1)
	 425,  // 8D: ADC L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 8E: ADC (HL) (M:2 T:7 steps:4)
	 430,  // 8F: ADC A (M:1 T:4 steps:1)
	 431,  // 90: SUB B (M:1 T:4 steps:1)
	 432,  // 91: SUB C (M:1 T:4 steps:1)
	 433,  // 92: SUB D (M:1 T:4 steps:1)
	 434,  // 93: SUB E (M:1 T:4 steps:1)
	 435,  // 94: SUB H (M:1 T:4 steps:1)
	 436,  // 95: SUB L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 96: SUB (HL) (M:2 T:7 steps:4)
	 441,  // 97: SUB A (M:1 T:4 steps:1)
	 442,  // 98: SBC B (M:1 T:4 steps:1)
	 443,  // 99: SBC C (M:1 T:4 steps:1)
	 444,  // 9A: SBC D (M:1 T:4 steps:1)
	 445,  // 9B: SBC E (M:1 T:4 steps:1)
	 446,  // 9C: SBC H (M:1 T:4 steps:1)
	 447,  // 9D: SBC L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // 9E: SBC (HL) (M:2 T:7 steps:4)
	 452,  // 9F: SBC A (M:1 T:4 steps:1)
	 453,  // A0: AND B (M:1 T:4 steps:1)
	 454,  // A1: AND C (M:1 T:4 steps:1)
	 455,  // A2: AND D (M:1 T:4 steps:1)
	 456,  // A3: AND E (M:1 T:4 steps:1)
	 457,  // A4: AND H (M:1 T:4 steps:1)
	 458,  // A5: AND L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // A6: AND (HL) (M:2 T:7 steps:4)
	 463,  // A7: AND A (M:1 T:4 steps:1)
	 464,  // A8: XOR B (M:1 T:4 steps:1)
	 465,  // A9: XOR C (M:1 T:4 steps:1)
	 466,  // AA: XOR D (M:1 T:4 steps:1)
	 467,  // AB: XOR E (M:1 T:4 steps:1)
	 468,  // AC: XOR H (M:1 T:4 steps:1)
	 469,  // AD: XOR L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // AE: XOR (HL) (M:2 T:7 steps:4)
	 474,  // AF: XOR A (M:1 T:4 steps:1)
	 475,  // B0: OR B (M:1 T:4 steps:1)
	 476,  // B1: OR C (M:1 T:4 steps:1)
	 477,  // B2: OR D (M:1 T:4 steps:1)
	 478,  // B3: OR E (M:1 T:4 steps:1)
	 479,  // B4: OR H (M:1 T:4 steps:1)
	 480,  // B5: OR L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // B6: OR (HL) (M:2 T:7 steps:4)
	 485,  // B7: OR A (M:1 T:4 steps:1)
	 486,  // B8: CP B (M:1 T:4 steps:1)
	 487,  // B9: CP C (M:1 T:4 steps:1)
	 488,  // BA: CP D (M:1 T:4 steps:1)
	 489,  // BB: CP E (M:1 T:4 steps:1)
	 490,  // BC: CP H (M:1 T:4 steps:1)
	 491,  // BD: CP L (M:1 T:4 steps:1)
	_Z80_OPSTATE_STEP_INDIRECT,  // BE: CP (HL) (M:2 T:7 steps:4)
	 496,  // BF: CP A (M:1 T:4 steps:1)
	 497,  // C0: RET NZ (M:4 T:11 steps:8)
	 505,  // C1: POP BC (M:3 T:10 steps:7)
	 512,  // C2: JP NZ,nn (M:3 T:10 steps:7)
	 519,  // C3: JP nn (M:3 T:10 steps:7)
	 526,  // C4: CALL NZ,nn (M:6 T:17 steps:14)
	 540,  // C5: PUSH BC (M:4 T:11 steps:8)
	 548,  // C6: ADD n (M:2 T:7 steps:4)
	 552,  // C7: RST 0h (M:4 T:11 steps:8)
	 560,  // C8: RET Z (M:4 T:11 steps:8)
	 568,  // C9: RET (M:3 T:10 steps:7)
	 575,  // CA: JP Z,nn (M:3 T:10 steps:7)
	 582,  // CB: CB prefix (M:1 T:4 steps:1)
	 583,  // CC: CALL Z,nn (M:6 T:17 steps:14)
	 597,  // CD: CALL nn (M:5 T:17 steps:14)
	 611,  // CE: ADC n (M:2 T:7 steps:4)
	 615,  // CF: RST 8h (M:4 T:11 steps:8)
	 623,  // D0: RET NC (M:4 T:11 steps:8)
	 631,  // D1: POP DE (M:3 T:10 steps:7)
	 638,  // D2: JP NC,nn (M:3 T:10 steps:7)
	 645,  // D3: OUT (n),A (M:3 T:11 steps:8)
	 653,  // D4: CALL NC,nn (M:6 T:17 steps:14)
	 667,  // D5: PUSH DE (M:4 T:11 steps:8)
	 675,  // D6: SUB n (M:2 T:7 steps:4)
	 679,  // D7: RST 10h (M:4 T:11 steps:8)
	 687,  // D8: RET C (M:4 T:11 steps:8)
	 695,  // D9: EXX (M:1 T:4 steps:1)
	 696,  // DA: JP C,nn (M:3 T:10 steps:7)
	 703,  // DB: IN A,(n) (M:3 T:11 steps:8)
	 711,  // DC: CALL C,nn (M:6 T:17 steps:14)
	 725,  // DD: DD prefix (M:1 T:4 steps:1)
	 726,  // DE: SBC n (M:2 T:7 steps:4)
	 730,  // DF: RST 18h (M:4 T:11 steps:8)
	 738,  // E0: RET PO (M:4 T:11 steps:8)
	 746,  // E1: POP HL (M:3 T:10 steps:7)
	 753,  // E2: JP PO,nn (M:3 T:10 steps:7)
	 760,  // E3: EX (SP),HL (M:5 T:19 steps:16)
	 776,  // E4: CALL PO,nn (M:6 T:17 steps:14)
	 790,  // E5: PUSH HL (M:4 T:11 steps:8)
	 798,  // E6: AND n (M:2 T:7 steps:4)
	 802,  // E7: RST 20h (M:4 T:11 steps:8)
	 810,  // E8: RET PE (M:4 T:11 steps:8)
	 818,  // E9: JP HL (M:1 T:4 steps:1)
	 819,  // EA: JP PE,nn (M:3 T:10 steps:7)
	 826,  // EB: EX DE,HL (M:1 T:4 steps:1)
	 827,  // EC: CALL PE,nn (M:6 T:17 steps:14)
	 841,  // ED: ED prefix (M:1 T:4 steps:1)
	 842,  // EE: XOR n (M:2 T:7 steps:4)
	 846,  // EF: RST 28h (M:4 T:11 steps:8)
	 854,  // F0: RET P (M:4 T:11 steps:8)
	 862,  // F1: POP AF (M:3 T:10 steps:7)
	 869,  // F2: JP P,nn (M:3 T:10 steps:7)
	 876,  // F3: DI (M:1 T:4 steps:1)
	 877,  // F4: CALL P,nn (M:6 T:17 steps:14)
	 891,  // F5: PUSH AF (M:4 T:11 steps:8)
	 899,  // F6: OR n (M:2 T:7 steps:4)
	 903,  // F7: RST 30h (M:4 T:11 steps:8)
	 911,  // F8: RET M (M:4 T:11 steps:8)
	 919,  // F9: LD SP,HL (M:2 T:6 steps:3)
	 922,  // FA: JP M,nn (M:3 T:10 steps:7)
	 929,  // FB: EI (M:1 T:4 steps:1)
	 930,  // FC: CALL M,nn (M:6 T:17 steps:14)
	 944,  // FD: FD prefix (M:1 T:4 steps:1)
	 945,  // FE: CP n (M:2 T:7 steps:4)
	 949,  // FF: RST 38h (M:4 T:11 steps:8)
};

static const uint16_t _z80_ed_optable[256] = {
	 957,  // 00: ED NOP (M:1 T:4 steps:1)
	 957,  // 01: ED NOP (M:1 T:4 steps:1)
	 957,  // 02: ED NOP (M:1 T:4 steps:1)
	 957,  // 03: ED NOP (M:1 T:4 steps:1)
	 957,  // 04: ED NOP (M:1 T:4 steps:1)
	 957,  // 05: ED NOP (M:1 T:4 steps:1)
	 957,  // 06: ED NOP (M:1 T:4 steps:1)
	 957,  // 07: ED NOP (M:1 T:4 steps:1)
	 957,  // 08: ED NOP (M:1 T:4 steps:1)
	 957,  // 09: ED NOP (M:1 T:4 steps:1)
	 957,  // 0A: ED NOP (M:1 T:4 steps:1)
	 957,  // 0B: ED NOP (M:1 T:4 steps:1)
	 957,  // 0C: ED NOP (M:1 T:4 steps:1)
	 957,  // 0D: ED NOP (M:1 T:4 steps:1)
	 957,  // 0E: ED NOP (M:1 T:4 steps:1)
	 957,  // 0F: ED NOP (M:1 T:4 steps:1)
	 957,  // 10: ED NOP (M:1 T:4 steps:1)
	 957,  // 11: ED NOP (M:1 T:4 steps:1)
	 957,  // 12: ED NOP (M:1 T:4 steps:1)
	 957,  // 13: ED NOP (M:1 T:4 steps:1)
	 957,  // 14: ED NOP (M:1 T:4 steps:1)
	 957,  // 15: ED NOP (M:1 T:4 steps:1)
	 957,  // 16: ED NOP (M:1 T:4 steps:1)
	 957,  // 17: ED NOP (M:1 T:4 steps:1)
	 957,  // 18: ED NOP (M:1 T:4 steps:1)
	 957,  // 19: ED NOP (M:1 T:4 steps:1)
	 957,  // 1A: ED NOP (M:1 T:4 steps:1)
	 957,  // 1B: ED NOP (M:1 T:4 steps:1)
	 957,  // 1C: ED NOP (M:1 T:4 steps:1)
	 957,  // 1D: ED NOP (M:1 T:4 steps:1)
	 957,  // 1E: ED NOP (M:1 T:4 steps:1)
	 957,  // 1F: ED NOP (M:1 T:4 steps:1)
	 957,  // 20: ED NOP (M:1 T:4 steps:1)
	 957,  // 21: ED NOP (M:1 T:4 steps:1)
	 957,  // 22: ED NOP (M:1 T:4 steps:1)
	 957,  // 23: ED NOP (M:1 T:4 steps:1)
	 957,  // 24: ED NOP (M:1 T:4 steps:1)
	 957,  // 25: ED NOP (M:1 T:4 steps:1)
	 957,  // 26: ED NOP (M:1 T:4 steps:1)
	 957,  // 27: ED NOP (M:1 T:4 steps:1)
	 957,  // 28: ED NOP (M:1 T:4 steps:1)
	 957,  // 29: ED NOP (M:1 T:4 steps:1)
	 957,  // 2A: ED NOP (M:1 T:4 steps:1)
	 957,  // 2B: ED NOP (M:1 T:4 steps:1)
	 957,  // 2C: ED NOP (M:1 T:4 steps:1)
	 957,  // 2D: ED NOP (M:1 T:4 steps:1)
	 957,  // 2E: ED NOP (M:1 T:4 steps:1)
	 957,  // 2F: ED NOP (M:1 T:4 steps:1)
	 957,  // 30: ED NOP (M:1 T:4 steps:1)
	 957,  // 31: ED NOP (M:1 T:4 steps:1)
	 957,  // 32: ED NOP (M:1 T:4 steps:1)
	 957,  // 33: ED NOP (M:1 T:4 steps:1)
	 957,  // 34: ED NOP (M:1 T:4 steps:1)
	 957,  // 35: ED NOP (M:1 T:4 steps:1)
	 957,  // 36: ED NOP (M:1 T:4 steps:1)
	 957,  // 37: ED NOP (M:1 T:4 steps:1)
	 957,  // 38: ED NOP (M:1 T:4 steps:1)
	 957,  // 39: ED NOP (M:1 T:4 steps:1)
	 957,  // 3A: ED NOP (M:1 T:4 steps:1)
	 957,  // 3B: ED NOP (M:1 T:4 steps:1)
	 957,  // 3C: ED NOP (M:1 T:4 steps:1)
	 957,  // 3D: ED NOP (M:1 T:4 steps:1)
	 957,  // 3E: ED NOP (M:1 T:4 steps:1)
	 957,  // 3F: ED NOP (M:1 T:4 steps:1)
	 958,  // 40: IN B,(C) (M:2 T:8 steps:5)
	 963,  // 41: OUT (C),B (M:2 T:8 steps:5)
	 968,  // 42: SBC HL,BC (M:2 T:11 steps:8)
	 976,  // 43: LD (nn),BC (M:5 T:16 steps:13)
	 989,  // 44: NEG (M:1 T:4 steps:1)
	 990,  // 45: RETN (M:3 T:10 steps:7)
	 997,  // 46: IM 0 (M:1 T:4 steps:1)
	 998,  // 47: LD I,A (M:2 T:5 steps:2)
	1000,  // 48: IN C,(C) (M:2 T:8 steps:5)
	1005,  // 49: OUT (C),C (M:2 T:8 steps:5)
	1010,  // 4A: ADC HL,BC (M:2 T:11 steps:8)
	1018,  // 4B: LD BC,(nn) (M:5 T:16 steps:13)
	 989,  // 4C: NEG (M:1 T:4 steps:1)
	1031,  // 4D: RETI (M:3 T:10 steps:7)
	1038,  // 4E: IM 0 (M:1 T:4 steps:1)
	1039,  // 4F: LD R,A (M:2 T:5 steps:2)
	1041,  // 50: IN D,(C) (M:2 T:8 steps:5)
	1046,  // 51: OUT (C),D (M:2 T:8 steps:5)
	1051,  // 52: SBC HL,DE (M:2 T:11 steps:8)
	1059,  // 53: LD (nn),DE (M:5 T:16 steps:13)
	 989,  // 54: NEG (M:1 T:4 steps:1)
	1031,  // 55: RETI (M:3 T:10 steps:7)
	1072,  // 56: IM 1 (M:1 T:4 steps:1)
	1073,  // 57: LD A,I (M:2 T:5 steps:2)
	1075,  // 58: IN E,(C) (M:2 T:8 steps:5)
	1080,  // 59: OUT (C),E (M:2 T:8 steps:5)
	1085,  // 5A: ADC HL,DE (M:2 T:11 steps:8)
	1093,  // 5B: LD DE,(nn) (M:5 T:16 steps:13)
	 989,  // 5C: NEG (M:1 T:4 steps:1)
	1031,  // 5D: RETI (M:3 T:10 steps:7)
	1106,  // 5E: IM 2 (M:1 T:4 steps:1)
	1107,  // 5F: LD A,R (M:2 T:5 steps:2)
	1109,  // 60: IN H,(C) (M:2 T:8 steps:5)
	1114,  // 61: OUT (C),H (M:2 T:8 steps:5)
	1119,  // 62: SBC HL,HL (M:2 T:11 steps:8)
	1127,  // 63: LD (nn),HL (M:5 T:16 steps:13)
	 989,  // 64: NEG (M:1 T:4 steps:1)
	1031,  // 65: RETI (M:3 T:10 steps:7)
	1140,  // 66: IM 0 (M:1 T:4 steps:1)
	1141,  // 67: RRD (M:4 T:14 steps:11)
	1152,  // 68: IN L,(C) (M:2 T:8 steps:5)
	1157,  // 69: OUT (C),L (M:2 T:8 steps:5)
	1162,  // 6A: ADC HL,HL (M:2 T:11 steps:8)
	1170,  // 6B: LD HL,(nn) (M:5 T:16 steps:13)
	 989,  // 6C: NEG (M:1 T:4 steps:1)
	1031,  // 6D: RETI (M:3 T:10 steps:7)
	1183,  // 6E: IM 0 (M:1 T:4 steps:1)
	1184,  // 6F: RLD (M:4 T:14 steps:11)
	1195,  // 70: IN (C) (M:2 T:8 steps:5)
	1200,  // 71: OUT (C),0 (M:2 T:8 steps:5)
	1205,  // 72: SBC HL,SP (M:2 T:11 steps:8)
	1213,  // 73: LD (nn),SP (M:5 T:16 steps:13)
	 989,  // 74: NEG (M:1 T:4 steps:1)
	1031,  // 75: RETI (M:3 T:10 steps:7)
	1226,  // 76: IM 1 (M:1 T:4 steps:1)
	 957,  // 77: ED NOP (M:1 T:4 steps:1)
	1227,  // 78: IN A,(C) (M:2 T:8 steps:5)
	1232,  // 79: OUT (C),A (M:2 T:8 steps:5)
	1237,  // 7A: ADC HL,SP (M:2 T:11 steps:8)
	1245,  // 7B: LD SP,(nn) (M:5 T:16 steps:13)
	 989,  // 7C: NEG (M:1 T:4 steps:1)
	1031,  // 7D: RETI (M:3 T:10 steps:7)
	1258,  // 7E: IM 2 (M:1 T:4 steps:1)
	 957,  // 7F: ED NOP (M:1 T:4 steps:1)
	 957,  // 80: ED NOP (M:1 T:4 steps:1)
	 957,  // 81: ED NOP (M:1 T:4 steps:1)
	 957,  // 82: ED NOP (M:1 T:4 steps:1)
	 957,  // 83: ED NOP (M:1 T:4 steps:1)
	 957,  // 84: ED NOP (M:1 T:4 steps:1)
	 957,  // 85: ED NOP (M:1 T:4 steps:1)
	 957,  // 86: ED NOP (M:1 T:4 steps:1)
	 957,  // 87: ED NOP (M:1 T:4 steps:1)
	 957,  // 88: ED NOP (M:1 T:4 steps:1)
	 957,  // 89: ED NOP (M:1 T:4 steps:1)
	 957,  // 8A: ED NOP (M:1 T:4 steps:1)
	 957,  // 8B: ED NOP (M:1 T:4 steps:1)
	 957,  // 8C: ED NOP (M:1 T:4 steps:1)
	 957,  // 8D: ED NOP (M:1 T:4 steps:1)
	 957,  // 8E: ED NOP (M:1 T:4 steps:1)
	 957,  // 8F: ED NOP (M:1 T:4 steps:1)
	 957,  // 90: ED NOP (M:1 T:4 steps:1)
	 957,  // 91: ED NOP (M:1 T:4 steps:1)
	 957,  // 92: ED NOP (M:1 T:4 steps:1)
	 957,  // 93: ED NOP (M:1 T:4 steps:1)
	 957,  // 94: ED NOP (M:1 T:4 steps:1)
	 957,  // 95: ED NOP (M:1 T:4 steps:1)
	 957,  // 96: ED NOP (M:1 T:4 steps:1)
	 957,  // 97: ED NOP (M:1 T:4 steps:1)
	 957,  // 98: ED NOP (M:1 T:4 steps:1)
	 957,  // 99: ED NOP (M:1 T:4 steps:1)
	 957,  // 9A: ED NOP (M:1 T:4 steps:1)
	 957,  // 9B: ED NOP (M:1 T:4 steps:1)
	 957,  // 9C: ED NOP (M:1 T:4 steps:1)
	 957,  // 9D: ED NOP (M:1 T:4 steps:1)
	 957,  // 9E: ED NOP (M:1 T:4 steps:1)
	 957,  // 9F: ED NOP (M:1 T:4 steps:1)
	1259,  // A0: LDI (M:4 T:12 steps:9)
	1268,  // A1: CPI (M:3 T:12 steps:9)
	1277,  // A2: INI (M:4 T:12 steps:9)
	1286,  // A3: OUTI (M:4 T:12 steps:9)
	 957,  // A4: ED NOP (M:1 T:4 steps:1)
	 957,  // A5: ED NOP (M:1 T:4 steps:1)
	 957,  // A6: ED NOP (M:1 T:4 steps:1)
	 957,  // A7: ED NOP (M:1 T:4 steps:1)
	1295,  // A8: LDD (M:4 T:12 steps:9)
	1304,  // A9: CPD (M:3 T:12 steps:9)
	1313,  // AA: IND (M:4 T:12 steps:9)
	1322,  // AB: OUTD (M:4 T:12 steps:9)
	 957,  // AC: ED NOP (M:1 T:4 steps:1)
	 957,  // AD: ED NOP (M:1 T:4 steps:1)
	 957,  // AE: ED NOP (M:1 T:4 steps:1)
	 957,  // AF: ED NOP (M:1 T:4 steps:1)
	1331,  // B0: LDIR (M:5 T:17 steps:14)
	1345,  // B1: CPIR (M:4 T:17 steps:14)
	1359,  // B2: INIR (M:5 T:17 steps:14)
	1373,  // B3: OTIR (M:5 T:17 steps:14)
	 957,  // B4: ED NOP (M:1 T:4 steps:1)
	 957,  // B5: ED NOP (M:1 T:4 steps:1)
	 957,  // B6: ED NOP (M:1 T:4 steps:1)
	 957,  // B7: ED NOP (M:1 T:4 steps:1)
	1387,  // B8: LDDR (M:5 T:17 steps:14)
	1401,  // B9: CPDR (M:4 T:17 steps:14)
	1415,  // BA: INDR (M:5 T:17 steps:14)
	1429,  // BB: OTDR (M:5 T:17 steps:14)
	 957,  // BC: ED NOP (M:1 T:4 steps:1)
	 957,  // BD: ED NOP (M:1 T:4 steps:1)
	 957,  // BE: ED NOP (M:1 T:4 steps:1)
	 957,  // BF: ED NOP (M:1 T:4 steps:1)
	 957,  // C0: ED NOP (M:1 T:4 steps:1)
	 957,  // C1: ED NOP (M:1 T:4 steps:1)
	 957,  // C2: ED NOP (M:1 T:4 steps:1)
	 957,  // C3: ED NOP (M:1 T:4 steps:1)
	 957,  // C4: ED NOP (M:1 T:4 steps:1)
	 957,  // C5: ED NOP (M:1 T:4 steps:1)
	 957,  // C6: ED NOP (M:1 T:4 steps:1)
	 957,  // C7: ED NOP (M:1 T:4 steps:1)
	 957,  // C8: ED NOP (M:1 T:4 steps:1)
	 957,  // C9: ED NOP (M:1 T:4 steps:1)
	 957,  // CA: ED NOP (M:1 T:4 steps:1)
	 957,  // CB: ED NOP (M:1 T:4 steps:1)
	 957,  // CC: ED NOP (M:1 T:4 steps:1)
	 957,  // CD: ED NOP (M:1 T:4 steps:1)
	 957,  // CE: ED NOP (M:1 T:4 steps:1)
	 957,  // CF: ED NOP (M:1 T:4 steps:1)
	 957,  // D0: ED NOP (M:1 T:4 steps:1)
	 957,  // D1: ED NOP (M:1 T:4 steps:1)
	 957,  // D2: ED NOP (M:1 T:4 steps:1)
	 957,  // D3: ED NOP (M:1 T:4 steps:1)
	 957,  // D4: ED NOP (M:1 T:4 steps:1)
	 957,  // D5: ED NOP (M:1 T:4 steps:1)
	 957,  // D6: ED NOP (M:1 T:4 steps:1)
	 957,  // D7: ED NOP (M:1 T:4 steps:1)
	 957,  // D8: ED NOP (M:1 T:4 steps:1)
	 957,  // D9: ED NOP (M:1 T:4 steps:1)
	 957,  // DA: ED NOP (M:1 T:4 steps:1)
	 957,  // DB: ED NOP (M:1 T:4 steps:1)
	 957,  // DC: ED NOP (M:1 T:4 steps:1)
	 957,  // DD: ED NOP (M:1 T:4 steps:1)
	 957,  // DE: ED NOP (M:1 T:4 steps:1)
	 957,  // DF: ED NOP (M:1 T:4 steps:1)
	 957,  // E0: ED NOP (M:1 T:4 steps:1)
	 957,  // E1: ED NOP (M:1 T:4 steps:1)
	 957,  // E2: ED NOP (M:1 T:4 steps:1)
	 957,  // E3: ED NOP (M:1 T:4 steps:1)
	 957,  // E4: ED NOP (M:1 T:4 steps:1)
	 957,  // E5: ED NOP (M:1 T:4 steps:1)
	 957,  // E6: ED NOP (M:1 T:4 steps:1)
	 957,  // E7: ED NOP (M:1 T:4 steps:1)
	 957,  // E8: ED NOP (M:1 T:4 steps:1)
	 957,  // E9: ED NOP (M:1 T:4 steps:1)
	 957,  // EA: ED NOP (M:1 T:4 steps:1)
	 957,  // EB: ED NOP (M:1 T:4 steps:1)
	 957,  // EC: ED NOP (M:1 T:4 steps:1)
	 957,  // ED: ED NOP (M:1 T:4 steps:1)
	 957,  // EE: ED NOP (M:1 T:4 steps:1)
	 957,  // EF: ED NOP (M:1 T:4 steps:1)
	 957,  // F0: ED NOP (M:1 T:4 steps:1)
	 957,  // F1: ED NOP (M:1 T:4 steps:1)
	 957,  // F2: ED NOP (M:1 T:4 steps:1)
	 957,  // F3: ED NOP (M:1 T:4 steps:1)
	 957,  // F4: ED NOP (M:1 T:4 steps:1)
	 957,  // F5: ED NOP (M:1 T:4 steps:1)
	 957,  // F6: ED NOP (M:1 T:4 steps:1)
	 957,  // F7: ED NOP (M:1 T:4 steps:1)
	 957,  // F8: ED NOP (M:1 T:4 steps:1)
	 957,  // F9: ED NOP (M:1 T:4 steps:1)
	 957,  // FA: ED NOP (M:1 T:4 steps:1)
	 957,  // FB: ED NOP (M:1 T:4 steps:1)
	 957,  // FC: ED NOP (M:1 T:4 steps:1)
	 957,  // FD: ED NOP (M:1 T:4 steps:1)
	 957,  // FE: ED NOP (M:1 T:4 steps:1)
	 957,  // FF: ED NOP (M:1 T:4 steps:1)
};

static const uint16_t _z80_special_optable[_Z80_OPSTATE_NUM_SPECIAL_OPS] = {
	1443,  // 00: cb (M:1 T:4 steps:1)
	1444,  // 01: cbhl (M:3 T:11 steps:8)
	1452,  // 02: ddfdcb (M:6 T:18 steps:15)
	1467,  // 03: int_im0 (M:6 T:9 steps:6)
	1473,  // 04: int_im1 (M:7 T:16 steps:13)
	1486,  // 05: int_im2 (M:9 T:22 steps:19)
	1505,  // 06: nmi (M:5 T:14 steps:11)
};

// initiate refresh cycle
static inline uint64_t _z80_refresh(z80_t* cpu, uint64_t pins) {
	pins = _z80_set_ab_x(pins, cpu->ir, Z80_MREQ | Z80_RFSH);
	cpu->r = (cpu->r & 0x80) | ((cpu->r + 1) & 0x7F);
	return pins;
}

// initiate a fetch machine cycle for regular (non-prefixed) instructions, or initiate interrupt handling
static inline uint64_t _z80_fetch(z80_t* cpu, uint64_t pins) {
	cpu->hlx_idx = 0;
	cpu->prefix_active = false;
	// shortcut no interrupts requested
	if (cpu->int_bits == 0) {
		cpu->step = 0xFFFF;
		return _z80_set_ab_x(pins, cpu->pc++, Z80_M1 | Z80_MREQ | Z80_RD);
	}
	else if (cpu->int_bits & Z80_NMI) {
		// non-maskable interrupt starts with a regular M1 machine cycle
		cpu->step = _z80_special_optable[_Z80_OPSTATE_SLOT_NMI];
		cpu->int_bits = 0;
		if (pins & Z80_HALT) {
			pins &= ~Z80_HALT;
			cpu->pc++;
		}
		// NOTE: PC is *not* incremented!
		return _z80_set_ab_x(pins, cpu->pc, Z80_M1 | Z80_MREQ | Z80_RD);
	}
	else if (cpu->int_bits & Z80_INT) {
		if (cpu->iff1) {
			// maskable interrupts start with a special M1 machine cycle which
			// doesn't fetch the next opcode, but instead activate the
			// pins M1|IOQR to request a special byte which is handled differently
			// depending on interrupt mode
			cpu->step = _z80_special_optable[_Z80_OPSTATE_SLOT_INT_IM0 + cpu->im];
			cpu->int_bits = 0;
			if (pins & Z80_HALT) {
				pins &= ~Z80_HALT;
				cpu->pc++;
			}
			// NOTE: PC is not incremented, and no pins are activated here
			return pins;
		}
		else {
			// oops, maskable interrupt requested but disabled
			cpu->step = 0xFFFF;
			return _z80_set_ab_x(pins, cpu->pc++, Z80_M1 | Z80_MREQ | Z80_RD);
		}
	}
	else {
		_Z80_UNREACHABLE;
		return pins;
	}
}

static inline uint64_t _z80_fetch_cb(z80_t* cpu, uint64_t pins) {
	cpu->prefix_active = true;
	if (cpu->hlx_idx > 0) {
		// this is a DD+CB / FD+CB instruction, continue
		// execution on the special DDCB/FDCB decoder block which
		// loads the d-offset first and then the opcode in a
		// regular memory read machine cycle
		cpu->step = _z80_special_optable[_Z80_OPSTATE_SLOT_DDFDCB];
	}
	else {
		// this is a regular CB-prefixed instruction, continue
		// execution on a special fetch machine cycle which doesn't
		// handle DD/FD prefix and then branches either to the
		// special CB or CBHL decoder block
		cpu->step = 21; // => step 22: opcode fetch for CB prefixed instructions
		pins = _z80_set_ab_x(pins, cpu->pc++, Z80_M1 | Z80_MREQ | Z80_RD);
	}
	return pins;
}

static inline uint64_t _z80_fetch_dd(z80_t* cpu, uint64_t pins) {
	cpu->step = 2;   // => step 3: opcode fetch for DD/FD prefixed instructions
	cpu->hlx_idx = 1;
	cpu->prefix_active = true;
	return _z80_set_ab_x(pins, cpu->pc++, Z80_M1 | Z80_MREQ | Z80_RD);
}

static inline uint64_t _z80_fetch_fd(z80_t* cpu, uint64_t pins) {
	cpu->step = 2;   // => step 3: opcode fetch for DD/FD prefixed instructions
	cpu->hlx_idx = 2;
	cpu->prefix_active = true;
	return _z80_set_ab_x(pins, cpu->pc++, Z80_M1 | Z80_MREQ | Z80_RD);
}

static inline uint64_t _z80_fetch_ed(z80_t* cpu, uint64_t pins) {
	cpu->step = 24; // => step 25: opcode fetch for ED prefixed instructions
	cpu->hlx_idx = 0;
	cpu->prefix_active = true;
	return _z80_set_ab_x(pins, cpu->pc++, Z80_M1 | Z80_MREQ | Z80_RD);
}

uint64_t z80_prefetch(z80_t* cpu, uint16_t new_pc) {
	cpu->pc = new_pc;
	// overlapped M1:T1 of the NOP instruction to initiate opcode fetch at new pc
	cpu->step = _z80_optable[0] + 1;
	return 0;
}



// pin helper macros
#define _sa(ab)             pins=_z80_set_ab(pins,ab)
#define _sax(ab,x)          pins=_z80_set_ab_x(pins,ab,x)
#define _sad(ab,d)          pins=_z80_set_ab_db(pins,ab,d)
#define _sadx(ab,d,x)       pins=_z80_set_ab_db_x(pins,ab,d,x)
#define _gd()               _z80_get_db(pins)

// high level helper macros
#define _skip(n)        cpu->step+=(n);
#define _fetch_dd()     pins=_z80_fetch_dd(cpu,pins);
#define _fetch_fd()     pins=_z80_fetch_fd(cpu,pins);
#define _fetch_ed()     pins=_z80_fetch_ed(cpu,pins);
#define _fetch_cb()     pins=_z80_fetch_cb(cpu,pins);
#define _mread(ab)      _sax(ab,Z80_MREQ|Z80_RD)
#define _mwrite(ab,d)   _sadx(ab,d,Z80_MREQ|Z80_WR)
#define _ioread(ab)     _sax(ab,Z80_IORQ|Z80_RD)
#define _iowrite(ab,d)  _sadx(ab,d,Z80_IORQ|Z80_WR)
#define _wait()         {if(pins&Z80_WAIT)goto track_int_bits;}
#define _cc_nz          (!(cpu->f&Z80_ZF))
#define _cc_z           (cpu->f&Z80_ZF)
#define _cc_nc          (!(cpu->f&Z80_CF))
#define _cc_c           (cpu->f&Z80_CF)
#define _cc_po          (!(cpu->f&Z80_PF))
#define _cc_pe          (cpu->f&Z80_PF)
#define _cc_p           (!(cpu->f&Z80_SF))
#define _cc_m           (cpu->f&Z80_SF)

uint64_t z80_tick(z80_t* cpu, uint64_t pins) {
	pins &= ~(Z80_CTRL_PIN_MASK | Z80_RETI);
	switch (cpu->step) {
		//=== shared fetch machine cycle for non-DD/FD-prefixed ops
		// M1/T2: load opcode from data bus
	case 0: _wait(); cpu->opcode = _gd(); goto step_next;
		// M1/T3: refresh cycle
	case 1: pins = _z80_refresh(cpu, pins); goto step_next;
		// M1/T4: branch to instruction 'payload'
	case 2: {
		cpu->step = _z80_optable[cpu->opcode];
		// preload effective address for (HL) ops
		cpu->addr = cpu->hl;
	} goto step_next;
		  //=== shared fetch machine cycle for DD/FD-prefixed ops
		  // M1/T2: load opcode from data bus
	case 3: _wait(); cpu->opcode = _gd(); goto step_next;
		// M1/T3: refresh cycle
	case 4: pins = _z80_refresh(cpu, pins); goto step_next;
		// M1/T4: branch to instruction 'payload'
	case 5: {
		cpu->step = _z80_ddfd_optable[cpu->opcode];
		cpu->addr = cpu->hlx[cpu->hlx_idx].hl;
	} goto step_next;
		  //=== optional d-loading cycle for (IX+d), (IY+d)
		  //--- mread
	case 6: goto step_next;
	case 7: _wait(); _mread(cpu->pc++); goto step_next;
	case 8: cpu->addr += (int8_t)_gd(); cpu->wz = cpu->addr; goto step_next;
		//--- filler ticks
	case 9: goto step_next;
	case 10: goto step_next;
	case 11: goto step_next;
	case 12: goto step_next;
	case 13: {
		// branch to actual instruction
		cpu->step = _z80_optable[cpu->opcode];
	} goto step_next;
		   //=== special case d-loading cycle for (IX+d),n where the immediate load
		   //    is hidden in the d-cycle load
		   //--- mread for d offset
	case 14: goto step_next;
	case 15: _wait(); _mread(cpu->pc++); goto step_next;
	case 16: cpu->addr += (int8_t)_gd(); cpu->wz = cpu->addr; goto step_next;
		//--- mread for n
	case 17: goto step_next;
	case 18: _wait(); _mread(cpu->pc++); goto step_next;
	case 19: cpu->dlatch = _gd(); goto step_next;
		//--- filler tick
	case 20: goto step_next;
	case 21: {
		// branch to ld (hl),n and skip the original mread cycle for loading 'n'
		cpu->step = _z80_optable[cpu->opcode] + 3;
	} goto step_next;
		   //=== special opcode fetch machine cycle for CB-prefixed instructions
	case 22: _wait(); cpu->opcode = _gd(); goto step_next;
	case 23: pins = _z80_refresh(cpu, pins); goto step_next;
	case 24: {
		if ((cpu->opcode & 7) == 6) {
			// this is a (HL) instruction
			cpu->addr = cpu->hl;
			cpu->step = _z80_special_optable[_Z80_OPSTATE_SLOT_CBHL];
		}
		else {
			cpu->step = _z80_special_optable[_Z80_OPSTATE_SLOT_CB];
		}
	} goto step_next;
		   //=== special opcode fetch machine cycle for ED-prefixed instructions
		   // M1/T2: load opcode from data bus
	case 25: _wait(); cpu->opcode = _gd(); goto step_next;
		// M1/T3: refresh cycle
	case 26: pins = _z80_refresh(cpu, pins); goto step_next;
		// M1/T4: branch to instruction 'payload'
	case 27: cpu->step = _z80_ed_optable[cpu->opcode]; goto step_next;
		//=== from here on code-generated

		//  00: NOP (M:1 T:4)
		// -- overlapped
	case   28: goto fetch_next;

		//  01: LD BC,nn (M:3 T:10)
		// -- mread
	case   29: goto step_next;
	case   30: _wait(); _mread(cpu->pc++); goto step_next;
	case   31: cpu->c = _gd(); goto step_next;
		// -- mread
	case   32: goto step_next;
	case   33: _wait(); _mread(cpu->pc++); goto step_next;
	case   34: cpu->b = _gd(); goto step_next;
		// -- overlapped
	case   35: goto fetch_next;

		//  02: LD (BC),A (M:2 T:7)
		// -- mwrite
	case   36: goto step_next;
	case   37: _wait(); _mwrite(cpu->bc, cpu->a); cpu->wzl = cpu->c + 1; cpu->wzh = cpu->a; goto step_next;
	case   38: goto step_next;
		// -- overlapped
	case   39: goto fetch_next;

		//  03: INC BC (M:2 T:6)
		// -- generic
	case   40: cpu->bc++; goto step_next;
	case   41: goto step_next;
		// -- overlapped
	case   42: goto fetch_next;

		//  04: INC B (M:1 T:4)
		// -- overlapped
	case   43: cpu->b = _z80_inc8(cpu, cpu->b); goto fetch_next;

		//  05: DEC B (M:1 T:4)
		// -- overlapped
	case   44: cpu->b = _z80_dec8(cpu, cpu->b); goto fetch_next;

		//  06: LD B,n (M:2 T:7)
		// -- mread
	case   45: goto step_next;
	case   46: _wait(); _mread(cpu->pc++); goto step_next;
	case   47: cpu->b = _gd(); goto step_next;
		// -- overlapped
	case   48: goto fetch_next;

		//  07: RLCA (M:1 T:4)
		// -- overlapped
	case   49: _z80_rlca(cpu); goto fetch_next;

		//  08: EX AF,AF' (M:1 T:4)
		// -- overlapped
	case   50: _z80_ex_af_af2(cpu); goto fetch_next;

		//  09: ADD HL,BC (M:2 T:11)
		// -- generic
	case   51: _z80_add16(cpu, cpu->bc); goto step_next;
	case   52: goto step_next;
	case   53: goto step_next;
	case   54: goto step_next;
	case   55: goto step_next;
	case   56: goto step_next;
	case   57: goto step_next;
		// -- overlapped
	case   58: goto fetch_next;

		//  0A: LD A,(BC) (M:2 T:7)
		// -- mread
	case   59: goto step_next;
	case   60: _wait(); _mread(cpu->bc); goto step_next;
	case   61: cpu->a = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case   62: goto fetch_next;

		//  0B: DEC BC (M:2 T:6)
		// -- generic
	case   63: cpu->bc--; goto step_next;
	case   64: goto step_next;
		// -- overlapped
	case   65: goto fetch_next;

		//  0C: INC C (M:1 T:4)
		// -- overlapped
	case   66: cpu->c = _z80_inc8(cpu, cpu->c); goto fetch_next;

		//  0D: DEC C (M:1 T:4)
		// -- overlapped
	case   67: cpu->c = _z80_dec8(cpu, cpu->c); goto fetch_next;

		//  0E: LD C,n (M:2 T:7)
		// -- mread
	case   68: goto step_next;
	case   69: _wait(); _mread(cpu->pc++); goto step_next;
	case   70: cpu->c = _gd(); goto step_next;
		// -- overlapped
	case   71: goto fetch_next;

		//  0F: RRCA (M:1 T:4)
		// -- overlapped
	case   72: _z80_rrca(cpu); goto fetch_next;

		//  10: DJNZ d (M:4 T:13)
		// -- generic
	case   73: goto step_next;
		// -- mread
	case   74: goto step_next;
	case   75: _wait(); _mread(cpu->pc++); goto step_next;
	case   76: cpu->dlatch = _gd(); if (--cpu->b == 0) { _skip(5); }; goto step_next;
		// -- generic
	case   77: cpu->pc += (int8_t)cpu->dlatch; cpu->wz = cpu->pc; goto step_next;
	case   78: goto step_next;
	case   79: goto step_next;
	case   80: goto step_next;
	case   81: goto step_next;
		// -- overlapped
	case   82: goto fetch_next;

		//  11: LD DE,nn (M:3 T:10)
		// -- mread
	case   83: goto step_next;
	case   84: _wait(); _mread(cpu->pc++); goto step_next;
	case   85: cpu->e = _gd(); goto step_next;
		// -- mread
	case   86: goto step_next;
	case   87: _wait(); _mread(cpu->pc++); goto step_next;
	case   88: cpu->d = _gd(); goto step_next;
		// -- overlapped
	case   89: goto fetch_next;

		//  12: LD (DE),A (M:2 T:7)
		// -- mwrite
	case   90: goto step_next;
	case   91: _wait(); _mwrite(cpu->de, cpu->a); cpu->wzl = cpu->e + 1; cpu->wzh = cpu->a; goto step_next;
	case   92: goto step_next;
		// -- overlapped
	case   93: goto fetch_next;

		//  13: INC DE (M:2 T:6)
		// -- generic
	case   94: cpu->de++; goto step_next;
	case   95: goto step_next;
		// -- overlapped
	case   96: goto fetch_next;

		//  14: INC D (M:1 T:4)
		// -- overlapped
	case   97: cpu->d = _z80_inc8(cpu, cpu->d); goto fetch_next;

		//  15: DEC D (M:1 T:4)
		// -- overlapped
	case   98: cpu->d = _z80_dec8(cpu, cpu->d); goto fetch_next;

		//  16: LD D,n (M:2 T:7)
		// -- mread
	case   99: goto step_next;
	case  100: _wait(); _mread(cpu->pc++); goto step_next;
	case  101: cpu->d = _gd(); goto step_next;
		// -- overlapped
	case  102: goto fetch_next;

		//  17: RLA (M:1 T:4)
		// -- overlapped
	case  103: _z80_rla(cpu); goto fetch_next;

		//  18: JR d (M:3 T:12)
		// -- mread
	case  104: goto step_next;
	case  105: _wait(); _mread(cpu->pc++); goto step_next;
	case  106: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case  107: cpu->pc += (int8_t)cpu->dlatch; cpu->wz = cpu->pc; goto step_next;
	case  108: goto step_next;
	case  109: goto step_next;
	case  110: goto step_next;
	case  111: goto step_next;
		// -- overlapped
	case  112: goto fetch_next;

		//  19: ADD HL,DE (M:2 T:11)
		// -- generic
	case  113: _z80_add16(cpu, cpu->de); goto step_next;
	case  114: goto step_next;
	case  115: goto step_next;
	case  116: goto step_next;
	case  117: goto step_next;
	case  118: goto step_next;
	case  119: goto step_next;
		// -- overlapped
	case  120: goto fetch_next;

		//  1A: LD A,(DE) (M:2 T:7)
		// -- mread
	case  121: goto step_next;
	case  122: _wait(); _mread(cpu->de); goto step_next;
	case  123: cpu->a = _gd(); cpu->wz = cpu->de + 1; goto step_next;
		// -- overlapped
	case  124: goto fetch_next;

		//  1B: DEC DE (M:2 T:6)
		// -- generic
	case  125: cpu->de--; goto step_next;
	case  126: goto step_next;
		// -- overlapped
	case  127: goto fetch_next;

		//  1C: INC E (M:1 T:4)
		// -- overlapped
	case  128: cpu->e = _z80_inc8(cpu, cpu->e); goto fetch_next;

		//  1D: DEC E (M:1 T:4)
		// -- overlapped
	case  129: cpu->e = _z80_dec8(cpu, cpu->e); goto fetch_next;

		//  1E: LD E,n (M:2 T:7)
		// -- mread
	case  130: goto step_next;
	case  131: _wait(); _mread(cpu->pc++); goto step_next;
	case  132: cpu->e = _gd(); goto step_next;
		// -- overlapped
	case  133: goto fetch_next;

		//  1F: RRA (M:1 T:4)
		// -- overlapped
	case  134: _z80_rra(cpu); goto fetch_next;

		//  20: JR NZ,d (M:3 T:12)
		// -- mread
	case  135: goto step_next;
	case  136: _wait(); _mread(cpu->pc++); goto step_next;
	case  137: cpu->dlatch = _gd(); if (!(_cc_nz)) { _skip(5); }; goto step_next;
		// -- generic
	case  138: cpu->pc += (int8_t)cpu->dlatch; cpu->wz = cpu->pc; goto step_next;
	case  139: goto step_next;
	case  140: goto step_next;
	case  141: goto step_next;
	case  142: goto step_next;
		// -- overlapped
	case  143: goto fetch_next;

		//  21: LD HL,nn (M:3 T:10)
		// -- mread
	case  144: goto step_next;
	case  145: _wait(); _mread(cpu->pc++); goto step_next;
	case  146: cpu->hlx[cpu->hlx_idx].l = _gd(); goto step_next;
		// -- mread
	case  147: goto step_next;
	case  148: _wait(); _mread(cpu->pc++); goto step_next;
	case  149: cpu->hlx[cpu->hlx_idx].h = _gd(); goto step_next;
		// -- overlapped
	case  150: goto fetch_next;

		//  22: LD (nn),HL (M:5 T:16)
		// -- mread
	case  151: goto step_next;
	case  152: _wait(); _mread(cpu->pc++); goto step_next;
	case  153: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  154: goto step_next;
	case  155: _wait(); _mread(cpu->pc++); goto step_next;
	case  156: cpu->wzh = _gd(); goto step_next;
		// -- mwrite
	case  157: goto step_next;
	case  158: _wait(); _mwrite(cpu->wz++, cpu->hlx[cpu->hlx_idx].l); goto step_next;
	case  159: goto step_next;
		// -- mwrite
	case  160: goto step_next;
	case  161: _wait(); _mwrite(cpu->wz, cpu->hlx[cpu->hlx_idx].h); goto step_next;
	case  162: goto step_next;
		// -- overlapped
	case  163: goto fetch_next;

		//  23: INC HL (M:2 T:6)
		// -- generic
	case  164: cpu->hlx[cpu->hlx_idx].hl++; goto step_next;
	case  165: goto step_next;
		// -- overlapped
	case  166: goto fetch_next;

		//  24: INC H (M:1 T:4)
		// -- overlapped
	case  167: cpu->hlx[cpu->hlx_idx].h = _z80_inc8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  25: DEC H (M:1 T:4)
		// -- overlapped
	case  168: cpu->hlx[cpu->hlx_idx].h = _z80_dec8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  26: LD H,n (M:2 T:7)
		// -- mread
	case  169: goto step_next;
	case  170: _wait(); _mread(cpu->pc++); goto step_next;
	case  171: cpu->hlx[cpu->hlx_idx].h = _gd(); goto step_next;
		// -- overlapped
	case  172: goto fetch_next;

		//  27: DAA (M:1 T:4)
		// -- overlapped
	case  173: _z80_daa(cpu); goto fetch_next;

		//  28: JR Z,d (M:3 T:12)
		// -- mread
	case  174: goto step_next;
	case  175: _wait(); _mread(cpu->pc++); goto step_next;
	case  176: cpu->dlatch = _gd(); if (!(_cc_z)) { _skip(5); }; goto step_next;
		// -- generic
	case  177: cpu->pc += (int8_t)cpu->dlatch; cpu->wz = cpu->pc; goto step_next;
	case  178: goto step_next;
	case  179: goto step_next;
	case  180: goto step_next;
	case  181: goto step_next;
		// -- overlapped
	case  182: goto fetch_next;

		//  29: ADD HL,HL (M:2 T:11)
		// -- generic
	case  183: _z80_add16(cpu, cpu->hlx[cpu->hlx_idx].hl); goto step_next;
	case  184: goto step_next;
	case  185: goto step_next;
	case  186: goto step_next;
	case  187: goto step_next;
	case  188: goto step_next;
	case  189: goto step_next;
		// -- overlapped
	case  190: goto fetch_next;

		//  2A: LD HL,(nn) (M:5 T:16)
		// -- mread
	case  191: goto step_next;
	case  192: _wait(); _mread(cpu->pc++); goto step_next;
	case  193: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  194: goto step_next;
	case  195: _wait(); _mread(cpu->pc++); goto step_next;
	case  196: cpu->wzh = _gd(); goto step_next;
		// -- mread
	case  197: goto step_next;
	case  198: _wait(); _mread(cpu->wz++); goto step_next;
	case  199: cpu->hlx[cpu->hlx_idx].l = _gd(); goto step_next;
		// -- mread
	case  200: goto step_next;
	case  201: _wait(); _mread(cpu->wz); goto step_next;
	case  202: cpu->hlx[cpu->hlx_idx].h = _gd(); goto step_next;
		// -- overlapped
	case  203: goto fetch_next;

		//  2B: DEC HL (M:2 T:6)
		// -- generic
	case  204: cpu->hlx[cpu->hlx_idx].hl--; goto step_next;
	case  205: goto step_next;
		// -- overlapped
	case  206: goto fetch_next;

		//  2C: INC L (M:1 T:4)
		// -- overlapped
	case  207: cpu->hlx[cpu->hlx_idx].l = _z80_inc8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  2D: DEC L (M:1 T:4)
		// -- overlapped
	case  208: cpu->hlx[cpu->hlx_idx].l = _z80_dec8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  2E: LD L,n (M:2 T:7)
		// -- mread
	case  209: goto step_next;
	case  210: _wait(); _mread(cpu->pc++); goto step_next;
	case  211: cpu->hlx[cpu->hlx_idx].l = _gd(); goto step_next;
		// -- overlapped
	case  212: goto fetch_next;

		//  2F: CPL (M:1 T:4)
		// -- overlapped
	case  213: _z80_cpl(cpu); goto fetch_next;

		//  30: JR NC,d (M:3 T:12)
		// -- mread
	case  214: goto step_next;
	case  215: _wait(); _mread(cpu->pc++); goto step_next;
	case  216: cpu->dlatch = _gd(); if (!(_cc_nc)) { _skip(5); }; goto step_next;
		// -- generic
	case  217: cpu->pc += (int8_t)cpu->dlatch; cpu->wz = cpu->pc; goto step_next;
	case  218: goto step_next;
	case  219: goto step_next;
	case  220: goto step_next;
	case  221: goto step_next;
		// -- overlapped
	case  222: goto fetch_next;

		//  31: LD SP,nn (M:3 T:10)
		// -- mread
	case  223: goto step_next;
	case  224: _wait(); _mread(cpu->pc++); goto step_next;
	case  225: cpu->spl = _gd(); goto step_next;
		// -- mread
	case  226: goto step_next;
	case  227: _wait(); _mread(cpu->pc++); goto step_next;
	case  228: cpu->sph = _gd(); goto step_next;
		// -- overlapped
	case  229: goto fetch_next;

		//  32: LD (nn),A (M:4 T:13)
		// -- mread
	case  230: goto step_next;
	case  231: _wait(); _mread(cpu->pc++); goto step_next;
	case  232: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  233: goto step_next;
	case  234: _wait(); _mread(cpu->pc++); goto step_next;
	case  235: cpu->wzh = _gd(); goto step_next;
		// -- mwrite
	case  236: goto step_next;
	case  237: _wait(); _mwrite(cpu->wz++, cpu->a); cpu->wzh = cpu->a; goto step_next;
	case  238: goto step_next;
		// -- overlapped
	case  239: goto fetch_next;

		//  33: INC SP (M:2 T:6)
		// -- generic
	case  240: cpu->sp++; goto step_next;
	case  241: goto step_next;
		// -- overlapped
	case  242: goto fetch_next;

		//  34: INC (HL) (M:3 T:11)
		// -- mread
	case  243: goto step_next;
	case  244: _wait(); _mread(cpu->addr); goto step_next;
	case  245: cpu->dlatch = _gd(); cpu->dlatch = _z80_inc8(cpu, cpu->dlatch); goto step_next;
	case  246: goto step_next;
		// -- mwrite
	case  247: goto step_next;
	case  248: _wait(); _mwrite(cpu->addr, cpu->dlatch); goto step_next;
	case  249: goto step_next;
		// -- overlapped
	case  250: goto fetch_next;

		//  35: DEC (HL) (M:3 T:11)
		// -- mread
	case  251: goto step_next;
	case  252: _wait(); _mread(cpu->addr); goto step_next;
	case  253: cpu->dlatch = _gd(); cpu->dlatch = _z80_dec8(cpu, cpu->dlatch); goto step_next;
	case  254: goto step_next;
		// -- mwrite
	case  255: goto step_next;
	case  256: _wait(); _mwrite(cpu->addr, cpu->dlatch); goto step_next;
	case  257: goto step_next;
		// -- overlapped
	case  258: goto fetch_next;

		//  36: LD (HL),n (M:3 T:10)
		// -- mread
	case  259: goto step_next;
	case  260: _wait(); _mread(cpu->pc++); goto step_next;
	case  261: cpu->dlatch = _gd(); goto step_next;
		// -- mwrite
	case  262: goto step_next;
	case  263: _wait(); _mwrite(cpu->addr, cpu->dlatch); goto step_next;
	case  264: goto step_next;
		// -- overlapped
	case  265: goto fetch_next;

		//  37: SCF (M:1 T:4)
		// -- overlapped
	case  266: _z80_scf(cpu); goto fetch_next;

		//  38: JR C,d (M:3 T:12)
		// -- mread
	case  267: goto step_next;
	case  268: _wait(); _mread(cpu->pc++); goto step_next;
	case  269: cpu->dlatch = _gd(); if (!(_cc_c)) { _skip(5); }; goto step_next;
		// -- generic
	case  270: cpu->pc += (int8_t)cpu->dlatch; cpu->wz = cpu->pc; goto step_next;
	case  271: goto step_next;
	case  272: goto step_next;
	case  273: goto step_next;
	case  274: goto step_next;
		// -- overlapped
	case  275: goto fetch_next;

		//  39: ADD HL,SP (M:2 T:11)
		// -- generic
	case  276: _z80_add16(cpu, cpu->sp); goto step_next;
	case  277: goto step_next;
	case  278: goto step_next;
	case  279: goto step_next;
	case  280: goto step_next;
	case  281: goto step_next;
	case  282: goto step_next;
		// -- overlapped
	case  283: goto fetch_next;

		//  3A: LD A,(nn) (M:4 T:13)
		// -- mread
	case  284: goto step_next;
	case  285: _wait(); _mread(cpu->pc++); goto step_next;
	case  286: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  287: goto step_next;
	case  288: _wait(); _mread(cpu->pc++); goto step_next;
	case  289: cpu->wzh = _gd(); goto step_next;
		// -- mread
	case  290: goto step_next;
	case  291: _wait(); _mread(cpu->wz++); goto step_next;
	case  292: cpu->a = _gd(); goto step_next;
		// -- overlapped
	case  293: goto fetch_next;

		//  3B: DEC SP (M:2 T:6)
		// -- generic
	case  294: cpu->sp--; goto step_next;
	case  295: goto step_next;
		// -- overlapped
	case  296: goto fetch_next;

		//  3C: INC A (M:1 T:4)
		// -- overlapped
	case  297: cpu->a = _z80_inc8(cpu, cpu->a); goto fetch_next;

		//  3D: DEC A (M:1 T:4)
		// -- overlapped
	case  298: cpu->a = _z80_dec8(cpu, cpu->a); goto fetch_next;

		//  3E: LD A,n (M:2 T:7)
		// -- mread
	case  299: goto step_next;
	case  300: _wait(); _mread(cpu->pc++); goto step_next;
	case  301: cpu->a = _gd(); goto step_next;
		// -- overlapped
	case  302: goto fetch_next;

		//  3F: CCF (M:1 T:4)
		// -- overlapped
	case  303: _z80_ccf(cpu); goto fetch_next;

		//  40: LD B,B (M:1 T:4)
		// -- overlapped
	case  304: cpu->b = cpu->b; goto fetch_next;

		//  41: LD B,C (M:1 T:4)
		// -- overlapped
	case  305: cpu->b = cpu->c; goto fetch_next;

		//  42: LD B,D (M:1 T:4)
		// -- overlapped
	case  306: cpu->b = cpu->d; goto fetch_next;

		//  43: LD B,E (M:1 T:4)
		// -- overlapped
	case  307: cpu->b = cpu->e; goto fetch_next;

		//  44: LD B,H (M:1 T:4)
		// -- overlapped
	case  308: cpu->b = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  45: LD B,L (M:1 T:4)
		// -- overlapped
	case  309: cpu->b = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  46: LD B,(HL) (M:2 T:7)
		// -- mread
	case  310: goto step_next;
	case  311: _wait(); _mread(cpu->addr); goto step_next;
	case  312: cpu->b = _gd(); goto step_next;
		// -- overlapped
	case  313: goto fetch_next;

		//  47: LD B,A (M:1 T:4)
		// -- overlapped
	case  314: cpu->b = cpu->a; goto fetch_next;

		//  48: LD C,B (M:1 T:4)
		// -- overlapped
	case  315: cpu->c = cpu->b; goto fetch_next;

		//  49: LD C,C (M:1 T:4)
		// -- overlapped
	case  316: cpu->c = cpu->c; goto fetch_next;

		//  4A: LD C,D (M:1 T:4)
		// -- overlapped
	case  317: cpu->c = cpu->d; goto fetch_next;

		//  4B: LD C,E (M:1 T:4)
		// -- overlapped
	case  318: cpu->c = cpu->e; goto fetch_next;

		//  4C: LD C,H (M:1 T:4)
		// -- overlapped
	case  319: cpu->c = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  4D: LD C,L (M:1 T:4)
		// -- overlapped
	case  320: cpu->c = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  4E: LD C,(HL) (M:2 T:7)
		// -- mread
	case  321: goto step_next;
	case  322: _wait(); _mread(cpu->addr); goto step_next;
	case  323: cpu->c = _gd(); goto step_next;
		// -- overlapped
	case  324: goto fetch_next;

		//  4F: LD C,A (M:1 T:4)
		// -- overlapped
	case  325: cpu->c = cpu->a; goto fetch_next;

		//  50: LD D,B (M:1 T:4)
		// -- overlapped
	case  326: cpu->d = cpu->b; goto fetch_next;

		//  51: LD D,C (M:1 T:4)
		// -- overlapped
	case  327: cpu->d = cpu->c; goto fetch_next;

		//  52: LD D,D (M:1 T:4)
		// -- overlapped
	case  328: cpu->d = cpu->d; goto fetch_next;

		//  53: LD D,E (M:1 T:4)
		// -- overlapped
	case  329: cpu->d = cpu->e; goto fetch_next;

		//  54: LD D,H (M:1 T:4)
		// -- overlapped
	case  330: cpu->d = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  55: LD D,L (M:1 T:4)
		// -- overlapped
	case  331: cpu->d = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  56: LD D,(HL) (M:2 T:7)
		// -- mread
	case  332: goto step_next;
	case  333: _wait(); _mread(cpu->addr); goto step_next;
	case  334: cpu->d = _gd(); goto step_next;
		// -- overlapped
	case  335: goto fetch_next;

		//  57: LD D,A (M:1 T:4)
		// -- overlapped
	case  336: cpu->d = cpu->a; goto fetch_next;

		//  58: LD E,B (M:1 T:4)
		// -- overlapped
	case  337: cpu->e = cpu->b; goto fetch_next;

		//  59: LD E,C (M:1 T:4)
		// -- overlapped
	case  338: cpu->e = cpu->c; goto fetch_next;

		//  5A: LD E,D (M:1 T:4)
		// -- overlapped
	case  339: cpu->e = cpu->d; goto fetch_next;

		//  5B: LD E,E (M:1 T:4)
		// -- overlapped
	case  340: cpu->e = cpu->e; goto fetch_next;

		//  5C: LD E,H (M:1 T:4)
		// -- overlapped
	case  341: cpu->e = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  5D: LD E,L (M:1 T:4)
		// -- overlapped
	case  342: cpu->e = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  5E: LD E,(HL) (M:2 T:7)
		// -- mread
	case  343: goto step_next;
	case  344: _wait(); _mread(cpu->addr); goto step_next;
	case  345: cpu->e = _gd(); goto step_next;
		// -- overlapped
	case  346: goto fetch_next;

		//  5F: LD E,A (M:1 T:4)
		// -- overlapped
	case  347: cpu->e = cpu->a; goto fetch_next;

		//  60: LD H,B (M:1 T:4)
		// -- overlapped
	case  348: cpu->hlx[cpu->hlx_idx].h = cpu->b; goto fetch_next;

		//  61: LD H,C (M:1 T:4)
		// -- overlapped
	case  349: cpu->hlx[cpu->hlx_idx].h = cpu->c; goto fetch_next;

		//  62: LD H,D (M:1 T:4)
		// -- overlapped
	case  350: cpu->hlx[cpu->hlx_idx].h = cpu->d; goto fetch_next;

		//  63: LD H,E (M:1 T:4)
		// -- overlapped
	case  351: cpu->hlx[cpu->hlx_idx].h = cpu->e; goto fetch_next;

		//  64: LD H,H (M:1 T:4)
		// -- overlapped
	case  352: cpu->hlx[cpu->hlx_idx].h = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  65: LD H,L (M:1 T:4)
		// -- overlapped
	case  353: cpu->hlx[cpu->hlx_idx].h = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  66: LD H,(HL) (M:2 T:7)
		// -- mread
	case  354: goto step_next;
	case  355: _wait(); _mread(cpu->addr); goto step_next;
	case  356: cpu->h = _gd(); goto step_next;
		// -- overlapped
	case  357: goto fetch_next;

		//  67: LD H,A (M:1 T:4)
		// -- overlapped
	case  358: cpu->hlx[cpu->hlx_idx].h = cpu->a; goto fetch_next;

		//  68: LD L,B (M:1 T:4)
		// -- overlapped
	case  359: cpu->hlx[cpu->hlx_idx].l = cpu->b; goto fetch_next;

		//  69: LD L,C (M:1 T:4)
		// -- overlapped
	case  360: cpu->hlx[cpu->hlx_idx].l = cpu->c; goto fetch_next;

		//  6A: LD L,D (M:1 T:4)
		// -- overlapped
	case  361: cpu->hlx[cpu->hlx_idx].l = cpu->d; goto fetch_next;

		//  6B: LD L,E (M:1 T:4)
		// -- overlapped
	case  362: cpu->hlx[cpu->hlx_idx].l = cpu->e; goto fetch_next;

		//  6C: LD L,H (M:1 T:4)
		// -- overlapped
	case  363: cpu->hlx[cpu->hlx_idx].l = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  6D: LD L,L (M:1 T:4)
		// -- overlapped
	case  364: cpu->hlx[cpu->hlx_idx].l = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  6E: LD L,(HL) (M:2 T:7)
		// -- mread
	case  365: goto step_next;
	case  366: _wait(); _mread(cpu->addr); goto step_next;
	case  367: cpu->l = _gd(); goto step_next;
		// -- overlapped
	case  368: goto fetch_next;

		//  6F: LD L,A (M:1 T:4)
		// -- overlapped
	case  369: cpu->hlx[cpu->hlx_idx].l = cpu->a; goto fetch_next;

		//  70: LD (HL),B (M:2 T:7)
		// -- mwrite
	case  370: goto step_next;
	case  371: _wait(); _mwrite(cpu->addr, cpu->b); goto step_next;
	case  372: goto step_next;
		// -- overlapped
	case  373: goto fetch_next;

		//  71: LD (HL),C (M:2 T:7)
		// -- mwrite
	case  374: goto step_next;
	case  375: _wait(); _mwrite(cpu->addr, cpu->c); goto step_next;
	case  376: goto step_next;
		// -- overlapped
	case  377: goto fetch_next;

		//  72: LD (HL),D (M:2 T:7)
		// -- mwrite
	case  378: goto step_next;
	case  379: _wait(); _mwrite(cpu->addr, cpu->d); goto step_next;
	case  380: goto step_next;
		// -- overlapped
	case  381: goto fetch_next;

		//  73: LD (HL),E (M:2 T:7)
		// -- mwrite
	case  382: goto step_next;
	case  383: _wait(); _mwrite(cpu->addr, cpu->e); goto step_next;
	case  384: goto step_next;
		// -- overlapped
	case  385: goto fetch_next;

		//  74: LD (HL),H (M:2 T:7)
		// -- mwrite
	case  386: goto step_next;
	case  387: _wait(); _mwrite(cpu->addr, cpu->h); goto step_next;
	case  388: goto step_next;
		// -- overlapped
	case  389: goto fetch_next;

		//  75: LD (HL),L (M:2 T:7)
		// -- mwrite
	case  390: goto step_next;
	case  391: _wait(); _mwrite(cpu->addr, cpu->l); goto step_next;
	case  392: goto step_next;
		// -- overlapped
	case  393: goto fetch_next;

		//  76: HALT (M:1 T:4)
		// -- overlapped
	case  394: pins = _z80_halt(cpu, pins); goto fetch_next;

		//  77: LD (HL),A (M:2 T:7)
		// -- mwrite
	case  395: goto step_next;
	case  396: _wait(); _mwrite(cpu->addr, cpu->a); goto step_next;
	case  397: goto step_next;
		// -- overlapped
	case  398: goto fetch_next;

		//  78: LD A,B (M:1 T:4)
		// -- overlapped
	case  399: cpu->a = cpu->b; goto fetch_next;

		//  79: LD A,C (M:1 T:4)
		// -- overlapped
	case  400: cpu->a = cpu->c; goto fetch_next;

		//  7A: LD A,D (M:1 T:4)
		// -- overlapped
	case  401: cpu->a = cpu->d; goto fetch_next;

		//  7B: LD A,E (M:1 T:4)
		// -- overlapped
	case  402: cpu->a = cpu->e; goto fetch_next;

		//  7C: LD A,H (M:1 T:4)
		// -- overlapped
	case  403: cpu->a = cpu->hlx[cpu->hlx_idx].h; goto fetch_next;

		//  7D: LD A,L (M:1 T:4)
		// -- overlapped
	case  404: cpu->a = cpu->hlx[cpu->hlx_idx].l; goto fetch_next;

		//  7E: LD A,(HL) (M:2 T:7)
		// -- mread
	case  405: goto step_next;
	case  406: _wait(); _mread(cpu->addr); goto step_next;
	case  407: cpu->a = _gd(); goto step_next;
		// -- overlapped
	case  408: goto fetch_next;

		//  7F: LD A,A (M:1 T:4)
		// -- overlapped
	case  409: cpu->a = cpu->a; goto fetch_next;

		//  80: ADD B (M:1 T:4)
		// -- overlapped
	case  410: _z80_add8(cpu, cpu->b); goto fetch_next;

		//  81: ADD C (M:1 T:4)
		// -- overlapped
	case  411: _z80_add8(cpu, cpu->c); goto fetch_next;

		//  82: ADD D (M:1 T:4)
		// -- overlapped
	case  412: _z80_add8(cpu, cpu->d); goto fetch_next;

		//  83: ADD E (M:1 T:4)
		// -- overlapped
	case  413: _z80_add8(cpu, cpu->e); goto fetch_next;

		//  84: ADD H (M:1 T:4)
		// -- overlapped
	case  414: _z80_add8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  85: ADD L (M:1 T:4)
		// -- overlapped
	case  415: _z80_add8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  86: ADD (HL) (M:2 T:7)
		// -- mread
	case  416: goto step_next;
	case  417: _wait(); _mread(cpu->addr); goto step_next;
	case  418: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  419: _z80_add8(cpu, cpu->dlatch); goto fetch_next;

		//  87: ADD A (M:1 T:4)
		// -- overlapped
	case  420: _z80_add8(cpu, cpu->a); goto fetch_next;

		//  88: ADC B (M:1 T:4)
		// -- overlapped
	case  421: _z80_adc8(cpu, cpu->b); goto fetch_next;

		//  89: ADC C (M:1 T:4)
		// -- overlapped
	case  422: _z80_adc8(cpu, cpu->c); goto fetch_next;

		//  8A: ADC D (M:1 T:4)
		// -- overlapped
	case  423: _z80_adc8(cpu, cpu->d); goto fetch_next;

		//  8B: ADC E (M:1 T:4)
		// -- overlapped
	case  424: _z80_adc8(cpu, cpu->e); goto fetch_next;

		//  8C: ADC H (M:1 T:4)
		// -- overlapped
	case  425: _z80_adc8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  8D: ADC L (M:1 T:4)
		// -- overlapped
	case  426: _z80_adc8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  8E: ADC (HL) (M:2 T:7)
		// -- mread
	case  427: goto step_next;
	case  428: _wait(); _mread(cpu->addr); goto step_next;
	case  429: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  430: _z80_adc8(cpu, cpu->dlatch); goto fetch_next;

		//  8F: ADC A (M:1 T:4)
		// -- overlapped
	case  431: _z80_adc8(cpu, cpu->a); goto fetch_next;

		//  90: SUB B (M:1 T:4)
		// -- overlapped
	case  432: _z80_sub8(cpu, cpu->b); goto fetch_next;

		//  91: SUB C (M:1 T:4)
		// -- overlapped
	case  433: _z80_sub8(cpu, cpu->c); goto fetch_next;

		//  92: SUB D (M:1 T:4)
		// -- overlapped
	case  434: _z80_sub8(cpu, cpu->d); goto fetch_next;

		//  93: SUB E (M:1 T:4)
		// -- overlapped
	case  435: _z80_sub8(cpu, cpu->e); goto fetch_next;

		//  94: SUB H (M:1 T:4)
		// -- overlapped
	case  436: _z80_sub8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  95: SUB L (M:1 T:4)
		// -- overlapped
	case  437: _z80_sub8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  96: SUB (HL) (M:2 T:7)
		// -- mread
	case  438: goto step_next;
	case  439: _wait(); _mread(cpu->addr); goto step_next;
	case  440: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  441: _z80_sub8(cpu, cpu->dlatch); goto fetch_next;

		//  97: SUB A (M:1 T:4)
		// -- overlapped
	case  442: _z80_sub8(cpu, cpu->a); goto fetch_next;

		//  98: SBC B (M:1 T:4)
		// -- overlapped
	case  443: _z80_sbc8(cpu, cpu->b); goto fetch_next;

		//  99: SBC C (M:1 T:4)
		// -- overlapped
	case  444: _z80_sbc8(cpu, cpu->c); goto fetch_next;

		//  9A: SBC D (M:1 T:4)
		// -- overlapped
	case  445: _z80_sbc8(cpu, cpu->d); goto fetch_next;

		//  9B: SBC E (M:1 T:4)
		// -- overlapped
	case  446: _z80_sbc8(cpu, cpu->e); goto fetch_next;

		//  9C: SBC H (M:1 T:4)
		// -- overlapped
	case  447: _z80_sbc8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  9D: SBC L (M:1 T:4)
		// -- overlapped
	case  448: _z80_sbc8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  9E: SBC (HL) (M:2 T:7)
		// -- mread
	case  449: goto step_next;
	case  450: _wait(); _mread(cpu->addr); goto step_next;
	case  451: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  452: _z80_sbc8(cpu, cpu->dlatch); goto fetch_next;

		//  9F: SBC A (M:1 T:4)
		// -- overlapped
	case  453: _z80_sbc8(cpu, cpu->a); goto fetch_next;

		//  A0: AND B (M:1 T:4)
		// -- overlapped
	case  454: _z80_and8(cpu, cpu->b); goto fetch_next;

		//  A1: AND C (M:1 T:4)
		// -- overlapped
	case  455: _z80_and8(cpu, cpu->c); goto fetch_next;

		//  A2: AND D (M:1 T:4)
		// -- overlapped
	case  456: _z80_and8(cpu, cpu->d); goto fetch_next;

		//  A3: AND E (M:1 T:4)
		// -- overlapped
	case  457: _z80_and8(cpu, cpu->e); goto fetch_next;

		//  A4: AND H (M:1 T:4)
		// -- overlapped
	case  458: _z80_and8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  A5: AND L (M:1 T:4)
		// -- overlapped
	case  459: _z80_and8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  A6: AND (HL) (M:2 T:7)
		// -- mread
	case  460: goto step_next;
	case  461: _wait(); _mread(cpu->addr); goto step_next;
	case  462: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  463: _z80_and8(cpu, cpu->dlatch); goto fetch_next;

		//  A7: AND A (M:1 T:4)
		// -- overlapped
	case  464: _z80_and8(cpu, cpu->a); goto fetch_next;

		//  A8: XOR B (M:1 T:4)
		// -- overlapped
	case  465: _z80_xor8(cpu, cpu->b); goto fetch_next;

		//  A9: XOR C (M:1 T:4)
		// -- overlapped
	case  466: _z80_xor8(cpu, cpu->c); goto fetch_next;

		//  AA: XOR D (M:1 T:4)
		// -- overlapped
	case  467: _z80_xor8(cpu, cpu->d); goto fetch_next;

		//  AB: XOR E (M:1 T:4)
		// -- overlapped
	case  468: _z80_xor8(cpu, cpu->e); goto fetch_next;

		//  AC: XOR H (M:1 T:4)
		// -- overlapped
	case  469: _z80_xor8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  AD: XOR L (M:1 T:4)
		// -- overlapped
	case  470: _z80_xor8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  AE: XOR (HL) (M:2 T:7)
		// -- mread
	case  471: goto step_next;
	case  472: _wait(); _mread(cpu->addr); goto step_next;
	case  473: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  474: _z80_xor8(cpu, cpu->dlatch); goto fetch_next;

		//  AF: XOR A (M:1 T:4)
		// -- overlapped
	case  475: _z80_xor8(cpu, cpu->a); goto fetch_next;

		//  B0: OR B (M:1 T:4)
		// -- overlapped
	case  476: _z80_or8(cpu, cpu->b); goto fetch_next;

		//  B1: OR C (M:1 T:4)
		// -- overlapped
	case  477: _z80_or8(cpu, cpu->c); goto fetch_next;

		//  B2: OR D (M:1 T:4)
		// -- overlapped
	case  478: _z80_or8(cpu, cpu->d); goto fetch_next;

		//  B3: OR E (M:1 T:4)
		// -- overlapped
	case  479: _z80_or8(cpu, cpu->e); goto fetch_next;

		//  B4: OR H (M:1 T:4)
		// -- overlapped
	case  480: _z80_or8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  B5: OR L (M:1 T:4)
		// -- overlapped
	case  481: _z80_or8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  B6: OR (HL) (M:2 T:7)
		// -- mread
	case  482: goto step_next;
	case  483: _wait(); _mread(cpu->addr); goto step_next;
	case  484: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  485: _z80_or8(cpu, cpu->dlatch); goto fetch_next;

		//  B7: OR A (M:1 T:4)
		// -- overlapped
	case  486: _z80_or8(cpu, cpu->a); goto fetch_next;

		//  B8: CP B (M:1 T:4)
		// -- overlapped
	case  487: _z80_cp8(cpu, cpu->b); goto fetch_next;

		//  B9: CP C (M:1 T:4)
		// -- overlapped
	case  488: _z80_cp8(cpu, cpu->c); goto fetch_next;

		//  BA: CP D (M:1 T:4)
		// -- overlapped
	case  489: _z80_cp8(cpu, cpu->d); goto fetch_next;

		//  BB: CP E (M:1 T:4)
		// -- overlapped
	case  490: _z80_cp8(cpu, cpu->e); goto fetch_next;

		//  BC: CP H (M:1 T:4)
		// -- overlapped
	case  491: _z80_cp8(cpu, cpu->hlx[cpu->hlx_idx].h); goto fetch_next;

		//  BD: CP L (M:1 T:4)
		// -- overlapped
	case  492: _z80_cp8(cpu, cpu->hlx[cpu->hlx_idx].l); goto fetch_next;

		//  BE: CP (HL) (M:2 T:7)
		// -- mread
	case  493: goto step_next;
	case  494: _wait(); _mread(cpu->addr); goto step_next;
	case  495: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  496: _z80_cp8(cpu, cpu->dlatch); goto fetch_next;

		//  BF: CP A (M:1 T:4)
		// -- overlapped
	case  497: _z80_cp8(cpu, cpu->a); goto fetch_next;

		//  C0: RET NZ (M:4 T:11)
		// -- generic
	case  498: if (!_cc_nz) { _skip(6); }; goto step_next;
		// -- mread
	case  499: goto step_next;
	case  500: _wait(); _mread(cpu->sp++); goto step_next;
	case  501: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  502: goto step_next;
	case  503: _wait(); _mread(cpu->sp++); goto step_next;
	case  504: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  505: goto fetch_next;

		//  C1: POP BC (M:3 T:10)
		// -- mread
	case  506: goto step_next;
	case  507: _wait(); _mread(cpu->sp++); goto step_next;
	case  508: cpu->c = _gd(); goto step_next;
		// -- mread
	case  509: goto step_next;
	case  510: _wait(); _mread(cpu->sp++); goto step_next;
	case  511: cpu->b = _gd(); goto step_next;
		// -- overlapped
	case  512: goto fetch_next;

		//  C2: JP NZ,nn (M:3 T:10)
		// -- mread
	case  513: goto step_next;
	case  514: _wait(); _mread(cpu->pc++); goto step_next;
	case  515: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  516: goto step_next;
	case  517: _wait(); _mread(cpu->pc++); goto step_next;
	case  518: cpu->wzh = _gd(); if (_cc_nz) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  519: goto fetch_next;

		//  C3: JP nn (M:3 T:10)
		// -- mread
	case  520: goto step_next;
	case  521: _wait(); _mread(cpu->pc++); goto step_next;
	case  522: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  523: goto step_next;
	case  524: _wait(); _mread(cpu->pc++); goto step_next;
	case  525: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  526: goto fetch_next;

		//  C4: CALL NZ,nn (M:6 T:17)
		// -- mread
	case  527: goto step_next;
	case  528: _wait(); _mread(cpu->pc++); goto step_next;
	case  529: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  530: goto step_next;
	case  531: _wait(); _mread(cpu->pc++); goto step_next;
	case  532: cpu->wzh = _gd(); if (!_cc_nz) { _skip(7); }; goto step_next;
		// -- generic
	case  533: goto step_next;
		// -- mwrite
	case  534: goto step_next;
	case  535: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  536: goto step_next;
		// -- mwrite
	case  537: goto step_next;
	case  538: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  539: goto step_next;
		// -- overlapped
	case  540: goto fetch_next;

		//  C5: PUSH BC (M:4 T:11)
		// -- generic
	case  541: goto step_next;
		// -- mwrite
	case  542: goto step_next;
	case  543: _wait(); _mwrite(--cpu->sp, cpu->b); goto step_next;
	case  544: goto step_next;
		// -- mwrite
	case  545: goto step_next;
	case  546: _wait(); _mwrite(--cpu->sp, cpu->c); goto step_next;
	case  547: goto step_next;
		// -- overlapped
	case  548: goto fetch_next;

		//  C6: ADD n (M:2 T:7)
		// -- mread
	case  549: goto step_next;
	case  550: _wait(); _mread(cpu->pc++); goto step_next;
	case  551: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  552: _z80_add8(cpu, cpu->dlatch); goto fetch_next;

		//  C7: RST 0h (M:4 T:11)
		// -- generic
	case  553: goto step_next;
		// -- mwrite
	case  554: goto step_next;
	case  555: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  556: goto step_next;
		// -- mwrite
	case  557: goto step_next;
	case  558: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x00; cpu->pc = cpu->wz; goto step_next;
	case  559: goto step_next;
		// -- overlapped
	case  560: goto fetch_next;

		//  C8: RET Z (M:4 T:11)
		// -- generic
	case  561: if (!_cc_z) { _skip(6); }; goto step_next;
		// -- mread
	case  562: goto step_next;
	case  563: _wait(); _mread(cpu->sp++); goto step_next;
	case  564: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  565: goto step_next;
	case  566: _wait(); _mread(cpu->sp++); goto step_next;
	case  567: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  568: goto fetch_next;

		//  C9: RET (M:3 T:10)
		// -- mread
	case  569: goto step_next;
	case  570: _wait(); _mread(cpu->sp++); goto step_next;
	case  571: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  572: goto step_next;
	case  573: _wait(); _mread(cpu->sp++); goto step_next;
	case  574: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  575: goto fetch_next;

		//  CA: JP Z,nn (M:3 T:10)
		// -- mread
	case  576: goto step_next;
	case  577: _wait(); _mread(cpu->pc++); goto step_next;
	case  578: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  579: goto step_next;
	case  580: _wait(); _mread(cpu->pc++); goto step_next;
	case  581: cpu->wzh = _gd(); if (_cc_z) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  582: goto fetch_next;

		//  CB: CB prefix (M:1 T:4)
		// -- overlapped
	case  583: _fetch_cb(); goto step_next;

		//  CC: CALL Z,nn (M:6 T:17)
		// -- mread
	case  584: goto step_next;
	case  585: _wait(); _mread(cpu->pc++); goto step_next;
	case  586: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  587: goto step_next;
	case  588: _wait(); _mread(cpu->pc++); goto step_next;
	case  589: cpu->wzh = _gd(); if (!_cc_z) { _skip(7); }; goto step_next;
		// -- generic
	case  590: goto step_next;
		// -- mwrite
	case  591: goto step_next;
	case  592: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  593: goto step_next;
		// -- mwrite
	case  594: goto step_next;
	case  595: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  596: goto step_next;
		// -- overlapped
	case  597: goto fetch_next;

		//  CD: CALL nn (M:5 T:17)
		// -- mread
	case  598: goto step_next;
	case  599: _wait(); _mread(cpu->pc++); goto step_next;
	case  600: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  601: goto step_next;
	case  602: _wait(); _mread(cpu->pc++); goto step_next;
	case  603: cpu->wzh = _gd(); goto step_next;
	case  604: goto step_next;
		// -- mwrite
	case  605: goto step_next;
	case  606: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  607: goto step_next;
		// -- mwrite
	case  608: goto step_next;
	case  609: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  610: goto step_next;
		// -- overlapped
	case  611: goto fetch_next;

		//  CE: ADC n (M:2 T:7)
		// -- mread
	case  612: goto step_next;
	case  613: _wait(); _mread(cpu->pc++); goto step_next;
	case  614: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  615: _z80_adc8(cpu, cpu->dlatch); goto fetch_next;

		//  CF: RST 8h (M:4 T:11)
		// -- generic
	case  616: goto step_next;
		// -- mwrite
	case  617: goto step_next;
	case  618: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  619: goto step_next;
		// -- mwrite
	case  620: goto step_next;
	case  621: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x08; cpu->pc = cpu->wz; goto step_next;
	case  622: goto step_next;
		// -- overlapped
	case  623: goto fetch_next;

		//  D0: RET NC (M:4 T:11)
		// -- generic
	case  624: if (!_cc_nc) { _skip(6); }; goto step_next;
		// -- mread
	case  625: goto step_next;
	case  626: _wait(); _mread(cpu->sp++); goto step_next;
	case  627: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  628: goto step_next;
	case  629: _wait(); _mread(cpu->sp++); goto step_next;
	case  630: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  631: goto fetch_next;

		//  D1: POP DE (M:3 T:10)
		// -- mread
	case  632: goto step_next;
	case  633: _wait(); _mread(cpu->sp++); goto step_next;
	case  634: cpu->e = _gd(); goto step_next;
		// -- mread
	case  635: goto step_next;
	case  636: _wait(); _mread(cpu->sp++); goto step_next;
	case  637: cpu->d = _gd(); goto step_next;
		// -- overlapped
	case  638: goto fetch_next;

		//  D2: JP NC,nn (M:3 T:10)
		// -- mread
	case  639: goto step_next;
	case  640: _wait(); _mread(cpu->pc++); goto step_next;
	case  641: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  642: goto step_next;
	case  643: _wait(); _mread(cpu->pc++); goto step_next;
	case  644: cpu->wzh = _gd(); if (_cc_nc) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  645: goto fetch_next;

		//  D3: OUT (n),A (M:3 T:11)
		// -- mread
	case  646: goto step_next;
	case  647: _wait(); _mread(cpu->pc++); goto step_next;
	case  648: cpu->wzl = _gd(); cpu->wzh = cpu->a; goto step_next;
		// -- iowrite
	case  649: goto step_next;
	case  650: _iowrite(cpu->wz, cpu->a); goto step_next;
	case  651: _wait(); cpu->wzl++; goto step_next;
	case  652: goto step_next;
		// -- overlapped
	case  653: goto fetch_next;

		//  D4: CALL NC,nn (M:6 T:17)
		// -- mread
	case  654: goto step_next;
	case  655: _wait(); _mread(cpu->pc++); goto step_next;
	case  656: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  657: goto step_next;
	case  658: _wait(); _mread(cpu->pc++); goto step_next;
	case  659: cpu->wzh = _gd(); if (!_cc_nc) { _skip(7); }; goto step_next;
		// -- generic
	case  660: goto step_next;
		// -- mwrite
	case  661: goto step_next;
	case  662: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  663: goto step_next;
		// -- mwrite
	case  664: goto step_next;
	case  665: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  666: goto step_next;
		// -- overlapped
	case  667: goto fetch_next;

		//  D5: PUSH DE (M:4 T:11)
		// -- generic
	case  668: goto step_next;
		// -- mwrite
	case  669: goto step_next;
	case  670: _wait(); _mwrite(--cpu->sp, cpu->d); goto step_next;
	case  671: goto step_next;
		// -- mwrite
	case  672: goto step_next;
	case  673: _wait(); _mwrite(--cpu->sp, cpu->e); goto step_next;
	case  674: goto step_next;
		// -- overlapped
	case  675: goto fetch_next;

		//  D6: SUB n (M:2 T:7)
		// -- mread
	case  676: goto step_next;
	case  677: _wait(); _mread(cpu->pc++); goto step_next;
	case  678: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  679: _z80_sub8(cpu, cpu->dlatch); goto fetch_next;

		//  D7: RST 10h (M:4 T:11)
		// -- generic
	case  680: goto step_next;
		// -- mwrite
	case  681: goto step_next;
	case  682: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  683: goto step_next;
		// -- mwrite
	case  684: goto step_next;
	case  685: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x10; cpu->pc = cpu->wz; goto step_next;
	case  686: goto step_next;
		// -- overlapped
	case  687: goto fetch_next;

		//  D8: RET C (M:4 T:11)
		// -- generic
	case  688: if (!_cc_c) { _skip(6); }; goto step_next;
		// -- mread
	case  689: goto step_next;
	case  690: _wait(); _mread(cpu->sp++); goto step_next;
	case  691: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  692: goto step_next;
	case  693: _wait(); _mread(cpu->sp++); goto step_next;
	case  694: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  695: goto fetch_next;

		//  D9: EXX (M:1 T:4)
		// -- overlapped
	case  696: _z80_exx(cpu); goto fetch_next;

		//  DA: JP C,nn (M:3 T:10)
		// -- mread
	case  697: goto step_next;
	case  698: _wait(); _mread(cpu->pc++); goto step_next;
	case  699: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  700: goto step_next;
	case  701: _wait(); _mread(cpu->pc++); goto step_next;
	case  702: cpu->wzh = _gd(); if (_cc_c) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  703: goto fetch_next;

		//  DB: IN A,(n) (M:3 T:11)
		// -- mread
	case  704: goto step_next;
	case  705: _wait(); _mread(cpu->pc++); goto step_next;
	case  706: cpu->wzl = _gd(); cpu->wzh = cpu->a; goto step_next;
		// -- ioread
	case  707: goto step_next;
	case  708: goto step_next;
	case  709: _wait(); _ioread(cpu->wz++); goto step_next;
	case  710: cpu->a = _gd(); goto step_next;
		// -- overlapped
	case  711: goto fetch_next;

		//  DC: CALL C,nn (M:6 T:17)
		// -- mread
	case  712: goto step_next;
	case  713: _wait(); _mread(cpu->pc++); goto step_next;
	case  714: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  715: goto step_next;
	case  716: _wait(); _mread(cpu->pc++); goto step_next;
	case  717: cpu->wzh = _gd(); if (!_cc_c) { _skip(7); }; goto step_next;
		// -- generic
	case  718: goto step_next;
		// -- mwrite
	case  719: goto step_next;
	case  720: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  721: goto step_next;
		// -- mwrite
	case  722: goto step_next;
	case  723: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  724: goto step_next;
		// -- overlapped
	case  725: goto fetch_next;

		//  DD: DD prefix (M:1 T:4)
		// -- overlapped
	case  726: _fetch_dd(); goto step_next;

		//  DE: SBC n (M:2 T:7)
		// -- mread
	case  727: goto step_next;
	case  728: _wait(); _mread(cpu->pc++); goto step_next;
	case  729: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  730: _z80_sbc8(cpu, cpu->dlatch); goto fetch_next;

		//  DF: RST 18h (M:4 T:11)
		// -- generic
	case  731: goto step_next;
		// -- mwrite
	case  732: goto step_next;
	case  733: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  734: goto step_next;
		// -- mwrite
	case  735: goto step_next;
	case  736: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x18; cpu->pc = cpu->wz; goto step_next;
	case  737: goto step_next;
		// -- overlapped
	case  738: goto fetch_next;

		//  E0: RET PO (M:4 T:11)
		// -- generic
	case  739: if (!_cc_po) { _skip(6); }; goto step_next;
		// -- mread
	case  740: goto step_next;
	case  741: _wait(); _mread(cpu->sp++); goto step_next;
	case  742: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  743: goto step_next;
	case  744: _wait(); _mread(cpu->sp++); goto step_next;
	case  745: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  746: goto fetch_next;

		//  E1: POP HL (M:3 T:10)
		// -- mread
	case  747: goto step_next;
	case  748: _wait(); _mread(cpu->sp++); goto step_next;
	case  749: cpu->hlx[cpu->hlx_idx].l = _gd(); goto step_next;
		// -- mread
	case  750: goto step_next;
	case  751: _wait(); _mread(cpu->sp++); goto step_next;
	case  752: cpu->hlx[cpu->hlx_idx].h = _gd(); goto step_next;
		// -- overlapped
	case  753: goto fetch_next;

		//  E2: JP PO,nn (M:3 T:10)
		// -- mread
	case  754: goto step_next;
	case  755: _wait(); _mread(cpu->pc++); goto step_next;
	case  756: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  757: goto step_next;
	case  758: _wait(); _mread(cpu->pc++); goto step_next;
	case  759: cpu->wzh = _gd(); if (_cc_po) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  760: goto fetch_next;

		//  E3: EX (SP),HL (M:5 T:19)
		// -- mread
	case  761: goto step_next;
	case  762: _wait(); _mread(cpu->sp); goto step_next;
	case  763: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  764: goto step_next;
	case  765: _wait(); _mread(cpu->sp + 1); goto step_next;
	case  766: cpu->wzh = _gd(); goto step_next;
	case  767: goto step_next;
		// -- mwrite
	case  768: goto step_next;
	case  769: _wait(); _mwrite(cpu->sp + 1, cpu->hlx[cpu->hlx_idx].h); goto step_next;
	case  770: goto step_next;
		// -- mwrite
	case  771: goto step_next;
	case  772: _wait(); _mwrite(cpu->sp, cpu->hlx[cpu->hlx_idx].l); cpu->hlx[cpu->hlx_idx].hl = cpu->wz; goto step_next;
	case  773: goto step_next;
	case  774: goto step_next;
	case  775: goto step_next;
		// -- overlapped
	case  776: goto fetch_next;

		//  E4: CALL PO,nn (M:6 T:17)
		// -- mread
	case  777: goto step_next;
	case  778: _wait(); _mread(cpu->pc++); goto step_next;
	case  779: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  780: goto step_next;
	case  781: _wait(); _mread(cpu->pc++); goto step_next;
	case  782: cpu->wzh = _gd(); if (!_cc_po) { _skip(7); }; goto step_next;
		// -- generic
	case  783: goto step_next;
		// -- mwrite
	case  784: goto step_next;
	case  785: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  786: goto step_next;
		// -- mwrite
	case  787: goto step_next;
	case  788: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  789: goto step_next;
		// -- overlapped
	case  790: goto fetch_next;

		//  E5: PUSH HL (M:4 T:11)
		// -- generic
	case  791: goto step_next;
		// -- mwrite
	case  792: goto step_next;
	case  793: _wait(); _mwrite(--cpu->sp, cpu->hlx[cpu->hlx_idx].h); goto step_next;
	case  794: goto step_next;
		// -- mwrite
	case  795: goto step_next;
	case  796: _wait(); _mwrite(--cpu->sp, cpu->hlx[cpu->hlx_idx].l); goto step_next;
	case  797: goto step_next;
		// -- overlapped
	case  798: goto fetch_next;

		//  E6: AND n (M:2 T:7)
		// -- mread
	case  799: goto step_next;
	case  800: _wait(); _mread(cpu->pc++); goto step_next;
	case  801: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  802: _z80_and8(cpu, cpu->dlatch); goto fetch_next;

		//  E7: RST 20h (M:4 T:11)
		// -- generic
	case  803: goto step_next;
		// -- mwrite
	case  804: goto step_next;
	case  805: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  806: goto step_next;
		// -- mwrite
	case  807: goto step_next;
	case  808: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x20; cpu->pc = cpu->wz; goto step_next;
	case  809: goto step_next;
		// -- overlapped
	case  810: goto fetch_next;

		//  E8: RET PE (M:4 T:11)
		// -- generic
	case  811: if (!_cc_pe) { _skip(6); }; goto step_next;
		// -- mread
	case  812: goto step_next;
	case  813: _wait(); _mread(cpu->sp++); goto step_next;
	case  814: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  815: goto step_next;
	case  816: _wait(); _mread(cpu->sp++); goto step_next;
	case  817: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  818: goto fetch_next;

		//  E9: JP HL (M:1 T:4)
		// -- overlapped
	case  819: cpu->pc = cpu->hlx[cpu->hlx_idx].hl; goto fetch_next;

		//  EA: JP PE,nn (M:3 T:10)
		// -- mread
	case  820: goto step_next;
	case  821: _wait(); _mread(cpu->pc++); goto step_next;
	case  822: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  823: goto step_next;
	case  824: _wait(); _mread(cpu->pc++); goto step_next;
	case  825: cpu->wzh = _gd(); if (_cc_pe) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  826: goto fetch_next;

		//  EB: EX DE,HL (M:1 T:4)
		// -- overlapped
	case  827: _z80_ex_de_hl(cpu); goto fetch_next;

		//  EC: CALL PE,nn (M:6 T:17)
		// -- mread
	case  828: goto step_next;
	case  829: _wait(); _mread(cpu->pc++); goto step_next;
	case  830: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  831: goto step_next;
	case  832: _wait(); _mread(cpu->pc++); goto step_next;
	case  833: cpu->wzh = _gd(); if (!_cc_pe) { _skip(7); }; goto step_next;
		// -- generic
	case  834: goto step_next;
		// -- mwrite
	case  835: goto step_next;
	case  836: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  837: goto step_next;
		// -- mwrite
	case  838: goto step_next;
	case  839: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  840: goto step_next;
		// -- overlapped
	case  841: goto fetch_next;

		//  ED: ED prefix (M:1 T:4)
		// -- overlapped
	case  842: _fetch_ed(); goto step_next;

		//  EE: XOR n (M:2 T:7)
		// -- mread
	case  843: goto step_next;
	case  844: _wait(); _mread(cpu->pc++); goto step_next;
	case  845: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  846: _z80_xor8(cpu, cpu->dlatch); goto fetch_next;

		//  EF: RST 28h (M:4 T:11)
		// -- generic
	case  847: goto step_next;
		// -- mwrite
	case  848: goto step_next;
	case  849: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  850: goto step_next;
		// -- mwrite
	case  851: goto step_next;
	case  852: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x28; cpu->pc = cpu->wz; goto step_next;
	case  853: goto step_next;
		// -- overlapped
	case  854: goto fetch_next;

		//  F0: RET P (M:4 T:11)
		// -- generic
	case  855: if (!_cc_p) { _skip(6); }; goto step_next;
		// -- mread
	case  856: goto step_next;
	case  857: _wait(); _mread(cpu->sp++); goto step_next;
	case  858: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  859: goto step_next;
	case  860: _wait(); _mread(cpu->sp++); goto step_next;
	case  861: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  862: goto fetch_next;

		//  F1: POP AF (M:3 T:10)
		// -- mread
	case  863: goto step_next;
	case  864: _wait(); _mread(cpu->sp++); goto step_next;
	case  865: cpu->f = _gd(); goto step_next;
		// -- mread
	case  866: goto step_next;
	case  867: _wait(); _mread(cpu->sp++); goto step_next;
	case  868: cpu->a = _gd(); goto step_next;
		// -- overlapped
	case  869: goto fetch_next;

		//  F2: JP P,nn (M:3 T:10)
		// -- mread
	case  870: goto step_next;
	case  871: _wait(); _mread(cpu->pc++); goto step_next;
	case  872: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  873: goto step_next;
	case  874: _wait(); _mread(cpu->pc++); goto step_next;
	case  875: cpu->wzh = _gd(); if (_cc_p) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  876: goto fetch_next;

		//  F3: DI (M:1 T:4)
		// -- overlapped
	case  877: cpu->iff1 = cpu->iff2 = false; goto fetch_next;

		//  F4: CALL P,nn (M:6 T:17)
		// -- mread
	case  878: goto step_next;
	case  879: _wait(); _mread(cpu->pc++); goto step_next;
	case  880: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  881: goto step_next;
	case  882: _wait(); _mread(cpu->pc++); goto step_next;
	case  883: cpu->wzh = _gd(); if (!_cc_p) { _skip(7); }; goto step_next;
		// -- generic
	case  884: goto step_next;
		// -- mwrite
	case  885: goto step_next;
	case  886: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  887: goto step_next;
		// -- mwrite
	case  888: goto step_next;
	case  889: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  890: goto step_next;
		// -- overlapped
	case  891: goto fetch_next;

		//  F5: PUSH AF (M:4 T:11)
		// -- generic
	case  892: goto step_next;
		// -- mwrite
	case  893: goto step_next;
	case  894: _wait(); _mwrite(--cpu->sp, cpu->a); goto step_next;
	case  895: goto step_next;
		// -- mwrite
	case  896: goto step_next;
	case  897: _wait(); _mwrite(--cpu->sp, cpu->f); goto step_next;
	case  898: goto step_next;
		// -- overlapped
	case  899: goto fetch_next;

		//  F6: OR n (M:2 T:7)
		// -- mread
	case  900: goto step_next;
	case  901: _wait(); _mread(cpu->pc++); goto step_next;
	case  902: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  903: _z80_or8(cpu, cpu->dlatch); goto fetch_next;

		//  F7: RST 30h (M:4 T:11)
		// -- generic
	case  904: goto step_next;
		// -- mwrite
	case  905: goto step_next;
	case  906: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  907: goto step_next;
		// -- mwrite
	case  908: goto step_next;
	case  909: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x30; cpu->pc = cpu->wz; goto step_next;
	case  910: goto step_next;
		// -- overlapped
	case  911: goto fetch_next;

		//  F8: RET M (M:4 T:11)
		// -- generic
	case  912: if (!_cc_m) { _skip(6); }; goto step_next;
		// -- mread
	case  913: goto step_next;
	case  914: _wait(); _mread(cpu->sp++); goto step_next;
	case  915: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  916: goto step_next;
	case  917: _wait(); _mread(cpu->sp++); goto step_next;
	case  918: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  919: goto fetch_next;

		//  F9: LD SP,HL (M:2 T:6)
		// -- generic
	case  920: cpu->sp = cpu->hlx[cpu->hlx_idx].hl; goto step_next;
	case  921: goto step_next;
		// -- overlapped
	case  922: goto fetch_next;

		//  FA: JP M,nn (M:3 T:10)
		// -- mread
	case  923: goto step_next;
	case  924: _wait(); _mread(cpu->pc++); goto step_next;
	case  925: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  926: goto step_next;
	case  927: _wait(); _mread(cpu->pc++); goto step_next;
	case  928: cpu->wzh = _gd(); if (_cc_m) { cpu->pc = cpu->wz; }; goto step_next;
		// -- overlapped
	case  929: goto fetch_next;

		//  FB: EI (M:1 T:4)
		// -- overlapped
	case  930: cpu->iff1 = cpu->iff2 = false; pins = _z80_fetch(cpu, pins); cpu->iff1 = cpu->iff2 = true; goto step_next;

		//  FC: CALL M,nn (M:6 T:17)
		// -- mread
	case  931: goto step_next;
	case  932: _wait(); _mread(cpu->pc++); goto step_next;
	case  933: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  934: goto step_next;
	case  935: _wait(); _mread(cpu->pc++); goto step_next;
	case  936: cpu->wzh = _gd(); if (!_cc_m) { _skip(7); }; goto step_next;
		// -- generic
	case  937: goto step_next;
		// -- mwrite
	case  938: goto step_next;
	case  939: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  940: goto step_next;
		// -- mwrite
	case  941: goto step_next;
	case  942: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->pc = cpu->wz; goto step_next;
	case  943: goto step_next;
		// -- overlapped
	case  944: goto fetch_next;

		//  FD: FD prefix (M:1 T:4)
		// -- overlapped
	case  945: _fetch_fd(); goto step_next;

		//  FE: CP n (M:2 T:7)
		// -- mread
	case  946: goto step_next;
	case  947: _wait(); _mread(cpu->pc++); goto step_next;
	case  948: cpu->dlatch = _gd(); goto step_next;
		// -- overlapped
	case  949: _z80_cp8(cpu, cpu->dlatch); goto fetch_next;

		//  FF: RST 38h (M:4 T:11)
		// -- generic
	case  950: goto step_next;
		// -- mwrite
	case  951: goto step_next;
	case  952: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case  953: goto step_next;
		// -- mwrite
	case  954: goto step_next;
	case  955: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = 0x38; cpu->pc = cpu->wz; goto step_next;
	case  956: goto step_next;
		// -- overlapped
	case  957: goto fetch_next;

		// ED 00: ED NOP (M:1 T:4)
		// -- overlapped
	case  958: goto fetch_next;

		// ED 40: IN B,(C) (M:2 T:8)
		// -- ioread
	case  959: goto step_next;
	case  960: goto step_next;
	case  961: _wait(); _ioread(cpu->bc); goto step_next;
	case  962: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case  963: cpu->b = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 41: OUT (C),B (M:2 T:8)
		// -- iowrite
	case  964: goto step_next;
	case  965: _iowrite(cpu->bc, cpu->b); goto step_next;
	case  966: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case  967: goto step_next;
		// -- overlapped
	case  968: goto fetch_next;

		// ED 42: SBC HL,BC (M:2 T:11)
		// -- generic
	case  969: _z80_sbc16(cpu, cpu->bc); goto step_next;
	case  970: goto step_next;
	case  971: goto step_next;
	case  972: goto step_next;
	case  973: goto step_next;
	case  974: goto step_next;
	case  975: goto step_next;
		// -- overlapped
	case  976: goto fetch_next;

		// ED 43: LD (nn),BC (M:5 T:16)
		// -- mread
	case  977: goto step_next;
	case  978: _wait(); _mread(cpu->pc++); goto step_next;
	case  979: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  980: goto step_next;
	case  981: _wait(); _mread(cpu->pc++); goto step_next;
	case  982: cpu->wzh = _gd(); goto step_next;
		// -- mwrite
	case  983: goto step_next;
	case  984: _wait(); _mwrite(cpu->wz++, cpu->c); goto step_next;
	case  985: goto step_next;
		// -- mwrite
	case  986: goto step_next;
	case  987: _wait(); _mwrite(cpu->wz, cpu->b); goto step_next;
	case  988: goto step_next;
		// -- overlapped
	case  989: goto fetch_next;

		// ED 44: NEG (M:1 T:4)
		// -- overlapped
	case  990: _z80_neg8(cpu); goto fetch_next;

		// ED 45: RETN (M:3 T:10)
		// -- mread
	case  991: goto step_next;
	case  992: _wait(); _mread(cpu->sp++); goto step_next;
	case  993: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case  994: goto step_next;
	case  995: _wait(); _mread(cpu->sp++); goto step_next;
	case  996: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case  997: pins = _z80_fetch(cpu, pins); cpu->iff1 = cpu->iff2; goto step_next;

		// ED 46: IM 0 (M:1 T:4)
		// -- overlapped
	case  998: cpu->im = 0; goto fetch_next;

		// ED 47: LD I,A (M:2 T:5)
		// -- generic
	case  999: goto step_next;
		// -- overlapped
	case 1000: cpu->i = cpu->a; goto fetch_next;

		// ED 48: IN C,(C) (M:2 T:8)
		// -- ioread
	case 1001: goto step_next;
	case 1002: goto step_next;
	case 1003: _wait(); _ioread(cpu->bc); goto step_next;
	case 1004: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1005: cpu->c = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 49: OUT (C),C (M:2 T:8)
		// -- iowrite
	case 1006: goto step_next;
	case 1007: _iowrite(cpu->bc, cpu->c); goto step_next;
	case 1008: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1009: goto step_next;
		// -- overlapped
	case 1010: goto fetch_next;

		// ED 4A: ADC HL,BC (M:2 T:11)
		// -- generic
	case 1011: _z80_adc16(cpu, cpu->bc); goto step_next;
	case 1012: goto step_next;
	case 1013: goto step_next;
	case 1014: goto step_next;
	case 1015: goto step_next;
	case 1016: goto step_next;
	case 1017: goto step_next;
		// -- overlapped
	case 1018: goto fetch_next;

		// ED 4B: LD BC,(nn) (M:5 T:16)
		// -- mread
	case 1019: goto step_next;
	case 1020: _wait(); _mread(cpu->pc++); goto step_next;
	case 1021: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1022: goto step_next;
	case 1023: _wait(); _mread(cpu->pc++); goto step_next;
	case 1024: cpu->wzh = _gd(); goto step_next;
		// -- mread
	case 1025: goto step_next;
	case 1026: _wait(); _mread(cpu->wz++); goto step_next;
	case 1027: cpu->c = _gd(); goto step_next;
		// -- mread
	case 1028: goto step_next;
	case 1029: _wait(); _mread(cpu->wz); goto step_next;
	case 1030: cpu->b = _gd(); goto step_next;
		// -- overlapped
	case 1031: goto fetch_next;

		// ED 4D: RETI (M:3 T:10)
		// -- mread
	case 1032: goto step_next;
	case 1033: _wait(); _mread(cpu->sp++); goto step_next;
	case 1034: cpu->wzl = _gd(); pins |= Z80_RETI; goto step_next;
		// -- mread
	case 1035: goto step_next;
	case 1036: _wait(); _mread(cpu->sp++); goto step_next;
	case 1037: cpu->wzh = _gd(); cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case 1038: pins = _z80_fetch(cpu, pins); cpu->iff1 = cpu->iff2; goto step_next;

		// ED 4E: IM 0 (M:1 T:4)
		// -- overlapped
	case 1039: cpu->im = 0; goto fetch_next;

		// ED 4F: LD R,A (M:2 T:5)
		// -- generic
	case 1040: goto step_next;
		// -- overlapped
	case 1041: cpu->r = cpu->a; goto fetch_next;

		// ED 50: IN D,(C) (M:2 T:8)
		// -- ioread
	case 1042: goto step_next;
	case 1043: goto step_next;
	case 1044: _wait(); _ioread(cpu->bc); goto step_next;
	case 1045: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1046: cpu->d = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 51: OUT (C),D (M:2 T:8)
		// -- iowrite
	case 1047: goto step_next;
	case 1048: _iowrite(cpu->bc, cpu->d); goto step_next;
	case 1049: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1050: goto step_next;
		// -- overlapped
	case 1051: goto fetch_next;

		// ED 52: SBC HL,DE (M:2 T:11)
		// -- generic
	case 1052: _z80_sbc16(cpu, cpu->de); goto step_next;
	case 1053: goto step_next;
	case 1054: goto step_next;
	case 1055: goto step_next;
	case 1056: goto step_next;
	case 1057: goto step_next;
	case 1058: goto step_next;
		// -- overlapped
	case 1059: goto fetch_next;

		// ED 53: LD (nn),DE (M:5 T:16)
		// -- mread
	case 1060: goto step_next;
	case 1061: _wait(); _mread(cpu->pc++); goto step_next;
	case 1062: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1063: goto step_next;
	case 1064: _wait(); _mread(cpu->pc++); goto step_next;
	case 1065: cpu->wzh = _gd(); goto step_next;
		// -- mwrite
	case 1066: goto step_next;
	case 1067: _wait(); _mwrite(cpu->wz++, cpu->e); goto step_next;
	case 1068: goto step_next;
		// -- mwrite
	case 1069: goto step_next;
	case 1070: _wait(); _mwrite(cpu->wz, cpu->d); goto step_next;
	case 1071: goto step_next;
		// -- overlapped
	case 1072: goto fetch_next;

		// ED 56: IM 1 (M:1 T:4)
		// -- overlapped
	case 1073: cpu->im = 1; goto fetch_next;

		// ED 57: LD A,I (M:2 T:5)
		// -- generic
	case 1074: goto step_next;
		// -- overlapped
	case 1075: cpu->a = cpu->i; cpu->f = _z80_sziff2_flags(cpu, cpu->i); goto fetch_next;

		// ED 58: IN E,(C) (M:2 T:8)
		// -- ioread
	case 1076: goto step_next;
	case 1077: goto step_next;
	case 1078: _wait(); _ioread(cpu->bc); goto step_next;
	case 1079: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1080: cpu->e = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 59: OUT (C),E (M:2 T:8)
		// -- iowrite
	case 1081: goto step_next;
	case 1082: _iowrite(cpu->bc, cpu->e); goto step_next;
	case 1083: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1084: goto step_next;
		// -- overlapped
	case 1085: goto fetch_next;

		// ED 5A: ADC HL,DE (M:2 T:11)
		// -- generic
	case 1086: _z80_adc16(cpu, cpu->de); goto step_next;
	case 1087: goto step_next;
	case 1088: goto step_next;
	case 1089: goto step_next;
	case 1090: goto step_next;
	case 1091: goto step_next;
	case 1092: goto step_next;
		// -- overlapped
	case 1093: goto fetch_next;

		// ED 5B: LD DE,(nn) (M:5 T:16)
		// -- mread
	case 1094: goto step_next;
	case 1095: _wait(); _mread(cpu->pc++); goto step_next;
	case 1096: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1097: goto step_next;
	case 1098: _wait(); _mread(cpu->pc++); goto step_next;
	case 1099: cpu->wzh = _gd(); goto step_next;
		// -- mread
	case 1100: goto step_next;
	case 1101: _wait(); _mread(cpu->wz++); goto step_next;
	case 1102: cpu->e = _gd(); goto step_next;
		// -- mread
	case 1103: goto step_next;
	case 1104: _wait(); _mread(cpu->wz); goto step_next;
	case 1105: cpu->d = _gd(); goto step_next;
		// -- overlapped
	case 1106: goto fetch_next;

		// ED 5E: IM 2 (M:1 T:4)
		// -- overlapped
	case 1107: cpu->im = 2; goto fetch_next;

		// ED 5F: LD A,R (M:2 T:5)
		// -- generic
	case 1108: goto step_next;
		// -- overlapped
	case 1109: cpu->a = cpu->r; cpu->f = _z80_sziff2_flags(cpu, cpu->r); goto fetch_next;

		// ED 60: IN H,(C) (M:2 T:8)
		// -- ioread
	case 1110: goto step_next;
	case 1111: goto step_next;
	case 1112: _wait(); _ioread(cpu->bc); goto step_next;
	case 1113: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1114: cpu->h = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 61: OUT (C),H (M:2 T:8)
		// -- iowrite
	case 1115: goto step_next;
	case 1116: _iowrite(cpu->bc, cpu->h); goto step_next;
	case 1117: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1118: goto step_next;
		// -- overlapped
	case 1119: goto fetch_next;

		// ED 62: SBC HL,HL (M:2 T:11)
		// -- generic
	case 1120: _z80_sbc16(cpu, cpu->hl); goto step_next;
	case 1121: goto step_next;
	case 1122: goto step_next;
	case 1123: goto step_next;
	case 1124: goto step_next;
	case 1125: goto step_next;
	case 1126: goto step_next;
		// -- overlapped
	case 1127: goto fetch_next;

		// ED 63: LD (nn),HL (M:5 T:16)
		// -- mread
	case 1128: goto step_next;
	case 1129: _wait(); _mread(cpu->pc++); goto step_next;
	case 1130: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1131: goto step_next;
	case 1132: _wait(); _mread(cpu->pc++); goto step_next;
	case 1133: cpu->wzh = _gd(); goto step_next;
		// -- mwrite
	case 1134: goto step_next;
	case 1135: _wait(); _mwrite(cpu->wz++, cpu->l); goto step_next;
	case 1136: goto step_next;
		// -- mwrite
	case 1137: goto step_next;
	case 1138: _wait(); _mwrite(cpu->wz, cpu->h); goto step_next;
	case 1139: goto step_next;
		// -- overlapped
	case 1140: goto fetch_next;

		// ED 66: IM 0 (M:1 T:4)
		// -- overlapped
	case 1141: cpu->im = 0; goto fetch_next;

		// ED 67: RRD (M:4 T:14)
		// -- mread
	case 1142: goto step_next;
	case 1143: _wait(); _mread(cpu->hl); goto step_next;
	case 1144: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case 1145: cpu->dlatch = _z80_rrd(cpu, cpu->dlatch); goto step_next;
	case 1146: goto step_next;
	case 1147: goto step_next;
	case 1148: goto step_next;
		// -- mwrite
	case 1149: goto step_next;
	case 1150: _wait(); _mwrite(cpu->hl, cpu->dlatch); cpu->wz = cpu->hl + 1; goto step_next;
	case 1151: goto step_next;
		// -- overlapped
	case 1152: goto fetch_next;

		// ED 68: IN L,(C) (M:2 T:8)
		// -- ioread
	case 1153: goto step_next;
	case 1154: goto step_next;
	case 1155: _wait(); _ioread(cpu->bc); goto step_next;
	case 1156: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1157: cpu->l = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 69: OUT (C),L (M:2 T:8)
		// -- iowrite
	case 1158: goto step_next;
	case 1159: _iowrite(cpu->bc, cpu->l); goto step_next;
	case 1160: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1161: goto step_next;
		// -- overlapped
	case 1162: goto fetch_next;

		// ED 6A: ADC HL,HL (M:2 T:11)
		// -- generic
	case 1163: _z80_adc16(cpu, cpu->hl); goto step_next;
	case 1164: goto step_next;
	case 1165: goto step_next;
	case 1166: goto step_next;
	case 1167: goto step_next;
	case 1168: goto step_next;
	case 1169: goto step_next;
		// -- overlapped
	case 1170: goto fetch_next;

		// ED 6B: LD HL,(nn) (M:5 T:16)
		// -- mread
	case 1171: goto step_next;
	case 1172: _wait(); _mread(cpu->pc++); goto step_next;
	case 1173: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1174: goto step_next;
	case 1175: _wait(); _mread(cpu->pc++); goto step_next;
	case 1176: cpu->wzh = _gd(); goto step_next;
		// -- mread
	case 1177: goto step_next;
	case 1178: _wait(); _mread(cpu->wz++); goto step_next;
	case 1179: cpu->l = _gd(); goto step_next;
		// -- mread
	case 1180: goto step_next;
	case 1181: _wait(); _mread(cpu->wz); goto step_next;
	case 1182: cpu->h = _gd(); goto step_next;
		// -- overlapped
	case 1183: goto fetch_next;

		// ED 6E: IM 0 (M:1 T:4)
		// -- overlapped
	case 1184: cpu->im = 0; goto fetch_next;

		// ED 6F: RLD (M:4 T:14)
		// -- mread
	case 1185: goto step_next;
	case 1186: _wait(); _mread(cpu->hl); goto step_next;
	case 1187: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case 1188: cpu->dlatch = _z80_rld(cpu, cpu->dlatch); goto step_next;
	case 1189: goto step_next;
	case 1190: goto step_next;
	case 1191: goto step_next;
		// -- mwrite
	case 1192: goto step_next;
	case 1193: _wait(); _mwrite(cpu->hl, cpu->dlatch); cpu->wz = cpu->hl + 1; goto step_next;
	case 1194: goto step_next;
		// -- overlapped
	case 1195: goto fetch_next;

		// ED 70: IN (C) (M:2 T:8)
		// -- ioread
	case 1196: goto step_next;
	case 1197: goto step_next;
	case 1198: _wait(); _ioread(cpu->bc); goto step_next;
	case 1199: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1200: _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 71: OUT (C),0 (M:2 T:8)
		// -- iowrite
	case 1201: goto step_next;
	case 1202: _iowrite(cpu->bc, 0); goto step_next;
	case 1203: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1204: goto step_next;
		// -- overlapped
	case 1205: goto fetch_next;

		// ED 72: SBC HL,SP (M:2 T:11)
		// -- generic
	case 1206: _z80_sbc16(cpu, cpu->sp); goto step_next;
	case 1207: goto step_next;
	case 1208: goto step_next;
	case 1209: goto step_next;
	case 1210: goto step_next;
	case 1211: goto step_next;
	case 1212: goto step_next;
		// -- overlapped
	case 1213: goto fetch_next;

		// ED 73: LD (nn),SP (M:5 T:16)
		// -- mread
	case 1214: goto step_next;
	case 1215: _wait(); _mread(cpu->pc++); goto step_next;
	case 1216: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1217: goto step_next;
	case 1218: _wait(); _mread(cpu->pc++); goto step_next;
	case 1219: cpu->wzh = _gd(); goto step_next;
		// -- mwrite
	case 1220: goto step_next;
	case 1221: _wait(); _mwrite(cpu->wz++, cpu->spl); goto step_next;
	case 1222: goto step_next;
		// -- mwrite
	case 1223: goto step_next;
	case 1224: _wait(); _mwrite(cpu->wz, cpu->sph); goto step_next;
	case 1225: goto step_next;
		// -- overlapped
	case 1226: goto fetch_next;

		// ED 76: IM 1 (M:1 T:4)
		// -- overlapped
	case 1227: cpu->im = 1; goto fetch_next;

		// ED 78: IN A,(C) (M:2 T:8)
		// -- ioread
	case 1228: goto step_next;
	case 1229: goto step_next;
	case 1230: _wait(); _ioread(cpu->bc); goto step_next;
	case 1231: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; goto step_next;
		// -- overlapped
	case 1232: cpu->a = _z80_in(cpu, cpu->dlatch); goto fetch_next;

		// ED 79: OUT (C),A (M:2 T:8)
		// -- iowrite
	case 1233: goto step_next;
	case 1234: _iowrite(cpu->bc, cpu->a); goto step_next;
	case 1235: _wait(); cpu->wz = cpu->bc + 1; goto step_next;
	case 1236: goto step_next;
		// -- overlapped
	case 1237: goto fetch_next;

		// ED 7A: ADC HL,SP (M:2 T:11)
		// -- generic
	case 1238: _z80_adc16(cpu, cpu->sp); goto step_next;
	case 1239: goto step_next;
	case 1240: goto step_next;
	case 1241: goto step_next;
	case 1242: goto step_next;
	case 1243: goto step_next;
	case 1244: goto step_next;
		// -- overlapped
	case 1245: goto fetch_next;

		// ED 7B: LD SP,(nn) (M:5 T:16)
		// -- mread
	case 1246: goto step_next;
	case 1247: _wait(); _mread(cpu->pc++); goto step_next;
	case 1248: cpu->wzl = _gd(); goto step_next;
		// -- mread
	case 1249: goto step_next;
	case 1250: _wait(); _mread(cpu->pc++); goto step_next;
	case 1251: cpu->wzh = _gd(); goto step_next;
		// -- mread
	case 1252: goto step_next;
	case 1253: _wait(); _mread(cpu->wz++); goto step_next;
	case 1254: cpu->spl = _gd(); goto step_next;
		// -- mread
	case 1255: goto step_next;
	case 1256: _wait(); _mread(cpu->wz); goto step_next;
	case 1257: cpu->sph = _gd(); goto step_next;
		// -- overlapped
	case 1258: goto fetch_next;

		// ED 7E: IM 2 (M:1 T:4)
		// -- overlapped
	case 1259: cpu->im = 2; goto fetch_next;

		// ED A0: LDI (M:4 T:12)
		// -- mread
	case 1260: goto step_next;
	case 1261: _wait(); _mread(cpu->hl++); goto step_next;
	case 1262: cpu->dlatch = _gd(); goto step_next;
		// -- mwrite
	case 1263: goto step_next;
	case 1264: _wait(); _mwrite(cpu->de++, cpu->dlatch); goto step_next;
	case 1265: goto step_next;
		// -- generic
	case 1266: _z80_ldi_ldd(cpu, cpu->dlatch); goto step_next;
	case 1267: goto step_next;
		// -- overlapped
	case 1268: goto fetch_next;

		// ED A1: CPI (M:3 T:12)
		// -- mread
	case 1269: goto step_next;
	case 1270: _wait(); _mread(cpu->hl++); goto step_next;
	case 1271: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case 1272: cpu->wz++; _z80_cpi_cpd(cpu, cpu->dlatch); goto step_next;
	case 1273: goto step_next;
	case 1274: goto step_next;
	case 1275: goto step_next;
	case 1276: goto step_next;
		// -- overlapped
	case 1277: goto fetch_next;

		// ED A2: INI (M:4 T:12)
		// -- generic
	case 1278: goto step_next;
		// -- ioread
	case 1279: goto step_next;
	case 1280: goto step_next;
	case 1281: _wait(); _ioread(cpu->bc); goto step_next;
	case 1282: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; cpu->b--;; goto step_next;
		// -- mwrite
	case 1283: goto step_next;
	case 1284: _wait(); _mwrite(cpu->hl++, cpu->dlatch); _z80_ini_ind(cpu, cpu->dlatch, cpu->c + 1); goto step_next;
	case 1285: goto step_next;
		// -- overlapped
	case 1286: goto fetch_next;

		// ED A3: OUTI (M:4 T:12)
		// -- generic
	case 1287: goto step_next;
		// -- mread
	case 1288: goto step_next;
	case 1289: _wait(); _mread(cpu->hl++); goto step_next;
	case 1290: cpu->dlatch = _gd(); cpu->b--; goto step_next;
		// -- iowrite
	case 1291: goto step_next;
	case 1292: _iowrite(cpu->bc, cpu->dlatch); goto step_next;
	case 1293: _wait(); cpu->wz = cpu->bc + 1; _z80_outi_outd(cpu, cpu->dlatch); goto step_next;
	case 1294: goto step_next;
		// -- overlapped
	case 1295: goto fetch_next;

		// ED A8: LDD (M:4 T:12)
		// -- mread
	case 1296: goto step_next;
	case 1297: _wait(); _mread(cpu->hl--); goto step_next;
	case 1298: cpu->dlatch = _gd(); goto step_next;
		// -- mwrite
	case 1299: goto step_next;
	case 1300: _wait(); _mwrite(cpu->de--, cpu->dlatch); goto step_next;
	case 1301: goto step_next;
		// -- generic
	case 1302: _z80_ldi_ldd(cpu, cpu->dlatch); goto step_next;
	case 1303: goto step_next;
		// -- overlapped
	case 1304: goto fetch_next;

		// ED A9: CPD (M:3 T:12)
		// -- mread
	case 1305: goto step_next;
	case 1306: _wait(); _mread(cpu->hl--); goto step_next;
	case 1307: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case 1308: cpu->wz--; _z80_cpi_cpd(cpu, cpu->dlatch); goto step_next;
	case 1309: goto step_next;
	case 1310: goto step_next;
	case 1311: goto step_next;
	case 1312: goto step_next;
		// -- overlapped
	case 1313: goto fetch_next;

		// ED AA: IND (M:4 T:12)
		// -- generic
	case 1314: goto step_next;
		// -- ioread
	case 1315: goto step_next;
	case 1316: goto step_next;
	case 1317: _wait(); _ioread(cpu->bc); goto step_next;
	case 1318: cpu->dlatch = _gd(); cpu->wz = cpu->bc - 1; cpu->b--;; goto step_next;
		// -- mwrite
	case 1319: goto step_next;
	case 1320: _wait(); _mwrite(cpu->hl--, cpu->dlatch); _z80_ini_ind(cpu, cpu->dlatch, cpu->c - 1); goto step_next;
	case 1321: goto step_next;
		// -- overlapped
	case 1322: goto fetch_next;

		// ED AB: OUTD (M:4 T:12)
		// -- generic
	case 1323: goto step_next;
		// -- mread
	case 1324: goto step_next;
	case 1325: _wait(); _mread(cpu->hl--); goto step_next;
	case 1326: cpu->dlatch = _gd(); cpu->b--; goto step_next;
		// -- iowrite
	case 1327: goto step_next;
	case 1328: _iowrite(cpu->bc, cpu->dlatch); goto step_next;
	case 1329: _wait(); cpu->wz = cpu->bc - 1; _z80_outi_outd(cpu, cpu->dlatch); goto step_next;
	case 1330: goto step_next;
		// -- overlapped
	case 1331: goto fetch_next;

		// ED B0: LDIR (M:5 T:17)
		// -- mread
	case 1332: goto step_next;
	case 1333: _wait(); _mread(cpu->hl++); goto step_next;
	case 1334: cpu->dlatch = _gd(); goto step_next;
		// -- mwrite
	case 1335: goto step_next;
	case 1336: _wait(); _mwrite(cpu->de++, cpu->dlatch); goto step_next;
	case 1337: goto step_next;
		// -- generic
	case 1338: if (!_z80_ldi_ldd(cpu, cpu->dlatch)) { _skip(5); }; goto step_next;
	case 1339: goto step_next;
		// -- generic
	case 1340: cpu->wz = --cpu->pc; --cpu->pc;; goto step_next;
	case 1341: goto step_next;
	case 1342: goto step_next;
	case 1343: goto step_next;
	case 1344: goto step_next;
		// -- overlapped
	case 1345: goto fetch_next;

		// ED B1: CPIR (M:4 T:17)
		// -- mread
	case 1346: goto step_next;
	case 1347: _wait(); _mread(cpu->hl++); goto step_next;
	case 1348: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case 1349: cpu->wz++; if (!_z80_cpi_cpd(cpu, cpu->dlatch)) { _skip(5); }; goto step_next;
	case 1350: goto step_next;
	case 1351: goto step_next;
	case 1352: goto step_next;
	case 1353: goto step_next;
		// -- generic
	case 1354: cpu->wz = --cpu->pc; --cpu->pc; goto step_next;
	case 1355: goto step_next;
	case 1356: goto step_next;
	case 1357: goto step_next;
	case 1358: goto step_next;
		// -- overlapped
	case 1359: goto fetch_next;

		// ED B2: INIR (M:5 T:17)
		// -- generic
	case 1360: goto step_next;
		// -- ioread
	case 1361: goto step_next;
	case 1362: goto step_next;
	case 1363: _wait(); _ioread(cpu->bc); goto step_next;
	case 1364: cpu->dlatch = _gd(); cpu->wz = cpu->bc + 1; cpu->b--;; goto step_next;
		// -- mwrite
	case 1365: goto step_next;
	case 1366: _wait(); _mwrite(cpu->hl++, cpu->dlatch); if (!_z80_ini_ind(cpu, cpu->dlatch, cpu->c + 1)) { _skip(5); }; goto step_next;
	case 1367: goto step_next;
		// -- generic
	case 1368: cpu->wz = --cpu->pc; --cpu->pc; goto step_next;
	case 1369: goto step_next;
	case 1370: goto step_next;
	case 1371: goto step_next;
	case 1372: goto step_next;
		// -- overlapped
	case 1373: goto fetch_next;

		// ED B3: OTIR (M:5 T:17)
		// -- generic
	case 1374: goto step_next;
		// -- mread
	case 1375: goto step_next;
	case 1376: _wait(); _mread(cpu->hl++); goto step_next;
	case 1377: cpu->dlatch = _gd(); cpu->b--; goto step_next;
		// -- iowrite
	case 1378: goto step_next;
	case 1379: _iowrite(cpu->bc, cpu->dlatch); goto step_next;
	case 1380: _wait(); cpu->wz = cpu->bc + 1; if (!_z80_outi_outd(cpu, cpu->dlatch)) { _skip(5); }; goto step_next;
	case 1381: goto step_next;
		// -- generic
	case 1382: cpu->wz = --cpu->pc; --cpu->pc; goto step_next;
	case 1383: goto step_next;
	case 1384: goto step_next;
	case 1385: goto step_next;
	case 1386: goto step_next;
		// -- overlapped
	case 1387: goto fetch_next;

		// ED B8: LDDR (M:5 T:17)
		// -- mread
	case 1388: goto step_next;
	case 1389: _wait(); _mread(cpu->hl--); goto step_next;
	case 1390: cpu->dlatch = _gd(); goto step_next;
		// -- mwrite
	case 1391: goto step_next;
	case 1392: _wait(); _mwrite(cpu->de--, cpu->dlatch); goto step_next;
	case 1393: goto step_next;
		// -- generic
	case 1394: if (!_z80_ldi_ldd(cpu, cpu->dlatch)) { _skip(5); }; goto step_next;
	case 1395: goto step_next;
		// -- generic
	case 1396: cpu->wz = --cpu->pc; --cpu->pc;; goto step_next;
	case 1397: goto step_next;
	case 1398: goto step_next;
	case 1399: goto step_next;
	case 1400: goto step_next;
		// -- overlapped
	case 1401: goto fetch_next;

		// ED B9: CPDR (M:4 T:17)
		// -- mread
	case 1402: goto step_next;
	case 1403: _wait(); _mread(cpu->hl--); goto step_next;
	case 1404: cpu->dlatch = _gd(); goto step_next;
		// -- generic
	case 1405: cpu->wz--; if (!_z80_cpi_cpd(cpu, cpu->dlatch)) { _skip(5); }; goto step_next;
	case 1406: goto step_next;
	case 1407: goto step_next;
	case 1408: goto step_next;
	case 1409: goto step_next;
		// -- generic
	case 1410: cpu->wz = --cpu->pc; --cpu->pc; goto step_next;
	case 1411: goto step_next;
	case 1412: goto step_next;
	case 1413: goto step_next;
	case 1414: goto step_next;
		// -- overlapped
	case 1415: goto fetch_next;

		// ED BA: INDR (M:5 T:17)
		// -- generic
	case 1416: goto step_next;
		// -- ioread
	case 1417: goto step_next;
	case 1418: goto step_next;
	case 1419: _wait(); _ioread(cpu->bc); goto step_next;
	case 1420: cpu->dlatch = _gd(); cpu->wz = cpu->bc - 1; cpu->b--;; goto step_next;
		// -- mwrite
	case 1421: goto step_next;
	case 1422: _wait(); _mwrite(cpu->hl--, cpu->dlatch); if (!_z80_ini_ind(cpu, cpu->dlatch, cpu->c - 1)) { _skip(5); }; goto step_next;
	case 1423: goto step_next;
		// -- generic
	case 1424: cpu->wz = --cpu->pc; --cpu->pc; goto step_next;
	case 1425: goto step_next;
	case 1426: goto step_next;
	case 1427: goto step_next;
	case 1428: goto step_next;
		// -- overlapped
	case 1429: goto fetch_next;

		// ED BB: OTDR (M:5 T:17)
		// -- generic
	case 1430: goto step_next;
		// -- mread
	case 1431: goto step_next;
	case 1432: _wait(); _mread(cpu->hl--); goto step_next;
	case 1433: cpu->dlatch = _gd(); cpu->b--; goto step_next;
		// -- iowrite
	case 1434: goto step_next;
	case 1435: _iowrite(cpu->bc, cpu->dlatch); goto step_next;
	case 1436: _wait(); cpu->wz = cpu->bc - 1; if (!_z80_outi_outd(cpu, cpu->dlatch)) { _skip(5); }; goto step_next;
	case 1437: goto step_next;
		// -- generic
	case 1438: cpu->wz = --cpu->pc; --cpu->pc; goto step_next;
	case 1439: goto step_next;
	case 1440: goto step_next;
	case 1441: goto step_next;
	case 1442: goto step_next;
		// -- overlapped
	case 1443: goto fetch_next;

		// CB 00: cb (M:1 T:4)
		// -- overlapped
	case 1444: { uint8_t z = cpu->opcode & 7; _z80_cb_action(cpu, z, z); }; goto fetch_next;

		// CB 00: cbhl (M:3 T:11)
		// -- mread
	case 1445: goto step_next;
	case 1446: _wait(); _mread(cpu->hl); goto step_next;
	case 1447: cpu->dlatch = _gd(); if (!_z80_cb_action(cpu, 6, 6)) { _skip(3); }; goto step_next;
	case 1448: goto step_next;
		// -- mwrite
	case 1449: goto step_next;
	case 1450: _wait(); _mwrite(cpu->hl, cpu->dlatch); goto step_next;
	case 1451: goto step_next;
		// -- overlapped
	case 1452: goto fetch_next;

		// CB 00: ddfdcb (M:6 T:18)
		// -- generic
	case 1453: _wait(); _mread(cpu->pc++); goto step_next;
		// -- generic
	case 1454: _z80_ddfdcb_addr(cpu, pins); goto step_next;
		// -- mread
	case 1455: goto step_next;
	case 1456: _wait(); _mread(cpu->pc++); goto step_next;
	case 1457: cpu->opcode = _gd(); goto step_next;
	case 1458: goto step_next;
	case 1459: goto step_next;
		// -- mread
	case 1460: goto step_next;
	case 1461: _wait(); _mread(cpu->addr); goto step_next;
	case 1462: cpu->dlatch = _gd(); if (!_z80_cb_action(cpu, 6, cpu->opcode & 7)) { _skip(3); }; goto step_next;
	case 1463: goto step_next;
		// -- mwrite
	case 1464: goto step_next;
	case 1465: _wait(); _mwrite(cpu->addr, cpu->dlatch); goto step_next;
	case 1466: goto step_next;
		// -- overlapped
	case 1467: goto fetch_next;

		//  00: int_im0 (M:6 T:9)
		// -- generic
	case 1468: cpu->iff1 = cpu->iff2 = false; goto step_next;
		// -- generic
	case 1469: pins |= (Z80_M1 | Z80_IORQ); goto step_next;
		// -- generic
	case 1470: _wait(); cpu->opcode = _z80_get_db(pins); goto step_next;
		// -- generic
	case 1471: pins = _z80_refresh(cpu, pins); goto step_next;
		// -- generic
	case 1472: cpu->step = _z80_optable[cpu->opcode]; cpu->addr = cpu->hl; goto step_next;
		// -- overlapped
	case 1473: goto fetch_next;

		//  00: int_im1 (M:7 T:16)
		// -- generic
	case 1474: cpu->iff1 = cpu->iff2 = false; goto step_next;
		// -- generic
	case 1475: pins |= (Z80_M1 | Z80_IORQ); goto step_next;
		// -- generic
	case 1476: _wait(); goto step_next;
		// -- generic
	case 1477: pins = _z80_refresh(cpu, pins); goto step_next;
	case 1478: goto step_next;
	case 1479: goto step_next;
		// -- mwrite
	case 1480: goto step_next;
	case 1481: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case 1482: goto step_next;
		// -- mwrite
	case 1483: goto step_next;
	case 1484: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = cpu->pc = 0x0038; goto step_next;
	case 1485: goto step_next;
		// -- overlapped
	case 1486: goto fetch_next;

		//  00: int_im2 (M:9 T:22)
		// -- generic
	case 1487: cpu->iff1 = cpu->iff2 = false; goto step_next;
		// -- generic
	case 1488: pins |= (Z80_M1 | Z80_IORQ); goto step_next;
		// -- generic
	case 1489: _wait(); cpu->dlatch = _z80_get_db(pins); goto step_next;
		// -- generic
	case 1490: pins = _z80_refresh(cpu, pins); goto step_next;
	case 1491: goto step_next;
	case 1492: goto step_next;
		// -- mwrite
	case 1493: goto step_next;
	case 1494: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case 1495: goto step_next;
		// -- mwrite
	case 1496: goto step_next;
	case 1497: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wzl = cpu->dlatch; cpu->wzh = cpu->i; goto step_next;
	case 1498: goto step_next;
		// -- mread
	case 1499: goto step_next;
	case 1500: _wait(); _mread(cpu->wz++); goto step_next;
	case 1501: cpu->dlatch = _gd(); goto step_next;
		// -- mread
	case 1502: goto step_next;
	case 1503: _wait(); _mread(cpu->wz); goto step_next;
	case 1504: cpu->wzh = _gd(); cpu->wzl = cpu->dlatch; cpu->pc = cpu->wz; goto step_next;
		// -- overlapped
	case 1505: goto fetch_next;

		//  00: nmi (M:5 T:14)
		// -- generic
	case 1506: _wait(); cpu->iff1 = false; goto step_next;
		// -- generic
	case 1507: pins = _z80_refresh(cpu, pins); goto step_next;
	case 1508: goto step_next;
	case 1509: goto step_next;
		// -- mwrite
	case 1510: goto step_next;
	case 1511: _wait(); _mwrite(--cpu->sp, cpu->pch); goto step_next;
	case 1512: goto step_next;
		// -- mwrite
	case 1513: goto step_next;
	case 1514: _wait(); _mwrite(--cpu->sp, cpu->pcl); cpu->wz = cpu->pc = 0x0066; goto step_next;
	case 1515: goto step_next;
		// -- overlapped
	case 1516: goto fetch_next;

	default: _Z80_UNREACHABLE;
	}
fetch_next: pins = _z80_fetch(cpu, pins);
step_next:  cpu->step += 1;
track_int_bits: {
	// track NMI 0 => 1 edge and current INT pin state, this will track the
	// relevant interrupt status up to the last instruction cycle and will
	// be checked in the first M1 cycle (during _fetch)
	const uint64_t rising_nmi = (pins ^ cpu->pins) & pins; // NMI 0 => 1
	cpu->pins = pins;
	cpu->int_bits = ((cpu->int_bits | rising_nmi) & Z80_NMI) | (pins & Z80_INT);
}
return pins;
}

#undef _sa
#undef _sax
#undef _sad
#undef _sadx
#undef _gd
#undef _skip
#undef _fetch_dd
#undef _fetch_fd
#undef _fetch_ed
#undef _fetch_cb
#undef _mread
#undef _mwrite
#undef _ioread
#undef _iowrite
#undef _wait
#undef _cc_nz
#undef _cc_z
#undef _cc_nc
#undef _cc_c
#undef _cc_po
#undef _cc_pe
#undef _cc_p
#undef _cc_m

#endif // CHIPS_IMPL#pragma once
