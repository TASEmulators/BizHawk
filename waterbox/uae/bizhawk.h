#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

// waterbox
#include "emulibc.h"
#include "waterboxcore.h"

// core
#include "include/sysdeps.h"
#include "include/options.h"
#include "include/uae.h"
#include "include/memory.h"
#include "include/custom.h"
#include "include/drawing.h"
#include "include/inputdevice.h"

#include "libretro/libretro-core.h"

static const int FILENAME_MAXLENGTH = 4;
static const int KEY_COUNT = 0x68;

int16_t* sound_buffer = NULL;
int sound_sample_count = 0;
static char last_key_state[KEY_COUNT];
static int last_mouse_x;
static int last_mouse_y;

extern int thisframe_y_adjust;
extern unsigned short int defaultw;
extern unsigned short int defaulth;
extern int retro_max_diwlastword;

extern int umain(int argc, char **argv);
extern int m68k_go(int may_quit, int resume);
extern void init_output_audio_buffer(int32_t capacity);
extern void upload_output_audio_buffer();
extern void disk_eject(int num);
extern void disk_insert_force (int num, const TCHAR *name, bool forcedwriteprotect);

enum Axis
{
	AXIS_HORIZONTAL,
	AXIS_VERTICAL
};

enum JoystickRange
{
	JOY_MIN = -1,
	JOY_MID,
	JOY_MAX
};

enum ControllerPort
{
	PORT_0,
	PORT_1
};

enum MouseButtons
{
	MOUSE_LEFT,
	MOUSE_RIGHT,
	MOUSE_MIDDLE
};

enum MousePosition
{
	MOUSE_RELATIVE,
	MOUSE_ABSOLUTE
};

enum AudioChannels
{
	AUDIO_MONO = 1,
	AUDIO_STEREO
};

enum DriveAction
{
	ACTION_NONE,
	ACTION_EJECT,
	ACTION_INSERT
};

typedef union
{
    struct
    {
        bool up:1;
        bool down:1;
        bool left:1;
        bool right:1;
        bool b1:1;
        bool b2:1;
        bool b3:1;
    };
    uint8_t data;
} PUAEJoystick;

typedef struct
{
	FrameInfo base;
	PUAEJoystick JoystickState;
	uint8_t MouseButtons;
	int MouseX;
	int MouseY;
	char Keys[KEY_COUNT];
	int CurrentDrive;
	int Action;
	char FileName[FILENAME_MAXLENGTH];
} MyFrameInfo;

size_t biz_audio_cb(const int16_t *data, size_t frames)
{
	sound_sample_count = frames;
	memcpy(sound_buffer, data, frames * sizeof(int16_t) * AUDIO_STEREO);
}

void biz_log_cb(enum retro_log_level level, const char *fmt, ...)
{
	fprintf(stderr, "[PUAE ");
	switch (level)
	{
		case RETRO_LOG_DEBUG:
			fprintf(stderr, "DEBUG]: ");
			break;
		case RETRO_LOG_INFO:
			fprintf(stderr, "INFO]: ");
			break;
		case RETRO_LOG_WARN:
			fprintf(stderr, "WARN]: ");
			break;
		case RETRO_LOG_ERROR:
			fprintf(stderr, "ERROR]: ");
			break;
	}
	va_list va;
	va_start(va, fmt);
	vfprintf(stderr, fmt, va);
	va_end(va);
}