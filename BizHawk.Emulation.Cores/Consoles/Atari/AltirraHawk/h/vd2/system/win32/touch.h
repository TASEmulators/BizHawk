//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2014 Avery Lee, All Rights Reserved.
//
//	Beginning with 1.6.0, the VirtualDub system library is licensed
//	differently than the remainder of VirtualDub.  This particular file is
//	thus licensed as follows (the "zlib" license):
//
//	This software is provided 'as-is', without any express or implied
//	warranty.  In no event will the authors be held liable for any
//	damages arising from the use of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it
//	freely, subject to the following restrictions:
//
//	1.	The origin of this software must not be misrepresented; you must
//		not claim that you wrote the original software. If you use this
//		software in a product, an acknowledgment in the product
//		documentation would be appreciated but is not required.
//	2.	Altered source versions must be plainly marked as such, and must
//		not be misrepresented as being the original software.
//	3.	This notice may not be removed or altered from any source
//		distribution.

#ifndef f_VD2_SYSTEM_WIN32_TOUCH_H
#define f_VD2_SYSTEM_WIN32_TOUCH_H

#include <windows.h>

#ifndef WM_TOUCH
	#define WM_TOUCH			0x0240

	#define TOUCHEVENTF_MOVE		0x0001
	#define TOUCHEVENTF_DOWN		0x0002
	#define TOUCHEVENTF_UP			0x0004
	#define TOUCHEVENTF_INRANGE		0x0008
	#define TOUCHEVENTF_PRIMARY		0x0010
	#define TOUCHEVENTF_NOCOALESCE	0x0020
	#define TOUCHEVENTF_PALM		0x0080

	#define TOUCHINPUTMASKF_CONTACTAREA		0x0004
	#define TOUCHINPUTMASKF_EXTRAINFO		0x0002
	#define TOUCHINPUTMASKF_TIMEFROMSYSTEM	0x0001

	typedef struct tagTOUCHINPUT {
		LONG		x;
		LONG		y;
		HANDLE		hSource;
		DWORD		dwID;
		DWORD		dwFlags;
		DWORD		dwMask;
		DWORD		dwTime;
		ULONG_PTR	dwExtraInfo;
		DWORD		cxContact;
		DWORD		cyContact;
	} TOUCHINPUT, *PTOUCHINPUT;

	DECLARE_HANDLE(HTOUCHINPUT);
#endif

#ifndef WM_TABLET_DEFBASE
	#define WM_TABLET_DEFBASE						0x02C0
	#define WM_TABLET_QUERYSYSTEMGESTURESTATUS		(WM_TABLET_DEFBASE + 12)

	#define TABLET_DISABLE_PRESSANDHOLD			0x00000001
	#define TABLET_DISABLE_FLICKS				0x00010000
#endif

#ifndef WM_TOUCHHITTESTING
	#define WM_TOUCHHITTESTING	0x024D

	#define TOUCH_HIT_TESTING_PROXIMITY_FARTHEST 0xFFF

	#define TOUCH_HIT_TESTING_DEFAULT 0
	#define TOUCH_HIT_TESTING_CLIENT 1
	#define TOUCH_HIT_TESTING_NONE 2

	typedef struct tagTOUCH_HIT_TESTING_PROXIMITY_EVALUATION {
		UINT16 score;
		POINT adjustedPoint;
	} TOUCH_HIT_TESTING_PROXIMITY_EVALUATION, *PTOUCH_HIT_TESTING_PROXIMITY_EVALUATION;

	typedef struct tagTOUCH_HIT_TESTING_INPUT {
		UINT32 pointerId;
		POINT point;
		RECT boundingBox;
		RECT nonOccludedBoundingBox;
		UINT32 orientation;
	} TOUCH_HIT_TESTING_INPUT, *PTOUCH_HIT_TESTING_INPUT;
#endif

#ifndef TWF_WANTPALM
#define TWF_WANTPALM 0x00000002
#endif

#ifndef GC_ALLGESTURES
	#define GC_ALLGESTURES	0x00000001

	#define GID_PAN				4
	#define GID_PRESSANDTAP		7

	#define GC_PAN		0x00000001
	#define GC_PAN_WITH_SINGLE_FINGER_VERTICALLY		0x00000002
	#define GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY		0x00000004
	#define GC_PAN_WITH_GUTTER	0x00000008

	#define	GC_PRESSANDTAP		0x00000001

	#define WM_GESTURENOTIFY	0x011A

	typedef struct tagGESTURECONFIG {
		DWORD dwID;
		DWORD dwWant;
		DWORD dwBlock;
	} GESTURECONFIG, *PGESTURECONFIG;

	typedef struct tagGESTURENOTIFYSTRUCT {
		UINT cbSize;
		DWORD dwFlags;
		HWND hwndTarget;
		POINTS ptsLocation;
		DWORD dwInstanceID;
	} GESTURENOTIFYSTRUCT, *PGESTURENOTIFYSTRUCT;
#endif

BOOL WINAPI VDPhysicalToLogicalPointW32(HWND hwnd, LPPOINT lpPoint);

LRESULT WINAPI VDPackTouchHitTestingProximityEvaluationW32(const TOUCH_HIT_TESTING_INPUT *input, const TOUCH_HIT_TESTING_PROXIMITY_EVALUATION *eval);
BOOL WINAPI VDRegisterTouchHitTestingWindowW32(HWND hwnd, ULONG value);
BOOL WINAPI VDEvaluateProximityToRectW32(const RECT *controlBoundingBox, const TOUCH_HIT_TESTING_INPUT *pHitTestingInput, TOUCH_HIT_TESTING_PROXIMITY_EVALUATION *pProximityEval);

BOOL WINAPI VDRegisterTouchWindowW32(HWND hwnd, ULONG flags);
BOOL WINAPI VDUnregisterTouchWindowW32(HWND hwnd);

BOOL WINAPI VDGetTouchInputInfoW32(HTOUCHINPUT hTouchInput, UINT cInputs, PTOUCHINPUT pInputs, int cbSize);
BOOL WINAPI VDCloseTouchInputHandleW32(HTOUCHINPUT hTouchInput);

BOOL WINAPI VDSetGestureConfigW32(HWND hwnd, DWORD dwReserved, UINT cIDs, PGESTURECONFIG pGestureConfig, UINT cbSize);

#endif
