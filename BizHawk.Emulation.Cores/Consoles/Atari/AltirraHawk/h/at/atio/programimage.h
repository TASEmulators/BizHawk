//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - binary program image definitions
//	Copyright (C) 2008-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_ATIO_PROGRAMIMAGE_H
#define f_AT_ATIO_PROGRAMIMAGE_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_vectorview.h>

enum class ATProgramBlockType : uint8 {
	None,
	Error,
	DOS,				// [FFFF] lo16 hi16
	SDXAbsolute,		// FFFA lo16 hi16
	SDXRelocatable,		// FFFE segno8 flags8 baseaddr16 length16
	SDXFixup,			// FFFD ... FC
	SDXSymRef,			// FFFB name[8] ... FC
	SDXSymDef			// FFFC name[8]
};

struct ATProgramBlock {
	ATProgramBlockType mType;
	uint32 mImageOffset;
	uint16 mBaseAddress;
	uint32 mLength;
};

bool ATParseProgramImage(const void *src, uint32 len, vdfastvector<ATProgramBlock>& blocks);
bool ATCheckProgramOverlap(uint32 start, uint32 len, const vdvector_view<const ATProgramBlock>& blocks);

#endif	// f_AT_ATIO_PROGRAMIMAGE_H
