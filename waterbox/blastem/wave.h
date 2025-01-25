/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm. 
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef WAVE_H_
#define WAVE_H_

#include <stdint.h>
#include <stdio.h>

#pragma pack(push, 1)

typedef struct {
	char     id[4];
	uint32_t size;
	char     format[4];
} riff_chunk;

typedef struct {
	char     id[4];
	uint32_t size;
} riff_sub_chunk;

typedef struct {
	riff_chunk     chunk;
	riff_sub_chunk format_header;
	uint16_t       audio_format;
	uint16_t       num_channels;
	uint32_t       sample_rate;
	uint32_t       byte_rate;
	uint16_t       block_align;
	uint16_t       bits_per_sample;
	riff_sub_chunk data_header;
} wave_header;

#pragma pack(pop)

int wave_init(FILE * f, uint32_t sample_rate, uint16_t bits_per_sample, uint16_t num_channels);
int wave_finalize(FILE * f);

#endif //WAVE_H_

