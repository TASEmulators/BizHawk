//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>
#include <vd2/system/cpuaccel.h>
#include "gtiarenderer.h"
#include "gtiatables.h"
#include "savestate.h"

using namespace ATGTIA;

#ifdef VD_CPU_X86
extern "C" void VDCDECL atasm_gtia_render_lores_fast_ssse3(
	void *dst,
	const uint8 *src,
	uint32 n,
	const uint8 *color_table
);

extern "C" void VDCDECL atasm_gtia_render_mode8_fast_ssse3(
	void *dst,
	const uint8 *src,
	const uint8 *lumasrc,
	uint32 n,
	const uint8 *color_table
);
#elif VD_CPU_X64
#include "gtiarenderer_ssse3_intrin.inl"
#elif VD_CPU_ARM64
#include "gtiarenderer_neon.inl"
#endif

ATGTIARenderer::ATGTIARenderer()
	: mpMergeBuffer(NULL)
	, mpAnticBuffer(NULL)
	, mpDst(NULL)
	, mX(0)
	, mRCIndex(0)
	, mRCCount(0)
	, mbHiresMode(false)
	, mbVBlank(true)
	, mbSECAMMode(false)
	, mPRIOR(0)
	, mpPriTable(NULL)
	, mpColorTable(NULL)
{
	memset(mColorTable, 0, sizeof mColorTable);

	ATInitGTIAPriorityTables(mPriorityTables);

	mpColorTable = mColorTable;
	mpPriTable = mPriorityTables[0];
}

ATGTIARenderer::~ATGTIARenderer() {
}

void ATGTIARenderer::SetAnalysisMode(bool enable) {
	mpColorTable = enable ? kATAnalysisColorTable : mColorTable;
}

void ATGTIARenderer::SetVBlank(bool vblank) {
	mbVBlank = vblank;
}

void ATGTIARenderer::SetCTIAMode() {
	// scrub any register changes
	for(int i=mRCIndex; i<mRCCount; ++i) {
		if (mRegisterChanges[i].mReg == 0x1B)
			mRegisterChanges[i].mValue &= 0x3F;
	}
}

void ATGTIARenderer::ColdReset() {
	memset(mColorTable, 0, sizeof mColorTable);
}

void ATGTIARenderer::BeginScanline(uint8 *dst, const uint8 *mergeBuffer, const uint8 *anticBuffer, bool hires) {
	mpDst = dst;
	mbHiresMode = hires;
	mbGTIAEnableTransition = false;
	mbGTIATransitionFromHiresMode = hires;
	mX = mbVBlank ? 34 : 0;
	mpMergeBuffer = mergeBuffer;
	mpAnticBuffer = anticBuffer;

	VDASSERT(((uintptr)mpDst & 15) == 0);
	VDASSERT(((uintptr)mpMergeBuffer & 7) == 0);

	memset(mpDst, mpColorTable[kColorBAK], 68);
}

