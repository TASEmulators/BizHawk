#include "bizhawk.hpp"
#include <stdlib.h>
#include <string>
#include <libretro.h>
#include <functional>
#include <jaffarCommon/file.hpp>
#include <SDL.h>
#include <libretro.h>
#include <GPU/GPU.h>

std::string _cdImageFilePath = "__CDROM_PATH.iso";

// Flag to indicate whether the nvram changed
int _nvramChanged;

// Storing inputs for the game to read from
gamePad_t _inputData;

// flag to indicate whether the inputs were polled
bool _readInputs;

// Audio state
#define _MAX_SAMPLES 4096
#define _CHANNEL_COUNT 2
int16_t _audioBuffer[_MAX_SAMPLES * _CHANNEL_COUNT];
size_t _audioSamples;

// Video State
uint32_t* _videoBuffer;
size_t _videoHeight;
size_t _videoWidth;
size_t _videoPitch;
size_t _videoBufferSize = 0;

std::string _compatibilityFileData = "";
std::string _compatibilityVRFileData = "";
std::string _ppgeFontFileData = "";
std::string _ppgeAtlasFontZimFileData = "";
std::string _ppgeAtlasFontMetadataFileData = "";
std::string _atlasFontZimFileData = "";
std::string _atlasFontMetadataFileData = "";

// Current Frame information
MyFrameInfo _f;

extern "C"
{
  void retro_set_audio_sample(retro_audio_sample_t cb);
  void retro_set_audio_sample_batch(retro_audio_sample_batch_t cb);
  void retro_set_environment(retro_environment_t cb);
  void retro_set_input_poll(retro_input_poll_t cb);
  void retro_set_input_state(retro_input_state_t cb);
  void retro_set_log_printf(retro_log_printf_t cb);
  void retro_set_video_refresh(retro_video_refresh_t cb);
  RETRO_API void *retro_get_memory_data(unsigned id);
  RETRO_API size_t retro_get_memory_size(unsigned id);
  void lr_input_device_set(const uint32_t port_, const uint32_t device_);
  RETRO_API size_t retro_serialize_size(void);
  RETRO_API bool retro_serialize(void *data, size_t size);
  RETRO_API bool retro_unserialize(const void *data, size_t size);
  void retro_unload_game(void);
  void retro_deinit(void);
}

void RETRO_CALLCONV retro_video_refresh_callback(const void* data, unsigned width, unsigned height, size_t pitch)
{
	//printf("Video %p, w: %u, h: %u, p: %lu\n", data, width, height, pitch);
	//size_t checksum = 0;
	//for (size_t i = 0; i < height; i++)
	// for (size_t j = 0; i < width; i++)
	//  checksum += ((uint32_t*)data)[i * width + j];
	//printf("Video Checksum: 0x%lX\n", checksum);

  _videoBuffer = (uint32_t*)data;
  _videoWidth = width;
  _videoHeight = height;
  _videoPitch = pitch;
  _videoBufferSize = sizeof(uint32_t) * _videoWidth * _videoHeight;
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
  memcpy(_audioBuffer, data, sizeof(int16_t) * frames * _CHANNEL_COUNT);
  _audioSamples = frames;
  return frames;
}

void RETRO_CALLCONV retro_input_poll_callback()
{
  // printf("Libretro Input Poll Callback Called:\n");
}

int16_t RETRO_CALLCONV retro_input_state_callback(unsigned port, unsigned device, unsigned index, unsigned id)
{
  if (device == RETRO_DEVICE_JOYPAD) switch (id)
  {
    case RETRO_DEVICE_ID_JOYPAD_UP: return _inputData.up ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_DOWN: return _inputData.down ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_LEFT: return _inputData.left ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_RIGHT: return _inputData.right ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_L: return _inputData.ltrigger ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_R: return _inputData.rtrigger ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_SELECT: return _inputData.select ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_START: return _inputData.start ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_X: return _inputData.triangle ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_Y: return _inputData.square ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_B: return _inputData.cross ? 1 : 0;
    case RETRO_DEVICE_ID_JOYPAD_A: return _inputData.circle ? 1 : 0;
    default: return 0;
  }

  if (device == RETRO_DEVICE_ANALOG) switch (id)
  {
      case RETRO_DEVICE_ID_ANALOG_X:
        if (index == RETRO_DEVICE_INDEX_ANALOG_LEFT) return _inputData.leftAnalogX; 
        if (index == RETRO_DEVICE_INDEX_ANALOG_RIGHT) return _inputData.rightAnalogX; 
        return 0;

      case RETRO_DEVICE_ID_ANALOG_Y:
        if (index == RETRO_DEVICE_INDEX_ANALOG_LEFT) return _inputData.leftAnalogY; 
        if (index == RETRO_DEVICE_INDEX_ANALOG_RIGHT) return _inputData.rightAnalogY; 
        return 0;

      default: return 0;
  }

  return 0;
}

