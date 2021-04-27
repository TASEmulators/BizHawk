#include <emulator/emulator.hpp>

#undef register
#define register
#include "sai/sai.cpp"

uint32_t* colortable;
#include "snes_ntsc/snes_ntsc.h"
#include "snes_ntsc/snes_ntsc.c"

#include "none.cpp"
#include "scanlines-light.cpp"
#include "scanlines-dark.cpp"
#include "scanlines-black.cpp"
#include "pixellate2x.cpp"
#include "scale2x.cpp"
#include "2xsai.cpp"
#include "super-2xsai.cpp"
#include "super-eagle.cpp"
#include "lq2x.cpp"
#include "hq2x.cpp"
#include "ntsc-rf.cpp"
#include "ntsc-composite.cpp"
#include "ntsc-svideo.cpp"
#include "ntsc-rgb.cpp"