void ATGTIARenderer::RenderScanline(int xend, bool pfgraphics, bool pmgraphics, bool mixed) {
	int x1 = mX;

	if (x1 >= xend)
		return;

	// render spans and process register changes
	do {
		int x2 = xend;

		if (mRCIndex < mRCCount) {
			const RegisterChange *rc0 = &mRegisterChanges[mRCIndex];
			const RegisterChange *rc = rc0;
			do {
				int xchg = rc->mPos;
				if (xchg > x1) {
					if (x2 > xchg)
						x2 = xchg;
					break;
				}

				++rc;
			} while(++mRCIndex < mRCCount);

			UpdateRegisters(rc0, (int)(rc - rc0));
		}

		if (mbGTIAEnableTransition) {
			switch(mTransitionPhase) {
				case 0:		// turning on GTIA mode
					if ((mPRIOR & 0xc0) == 0x40) {
						RenderMode9Transition1(x1);
						mTransitionPhase = 1;
					} else {
						if (mbGTIATransitionFromHiresMode)
							RenderMode8(x1, x1+1);
						else
							RenderLores(x1, x1+1);

						if ((mPRIOR & 0xc0) == 0x80) {
							mTransitionPhase = 1;
						} else {
							mbGTIAEnableTransition = false;
						}
					}
					break;

				case 1:	// mode 8 -> mode 9 (1/2) or 10 (1/3)
					if ((mPRIOR & 0xc0) == 0x80)
						RenderMode10Transition2(x1);
					else
						RenderMode9Transition2(x1);
					mTransitionPhase = 2;
					break;

				case 2:	// mode 8 -> mode 9 (2/2) or 10 (2/3)
					if ((mPRIOR & 0xc0) == 0x80) {
						RenderMode10Transition2(x1);
						mTransitionPhase = 3;
					} else {
						RenderMode9Transition2(x1);
						mbGTIAEnableTransition = false;
					}
					break;

				case 3:	// mode 8 -> mode 10, cclk 2/3
					RenderMode10Transition3(x1);
					mbGTIAEnableTransition = false;
					break;

				case 5:
					mbGTIAEnableTransition = false;
				case 4:	// mode 10 -> mode 15alt
					RenderBlank(x1);
					++mTransitionPhase;
					break;

				case 7:
					mbGTIAEnableTransition = false;
				case 6:
					RenderMode10(x1, x1+1);
					++mTransitionPhase;
					break;
			}

			++x1;
			continue;
		}

		// 40 column mode is set by ANTIC during horizontal blank if ANTIC modes 2, 3, or
		// F are used. 40 column mode has the following effects:
		//
		//	* The priority logic always sees PF2.
		//	* The collision logic sees either BAK or PF2. Adjacent bits are ORed each color
		//	  clock to determine this (PF2C in schematic).
		//	* The playfield bits are used instead to substitute the luminance of PF1 on top
		//	  of the priority logic output. This happens even if players have priority.
		//
		// The flip-flip in the GTIA that controls 40 column mode can only be set by the
		// horizontal sync command, but can be reset at any time whenever either of the
		// top two bits of PRIOR are set. If this happens, the GTIA will begin interpreting
		// AN0-AN2 in lores mode, but ANTIC will continue sending in hires mode. The result
		// is that the bit pair patterns 00-11 produce PF0-PF3 instead of BAK + PF0-PF2 as
		// usual.

		switch(mPRIOR & 0xc0) {
			case 0x00:
				if (pmgraphics) {
					if (mbHiresMode)
						RenderMode8(x1, x2);
					else
						RenderLores(x1, x2);
				} else {
					if (mbHiresMode)
						RenderMode8Fast(x1, x2);
					else
						RenderLoresFast(x1, x2);
				}
				break;

			case 0x40:
				if (pmgraphics)
					RenderMode9(x1, x2);
				else
					RenderMode9Fast(x1, x2);
				break;

			case 0x80:
				if (pmgraphics || mixed)
					RenderMode10(x1, x2);
				else
					RenderMode10Fast(x1, x2);
				break;

			case 0xC0:
				if (pmgraphics || mixed)
					RenderMode11(x1, x2);
				else
					RenderMode11Fast(x1, x2);
				break;
		}

		x1 = x2;
	} while(x1 < xend);

	mX = x1;
}

void ATGTIARenderer::EndScanline() {
	if (mpDst) {
		memset(mpDst + 444, mpColorTable[8], 456 - 444);
		mpDst = NULL;
	}

	// commit any outstanding register changes
	if (mRCIndex < mRCCount)
		UpdateRegisters(&mRegisterChanges[mRCIndex], mRCCount - mRCIndex);

	mRCCount = 0;
	mRCIndex = 0;
	mRegisterChanges.clear();
}

void ATGTIARenderer::AddRegisterChange(uint8 pos, uint8 addr, uint8 value) {
	RegisterChanges::iterator it(mRegisterChanges.end()), itBegin(mRegisterChanges.begin());

	while(it != itBegin && it[-1].mPos > pos)
		--it;

	RegisterChange change;
	change.mPos = pos;
	change.mReg = addr;
	change.mValue = value;
	change.mPad = 0;
	mRegisterChanges.insert(it, change);

	++mRCCount;
}

void ATGTIARenderer::SetRegisterImmediate(uint8 addr, uint8 value) {
	RegisterChange change;
	change.mPos = 0;
	change.mReg = addr;
	change.mValue = value;
	change.mPad = 0;

	UpdateRegisters(&change, 1);
}

template<class T>
void ATGTIARenderer::ExchangeState(T& io) {
	io != mX;
	io != mRCIndex;
	io != mRCCount;
	io != mbHiresMode;
	io != mPRIOR;

	// Note that we don't include the color table here as that is reloaded by the GTIA emulator.
}

