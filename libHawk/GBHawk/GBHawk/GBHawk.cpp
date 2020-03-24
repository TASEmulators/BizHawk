// GBHawk.cpp : Defines the exported functions for the DLL.
//

#include "GBHawk.h"
#include "Core.h"

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace GBHawk;

#pragma region Core
// Create pointer to a core instance
GBHawk_EXPORT GBCore* GB_create()
{
	return new GBCore();
}

// free the memory from the core pointer
GBHawk_EXPORT void GB_destroy(GBCore* p)
{
	delete p->MemMap.bios_rom;
	delete p->MemMap.basic_rom;
	delete p->MemMap.rom_1;
	delete p->MemMap.rom_2;
	std::free(p);
}

// load bios and basic into the core
GBHawk_EXPORT void GB_load_bios(GBCore* p, uint8_t* bios, uint8_t* basic)
{
	p->Load_BIOS(bios, basic);
}

// load a rom into the core
GBHawk_EXPORT void GB_load(GBCore* p, uint8_t* rom_1, uint32_t size_1, uint32_t mapper_1, uint8_t* rom_2, uint32_t size_2, uint8_t mapper_2)
{
	p->Load_ROM(rom_1, size_1, mapper_1, rom_2, size_2, mapper_2);
}

// advance a frame
GBHawk_EXPORT bool GB_frame_advance(GBCore* p, uint8_t ctrl1, uint8_t ctrl2, uint8_t* kbrows, bool render, bool sound)
{
	return p->FrameAdvance(ctrl1, ctrl2, kbrows, render, sound);
}

// send video data to external video provider
GBHawk_EXPORT void GB_get_video(GBCore* p, uint32_t* dest)
{
	p->GetVideo(dest);
}

// send audio data to external audio provider
GBHawk_EXPORT uint32_t GB_get_audio(GBCore* p, int32_t* dest, int32_t* n_samp)
{
	return p->GetAudio(dest, n_samp);
}

#pragma region State Save / Load

// save state
GBHawk_EXPORT void GB_save_state(GBCore* p, uint8_t* saver)
{
	p->SaveState(saver);
}

// load state
GBHawk_EXPORT void GB_load_state(GBCore* p, uint8_t* loader)
{
	p->LoadState(loader);
}

#pragma endregion

#pragma region Memory Domain Functions

GBHawk_EXPORT uint8_t GB_getsysbus(GBCore* p, uint32_t addr) {
	return p->GetSysBus(addr);
}

GBHawk_EXPORT uint8_t GB_getvram(GBCore* p, uint32_t addr) {
	return p->GetVRAM(addr);
}

GBHawk_EXPORT uint8_t GB_getram(GBCore* p, uint32_t addr) {
	return p->GetRAM(addr);
}

#pragma endregion


#pragma region Tracer

// set tracer callback
GBHawk_EXPORT void GB_settracecallback(GBCore* p, void (*callback)(int)) {
	p->SetTraceCallback(callback);
}

// return the cpu trace header length
GBHawk_EXPORT int GB_getheaderlength(GBCore* p) {
	return p->GetHeaderLength();
}

// return the cpu disassembly length
GBHawk_EXPORT int GB_getdisasmlength(GBCore* p) {
	return p->GetDisasmLength();
}

// return the cpu register string length
GBHawk_EXPORT int GB_getregstringlength(GBCore* p) {
	return p->GetRegStringLength();
}

// return the cpu trace header
GBHawk_EXPORT void GB_getheader(GBCore* p, char* h, int l) {
	p->GetHeader(h, l);
}

// return the cpu register state
GBHawk_EXPORT void GB_getregisterstate(GBCore* p, char* r, int t, int l) {
	p->GetRegisterState(r, t, l);
}

// return the cpu disassembly
GBHawk_EXPORT void GB_getdisassembly(GBCore* p, char* d, int t, int l) {
	p->GetDisassembly(d, t, l);
}

#pragma endregion