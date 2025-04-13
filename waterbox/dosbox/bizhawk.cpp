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
#include <vga.h>
#include <mem.h>

// DOSBox functions
extern int _main(int argc, char* argv[]);
void runMain() { _main(0, nullptr); }
extern void VGA_SetupDrawing(Bitu /*val*/);
extern void swapInDrive(int drive, unsigned int position);

// Coroutines: they allow us to jump in and out the dosbox core
cothread_t _emuCoroutine;
cothread_t _driverCoroutine;

// Timing-related stuff
double ticksTarget;
double ticksPerFrame;
uint32_t ticksElapsed;
uint32_t _GetTicks() { return ticksElapsed; }
void _Delay(uint32_t ticks) { ticksElapsed += ticks; 	co_switch(_driverCoroutine); }

// VGA refresh rate information
uint64_t _vgaRefreshRateNumerator;
uint64_t _vgaRefreshRateDenominator;

// Memory file directory
jaffarCommon::file::MemoryFileDirectory _memFileDirectory;

// Audio stuff
std::vector<int16_t> _audioSamples;

// Keyboard related variables
std::set<KBD_KEYS> _prevPressedKeys;
extern std::set<KBD_KEYS> _pressedKeys;
extern std::set<KBD_KEYS> _releasedKeys;

// mouse related variables
extern int mickey_threshold;
extern bool user_cursor_locked;
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

// Drive activity monitoring
bool _driveUsed = false;
ECL_EXPORT bool getDriveActivityFlag() { return _driveUsed; }

// SRAM Management
bool _preserveHardDiskContents;
constexpr char writableHDDSrcFile[] = "__WritableHardDiskDrive";
constexpr char writableHDDDstFile[] = "__WritableHardDiskDrive.img";
int get_sram_size() { return (int)_memFileDirectory.getFileSize(writableHDDDstFile); }
uint8_t* get_sram_buffer() { return _memFileDirectory.getFileBuffer(writableHDDDstFile); }

ECL_EXPORT bool Init(InitSettings* settings)
{
	// If size is non-negative, we need to load the writable hard disk into memory
	if (settings->writableHDDImageFileSize == 0) printf("No writable hard disk drive selected.");
	else	{
		// Loading HDD file into mem file directory
		printf("Creating hard disk drive mem file '%s' -> '%s' (%lu bytes)\n", writableHDDSrcFile, writableHDDDstFile, settings->writableHDDImageFileSize);
		auto result = loadFileIntoMemoryFileDirectory(writableHDDSrcFile, writableHDDDstFile, settings->writableHDDImageFileSize);
		if (result == false || _memFileDirectory.contains(writableHDDDstFile) == false) 
		{
			fprintf(stderr, "Could not create hard disk drive mem file\n");
			return false; 
		}
	}

  	// Storing hard disk preservation option
	_preserveHardDiskContents = settings->preserveHardDiskContents == 1;

	// Setting dummy drivers for env variables
	setenv("SDL_VIDEODRIVER", "dummy", 1);
	setenv("SDL_AUDIODRIVER", "dummy", 1);

	// Setting timer
	double fps = (double)settings->fpsNumerator / (double)settings->fpsDenominator;
	ticksPerFrame = 1000.0 / fps;
	ticksTarget = 0.0;
	ticksElapsed = 0;

	// Setting VGA refresh rate initial value, just in case
	_vgaRefreshRateNumerator = settings->fpsNumerator;
    _vgaRefreshRateDenominator = settings->fpsDenominator;

	printf("Starting DOSBox-x Coroutine...\n");
	_driverCoroutine = co_active();
	constexpr size_t stackSize = 4 * 1024 * 1024;
	_emuCoroutine = co_create(stackSize, runMain);
	co_switch(_emuCoroutine);

	// Initializing joysticks
	stick[0].enabled = settings->joystick1Enabled == 1;
	stick[1].enabled = settings->joystick2Enabled  == 1;

	stick[0].xpos = 0.0;
	stick[0].ypos = 0.0;
	stick[0].button[0] = false;
	stick[0].button[1] = false;

	stick[1].xpos = 0.0;
	stick[1].ypos = 0.0;
	stick[1].button[0] = false;
	stick[1].button[1] = false;

 	// Initializing mouse
	user_cursor_locked = true;

	return true;
}