void ATGTIARenderer::LoadState(ATSaveStateReader& reader) {
	ExchangeState(reader);

	// read register changes
	mRegisterChanges.resize(mRCCount);
	for(int i=0; i<mRCCount; ++i) {
		RegisterChange& rc = mRegisterChanges[i];

		rc.mPos = reader.ReadUint8();
		rc.mReg = reader.ReadUint8();
		rc.mValue = reader.ReadUint8();
	}

	UpdatePriorityTable();
}

void ATGTIARenderer::ResetState() {
	mRegisterChanges.clear();
	mRCIndex = 0;
	mRCCount = 0;
	mbHiresMode = false;
	mPRIOR = 0;
	mX = 0;
}

void ATGTIARenderer::SaveState(ATSaveStateWriter& writer) {
	ExchangeState(writer);

	// write register changes
	for(int i=0; i<mRCCount; ++i) {
		const RegisterChange& rc = mRegisterChanges[i];

		writer.WriteUint8(rc.mPos);
		writer.WriteUint8(rc.mReg);
		writer.WriteUint8(rc.mValue);
	}
}

void ATGTIARenderer::UpdateRegisters(const RegisterChange *rc, int count) {
	while(count--) {
		// process register change
		uint8 value = rc->mValue;

		switch(rc->mReg) {
		case 0x12:
			value &= 0xfe;
			mColorTable[kColorP0] = value;
			mColorTable[kColorP0P1] = value | mColorTable[kColorP1];
			mColorTable[kColorPF0P0] = mColorTable[kColorPF0] | value;
			mColorTable[kColorPF0P0P1] = mColorTable[kColorPF0P1] | value;
			mColorTable[kColorPF1P0] = mColorTable[kColorPF1] | value;
			mColorTable[kColorPF1P0P1] = mColorTable[kColorPF1P1] | value;
			break;
		case 0x13:
			value &= 0xfe;
			mColorTable[kColorP1] = value;
			mColorTable[kColorP0P1] = mColorTable[kColorP0] | value;
			mColorTable[kColorPF0P1] = mColorTable[kColorPF0] | value;
			mColorTable[kColorPF0P0P1] = mColorTable[kColorPF0P0] | value;
			mColorTable[kColorPF1P1] = mColorTable[kColorPF1] | value;
			mColorTable[kColorPF1P0P1] = mColorTable[kColorPF1P0] | value;
			break;
		case 0x14:
			value &= 0xfe;
			mColorTable[kColorP2] = value;
			mColorTable[kColorP2P3] = value | mColorTable[kColorP3];
			mColorTable[kColorPF2P2] = mColorTable[kColorPF2] | value;
			mColorTable[kColorPF2P2P3] = mColorTable[kColorPF2P3] | value;
			mColorTable[kColorPF3P2] = mColorTable[kColorPF3] | value;
			mColorTable[kColorPF3P2P3] = mColorTable[kColorPF3P3] | value;
			break;
		case 0x15:
			value &= 0xfe;
			mColorTable[kColorP3] = value;
			mColorTable[kColorP2P3] = mColorTable[kColorP2] | value;
			mColorTable[kColorPF2P3] = mColorTable[kColorPF2] | value;
			mColorTable[kColorPF2P2P3] = mColorTable[kColorPF2P2] | value;
			mColorTable[kColorPF3P3] = mColorTable[kColorPF3] | value;
			mColorTable[kColorPF3P2P3] = mColorTable[kColorPF3P2] | value;
			break;
		case 0x16:
			value &= 0xfe;
			mColorTable[kColorPF0] = value;
			mColorTable[kColorPF0P0] = value | mColorTable[kColorP0];
			mColorTable[kColorPF0P1] = value | mColorTable[kColorP1];
			mColorTable[kColorPF0P0P1] = value | mColorTable[kColorP0P1];
			break;
		case 0x17:
			value &= 0xfe;
			mColorTable[kColorPF1] = value;
			mColorTable[kColorPF1P0] = value | mColorTable[kColorP0];
			mColorTable[kColorPF1P1] = value | mColorTable[kColorP1];
			mColorTable[kColorPF1P0P1] = value | mColorTable[kColorP0P1];
			break;
		case 0x18:
			value &= 0xfe;
			mColorTable[kColorPF2] = value;
			mColorTable[kColorPF2P2] = value | mColorTable[kColorP2];
			mColorTable[kColorPF2P3] = value | mColorTable[kColorP3];
			mColorTable[kColorPF2P2P3] = value | mColorTable[kColorP2P3];
			break;
		case 0x19:
			value &= 0xfe;
			mColorTable[kColorPF3] = value;
			mColorTable[kColorPF3P2] = value | mColorTable[kColorP2];
			mColorTable[kColorPF3P3] = value | mColorTable[kColorP3];
			mColorTable[kColorPF3P2P3] = value | mColorTable[kColorP2P3];
			break;
		case 0x1A:
			value &= 0xfe;
			mColorTable[kColorBAK] = value;
			break;
		case 0x1B:
			mbGTIATransitionFromHiresMode = mbHiresMode;

			if ((value & 0xc0) && !(mPRIOR & 0xc0)) {
				mbGTIAEnableTransition = true;
				mTransitionPhase = 0;
			} else if (!(value & 0xc0) && (mPRIOR & 0xc0)) {
				switch(mPRIOR & 0xc0) {
				case 0x40:		// mode 9 -> mode 8 -- 2 color clocks blanked
				case 0xc0:		// mode 11 -> mode 8 -- 2 color clocks blanked
					mbGTIAEnableTransition = true;
					mTransitionPhase = 4;
					break;

				case 0x80:		// mode 9 -> mode 8 -- mode 9 extends 2cclks
					mbGTIAEnableTransition = true;
					mTransitionPhase = 6;
					break;
				}
			}

			mPRIOR = value;

			UpdatePriorityTable();

			if (value & 0xC0)
				mbHiresMode = false;
			break;
		}

		++rc;
	}
}

