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

// Expose the functions for C# interoperability
extern "C" {
	__declspec(dllexport) LibFz80* CreateLibFz80();
	__declspec(dllexport) void DestroyLibFz80(LibFz80* instance);
	__declspec(dllexport) uint64_t LibFz80_Initialize(LibFz80* instance);
	__declspec(dllexport) uint64_t LibFz80_Reset(LibFz80* instance);
	__declspec(dllexport) uint64_t LibFz80_Tick(LibFz80* instance, uint64_t pins);
	__declspec(dllexport) uint64_t LibFz80_Prefetch(LibFz80* instance, uint16_t new_pc);
	__declspec(dllexport) bool LibFz80_InstructionDone(LibFz80* instance);

	// expose struct field getters
	__declspec(dllexport) uint16_t LibFz80_GET_step(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_addr(LibFz80* instance);
	__declspec(dllexport) uint8_t LibFz80_GET_dlatch(LibFz80* instance);
	__declspec(dllexport) uint8_t LibFz80_GET_opcode(LibFz80* instance);
	__declspec(dllexport) uint8_t LibFz80_GET_hlx_idx(LibFz80* instance);
	__declspec(dllexport) bool LibFz80_GET_prefix_active(LibFz80* instance);
	__declspec(dllexport) uint64_t LibFz80_GET_pins(LibFz80* instance);
	__declspec(dllexport) uint64_t LibFz80_GET_int_bits(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_pc(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_af(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_bc(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_de(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_hl(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_ix(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_iy(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_wz(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_sp(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_ir(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_af2(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_bc2(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_de2(LibFz80* instance);
	__declspec(dllexport) uint16_t LibFz80_GET_hl2(LibFz80* instance);
	__declspec(dllexport) uint8_t LibFz80_GET_im(LibFz80* instance);
	__declspec(dllexport) bool LibFz80_GET_iff1(LibFz80* instance);
	__declspec(dllexport) bool LibFz80_GET_iff2(LibFz80* instance);

	// expose struct field setters
	__declspec(dllexport) void LibFz80_SET_step(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_addr(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_dlatch(LibFz80* instance, uint8_t value);
	__declspec(dllexport) void LibFz80_SET_opcode(LibFz80* instance, uint8_t value);
	__declspec(dllexport) void LibFz80_SET_hlx_idx(LibFz80* instance, uint8_t value);
	__declspec(dllexport) void LibFz80_SET_prefix_active(LibFz80* instance, bool value);
	__declspec(dllexport) void LibFz80_SET_pins(LibFz80* instance, uint64_t value);
	__declspec(dllexport) void LibFz80_SET_int_bits(LibFz80* instance, uint64_t value);
	__declspec(dllexport) void LibFz80_SET_pc(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_af(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_bc(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_de(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_hl(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_ix(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_iy(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_wz(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_sp(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_ir(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_af2(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_bc2(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_de2(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_hl2(LibFz80* instance, uint16_t value);
	__declspec(dllexport) void LibFz80_SET_im(LibFz80* instance, uint8_t value);
	__declspec(dllexport) void LibFz80_SET_iff1(LibFz80* instance, bool value);
	__declspec(dllexport) void LibFz80_SET_iff2(LibFz80* instance, bool value);
}

#endif // FLOOOHZ80_H