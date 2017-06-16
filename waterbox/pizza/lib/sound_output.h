#pragma once

#include <stdint.h>

void blip_left(int delta);
void blip_right(int delta);

void sound_output_init(double clock_rate, double sample_rate);
int sound_output_read(int16_t* output);
