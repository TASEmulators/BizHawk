#define CHIPS_IMPL
#include "FlooohZ80.h"
#include <stdlib.h> // Include for malloc and free

LibFz80::LibFz80() {
	z80Instance = new z80_t();
	z80_init(z80Instance);
}

LibFz80::~LibFz80() {
	delete z80Instance;
}

uint64_t LibFz80::Initialize() {
	return z80_init(z80Instance);
}

uint64_t LibFz80::Reset() {
	return z80_reset(z80Instance);
}

uint64_t LibFz80::Tick(uint64_t pins) {
	return z80_tick(z80Instance, pins);
}

uint64_t LibFz80::Prefetch(uint16_t new_pc) {
	return z80_prefetch(z80Instance, new_pc);
}

bool LibFz80::InstructionDone() {
	return z80_opdone(z80Instance);
}

// Getter methods for z80_t members
uint16_t LibFz80::GET_step() { return z80Instance->step; }
uint16_t LibFz80::GET_addr() { return z80Instance->addr; }
uint8_t LibFz80::GET_dlatch() { return z80Instance->dlatch; }
uint8_t LibFz80::GET_opcode() { return z80Instance->opcode; }
uint8_t LibFz80::GET_hlx_idx() { return z80Instance->hlx_idx; }
bool LibFz80::GET_prefix_active() { return z80Instance->prefix_active; }
uint64_t LibFz80::GET_pins() { return z80Instance->pins; }
uint64_t LibFz80::GET_int_bits() { return z80Instance->int_bits; }
uint16_t LibFz80::GET_pc() { return z80Instance->pc; }
uint16_t LibFz80::GET_af() { return z80Instance->af; }
uint16_t LibFz80::GET_bc() { return z80Instance->bc; }
uint16_t LibFz80::GET_de() { return z80Instance->de; }
uint16_t LibFz80::GET_hl() { return z80Instance->hl; }
uint16_t LibFz80::GET_ix() { return z80Instance->ix; }
uint16_t LibFz80::GET_iy() { return z80Instance->iy; }
uint16_t LibFz80::GET_wz() { return z80Instance->wz; }
uint16_t LibFz80::GET_sp() { return z80Instance->sp; }
uint16_t LibFz80::GET_ir() { return z80Instance->ir; }
uint16_t LibFz80::GET_af2() { return z80Instance->af2; }
uint16_t LibFz80::GET_bc2() { return z80Instance->bc2; }
uint16_t LibFz80::GET_de2() { return z80Instance->de2; }
uint16_t LibFz80::GET_hl2() { return z80Instance->hl2; }
uint8_t LibFz80::GET_im() { return z80Instance->im; }
bool LibFz80::GET_iff1() { return z80Instance->iff1; }
bool LibFz80::GET_iff2() { return z80Instance->iff2; }

// Setter methods for z80_t members
void LibFz80::SET_step(uint16_t value) { z80Instance->step = value; }
void LibFz80::SET_addr(uint16_t value) { z80Instance->addr = value; }
void LibFz80::SET_dlatch(uint8_t value) { z80Instance->dlatch = value; }
void LibFz80::SET_opcode(uint8_t value) { z80Instance->opcode = value; }
void LibFz80::SET_hlx_idx(uint8_t value) { z80Instance->hlx_idx = value; }
void LibFz80::SET_prefix_active(bool value) { z80Instance->prefix_active = value; }
void LibFz80::SET_pins(uint64_t value) { z80Instance->pins = value; }
void LibFz80::SET_int_bits(uint64_t value) { z80Instance->int_bits = value; }
void LibFz80::SET_pc(uint16_t value) { z80Instance->pc = value; }
void LibFz80::SET_af(uint16_t value) { z80Instance->af = value; }
void LibFz80::SET_bc(uint16_t value) { z80Instance->bc = value; }
void LibFz80::SET_de(uint16_t value) { z80Instance->de = value; }
void LibFz80::SET_hl(uint16_t value) { z80Instance->hl = value; }
void LibFz80::SET_ix(uint16_t value) { z80Instance->ix = value; }
void LibFz80::SET_iy(uint16_t value) { z80Instance->iy = value; }
void LibFz80::SET_wz(uint16_t value) { z80Instance->wz = value; }
void LibFz80::SET_sp(uint16_t value) { z80Instance->sp = value; }
void LibFz80::SET_ir(uint16_t value) { z80Instance->ir = value; }
void LibFz80::SET_af2(uint16_t value) { z80Instance->af2 = value; }
void LibFz80::SET_bc2(uint16_t value) { z80Instance->bc2 = value; }
void LibFz80::SET_de2(uint16_t value) { z80Instance->de2 = value; }
void LibFz80::SET_hl2(uint16_t value) { z80Instance->hl2 = value; }
void LibFz80::SET_im(uint8_t value) { z80Instance->im = value; }
void LibFz80::SET_iff1(bool value) { z80Instance->iff1 = value; }
void LibFz80::SET_iff2(bool value) { z80Instance->iff2 = value; }

