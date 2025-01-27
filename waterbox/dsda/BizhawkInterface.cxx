#include <cstdio>
#include <cstdint>
#include "BizhawkInterface.hxx"
#include <d_player.h>

extern "C"
{
  int headlessMain(int argc, char **argv);
  void headlessRunSingleTick();
  void headlessUpdateSounds(void);
  void headlessClearTickCommand();
  void headlessSetTickCommand(int playerId, int forwardSpeed, int strafingSpeed, int turningSpeed, int fire, int action, int weapon, int altWeapon);

  // Video-related functions
  void headlessUpdateVideo(void);
  void* headlessGetVideoBuffer();
  int headlessGetVideoPitch();
  int headlessGetVideoWidth();
  int headlessGetVideoHeight();
  void headlessEnableRendering();
  void headlessDisableRendering();
  uint32_t* headlessGetPallette();

  void headlessSetSaveStatePointer(void* savePtr, int saveStateSize);
  size_t headlessGetEffectiveSaveSize();
  void dsda_ArchiveAll(void);
  void dsda_UnArchiveAll(void);
  void headlessGetMapName(char* outString);
}

// Players information
extern "C" int enableOutput;
extern "C" player_t players[MAX_MAXPLAYERS];
extern "C" int preventLevelExit;
extern "C" int preventGameEnd;
extern "C" int reachedLevelExit;
extern "C" int reachedGameEnd;
extern "C" int gamemap;
extern "C" int gametic;

uint8_t* _videoBuffer;

struct InitSettings
{
	uint32_t dummy;
};

ECL_EXPORT void dsda_get_audio(int *n, void **buffer)
{
	*n = 0;
	*buffer = nullptr;
}

ECL_EXPORT void dsda_get_video(int& w, int& h, int& pitch, uint8_t*& buffer)
{
	buffer = _videoBuffer;
	w = 320;
	h = 200;
	pitch = 0;
}


ECL_EXPORT void dsda_frame_advance()
{
}

ECL_ENTRY void (*input_callback_cb)(void);

void real_input_callback(void)
{
	if (input_callback_cb)
		input_callback_cb();
}

ECL_EXPORT void dsda_set_input_callback(ECL_ENTRY void (*fecb)(void))
{
	input_callback_cb = fecb;
}


ECL_EXPORT int dsda_init(
	const char* wadFileName,
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
    _videoBuffer = (uint8_t*) alloc_invisible(4 * 1024 * 1024);
	return 1;
}

