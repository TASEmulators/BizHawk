#include "bizhawk.hpp"
#include <stdlib.h>
#include <string>
#include <libretro.h>
#include <lr_input.h>
#include <opera_vdlp.h>
#include <opera_mem.h>
#include <opera_cdrom.h>
#include <opera_xbus.h>

std::string _biosFilePath;
std::string _gameFilePath;
std::string _fontFilePath;
int _port1Type;
int _port2Type;
controllerData_t _port1Value;
controllerData_t _port2Value;
uint32_t* _videoBuffer;
size_t _videoHeight;
size_t _videoWidth;
size_t _videoPitch;
int _region;
int _nvramChanged;
int _inputPortsRead;

#define _MAX_SAMPLES 4096
#define _CHANNEL_COUNT 2
int16_t _audioBuffer[_MAX_SAMPLES * _CHANNEL_COUNT];
size_t _audioSamples;

extern "C"
{  
  void* xbus_cdrom_plugin(int   proc_,   void* data_);
  void opera_cdrom_set_callbacks(opera_cdrom_get_size_cb_t get_size_,  opera_cdrom_set_sector_cb_t set_sector_,  opera_cdrom_read_sector_cb_t read_sector_);
  void opera_nvram_init(void *buf, const int bufsize);
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
  memcpy(_audioBuffer, data, sizeof(int16_t) * frames * _CHANNEL_COUNT);
  _audioSamples = frames;
  return frames;
}

void RETRO_CALLCONV retro_input_poll_callback()
{
  // printf("Libretro Input Poll Callback Called:\n");
}