void ATGTIARenderer::RenderBlank(int x1) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	dst[0] = dst[1] = colorTable[priTable[src[0] & 0xf0]];
}

void ATGTIARenderer::RenderLores(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	int w = x2 - x1;
	int w4 = w >> 2;

	for(int i=0; i<w4; ++i) {
		dst[0] = dst[1] = colorTable[priTable[src[0]]];
		dst[2] = dst[3] = colorTable[priTable[src[1]]];
		dst[4] = dst[5] = colorTable[priTable[src[2]]];
		dst[6] = dst[7] = colorTable[priTable[src[3]]];
		src += 4;
		dst += 8;
	}

	for(int i=w & 3; i; --i) {
		dst[0] = dst[1] = colorTable[priTable[src[0]]];
		++src;
		dst += 2;
	}
}

void ATGTIARenderer::RenderLoresFast(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

#if defined(VD_CPU_X86) || defined(VD_CPU_AMD64)
	if (CPUGetEnabledExtensions() & CPUF_SUPPORTS_SSSE3) {
		atasm_gtia_render_lores_fast_ssse3(
			dst,
			src,
			x2 - x1,
			colorTable
		);

		return;
	}
#elif defined(VD_CPU_ARM64)
	atasm_gtia_render_lores_fast_neon(dst, src, x2 - x1, colorTable);
	return;
#endif

	int w = x2 - x1;
	int w4 = w >> 2;

	uint16 fasttab[9];
	fasttab[  0] = (uint16)colorTable[kColorBAK] * 0x0101;
	fasttab[PF0] = (uint16)colorTable[kColorPF0] * 0x0101;
	fasttab[PF1] = (uint16)colorTable[kColorPF1] * 0x0101;
	fasttab[PF2] = (uint16)colorTable[kColorPF2] * 0x0101;
	fasttab[PF3] = (uint16)colorTable[kColorPF3] * 0x0101;

	for(int i=0; i<w4; ++i) {
		*(uint16 *)&dst[0] = fasttab[src[0]];
		*(uint16 *)&dst[2] = fasttab[src[1]];
		*(uint16 *)&dst[4] = fasttab[src[2]];
		*(uint16 *)&dst[6] = fasttab[src[3]];
		src += 4;
		dst += 8;
	}

	for(int i=w & 3; i; --i) {
		*(uint16 *)&dst[0] = fasttab[src[0]];
		++src;
		dst += 2;
	}
}

void ATGTIARenderer::RenderMode8(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	const uint8 *lumasrc = &mpAnticBuffer[x1];
	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	const uint8 luma1 = mpColorTable[5] & 0xf;

	const uint32 andtab[4]={
		0xffff,
		0xf0ff,
		0xfff0,
		0xf0f0,
	};

	const uint32 addtab[4]={
		0x0000,
		(uint32)luma1 << 8,
		luma1,
		(uint32)luma1 * 0x0101
	};

	int w = x2 - x1;
	while(w--) {
		uint32 lb = *lumasrc++ & 3;

		uint32 c0 = (uint32)colorTable[priTable[*src++]];

		c0 += (c0 << 8);

		*(uint16 *)dst = (c0 & andtab[lb]) + addtab[lb];
		dst += 2;
	}
}

