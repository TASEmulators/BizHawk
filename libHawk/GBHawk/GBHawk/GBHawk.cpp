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
	delete p->MemMap.ROM;
	std::free(p);
}

// load bios into the core
GBHawk_EXPORT void GB_load_bios(GBCore* p, uint8_t* bios, bool GBC_console, bool GBC_as_GBA)
{
	p->Load_BIOS(bios, GBC_console, GBC_as_GBA);
}

// load a rom into the core
GBHawk_EXPORT void GB_load(GBCore* p, uint8_t* rom_1, uint32_t size_1, char* MD5, uint32_t RTC_initial, uint32_t RTC_offset)
{
	string MD5_s(MD5, 32);
	
	p->Load_ROM(rom_1, size_1, MD5_s, RTC_initial, RTC_offset);
}

// Hard reset (note: does not change RTC, that only happens on load)
GBHawk_EXPORT void GB_Reset(GBCore* p)
{
	p->Reset();
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
GBHawk_EXPORT uint32_t GB_get_audio(GBCore* p, int32_t* dest_L, int32_t* n_samp_L, int32_t* dest_R, int32_t* n_samp_R)
{
	return p->GetAudio(dest_L, n_samp_L, dest_R, n_samp_R);
}
#pragma endregion

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

GBHawk_EXPORT uint8_t GB_getram(GBCore* p, uint32_t addr) {
	return p->GetRAM(addr);
}

GBHawk_EXPORT uint8_t GB_getvram(GBCore* p, uint32_t addr) {
	return p->GetVRAM(addr);
}

GBHawk_EXPORT uint8_t GB_getoam(GBCore* p, uint32_t addr) {
	return p->GetOAM(addr);
}

GBHawk_EXPORT uint8_t GB_gethram(GBCore* p, uint32_t addr) {
	return p->GetHRAM(addr);
}

GBHawk_EXPORT uint8_t GB_getsysbus(GBCore* p, uint32_t addr) {
	return p->GetSysBus(addr);
}

GBHawk_EXPORT void GB_setram(GBCore* p, uint32_t addr, uint8_t value) {
	 p->SetRAM(addr, value);
}

GBHawk_EXPORT void GB_setvram(GBCore* p, uint32_t addr, uint8_t value) {
	 p->SetVRAM(addr, value);
}

GBHawk_EXPORT void GB_setoam(GBCore* p, uint32_t addr, uint8_t value) {
	 p->SetOAM(addr, value);
}

GBHawk_EXPORT void GB_sethram(GBCore* p, uint32_t addr, uint8_t value) {
	 p->SetHRAM(addr, value);
}

GBHawk_EXPORT void GB_setsysbus(GBCore* p, uint32_t addr, uint8_t value) {
	 p->SetSysBus(addr, value);
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

#pragma region PPU Viewer

// set tracer callback
GBHawk_EXPORT uint8_t* GB_get_ppu_pntrs(GBCore* p, int sel) {
	
	if (p->MemMap.is_GBC) 
	{
		switch (sel)
		{
		case 0: return p->MemMap.VRAM; break;
		case 1: return p->MemMap.OAM; break;
		case 2: return (uint8_t*)p->ppu->OBJ_palette; break;
		case 3: return (uint8_t*)p->ppu->BG_palette; break;
		}
	}
	else 
	{
		// need to fix this for GB
		switch (sel)
		{
		case 0: return p->MemMap.VRAM; break;
		case 1: return p->MemMap.OAM; break;
		case 2: return (uint8_t*)p->ppu->OBJ_palette; break;
		case 3: return (uint8_t*)p->ppu->BG_palette; break;
		}
	}

	return nullptr;
}

// return LCDC state for the ppu viewer
GBHawk_EXPORT uint8_t GB_get_LCDC(GBCore* p) {
	return p->ppu->LCDC;
}

// set scanline callback
GBHawk_EXPORT void GB_setscanlinecallback(GBCore* p, void (*callback)(void), int sl) {
	p->SetScanlineCallback(callback, sl);
}

#pragma endregion

#pragma region Debuggable functions

// return cpu cycle count
GBHawk_EXPORT uint64_t GB_cpu_cycles(GBCore* p) {
	return p->cpu.TotalExecutedCycles;
}

// return cpu registers
GBHawk_EXPORT uint8_t GB_cpu_get_regs(GBCore* p, int reg) {
	return p->cpu.Regs[reg];
}

// return cpu flags
GBHawk_EXPORT bool GB_cpu_get_flags(GBCore* p, int reg) {
	bool ret = false;

	switch (reg)
	{
	case (0): ret = p->cpu.FlagI; break;
	case (1): ret = p->cpu.FlagCget(); break;
	case (2): ret = p->cpu.FlagHget(); break;
	case (3): ret = p->cpu.FlagNget(); break;
	case (4): ret = p->cpu.FlagZget(); break;
	}

	return ret;
}

// set cpu registers
GBHawk_EXPORT void GB_cpu_set_regs(GBCore* p, int reg, uint8_t value) {
	p->cpu.Regs[reg] = value;
}

// set cpu flags
GBHawk_EXPORT void GB_cpu_set_flags(GBCore* p, int reg, bool value) {

	switch (reg)
	{
	case (0): p->cpu.FlagI = value; break;
	case (1): p->cpu.FlagCset(value); break;
	case (2): p->cpu.FlagHset(value); break;
	case (3): p->cpu.FlagNset(value); break;
	case (4): p->cpu.FlagZset(value); break;
	}
}

#pragma endregion