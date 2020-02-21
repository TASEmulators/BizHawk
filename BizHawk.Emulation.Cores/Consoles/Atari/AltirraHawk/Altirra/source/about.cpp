//	VirtualDub - Video processing and capture application
//	Copyright (C) 1998-2001 Avery Lee
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

#include <stdafx.h>

#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include <windows.h>
#include <richedit.h>

#include "resource.h"
#include "oshelper.h"

#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include <vd2/system/w32assist.h>

namespace {
	struct StreamInData {
		const char *pos;
		int len;
	};

#pragma pack(push, 4)
	struct EDITSTREAM_fixed {
		DWORD_PTR	dwCookie;
		DWORD	dwError;
		EDITSTREAMCALLBACK pfnCallback;		// WinXP x64 build 1290 calls this at [rax+0Ch]!
	};
#pragma pack(pop)

	DWORD CALLBACK TextToRichTextControlCallback(DWORD_PTR dwCookie, LPBYTE pbBuff, LONG cb, LONG *pcb) {
		StreamInData& sd = *(StreamInData *)dwCookie;

		if (cb > sd.len)
			cb = sd.len;

		memcpy(pbBuff, sd.pos, cb);
		sd.pos += cb;
		sd.len -= cb;

		*pcb = cb;
		return 0;
	}

	typedef vdfastvector<char> tTextStream;

	void append(tTextStream& stream, const char *string) {
		stream.insert(stream.end(), string, string+strlen(string));
	}

	void append_cooked(tTextStream& stream, const char *string, const char *stringEnd, bool rtfEscape) {
		while(string != stringEnd) {
			const char *s = string;

			if (*s == '%') {
				const char *varbase = ++s;

				while(s != stringEnd && *s != '%')
					++s;

				const ptrdiff_t len = s - varbase;

				VDASSERT(len == 0);

				stream.push_back('%');

				if (s != stringEnd)
					++s;

				string = s;
				continue;
			}

			if (rtfEscape) {
				if (*s == '{' || *s == '\\' || *s == '}')
					stream.push_back('\\');

				++s;
				while(s != stringEnd && *s != '{' && *s != '\\' && *s != '}' && *s != '%')
					++s;
			} else {
				++s;
				while(s != stringEnd && *s != '%')
					++s;
			}

			stream.insert(stream.end(), string, s);
			string = s;
		}
	}

	void TextToRichTextControl(LPCTSTR resName, HWND hdlg, HWND hwndText) {
		HRSRC hResource = FindResource(NULL, resName, _T("STUFF"));

		if (!hResource)
			return;

		HGLOBAL hGlobal = LoadResource(NULL, hResource);
		if (!hGlobal)
			return;

		LPVOID lpData = LockResource(hGlobal);
		if (!lpData)
			return;

		DWORD len = SizeofResource(NULL, hResource);

		VDString tmp((const char *)lpData, (const char *)lpData + len);

		const char *const title = (const char *)tmp.c_str();
		const char *s = title;

		while(*s!='\r') ++s;

		SetWindowTextA(hdlg, VDString(title, (uint32)(s-title)).c_str());
		s+=2;

		tTextStream rtf;

		static const char header[]=
					"{\\rtf"
					"{\\fonttbl"
						"{\\f0\\fswiss MS Shell Dlg;}"
						"{\\f1\\fnil\\fcharset2 Symbol;}"
					"}"
					"{\\colortbl;\\red0\\green64\\blue128;}"
					"\\fs20 "
					;
		static const char listStart[]="{\\*\\pn\\pnlvlblt\\pnindent0{\\pntxtb\\'B7}}\\fi-240\\li540 ";

		append(rtf, header);

		bool list_active = false;

		while(*s) {
			// parse line
			int spaces = 0;

			while(*s == ' ') {
				++s;
				++spaces;
			}

			const char *end = s, *t;
			while(*end && *end != '\r' && *end != '\n')
				++end;

			// check for header, etc.
			if (*s == '[') {
				t = ++s;
				while(t != end && *t != ']')
					++t;

				append(rtf, "\\cf1\\li300\\i ");
				append_cooked(rtf, s, t, true);
				append(rtf, "\\i0\\cf0\\par ");
			} else {
				if (*s == '*') {
					if (!list_active) {
						list_active = true;
						append(rtf, listStart);
					} else
						append(rtf, "\\par ");

					append_cooked(rtf, s + 2, end, true);
				} else {
					if (list_active) {
						rtf.push_back(' ');
						if (s == end) {
							list_active = false;
							append(rtf, "\\par\\pard");
						}
					}

					if (!list_active) {
						if (spaces)
							append(rtf, "\\li300 ");
						else
							append(rtf, "\\li0 ");
					}

					append_cooked(rtf, s, end, true);

					if (!list_active)
						append(rtf, "\\par ");
				}
			}

			// skip line termination
			s = end;
			if (*s == '\r' || *s == '\n') {
				++s;
				if ((s[0] ^ s[-1]) == ('\r' ^ '\n'))
					++s;
			}
		}

		rtf.push_back('}');

		SendMessage(hwndText, EM_EXLIMITTEXT, 0, (LPARAM)rtf.size());

		EDITSTREAM_fixed es;

		StreamInData sd={rtf.data(), (int)rtf.size()};

		es.dwCookie = (DWORD_PTR)&sd;
		es.dwError = 0;
		es.pfnCallback = (EDITSTREAMCALLBACK)TextToRichTextControlCallback;

		SendMessage(hwndText, EM_STREAMIN, SF_RTF, (LPARAM)&es);
		SendMessage(hwndText, EM_SETSEL, 0, 0);
		SetFocus(hwndText);
	}
}

