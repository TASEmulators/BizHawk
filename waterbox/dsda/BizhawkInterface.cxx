#include <vector>
#include <string>
#include <cstdio>
#include <cstdint>
#include "BizhawkInterface.hxx"
#include <d_player.h>
#include <w_wad.h>

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
  
  void D_AddFile (const char *file, wad_source_t source, void* const buffer, const size_t size);
  void AddIWAD(const char *iwad, void* const buffer, const size_t size); 
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

struct InitSettings
{
	uint32_t dummy;
};

ECL_EXPORT void dsda_get_audio(int *n, void **buffer)
{
	*n = 0;
	*buffer = nullptr;
}

ECL_EXPORT void dsda_get_video(int& w, int& h, int& pitch, uint8_t*& buffer, int& paletteSize, uint32_t*& paletteBuffer)
{
	buffer = (uint8_t*)headlessGetVideoBuffer();
	w = headlessGetVideoWidth();
	h = headlessGetVideoHeight();
	pitch = headlessGetVideoPitch();
	paletteSize = 256;
	paletteBuffer = headlessGetPallette();
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

bool foundIWAD = false;

ECL_EXPORT int dsda_init(struct InitSettings *settings)
{
  return 1;
}


ECL_EXPORT int dsda_add_wad_file(const char *filename, const int size, ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize))
{
  printf("Loading WAD '%s' of size %d...\n", filename, size);
  auto wadFileBuffer = (unsigned char*) malloc(size);

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
	AddIWAD(filename, wadFileBuffer, size);
    printf("Loaded IWAD '%s' correctly\n", filename);
  } 
 
  // Checking for correct header
  if (recognizedFormat == false) { fprintf(stderr, "Error with '%s': it contains an unrecognized header '%s'\n", filename, header); return 0; }

  // Return 1 for all ok
  return 1;
}