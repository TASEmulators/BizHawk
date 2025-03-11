#include "bizhawk.hpp"
#include <stdlib.h>
#include <string>
#include <libretro.h>
#include <opera_vdlp.h>

struct MemoryAreas 
{
	uint8_t* wram;
  uint8_t* vram;
};

struct MemorySizes
{
	size_t wram;
  size_t vram;
};

#define _MAX_AUDIO_SAMPLE_COUNT 4096

std::string _biosFilePath;
std::string _gameFilePath;
std::string _fontFilePath;
int _port1Type;
int _port2Type;
controllerData_t _port1Value;
controllerData_t _port2Value;
MemoryAreas _memoryAreas;
MemorySizes _memorySizes;
uint32_t* _videoBuffer;
size_t _videoHeight;
size_t _videoWidth;
size_t _videoPitch;

int16_t* _audioBuffer;
size_t _audioSamples;

extern "C"
{
    void opera_lr_callbacks_set_audio_sample(retro_audio_sample_t cb);
    void opera_lr_callbacks_set_audio_sample_batch(retro_audio_sample_batch_t cb);
    void opera_lr_callbacks_set_environment(retro_environment_t cb);
    void opera_lr_callbacks_set_input_poll(retro_input_poll_t cb);
    void opera_lr_callbacks_set_input_state(retro_input_state_t cb);
    void opera_lr_callbacks_set_log_printf(retro_log_printf_t cb);
    void opera_lr_callbacks_set_video_refresh(retro_video_refresh_t cb);
    RETRO_API void *retro_get_memory_data(unsigned id);
    RETRO_API size_t retro_get_memory_size(unsigned id);
				void retro_set_controller_port_device(unsigned port_, unsigned device_);
				void retro_get_system_av_info(struct retro_system_av_info *info_);
}

void RETRO_CALLCONV retro_video_refresh_callback(const void *data, unsigned width, unsigned height, size_t pitch)
{
		// printf("Video %p, w: %u, h: %u, p: %lu\n", data, width, height, pitch);
		_videoBuffer = (uint32_t*)data;
		_videoWidth = width;
		_videoHeight = height;
		_videoPitch = pitch;
}

void RETRO_CALLCONV retro_log_printf_callback(enum retro_log_level level, const char *format, ...)
{
		va_list ap;
		va_start(ap, format);
		printf(format, ap);
		va_end(ap);
}

size_t RETRO_CALLCONV retro_audio_sample_batch_callback(const int16_t *data, size_t frames)
{
	 _audioBuffer = (int16_t*)data;
 	_audioSamples = frames >> 1;
	 return frames;
}

void RETRO_CALLCONV retro_input_poll_callback()
{
		// printf("Libretro Input Poll Callback Called:\n");
}

int16_t processController(controllerData_t& portValue, unsigned device, unsigned index, unsigned id)
{
	if (device == RETRO_DEVICE_JOYPAD)
	{
		 if (id == RETRO_DEVICE_ID_JOYPAD_UP) return portValue.gamePad.up;
		 if (id == RETRO_DEVICE_ID_JOYPAD_DOWN) return portValue.gamePad.down;
		 if (id == RETRO_DEVICE_ID_JOYPAD_LEFT) return portValue.gamePad.left;
		 if (id == RETRO_DEVICE_ID_JOYPAD_RIGHT) return portValue.gamePad.right;
		 if (id == RETRO_DEVICE_ID_JOYPAD_L) return portValue.gamePad.buttonL;
		 if (id == RETRO_DEVICE_ID_JOYPAD_R) return portValue.gamePad.buttonR;
		 if (id == RETRO_DEVICE_ID_JOYPAD_SELECT) return portValue.gamePad.select;
		 if (id == RETRO_DEVICE_ID_JOYPAD_START) return portValue.gamePad.start;
		 if (id == RETRO_DEVICE_ID_JOYPAD_X) return portValue.gamePad.buttonX;
		 if (id == RETRO_DEVICE_ID_JOYPAD_Y) return portValue.gamePad.buttonY;
		 if (id == RETRO_DEVICE_ID_JOYPAD_B) return portValue.gamePad.buttonB;
		 if (id == RETRO_DEVICE_ID_JOYPAD_A) return portValue.gamePad.buttonA;
	}

	if (device == RETRO_DEVICE_MOUSE)
	{
		if (id == RETRO_DEVICE_ID_MOUSE_X) return portValue.mouse.dX;
		if (id == RETRO_DEVICE_ID_MOUSE_Y) return portValue.mouse.dY;
		if (id == RETRO_DEVICE_ID_MOUSE_LEFT) return portValue.mouse.leftButton;
		if (id == RETRO_DEVICE_ID_MOUSE_MIDDLE) return portValue.mouse.middleButton;
		if (id == RETRO_DEVICE_ID_MOUSE_RIGHT) return portValue.mouse.rightButton;
		if (id == RETRO_DEVICE_ID_MOUSE_BUTTON_4) return portValue.mouse.fourthButton;
	}

	return 0;
}

int16_t RETRO_CALLCONV retro_input_state_callback(unsigned port, unsigned device, unsigned index, unsigned id)
{
	// printf("Libretro Input State Callback Called. Port: %u, Device: %u, Index: %u, Id: %u\n", port, device, index, id);
	if (port == 0) return processController(_port1Value, device, index, id);
	if (port == 1) return processController(_port2Value, device, index, id);

	return 0;
}

