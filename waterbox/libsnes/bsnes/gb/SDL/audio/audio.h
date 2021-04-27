#ifndef sdl_audio_h
#define sdl_audio_h

#include <stdbool.h>
#include <stddef.h>
#include <Core/gb.h>

bool GB_audio_is_playing(void);
void GB_audio_set_paused(bool paused);
void GB_audio_clear_queue(void);
unsigned GB_audio_get_frequency(void);
size_t GB_audio_get_queue_length(void);
void GB_audio_queue_sample(GB_sample_t *sample);
void GB_audio_init(void);

#endif /* sdl_audio_h */