class ATUIDialogChangeLog final : public VDResizableDialogFrameW32 {
public:
	ATUIDialogChangeLog();

private:
	bool OnLoaded();
	void OnDataExchange(bool write);
};

void ATUIDialogChangeLog::OnDataExchange(bool write) {
	if (!write) {
		TextToRichTextControl(MAKEINTRESOURCE(IDR_CHANGES), mhdlg, GetDlgItem(mhdlg, IDC_TEXT));
	}
}

ATUIDialogChangeLog::ATUIDialogChangeLog()
	: VDResizableDialogFrameW32(IDD_CHANGE_LOG)
{
}

bool ATUIDialogChangeLog::OnLoaded() {
	mResizer.Add(IDC_TEXT, mResizer.kMC | mResizer.kAvoidFlicker | mResizer.kSuppressFontChange);
	mResizer.Add(IDOK, mResizer.kBR);

	return VDResizableDialogFrameW32::OnLoaded();
}

void ATShowChangeLog(VDGUIHandle hParent) {
	ATUIDialogChangeLog dlg;
	dlg.ShowDialog(hParent);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogCmdLineHelp final : public VDResizableDialogFrameW32 {
public:
	ATUIDialogCmdLineHelp();

private:
	bool OnLoaded() override;
	void OnSize() override;
	void OnContextMenu(uint32 id, int x, int y) override;

	void UpdateMargins();

	VDUIProxyRichEditControl mTextView;
};

ATUIDialogCmdLineHelp::ATUIDialogCmdLineHelp()
	: VDResizableDialogFrameW32(IDD_CMDLINEHELP)
{
}

bool ATUIDialogCmdLineHelp::OnLoaded() {
	SetCurrentSizeAsMinSize();

	AddProxy(&mTextView, IDC_TEXT);

	mResizer.Add(IDC_TEXT, mResizer.kMC | mResizer.kAvoidFlicker | mResizer.kSuppressFontChange);
	mResizer.Add(IDOK, mResizer.kAnchorX1_C | mResizer.kAnchorX2_C | mResizer.kAnchorY1_B | mResizer.kAnchorY2_B);

	vdfastvector<uint8> data;
	ATLoadMiscResource(IDR_CMDLINEHELP, data);

	const VDStringW& str = VDTextU8ToW(VDStringSpanA((const char *)data.begin(), (const char *)data.end()));
	VDStringA rtfStr;

	rtfStr =	"{\\rtf"
				"{\\fonttbl"
					"{\\f0\\fmodern Lucida Console;}"
				"}"
				"{\\colortbl;\\red160\\green160\\blue160;\\red248\\green248\\blue248;}"
				"\\fs18 ";

	VDStringW lineBuf;
	for(const wchar_t c : str) {
		if (c == '\r')
			continue;

		if (c == '\n') {
			// hack to find headings
			if (wcsstr(lineBuf.c_str(), L"--"))
				rtfStr += "\\cf2 ";
			else
				rtfStr += "\\cf1 ";

			mTextView.AppendEscapedRTF(rtfStr, lineBuf.c_str());
			lineBuf.clear();
			rtfStr += "\\line ";
		} else
			lineBuf.push_back(c);
	}

	rtfStr.push_back('}');
	
	mTextView.SetBackgroundColor(0);
	mTextView.SetTextRTF(rtfStr.c_str());

	UpdateMargins();

	return VDResizableDialogFrameW32::OnLoaded();
}

void ATUIDialogCmdLineHelp::OnSize() {
	VDResizableDialogFrameW32::OnSize();

	UpdateMargins();
}

void ATUIDialogCmdLineHelp::OnContextMenu(uint32 id, int x, int y) {
	static constexpr const wchar_t *kMenuItems[]={
		L"Copy\tCtrl+C",
		nullptr
	};

	if (ActivatePopupMenu(x, y, kMenuItems) >= 0) {
		if (!mTextView.IsSelectionPresent())
			mTextView.SelectAll();

		mTextView.Copy();
	}
}

void ATUIDialogCmdLineHelp::UpdateMargins() {
	mTextView.UpdateMargins(mCurrentDpi/6, mCurrentDpi/6);
}

void ATUIShowDialogCmdLineHelp(VDGUIHandle hParent) {
	ATUIDialogCmdLineHelp dlg;
	dlg.ShowDialog(hParent);
}
