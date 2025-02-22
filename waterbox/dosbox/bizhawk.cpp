#include "bizhawk.hpp"
#include "../libco/libco.h"
#include <stdlib.h>
#include <config.h>
#include <sdlmain.h>
#include <render.h>
#include <keyboard.h>
#include <set>
#include <jaffarCommon/file.hpp>
#include <mixer.h>
#include <joystick.h>
#include <mouse.h>

extern int _main(int argc, char* argv[]);
void runMain() { _main(0, nullptr); }
extern void VGA_SetupDrawing(Bitu /*val*/);
extern void swapInDrive(int drive, unsigned int position);
cothread_t _emuCoroutine;
cothread_t _driverCoroutine;

#define __FPS__ 59.8260993957519531
double ticksTarget;
constexpr double ticksPerFrame = 1000.0 / __FPS__;
uint32_t ticksElapsed;
uint32_t _GetTicks() { return ticksElapsed; }
void _Delay(uint32_t ticks) { ticksElapsed += ticks; 	co_switch(_driverCoroutine); }

jaffarCommon::file::MemoryFileDirectory _memFileDirectory;
std::set<KBD_KEYS> _prevPressedKeys;
extern std::set<KBD_KEYS> _pressedKeys;
extern std::set<KBD_KEYS> _releasedKeys;
std::vector<int16_t> _audioSamples;

MouseInput _prevMouse;
#define MOUSE_MAX_X 800
#define MOUSE_MAX_Y 600

bool loadFileIntoMemoryFileDirectory(const std::string& srcFile, const std::string& dstFile, const ssize_t dstSize = -1)
{
		// Loading entire source file
		std::string srcFileData;
		bool        status = jaffarCommon::file::loadStringFromFile(srcFileData, srcFile);
		if (status == false) { fprintf(stderr, "Could not find/read from file: %s\n", srcFile.c_str()); return false; }

		// Uploading file into the mem file directory
		auto f = _memFileDirectory.fopen(dstFile, "w");
		if (f == NULL) { fprintf(stderr, "Could not open mem file for write: %s\n", dstFile.c_str()); return false; }

		// Copying data into mem file
		auto writtenBlocks = jaffarCommon::file::MemoryFile::fwrite(srcFileData.data(), 1, srcFileData.size(), f);
		if (writtenBlocks != srcFileData.size()) 
		{ 
			fprintf(stderr, "Could not write data into mem file: %s\n", dstFile.c_str());
			_memFileDirectory.fclose(f);
		 return false; 
		}

		// If required, resize dst file
		if (dstSize >= 0)
		{
			 auto ret = f->resize(dstSize);
				if (ret < 0) 
				{
					fprintf(stderr, "Could not resize mem file: %s\n", dstFile.c_str());
					_memFileDirectory.fclose(f);
					return false; 
				}
		} 

		// Closing mem file
		_memFileDirectory.fclose(f);

		return true;
}