void ATGTIARenderer::RenderMode8Fast(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;

	const uint8 *lumasrc = &mpAnticBuffer[x1];
	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

#if defined(VD_CPU_X86) || defined(VD_CPU_AMD64)
	if (CPUGetEnabledExtensions() & CPUF_SUPPORTS_SSSE3) {
		atasm_gtia_render_mode8_fast_ssse3(
			dst,
			src,
			lumasrc,
			x2 - x1,
			colorTable
		);

		return;
	}
#elif defined(VD_CPU_ARM64)
	atasm_gtia_render_mode8_fast_neon(
		dst,
		src,
		lumasrc,
		x2 - x1,
		colorTable
	);
	return;
#endif

	const uint8 luma1 = mpColorTable[5] & 0xf;

	const uint32 andtab[4]={
		0xffff,
		0xf0ff,
		0xfff0,
		0xf0f0,
	};

	const uint32 addtab[4]={
		0x0000,
		(uint32)luma1 << 8,
		luma1,
		(uint32)luma1 * 0x0101
	};

	const uint32 coltab[2]={
		(uint32)colorTable[kColorBAK] * 0x00000101,
		(uint32)colorTable[kColorPF2] * 0x00000101
	};

	int w = x2 - x1;
	while(w--) {
		uint32 lb = *lumasrc++;
		uint32 c0 = *(const uint32 *)((const uint8 *)coltab + (*src++));

		*(uint16 *)dst = (c0 & andtab[lb]) + addtab[lb];
		dst += 2;
	}
}

void ATGTIARenderer::RenderMode9(int x1, int x2) {
	static const uint8 kPlayerMaskLookup[16]={0xff};
	static const uint8 kPlayerMaskLookupSECAM[16]={0xfe};

	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	// 1 color / 16 luma mode
	//
	// In this mode, PF0-PF3 are forced off, so no playfield collisions ever register
	// and the playfield always registers as the background color. Luminance is
	// ORed in after the priority logic, but its substitution is gated by all P/M bits
	// and so it does not affect players or missiles. It does, however, affect PF3 if
	// the fifth player is enabled.

	int w = x2 - x1;

	if (mbSECAMMode) {
		while(w--) {
			uint8 code0 = *src++ & (P0|P1|P2|P3|PF3);
			uint8 pri0 = colorTable[priTable[code0]];

			const uint8 *lumasrc = &mpAnticBuffer[x1++ & ~1];
			uint8 l1 = (lumasrc[0] << 2) + lumasrc[1];

			uint8 c4 = pri0 | (l1 & kPlayerMaskLookupSECAM[code0 >> 4]);

			dst[0] = dst[1] = c4;
			dst += 2;
		}
	} else {
		while(w--) {
			uint8 code0 = *src++ & (P0|P1|P2|P3|PF3);
			uint8 pri0 = colorTable[priTable[code0]];

			const uint8 *lumasrc = &mpAnticBuffer[x1++ & ~1];
			uint8 l1 = (lumasrc[0] << 2) + lumasrc[1];

			uint8 c4 = pri0 | (l1 & kPlayerMaskLookup[code0 >> 4]);

			dst[0] = dst[1] = c4;
			dst += 2;
		}
	}
}

void ATGTIARenderer::RenderMode9Fast(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;
	uint8 *__restrict dst = mpDst + x1*2;
	const uint8 *__restrict anticSrc = mpAnticBuffer;

	// 1 color / 16 luma mode
	//
	// In this mode, PF0-PF3 are forced off, so no playfield collisions ever register
	// and the playfield always registers as the background color. Luminance is
	// ORed in after the priority logic, but its substitution is gated by all P/M bits
	// and so it does not affect players or missiles. It does, however, affect PF3 if
	// the fifth player is enabled.

	int w = x2 - x1;
	if (!w)
		return;

	const uint8 pfbak = colorTable[kColorBAK];

	if (mbSECAMMode) {
		while(w--) {
			const uint8 *lumasrc = &anticSrc[x1++ & ~1];
			uint8 l1 = (lumasrc[0] << 2) + lumasrc[1];

			uint8 c4 = pfbak | (l1 & 0xfe);

			dst[0] = dst[1] = c4;
			dst += 2;
		}
	} else {
		const uint8 *lumasrc = &anticSrc[x1];

		if (x1 & 1) {
			uint8 l1 = (lumasrc[-1] << 2) + lumasrc[0];

			uint8 c4 = pfbak | l1;

			dst[0] = dst[1] = c4;
			dst += 2;
			--w;
			++lumasrc;
		}

		int w2 = w >> 1;
		while(w2--) {
			uint8 l1 = (lumasrc[0] << 2) + lumasrc[1];
			lumasrc += 2;

			uint8 c4 = pfbak | l1;

			dst[0] = dst[1] = dst[2] = dst[3] = c4;
			dst += 4;
		}

		if (w & 1) {
			uint8 l1 = (lumasrc[0] << 2) + lumasrc[1];

			uint8 c4 = pfbak | l1;

			dst[0] = dst[1] = c4;
		}
	}
}

