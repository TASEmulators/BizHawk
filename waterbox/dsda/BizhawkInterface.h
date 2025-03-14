#ifndef __BIZHAWK_INTERFACE__
#define __BIZHAWK_INTERFACE__

#include "emulibc.h"
#include "d_player.h"
#include "w_wad.h"
#include "p_mobj.h"
#include "doomstat.h"
#include "g_game.h"

#include "dsda/args.h"

extern int headlessMain(int argc, char **argv);
extern void headlessRunSingleTick();
extern void headlessUpdateSounds(void);
extern void headlessClearTickCommand();
extern void headlessSetTickCommand(int playerId, int forwardSpeed, int strafingSpeed, int turningSpeed, int fire, int action, int weapon, int automap, int lookfly, int artifact, int jump, int endPlayer);

  // Video-related functions
extern void headlessUpdateVideo(void);
extern void* headlessGetVideoBuffer();
extern int headlessGetVideoPitch();
extern int headlessGetVideoWidth();
extern int headlessGetVideoHeight();
extern void headlessEnableVideoRendering();
extern void headlessDisableVideoRendering();
extern void headlessEnableAudioRendering();
extern void headlessDisableAudioRendering();
  uint32_t* headlessGetPallette();

extern void headlessSetSaveStatePointer(void* savePtr, int saveStateSize);
  size_t headlessGetEffectiveSaveSize();
extern void dsda_ArchiveAll(void);
extern void dsda_UnArchiveAll(void);
extern void headlessGetMapName(char* outString);
  
extern void D_AddFile (const char *file, wad_source_t source, void* const buffer, const size_t size);
extern void AddIWAD(const char *iwad, void* const buffer, const size_t size); 
extern unsigned char * I_CaptureAudio (int* nsamples);
extern void I_InitSound(void);
extern void I_SetSoundCap (void);

// Players information
extern int enableOutput;
extern int preventLevelExit;
extern int preventGameEnd;
extern int reachedLevelExit;
extern int reachedGameEnd;
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

#endif