char _deviceCountOption[256];
void configHandler(struct retro_variable *var)
{
  var->value = nullptr;
  printf("Variable Name: %s / Value: %s\n", var->key, var->value);
  std::string key(var->key);
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
    if (cmd == RETRO_ENVIRONMENT_SET_PIXEL_FORMAT) { *((retro_pixel_format*) data) = RETRO_PIXEL_FORMAT_XRGB8888; return true; }
    if (cmd == RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY) { return true; }
    if (cmd == RETRO_ENVIRONMENT_GET_SAVE_DIRECTORY) { return true; } 
    if (cmd == RETRO_ENVIRONMENT_GET_CORE_OPTIONS_VERSION) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_VARIABLES) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_CORE_OPTIONS_UPDATE_DISPLAY_CALLBACK) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_INPUT_DESCRIPTORS) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_CONTROLLER_INFO) { return false; }
    if (cmd == RETRO_ENVIRONMENT_GET_INPUT_BITMASKS) { return false; }
    if (cmd == RETRO_ENVIRONMENT_GET_USERNAME) { return false; }
    if (cmd == RETRO_ENVIRONMENT_GET_LANGUAGE) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_CORE_OPTIONS_DISPLAY) { return false; }
    if (cmd == RETRO_ENVIRONMENT_GET_PREFERRED_HW_RENDER) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_HW_RENDER) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SHUTDOWN) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_SYSTEM_AV_INFO) { return false; }
    if (cmd == RETRO_ENVIRONMENT_SET_GEOMETRY) { return false; }
    
    fprintf(stderr, "Unrecognized environment callback command: %u\n", cmd);

    return false;
}

/// CD Management Logic Start
#define CDIMAGE_SECTOR_SIZE 2048
uint32_t _currentSector = 0;
void (*cd_read_callback)(int32_t lba, void * dest);
int (*cd_sector_count_callback)();
EXPORT void SetCdCallbacks(void (*cdrc)(int32_t lba, void * dest), int (*cdscc)())
{
  cd_read_callback = cdrc;
  cd_sector_count_callback = cdscc;
}

uint32_t cd_get_size(void) {  return cd_sector_count_callback() * CDIMAGE_SECTOR_SIZE; }
uint32_t cd_get_sector_count(void) {  return cd_sector_count_callback(); }
void cd_set_sector(const uint32_t sector_) { _currentSector = sector_; }
void cd_read_sector(void *buf_) {  cd_read_callback(_currentSector, buf_); }
size_t readSegmentFromCD(void *buf_, const uint64_t address, const size_t size)
{
  uint64_t initialSector = address / CDIMAGE_SECTOR_SIZE;
  uint64_t sectorCount = size / CDIMAGE_SECTOR_SIZE;
  uint64_t lastSectorBytes = size % CDIMAGE_SECTOR_SIZE;

  uint8_t tmpBuf[CDIMAGE_SECTOR_SIZE];
  for (uint64_t i = 0; i < sectorCount; i++)
  {
    cd_set_sector(initialSector + i);
    cd_read_sector(tmpBuf);
    memcpy(&((uint8_t*)buf_)[CDIMAGE_SECTOR_SIZE * i], tmpBuf, CDIMAGE_SECTOR_SIZE);
  }

  if (lastSectorBytes > 0)
  {
    cd_set_sector(initialSector + sectorCount);
    cd_read_sector(tmpBuf);
    memcpy(&((uint8_t*)buf_)[CDIMAGE_SECTOR_SIZE * sectorCount], tmpBuf, lastSectorBytes);
  }

  return size;
}

/// CD Management Logic End