int16_t processController(const int portType, controllerData_t& portValue, const unsigned device, const unsigned index, const unsigned id)
{
  switch (portType)
  {
   case RETRO_DEVICE_JOYPAD:
    switch (id)
    {
      case RETRO_DEVICE_ID_JOYPAD_UP: return portValue.gamePad.up;
      case RETRO_DEVICE_ID_JOYPAD_DOWN: return portValue.gamePad.down;
      case RETRO_DEVICE_ID_JOYPAD_LEFT: return portValue.gamePad.left;
      case RETRO_DEVICE_ID_JOYPAD_RIGHT: return portValue.gamePad.right;
      case RETRO_DEVICE_ID_JOYPAD_L: return portValue.gamePad.buttonL;
      case RETRO_DEVICE_ID_JOYPAD_R: return portValue.gamePad.buttonR;
      case RETRO_DEVICE_ID_JOYPAD_SELECT: return portValue.gamePad.buttonX;
      case RETRO_DEVICE_ID_JOYPAD_START: return portValue.gamePad.buttonP;
      case RETRO_DEVICE_ID_JOYPAD_Y: return portValue.gamePad.buttonA;
      case RETRO_DEVICE_ID_JOYPAD_B: return portValue.gamePad.buttonB;
      case RETRO_DEVICE_ID_JOYPAD_A: return portValue.gamePad.buttonC;
      default: return 0;
    }

    case RETRO_DEVICE_MOUSE:
    switch (id)
    {
      case RETRO_DEVICE_ID_MOUSE_X: return portValue.mouse.dX;
      case RETRO_DEVICE_ID_MOUSE_Y: return portValue.mouse.dY;
      case RETRO_DEVICE_ID_MOUSE_LEFT: return portValue.mouse.leftButton;
      case RETRO_DEVICE_ID_MOUSE_MIDDLE: return portValue.mouse.middleButton;
      case RETRO_DEVICE_ID_MOUSE_RIGHT: return portValue.mouse.rightButton;
      case RETRO_DEVICE_ID_MOUSE_BUTTON_4: return portValue.mouse.fourthButton;
      default: return 0;
    }

    case RETRO_DEVICE_FLIGHTSTICK:
    if (index == RETRO_DEVICE_INDEX_ANALOG_BUTTON)
    {
      switch (id)
      {
        case RETRO_DEVICE_ID_JOYPAD_R2: return portValue.flightStick.fire;
        case RETRO_DEVICE_ID_JOYPAD_Y: return portValue.flightStick.buttonA;
        case RETRO_DEVICE_ID_JOYPAD_B: return portValue.flightStick.buttonB;
        case RETRO_DEVICE_ID_JOYPAD_A: return portValue.flightStick.buttonC;   
        case RETRO_DEVICE_ID_JOYPAD_UP: return portValue.flightStick.up;   
        case RETRO_DEVICE_ID_JOYPAD_DOWN: return portValue.flightStick.down;   
        case RETRO_DEVICE_ID_JOYPAD_LEFT: return portValue.flightStick.left;   
        case RETRO_DEVICE_ID_JOYPAD_RIGHT: return portValue.flightStick.right;
        case RETRO_DEVICE_ID_JOYPAD_START: return portValue.flightStick.buttonP;
        case RETRO_DEVICE_ID_JOYPAD_SELECT: return portValue.flightStick.buttonX; 
        case RETRO_DEVICE_ID_JOYPAD_L: return portValue.flightStick.leftTrigger;
        case RETRO_DEVICE_ID_JOYPAD_R: return portValue.flightStick.rightTrigger;
        default: return 0;
      }
    }
    else
    {
      switch (id)
      {
        case RETRO_DEVICE_ID_ANALOG_X:
          if (index == RETRO_DEVICE_INDEX_ANALOG_LEFT) return portValue.flightStick.horizontalAxis; 
          if (index == RETRO_DEVICE_INDEX_ANALOG_RIGHT) return portValue.flightStick.altitudeAxis;
          return 0;
  
        case RETRO_DEVICE_ID_ANALOG_Y:
          if (index == RETRO_DEVICE_INDEX_ANALOG_LEFT) return portValue.flightStick.verticalAxis;
          if (index == RETRO_DEVICE_INDEX_ANALOG_RIGHT) return portValue.flightStick.altitudeAxis;
          return 0;
        default: return 0;
      }
    }
    
    case RETRO_DEVICE_LIGHTGUN:
      switch (id)
      {
        case RETRO_DEVICE_ID_LIGHTGUN_SCREEN_X: return portValue.lightGun.screenX;
        case RETRO_DEVICE_ID_LIGHTGUN_SCREEN_Y: return portValue.lightGun.screenY;
        case RETRO_DEVICE_ID_LIGHTGUN_TRIGGER: return portValue.lightGun.trigger;
        case RETRO_DEVICE_ID_LIGHTGUN_SELECT: return portValue.lightGun.select;
        case RETRO_DEVICE_ID_LIGHTGUN_RELOAD: return portValue.lightGun.reload;
        case RETRO_DEVICE_ID_LIGHTGUN_IS_OFFSCREEN: return portValue.lightGun.isOffScreen;
        default: return 0;
      }

    case RETRO_DEVICE_ARCADE_LIGHTGUN:
      switch (id)
      {
        case RETRO_DEVICE_ID_LIGHTGUN_SCREEN_X:   return portValue.arcadeLightGun.screenX;
        case RETRO_DEVICE_ID_LIGHTGUN_SCREEN_Y:   return portValue.arcadeLightGun.screenY;
        case RETRO_DEVICE_ID_LIGHTGUN_TRIGGER:    return portValue.arcadeLightGun.trigger;
        case RETRO_DEVICE_ID_LIGHTGUN_SELECT:     return portValue.arcadeLightGun.select;
        case RETRO_DEVICE_ID_LIGHTGUN_START:    return portValue.arcadeLightGun.start;
        case RETRO_DEVICE_ID_LIGHTGUN_RELOAD:     return portValue.arcadeLightGun.reload;
        case RETRO_DEVICE_ID_LIGHTGUN_AUX_A:    return portValue.arcadeLightGun.auxA;
        case RETRO_DEVICE_ID_LIGHTGUN_IS_OFFSCREEN: return portValue.arcadeLightGun.isOffScreen;
        default: return 0;
      }

    case RETRO_DEVICE_ORBATAK_TRACKBALL:
      switch (id)
      {
        case RETRO_DEVICE_ID_ANALOG_X:
          if (index == RETRO_DEVICE_INDEX_ANALOG_LEFT) return portValue.orbatakTrackball.dX; 
          if (index == RETRO_DEVICE_INDEX_ANALOG_RIGHT) return portValue.orbatakTrackball.dX; 
          return 0;
    
        case RETRO_DEVICE_ID_ANALOG_Y:
          if (index == RETRO_DEVICE_INDEX_ANALOG_LEFT) return portValue.orbatakTrackball.dY; 
          if (index == RETRO_DEVICE_INDEX_ANALOG_RIGHT) return portValue.orbatakTrackball.dY; 
          return 0;
  
        case RETRO_DEVICE_ID_JOYPAD_SELECT: return portValue.orbatakTrackball.startP1;
        case RETRO_DEVICE_ID_JOYPAD_START: return portValue.orbatakTrackball.startP2;
        case RETRO_DEVICE_ID_JOYPAD_L: return portValue.orbatakTrackball.coinP1;
        case RETRO_DEVICE_ID_JOYPAD_R: return portValue.orbatakTrackball.coinP2;
        case RETRO_DEVICE_ID_JOYPAD_R2: return portValue.orbatakTrackball.service;
        default: return 0;
      }

    default: return 0;
  }

  return 0;
}

