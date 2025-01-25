#include <limits.h>
#include <string.h>
#include <stdlib.h>
#include <math.h>
#include "render_audio.h"
#include "util.h"
#include "config.h"
#include "blastem.h"

static uint8_t output_channels;
static uint32_t buffer_samples, sample_rate;

static audio_source *audio_sources[8];
static audio_source *inactive_audio_sources[8];
static uint8_t num_audio_sources;
static uint8_t num_inactive_audio_sources;

static float overall_gain_mult, *mix_buf;
static int sample_size;

typedef void (*conv_func)(float *samples, void *vstream, int sample_count);

static void convert_null(float *samples, void *vstream, int sample_count)
{
	memset(vstream, 0, sample_count * sample_size);
}

static void convert_s16(float *samples, void *vstream, int sample_count)
{
	int16_t *stream = vstream;
	for (int16_t *end = stream + sample_count; stream < end; stream++, samples++)
	{
		float sample = *samples;
		int16_t out_sample;
		if (sample >= 1.0f) {
			out_sample = 0x7FFF;
		} else if (sample <= -1.0f) {
			out_sample = -0x8000;
		} else {
			out_sample = sample * 0x7FFF;
		}
		*stream = out_sample;
	}
}

static void clamp_f32(float *samples, void *vstream, int sample_count)
{
	for (; sample_count > 0; sample_count--, samples++)
	{
		float sample = *samples;
		if (sample > 1.0f) {
			sample = 1.0f;
		} else if (sample < -1.0f) {
			sample = -1.0f;
		}
		*samples = sample;
	}
}

static int32_t mix_f32(audio_source *audio, float *stream, int samples)
{
	float *end = stream + samples;
	int16_t *src = audio->front;
	uint32_t i = audio->read_start;
	uint32_t i_end = audio->read_end;
	float *cur = stream;
	float gain_mult = audio->gain_mult * overall_gain_mult;
	size_t first_add = output_channels > 1 ? 1 : 0, second_add = output_channels > 1 ? output_channels - 1 : 1;
	if (audio->num_channels == 1) {
		while (cur < end && i != i_end)
		{
			*cur += gain_mult * ((float)src[i]) / 0x7FFF;
			cur += first_add;
			*cur += gain_mult * ((float)src[i++]) / 0x7FFF;
			cur += second_add;
			i &= audio->mask;
		}
	} else {
		while(cur < end && i != i_end)
		{
			*cur += gain_mult * ((float)src[i++]) / 0x7FFF;
			cur += first_add;
			*cur += gain_mult * ((float)src[i++]) / 0x7FFF;
			cur += second_add;
			i &= audio->mask;
		}
	}
	if (!render_is_audio_sync()) {
		audio->read_start = i;
	}
	if (cur != end) {
		debug_message("Underflow of %d samples, read_start: %d, read_end: %d, mask: %X\n", (int)(end-cur)/2, audio->read_start, audio->read_end, audio->mask);
		return (cur-end)/2;
	} else {
		return ((i_end - i) & audio->mask) / audio->num_channels;
	}
}

static conv_func convert;


int mix_and_convert(unsigned char *byte_stream, int len, int *min_remaining_out)
{
	int samples = len / sample_size;
	float *mix_dest = mix_buf ? mix_buf : (float *)byte_stream;
	memset(mix_dest, 0, samples * sizeof(float));
	int min_buffered = INT_MAX;
	int min_remaining_buffer = INT_MAX;
	for (uint8_t i = 0; i < num_audio_sources; i++)
	{
		int buffered = mix_f32(audio_sources[i], mix_dest, samples);
		int remaining = (audio_sources[i]->mask + 1) / audio_sources[i]->num_channels - buffered;
		min_buffered = buffered < min_buffered ? buffered : min_buffered;
		min_remaining_buffer = remaining < min_remaining_buffer ? remaining : min_remaining_buffer;
		audio_sources[i]->front_populated = 0;
		render_buffer_consumed(audio_sources[i]);
	}
	convert(mix_dest, byte_stream, samples);
	if (min_remaining_out) {
		*min_remaining_out = min_remaining_buffer;
	}
	return min_buffered;
}

uint8_t all_sources_ready(void)
{
	uint8_t num_populated = 0;
	num_populated = 0;
	for (uint8_t i = 0; i < num_audio_sources; i++)
	{
		if (audio_sources[i]->front_populated) {
			num_populated++;
		}
	}
	return num_populated == num_audio_sources;
}

#define BUFFER_INC_RES 0x40000000UL

