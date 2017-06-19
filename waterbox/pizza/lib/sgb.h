#pragma once
#include <stdint.h>

void sgb_write_ff00(uint8_t val, uint64_t time);

uint8_t sgb_read_ff00(uint64_t time);

void sgb_set_controller_data(const uint8_t* buttons);

void sgb_init(void);

void sgb_take_frame(uint32_t* vbuff);

void sgb_render_frame(uint32_t* vbuff);
