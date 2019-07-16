//
//   Copyright (C) 2007 by sinamas <sinamas at users.sourceforge.net>
//
//   This program is free software; you can redistribute it and/or modify
//   it under the terms of the GNU General Public License version 2 as
//   published by the Free Software Foundation.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License version 2 for more details.
//
//   You should have received a copy of the GNU General Public License
//   version 2 along with this program; if not, write to the
//   Free Software Foundation, Inc.,
//   51 Franklin St, Fifth Floor, Boston, MA  02110-1301, USA.
//

#ifndef GAMBATTE_H
#define GAMBATTE_H

#include "gbint.h"
#include "loadres.h"
#include <cstddef>
#include <string>
#include "newstate.h"

namespace gambatte {

enum { BG_PALETTE = 0, SP1_PALETTE = 1, SP2_PALETTE = 2 };

typedef void (*MemoryCallback)(int32_t address, int64_t cycleOffset);
typedef void (*CDCallback)(int32_t addr, int32_t addrtype, int32_t flags);

enum eCDLog_AddrType
{
	eCDLog_AddrType_ROM, eCDLog_AddrType_HRAM, eCDLog_AddrType_WRAM, eCDLog_AddrType_CartRAM,
	eCDLog_AddrType_None
};

enum eCDLog_Flags
{
	eCDLog_Flags_ExecFirst = 1,
	eCDLog_Flags_ExecOperand = 2,
	eCDLog_Flags_Data = 4,
};

class GB {
public:
	GB();
	~GB();

	enum LoadFlag {
		FORCE_DMG        = 1, /**< Treat the ROM as not having CGB support regardless of what its header advertises. */
		GBA_CGB          = 2, /**< Use GBA intial CPU register values when in CGB mode. */
		MULTICART_COMPAT = 4, /**< Use heuristics to detect and support some multicart MBCs disguised as MBC1. */
	};

	/**
	  * Load ROM image.
	  *
	  * @param romfile  Path to rom image file. Typically a .gbc, .gb, or .zip-file (if
	  *                 zip-support is compiled in).
	  * @param flags    ORed combination of LoadFlags.
	  * @return 0 on success, negative value on failure.
	  */
	LoadRes load(char const *romfiledata, unsigned romfilelength, unsigned flags);

	int loadBios(char const *biosfiledata, std::size_t size);

	/**
	  * Emulates until at least 'samples' audio samples are produced in the
	  * supplied audio buffer, or until a video frame has been drawn.
	  *
	  * There are 35112 audio (stereo) samples in a video frame.
	  * May run for up to 2064 audio samples too long.
	  *
	  * An audio sample consists of two native endian 2s complement 16-bit PCM samples,
	  * with the left sample preceding the right one. Usually casting audioBuf to
	  * int16_t* is OK. The reason for using an uint_least32_t* in the interface is to
	  * avoid implementation-defined behavior without compromising performance.
	  * libgambatte is strictly c++98, so fixed-width types are not an option (and even
	  * c99/c++11 cannot guarantee their availability).
	  *
	  * Returns early when a new video frame has finished drawing in the video buffer,
	  * such that the caller may update the video output before the frame is overwritten.
	  * The return value indicates whether a new video frame has been drawn, and the
	  * exact time (in number of samples) at which it was completed.
	  *
	  * @param videoBuf 160x144 RGB32 (native endian) video frame buffer or 0
	  * @param pitch distance in number of pixels (not bytes) from the start of one line
	  *              to the next in videoBuf.
	  * @param audioBuf buffer with space >= samples + 2064
	  * @param samples  in: number of stereo samples to produce,
	  *                out: actual number of samples produced
	  * @return sample offset in audioBuf at which the video frame was completed, or -1
	  *         if no new video frame was completed.
	  */
	std::ptrdiff_t runFor(gambatte::uint_least32_t *soundBuf, std::size_t &samples);

	void blitTo(gambatte::uint_least32_t *videoBuf, std::ptrdiff_t pitch);

	void setLayers(unsigned mask);

	/**
	  * Reset to initial state.
	  * Equivalent to reloading a ROM image, or turning a Game Boy Color off and on again.
	  */
	void reset();

	/**
	  * @param palNum 0 <= palNum < 3. One of BG_PALETTE, SP1_PALETTE and SP2_PALETTE.
	  * @param colorNum 0 <= colorNum < 4
	  */
	void setDmgPaletteColor(int palNum, int colorNum, unsigned long rgb32);

	void setCgbPalette(unsigned *lut);

	/** Sets the callback used for getting input state. */
	void setInputGetter(unsigned (*getInput)());

	void setReadCallback(MemoryCallback);
	void setWriteCallback(MemoryCallback);
	void setExecCallback(MemoryCallback);
	void setCDCallback(CDCallback);
	void setTraceCallback(void (*callback)(void *));
	void setScanlineCallback(void (*callback)(), int sl);
	void setLinkCallback(void(*callback)());

	/** Use cycle-based RTC instead of real-time. */
	void setTimeMode(bool useCycles);

	/** adjust the assumed clock speed of the CPU compared to the RTC */
	void setRtcDivisorOffset(long const rtcDivisorOffset);

	/** Returns true if the currently loaded ROM image is treated as having CGB support. */
	bool isCgb() const;

	/** Returns true if a ROM image is loaded. */
	bool isLoaded() const;

	/** Writes persistent cartridge data to disk. NOT Done implicitly on ROM close. */
	void loadSavedata(char const *data);
	int saveSavedataLength();
	void saveSavedata(char *dest);

	// 0 = vram, 1 = rom, 2 = wram, 3 = cartram, 4 = oam, 5 = hram
	bool getMemoryArea(int which, unsigned char **data, int *length);

	/** ROM header title of currently loaded ROM image. */
	std::string const romTitle() const;

	unsigned char externalRead(unsigned short addr);
	void externalWrite(unsigned short addr, unsigned char val);

	int linkStatus(int which);

	void getRegs(int *dest);

	void setInterruptAddresses(int *addrs, int numAddrs);
	int getHitInterruptAddress();

	template<bool isReader>void SyncState(NewState *ns);

private:
	struct Priv;
	Priv *const p_;

	GB(GB const &);
	GB & operator=(GB const &);
};
}

#endif
