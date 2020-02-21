#ifndef f_AT_DEBUGDISPLAY_H
#define f_AT_DEBUGDISPLAY_H

#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmaputils.h>

class IVDVideoDisplay;
class ATAnticEmulator;
class ATGTIAEmulator;
class ATMemoryManager;

class ATDebugDisplay {
	ATDebugDisplay(const ATDebugDisplay&);
	ATDebugDisplay& operator=(const ATDebugDisplay&);
public:
	ATDebugDisplay();
	~ATDebugDisplay();

	void Init(ATMemoryManager *memory, ATAnticEmulator *antic, ATGTIAEmulator *gtia, IVDVideoDisplay *display);
	void Shutdown();

	enum Mode {
		kMode_AnticHistory,
		kMode_AnticHistoryStart,
		kModeCount
	};

	enum PaletteMode {
		kPaletteMode_Registers,
		kPaletteMode_Analysis,
		kPaletteModeCount
	};

	Mode GetMode() const { return mMode; }
	void SetMode(Mode mode) { mMode = mode; }

	PaletteMode GetPaletteMode() const { return mPaletteMode; }
	void SetPaletteMode(PaletteMode mode) { mPaletteMode = mode; }

	void SetDLAddrOverride(sint32 addr) { mDLAddrOverride = addr; }
	void SetPFAddrOverride(sint32 addr) { mPFAddrOverride = addr; }

	void Update();

protected:
	ATMemoryManager *mpMemory;
	ATAnticEmulator *mpAntic;
	ATGTIAEmulator *mpGTIA;
	IVDVideoDisplay *mpDisplay;

	Mode	mMode;
	PaletteMode	mPaletteMode;
	sint32	mDLAddrOverride;
	sint32	mPFAddrOverride;

	VDPixmapBuffer mDisplayBuffer;

	uint8	mFontHi[2048];
	uint8	mFontLo[1024];
	uint32	mPalette[256];
};

#endif
