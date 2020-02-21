//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI library - generic message dialog
//	Copyright (C) 2009-2017 Avery Lee
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

#ifndef f_AT_ATNATIVEUI_GENERICDIALOG_H
#define f_AT_ATNATIVEUI_GENERICDIALOG_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/vectors.h>

enum ATUIGenericIconType {
	kATUIGenericIconType_None,
	kATUIGenericIconType_Info,
	kATUIGenericIconType_Warning,
	kATUIGenericIconType_Error
};

enum ATUIGenericResult {
	kATUIGenericResult_Cancel,
	kATUIGenericResult_OK,
	kATUIGenericResult_Allow,
	kATUIGenericResult_Deny,
	kATUIGenericResult_Yes,
	kATUIGenericResult_No,
};

enum ATUIGenericResultMask : uint32 {
	kATUIGenericResultMask_Cancel = UINT32_C(1) << kATUIGenericResult_Cancel,
	kATUIGenericResultMask_OK = UINT32_C(1) << kATUIGenericResult_OK,
	kATUIGenericResultMask_Allow = UINT32_C(1) << kATUIGenericResult_Allow,
	kATUIGenericResultMask_Deny = UINT32_C(1) << kATUIGenericResult_Deny,
	kATUIGenericResultMask_Yes = UINT32_C(1) << kATUIGenericResult_Yes,
	kATUIGenericResultMask_No = UINT32_C(1) << kATUIGenericResult_No,

	kATUIGenericResultMask_OKCancel = kATUIGenericResultMask_OK | kATUIGenericResultMask_Cancel,
	kATUIGenericResultMask_YesNoCancel = kATUIGenericResultMask_Yes | kATUIGenericResultMask_No | kATUIGenericResultMask_Cancel,
	kATUIGenericResultMask_AllowDeny = kATUIGenericResultMask_Allow | kATUIGenericResultMask_Deny
};

class ATUIGenericDialogOptions {
public:
	VDGUIHandle mhParent = nullptr;
	const wchar_t *mpMessage = nullptr;
	const wchar_t *mpCaption = nullptr;
	const wchar_t *mpTitle = nullptr;
	const char *mpIgnoreTag = nullptr;
	uint32 mValidIgnoreMask = 0;
	uint32 mResultMask = 0;
	vdrect32 mCenterTarget { 0, 0, 0, 0 };
	float mAspectLimit = 0;
	ATUIGenericIconType mIconType {};
	bool *mpCustomIgnoreFlag = nullptr;
};

void ATUISetDefaultGenericDialogCaption(const wchar_t *s);
void ATUIGenericDialogUndoAllIgnores();
ATUIGenericResult ATUIShowGenericDialog(const ATUIGenericDialogOptions& opts);
ATUIGenericResult ATUIShowGenericDialogAutoCenter(const ATUIGenericDialogOptions& opts);
bool ATUIConfirm(VDGUIHandle hParent, const char *ignoreTag, const wchar_t *message, const wchar_t *title = nullptr);

#endif