void ATGTIARenderer::RenderMode9Transition1(int x1) {
	static const uint8 kPlayerMaskLookup[16]={0xff};

	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	// This is complicated.
	//
	// What happens on the first color clock of the three color clock transition is that
	// both mode 9 and hires logic is active at the same time. Fun.

	uint8 code0 = *src++ & (P0|P1|P2|P3|PF3);
	uint8 pri0 = colorTable[priTable[code0]];

	const uint8 *lumasrc = &mpAnticBuffer[x1++];
	uint8 l1 = (lumasrc[0] << 2) + lumasrc[1];

	uint8 c0 = pri0;
	uint8 c1 = c0;

	if (l1 & 0x08)
		c0 = (c0 & 0xf0) + (mpColorTable[5] & 0xf);

	if (l1 & 0x04)
		c1 = (c1 & 0xf0) + (mpColorTable[5] & 0xf);

	const uint8 lumaadd = (l1 & kPlayerMaskLookup[code0 >> 4]);

	dst[0] = c0 | lumaadd;
	dst[1] = c1 | lumaadd;
}

void ATGTIARenderer::RenderMode9Transition2(int x1) {
	static const uint8 kPlayerMaskLookup[16]={0xff};

	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	// 1 color / 16 luma mode
	//
	// In this mode, PF0-PF3 are forced off, so no playfield collisions ever register
	// and the playfield always registers as the background color. Luminance is
	// ORed in after the priority logic, but its substitution is gated by all P/M bits
	// and so it does not affect players or missiles. It does, however, affect PF3 if
	// the fifth player is enabled.

	uint8 code0 = *src++ & (P0|P1|P2|P3|PF3);
	uint8 pri0 = colorTable[priTable[code0]];

	const uint8 *lumasrc = &mpAnticBuffer[x1++];
	uint8 l1 = (lumasrc[0] << 2) + lumasrc[0];

	uint8 c4 = pri0 | (l1 & kPlayerMaskLookup[code0 >> 4]);

	dst[0] = dst[1] = c4;
}

void ATGTIARenderer::RenderMode10(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *__restrict dst = mpDst + x1*2;
	const uint8 *__restrict src = mpMergeBuffer + x1;

	// 9 colors
	//
	// This mode works by using AN0-AN1 to trigger either the playfield or the player/missle
	// bits going into the priority logic. This means that when player colors are used, the
	// playfield takes the same priority as that player. Playfield collisions are triggered
	// only for PF0-PF3; P0-P3 colors coming from the playfield do not trigger collisions.

	static const uint8 kMode10Lookup[16]={
		P0,
		P1,
		P2,
		P3,
		PF0,
		PF1,
		PF2,
		PF3,
		0,
		0,
		0,
		0,
		PF0,
		PF1,
		PF2,
		PF3
	};

	// If the second pixel sent on the ANx bus is background, it prevents the playfields
	// from activating.
	static const uint8 kPFMask[16]={
		0xF0, 0xFF, 0xFF, 0xFF,
		0xFF, 0xFF, 0xFF, 0xFF,
		0xFF, 0xFF, 0xFF, 0xFF,
		0xFF, 0xFF, 0xFF, 0xFF,
	};

	int w = x2 - x1;
	if (!w)
		return;

	const uint8 *__restrict lumasrc = &mpAnticBuffer[(x1 - 1) & ~1];
	if (!(x1 & 1)) {
		uint8 l1 = lumasrc[0]*4 + lumasrc[1];
		lumasrc += 2;

		uint8 c4 = kMode10Lookup[l1] & kPFMask[*src & 15];

		dst[0] = dst[1] = colorTable[priTable[c4 | (*src++ & 0xf8)]];
		dst += 2;
	}

	int w2 = w >> 1;
	while(w2--) {
		uint8 l1 = lumasrc[0]*4 + lumasrc[1];
		lumasrc += 2;

		uint8 c4 = kMode10Lookup[l1] & kPFMask[src[1] & 15];

		dst[0] = dst[1] = colorTable[priTable[c4 | (src[0] & 0xf8)]];
		dst[2] = dst[3] = colorTable[priTable[c4 | (src[1] & 0xf8)]];
		dst += 4;
		src += 2;
	}

	if (w & 1) {
		uint8 l1 = lumasrc[0]*4 + lumasrc[1];

		uint8 c4 = kMode10Lookup[l1] & kPFMask[src[1] & 15];

		dst[0] = dst[1] = colorTable[priTable[c4 | (*src & 0xf8)]];
	}
}

