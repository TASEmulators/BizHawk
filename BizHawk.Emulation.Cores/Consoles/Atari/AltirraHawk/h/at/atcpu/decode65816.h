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

#ifndef AT_ATCPU_DECODE65816_H
#define AT_ATCPU_DECODE65816_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>

struct ATCPUDecoderTables65816 {
	uint16	mInsnPtrs[10][258];
	uint8	mDecodeHeap[0x5000];
};

class ATCPUDecoderGenerator65816 {
public:
	void	RebuildTables(ATCPUDecoderTables65816& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints);

private:
	bool	DecodeInsn(uint8 opcode, bool unalignedDP, bool emu, bool mode16, bool index16);

	void	Decode65816AddrDp(bool unalignedDP);
	void	Decode65816AddrDpX(bool unalignedDP, bool emu);
	void	Decode65816AddrDpY(bool unalignedDP, bool emu);
	void	Decode65816AddrDpInd(bool unalignedDP);
	void	Decode65816AddrDpIndX(bool unalignedDP, bool emu);
	void	Decode65816AddrDpIndY(bool unalignedDP, bool emu, bool forceCycle);
	void	Decode65816AddrDpLongInd(bool unalignedDP);
	void	Decode65816AddrDpLongIndY(bool unalignedDP);
	void	Decode65816AddrAbs();
	void	Decode65816AddrAbsX(bool forceCycle);
	void	Decode65816AddrAbsY(bool forceCycle);
	void	Decode65816AddrAbsLong();
	void	Decode65816AddrAbsLongX();
	void	Decode65816AddrStackRel();
	void	Decode65816AddrStackRelInd();

	bool	mbStopOnBRK;

	uint8	*mpDstState;
};

#endif