// Expose the functions for C# interoperability
extern "C" {

	#ifdef _WIN32
		#define FZ80_EXPORT __declspec(dllexport)
	#else
		#define FZ80_EXPORT __attribute__((visibility("default")))
	#endif

	FZ80_EXPORT LibFz80* CreateLibFz80() {
		return new LibFz80();
	}

	FZ80_EXPORT void DestroyLibFz80(LibFz80* instance) {
		delete instance;
	}

	FZ80_EXPORT uint64_t LibFz80_Initialize(LibFz80* instance) {
		return instance->Initialize();
	}

	FZ80_EXPORT uint64_t LibFz80_Reset(LibFz80* instance) {
		return instance->Reset();
	}

	FZ80_EXPORT uint64_t LibFz80_Tick(LibFz80* instance, uint64_t pins) {
		return instance->Tick(pins);
	}

	FZ80_EXPORT uint64_t LibFz80_Prefetch(LibFz80* instance, uint16_t new_pc) {
		return instance->Prefetch(new_pc);
	}

	FZ80_EXPORT bool LibFz80_InstructionDone(LibFz80* instance) {
		return instance->InstructionDone();
	}


	// Getter functions
	FZ80_EXPORT uint16_t LibFz80_GET_step(LibFz80* instance) { return instance->GET_step(); }
	FZ80_EXPORT uint16_t LibFz80_GET_addr(LibFz80* instance) { return instance->GET_addr(); }
	FZ80_EXPORT uint8_t LibFz80_GET_dlatch(LibFz80* instance) { return instance->GET_dlatch(); }
	FZ80_EXPORT uint8_t LibFz80_GET_opcode(LibFz80* instance) { return instance->GET_opcode(); }
	FZ80_EXPORT uint8_t LibFz80_GET_hlx_idx(LibFz80* instance) { return instance->GET_hlx_idx(); }
	FZ80_EXPORT bool LibFz80_GET_prefix_active(LibFz80* instance) { return instance->GET_prefix_active(); }
	FZ80_EXPORT uint64_t LibFz80_GET_pins(LibFz80* instance) { return instance->GET_pins(); }
	FZ80_EXPORT uint64_t LibFz80_GET_int_bits(LibFz80* instance) { return instance->GET_int_bits(); }
	FZ80_EXPORT uint16_t LibFz80_GET_pc(LibFz80* instance) { return instance->GET_pc(); }
	FZ80_EXPORT uint16_t LibFz80_GET_af(LibFz80* instance) { return instance->GET_af(); }
	FZ80_EXPORT uint16_t LibFz80_GET_bc(LibFz80* instance) { return instance->GET_bc(); }
	FZ80_EXPORT uint16_t LibFz80_GET_de(LibFz80* instance) { return instance->GET_de(); }
	FZ80_EXPORT uint16_t LibFz80_GET_hl(LibFz80* instance) { return instance->GET_hl(); }
	FZ80_EXPORT uint16_t LibFz80_GET_ix(LibFz80* instance) { return instance->GET_ix(); }
	FZ80_EXPORT uint16_t LibFz80_GET_iy(LibFz80* instance) { return instance->GET_iy(); }
	FZ80_EXPORT uint16_t LibFz80_GET_wz(LibFz80* instance) { return instance->GET_wz(); }
	FZ80_EXPORT uint16_t LibFz80_GET_sp(LibFz80* instance) { return instance->GET_sp(); }
	FZ80_EXPORT uint16_t LibFz80_GET_ir(LibFz80* instance) { return instance->GET_ir(); }
	FZ80_EXPORT uint16_t LibFz80_GET_af2(LibFz80* instance) { return instance->GET_af2(); }
	FZ80_EXPORT uint16_t LibFz80_GET_bc2(LibFz80* instance) { return instance->GET_bc2(); }
	FZ80_EXPORT uint16_t LibFz80_GET_de2(LibFz80* instance) { return instance->GET_de2(); }
	FZ80_EXPORT uint16_t LibFz80_GET_hl2(LibFz80* instance) { return instance->GET_hl2(); }
	FZ80_EXPORT uint8_t LibFz80_GET_im(LibFz80* instance) { return instance->GET_im(); }
	FZ80_EXPORT bool LibFz80_GET_iff1(LibFz80* instance) { return instance->GET_iff1(); }
	FZ80_EXPORT bool LibFz80_GET_iff2(LibFz80* instance) { return instance->GET_iff2(); }

	// Setter functions
	FZ80_EXPORT void LibFz80_SET_step(LibFz80* instance, uint16_t value) { instance->SET_step(value); }
	FZ80_EXPORT void LibFz80_SET_addr(LibFz80* instance, uint16_t value) { instance->SET_addr(value); }
	FZ80_EXPORT void LibFz80_SET_dlatch(LibFz80* instance, uint8_t value) { instance->SET_dlatch(value); }
	FZ80_EXPORT void LibFz80_SET_opcode(LibFz80* instance, uint8_t value) { instance->SET_opcode(value); }
	FZ80_EXPORT void LibFz80_SET_hlx_idx(LibFz80* instance, uint8_t value) { instance->SET_hlx_idx(value); }
	FZ80_EXPORT void LibFz80_SET_prefix_active(LibFz80* instance, bool value) { instance->SET_prefix_active(value); }
	FZ80_EXPORT void LibFz80_SET_pins(LibFz80* instance, uint64_t value) { instance->SET_pins(value);	}
	FZ80_EXPORT void LibFz80_SET_int_bits(LibFz80* instance, uint64_t value) { instance->SET_int_bits(value);	}
	FZ80_EXPORT void LibFz80_SET_pc(LibFz80* instance, uint16_t value) { instance->SET_pc(value);	}
	FZ80_EXPORT void LibFz80_SET_af(LibFz80* instance, uint16_t value) { instance->SET_af(value);	}
	FZ80_EXPORT void LibFz80_SET_bc(LibFz80* instance, uint16_t value) { instance->SET_bc(value);	}
	FZ80_EXPORT void LibFz80_SET_de(LibFz80* instance, uint16_t value) { instance->SET_de(value);	}
	FZ80_EXPORT void LibFz80_SET_hl(LibFz80* instance, uint16_t value) { instance->SET_hl(value);	}
	FZ80_EXPORT void LibFz80_SET_ix(LibFz80* instance, uint16_t value) { instance->SET_ix(value);	}
	FZ80_EXPORT void LibFz80_SET_iy(LibFz80* instance, uint16_t value) { instance->SET_iy(value);	}
	FZ80_EXPORT void LibFz80_SET_wz(LibFz80* instance, uint16_t value) { instance->SET_wz(value);	}
	FZ80_EXPORT void LibFz80_SET_sp(LibFz80* instance, uint16_t value) { instance->SET_sp(value);	}
	FZ80_EXPORT void LibFz80_SET_ir(LibFz80* instance, uint16_t value) { instance->SET_ir(value);	}
	FZ80_EXPORT void LibFz80_SET_af2(LibFz80* instance, uint16_t value) { instance->SET_af2(value); }
	FZ80_EXPORT void LibFz80_SET_bc2(LibFz80* instance, uint16_t value) { instance->SET_bc2(value); }
	FZ80_EXPORT void LibFz80_SET_de2(LibFz80* instance, uint16_t value) { instance->SET_de2(value); }
	FZ80_EXPORT void LibFz80_SET_hl2(LibFz80* instance, uint16_t value) { instance->SET_hl2(value); }
	FZ80_EXPORT void LibFz80_SET_im(LibFz80* instance, uint8_t value) { instance->SET_im(value); }
	FZ80_EXPORT void LibFz80_SET_iff1(LibFz80* instance, bool value) { instance->SET_iff1(value);	}
	FZ80_EXPORT void LibFz80_SET_iff2(LibFz80* instance, bool value) { instance->SET_iff2(value);	}
}