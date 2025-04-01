#include "bizhawk.hpp"
#include <stdlib.h>
#include <string>
#include <libretro.h>
#include <functional>
#include <jaffarCommon/dethreader.hpp>
#include <jaffarCommon/file.hpp>

// State for the dethreader runtime
__JAFFAR_COMMON_DETHREADER_STATE

// Memory file directory
jaffarCommon::file::MemoryFileDirectory _memFileDirectory;

// Flag to indicate the game was correctly loaded
bool _loadResult;

// Flag to indicate whether the nvram changed
int _nvramChanged;

// Storing inputs for the game to read from
gamePad_t _inputData;

// flag to indicate whether the inputs were polled
int _inputPortsRead; 

// Coroutines: they allow us to jump in and out the emu driver
cothread_t _emuDriverCoroutine;
cothread_t _driverCoroutine;

// This flag inficates whether us jumping into the emu driver corresponds to an advance state request
bool _advanceState = true;

// Where the rom is stored -- will be removed when using bk's CD interface
std::string _romFilePath;

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
}

// Emulation driver - runs in its own coroutine and conducts execution through the dethreader runtime system
void emuDriver()
{
  // Creating dethreader manager
  jaffarCommon::dethreader::Runtime r;

  // Creating init task
  auto initTask = []()
  {
    retro_init();

    struct retro_game_info game;
    game.path = _romFilePath.c_str();
    _loadResult = retro_load_game(&game);

    printf("Exiting from init task\n"); fflush(stdout);
    _emuDriverCoroutine = co_active();
    co_switch(_driverCoroutine);  
    retro_run();
    printf("back to init task\n"); fflush(stdout);
  };

  // Creating state advance task
  auto advanceTask = []()
  {
    printf("Advance Task before while true...\n"); fflush(stdout);
    while(true)
    {
      // If it's time to do it, run state
      if (_advanceState == true)
      {
        printf("Before Retro Run...\n"); fflush(stdout);
        retro_run();
        _advanceState = false;

        // Come back to driver scope
        _emuDriverCoroutine = co_active();
        co_switch(_driverCoroutine);
      } 

      // Yield execution
      jaffarCommon::dethreader::yield();
    }
  };

  // Addinng tasks
  r.createThread(initTask);
  r.createThread(advanceTask);

  // Running dethreader runtime
  r.initialize();
  r.run();
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
    
    fprintf(stderr, "Unrecognized environment callback command: %u\n", cmd);

    return false;
}

/// CD Management Logic Start
#define CDIMAGE_SECTOR_SIZE 2048
uint32_t _currentSector = 0;
void (*cd_read_callback)(int32_t lba, void * dest);
int (*cd_sector_count_callback)();
ECL_EXPORT void SetCdCallbacks(void (*cdrc)(int32_t lba, void * dest), int (*cdscc)())
{
  cd_read_callback = cdrc;
  cd_sector_count_callback = cdscc;
}

uint32_t cd_get_size(void) {  return cd_sector_count_callback(); }
void cd_set_sector(const uint32_t sector_) { _currentSector = sector_; }
void cd_read_sector(void *buf_) {  cd_read_callback(_currentSector, buf_); }
/// CD Management Logic End

ECL_EXPORT bool Init()
{ 
  retro_set_environment(retro_environment_callback);
  retro_set_input_poll(retro_input_poll_callback);
  retro_set_audio_sample_batch(retro_audio_sample_batch_callback);
  retro_set_video_refresh(retro_video_refresh_callback);
  retro_set_input_state(retro_input_state_callback);

  printf("Starting Emu Driver Coroutine...\n");
  _driverCoroutine = co_active();
  constexpr size_t stackSize = 4 * 1024 * 1024;
  _emuDriverCoroutine = co_create(stackSize, emuDriver);

  // Loading CD into memfile
  const auto cdSectorCount = cd_sector_count_callback();
  printf("Loading CD %u with %u sectors...\n", 0, cdSectorCount);

  // Uploading CD as a mem file
  _romFilePath = "CDROM0.bin";
	auto f = _memFileDirectory.fopen(_romFilePath, "w");
	if (f == NULL) { fprintf(stderr, "Could not open mem file for write: %s\n", _romFilePath.c_str()); return false; }

  // Writing contents into mem file, one by one
  for (size_t i = 0; i < cdSectorCount; i++)
  {
    uint8_t sector[CDIMAGE_SECTOR_SIZE];
    cd_set_sector(i);
    cd_read_sector(sector);
    auto writtenBlocks = jaffarCommon::file::MemoryFile::fwrite(sector, CDIMAGE_SECTOR_SIZE, 1, f);
    if (writtenBlocks != 1) 
    { 
      fprintf(stderr, "Could not write data into mem file: %s\n", _romFilePath.c_str());
      _memFileDirectory.fclose(f);
      return false; 
    }
  }

  // Closing file
  _memFileDirectory.fclose(f);
  
  // Initializing emu core
  co_switch(_emuDriverCoroutine);
  if (_loadResult == false) { fprintf(stderr, "Could not load game: '%s'\n", _romFilePath.c_str()); return false; }

  // Setting cd callbacks
  // libretro_cdrom_set_callbacks(cd_get_size, cd_set_sector, cd_read_sector);

  // Getting av info
  struct retro_system_av_info info;
  retro_get_system_av_info(&info);
  printf("PSP Framerate: %f\n", info.timing.fps);

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
  _inputData = f->gamePad;

  // Checking for changes in NVRAM
  _nvramChanged = false;

  // Checking if ports have been read
  _inputPortsRead = 0;

  // Jumping into the emu driver coroutine to run a single frame
  _advanceState = true;
  printf("Advancing Frame...\n"); fflush(stdout);
  co_switch(_emuDriverCoroutine);

  // The frame is lagged if no inputs were read
  f->base.Lagged = !_inputPortsRead;

  // Setting video buffer
  f->base.Width = _videoWidth;
  f->base.Height = _videoHeight;
  memcpy(f->base.VideoBuffer, _videoBuffer, sizeof(uint32_t) * _videoWidth * _videoHeight);

  // Setting audio buffer
  f->base.Samples = _audioSamples;
  memcpy(f->base.SoundBuffer, _audioBuffer, _audioSamples * sizeof(int16_t) * _CHANNEL_COUNT);
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
  int memAreaIdx = 0;

  m[memAreaIdx].Data  = retro_get_memory_data(RETRO_MEMORY_SYSTEM_RAM);
  m[memAreaIdx].Name  = "System RAM";
  m[memAreaIdx].Size  = retro_get_memory_size(RETRO_MEMORY_SYSTEM_RAM);
  m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;
  memAreaIdx++;

  // m[memAreaIdx].Data  = get_sram_buffer();
  // m[memAreaIdx].Name  = "Non-volatile RAM";
  // m[memAreaIdx].Size  = get_sram_size();
  // m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE; 
  // memAreaIdx++;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
  InputCallback = callback;
}
