#pragma once

#include "psx.h"

__declspec(dllexport) bool Load_EXE_Check(const char* fname);
__declspec(dllexport) void Load_EXE(PSX& psx, const wchar_t* fname);
__declspec(dllexport) void Load_BIOS(PSX& psx, const char* path);