// A callback function to update the output render, only when requested by the core
// Doing it on demand avoids screen tearing
uint32_t* _videoBuffer = nullptr;
size_t _videoBufferSize = 0;
int _videoWidth = 0;
int _videoHeight = 0;
void doRenderUpdateCallback()
{
	// printf("w: %u, h: %u, bytes: %p\n", sdl.surface->w, sdl.surface->h, sdl.surface->pixels);
	bool allocateBuffer = false;
	if (sdl.surface->w != _videoWidth) { allocateBuffer = true; _videoWidth = sdl.surface->w; }
	if (sdl.surface->h != _videoHeight) { allocateBuffer = true; _videoHeight = sdl.surface->h; }

	_videoBufferSize = _videoWidth * _videoHeight * sizeof(uint32_t);
	if (allocateBuffer == true)
	{
		if (_videoBuffer != nullptr) free(_videoBuffer);
		_videoBuffer = (uint32_t*) malloc(_videoBufferSize);
	}

	// Updating buffer
	memcpy(_videoBuffer, sdl.surface->pixels, _videoBufferSize);
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	// Clearing drive use flag
	_driveUsed = false;

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
	if (f->mouse.speedX != 0 || f->mouse.speedY != 0)
	{
		mouse.x = (double)mouse.min_x + ((double) f->mouse.posX / (double)MOUSE_MAX_X) * (double)mouse.max_x;
		mouse.y = (double)mouse.min_y + ((double) f->mouse.posY / (double)MOUSE_MAX_Y) * (double)mouse.max_y;

 		float adjustedDeltaX = (float) f->mouse.speedX * (float) f->mouse.sensitivity;
		float adjustedDeltaY = (float) f->mouse.speedY * (float) f->mouse.sensitivity;

		float dx = adjustedDeltaX * mouse.pixelPerMickey_x;
		float dy = adjustedDeltaY * mouse.pixelPerMickey_y;

		mouse.mickey_x = adjustedDeltaX * mouse.mickeysPerPixel_x ;
		mouse.mickey_y = adjustedDeltaY * mouse.mickeysPerPixel_y ;

		mouse.mickey_accum_x += (dx * mouse.mickeysPerPixel_x);
		mouse.mickey_accum_y += (dy * mouse.mickeysPerPixel_y);

		mouse.ps2x += adjustedDeltaX;
		mouse.ps2y += adjustedDeltaY;
		if (mouse.ps2x >= 32768.0)       mouse.ps2x -= 65536.0;
		else if (mouse.ps2x <= -32769.0) mouse.ps2x += 65536.0;
		if (mouse.ps2y >= 32768.0)       mouse.ps2y -= 65536.0;
		else if (mouse.ps2y <= -32769.0) mouse.ps2y += 65536.0;

		// printf("X: %d (%d) Y: %d (%d)\n", f->mouse.posX, f->mouse.speedX, f->mouse.posY, f->mouse.speedY);
		// printf("%d %f %d %f\n", mouse.mickey_x, mouse.mickey_accum_x, mouse.mickey_y, mouse.mickey_accum_y);

		Mouse_AddEvent(MOUSE_HAS_MOVED);
	}

 	if (f->mouse.leftButtonPressed) Mouse_ButtonPressed(0); 
	if (f->mouse.middleButtonPressed) Mouse_ButtonPressed(2);
	if (f->mouse.rightButtonPressed) Mouse_ButtonPressed(1);

	if (f->mouse.leftButtonReleased) Mouse_ButtonReleased(0); 
	if (f->mouse.middleButtonReleased) Mouse_ButtonReleased(2);
	if (f->mouse.rightButtonReleased) Mouse_ButtonReleased(1);

 	// Clearing audio sample buffer
	_audioSamples.clear();

 	// Increasing ticks target
	ticksTarget += ticksPerFrame;
	
	// Advancing until the required tick target is met
	while (ticksElapsed < (int)ticksTarget)	co_switch(_emuCoroutine);

	// Updating video output
	// printf("w: %u, h: %u, bytes: %p\n", sdl.surface->w, sdl.surface->h, sdl.surface->pixels);
	f->base.Width = sdl.surface->w;
	f->base.Height = sdl.surface->h;

	// size_t checksum = 0;
	// for (size_t i = 0; i < sdl.surface->w * sdl.surface->h * 4; i++) checksum += ((uint8_t*)sdl.surface->pixels)[i];
	// printf("Video checksum: %lu\n", checksum);
	if (_videoBuffer != nullptr) memcpy(f->base.VideoBuffer, _videoBuffer, _videoBufferSize);

	// Checking audio sample count
	// size_t checksum = 0;
	// for (size_t i = 0; i < _audioSamples.size(); i++) checksum += _audioSamples[i];
	// printf("Audio samples: %lu - Checksum: %lu\n", _audioSamples.size(), checksum);

	// Setting audio buffer
	memcpy(f->base.SoundBuffer, _audioSamples.data(), _audioSamples.size() * sizeof(int16_t));
	f->base.Samples  = _audioSamples.size() / 2;
}

