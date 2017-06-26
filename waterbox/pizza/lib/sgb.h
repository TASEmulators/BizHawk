#pragma once
#include <stdint.h>

void sgb_write_ff00(uint8_t val, uint64_t time);

uint8_t sgb_read_ff00(uint64_t time);

void sgb_set_controller_data(const uint8_t* buttons);

int sgb_init(const uint8_t* spc, int length);

void sgb_take_frame(uint32_t* vbuff);

void sgb_render_frame(uint32_t* vbuff);

void sgb_render_audio(uint64_t time, void(*callback)(int16_t l, int16_t r, uint64_t time));
