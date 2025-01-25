/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef PSG_CONTEXT_H_
#define PSG_CONTEXT_H_

#include <stdint.h>
#include "serialize.h"
#include "render_audio.h"
#include "vgm.h"

typedef struct {
	audio_source *audio;
	vgm_writer   *vgm;
	uint32_t clock_inc;
	uint32_t cycles;
	uint16_t lsfr;
	uint16_t counter_load[4];
	uint16_t counters[4];
	uint8_t  volume[4];
	uint8_t  output_state[4];
	uint8_t  noise_out;
	uint8_t  noise_use_tone;
	uint8_t  noise_type;
	uint8_t  latch;
} psg_context;


void psg_init(psg_context * context, uint32_t master_clock, uint32_t clock_div);
void psg_free(psg_context *context);
void psg_adjust_master_clock(psg_context * context, uint32_t master_clock);
void psg_write(psg_context * context, uint8_t value);
void psg_run(psg_context * context, uint32_t cycles);
void psg_vgm_log(psg_context *context, uint32_t master_clock, vgm_writer *vgm);
void psg_serialize(psg_context *context, serialize_buffer *buf);
void psg_deserialize(deserialize_buffer *buf, void *vcontext);

#endif //PSG_CONTEXT_H_