void configHandler(struct retro_variable *var)
{
		printf("Variable Name: %s / Value: %s\n", var->key, var->value);

		std::string key(var->key);
		if (key == "opera_bios") var->value = _biosFilePath.c_str();
		if (key == "opera_active_devices") var->value = "1";
}

const char* systemPath = ".";
bool RETRO_CALLCONV retro_environment_callback(unsigned cmd, void *data)
{
		// printf("Libretro Environment Callback Called: %u\n", cmd);

		if (cmd == RETRO_ENVIRONMENT_GET_LOG_INTERFACE) { *((retro_log_printf_t*)data) = retro_log_printf_callback; return true; }
		if (cmd == RETRO_ENVIRONMENT_SET_PERFORMANCE_LEVEL) { return true; }
		if (cmd == RETRO_ENVIRONMENT_SET_SERIALIZATION_QUIRKS) { return true; }
		if (cmd == RETRO_ENVIRONMENT_GET_VARIABLE) { configHandler((struct retro_variable *)data); return true; }
		if (cmd == RETRO_ENVIRONMENT_GET_VARIABLE_UPDATE) { return true; }
		if (cmd == RETRO_ENVIRONMENT_SET_PIXEL_FORMAT) { *((vdlp_pixel_format_e*) data) = VDLP_PIXEL_FORMAT_XRGB8888; return true; }
		if (cmd == RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY) { *((const char**)data) = systemPath; return true; }
		if (cmd == RETRO_ENVIRONMENT_GET_SAVE_DIRECTORY) { *((const char**)data) = systemPath; return true; }
		
		fprintf(stderr, "Unrecognized environment callback command: %u\n", cmd);

		return false;
}


ECL_EXPORT bool Init(const char* gameFilePath, const char* biosFilePath, const char* fontFilePath, int port1Type, int port2Type)
{ 
	_gameFilePath = gameFilePath;
	_biosFilePath = biosFilePath;
	_fontFilePath = fontFilePath;
	_port1Type = port1Type;
	_port2Type = port2Type;

	opera_lr_callbacks_set_environment(retro_environment_callback);
	opera_lr_callbacks_set_input_state(retro_input_state_callback);
	opera_lr_callbacks_set_input_poll(retro_input_poll_callback);
	opera_lr_callbacks_set_audio_sample_batch(retro_audio_sample_batch_callback);
	opera_lr_callbacks_set_video_refresh(retro_video_refresh_callback);

	retro_set_controller_port_device(0, port1Type);
	retro_set_controller_port_device(1, port2Type);
	retro_init();

	// Loading game file
	struct retro_game_info game;
	game.path = _gameFilePath.c_str();
	auto loadResult = retro_load_game(&game);
	if (loadResult == false) { fprintf(stderr, "Could not load game: '%s'\n", _gameFilePath.c_str()); return false; }

	//// Getting memory areas
	/** 2 = wram, 3 = vram*/

	// WRAM
	_memoryAreas.wram = (uint8_t*)retro_get_memory_data(RETRO_MEMORY_SYSTEM_RAM);
	_memorySizes.wram = retro_get_memory_size(RETRO_MEMORY_SYSTEM_RAM);

	// VRAM
	_memoryAreas.vram = (uint8_t*)retro_get_memory_data(RETRO_MEMORY_VIDEO_RAM);
	_memorySizes.vram = retro_get_memory_size(RETRO_MEMORY_VIDEO_RAM);

	// Getting av info
	struct retro_system_av_info info;
	retro_get_system_av_info(&info);
	printf("3DO Framerate: %f\n", info.timing.fps);

	return true;
}

ECL_EXPORT void opera_get_video(int& w, int& h, int& pitch, uint8_t*& buffer)
{
	buffer = (uint8_t*)_videoBuffer;
	w = _videoWidth;
	h = _videoHeight;
	pitch = _videoPitch;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	 // Setting inputs
		_port1Value = f->port1;
		_port2Value = f->port2;

  //printf("Mouse X%d(%d), Y%d(%d), L%d, M%d, B%d\n", _port1Value.mouse.posX, _port1Value.mouse.dX, _port1Value.mouse.posY, _port1Value.mouse.dY, _port1Value.mouse.leftButton, _port1Value.mouse.middleButton, _port1Value.mouse.rightButton);
		//fflush(stdout);

		// Running a single frame
		retro_run();

		// Setting video buffer
		f->base.Width = _videoWidth;
		f->base.Height = _videoHeight;
		memcpy(f->base.VideoBuffer, _videoBuffer, sizeof(uint32_t) * _videoWidth * _videoHeight);

		// Setting audio buffer
		f->base.Samples  = _audioSamples;
		memcpy(f->base.SoundBuffer, _audioBuffer, _audioSamples * sizeof(int16_t));
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int memAreaIdx = 0;

	m[memAreaIdx].Data  = _memoryAreas.wram;
	m[memAreaIdx].Name  = "Work RAM";
	m[memAreaIdx].Size  = _memorySizes.wram;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;
	memAreaIdx++;

	m[memAreaIdx].Data  = _memoryAreas.vram;
	m[memAreaIdx].Name  = "Video RAM";
	m[memAreaIdx].Size  = _memorySizes.vram;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
	memAreaIdx++;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
