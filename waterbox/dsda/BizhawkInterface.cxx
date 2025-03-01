#include "BizhawkInterface.hxx"

ECL_EXPORT void dsda_get_audio(int *n, void **buffer)
{
	int nSamples = 0;
	void* audioBuffer = nullptr;
    audioBuffer = I_CaptureAudio(&nSamples);
	// printf("audioBuffer: %p - nSamples: %d\n", audioBuffer, nSamples);

	if (n)
		*n = nSamples;
	if (buffer)
		*buffer = audioBuffer;
}

ECL_EXPORT void dsda_get_video(int& w, int& h, int& pitch, uint8_t*& buffer, int& paletteSize, uint32_t*& paletteBuffer)
{
	buffer = (uint8_t*)headlessGetVideoBuffer();
	w = headlessGetVideoWidth();
	h = headlessGetVideoHeight();
	pitch = headlessGetVideoPitch();
	paletteSize = PALETTE_SIZE;

	auto palette = headlessGetPallette();
	for (size_t i = 0; i < PALETTE_SIZE; i++)
	{
		uint8_t* srcColor = (uint8_t*)&palette[i];
		uint8_t* dstColor = (uint8_t*)&_convertedPaletteBuffer[i];
		dstColor[0] = srcColor[2];
		dstColor[1] = srcColor[1];
		dstColor[2] = srcColor[0];
		dstColor[3] = srcColor[3];
	} 

	paletteBuffer = _convertedPaletteBuffer;
}

