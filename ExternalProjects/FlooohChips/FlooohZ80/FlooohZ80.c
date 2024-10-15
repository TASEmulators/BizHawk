#define CHIPS_IMPL
#include "../chips/chips/z80.h"

#ifdef _WIN32
	#define FZ80_EXPORT __declspec(dllexport)
#else
	#define FZ80_EXPORT __attribute__((visibility("default")))
#endif

FZ80_EXPORT uint64_t LibFz80_Initialize(z80_t* z80)
{
	return z80_init(z80);
}

FZ80_EXPORT uint64_t LibFz80_Reset(z80_t* z80)
{
	return z80_reset(z80);
}

FZ80_EXPORT uint64_t LibFz80_Tick(z80_t* z80, uint64_t pins)
{
	return z80_tick(z80, pins);
}

FZ80_EXPORT uint64_t LibFz80_Prefetch(z80_t* z80, uint16_t new_pc)
{
	return z80_prefetch(z80, new_pc);
}

FZ80_EXPORT bool LibFz80_InstructionDone(z80_t* z80)
{
	return z80_opdone(z80);
}
