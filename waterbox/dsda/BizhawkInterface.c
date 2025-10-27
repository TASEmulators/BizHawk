#include "BizhawkInterface.h"

bool foundIWAD = false;
bool wipeDone = true;
bool fullVision = false;
int lookHeld[4] = { 0 };
int lastButtons[4] = { 0 };
AutomapButtons last_buttons = { 0 };

void render_updates(struct PackedRenderInfo *renderInfo)
{
  displayplayer = consoleplayer = renderInfo->PlayerPointOfView;
  dsda_reveal_map               = renderInfo->MapDetails;

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

  dsda_UpdateIntConfig(dsda_config_screenblocks, renderInfo->HeadsUpMode != HUD_VANILLA ? 11 : 10, true);
  dsda_UpdateIntConfig(dsda_config_hud_displayed, renderInfo->HeadsUpMode == HUD_NONE    ?  0 :  1, true);

  if (dsda_IntConfig(dsda_config_map_trail_mode) != renderInfo->MapTrail)
    dsda_UpdateIntConfig(dsda_config_map_trail_mode, renderInfo->MapTrail, true);

  if (dsda_IntConfig(dsda_config_map_trail_size) != renderInfo->MapTrailSize)
    dsda_UpdateIntConfig(dsda_config_map_trail_size, renderInfo->MapTrailSize, true);

  if (fullVision)
  {
    dsda_UpdateIntConfig(dsda_config_palette_ondamage, 0, true);
    dsda_UpdateIntConfig(dsda_config_palette_onbonus,  0, true);
    dsda_UpdateIntConfig(dsda_config_palette_onpowers, 0, true);

    for (int i = 0; i < g_maxplayers; i++)
    {
      if (playeringame[i])
      {
        players[i].fixedcolormap = 1;
        players[i].powers[pw_infrared] = -1;
      }
    }
  }
}

// normally this is caused by keyboard inputs, while mouse buttons reset the cast.
// we don't expose actual keyboard to user, so we have to trigger this
// based on what's usually a "random" keyboard input
void finale_inputs()
{
  if (gamestate != GS_FINALE)
    return;

  // pass fake event because finale only checks if it's keydown
  event_t event;
  event.type = ev_keydown;
  F_Responder(&event);
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
  player_t *player = &players[id];
  ticcmd_t *dest = &local_cmds[id];

  dest->forwardmove = src->RunSpeed;
  dest->sidemove    = src->StrafingSpeed;
  dest->lookfly     = src->FlyLook;
  dest->arti        = src->ArtifactUse; // use specific artifact (also jump/die)
  dest->angleturn   = src->TurningSpeed;
  dest->buttons     = src->Buttons & REGULAR_BUTTON_MASK;

  // explicitly select artifact through in-game GUI
  if (buttons & BUTTON_INVENTORY_LEFT && !(lastButtons[id] & BUTTON_INVENTORY_LEFT))
    InventoryMoveLeft ();

  if (buttons & BUTTON_INVENTORY_RIGHT && !(lastButtons[id] & BUTTON_INVENTORY_RIGHT))
    InventoryMoveRight();

  if (buttons & BUTTON_INVENTORY_SKIP && !(lastButtons[id] & BUTTON_INVENTORY_SKIP))
  { /* TODO */ }

  /* THE REST IS COPYPASTE FROM G_BuildTiccmd()!!! */

  if (buttons & BUTTON_ARTIFACT_USE && !(lastButtons[id] & BUTTON_ARTIFACT_USE))
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
  if (buttons & BUTTON_LOOK_DOWN || buttons & BUTTON_LOOK_UP)
    ++lookHeld[id];
  else
    lookHeld[id] = 0;

  if (lookHeld[id] < SLOWTURNTICS)
    lspeed = 1;
  else
    lspeed = 2;

  if (buttons & BUTTON_LOOK_UP)     look      =  lspeed;
  if (buttons & BUTTON_LOOK_DOWN)   look      = -lspeed;
  if (buttons & BUTTON_LOOK_CENTER) look      = TOCENTER;
  if (buttons & BUTTON_FLY_UP)      flyheight =  5; // note that the actual flyheight will be twice this
  if (buttons & BUTTON_FLY_DOWN)    flyheight = -5;
  if (buttons & BUTTON_FLY_CENTER)
  {
    flyheight = TOCENTER;
    look      = TOCENTER;
  }

  if (look != 0)
  {
    if (player->playerstate == PST_LIVE /*&& !dsda_FreeAim()*/)
    {
        if (look < 0) look += 16;
        dest->lookfly = look;
    }
  }
  if (flyheight != 0)
  {
    if (flyheight < 0) flyheight += 16;
    dest->lookfly |= flyheight << 4;
  }

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
        && (
          player->readyweapon==wp_fist
          || !player->powers[pw_strength]
          || P_WeaponPreferred(wp_chainsaw, wp_fist)
        )
      )
        newweapon = wp_chainsaw;

      // Select SSG from '3' only if it's owned and the player
      // does not have a shotgun, or if the shotgun is already
      // in use, or if the SSG is not already in use and the
      // player prefers it.
      if (newweapon == wp_shotgun
        && gamemode == commercial
        && player->weaponowned[wp_supershotgun]
        && (
          !player->weaponowned[wp_shotgun]
          || player->readyweapon == wp_shotgun
          || (player->readyweapon != wp_supershotgun && P_WeaponPreferred(wp_supershotgun, wp_shotgun))
        )
      )
        newweapon = wp_supershotgun;
    }

    dest->buttons |= (newweapon) << BT_WEAPONSHIFT;
  }

  if (dest->forwardmove
    || dest->sidemove
    || dest->lookfly
    || dest->arti)
  {
    finale_inputs();
  }

  lastButtons[id] = buttons;
}

