/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include "wave.h"
#include <stddef.h>
#include <string.h>

int wave_init(FILE * f, uint32_t sample_rate, uint16_t bits_per_sample, uint16_t num_channels)
{
	wave_header header;
	memcpy(header.chunk.id, "RIFF", 4);
	memcpy(header.chunk.format, "WAVE", 4);
	header.chunk.size = 0; //This will be filled in later
	memcpy(header.format_header.id, "fmt ", 4);
	header.format_header.size = sizeof(wave_header) - (sizeof(header.chunk) + sizeof(header.data_header) + sizeof(header.format_header));
	header.audio_format = 1;
	header.num_channels = num_channels;
	header.sample_rate = sample_rate;
	header.byte_rate = sample_rate * num_channels * (bits_per_sample/8);
	header.block_align = num_channels * (bits_per_sample/8);
	header.bits_per_sample = bits_per_sample;
	memcpy(header.data_header.id, "data", 4);
	header.data_header.size = 0;//This will be filled in later;
	return fwrite(&header, 1, sizeof(header), f) == sizeof(header);
}

int wave_finalize(FILE * f)
{
	uint32_t size = ftell(f);
	fseek(f, offsetof(wave_header, chunk.size), SEEK_SET);
	size -= 8;
	if (fwrite(&size, sizeof(size), 1, f) != 1) {
		fclose(f);
		return 0;
	}
	fseek(f, offsetof(wave_header, data_header.size), SEEK_SET);
	size -= 36;
	if (fwrite(&size, sizeof(size), 1, f) != 1) {
		fclose(f);
		return 0;
	}
	fclose(f);
	return 1;
}
