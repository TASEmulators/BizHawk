/***************************************************************************
 *   Copyright (C) 2007 by Sindre Aamås                                    *
 *   aamas@stud.ntnu.no                                                    *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License version 2 as     *
 *   published by the Free Software Foundation.                            *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License version 2 for more details.                *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   version 2 along with this program; if not, write to the               *
 *   Free Software Foundation, Inc.,                                       *
 *   59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.             *
 ***************************************************************************/
#ifndef GAMBATTE_H
#define GAMBATTE_H

#include "gbint.h"
#include <string>
#include <sstream>
#include <cstdint>
#include "newstate.h"

namespace gambatte {
enum { BG_PALETTE = 0, SP1_PALETTE = 1, SP2_PALETTE = 2 };

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
		MULTICART_COMPAT = 4  /**< Use heuristics to detect and support some multicart MBCs disguised as MBC1. */
	};
	
	/** Load ROM image.
	  *
	  * @param romfile  Path to rom image file. Typically a .gbc, .gb, or .zip-file (if zip-support is compiled in).
	  * @param flags    ORed combination of LoadFlags.
	  * @return 0 on success, negative value on failure.
	  */
	bool use_bios;
	int load(const char *romfiledata, unsigned romfilelength, const char *biosfiledata, unsigned biosfilelength, std::uint32_t now, unsigned flags = 0);
	
	/** Emulates until at least 'samples' stereo sound samples are produced in the supplied buffer,
	  * or until a video frame has been drawn.
	  *
	  * There are 35112 stereo sound samples in a video frame.
	  * May run for up to 2064 stereo samples too long.
	  * A stereo sample consists of two native endian 2s complement 16-bit PCM samples,
	  * with the left sample preceding the right one. Usually casting soundBuf to/from
	  * short* is OK and recommended. The reason for not using a short* in the interface
	  * is to avoid implementation-defined behaviour without compromising performance.
	  *
	  * Returns early when a new video frame has finished drawing in the video buffer,
	  * such that the caller may update the video output before the frame is overwritten.
	  * The return value indicates whether a new video frame has been drawn, and the
	  * exact time (in number of samples) at which it was drawn.
	  *
	  * @param soundBuf buffer with space >= samples + 2064
	  * @param samples in: number of stereo samples to produce, out: actual number of samples produced
	  * @return sample number at which the video frame was produced. -1 means no frame was produced.
	  */
	long runFor(gambatte::uint_least32_t *soundBuf, unsigned &samples);

	void blitTo(gambatte::uint_least32_t *videoBuf, int pitch);

	void setLayers(unsigned mask);

	/** Reset to initial state.
	  * Equivalent to reloading a ROM image, or turning a Game Boy Color off and on again.
	  */
	void reset(std::uint32_t now);
	
	/** @param palNum 0 <= palNum < 3. One of BG_PALETTE, SP1_PALETTE and SP2_PALETTE.
	  * @param colorNum 0 <= colorNum < 4
	  */
	void setDmgPaletteColor(unsigned palNum, unsigned colorNum, unsigned rgb32);
	
	void setCgbPalette(unsigned *lut);

	/** Sets the callback used for getting input state. */
	void setInputGetter(unsigned (*getInput)());
	
	void setReadCallback(void (*callback)(unsigned));
	void setWriteCallback(void (*callback)(unsigned));
	void setExecCallback(void (*callback)(unsigned));
	void setCDCallback(CDCallback);
	void setTraceCallback(void (*callback)(void *));
	void setScanlineCallback(void (*callback)(), int sl);
	void setRTCCallback(std::uint32_t (*callback)());

	/** Returns true if the currently loaded ROM image is treated as having CGB support. */
	bool isCgb() const;
	
	/** Returns true if a ROM image is loaded. */
	bool isLoaded() const;

	/** Writes persistent cartridge data to disk. NOT Done implicitly on ROM close. */
	void loadSavedata(const char *data);
	int saveSavedataLength();
	void saveSavedata(char *dest);
	
	// 0 = vram, 1 = rom, 2 = wram, 3 = cartram, 4 = oam, 5 = hram
	bool getMemoryArea(int which, unsigned char **data, int *length);
	
	/** ROM header title of currently loaded ROM image. */
	const std::string romTitle() const;

	unsigned char ExternalRead(unsigned short addr);
	void ExternalWrite(unsigned short addr, unsigned char val);

	int LinkStatus(int which);

	void GetRegs(int *dest);

	template<bool isReader>void SyncState(NewState *ns);

private:
	struct Priv;
	Priv *const p_;

	GB(const GB &);
	GB & operator=(const GB &);
};
}

#endif