void walkcam_inputs(struct PackedPlayerInput *inputs)
{
  sector_t *sec = R_PointInSector(players[consoleplayer].mo->x, players[consoleplayer].mo->y);
  
  if (inputs->WeaponSelect != walkcamera.type && inputs->WeaponSelect >= 0) // repurposed!
  {
    walkcamera.type = inputs->WeaponSelect;
    walkcamera.z    = sec->floorheight + 41 * FRACUNIT;
    P_SyncWalkcam(true, true);
  }

  if (!walkcamera.type)
    return;

  if (inputs->Buttons & BT_ATTACK)
  {
    walkcamera.x     = players[consoleplayer].mo->x;
    walkcamera.y     = players[consoleplayer].mo->y;
    walkcamera.z     = sec->floorheight + 41 * FRACUNIT;
    walkcamera.angle = players[consoleplayer].mo->angle;
  //walkcamera.pitch = dsda_PlayerPitch(&players[0]);
  }

  // moving forward
  walkcamera.x += FixedMul(
    (ORIG_FRICTION / 4) * inputs->RunSpeed,
    finecosine[walkcamera.angle >> ANGLETOFINESHIFT]);
  walkcamera.y += FixedMul(
    (ORIG_FRICTION / 4) * inputs->RunSpeed,
    finesine[walkcamera.angle >> ANGLETOFINESHIFT]);

  // strafing
  walkcamera.x += FixedMul(
    (ORIG_FRICTION / 6) * inputs->StrafingSpeed,
    finecosine[(walkcamera.angle - ANG90) >> ANGLETOFINESHIFT]);
  walkcamera.y += FixedMul(
    (ORIG_FRICTION / 6) * inputs->StrafingSpeed,
    finesine[(walkcamera.angle - ANG90) >> ANGLETOFINESHIFT]);

  walkcamera.z     += (char)inputs->FlyLook * FRACUNIT; // repurposed!
  walkcamera.angle += (     inputs->TurningSpeed / 8) << ANGLETOFINESHIFT;
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
  fullVision = renderInfo->FullVision;
  render_updates(renderInfo);
  headlessUpdateVideo();
}

ECL_EXPORT bool dsda_frame_advance(AutomapButtons buttons, struct PackedPlayerInput *playerInputs, struct PackedPlayerInput *walkcamInputs, struct PackedRenderInfo *renderInfo)
{
  if (renderInfo->RenderVideo)
    render_updates(renderInfo);

  // Setting inputs
  headlessClearTickCommand();

  if (gamestate == GS_LEVEL)
  {
    automap_inputs(buttons);
    walkcam_inputs(walkcamInputs);
  }

  if (buttons.data)
    finale_inputs();

  // Setting Players inputs
  player_input(&playerInputs[0], 0);
  player_input(&playerInputs[1], 1);
  player_input(&playerInputs[2], 2);
  player_input(&playerInputs[3], 3);

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
      headlessUpdateVideo();
  }

  // Assume wipe is lag
  return !wipeDone;
}

ECL_ENTRY void (*random_callback_cb)(int);

void biz_random_callback(int pr_class)
{
  if (random_callback_cb)
    random_callback_cb(pr_class);
}

ECL_EXPORT void dsda_set_random_callback(ECL_ENTRY void (*cb)(int))
{
  random_callback_cb = cb;
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

  displayplayer = consoleplayer = settings->DisplayPlayer;

  // Initializing DSDA core
  headlessMain(argc, argv);
  printf("DSDA Initialized\n");

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
  if (type == ARRAY_PLAYERS)
  {
    if (addr >= g_maxplayers * MEMORY_PADDED_PLAYER)
      return MEMORY_OUT_OF_BOUNDS;

    int index  = addr / MEMORY_PADDED_PLAYER;
    int offset = addr % MEMORY_PADDED_PLAYER;

    if (!playeringame[index] || offset >= sizeof(player_t))
      return MEMORY_NULL;

    player_t *player = &players[index];

    char *data = (char *)player + offset;  
    return *data;
  }
  else if (type == ARRAY_THINGS)
  {
    if (addr >= thinker_count * MEMORY_PADDED_THING)
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
    line_t *line = &lines[index];

    if (line == NULL || offset >= sizeof(line_t))
      return MEMORY_NULL;

    char *data = (char *)line + offset;
    return *data;
  }
  else if (type == ARRAY_SECTORS)
  {
    if (addr >= numsectors * MEMORY_PADDED_SECTOR)
      return MEMORY_OUT_OF_BOUNDS;

    int index    = addr / MEMORY_PADDED_SECTOR;
    int offset   = addr % MEMORY_PADDED_SECTOR;
    sector_t *sector = &sectors[index];

    if (sector == NULL || offset >= sizeof(sector_t))
      return MEMORY_NULL;

    char *data = (char *)sector + offset;  
    return *data;
  }
  else
    return MEMORY_OUT_OF_BOUNDS;
}