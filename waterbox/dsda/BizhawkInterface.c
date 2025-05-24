#include "BizhawkInterface.h"

bool foundIWAD = false;
bool wipeDone = true;
AutomapButtons last_buttons = { 0 };

void render_updates(struct PackedRenderInfo *renderInfo)
{
  dsda_UpdateIntConfig(dsda_config_usegamma,           renderInfo->Gamma,              true);
  dsda_UpdateIntConfig(dsda_config_automap_overlay,    renderInfo->MapOverlay,         true);
  dsda_UpdateIntConfig(dsda_config_show_messages,      renderInfo->ShowMessages,       true);
  dsda_UpdateIntConfig(dsda_config_sfx_volume,         renderInfo->SfxVolume,          true);
  dsda_UpdateIntConfig(dsda_config_music_volume,       renderInfo->MusicVolume,        true);
  dsda_UpdateIntConfig(dsda_config_hudadd_secretarea,  renderInfo->ReportSecrets,      true);
  dsda_UpdateIntConfig(dsda_config_exhud,              renderInfo->DsdaExHud,          true);
  dsda_UpdateIntConfig(dsda_config_coordinate_display, renderInfo->DisplayCoordinates, true);
  dsda_UpdateIntConfig(dsda_config_command_display,    renderInfo->DisplayCommands,    true);
  dsda_UpdateIntConfig(dsda_config_map_totals,         renderInfo->MapTotals,          true);
  dsda_UpdateIntConfig(dsda_config_map_time,           renderInfo->MapTime,            true);
  dsda_UpdateIntConfig(dsda_config_map_coordinates,    renderInfo->MapCoordinates,     true);
  dsda_UpdateIntConfig(dsda_config_screenblocks,       renderInfo->HeadsUpMode != HUD_VANILLA ? 11 : 10, true);
  dsda_UpdateIntConfig(dsda_config_hud_displayed,      renderInfo->HeadsUpMode == HUD_NONE    ?  0 :  1, true);
}

void automap_inputs(AutomapButtons buttons)
{
  static int bigstate = 0;
  m_paninc.y = 0;
  m_paninc.x = 0;

  if (buttons.AutomapToggle && !last_buttons.AutomapToggle)
  {
    if (automap_active)
    {
      AM_Stop(true);
      bigstate = 0;
    }
    else
      AM_Start(true);
  }

  if (buttons.AutomapFollow && !last_buttons.AutomapFollow)
  {
    dsda_ToggleConfig(dsda_config_automap_follow, true);
    dsda_AddMessage(automap_follow ? AMSTR_FOLLOWON : AMSTR_FOLLOWOFF);
  }
  
  if (buttons.AutomapGrid && !last_buttons.AutomapGrid)
  {
    dsda_ToggleConfig(dsda_config_automap_grid, true);
    dsda_AddMessage(automap_grid ? AMSTR_GRIDON : AMSTR_GRIDOFF);
  }
  
  if (buttons.AutomapMark && !last_buttons.AutomapMark)
  {
    if (!raven)
    {
      AM_addMark();
      doom_printf("%s %d", AMSTR_MARKEDSPOT, markpointnum - 1);
    }
  }
  
  if (buttons.AutomapClearMarks && !last_buttons.AutomapClearMarks)
  {
    AM_clearMarks();
    dsda_AddMessage(AMSTR_MARKSCLEARED);
  }

  if (buttons.AutomapFullZoom && !last_buttons.AutomapFullZoom)
  {
    bigstate = !bigstate;
    if (bigstate)
    {
      AM_saveScaleAndLoc();
      AM_minOutWindowScale();
    }
    else
      AM_restoreScaleAndLoc();
  }

  if (buttons.AutomapZoomOut)
  {
    mtof_zoommul = M_ZOOMOUT;
    ftom_zoommul = M_ZOOMIN;
    curr_mtof_zoommul = mtof_zoommul;
    zoom_leveltime = leveltime;
  }
  else if (buttons.AutomapZoomIn)
  {
    mtof_zoommul = M_ZOOMIN;
    ftom_zoommul = M_ZOOMOUT;
    curr_mtof_zoommul = mtof_zoommul;
    zoom_leveltime = leveltime;
  }
  else
  {
    stop_zooming = true;
    if (leveltime != zoom_leveltime)
      AM_StopZooming();
  }

  if (!automap_follow)
  {
    if (buttons.AutomapUp)    m_paninc.y += FTOM(map_pan_speed);
    if (buttons.AutomapDown)  m_paninc.y -= FTOM(map_pan_speed);
    if (buttons.AutomapRight) m_paninc.x += FTOM(map_pan_speed);
    if (buttons.AutomapLeft)  m_paninc.x -= FTOM(map_pan_speed);
  }

  last_buttons = buttons;
}

void player_input(struct PackedPlayerInput *inputs, int playerId)
{
  local_cmds[playerId].forwardmove = inputs->RunSpeed;
  local_cmds[playerId].sidemove    = inputs->StrafingSpeed;
  local_cmds[playerId].lookfly     = inputs->FlyLook;
  local_cmds[playerId].arti        = inputs->ArtifactUse;
  local_cmds[playerId].angleturn   = inputs->TurningSpeed;

  if (inputs->Buttons.Fire) local_cmds[playerId].buttons |= 0b00000001;
  if (inputs->Buttons.Use)  local_cmds[playerId].buttons |= 0b00000010;
  if (inputs->EndPlayer)    local_cmds[playerId].arti    |= 0b01000000;
  if (inputs->Jump)         local_cmds[playerId].arti    |= 0b10000000;

  if (inputs->WeaponSelect)
  {
    local_cmds[playerId].buttons |= BT_CHANGE;
    local_cmds[playerId].buttons |= (inputs->WeaponSelect - 1) << BT_WEAPONSHIFT;
  }
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

ECL_EXPORT bool dsda_frame_advance(AutomapButtons buttons, struct PackedPlayerInput *player1Inputs, struct PackedPlayerInput *player2Inputs, struct PackedPlayerInput *player3Inputs, struct PackedPlayerInput *player4Inputs, struct PackedRenderInfo *renderInfo)
{
  // Live render changes
  if (renderInfo->RenderVideo)
    render_updates(renderInfo);

  // Setting inputs
  headlessClearTickCommand();

  if (renderInfo->RenderVideo)
    automap_inputs(buttons);

  dsda_reveal_map = renderInfo->MapDetails;

  // Setting Players inputs
  player_input(player1Inputs, 0);
  player_input(player2Inputs, 1);
  player_input(player3Inputs, 2);
  player_input(player4Inputs, 3);

  // Enabling/Disabling rendering, as required
  if ( renderInfo->RenderVideo) headlessEnableVideoRendering();
  if ( renderInfo->RenderAudio) headlessEnableAudioRendering();
  if (!renderInfo->RenderVideo) headlessDisableVideoRendering();
  if (!renderInfo->RenderAudio) headlessDisableAudioRendering();

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
    if (renderInfo->RenderAudio)
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

  //if (compatibility_level >= boom_202_compatibility)
  //  rngseed = settings->RNGSeed;

  // Initializing audio
  I_SetSoundCap();
  I_InitSound();
  printf("Audio Initialized\n");

  // If required, prevent level exit and game end triggers
  preventLevelExit = settings->PreventLevelExit;
  preventGameEnd   = settings->PreventGameEnd;

  printf("Prevent Level Exit:  %d\n", preventLevelExit);
  printf("Prevent Game End:    %d\n", preventGameEnd);
  printf("Compatibility Level: %d\n", compatibility_level);

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