void render_audio_adjust_clock(audio_source *src, uint64_t master_clock, uint64_t sample_divider)
{
	src->buffer_inc = ((BUFFER_INC_RES * (uint64_t)sample_rate) / master_clock) * sample_divider;
}

void render_audio_adjust_speed(float adjust_ratio)
{
	for (uint8_t i = 0; i < num_audio_sources; i++)
	{
		audio_sources[i]->buffer_inc = ((double)audio_sources[i]->buffer_inc) + ((double)audio_sources[i]->buffer_inc) * adjust_ratio + 0.5;
	}
}

audio_source *render_audio_source(uint64_t master_clock, uint64_t sample_divider, uint8_t channels)
{
	audio_source *ret = NULL;
	uint32_t alloc_size = render_is_audio_sync() ? channels * buffer_samples : nearest_pow2(render_min_buffered() * 4 * channels);
	render_lock_audio();
		if (num_audio_sources < 8) {
			ret = calloc(1, sizeof(audio_source));
			ret->back = malloc(alloc_size * sizeof(int16_t));
			ret->front = render_is_audio_sync() ? malloc(alloc_size * sizeof(int16_t)) : ret->back;
			ret->front_populated = 0;
			ret->opaque = render_new_audio_opaque();
			ret->num_channels = channels;
			audio_sources[num_audio_sources++] = ret;
		}
	render_unlock_audio();
	if (!ret) {
		fatal_error("Too many audio sources!");
	} else {
		render_audio_adjust_clock(ret, master_clock, sample_divider);
		double lowpass_cutoff = get_lowpass_cutoff(config);
		double rc = (1.0 / lowpass_cutoff) / (2.0 * M_PI);
		ret->dt = 1.0 / ((double)master_clock / (double)(sample_divider));
		double alpha = ret->dt / (ret->dt + rc);
		ret->lowpass_alpha = (int32_t)(((double)0x10000) * alpha);
		ret->buffer_pos = 0;
		ret->buffer_fraction = 0;
		ret->last_left = ret->last_right = 0;
		ret->read_start = 0;
		ret->read_end = render_is_audio_sync() ? buffer_samples * channels : 0;
		ret->mask = render_is_audio_sync() ? 0xFFFFFFFF : alloc_size-1;
		ret->gain_mult = 1.0f;
	}
	render_audio_created(ret);
	
	return ret;
}


static float db_to_mult(float gain)
{
	return powf(10.0f, gain/20.0f);
}

void render_audio_source_gaindb(audio_source *src, float gain)
{
	src->gain_mult = db_to_mult(gain);
}

void render_pause_source(audio_source *src)
{
	uint8_t found = 0, remaining_sources;
	render_lock_audio();
		for (uint8_t i = 0; i < num_audio_sources; i++)
		{
			if (audio_sources[i] == src) {
				audio_sources[i] = audio_sources[--num_audio_sources];
				found = 1;
				remaining_sources = num_audio_sources;
				break;
			}
		}
		
	render_unlock_audio();
	if (found) {
		render_source_paused(src, remaining_sources);
	}
	inactive_audio_sources[num_inactive_audio_sources++] = src;
}

void render_resume_source(audio_source *src)
{
	render_lock_audio();
		if (num_audio_sources < 8) {
			audio_sources[num_audio_sources++] = src;
		}
	render_unlock_audio();
	for (uint8_t i = 0; i < num_inactive_audio_sources; i++)
	{
		if (inactive_audio_sources[i] == src) {
			inactive_audio_sources[i] = inactive_audio_sources[--num_inactive_audio_sources];
		}
	}
	render_source_resumed(src);
}

void render_free_source(audio_source *src)
{
	uint8_t found = 0;
	for (uint8_t i = 0; i < num_inactive_audio_sources; i++)
	{
		if (inactive_audio_sources[i] == src) {
			inactive_audio_sources[i] = inactive_audio_sources[--num_inactive_audio_sources];
			found = 1;
			break;
		}
	}
	if (!found) {
		render_pause_source(src);
		num_inactive_audio_sources--;
	}
	
	free(src->front);
	if (render_is_audio_sync()) {
		free(src->back);
		render_free_audio_opaque(src->opaque);
	}
	free(src);
}

static int16_t lowpass_sample(audio_source *src, int16_t last, int16_t current)
{
	int32_t tmp = current * src->lowpass_alpha + last * (0x10000 - src->lowpass_alpha);
	current = tmp >> 16;
	return current;
}

