#ifndef CALLBACKS_H
#define CALLBACKS_H

#include <stdint.h>

typedef void (*snes_input_poll_t)(void);
typedef int16_t (*snes_input_state_t)(int port, int device, int index, int id);
typedef void (*snes_no_lag_t)(void);
typedef void (*snes_video_frame_t)(int32_t* data, int width, int height);
typedef void (*snes_audio_sample_t)(uint16_t left, uint16_t right);
typedef char* (*snes_path_request_t)(int slot, const char* hint);
// typedef void (*snes_trace_t)(int which, char* message);

extern snes_input_poll_t snes_input_poll;
extern snes_input_state_t snes_input_state;
extern snes_no_lag_t snes_no_lag;
extern snes_video_frame_t snes_video_frame;
extern snes_audio_sample_t snes_audio_sample;
extern snes_path_request_t snes_path_request;
// extern snes_trace_t snes_trace;


#endif
