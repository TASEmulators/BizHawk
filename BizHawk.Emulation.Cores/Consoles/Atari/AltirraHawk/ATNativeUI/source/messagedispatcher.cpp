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

#include <stdafx.h>
#include <windows.h>
#include <windowsx.h>
#include <at/atnativeui/messagedispatcher.h>

void ATUINativeMouseMessages::OnMouseMove(sint32 x, sint32 y) {}
void ATUINativeMouseMessages::OnMouseDownL(sint32 x, sint32 y) {}
void ATUINativeMouseMessages::OnMouseUpL(sint32 x, sint32 y) {}
void ATUINativeMouseMessages::OnMouseDownR(sint32 x, sint32 y) {}
void ATUINativeMouseMessages::OnMouseUpR(sint32 x, sint32 y) {}
void ATUINativeMouseMessages::OnMouseWheel(sint32 x, sint32 y, float delta) {}
void ATUINativeMouseMessages::OnMouseLeave() {}

void ATUIDispatchWndProcMessage(ATUINativeMessageDispatchContext& ctx, ATUINativeMouseMessages& target) {
	switch(ctx.mMessageId) {
		case WM_MOUSEMOVE:
			target.OnMouseMove(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_LBUTTONDOWN:
			target.OnMouseDownL(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_LBUTTONUP:
			target.OnMouseUpL(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_RBUTTONDOWN:
			target.OnMouseDownR(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_RBUTTONUP:
			target.OnMouseUpR(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_MBUTTONDOWN:
			target.OnMouseDownR(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_MBUTTONUP:
			target.OnMouseUpR(GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam));
			break;

		case WM_MOUSELEAVE:
			target.OnMouseLeave();
			break;

		case WM_MOUSEWHEEL: {
			POINT pt { GET_X_LPARAM(ctx.mLParam), GET_Y_LPARAM(ctx.mLParam) };
			ScreenToClient(ctx.mhwnd, &pt);
			target.OnMouseWheel(pt.x, pt.y, (float)GET_WHEEL_DELTA_WPARAM(ctx.mWParam) / (float)WHEEL_DELTA);
			break;
		}

		default:
			return;
	}

	ctx.mbHandled = true;
}
