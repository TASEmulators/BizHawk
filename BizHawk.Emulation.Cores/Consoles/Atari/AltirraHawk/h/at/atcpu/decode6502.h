//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
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

#ifndef AT_ATCPU_DECODE6502_H
#define AT_ATCPU_DECODE6502_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>

struct ATCPUDecoderTables6502 {
	uint16	mInsnPtrs[258];
	uint8	mDecodeHeap[0x5000];
};

class ATCPUDecoderGenerator6502 {
public:
	void	RebuildTables(ATCPUDecoderTables6502& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints, bool isC02);

private:
	bool	DecodeInsn6502(uint8 opcode);
	bool	DecodeInsn6502Ill(uint8 opcode);
	bool	DecodeInsn65C02(uint8 opcode);

	void	DecodeReadZp();
	void	DecodeReadZpX();
	void	DecodeReadZpY();
	void	DecodeReadAbs();
	void	DecodeReadAbsX();
	void	DecodeReadAbsY();
	void	DecodeReadIndX();
	void	DecodeReadIndY();
	void	DecodeReadInd();

	bool	mbStopOnBRK;
	bool	mbHistoryEnabled;

	uint8	*mpDstState;
};

#endif