static void interp_sample(audio_source *src, int16_t last, int16_t current)
{
	int64_t tmp = last * ((src->buffer_fraction << 16) / src->buffer_inc);
	tmp += current * (0x10000 - ((src->buffer_fraction << 16) / src->buffer_inc));
	src->back[src->buffer_pos++] = tmp >> 16;
}

static uint32_t sync_samples;
void render_put_mono_sample(audio_source *src, int16_t value)
{
	value = lowpass_sample(src, src->last_left, value);
	src->buffer_fraction += src->buffer_inc;
	uint32_t base = render_is_audio_sync() ? 0 : src->read_end;
	while (src->buffer_fraction > BUFFER_INC_RES)
	{
		src->buffer_fraction -= BUFFER_INC_RES;
		interp_sample(src, src->last_left, value);
		
		if (((src->buffer_pos - base) & src->mask) >= sync_samples) {
			render_do_audio_ready(src);
		}
		src->buffer_pos &= src->mask;
	}
	src->last_left = value;
}

void render_put_stereo_sample(audio_source *src, int16_t left, int16_t right)
{
	left = lowpass_sample(src, src->last_left, left);
	right = lowpass_sample(src, src->last_right, right);
	src->buffer_fraction += src->buffer_inc;
	uint32_t base = render_is_audio_sync() ? 0 : src->read_end;
	while (src->buffer_fraction > BUFFER_INC_RES)
	{
		src->buffer_fraction -= BUFFER_INC_RES;
		
		interp_sample(src, src->last_left, left);
		interp_sample(src, src->last_right, right);
		
		if (((src->buffer_pos - base) & src->mask)/2 >= sync_samples) {
			render_do_audio_ready(src);
		}
		src->buffer_pos &= src->mask;
	}
	src->last_left = left;
	src->last_right = right;
}

static void update_source(audio_source *src, double rc, uint8_t sync_changed)
{
	double alpha = src->dt / (src->dt + rc);
	int32_t lowpass_alpha = (int32_t)(((double)0x10000) * alpha);
	src->lowpass_alpha = lowpass_alpha;
	if (sync_changed) {
		uint32_t alloc_size = render_is_audio_sync() ? src->num_channels * buffer_samples : nearest_pow2(render_min_buffered() * 4 * src->num_channels);
		src->back = realloc(src->back, alloc_size * sizeof(int16_t));
		if (render_is_audio_sync()) {
			src->front = malloc(alloc_size * sizeof(int16_t));
		} else {
			free(src->front);
			src->front = src->back;
		}
		src->mask = render_is_audio_sync() ? 0xFFFFFFFF : alloc_size-1;
		src->read_start = 0;
		src->read_end = render_is_audio_sync() ? buffer_samples * src->num_channels : 0;
		src->buffer_pos = 0;
	}
}

uint8_t old_audio_sync;
void render_audio_initialized(render_audio_format format, uint32_t rate, uint8_t channels, uint32_t buffer_size, int sample_size_in)
{
	sample_rate = rate;
	output_channels = channels;
	buffer_samples = buffer_size;
	sample_size = sample_size_in;
	if (mix_buf) {
		free(mix_buf);
		mix_buf = NULL;
	}
	switch(format)
	{
	case RENDER_AUDIO_S16:
		convert = convert_s16;
		mix_buf = calloc(output_channels * buffer_samples, sizeof(float));
		break;
	case RENDER_AUDIO_FLOAT:
		convert = clamp_f32;
		break;
	case RENDER_AUDIO_UNKNOWN:
		convert = convert_null;
		mix_buf = calloc(output_channels * buffer_samples, sizeof(float));
		break;
	}
	uint32_t syncs = render_audio_syncs_per_sec();
	if (syncs) {
		sync_samples = rate / syncs;
	} else {
		sync_samples = buffer_samples;
	}
	char * gain_str = tern_find_path(config, "audio\0gain\0", TVAL_PTR).ptrval;
	overall_gain_mult = db_to_mult(gain_str ? atof(gain_str) : 0.0f);
	uint8_t sync_changed = old_audio_sync != render_is_audio_sync();
	old_audio_sync = render_is_audio_sync();
	double lowpass_cutoff = get_lowpass_cutoff(config);
	double rc = (1.0 / lowpass_cutoff) / (2.0 * M_PI);
	render_lock_audio();
		for (uint8_t i = 0; i < num_audio_sources; i++)
		{
			update_source(audio_sources[i], rc, sync_changed);
		}
	render_unlock_audio();
	for (uint8_t i = 0; i < num_inactive_audio_sources; i++)
	{
		update_source(inactive_audio_sources[i], rc, sync_changed);
	}
}