//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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

#ifndef f_COMPATENGINE_H
#define f_COMPATENGINE_H

#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_vectorview.h>
#include "compatdb.h"

void ATCompatInit();
void ATCompatShutdown();

void ATCompatLoadExtDatabase(const wchar_t *ext, bool testOnly);
void ATCompatReloadExtDatabase();

bool ATCompatIsAllMuted();
void ATCompatSetAllMuted(bool mute);
bool ATCompatIsTitleMuted(const ATCompatDBTitle *title);
void ATCompatSetTitleMuted(const ATCompatDBTitle *title, bool mute);
void ATCompatUnmuteAllTitles();

bool ATCompatIsTagApplicable(ATCompatKnownTag knownTag);

struct ATCompatMarker {
	ATCompatRuleType mRuleType;
	uint64 mValue;
};

const ATCompatDBTitle *ATCompatFindTitle(const vdvector_view<const ATCompatMarker>& markers, vdfastvector<ATCompatKnownTag>& tags);

const ATCompatDBTitle *ATCompatCheck(vdfastvector<ATCompatKnownTag>& tags);
void ATCompatAdjust(VDGUIHandle h, const ATCompatKnownTag *tags, size_t numTags);

#endif
