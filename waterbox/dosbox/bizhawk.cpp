#include "bizhawk.hpp"
#include "../libco/libco.h"
#include <stdlib.h>
#include <config.h>
#include <sdlmain.h>
#include <render.h>
#include <keyboard.h>
#include <set>
#include <third_party/jaffarCommon/include/jaffarCommon/file.hpp>

extern int _main(int argc, char* argv[]);
void runMain() { _main(0, nullptr); }
extern void VGA_SetupDrawing(Bitu /*val*/);
cothread_t _emuCoroutine;
cothread_t _driverCoroutine;

#define __FPS__ 60
double ticksElapsed;
uint32_t ticksElapsedInt;
uint32_t _GetTicks() { return ticksElapsedInt; }
jaffarCommon::file::MemoryFileDirectory _memfileDirectory;

std::set<KBD_KEYS> _prevPressedKeys;
extern std::set<KBD_KEYS> _pressedKeys;
extern std::set<KBD_KEYS> _releasedKeys;

ECL_EXPORT bool Init(int argc, char **argv)
{
		// Loading entire floppy disk
		std::string fp0FileName = "FloppyDisk0";
		std::string floppyDisk0FileData;
		bool        status = jaffarCommon::file::loadStringFromFile(floppyDisk0FileData, fp0FileName);
		if (status == false) { fprintf(stderr, "Could not find/read from Floppy Disk 0 file: %s\n", fp0FileName.c_str()); return false; }

		// Uploading file into the mem file directory
		auto f = _memfileDirectory.fopen(fp0FileName, "w", floppyDisk0FileData.size());
		if (f == NULL) { fprintf(stderr, "Could not open mem file: %s\n", fp0FileName.c_str()); return false; }

		// Copying data into mem file
		auto writtenBlocks = jaffarCommon::file::MemoryFile::fwrite(floppyDisk0FileData.data(), floppyDisk0FileData.size(), 1, f);
		if (writtenBlocks != 1)  { fprintf(stderr, "Could not write data into mem file: %s\n", fp0FileName.c_str()); return false; }

		// Closing mem file
		_memfileDirectory.fclose(f);
	
	// FILE* f = fopen("FloppyDisk0", "rb");
	// 
	// if (f == NULL) return false;
	// else 
	// {
	// 	fseek(f, 0L, SEEK_END);
 //        size_t size = ftell(f);
	// 	printf("File Size: %lu\n", size);
	// 	fclose(f);
	// }

		// Setting dummy drivers for env variables
		setenv("SDL_VIDEODRIVER", "dummy", 1);
		setenv("SDL_AUDIODRIVER", "dummy", 1);

		// Setting timer
		ticksElapsed = 0.0;
		ticksElapsedInt = 0;

	_driverCoroutine = co_active();
	constexpr size_t stackSize = 4 * 1024 * 1024;
	_emuCoroutine = co_create(stackSize, runMain);
	co_switch(_emuCoroutine);

	return true;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
 // Processing inputs
	_releasedKeys.clear();
	_pressedKeys.clear();

	std::set<KBD_KEYS> newPressedKeys;
	for (size_t i = 0; i < KEY_COUNT; i++)
	{
		 auto key = (KBD_KEYS)i;
			bool wasPressed = _prevPressedKeys.find(key) != _prevPressedKeys.end();

		 if (f->Keys[i] > 0) 
			{
				if (wasPressed == false)  _pressedKeys.insert(key);
				newPressedKeys.insert(key);
			}  
			if (f->Keys[i] == 0)  if (wasPressed == true)   _releasedKeys.insert(key);
	}
	_prevPressedKeys = newPressedKeys;
 
	// Advancing timer
	constexpr double ticksPerFrame = 1000.0 / __FPS__;
	ticksElapsed += ticksPerFrame; // Miliseconds per frame
	ticksElapsedInt = (uint32_t)std::floor(ticksElapsed);
	// printf("Time Elapsed: %f / %u (delta: %f)\n", ticksElapsed,ticksElapsedInt,ticksPerFrame);

	co_switch(_emuCoroutine);

	// printf("w: %u, h: %u, bytes: %p\n", sdl.surface->w, sdl.surface->h, sdl.surface->pixels);
	f->base.Width = sdl.surface->w;
	f->base.Height = sdl.surface->h;

	// size_t checksum = 0;
	// for (size_t i = 0; i < sdl.surface->w * sdl.surface->h * 4; i++) checksum += ((uint8_t*)sdl.surface->pixels)[i];
	// printf("Video checksum: %lu\n", checksum);
	memcpy(f->base.VideoBuffer, sdl.surface->pixels, f->base.Width * f->base.Height * 4);
}

uint8_t mainRAM[256];

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data  = mainRAM;
	m[0].Name  = "Main RAM";
	m[0].Size  = 256;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

}

void (*LEDCallback)();
ECL_EXPORT void SetLEDCallback(void (*callback)())
{
	LEDCallback = callback;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
