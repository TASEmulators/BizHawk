//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

#include <stdafx.h>
#include <vd2/VDDisplay/display.h>
#include "debugdisplay.h"
#include "antic.h"
#include "gtia.h"
#include "memorymanager.h"

ATDebugDisplay::ATDebugDisplay()
	: mpMemory(NULL)
	, mpAntic(NULL)
	, mpGTIA(NULL)
	, mpDisplay(NULL)
	, mMode(kMode_AnticHistory)
	, mPaletteMode(kPaletteMode_Registers)
	, mDLAddrOverride(-1)
	, mPFAddrOverride(-1)

{
}

ATDebugDisplay::~ATDebugDisplay() {
}

void ATDebugDisplay::Init(ATMemoryManager *memory, ATAnticEmulator *antic, ATGTIAEmulator *gtia, IVDVideoDisplay *display) {
	mpMemory = memory;
	mpAntic = antic;
	mpDisplay = display;
	mpGTIA = gtia;

	VDPixmapLayout layout = {0};

	// The wide display covers $20-$DF (192 color clocks), but only $22-DD is visible
	// in the display. We allocate a 208 color clock wide bitmap to allow room for
	// horizontal scrolling emulation.
	layout.data = 4;
	layout.w = 376;
	layout.h = 240;
	layout.pitch = 416;
	layout.format = nsVDPixmap::kPixFormat_Pal8;
	layout.palette = 0;


	mDisplayBuffer.init(layout);
	mDisplayBuffer.palette = mPalette;
}

void ATDebugDisplay::Shutdown() {
	if (mpDisplay)
		mpDisplay->Reset();

	mpMemory = NULL;
	mpAntic = NULL;
	mpGTIA = NULL;
	mpDisplay = NULL;

	mDisplayBuffer.clear();
}

