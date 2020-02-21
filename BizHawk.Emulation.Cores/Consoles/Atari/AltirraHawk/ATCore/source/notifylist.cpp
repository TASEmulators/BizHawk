//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - notification list
//	Copyright (C) 2009-2018 Avery Lee
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

#include "stdafx.h"
#include <at/atcore/notifylist.h>

void ATNotifyListBase::ResetIterators() {
	for(Iterator *p = mpIteratorList; p; p = p->mpNext) {
		p->mIndex = 0;
		p->mLength = 0;
	}
}

void ATNotifyListBase::AdjustIteratorsForAdd(size_t pos) {
	for(Iterator *p = mpIteratorList; p; p = p->mpNext) {
		++p->mLength;

		if (p->mIndex >= pos)
			++p->mIndex;
	}
}

void ATNotifyListBase::AdjustIteratorsForRemove(size_t pos) {
	for(Iterator *p = mpIteratorList; p; p = p->mpNext) {
		VDASSERT(p->mLength > 0);
		--p->mLength;

		if (p->mIndex > pos)
			--p->mIndex;
	}
}

void ATNotifyListBase::AdjustIteratorsForRemove(size_t start, size_t end) {
	VDASSERT(end >= start);
	size_t delta = end - start;

	if (!delta)
		return;

	for(Iterator *p = mpIteratorList; p; p = p->mpNext) {
		VDASSERT(p->mLength >= delta);
		p->mLength -= delta;

		if (p->mIndex > start) {
			if (p->mIndex <= end)
				p->mIndex = start;
			else
				p->mIndex -= delta;
		}
	}
}