ECL_EXPORT void dsda_frame_advance(struct PackedPlayerInput *player1Inputs, struct PackedPlayerInput *player2Inputs, struct PackedPlayerInput *player3Inputs, struct PackedPlayerInput *player4Inputs, struct PackedRenderInfo *renderInfo)
{
	// Setting inputs
    headlessClearTickCommand();

    // Setting Player 1 inputs
	headlessSetTickCommand
	(
		0,
		player1Inputs->_RunSpeed,
		player1Inputs->_StrafingSpeed,
		player1Inputs->_TurningSpeed,
		player1Inputs->_Fire,
		player1Inputs->_Action,
		player1Inputs->_WeaponSelect,
		player1Inputs->_Automap,
		player1Inputs->_FlyLook,
		player1Inputs->_ArtifactUse,
		player1Inputs->_Jump,
		player1Inputs->_EndPlayer
	);

	// Setting Player 2 inputs
	headlessSetTickCommand
	(
		1,
		player2Inputs->_RunSpeed,
		player2Inputs->_StrafingSpeed,
		player2Inputs->_TurningSpeed,
		player2Inputs->_Fire,
		player2Inputs->_Action,
		player2Inputs->_WeaponSelect,
		player2Inputs->_Automap,
		player2Inputs->_FlyLook,
		player2Inputs->_ArtifactUse,
		player2Inputs->_Jump,
		player2Inputs->_EndPlayer
	);

	// Setting Player 3 inputs
	headlessSetTickCommand
	(
		2,
		player3Inputs->_RunSpeed,
		player3Inputs->_StrafingSpeed,
		player3Inputs->_TurningSpeed,
		player3Inputs->_Fire,
		player3Inputs->_Action,
		player3Inputs->_WeaponSelect,
		player3Inputs->_Automap,
		player3Inputs->_FlyLook,
		player3Inputs->_ArtifactUse,
		player3Inputs->_Jump,
		player3Inputs->_EndPlayer
	);

    // Setting Player 4 inputs
	headlessSetTickCommand
	(
		3,
		player4Inputs->_RunSpeed,
		player4Inputs->_StrafingSpeed,
		player4Inputs->_TurningSpeed,
		player4Inputs->_Fire,
		player4Inputs->_Action,
		player4Inputs->_WeaponSelect,
		player4Inputs->_Automap,
		player4Inputs->_FlyLook,
		player4Inputs->_ArtifactUse,
		player4Inputs->_Jump,
		player4Inputs->_EndPlayer
	);

   // Enabling/Disabling rendering, as required
   if (renderInfo->_RenderVideo == 0) headlessDisableVideoRendering();
   if (renderInfo->_RenderVideo == 1) headlessEnableVideoRendering();
   if (renderInfo->_RenderAudio == 0) headlessDisableAudioRendering();
   if (renderInfo->_RenderAudio == 1) headlessEnableAudioRendering();

	// Running a single tick
	headlessRunSingleTick();

	// Move positional sounds 
	headlessUpdateSounds();

    // Updating video
    if (renderInfo->_RenderVideo == 1)
	{
	  displayplayer = consoleplayer = renderInfo->_PlayerPointOfView;
	  headlessUpdateVideo();
	} 
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

bool foundIWAD = false;

ECL_EXPORT int dsda_init(InitSettings *settings, int argc, char **argv)
{
  printf("Passing arguments: \n");
  for (int i = 0; i < argc; i++) printf("%s ", argv[i]);
  printf("\n");

  // Setting players in game
  playeringame[0] = settings->_Player1Present;
  playeringame[1] = settings->_Player2Present;
  playeringame[2] = settings->_Player3Present;
  playeringame[3] = settings->_Player4Present;

  // Handle class
  PlayerClass[0] = (pclass_t)settings->_Player1Class;
  PlayerClass[1] = (pclass_t)settings->_Player2Class;
  PlayerClass[2] = (pclass_t)settings->_Player3Class;
  PlayerClass[3] = (pclass_t)settings->_Player4Class;

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
  preventLevelExit = settings->_PreventLevelExit;
  preventGameEnd = settings->_PreventGameEnd;

  printf("Prevent Level Exit: %d\n", preventLevelExit);
  printf("Prevent Game End:   %d\n", preventGameEnd);

  // Enabling DSDA output, for debugging
  enableOutput = 1;

  return 1;
}


ECL_EXPORT int dsda_add_wad_file(const char *filename, const int size, ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize))
{
  printf("Loading WAD '%s' of size %d...\n", filename, size);
  auto wadFileBuffer = (unsigned char*) alloc_invisible(size);

  if (wadFileBuffer == NULL) { fprintf(stderr, "Error creating buffer. Do we have enough memory in the waterbox?\n"); return 0; }
  else printf("Created buffer at address: %p\n", wadFileBuffer);
  
  int loadSize = feload_archive_cb(filename, wadFileBuffer, size);
  if (loadSize != size) { fprintf(stderr, "Error loading '%s': read %d bytes, but expected %d bytes\n", filename, loadSize, size); return 0; }

  // Check size is enough
  if (size < 5) { fprintf(stderr, "Error loading '%s': read %d bytes, which is too small\n", filename, size); return 0; }

  // Getting wad header
  char header[5];
  header[0] = wadFileBuffer[0];
  header[1] = wadFileBuffer[1];
  header[2] = wadFileBuffer[2];
  header[3] = wadFileBuffer[3];
  header[4] = '\0';

  // Getting string
  std::string headerString(header);

  // Safety checks
  bool recognizedFormat = false;

  // Loading PWAD
  if (headerString == "PWAD")
  {
	recognizedFormat = true;

    // Loading PWAD
	D_AddFile(filename, source_pwad, wadFileBuffer, size);
	printf("Loaded PWAD '%s' correctly\n", filename);
  } 

  // Loading IWAD
  if (headerString == "IWAD")
  {
    recognizedFormat = true;

    // Checking for repeated IWAD
	if (foundIWAD == true) { fprintf(stderr, "Error with '%s': an IWAD was already loaded before\n", filename); return 0; }
	foundIWAD = true;

    // Loading IWAD
	printf("Loading IWAD '%s'...\n", filename);
	AddIWAD(filename, wadFileBuffer, size);
    printf("Loaded IWAD '%s' correctly\n", filename);
  } 
 
  // Checking for correct header
  if (recognizedFormat == false) { fprintf(stderr, "Error with '%s': it contains an unrecognized header '%s'\n", filename, header); return 0; }

  // Return 1 for all ok
  return 1;
}

// the Doom engine doesn't have traditional memory regions because it's not an emulator
// but there's still useful data in memory that we can expose
// so we turn it into artificial memory domains, one for each entity array
// TODO: expose sectors and linedefs like xdre does (but better)
ECL_EXPORT char dsda_read_memory_array(int type, unsigned int addr)
{
  char out_of_bounts = 0xFF;
  char null_thing = 0x88;
  int padded_size = 512; // sizeof(mobj_t) is 464 but we pad for nice representation
  
  if (addr >= numthings * padded_size) return out_of_bounts;
  
  int index = addr / padded_size;
  int offset = addr % padded_size;
  mobj_t *mobj = mobj_ptrs[index];
  
  if (mobj == NULL) return null_thing;
  
  char *data = (char *)mobj + offset;  
  return *data;
}