ECL_EXPORT bool Init(bool joystick1Enabled, bool joystick2Enabled, bool mouseEnabled, uint64_t writableHDDImageFileSize)
{
	 // If size is non-negative, we need to load the writable hard disk into memory
		if (writableHDDImageFileSize == 0) printf("No writable hard disk drive selected.");
		else	{
			// Loading HDD file into mem file directory
			std::string writableHDDSrcFile = "__WritableHardDiskDrive";
			std::string writableHDDDstFile = "__WritableHardDiskDrive.img";
			printf("Creating hard disk drive mem file '%s' -> '%s' (%lu bytes)\n", writableHDDSrcFile.c_str(), writableHDDDstFile.c_str(), writableHDDImageFileSize);
			auto result = loadFileIntoMemoryFileDirectory(writableHDDSrcFile, writableHDDDstFile, writableHDDImageFileSize);
			if (result == false || _memFileDirectory.contains(writableHDDDstFile) == false) 
			{
				fprintf(stderr, "Could not create hard disk drive mem file\n");
				return false; 
			}
		}

		// Setting dummy drivers for env variables
		setenv("SDL_VIDEODRIVER", "dummy", 1);
		setenv("SDL_AUDIODRIVER", "dummy", 1);

		// Setting timer
		ticksTarget = 0.0;
		ticksElapsed = 0;

	printf("Starting DOSBox-x Coroutine...\n");
	_driverCoroutine = co_active();
	constexpr size_t stackSize = 4 * 1024 * 1024;
	_emuCoroutine = co_create(stackSize, runMain);
	co_switch(_emuCoroutine);

 // Initializing joysticks
 stick[0].enabled = joystick1Enabled;
	stick[1].enabled = joystick2Enabled;

	stick[0].xpos = 0.0;
	stick[0].ypos = 0.0;
	stick[0].button[0] = false;
	stick[0].button[1] = false;

	stick[1].xpos = 0.0;
	stick[1].ypos = 0.0;
	stick[1].button[0] = false;
	stick[1].button[1] = false;

 // Initializing mouse
	_prevMouse.posX = 0;
	_prevMouse.posY = 0;
	_prevMouse.leftButton = 0;
	_prevMouse.middleButton = 0;
	_prevMouse.rightButton = 0;

	return true;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
 // Processing keyboard inputs
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
 
	// Processing drive swapping
	if (f->driveActions.insertFloppyDisk >= 0)
	{
		printf("Swapping to Floppy Disk: %d\n", f->driveActions.insertFloppyDisk);
		swapInDrive(0, f->driveActions.insertFloppyDisk + 1); // 0 is A:
	}
	
	if (f->driveActions.insertCDROM >= 0)
	{
		printf("Swapping to CDROM: %d\n", f->driveActions.insertCDROM);
		swapInDrive(3, f->driveActions.insertFloppyDisk + 1); // 3 is D:
	}

 // Processing joystick inputs
	if (stick[0].enabled)
	{
		stick[0].xpos = 0.0;
		stick[0].ypos = 0.0;
		if (f->joy1.up)    stick[0].ypos = -1.0f;
		if (f->joy1.down)  stick[0].ypos = 1.0f;
		if (f->joy1.left)  stick[0].xpos = -1.0f;
		if (f->joy1.right) stick[0].xpos = 1.0f;
		stick[0].button[0] = f->joy1.button1;
		stick[0].button[1] = f->joy1.button2;
	}

	if (stick[1].enabled)
	{
		stick[1].xpos = 0.0;
		stick[1].ypos = 0.0;
		if (f->joy2.up)    stick[1].ypos = -1.0f;
		if (f->joy2.down)  stick[1].ypos = 1.0f;
		if (f->joy2.left)  stick[1].xpos = -1.0f;
		if (f->joy2.right) stick[1].xpos = 1.0f;
		stick[1].button[0] = f->joy2.button1;
		stick[1].button[1] = f->joy2.button2;
	}

	// Processing mouse inputs
	if (f->mouse.posX != _prevMouse.posX || f->mouse.posY != _prevMouse.posY)
	{
		mouse.x = (double)mouse.min_x + ((double) f->mouse.posX / (double)MOUSE_MAX_X) * (double)mouse.max_x;
		mouse.y = (double)mouse.min_y + ((double) f->mouse.posY / (double)MOUSE_MAX_Y) * (double)mouse.max_y;
		Mouse_AddEvent(MOUSE_HAS_MOVED);
	}

	_prevMouse.posX = f->mouse.posX;
	_prevMouse.posY = f->mouse.posY;

	if (_prevMouse.leftButton == 0 && f->mouse.leftButton == 1)	{ Mouse_ButtonPressed(0); _prevMouse.leftButton = 1; }
	if (_prevMouse.middleButton == 0 && f->mouse.middleButton == 1)	{ Mouse_ButtonPressed(2); _prevMouse.middleButton = 1; }
	if (_prevMouse.rightButton == 0 && f->mouse.rightButton == 1)	{ Mouse_ButtonPressed(1);  _prevMouse.rightButton = 1; }

	if (_prevMouse.leftButton == 1 && f->mouse.leftButton == 0)	{ Mouse_ButtonReleased(0); _prevMouse.leftButton = 0; }
	if (_prevMouse.middleButton == 1 && f->mouse.middleButton == 0)	{ Mouse_ButtonReleased(2); _prevMouse.middleButton = 0; }
	if (_prevMouse.rightButton == 1 && f->mouse.rightButton == 0)	{ Mouse_ButtonReleased(1); _prevMouse.rightButton = 0; }

 // Clearing audio sample buffer
		_audioSamples.clear();

 // Increasing ticks target
	ticksTarget += ticksPerFrame;
	
	// Advancing until the required tick target is met
	while (ticksElapsed < (int)ticksTarget)
	{
		// Advance frame 1ms at a time for correct internal timing
		ticksElapsed += 1;

		// Jumping back into dosbox
		co_switch(_emuCoroutine);
	}

	// Checking audio sample count
	// size_t checksum = 0;
	// for (size_t i = 0; i < _audioSamples.size(); i++) checksum += _audioSamples[i];
	// printf("Audio samples: %lu - Checksum: %lu\n", _audioSamples.size(), checksum);

	// printf("w: %u, h: %u, bytes: %p\n", sdl.surface->w, sdl.surface->h, sdl.surface->pixels);
	f->base.Width = sdl.surface->w;
	f->base.Height = sdl.surface->h;

	// size_t checksum = 0;
	// for (size_t i = 0; i < sdl.surface->w * sdl.surface->h * 4; i++) checksum += ((uint8_t*)sdl.surface->pixels)[i];
	// printf("Video checksum: %lu\n", checksum);
	memcpy(f->base.VideoBuffer, sdl.surface->pixels, f->base.Width * f->base.Height * 4);

	// Setting audio buffer
	memcpy(f->base.SoundBuffer, _audioSamples.data(), _audioSamples.size() * sizeof(int16_t));
	f->base.Samples  = _audioSamples.size() / 2;
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