ECL_EXPORT uint64_t getVGARefreshRateNumerator() { return _vgaRefreshRateNumerator; }
ECL_EXPORT uint64_t getVGARefreshRateDenominator() { return _vgaRefreshRateDenominator; }

#define DOS_CONVENTIONAL_MEMORY_SIZE (640 * 1024)
#define DOS_UPPER_MEMORY_SIZE (384 * 1024)
#define DOS_LOWER_MEMORY_SIZE (DOS_CONVENTIONAL_MEMORY_SIZE + DOS_UPPER_MEMORY_SIZE)

/// CD Management Logic Start
void (*cd_read_callback)(char* cdRomFile, int32_t lba, void * dest, int sectorSize);
ECL_EXPORT void SetCdCallbacks(void (*cdrc)(char* cdRomFile, int32_t lba, void * dest, int sectorSize))
{
	cd_read_callback = cdrc;
}

CDData_t _cdData[MAX_CD_COUNT];
size_t _cdCount = 0;
ECL_EXPORT void pushCDData(int cdIdx, int numSectors, int numTracks)
{
	_cdCount++;
	_cdData[cdIdx].numSectors = numSectors;
	_cdData[cdIdx].numTracks = numTracks;
	printf("Pushing CD %d. NumSectors: %d, NumTracks: %d\n", cdIdx, _cdData[cdIdx].numSectors, _cdData[cdIdx].numTracks);
}

ECL_EXPORT void pushTrackData(int cdIdx, int trackId, CDTrack_t* data)
{
	_cdData[cdIdx].tracks[trackId] = *data;
	printf("  + CD: %d Track %d - Offset: %d - Start: %d - End: %d - Mode: %d - loopEnabled: %d - loopOffset: %d\n", cdIdx, trackId,
	_cdData[cdIdx].tracks[trackId].offset,
	_cdData[cdIdx].tracks[trackId].start,
	_cdData[cdIdx].tracks[trackId].end,
	_cdData[cdIdx].tracks[trackId].mode,
	_cdData[cdIdx].tracks[trackId].loopEnabled,
	_cdData[cdIdx].tracks[trackId].loopOffset);
}

/// CD Management Logic End

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int memAreaIdx = 0;

	m[memAreaIdx].Data  = MemBase;
	m[memAreaIdx].Name  = "Conventional Memory";
	m[memAreaIdx].Size  = DOS_CONVENTIONAL_MEMORY_SIZE;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;
	memAreaIdx++;

	m[memAreaIdx].Data  = &MemBase[DOS_CONVENTIONAL_MEMORY_SIZE];
	m[memAreaIdx].Name  = "Upper Memory Area";
	m[memAreaIdx].Size  = DOS_UPPER_MEMORY_SIZE;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
	memAreaIdx++;

	int highMemSize = MemSize - DOS_LOWER_MEMORY_SIZE;
	if (highMemSize > 0)
	{
		m[memAreaIdx].Data  = &MemBase[DOS_LOWER_MEMORY_SIZE];
		m[memAreaIdx].Name  = "Extended Memory";
		m[memAreaIdx].Size  = MemSize - DOS_LOWER_MEMORY_SIZE;
		m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
		memAreaIdx++;
	}

	m[memAreaIdx].Data  = MemBase;
	m[memAreaIdx].Name  = "Physical RAM";
	m[memAreaIdx].Size  = MemSize;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
	memAreaIdx++;

	m[memAreaIdx].Data  = vga.mem.linear;
	m[memAreaIdx].Name  = "Video RAM";
	m[memAreaIdx].Size  = vga.mem.memsize;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
	memAreaIdx++;
	
	size_t sramSize = get_sram_size();
	if (sramSize > 0)
	{
		m[memAreaIdx].Data  = get_sram_buffer();
		m[memAreaIdx].Name  = "Writeable HDD (SaveRAM)";
		m[memAreaIdx].Size  = sramSize;
		m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
		m[memAreaIdx].Flags |= _preserveHardDiskContents ? MEMORYAREA_FLAGS_SAVERAMMABLE : 0;
		memAreaIdx++;
	}
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