void ATDebugDisplay::Update() {
	ATGTIARegisterState gtiaState;
	ATAnticRegisterState anticState;

	mpAntic->GetRegisterState(anticState);
	mpGTIA->GetRegisterState(gtiaState);

	const ATAnticEmulator::DLHistoryEntry *const history = mpAntic->GetDLHistory() + 8;

	bool prevvscroll = false;
	int fonthibase = -1;
	int fontlobase = -1;

	uint8 pfbak;
	uint8 pf0;
	uint8 pf1;
	uint8 pf2;
	uint8 pf3;
	uint8 pm0;
	uint8 pm1;
	uint8 pm2;
	uint8 pm3;

	if (mPaletteMode == kPaletteMode_Registers) {
		pfbak = gtiaState.mReg[0x1A];
		pf0 = gtiaState.mReg[0x16];
		pf1 = gtiaState.mReg[0x17];
		pf2 = gtiaState.mReg[0x18];
		pf3 = gtiaState.mReg[0x19];
		pm0 = gtiaState.mReg[0x12];
		pm1 = gtiaState.mReg[0x13];
		pm2 = gtiaState.mReg[0x14];
		pm3 = gtiaState.mReg[0x15];
	} else {
		pfbak = 0x00;
		pf0 = 0x04;
		pf1 = 0x08;
		pf2 = 0x0C;
		pf3 = 0x0F;
		pm0 = 0x18;
		pm1 = 0x58;
		pm2 = 0x78;
		pm3 = 0xB8;
	}

	memset(mDisplayBuffer.base(), pfbak, mDisplayBuffer.size());

	ATAnticEmulator::DLHistoryEntry hval = history[0];

	if (mDLAddrOverride >= 0)
		hval.mDLAddress = (uint16)mDLAddrOverride;

	hval.mDMACTL = anticState.mDMACTL;
	hval.mCHBASE = anticState.mCHBASE >> 1;

	if (mPFAddrOverride >= 0)
		hval.mPFAddress = (uint16)mPFAddrOverride;

	uint32 dlbase = hval.mDLAddress & 0xfc00;
	uint32 dloffset = hval.mDLAddress;

	for(int y=0; y<240; ) {
		if (mMode == kMode_AnticHistory) {
			const ATAnticEmulator::DLHistoryEntry& hvalsrc = history[y];

			if (!hvalsrc.mbValid) {
				++y;
				continue;
			}
			
			uint16 pfaddr = hval.mPFAddress;
			hval = hvalsrc;

			if (mPFAddrOverride >= 0)
				hval.mPFAddress = pfaddr;
		} else {
			hval.mDLAddress = dlbase + (dloffset & 0x3ff);
			hval.mControl = mpMemory->DebugAnticReadByte(dlbase + (dloffset++ & 0x3ff));

			uint8 cmode = hval.mControl & 0x0f;
			if (cmode) {
				if (cmode == 1 || (hval.mControl & 0x40)) {
					uint8 lo = mpMemory->DebugAnticReadByte(dlbase + (dloffset++ & 0x3ff));
					uint8 hi = mpMemory->DebugAnticReadByte(dlbase + (dloffset++ & 0x3ff));

					uint16 addr = (uint16)(lo + ((uint32)hi << 8));

					if (cmode == 1)
						hval.mDLAddress = addr;
					else if (mPFAddrOverride < 0)
						hval.mPFAddress = addr;
				}
			}
		}

		const uint8 mode = hval.mControl & 15;

		// check for jump
		if (mode == 1) {
			if (hval.mControl & 0x40)
				break;

			++y;
			continue;
		}

		// check if vertical scrolling is enabled
		const bool vscroll = mode >= 2 && (hval.mControl & 0x20);

		// compute initial starting and ending rows
		static const uint8 kModeEndTable[14]={
			7,9,7,15,7,15,7,3,3,1,0,1,0,0
		};

		uint8 row = 0;
		uint8 rowlast = mode ? kModeEndTable[mode - 2] : (hval.mControl >> 4) & 7;

		if (vscroll != prevvscroll) {
			if (vscroll)
				row = hval.mHVScroll >> 4;
			else
				rowlast = hval.mHVScroll >> 4;

			prevvscroll = vscroll;
		}

		// skip out now for blank lines
		const uint8 displayWidth = hval.mDMACTL & 3;

		if (mode == 0 || displayWidth == 0) {
			y += ((rowlast - row) & 15) + 1;
			continue;
		}

		// check for horizontal scrolling and compute effective widths
		const bool hscroll = (hval.mControl & 0x10) != 0;

		uint8 fetchWidth = displayWidth;

		if (hscroll) {
			static const uint8 kHScrollFetchTable[4] = {0,2,3,3};

			fetchWidth = kHScrollFetchTable[fetchWidth];
		}

		// compute number of bytes to fetch
		static const uint8 kFetchModeTable[14]={
			2,2,2,2,1,1,0,0,1,1,1,2,2,2
		};

		const int fetchmode = kFetchModeTable[mode - 2];
		const int rowbytes = (6 + (fetchWidth << 1)) << fetchmode;
		uint16 pfad = hval.mPFAddress;

		hval.mPFAddress = pfad + rowbytes;

		// fetch bytes
		uint8 rowbuffer[48];

		uint32 overflow = (pfad & 0xfff) + rowbytes;
		if (overflow >= 0x1000) {
			// Playfield address would wrap -- do split read
			overflow -= 0x1000;
			mpMemory->DebugAnticReadMemory(rowbuffer, pfad, rowbytes - overflow);
			mpMemory->DebugAnticReadMemory(rowbuffer + (rowbytes - overflow), pfad & 0xf000, overflow);
		} else {
			// Unwrapped case
			mpMemory->DebugAnticReadMemory(rowbuffer, pfad, rowbytes);
		}

		// mask off bytes that go beyond the DMA stop
		static const uint8 kFetchStartTable[2][3]={
			{ 28, 20, 12 }, { 26, 18, 10 }
		};

		int fetchstart = kFetchStartTable[mode < 8][fetchWidth - 1];
		int fetchlimit = (106 - fetchstart) >> (3 - fetchmode);

		if (rowbytes > fetchlimit)
			memset(rowbuffer + fetchlimit, 0, rowbytes - fetchlimit);

		// read in the font if we need it
		if (mode < 6) {
			int hibase = (hval.mCHBASE << 1) & 0xfc;

			if (fonthibase != hibase) {
				fonthibase = hibase;
				mpMemory->DebugAnticReadMemory(mFontHi, (uint16)hibase << 8, sizeof mFontHi);
			}
		} else if (mode < 8) {
			int lobase = hval.mCHBASE << 1;

			if (fontlobase != lobase) {
				fontlobase = lobase;
				mpMemory->DebugAnticReadMemory(mFontLo, (uint16)lobase << 8, sizeof mFontLo);
			}
		}

		// render out rows
		const int fontRowInv = (anticState.mCHACTL & 4) ? 0x07 : 0x00;
		const uint8 charBlankMask = (anticState.mCHACTL & 1) ? 0x00 : 0xFF;
		const uint8 charInvMask = (anticState.mCHACTL & 2) ? 0xFF : 0x00;

		do {
			uint8 *dst0 = (uint8 *)mDisplayBuffer.data + mDisplayBuffer.pitch * y;
			uint8 *dst = dst0;

			// offset left of display by fetch width (NOT display width!)
			dst += 96 - 32 * fetchWidth;

			if (hscroll)
				dst += 2 * (hval.mHVScroll & 15);

			switch(mode) {
				case 2:
					{
						const uint8 *fontptr = mFontHi + ((row & 7) ^ fontRowInv);

						const uint8 back = pf2;
						const uint8 fore = (back & 0xf0) + (pf1 & 0x0e);
						for(int i = 0; i < rowbytes; ++i) {
							uint8 c = fontptr[(uint32)(rowbuffer[i] & 0x7f) << 3];

							if (rowbuffer[i] & 0x80) {
								c &= charBlankMask;
								c ^= charInvMask;
							}

							dst[0] = (c & 0x80) ? fore : back;
							dst[1] = (c & 0x40) ? fore : back;
							dst[2] = (c & 0x20) ? fore : back;
							dst[3] = (c & 0x10) ? fore : back;
							dst[4] = (c & 0x08) ? fore : back;
							dst[5] = (c & 0x04) ? fore : back;
							dst[6] = (c & 0x02) ? fore : back;
							dst[7] = (c & 0x01) ? fore : back;
							dst += 8;
						}
					}
					break;

				case 3:
					{
						int fontRow = row & 7;
						const uint8 *fontptr = mFontHi + (fontRow ^ fontRowInv);

						const uint8 back = pf2;
						const uint8 fore = (back & 0xf0) + (pf1 & 0x0e);

						uint8 mask[4];
						mask[0] = mask[1] = mask[2] = row < 8;
						mask[3] = ((row - 2) & 15) < 8;

						for(int i = 0; i < rowbytes; ++i) {
							uint8 c = rowbuffer[i];
							uint8 d = fontptr[(uint32)(c & 0x7f) << 3] & mask[(c >> 5) & 3];

							if (c & 0x80) {
								d &= charBlankMask;
								d ^= charInvMask;
							}

							dst[0] = (d & 0x80) ? fore : back;
							dst[1] = (d & 0x40) ? fore : back;
							dst[2] = (d & 0x20) ? fore : back;
							dst[3] = (d & 0x10) ? fore : back;
							dst[4] = (d & 0x08) ? fore : back;
							dst[5] = (d & 0x04) ? fore : back;
							dst[6] = (d & 0x02) ? fore : back;
							dst[7] = (d & 0x01) ? fore : back;
							dst += 8;
						}
					}
					break;

				case 4:
				case 5:
					{
						int fontRow = row;

						if (mode == 5)
							fontRow >>= 1;

						const uint8 *fontptr = mFontHi + ((fontRow & 7) ^ fontRowInv);

						const uint8 palA[4] = {
							pfbak,
							pf0,
							pf1,
							pf2
						};

						const uint8 palB[4] = {
							pfbak,
							pf0,
							pf1,
							pf3
						};

						for(int i=0; i<rowbytes; ++i) {
							const uint8 c = rowbuffer[i];
							const uint8 d = fontptr[(int)(c & 0x7f) << 3];

							const uint8 *pal = (c & 0x80) ? palB : palA;

							dst[0] = dst[1] = pal[(d >> 6) & 3];
							dst[2] = dst[3] = pal[(d >> 4) & 3];
							dst[4] = dst[5] = pal[(d >> 2) & 3];
							dst[6] = dst[7] = pal[(d >> 0) & 3];
							dst += 8;
						}
					}
					break;

				case 6:
				case 7:
					{
						int fontRow = row;
						
						if (mode == 7)
							fontRow >>= 1;

						const uint8 *fontptr = mFontLo + ((fontRow & 7) ^ fontRowInv);

						const uint8 pal[4] = {
							pf0,
							pf1,
							pf2,
							pf3
						};

						const uint8 back = pfbak;

						for(int i = 0; i < rowbytes; ++i) {
							const uint8 c = fontptr[(uint32)(rowbuffer[i] & 0x3f) << 3];
							const uint8 fore = pal[rowbuffer[i] >> 6];

							dst[ 0] = dst[ 1] = (c & 0x80) ? fore : back;
							dst[ 2] = dst[ 3] = (c & 0x40) ? fore : back;
							dst[ 4] = dst[ 5] = (c & 0x20) ? fore : back;
							dst[ 6] = dst[ 7] = (c & 0x10) ? fore : back;
							dst[ 8] = dst[ 9] = (c & 0x08) ? fore : back;
							dst[10] = dst[11] = (c & 0x04) ? fore : back;
							dst[12] = dst[13] = (c & 0x02) ? fore : back;
							dst[14] = dst[15] = (c & 0x01) ? fore : back;
							dst += 16;
						}
					}
					break;

				case 8:
					{
						const uint32 pal[4] = {
							(uint32)pfbak * 0x01010101,
							(uint32)pf0 * 0x01010101,
							(uint32)pf1 * 0x01010101,
							(uint32)pf2 * 0x01010101,
						};

						uint32 *dst32 = (uint32 *)dst;

						for(int i=0; i<rowbytes; ++i) {
							const uint8 c  = rowbuffer[i];

							dst32[0] = dst32[1] = pal[(c >> 6) & 3];
							dst32[2] = dst32[3] = pal[(c >> 4) & 3];
							dst32[4] = dst32[5] = pal[(c >> 2) & 3];
							dst32[6] = dst32[7] = pal[(c >> 0) & 3];
							dst32 += 4;
						}
					}
					break;

				case 9:
					{
						const uint32 back = (uint32)pfbak * 0x01010101;
						const uint32 fore = (uint32)pf0 * 0x01010101;

						uint32 *dst32 = (uint32 *)dst;

						for(int i=0; i<rowbytes; ++i) {
							const uint8 c  = rowbuffer[i];

							dst32[0] = (c & 0x80) ? fore : back;
							dst32[1] = (c & 0x40) ? fore : back;
							dst32[2] = (c & 0x20) ? fore : back;
							dst32[3] = (c & 0x10) ? fore : back;
							dst32[4] = (c & 0x08) ? fore : back;
							dst32[5] = (c & 0x04) ? fore : back;
							dst32[6] = (c & 0x02) ? fore : back;
							dst32[7] = (c & 0x01) ? fore : back;
							dst32 += 8;
						}
					}
					break;

				case 10:
					{
						const uint32 pal[4] = {
							(uint32)pfbak * 0x01010101,
							(uint32)pf0 * 0x01010101,
							(uint32)pf1 * 0x01010101,
							(uint32)pf2 * 0x01010101,
						};

						uint32 *dst32 = (uint32 *)dst;

						for(int i=0; i<rowbytes; ++i) {
							const uint8 c  = rowbuffer[i];

							dst32[0] = pal[(c >> 6) & 3];
							dst32[1] = pal[(c >> 4) & 3];
							dst32[2] = pal[(c >> 2) & 3];
							dst32[3] = pal[(c >> 0) & 3];
							dst32 += 4;
						}
					}
					break;

				case 11:
				case 12:
					{
						const uint8 back = pfbak;
						const uint8 fore = pf0;

						for(int i=0; i<rowbytes; ++i) {
							const uint8 c  = rowbuffer[i];

							dst[ 0] = dst[ 1] = (c & 0x80) ? fore : back;
							dst[ 2] = dst[ 3] = (c & 0x40) ? fore : back;
							dst[ 4] = dst[ 5] = (c & 0x20) ? fore : back;
							dst[ 6] = dst[ 7] = (c & 0x10) ? fore : back;
							dst[ 8] = dst[ 9] = (c & 0x08) ? fore : back;
							dst[10] = dst[11] = (c & 0x04) ? fore : back;
							dst[12] = dst[13] = (c & 0x02) ? fore : back;
							dst[14] = dst[15] = (c & 0x01) ? fore : back;
							dst += 16;
						}
					}
					break;

				case 13:
				case 14:
					{
						const uint8 pal[4] = {
							pfbak,
							pf0,
							pf1,
							pf2
						};

						for(int i=0; i<rowbytes; ++i) {
							const uint8 c  = rowbuffer[i];

							dst[0] = dst[1] = pal[(c >> 6) & 3];
							dst[2] = dst[3] = pal[(c >> 4) & 3];
							dst[4] = dst[5] = pal[(c >> 2) & 3];
							dst[6] = dst[7] = pal[(c >> 0) & 3];
							dst += 8;
						}
					}
					break;

				case 15:
					switch(gtiaState.mReg[0x1B] >> 6) {
						case 0:
							{
								const uint8 back = pf2;
								const uint8 fore = (back & 0xf0) + (pf1 & 0x0e);

								for(int i=0; i<rowbytes; ++i) {
									const uint8 c  = rowbuffer[i];

									dst[0] = (c & 0x80) ? fore : back;
									dst[1] = (c & 0x40) ? fore : back;
									dst[2] = (c & 0x20) ? fore : back;
									dst[3] = (c & 0x10) ? fore : back;
									dst[4] = (c & 0x08) ? fore : back;
									dst[5] = (c & 0x04) ? fore : back;
									dst[6] = (c & 0x02) ? fore : back;
									dst[7] = (c & 0x01) ? fore : back;
									dst += 8;
								}
							}
							break;

						case 1:
							{
								const uint8 hue = pfbak & 0xf0;

								for(int i=0; i<rowbytes; ++i) {
									const uint8 c  = rowbuffer[i];

									dst[0] = dst[1] = dst[2] = dst[3] = hue + (c >> 4);
									dst[4] = dst[5] = dst[6] = dst[7] = hue + (c & 0x0f);
									dst += 8;
								}
							}
							break;

						case 2:
							{
								const uint8 pal[16] = {
									pm0,
									pm1,
									pm2,
									pm3,
									pf0,
									pf1,
									pf2,
									pf3,
									pfbak,
									pfbak,
									pfbak,
									pfbak,
									pf0,
									pf1,
									pf2,
									pf3
								};

								dst += 2;

								for(int i=0; i<rowbytes; ++i) {
									const uint8 c  = rowbuffer[i];

									dst[0] = dst[1] = dst[2] = dst[3] = pal[c >> 4];
									dst[4] = dst[5] = dst[6] = dst[7] = pal[c & 0x0f];
									dst += 8;
								}
							}
							break;

						case 3:
							{
								uint8 pal[16];

								pal[0] = 0;

								for(int i=1; i<16; ++i)
									pal[i] = (i << 4) + (pfbak & 15);

								for(int i=0; i<rowbytes; ++i) {
									const uint8 c  = rowbuffer[i];

									dst[0] = dst[1] = dst[2] = dst[3] = pal[c >> 4];
									dst[4] = dst[5] = dst[6] = dst[7] = pal[c & 0x0f];
									dst += 8;
								}
							}
							break;
					}
					break;
			}

			// Mask off portions of the wide playfield that aren't visible, but are still
			// handled by GTIA. The displayable area is $22-$DD, but the playfield only
			// shows up starting at $2C.
			memset(dst0, pfbak, 0x0C * 2);

			// If the display width and fetch width don't match, mask off the borders. This
			// is not needed for wide scrolled playfields as those shift in background on
			// the left and are always cut off on the right.
			if (displayWidth != fetchWidth) {
				switch(displayWidth) {
					case 1:		// narrow -> normal

						// fill $30-$3F
						memset(dst0 + (0x30 - 0x20)*2, pfbak, 0x10 * 2);

						// fill $C0-$CF
						memset(dst0 + (0xC0 - 0x20)*2, pfbak, 0x10 * 2);
						break;

					case 2:		// normal -> wide
						// fill $20-$2F
						memset(dst0 + (0x20 - 0x20)*2, pfbak, 0x10 * 2);

						// fill $D0-$DF
						memset(dst0 + (0xD0 - 0x20)*2, pfbak, 0x10 * 2);
						break;
				}
			}

			++y;
		} while(row++ != rowlast && y < 240);
	}

	mpGTIA->GetPalette(mPalette);
	mpDisplay->SetSourcePersistent(true, mDisplayBuffer);
}
