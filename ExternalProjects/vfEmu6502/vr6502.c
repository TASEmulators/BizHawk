#include "vrEmu6502/src/vrEmu6502.h"

#ifdef _WIN32
#define VR_EMU_6502_DLLEXPORT __declspec(dllexport)
#else
#define VR_EMU_6502_DLLEXPORT __attribute__((visibility("default")))
#endif