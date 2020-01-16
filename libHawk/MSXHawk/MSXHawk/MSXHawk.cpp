// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "MSXHawk.h"
#include "Core.h"

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace MSXHawk;

// Create pointer to a core instance
MSXHAWK_EXPORT MSXCore* MSX_create()
{
	return new MSXCore();
}

// free the memory from the core pointer
MSXHAWK_EXPORT void MSX_destroy(MSXCore* p)
{
	std::free(p);
}

// load a rom into the core
MSXHAWK_EXPORT void MSX_load(MSXCore* p, uint8_t* rom, unsigned int size, int mapper)
{
	p->Load_ROM(rom, size, mapper);
}

// advance a frame
MSXHAWK_EXPORT void MSX_frame_advance(MSXCore* p, uint8_t ctrl1, uint8_t ctrl2, bool render, bool sound)
{
	p->FrameAdvance(ctrl1, ctrl2, render, sound);
}

// send video data to external video provider
MSXHAWK_EXPORT void MSX_get_video(MSXCore* p, uint32_t* dest)
{
	p->GetVideo(dest);
}

// set tracer callback
MSXHAWK_EXPORT void MSX_settracecallback(MSXCore* p, void (*callback)(int)) {
	p->SetTraceCallback(callback);
}

#pragma region Tracer

// return the cpu trace header length
MSXHAWK_EXPORT int MSX_getheaderlength(MSXCore* p) {
	return p->GetHeaderLength();
}

// return the cpu disassembly length
MSXHAWK_EXPORT int MSX_getdisasmlength(MSXCore* p) {
	return p->GetDisasmLength();
}

// return the cpu register string length
MSXHAWK_EXPORT int MSX_getregstringlength(MSXCore* p) {
	return p->GetRegStringLength();
}

// return the cpu trace header
MSXHAWK_EXPORT void MSX_getheader(MSXCore* p, char* h, int l) {
	p->GetHeader(h, l);
}

// return the cpu register state
MSXHAWK_EXPORT void MSX_getregisterstate(MSXCore* p, char* r, int t, int l) {
	p->GetRegisterState(r, t, l);
}

// return the cpu disassembly
MSXHAWK_EXPORT void MSX_getdisassembly(MSXCore* p, char* d, int t, int l) {
	p->GetDisassembly(d, t, l);
}

#pragma endregion