int16_t RETRO_CALLCONV retro_input_state_callback(unsigned port, unsigned device, unsigned index, unsigned id)
{
  // printf("Libretro Input State Callback Called. Port: %u, Device: %u, Index: %u, Id: %u\n", port, device, index, id);
  if (port == 0) return processController(_port1Type, _port1Value, device, index, id);
  if (port == 1) return processController(_port2Type, _port2Value, device, index, id);

  return 0;
}

char _deviceCountOption[256];
void configHandler(struct retro_variable *var)
{
  printf("Variable Name: %s / Value: %s\n", var->key, var->value);

  std::string key(var->key);

  if (key == "opera_bios" && _biosFilePath != "None") var->value = _biosFilePath.c_str();
  if (key == "opera_font" && _fontFilePath != "None") var->value = _fontFilePath.c_str();
  if (key == "opera_region")
  {
   if (_region == 0) var->value = "ntsc";
   if (_region == 1) var->value = "pal1";
   if (_region == 2) var->value = "pal2";
  } 
  if (key == "opera_active_devices") 
  {
    int deviceCount = 0;
    if (_port1Type != RETRO_DEVICE_NONE) deviceCount++;
    if (_port2Type != RETRO_DEVICE_NONE) deviceCount++;
    sprintf(_deviceCountOption, "%d", deviceCount);
    var->value = _deviceCountOption;
  }
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

// SRAM management start
bool _sram_changed = false;
ECL_EXPORT bool sram_changed() { return _nvramChanged; }
ECL_EXPORT int get_sram_size() { return NVRAM_SIZE; }
ECL_EXPORT uint8_t* get_sram_buffer() { return (uint8_t*) NVRAM; }
ECL_EXPORT void get_sram(uint8_t* sramBuffer)
{
  if (NVRAM == NULL) return;
  memcpy(sramBuffer, get_sram_buffer(), get_sram_size());
}

ECL_EXPORT void set_sram(uint8_t* sramBuffer)
{
  if (NVRAM == NULL) opera_nvram_init(NVRAM,NVRAM_SIZE);
  memcpy(get_sram_buffer(), sramBuffer, get_sram_size());
}
// SRAM Management end

ECL_EXPORT bool Init(const char* gameFilePath, const char* biosFilePath, const char* fontFilePath, int port1Type, int port2Type, int region)
{ 
  _gameFilePath = gameFilePath;
  _biosFilePath = biosFilePath;
  _fontFilePath = fontFilePath;
  _port1Type = port1Type;
  _port2Type = port2Type;
  _region = region;

  opera_lr_callbacks_set_environment(retro_environment_callback);
  opera_lr_callbacks_set_input_state(retro_input_state_callback);
  opera_lr_callbacks_set_input_poll(retro_input_poll_callback);
  opera_lr_callbacks_set_audio_sample_batch(retro_audio_sample_batch_callback);
  opera_lr_callbacks_set_video_refresh(retro_video_refresh_callback);

  retro_set_controller_port_device(0, port1Type);
  retro_set_controller_port_device(1, port2Type);
  retro_init();

  // Setting cd callbacks
  opera_cdrom_set_callbacks(cd_get_size, cd_set_sector, cd_read_sector);

  // Loading game file
  struct retro_game_info game;
  game.path = _gameFilePath.c_str();
  auto loadResult = retro_load_game(&game);
  if (loadResult == false) { fprintf(stderr, "Could not load game: '%s'\n", _gameFilePath.c_str()); return false; }

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

  // Checking for changes in NVRAM
  _nvramChanged = false;

  // Checking if ports have been read
  _inputPortsRead = 0;

  // If resetting, do it now. Otherwise, running a single frame
  if (f->isReset == 1) retro_reset();
  else retro_run();

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

  m[memAreaIdx].Data  = retro_get_memory_data(RETRO_MEMORY_VIDEO_RAM);
  m[memAreaIdx].Name  = "Video RAM";
  m[memAreaIdx].Size  = retro_get_memory_size(RETRO_MEMORY_VIDEO_RAM);
  m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
  memAreaIdx++;

  m[memAreaIdx].Data  = get_sram_buffer();
  m[memAreaIdx].Name  = "Non-volatile RAM";
  m[memAreaIdx].Size  = get_sram_size();
  m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE; 
  memAreaIdx++;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
  InputCallback = callback;
}
