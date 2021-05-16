#ifndef CALLBACKS_H
#define CALLBACKS_H

#include <stdint.h>

typedef void (*snes_input_poll_t)(void);
typedef int16_t (*snes_input_state_t)(int port, int index, int id);
typedef void (*snes_no_lag_t)(void);
typedef void (*snes_video_frame_t)(const uint16_t* data, int width, int height, int pitch);
typedef char* (*snes_path_request_t)(int slot, const char* hint, int required);
typedef void (*snes_trace_t)(const char* disassembly, const char* register_info);

struct SnesCallbacks {
    snes_input_state_t snes_input_state;
    snes_no_lag_t snes_no_lag;
    snes_video_frame_t snes_video_frame;
    snes_path_request_t snes_path_request;
    snes_trace_t snes_trace;
};

extern SnesCallbacks snesCallbacks;

struct SnesFrameAdvanceInfo {
	short* audio;
	bool renderAudio;
	bool renderVideo;
};

extern short* ExternalAudioBuffer;

#endif
