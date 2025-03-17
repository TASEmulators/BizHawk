#include "BizhawkInterface.h"

bool foundIWAD = false;
bool wipeDone = true;
int last_automap_input[4] = { 0 };

void send_input(struct PackedPlayerInput *inputs, int playerId)
{
  local_cmds[playerId].forwardmove = inputs->RunSpeed;
  local_cmds[playerId].sidemove    = inputs->StrafingSpeed;
  local_cmds[playerId].angleturn   = (shorttics || !longtics) ? inputs->TurningSpeed << 8 : inputs->TurningSpeed;

  if (inputs->Fire == 1)   local_cmds[playerId].buttons |= 0b00000001;
  if (inputs->Action == 1) local_cmds[playerId].buttons |= 0b00000010;

  if (inputs->WeaponSelect != 0)
  {
    local_cmds[playerId].buttons |= BT_CHANGE;
    local_cmds[playerId].buttons |= (inputs->WeaponSelect - 1)<<BT_WEAPONSHIFT;
  }

  local_cmds[playerId].lookfly = inputs->FlyLook;
  local_cmds[playerId].arti    = inputs->ArtifactUse;
  
  if (inputs->EndPlayer == 1) local_cmds[playerId].arti |= 0b01000000;
  if (inputs->Jump      == 1) local_cmds[playerId].arti |= 0b10000000;

  if (inputs->Automap && !last_automap_input[playerId])
  {
    if (automap_input)
      AM_Stop(true);
    else
      AM_Start(true);
  }
  last_automap_input[playerId] = inputs->Automap;

  // printf("ForwardSpeed: %d - sideMove:     %d - angleTurn:    %d - buttons: %u\n", forwardSpeed, strafingSpeed, turningSpeed, local_cmds[playerId].buttons);
}

ECL_EXPORT void dsda_get_audio(int *n, void **buffer)
{
  int nSamples = 0;
  void* audioBuffer = NULL;
  audioBuffer = I_CaptureAudio(&nSamples);
  // printf("audioBuffer: %p - nSamples: %d\n", audioBuffer, nSamples);

  if (n)
    *n = nSamples;
  if (buffer)
    *buffer = audioBuffer;
}

ECL_EXPORT void dsda_get_video(int *w, int *h, int *pitch, uint8_t **buffer, int *paletteSize, uint32_t **paletteBuffer)
{
  *buffer = (uint8_t *)headlessGetVideoBuffer();
  *w = headlessGetVideoWidth();
  *h = headlessGetVideoHeight();
  *pitch = headlessGetVideoPitch();
  *paletteSize = PALETTE_SIZE;

  uint32_t *palette = headlessGetPallette();
  for (size_t i = 0; i < PALETTE_SIZE; i++)
  {
    uint8_t *srcColor = (uint8_t *)&palette[i];
    uint8_t *dstColor = (uint8_t *)&_convertedPaletteBuffer[i];
    dstColor[0] = srcColor[2];
    dstColor[1] = srcColor[1];
    dstColor[2] = srcColor[0];
    dstColor[3] = srcColor[3];
  }

  *paletteBuffer = _convertedPaletteBuffer;
}

