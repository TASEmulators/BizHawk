// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "MSXHawk.h"
#include "Core.h"

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace MSXHawk;

#pragma region Core
// Create pointer to a core instance
MSXHawk_EXPORT MSXCore* MSX_create()
{
	return new MSXCore();
}

// free the memory from the core pointer
MSXHawk_EXPORT void MSX_destroy(MSXCore* p)
{
	delete p->MemMap.bios_rom;
	delete p->MemMap.basic_rom;
	delete p->MemMap.rom_1;
	delete p->MemMap.rom_2;
	std::free(p);
}

// load bios and basic into the core
MSXHawk_EXPORT void MSX_load_bios(MSXCore* p, uint8_t* bios, uint8_t* basic)
{
	p->Load_BIOS(bios, basic);
}

// load a rom into the core
MSXHawk_EXPORT void MSX_load(MSXCore* p, uint8_t* rom_1, uint32_t size_1, uint32_t mapper_1, uint8_t* rom_2, uint32_t size_2, uint8_t mapper_2)
{
	p->Load_ROM(rom_1, size_1, mapper_1, rom_2, size_2, mapper_2);
}

// advance a frame
MSXHawk_EXPORT bool MSX_frame_advance(MSXCore* p, uint8_t ctrl1, uint8_t ctrl2, bool render, bool sound)
{
	p->FrameAdvance(ctrl1, ctrl2, render, sound);
	return p->vdp.EnableInterrupts();
}

// send video data to external video provider
MSXHawk_EXPORT void MSX_get_video(MSXCore* p, uint32_t* dest)
{
	p->GetVideo(dest);
}

// send audio data to external audio provider
MSXHawk_EXPORT uint32_t MSX_get_audio(MSXCore* p, int32_t* dest, int32_t* n_samp)
{
	return p->GetAudio(dest, n_samp);
}

#pragma region State Save / Load

// save state
MSXHawk_EXPORT void MSX_save_state(MSXCore* p, uint8_t* saver)
{
	p->SaveState(saver);
}

// load state
MSXHawk_EXPORT void MSX_load_state(MSXCore* p, uint8_t* loader)
{
	p->LoadState(loader);
}

#pragma endregion

#pragma region Memory Domain Functions

MSXHawk_EXPORT uint8_t MSX_getsysbus(MSXCore* p, uint32_t addr) {
	return p->GetSysBus(addr);
}

MSXHawk_EXPORT uint8_t MSX_getvram(MSXCore* p, uint32_t addr) {
	return p->GetVRAM(addr);
}
#pragma endregion


#pragma region Tracer

// set tracer callback
MSXHawk_EXPORT void MSX_settracecallback(MSXCore* p, void (*callback)(int)) {
	p->SetTraceCallback(callback);
}

// return the cpu trace header length
MSXHawk_EXPORT int MSX_getheaderlength(MSXCore* p) {
	return p->GetHeaderLength();
}

// return the cpu disassembly length
MSXHawk_EXPORT int MSX_getdisasmlength(MSXCore* p) {
	return p->GetDisasmLength();
}

// return the cpu register string length
MSXHawk_EXPORT int MSX_getregstringlength(MSXCore* p) {
	return p->GetRegStringLength();
}

// return the cpu trace header
MSXHawk_EXPORT void MSX_getheader(MSXCore* p, char* h, int l) {
	p->GetHeader(h, l);
}

// return the cpu register state
MSXHawk_EXPORT void MSX_getregisterstate(MSXCore* p, char* r, int t, int l) {
	p->GetRegisterState(r, t, l);
}

// return the cpu disassembly
MSXHawk_EXPORT void MSX_getdisassembly(MSXCore* p, char* d, int t, int l) {
	p->GetDisassembly(d, t, l);
}

#pragma endregion