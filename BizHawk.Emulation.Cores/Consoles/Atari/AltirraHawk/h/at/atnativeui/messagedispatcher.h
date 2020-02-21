//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI toolkit
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

#ifndef f_AT_ATNATIVEUI_MESSAGEDISPATCHER_H
#define f_AT_ATNATIVEUI_MESSAGEDISPATCHER_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/win32/miniwindows.h>

struct ATUINativeMessageDispatchContext {
	VDZHWND mhwnd;
	VDZUINT mMessageId;
	VDZWPARAM mWParam;
	VDZLPARAM mLParam;
	VDZLRESULT mResult;
	bool mbHandled;
};

struct ATUINativeMessageDispatchResult {
	VDZLRESULT mResult;
	bool mbHandled;
};

template<typename... T_SubDispatchers, typename T_Target>
ATUINativeMessageDispatchResult ATUIDispatchWndProcMessage(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam, T_Target& target) {
	ATUINativeMessageDispatchContext ctx { hwnd, msg, wParam, lParam };
	const char dummy[] = { (ATUIDispatchWndProcMessage(ctx, static_cast<T_SubDispatchers&>(target)), 0)... };
	(void)dummy;

	return { ctx.mResult, ctx.mbHandled };
}

class ATUINativeMouseMessages {
public:
	virtual void OnMouseMove(sint32 x, sint32 y);
	virtual void OnMouseDownL(sint32 x, sint32 y);
	virtual void OnMouseUpL(sint32 x, sint32 y);
	virtual void OnMouseDownR(sint32 x, sint32 y);
	virtual void OnMouseUpR(sint32 x, sint32 y);
	virtual void OnMouseWheel(sint32 x, sint32 y, float delta);
	virtual void OnMouseLeave();
};

void ATUIDispatchWndProcMessage(ATUINativeMessageDispatchContext& ctx, ATUINativeMouseMessages& target);

#endif
