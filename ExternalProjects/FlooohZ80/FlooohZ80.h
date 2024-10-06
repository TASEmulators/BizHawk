#ifndef FLOOOHZ80_H
#define FLOOOHZ80_H

#include <stdint.h>
#include <stdbool.h>
#include "z80.h"

class LibFz80 {
public:
	LibFz80();
	~LibFz80();

	uint64_t Initialize();
	uint64_t Reset();
	uint64_t Tick(uint64_t pins);
	uint64_t Prefetch(uint16_t new_pc);
	bool InstructionDone();

	// getter methods for the z80_t struct members
	uint16_t GET_step();
	uint16_t GET_addr();
	uint8_t GET_dlatch();
	uint8_t GET_opcode();
	uint8_t GET_hlx_idx();
	bool GET_prefix_active();
	uint64_t GET_pins();
	uint64_t GET_int_bits();
	uint16_t GET_pc();
	uint16_t GET_af();
	uint16_t GET_bc();
	uint16_t GET_de();
	uint16_t GET_hl();
	uint16_t GET_ix();
	uint16_t GET_iy();
	uint16_t GET_wz();
	uint16_t GET_sp();
	uint16_t GET_ir();
	uint16_t GET_af2();
	uint16_t GET_bc2();
	uint16_t GET_de2();
	uint16_t GET_hl2();
	uint8_t GET_im();
	bool GET_iff1();
	bool GET_iff2();

	// setter methods for the z80_t struct members
	void SET_step(uint16_t value);
	void SET_addr(uint16_t value);
	void SET_dlatch(uint8_t value);
	void SET_opcode(uint8_t value);
	void SET_hlx_idx(uint8_t value);
	void SET_prefix_active(bool value);
	void SET_pins(uint64_t value);
	void SET_int_bits(uint64_t value);
	void SET_pc(uint16_t value);
	void SET_af(uint16_t value);
	void SET_bc(uint16_t value);
	void SET_de(uint16_t value);
	void SET_hl(uint16_t value);
	void SET_ix(uint16_t value);
	void SET_iy(uint16_t value);
	void SET_wz(uint16_t value);
	void SET_sp(uint16_t value);
	void SET_ir(uint16_t value);
	void SET_af2(uint16_t value);
	void SET_bc2(uint16_t value);
	void SET_de2(uint16_t value);
	void SET_hl2(uint16_t value);
	void SET_im(uint8_t value);
	void SET_iff1(bool value);
	void SET_iff2(bool value);

private:
	z80_t* z80Instance;
};

#endif // FLOOOHZ80_H