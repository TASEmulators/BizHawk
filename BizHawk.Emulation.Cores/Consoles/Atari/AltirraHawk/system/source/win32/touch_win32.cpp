//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2004 Avery Lee, All Rights Reserved.
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

#include <stdafx.h>
#include <vd2/system/win32/touch.h>
#include <tchar.h>

BOOL WINAPI VDPhysicalToLogicalPointW32(HWND hwnd, LPPOINT lpPoint) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "PhysicalToLogicalPoint");

	return pfn && ((BOOL (WINAPI *)(HWND, LPPOINT))pfn)(hwnd, lpPoint);
}

LRESULT WINAPI VDPackTouchHitTestingProximityEvaluationW32(const TOUCH_HIT_TESTING_INPUT *input, const TOUCH_HIT_TESTING_PROXIMITY_EVALUATION *eval) {
	typedef LRESULT (WINAPI *tpPackTouchHitTestingProximityEvaluation)(const TOUCH_HIT_TESTING_INPUT *input, const TOUCH_HIT_TESTING_PROXIMITY_EVALUATION *eval);
	static tpPackTouchHitTestingProximityEvaluation spfn = (tpPackTouchHitTestingProximityEvaluation)GetProcAddress(GetModuleHandle(_T("user32")), "PackTouchHitTestingProximityEvaluation");

	return spfn ? spfn(input, eval) : 0;
}

BOOL WINAPI VDRegisterTouchHitTestingWindowW32(HWND hwnd, ULONG value) {
	typedef LRESULT (WINAPI *tpRegisterTouchHitTestingWindow)(HWND hwnd, ULONG value);
	static tpRegisterTouchHitTestingWindow spfn = (tpRegisterTouchHitTestingWindow)GetProcAddress(GetModuleHandle(_T("user32")), "RegisterTouchHitTestingWindow");

	return spfn && spfn(hwnd, value);
}

BOOL WINAPI VDEvaluateProximityToRectW32(const RECT *controlBoundingBox, const TOUCH_HIT_TESTING_INPUT *pHitTestingInput, TOUCH_HIT_TESTING_PROXIMITY_EVALUATION *pProximityEval) {
	typedef BOOL (WINAPI *tpEvaluateProximityToRect)(const RECT *controlBoundingBox, const TOUCH_HIT_TESTING_INPUT *pHitTestingInput, TOUCH_HIT_TESTING_PROXIMITY_EVALUATION *pProximityEval);
	static tpEvaluateProximityToRect spfn = (tpEvaluateProximityToRect)GetProcAddress(GetModuleHandle(_T("user32")), "EvaluateProximityToRect");

	return spfn && spfn(controlBoundingBox, pHitTestingInput, pProximityEval);
};

BOOL WINAPI VDRegisterTouchWindowW32(HWND hwnd, ULONG flags) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "RegisterTouchWindow");

	return pfn && ((BOOL (WINAPI *)(HWND, ULONG))pfn)(hwnd, flags);
}

BOOL WINAPI VDUnregisterTouchWindowW32(HWND hwnd) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "UnregisterTouchWindow");

	return pfn && ((BOOL (WINAPI *)(HWND))pfn)(hwnd);
}

BOOL WINAPI VDGetTouchInputInfoW32(HTOUCHINPUT hTouchInput, UINT cInputs, PTOUCHINPUT pInputs, int cbSize) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "GetTouchInputInfo");

	return pfn && ((BOOL (WINAPI *)(HTOUCHINPUT, UINT, PTOUCHINPUT, int))pfn)(hTouchInput, cInputs, pInputs, cbSize);
}

BOOL WINAPI VDCloseTouchInputHandleW32(HTOUCHINPUT hTouchInput) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "CloseTouchInputHandle");

	return pfn && ((BOOL (WINAPI *)(HTOUCHINPUT))pfn)(hTouchInput);
}

BOOL WINAPI VDSetGestureConfigW32(HWND hwnd, DWORD dwReserved, UINT cIDs, PGESTURECONFIG pGestureConfig, UINT cbSize) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "SetGestureConfig");

	return pfn && ((BOOL (WINAPI *)(HWND, DWORD, UINT, PGESTURECONFIG, UINT))pfn)(hwnd, dwReserved, cIDs, pGestureConfig, cbSize);
}