/// Resource loader
EXPORT bool loadResource(const char* resourceName, uint8_t* buffer, int resourceLen)
{
	std::string name = std::string(resourceName);
	printf("Trying to load resource: %s\n", name.c_str());

	if (name == "compat.ini") {
		printf("Loading resource: %s\n", name.c_str());
		_compatibilityFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}
	
	if (name == "compatvr.ini")
	{
		printf("Loading resource: %s\n", name.c_str());
		_compatibilityVRFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}

  if (name == "PPGeFont.ttf")
	{
		printf("Loading resource: %s\n", name.c_str());
		_ppgeFontFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}

	if (name == "ppge_atlas.zim")
	{
		printf("Loading resource: %s\n", name.c_str());
		_ppgeAtlasFontZimFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}

	if (name == "ppge_atlas.meta")
	{
		printf("Loading resource: %s\n", name.c_str());
		_ppgeAtlasFontMetadataFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}

	if (name == "font_atlas.zim")
	{
		printf("Loading resource: %s\n", name.c_str());
		_atlasFontZimFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}

	if (name == "font_atlas.meta")
	{
		printf("Loading resource: %s\n", name.c_str());
		_atlasFontMetadataFileData = std::string((const char*)buffer, resourceLen);
		return true;
	}

	return false;
}


EXPORT bool Init()
{ 
	retro_set_environment(retro_environment_callback);
	retro_set_input_poll(retro_input_poll_callback);
	retro_set_audio_sample_batch(retro_audio_sample_batch_callback);
	retro_set_video_refresh(retro_video_refresh_callback);
	retro_set_input_state(retro_input_state_callback);

	// Normal way to initialize
	retro_init();
	struct retro_game_info game;
	game.path = _cdImageFilePath.c_str();
	auto loadResult = retro_load_game(&game);
	if (loadResult == false) { fprintf(stderr, "Could not load game"); return false; }

	// Advancing until gpu is initialized -- this is necessary for proper savestates
	while (!gpu) retro_run();

	// Getting av info
	struct retro_system_av_info info;
	retro_get_system_av_info(&info);
	printf("PSP Framerate: %f\n", info.timing.fps);

	return true;
}

EXPORT void GetVideo(uint32_t* videoBuffer)
{
  memcpy(videoBuffer, _videoBuffer, _videoBufferSize);
}

EXPORT void FrameAdvance(MyFrameInfo f)
{
  // Setting Frame information
  _f = f;

  // Setting input data
  _inputData = _f.gamePad;

  //printf("up: %d\n", _inputData.up);
  //printf("down: %d\n", _inputData.down);
  //printf("left: %d\n", _inputData.left);
  //printf("right: %d\n", _inputData.right);
  //printf("ltrigger: %d\n", _inputData.ltrigger);
  //printf("rtrigger: %d\n", _inputData.rtrigger);
  //printf("select: %d\n", _inputData.select);
  //printf("start: %d\n", _inputData.start);
  //printf("triangle: %d\n", _inputData.triangle);
  //printf("square: %d\n", _inputData.square);
  //printf("cross: %d\n", _inputData.cross);
  //printf("circle: %d\n", _inputData.circle);
  //printf("leftAnalogX: %d\n", _inputData.leftAnalogX);
  //printf("rightAnalogX: %d\n", _inputData.rightAnalogX);
  //printf("leftAnalogY: %d\n", _inputData.leftAnalogY);
  //printf("rightAnalogY: %d\n", _inputData.rightAnalogY);

  // Checking for changes in NVRAM
  _nvramChanged = false;

  // Checking if ports have been read
  _readInputs = 0;

  // Jumping into the emu driver coroutine to run a single frame
  retro_run();
}

// EXPORT void GetMemoryAreas(MemoryArea *m)
// {
//   int memAreaIdx = 0;

//   m[memAreaIdx].Data  = retro_get_memory_data(RETRO_MEMORY_SYSTEM_RAM);
//   m[memAreaIdx].Name  = "System RAM";
//   m[memAreaIdx].Size  = retro_get_memory_size(RETRO_MEMORY_SYSTEM_RAM);
//   m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;
//   memAreaIdx++;

//   // m[memAreaIdx].Data  = get_sram_buffer();
//   // m[memAreaIdx].Name  = "Non-volatile RAM";
//   // m[memAreaIdx].Size  = get_sram_size();
//   // m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE; 
//   // memAreaIdx++;
// }

void (*InputCallback)();
EXPORT void SetInputCallback(void (*callback)())
{
  InputCallback = callback;
}

EXPORT int GetStateSize()
{
	return retro_serialize_size();
}

EXPORT void SaveState(uint8_t* buffer)
{
	printf("Saving State\n");
	bool result = retro_serialize(buffer, retro_serialize_size());
	if (result == false) printf("Save state failed\n");
}

EXPORT void LoadState(uint8_t* buffer, int size)
{
	printf("Loading State\n");
	bool result = retro_unserialize(buffer, size);
	if (result == false) printf("Load state failed\n");
}

EXPORT void Deinit()
{
	retro_unload_game();
	retro_deinit();
}