#ifndef RENDER_AUDIO_H_
#define RENDER_AUDIO_H_

#include <stdint.h>
typedef enum {
	RENDER_AUDIO_S16,
	RENDER_AUDIO_FLOAT,
	RENDER_AUDIO_UNKNOWN
} render_audio_format;

typedef struct {
	void     *opaque;
	int16_t  *front;
	int16_t  *back;
	double   dt;
	uint64_t buffer_fraction;
	uint64_t buffer_inc;
	float    gain_mult;
	uint32_t buffer_pos;
	uint32_t read_start;
	uint32_t read_end;
	uint32_t lowpass_alpha;
	uint32_t mask;
	int16_t  last_left;
	int16_t  last_right;
	uint8_t  num_channels;
	uint8_t  front_populated;
} audio_source;

//public interface
audio_source *render_audio_source(uint64_t master_clock, uint64_t sample_divider, uint8_t channels);
void render_audio_source_gaindb(audio_source *src, float gain);
void render_audio_adjust_clock(audio_source *src, uint64_t master_clock, uint64_t sample_divider);
void render_put_mono_sample(audio_source *src, int16_t value);
void render_put_stereo_sample(audio_source *src, int16_t left, int16_t right);
void render_pause_source(audio_source *src);
void render_resume_source(audio_source *src);
void render_free_source(audio_source *src);
//interface for render backends
void render_audio_initialized(render_audio_format format, uint32_t rate, uint8_t channels, uint32_t buffer_size, int sample_size);
int mix_and_convert(unsigned char *byte_stream, int len, int *min_remaining_out);
uint8_t all_sources_ready(void);
void render_audio_adjust_speed(float adjust_ratio);
//to be implemented by render backend
uint8_t render_is_audio_sync(void);
void render_buffer_consumed(audio_source *src);
void *render_new_audio_opaque(void);
void render_free_audio_opaque(void *opaque);
void render_lock_audio(void);
void render_unlock_audio(void);
uint32_t render_min_buffered(void);
uint32_t render_audio_syncs_per_sec(void);
void render_audio_created(audio_source *src);
void render_do_audio_ready(audio_source *src);
void render_source_paused(audio_source *src, uint8_t remaining_sources);
void render_source_resumed(audio_source *src);
#endif //RENDER_AUDIO_H_
