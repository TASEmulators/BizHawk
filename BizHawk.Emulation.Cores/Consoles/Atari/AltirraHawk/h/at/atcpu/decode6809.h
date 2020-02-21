//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
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

#ifndef AT_ATCPU_DECODE6809_H
#define AT_ATCPU_DECODE6809_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>

struct ATCPUDecoderTables6809 {
	uint16	mInsns[256];
	uint16	mInsns10[256];
	uint16	mInsns11[256];
	uint16	mIndexedModes[33];
	uint8	mDecodeHeap[0x2000];

	uint16	mIrqSequence;
	uint16	mCwaiIrqSequence;
	uint16	mFirqSequence;
	uint16	mCwaiFirqSequence;
	uint16	mNmiSequence;
	uint16	mCwaiNmiSequence;
};

class ATCPUDecoderGenerator6809 {
public:
	void	RebuildTables(ATCPUDecoderTables6809& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints);

private:
	void	DecodeInsns(ATCPUDecoderTables6809& dst, uint16 *p, bool (ATCPUDecoderGenerator6809::*pfn)(uint8), bool historyTracing, bool enableBreakpoints);
	bool	DecodeInsn(uint8 opcode);
	bool	DecodeInsn10(uint8 opcode);
	bool	DecodeInsn11(uint8 opcode);

	void	DecodeAddrDirect_2();
	void	DecodeAddrIndexed_2p();
	void	DecodeAddrExtended_3();

	bool	mbStopOnBRK;

	uint8	*mpDstState;
};

#endif
