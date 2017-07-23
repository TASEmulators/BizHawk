#pragma once
#include <stdint.h>

// whenever a time is asked for, it is relative to a clock that ticks 35112 times
// per nominal frame on the GB lcd, starts at 0 when emulation begins, and never resets/rebases

// write to MMIO ff00. only bits 4 and 5 are used
void sgb_write_ff00(uint8_t val, uint64_t time);

// read from MMIO ff00. supplies data for all 8 bits
uint8_t sgb_read_ff00(uint64_t time);

// set controller data to be used by subsequent controller reads
// buttons[0] = controller 1, buttons[3] = controller 4
// 7......0
// DULRSsBA
void sgb_set_controller_data(const uint8_t* buttons);

// initialize the SGB module.  pass an SPC file that results from the real S-CPU initialization,
// and the length of that file
int sgb_init(const uint8_t* spc, int length);

// call whenever the gameboy has finished producing a video frame
// data is 32bpp 160x144 screen data.  for each pixel:
//31                          7      0
// xxxxxxxx xxxxxxxx xxxxxxxx DDxxxxxx -- DD = 0, 1, 2, or 3.  x = don't care
void sgb_take_frame(uint32_t* vbuff);

// copy the finished video frame to an output buffer.  pixel format is 32bpp xrgb
// can be called at any time, including right after sgb_take_frame
void sgb_render_frame(uint32_t* vbuff);

// call to finish a frame's worth of audio. should be called once every 35112 time units (some jitter is OK)
// callback will be called with L and R sample values for various time points
// between the last time sgb_render_audio was called and now
void sgb_render_audio(uint64_t time, void(*callback)(int16_t l, int16_t r, uint64_t time));
