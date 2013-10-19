#ifndef AVIOUT_H_INCLUDED
#define AVIOUT_H_INCLUDED

#include "windows.h"

#ifdef __cplusplus
extern "C" {
#endif
int DRV_AviIsRecording();
int DRV_AviBegin(const char* fname, HWND HWnd);

void DRV_AviEnd();
void DRV_AviVideoUpdate(const u16* buffer, HWND HWnd);
void DRV_AviSoundUpdate(void* soundData, int soundLen);
#ifdef __cplusplus
}
#endif
#endif