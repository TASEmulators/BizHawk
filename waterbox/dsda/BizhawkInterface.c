#include "BizhawkInterface.h"

bool foundIWAD = false;
bool wipeDone = true;
int lookHeld[4] = { 0 };
int lastButtons[4] = { 0 };
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

void player_input(struct PackedPlayerInput *src, int id)
{
  int lspeed = 0;
  int look = 0;
  int flyheight = 0;
  int buttons = src->Buttons & EXTRA_BUTTON_MASK;
  player_t *player = &players[consoleplayer];
  ticcmd_t *dest = &local_cmds[id];

  dest->forwardmove = src->RunSpeed;
  dest->sidemove    = src->StrafingSpeed;
  dest->lookfly     = src->FlyLook;
  dest->arti        = src->ArtifactUse; // use specific artifact (also jump/die)
  dest->angleturn   = src->TurningSpeed;
  dest->buttons     = src->Buttons & REGULAR_BUTTON_MASK;

  // explicitly select artifact through in-game GUI
  if (buttons & INVENTORY_LEFT && !(lastButtons[id] & INVENTORY_LEFT))
    InventoryMoveLeft ();

  if (buttons & INVENTORY_RIGHT && !(lastButtons[id] & INVENTORY_RIGHT))
    InventoryMoveRight();

  if (buttons & INVENTORY_SKIP && !(lastButtons[id] & INVENTORY_SKIP))
  { /* TODO */ }

  /* THE REST IS COPYPASTE FROM G_BuildTiccmd()!!! */

  if (buttons & ARTIFACT_USE && !(lastButtons[id] & ARTIFACT_USE))
  {
    // use currently selected artifact
    if (inventory)
    {
      player->readyArtifact = player->inventory[inv_ptr].type;
      inventory = false;
      dest->arti &= ~AFLAG_MASK; // leave jump/die intact, zero out the rest
    }
    else
    {
      dest->arti |= player->inventory[inv_ptr].type & AFLAG_MASK;
    }
  }

  // look/fly up/down/center keys override analog value
  if (buttons & LOOK_DOWN || buttons & LOOK_UP)
    ++lookHeld[id];
  else
    lookHeld[id] = 0;

  if (lookHeld[id] < SLOWTURNTICS)
    lspeed = 1;
  else
    lspeed = 2;

  if (buttons & LOOK_UP)     look      =  lspeed;
  if (buttons & LOOK_DOWN)   look      = -lspeed;
  if (buttons & LOOK_CENTER) look      = TOCENTER;
  if (buttons & FLY_UP)      flyheight =  5; // note that the actual flyheight will be twice this
  if (buttons & FLY_DOWN)    flyheight = -5;
  if (buttons & FLY_CENTER)
  {
    flyheight = TOCENTER;
    look      = TOCENTER;
  }

  if (player->playerstate == PST_LIVE /*&& !dsda_FreeAim()*/)
  {
      if (look < 0) look += 16;
      dest->lookfly = look;
  }
  if (flyheight < 0) flyheight += 16;
  dest->lookfly |= flyheight << 4;

  // weapon selection
  if (dest->buttons & BT_CHANGE)
  {
    int newweapon = src->WeaponSelect - 1;

    if (!demo_compatibility)
    {
      // only select chainsaw from '1' if it's owned, it's
      // not already in use, and the player prefers it or
      // the fist is already in use, or the player does not
      // have the berserker strength.
      if (newweapon==wp_fist
        && player->weaponowned[wp_chainsaw]
        && player->readyweapon!=wp_chainsaw
        && (player->readyweapon==wp_fist || !player->powers[pw_strength] || P_WeaponPreferred(wp_chainsaw, wp_fist)))
        newweapon = wp_chainsaw;
    }

    dest->buttons |= (newweapon) << BT_WEAPONSHIFT;
  }

  lastButtons[id] = buttons;
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

ECL_EXPORT void dsda_get_video(struct VideoInfo* vi)
{
  vi->buffer = (uint8_t *)headlessGetVideoBuffer();
  vi->width = headlessGetVideoWidth();
  vi->height = headlessGetVideoHeight();
  vi->pitch = headlessGetVideoPitch();
  vi->paletteSize = PALETTE_SIZE;

  uint32_t *palette = headlessGetPallette() + PALETTE_SIZE * currentPaletteIndex;
  for (size_t i = 0; i < PALETTE_SIZE; i++)
  {
    uint8_t *srcColor = (uint8_t *)&palette[i];
    uint8_t *dstColor = (uint8_t *)&_convertedPaletteBuffer[i];
    dstColor[0] = srcColor[2];
    dstColor[1] = srcColor[1];
    dstColor[2] = srcColor[0];
    dstColor[3] = srcColor[3];
  }

  vi->paletteBuffer = _convertedPaletteBuffer;
}

ECL_EXPORT void dsda_init_video(struct PackedRenderInfo *renderInfo)
{
  render_updates(renderInfo);
  headlessUpdateVideo();
}

ECL_EXPORT bool dsda_frame_advance(AutomapButtons buttons, struct PackedPlayerInput *player1Inputs, struct PackedPlayerInput *player2Inputs, struct PackedPlayerInput *player3Inputs, struct PackedPlayerInput *player4Inputs, struct PackedRenderInfo *renderInfo)
{
  if (renderInfo->RenderVideo)
    render_updates(renderInfo);

  // Setting inputs
  headlessClearTickCommand();

  if (renderInfo->RenderVideo && gamestate == GS_LEVEL)
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
  if (type == ARRAY_THINGS)
  {
    if (addr >= numthings * MEMORY_PADDED_THING)
      return MEMORY_OUT_OF_BOUNDS;

    int index    = addr / MEMORY_PADDED_THING;
    int offset   = addr % MEMORY_PADDED_THING;
    mobj_t *mobj = mobj_ptrs[index];

    if (mobj == NULL || offset >= sizeof(mobj_t))
      return MEMORY_NULL;

    char *data = (char *)mobj + offset;  
    return *data;
  }
  else if (type == ARRAY_LINES)
  {
    if (addr >= numlines * MEMORY_PADDED_LINE)
      return MEMORY_OUT_OF_BOUNDS;

    int index    = addr / MEMORY_PADDED_LINE;
    int offset   = addr % MEMORY_PADDED_LINE;
    int size     = sizeof(line_t);
    line_t *line = &lines[index];

    if (line == NULL || offset >= size + MEMORY_LINE_EXTRA)
      return MEMORY_NULL;

    if (offset >= size)
    {
      int extra_size   = sizeof(int) * 2;
      int extra_index  = (offset - size) / extra_size;
      int extra_offset = (offset - size) % extra_size;
      char *data       = extra_index == 0
        ? (char *)line->v1 + extra_offset
        : (char *)line->v2 + extra_offset;
      return *data;
    }

    char *data = (char *)line + offset;
    return *data;
  }
  else
    return MEMORY_OUT_OF_BOUNDS;
}