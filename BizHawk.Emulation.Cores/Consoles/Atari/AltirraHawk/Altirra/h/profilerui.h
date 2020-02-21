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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#ifndef f_AT_PROFILERUI_H
#define f_AT_PROFILERUI_H

#include <vd2/system/refcount.h>
#include <vd2/system/vdstl_vectorview.h>

class ATUINativeWindowProxy;
class ATUINativeWindow;
class ATProfileSession;
struct ATProfileFrame;
struct ATProfileMergedFrame;

class IATUIProfileView : public IVDRefCount {
public:
	virtual ATUINativeWindow *AsUINativeWindow() = 0;

	virtual bool Create(ATUINativeWindowProxy *parent, uint32 id) = 0;
	virtual void SetData(const ATProfileSession *session, const ATProfileFrame *frame, ATProfileMergedFrame *mergedFrame) = 0;
};

void ATUICreateProfileView(IATUIProfileView **view);

vdvector_view<const uint32> ATUIGetProfilerCounterModeMenuIds();

#endif