void ATGTIARenderer::RenderMode10Fast(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;
	uint8 *__restrict dst = mpDst + x1*2;

	// 9 colors
	//
	// This mode works by using AN0-AN1 to trigger either the playfield or the player/missle
	// bits going into the priority logic. This means that when player colors are used, the
	// playfield takes the same priority as that player. Playfield collisions are triggered
	// only for PF0-PF3; P0-P3 colors coming from the playfield do not trigger collisions.

	const uint8 colorLookup[16]={
		colorTable[kColorP0],
		colorTable[kColorP1],
		colorTable[kColorP2],
		colorTable[kColorP3],
		colorTable[kColorPF0],
		colorTable[kColorPF1],
		colorTable[kColorPF2],
		colorTable[kColorPF3],
		colorTable[kColorBAK],
		colorTable[kColorBAK],
		colorTable[kColorBAK],
		colorTable[kColorBAK],
		colorTable[kColorPF0],
		colorTable[kColorPF1],
		colorTable[kColorPF2],
		colorTable[kColorPF3]
	};

	const uint8 *__restrict lumasrc = &mpAnticBuffer[(x1 - 1) & ~1];

	int w = x2 - x1;
	if (!w)
		return;

	if (!(x1 & 1)) {
		uint8 l1 = lumasrc[0]*4 + lumasrc[1];
		lumasrc += 2;
		uint8 c4 = colorLookup[l1];

		dst[0] = dst[1] = c4;
		dst += 2;
		--w;
	}

	int w2 = w >> 1;
	while(w2--) {
		uint8 l1 = lumasrc[0]*4 + lumasrc[1];
		lumasrc += 2;

		uint8 c4 = colorLookup[l1];

		dst[0] = dst[1] = dst[2] = dst[3] = c4;
		dst += 4;
	}

	if (w & 1) {
		uint8 l1 = lumasrc[0]*4 + lumasrc[1];
		lumasrc += 2;
		uint8 c4 = colorLookup[l1];

		dst[0] = dst[1] = c4;
		dst += 2;
		--w;
	}
}

void ATGTIARenderer::RenderMode10Transition2(int x1) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	// 9 colors
	//
	// This mode works by using AN0-AN1 to trigger either the playfield or the player/missle
	// bits going into the priority logic. This means that when player colors are used, the
	// playfield takes the same priority as that player. Playfield collisions are triggered
	// only for PF0-PF3; P0-P3 colors coming from the playfield do not trigger collisions.

	static const uint8 kMode10Lookup[16]={
		P0,
		P1,
		P2,
		P3,
		PF0,
		PF1,
		PF2,
		PF3,
		0,
		0,
		0,
		0,
		PF0,
		PF1,
		PF2,
		PF3
	};

	const uint8 *lumasrc = &mpAnticBuffer[x1++];
	uint8 l1 = (lumasrc[-1] << 2) + lumasrc[0];

	uint8 c4 = kMode10Lookup[l1];

	dst[0] = dst[1] = colorTable[priTable[c4 | (*src++ & 0xf8)]];
	dst += 2;
}

