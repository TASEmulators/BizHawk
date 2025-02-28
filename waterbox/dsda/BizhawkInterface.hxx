#pragma once

#include <vector>
#include <string>
#include <cstdio>
#include <cstdint>

#include "emulibc.h"
#include "d_player.h"
#include "w_wad.h"
#include "p_mobj.h"
#include "doomstat.h"
#include "g_game.h"

#include "dsda/args.h"

extern "C"
{
  int headlessMain(int argc, char **argv);
  void headlessRunSingleTick();
  void headlessUpdateSounds(void);
  void headlessClearTickCommand();
  void headlessSetTickCommand(int playerId, int forwardSpeed, int strafingSpeed, int turningSpeed, int fire, int action, int weapon, int automap, int lookfly, int artifact, int jump, int endPlayer);

  // Video-related functions
  void headlessUpdateVideo(void);
  void* headlessGetVideoBuffer();
  int headlessGetVideoPitch();
  int headlessGetVideoWidth();
  int headlessGetVideoHeight();
  void headlessEnableVideoRendering();
  void headlessDisableVideoRendering();
  void headlessEnableAudioRendering();
  void headlessDisableAudioRendering();
  uint32_t* headlessGetPallette();

  void headlessSetSaveStatePointer(void* savePtr, int saveStateSize);
  size_t headlessGetEffectiveSaveSize();
  void dsda_ArchiveAll(void);
  void dsda_UnArchiveAll(void);
  void headlessGetMapName(char* outString);
  
  void D_AddFile (const char *file, wad_source_t source, void* const buffer, const size_t size);
  void AddIWAD(const char *iwad, void* const buffer, const size_t size); 
  unsigned char * I_CaptureAudio (int* nsamples);
  void I_InitSound(void);
  void I_SetSoundCap (void);
}

// Players information
extern "C" int enableOutput;
extern "C" int preventLevelExit;
extern "C" int preventGameEnd;
extern "C" int reachedLevelExit;
extern "C" int reachedGameEnd;
extern int numthings;
extern mobj_t **mobj_ptrs;
extern dsda_arg_t arg_value[dsda_arg_count];

#define PALETTE_SIZE 256
uint32_t _convertedPaletteBuffer[PALETTE_SIZE];

enum MemoryArrayType
{
	ARRAY_THINGS = 0,
	ARRAY_LINES = 1,
	ARRAY_SECTORS = 2
};

struct InitSettings
{
	int _Player1Present;
	int _Player2Present;
	int _Player3Present;
	int _Player4Present;
	int _Player1Class;
	int _Player2Class;
	int _Player3Class;
	int _Player4Class;
	int _PreventLevelExit;
	int _PreventGameEnd;
} __attribute__((packed));

struct PackedPlayerInput
{
	int _RunSpeed;
	int _StrafingSpeed;
	int _TurningSpeed;
	int _WeaponSelect;
	int _Fire;
	int _Action;
	int _Automap;
	int _FlyLook;
	int _ArtifactUse;
	int _Jump;
	int _EndPlayer;
} __attribute__((packed));

struct PackedRenderInfo
{
	int _RenderVideo;
	int _RenderAudio;
	int _PlayerPointOfView;
} __attribute__((packed));

dboolean dsda_Flag(dsda_arg_identifier_t id) {
  return arg_value[id].found;
}