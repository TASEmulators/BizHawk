//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

#ifndef f_AT_ATUI_CONSTANTS_H
#define f_AT_ATUI_CONSTANTS_H

#include <vd2/system/vdtypes.h>

enum ATUITouchMode : uint32 {
	kATUITouchMode_Default,
	kATUITouchMode_Immediate,
	kATUITouchMode_Direct,
	kATUITouchMode_VerticalPan,
	kATUITouchMode_2DPan,
	kATUITouchMode_2DPanSmooth,
	kATUITouchMode_MultiTouch,
	kATUITouchMode_MultiTouchImmediate,
	kATUITouchMode_Dynamic,
	kATUITouchMode_MultiTouchDynamic
};

enum ATUICursorImage : uint32 {
	kATUICursorImage_None,
	kATUICursorImage_Hidden,
	kATUICursorImage_Arrow,
	kATUICursorImage_IBeam,
	kATUICursorImage_Cross,
	kATUICursorImage_Query,
	kATUICursorImage_Target,
	kATUICursorImage_TargetOff
};

#endif