void ATGTIARenderer::RenderMode10Transition3(int x1) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	// 9 colors
	//
	// This mode works by using AN0-AN1 to trigger either the playfield or the player/missle
	// bits going into the priority logic. This means that when player colors are used, the
	// playfield takes the same priority as that player. Playfield collisions are triggered
	// only for PF0-PF3; P0-P3 colors coming from the playfield do not trigger collisions.

	static const uint8 kMode10Lookup[16]={
		P0,
		P1,
		P2,
		P3,
		PF0,
		PF1,
		PF2,
		PF3,
		0,
		0,
		0,
		0,
		PF0,
		PF1,
		PF2,
		PF3
	};

	const uint8 *lumasrc = &mpAnticBuffer[x1++];
	uint8 l1 = (lumasrc[-1] << 2) + lumasrc[-1];

	uint8 c4 = kMode10Lookup[l1];

	dst[0] = dst[1] = colorTable[priTable[c4 | (*src++ & 0xf8)]];
	dst += 2;
}

void ATGTIARenderer::RenderMode11(int x1, int x2) {
	const uint8 *__restrict colorTable = mpColorTable;
	const uint8 *__restrict priTable = mpPriTable;

	uint8 *dst = mpDst + x1*2;
	const uint8 *src = mpMergeBuffer + x1;

	// 16 colors / 1 luma
	//
	// In this mode, PF0-PF3 are forced off, so no playfield collisions ever register
	// and the playfield always registers as the background color. Chroma is
	// ORed in after the priority logic, but its substitution is gated by all P/M bits
	// and so it does not affect players or missiles. It does, however, affect PF3 if
	// the fifth player is enabled.

	static const uint8 kMode11Lookup[16][2][2]={
		{{0xff,0xff},{0xff,0xf0}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}},
		{{0x00,0xff},{0x00,0xff}}
	};

	int w = x2 - x1;

	while(w--) {
		const uint8 code0 = *src++ & (P0|P1|P2|P3|PF3);
		uint8 pri0 = colorTable[priTable[code0]];

		const uint8 *lumasrc = &mpAnticBuffer[x1++ & ~1];
		uint8 l0 = (lumasrc[0] << 6) + (lumasrc[1] << 4);

		uint8 c0 = (pri0 | (l0 & kMode11Lookup[code0 >> 4][l0 == 0][0])) & kMode11Lookup[code0 >> 4][l0 == 0][1];

		dst[0] = dst[1] = c0;
		dst += 2;
	}
}

// This is a faster version of the mode 11 renderer for when we know there are
// no P/M graphics or mid-line switches involved.
void ATGTIARenderer::RenderMode11Fast(int x1, int x2) {
	const uint8 *VDRESTRICT colorTable = mpColorTable;
	const uint8 *VDRESTRICT priTable = mpPriTable;

	uint8 *VDRESTRICT dst = mpDst + x1*2;

	// 16 colors / 1 luma
	//
	// In this mode, PF0-PF3 are forced off, so no playfield collisions ever register
	// and the playfield always registers as the background color. Chroma is
	// ORed in after the priority logic, but its substitution is gated by all P/M bits
	// and so it does not affect players or missiles. It does, however, affect PF3 if
	// the fifth player is enabled.

	static const uint8 kMode11FastLookup[7]={
		0xf0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff
	};

	int w = x2 - x1;

	const uint8 pri0 = colorTable[priTable[0]];
	const uint8 *VDRESTRICT lumasrc = &mpAnticBuffer[x1 & ~1];

	if (x1 & 1) {
		uint8 l0 = lumasrc[0];
		uint8 l1 = lumasrc[1];

		uint8 chroma = (l0 << 6) + (l1 << 4);

		uint8 c0 = (pri0 | chroma) & kMode11FastLookup[l0 + l1];

		dst[0] = dst[1] = c0;
		dst += 2;

		--w;
		lumasrc += 2;
	}

	uint32 w2 = w >> 1;
	if (w2) {
		do {
			uint8 l0 = lumasrc[0];
			uint8 l1 = lumasrc[1];

			uint8 chroma = (l0 << 6) + (l1 << 4);

			uint8 c0 = (pri0 | chroma) & kMode11FastLookup[l0 + l1];

			dst[0] = dst[1] = dst[2] = dst[3] = c0;
			dst += 4;
			lumasrc += 2;
		} while(--w2);
	}

	if (w & 1) {
		uint8 l0 = lumasrc[0];
		uint8 l1 = lumasrc[1];

		uint8 chroma = (l0 << 6) + (l1 << 4);

		uint8 c0 = (pri0 | chroma) & kMode11FastLookup[l0 + l1];

		dst[0] = dst[1] = c0;
	}
}

void ATGTIARenderer::UpdatePriorityTable() {
	mpPriTable = mPriorityTables[(mPRIOR & 15) + (mPRIOR & 32 ? 16 : 0)];
}
