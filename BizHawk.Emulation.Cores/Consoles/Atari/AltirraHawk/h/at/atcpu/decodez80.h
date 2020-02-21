//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2016 Avery Lee
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

#ifndef AT_ATCPU_DECODEZ80_H
#define AT_ATCPU_DECODEZ80_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>

struct ATCPUDecoderTablesZ80 {
	uint16	mInsns[256];
	uint16	mInsnsCB[256];
	uint16	mInsnsED[256];
	uint16	mInsnsDDFD[256];
	uint16	mInsnsDDFDCB[256];
	uint16	mIrqSequence;
	uint16	mNmiSequence;
	uint8	mDecodeHeap[0x5000];
};

class ATCPUDecoderGeneratorZ80 {
public:
	void	RebuildTables(ATCPUDecoderTablesZ80& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints);

private:
	void	DecodeInsns(ATCPUDecoderTablesZ80& dst, uint16 *p, bool (ATCPUDecoderGeneratorZ80::*pfn)(uint8), bool historyTracing, bool enableBreakpoints);
	bool	DecodeInsn(uint8 opcode);
	bool	DecodeInsnED(uint8 opcode);

	template<bool T_UseIXIY>
	bool	DecodeInsnCB(uint8 opcode);

	bool	DecodeInsnDDFD(uint8 opcode);

	void	DecodeArgToData(uint8 index, bool skipHLAddr);
	void	DecodeDataToArg(uint8 index, bool skipHLAddr);

	void	DecodeArgToData16(uint8 index);
	void	DecodeArgToAddr16(uint8 index);
	void	DecodeDataToArg16(uint8 index);

	bool	mbStopOnBRK;

	uint8	*mpDstState;
};

#endif
