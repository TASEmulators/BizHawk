#ifndef __BIZHAWK_INTERFACE__
#define __BIZHAWK_INTERFACE__

#include "emulibc.h"

#include "am_map.h"
#include "d_englsh.h"
#include "d_main.h"
#include "d_player.h"
#include "doomstat.h"
#include "doomtype.h"
#include "f_wipe.h"
#include "g_game.h"
#include "i_sound.h"
#include "i_video.h"
#include "p_mobj.h"

#include "dsda/args.h"
#include "dsda/messenger.h"
#include "dsda/settings.h"

extern int headlessMain(int argc, char **argv);
extern void headlessRunSingleTick();
extern void headlessClearTickCommand();
extern void headlessSetTickCommand(int playerId, int forwardSpeed, int strafingSpeed, int turningSpeed, int fire, int action, int weapon, int automap, int lookfly, int artifact, int jump, int endPlayer);
extern void headlessGetMapName(char *outString);
extern void headlessSetSaveStatePointer(void *savePtr, int saveStateSize);
extern size_t headlessGetEffectiveSaveSize();
extern unsigned int rngseed;

extern dboolean InventoryMoveLeft();
extern dboolean InventoryMoveRight();

// Video
extern void headlessUpdateVideo();
extern void* headlessGetVideoBuffer();
extern int headlessGetVideoPitch();
extern int headlessGetVideoWidth();
extern int headlessGetVideoHeight();
extern void headlessEnableVideoRendering();
extern void headlessDisableVideoRendering();
extern uint32_t* headlessGetPallette();
extern int currentPaletteIndex;

// Audio
extern void headlessUpdateSounds();
extern void headlessEnableAudioRendering();
extern void headlessDisableAudioRendering();
extern uint8_t *I_CaptureAudio (int *nsamples);

// Players information
extern int enableOutput;
extern int preventLevelExit;
extern int preventGameEnd;
extern int reachedLevelExit;
extern int reachedGameEnd;
extern int numthings;
extern mobj_t **mobj_ptrs;
extern dsda_arg_t arg_value[dsda_arg_count];
extern int inv_ptr;
extern dboolean inventory;

// Automap
extern void AM_addMark();
extern void AM_StopZooming();
extern void AM_saveScaleAndLoc();
extern int AM_minOutWindowScale();
extern int AM_restoreScaleAndLoc();
extern int dsda_reveal_map;
extern int automap_active;
extern int automap_follow;
extern int automap_grid;
extern int markpointnum;
extern int zoom_leveltime;
extern dboolean stop_zooming;
extern mpoint_t m_paninc;
extern fixed_t mtof_zoommul;
extern fixed_t ftom_zoommul;
extern fixed_t curr_mtof_zoommul;
extern int map_pan_speed;
extern int map_scroll_speed;
extern fixed_t scale_mtof;
extern fixed_t scale_ftom;
#define FTOM(x) FixedMul(((x)<<16),scale_ftom)
#define M_ZOOMIN ((int) ((float)FRACUNIT * (1.00f + map_scroll_speed / 200.0f)))
#define M_ZOOMOUT ((int) ((float)FRACUNIT / (1.00f + map_scroll_speed / 200.0f)))

#ifdef PALETTE_SIZE
#undef PALETTE_SIZE
#endif
#define PALETTE_SIZE 256
uint32_t _convertedPaletteBuffer[PALETTE_SIZE];

#define SLOWTURNTICS 6

enum ExtraButtons
{
  REGULAR_BUTTON_MASK = 0b0000000000000111,
  INVENTORY_LEFT      = 0b0000000000001000,
  INVENTORY_RIGHT     = 0b0000000000010000,
  INVENTORY_SKIP      = 0b0000000000100000,
  ARTIFACT_USE        = 0b0000000001000000,
  LOOK_UP             = 0b0000000010000000,
  LOOK_DOWN           = 0b0000000100000000,
  LOOK_CENTER         = 0b0000001000000000,
  FLY_UP              = 0b0000010000000000,
  FLY_DOWN            = 0b0000100000000000,
  FLY_CENTER          = 0b0001000000000000,
  EXTRA_BUTTON_MASK   = 0b0001111111111000,
};

enum HudMode
{
  HUD_VANILLA = 0,
  HUD_DSDA    = 1,
  HUD_NONE    = 2
};

enum MemoryArrayType
{
  ARRAY_THINGS  = 0,
  ARRAY_LINES   = 1,
  ARRAY_SECTORS = 2
};

typedef union
{
    struct
    {
        bool AutomapToggle:1;
        bool AutomapZoomIn:1;
        bool AutomapZoomOut:1;
        bool AutomapFullZoom:1;
        bool AutomapFollow:1;
        bool AutomapUp:1;
        bool AutomapDown:1;
        bool AutomapRight:1;
        bool AutomapLeft:1;
        bool AutomapGrid:1;
        bool AutomapMark:1;
        bool AutomapClearMarks:1;
    };
    uint32_t data;
} AutomapButtons;

struct InitSettings
{
  int Player1Present;
  int Player2Present;
  int Player3Present;
  int Player4Present;
  int Player1Class;
  int Player2Class;
  int Player3Class;
  int Player4Class;
  int PreventLevelExit;
  int PreventGameEnd;
  //unsigned int RNGSeed;
} __attribute__((packed));

struct PackedPlayerInput
{
  int RunSpeed;
  int StrafingSpeed;
  int TurningSpeed;
  int WeaponSelect;
  int Buttons;
  int FlyLook;
  int ArtifactUse;
  int Jump;
  int EndPlayer;
} __attribute__((packed));

struct PackedRenderInfo
{
  int RenderVideo;
  int RenderAudio;
  int SfxVolume;
  int MusicVolume;
  int Gamma;
  int ShowMessages;
  int ReportSecrets;
  int HeadsUpMode;
  int DsdaExHud;
  int DisplayCoordinates;
  int DisplayCommands;
  int MapTotals;
  int MapTime;
  int MapCoordinates;
  int MapDetails;
  int MapOverlay;
  int PlayerPointOfView;
} __attribute__((packed));

struct VideoInfo {
  int width;
	int height;
	int pitch;
	int paletteSize;
	uint32_t* paletteBuffer;
	uint8_t* buffer;
};

#endif