ECL_EXPORT bool dsda_frame_advance(struct PackedPlayerInput *player1Inputs, struct PackedPlayerInput *player2Inputs, struct PackedPlayerInput *player3Inputs, struct PackedPlayerInput *player4Inputs, struct PackedRenderInfo *renderInfo)
{
  // Setting inputs
  headlessClearTickCommand();

  // Setting Players inputs
  send_input(player1Inputs, 0);
  send_input(player2Inputs, 1);
  send_input(player3Inputs, 2);
  send_input(player4Inputs, 3);

  // Enabling/Disabling rendering, as required
  if (!renderInfo->RenderVideo) headlessDisableVideoRendering();
  if (renderInfo->RenderVideo) headlessEnableVideoRendering();
  if (!renderInfo->RenderAudio) headlessDisableAudioRendering();
  if (renderInfo->RenderAudio) headlessEnableAudioRendering();

  if ((wipe_Pending() || !wipeDone) && dsda_RenderWipeScreen())
  {
    wipeDone = wipe_ScreenWipe(1);
    I_FinishUpdate();
  }
  else
  {
    // Running a single tick
    headlessRunSingleTick();

    // Move positional sounds
    headlessUpdateSounds();

    // Updating video
    if (renderInfo->RenderVideo)
    {
      displayplayer = consoleplayer = renderInfo->PlayerPointOfView;
      headlessUpdateVideo();
    }
  }

  // Assume wipe is lag
  return !wipeDone;
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

ECL_EXPORT int dsda_init(struct InitSettings *settings, int argc, char **argv)
{
  printf("Passing arguments: \n");
  for (int i = 0; i < argc; i++) printf("%s ", argv[i]);
  printf("\n");

  // Setting players in game
  playeringame[0] = settings->Player1Present;
  playeringame[1] = settings->Player2Present;
  playeringame[2] = settings->Player3Present;
  playeringame[3] = settings->Player4Present;

  // Handle class
  PlayerClass[0] = (pclass_t)settings->Player1Class;
  PlayerClass[1] = (pclass_t)settings->Player2Class;
  PlayerClass[2] = (pclass_t)settings->Player3Class;
  PlayerClass[3] = (pclass_t)settings->Player4Class;

  // Initializing DSDA core
  headlessMain(argc, argv);
  printf("DSDA Initialized\n");  

  switch(compatibility_level) {
  case prboom_6_compatibility:
    longtics = 1;
    break;
  case mbf21_compatibility:
    longtics = 1;
    shorttics = !dsda_Flag(dsda_arg_longtics);
    break;
  default:
    longtics = dsda_Flag(dsda_arg_longtics);
    break;
  }

  // Initializing audio
  I_SetSoundCap();
  I_InitSound();
  printf("Audio Initialized\n");

  // If required, prevent level exit and game end triggers
  preventLevelExit = settings->PreventLevelExit;
  preventGameEnd = settings->PreventGameEnd;

  printf("Prevent Level Exit: %d\n", preventLevelExit);
  printf("Prevent Game End:   %d\n", preventGameEnd);

  // Enabling DSDA output, for debugging
  enableOutput = 1;

  return 1;
}


ECL_EXPORT int dsda_add_wad_file(const char *filename, const int size, ECL_ENTRY int (*feload_archive_cb)(const char *filename, uint8_t *buffer, int maxsize))
{
  printf("Loading WAD '%s' of size %d...\n", filename, size);
  uint8_t *wadFileBuffer = (uint8_t *)alloc_invisible(size);

  if (wadFileBuffer == NULL)
  {
    fprintf(stderr, "Error creating buffer. Do we have enough memory in the waterbox?\n");
    return 0;
  }
  else
    printf("Created buffer at address: %p\n", wadFileBuffer);

  int loadSize = feload_archive_cb(filename, wadFileBuffer, size);
  if (loadSize != size)
  {
    fprintf(stderr, "Error loading '%s': read %d bytes, but expected %d bytes\n", filename, loadSize, size);
    return 0;
  }

  // Check size is enough
  if (size < 5)
  {
    fprintf(stderr, "Error loading '%s': read %d bytes, which is too small\n", filename, size);
    return 0;
  }

  // Getting wad header
  char header[5];
  header[0] = wadFileBuffer[0];
  header[1] = wadFileBuffer[1];
  header[2] = wadFileBuffer[2];
  header[3] = wadFileBuffer[3];
  header[4] = '\0';

  // Safety checks
  bool recognizedFormat = false;

  // Loading PWAD
  if (!strcmp(header, "PWAD"))
  {
    recognizedFormat = true;

    // Loading PWAD
    D_AddFile(filename, source_pwad, wadFileBuffer, size);
    printf("Loaded PWAD '%s' correctly\n", filename);
  } 

  // Loading IWAD
  if (!strcmp(header, "IWAD"))
  {
    recognizedFormat = true;

    // Checking for repeated IWAD
    if (foundIWAD == true)
    {
      fprintf(stderr, "Error with '%s': an IWAD was already loaded before\n", filename);
      return 0;
    }
    foundIWAD = true;

    // Loading IWAD
    printf("Loading IWAD '%s'...\n", filename);
    AddIWAD(filename, wadFileBuffer, size);
    printf("Loaded IWAD '%s' correctly\n", filename);
  } 

  // Checking for correct header
  if (recognizedFormat == false)
  {
    fprintf(stderr, "Error with '%s': it contains an unrecognized header '%s'\n", filename, header);
    return 0;
  }

  // All ok
  return 1 << gamemode;
}

// the Doom engine doesn't have traditional memory regions because it's not an emulator
// but there's still useful data in memory that we can expose
// so we turn it into artificial memory domains, one for each entity array
// TODO: expose sectors and linedefs like xdre does (but better)
ECL_EXPORT char dsda_read_memory_array(int type, uint32_t addr)
{
  char out_of_bounts = 0xFF;
  char null_thing = 0x88;
  int padded_size = 512; // sizeof(mobj_t) is 464 but we pad for nice representation

  if (addr >= numthings * padded_size)
    return out_of_bounts;

  int index = addr / padded_size;
  int offset = addr % padded_size;
  mobj_t *mobj = mobj_ptrs[index];

  if (mobj == NULL)
    return null_thing;

  char *data = (char *)mobj + offset;  
  return *data;
}