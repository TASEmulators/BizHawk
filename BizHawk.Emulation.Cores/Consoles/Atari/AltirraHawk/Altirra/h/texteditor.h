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

#ifndef TEXTEDITOR_H
#define TEXTEDITOR_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/win32/miniwindows.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>

class IVDStream;

class IVDTextEditorStreamOut {
public:
	virtual void Write(const void *buf, size_t len) = 0;
};

class IVDTextEditorCallback {
public:
	virtual void OnTextEditorUpdated() = 0;
	virtual void OnTextEditorScrolled(int firstVisiblePara, int lastVisiblePara, int visibleParaCount, int totalParaCount) = 0;
};

class IVDTextEditorColorization {
public:
	virtual void AddTextColorPoint(int start, sint32 fore, sint32 back) = 0;
};

class IVDTextEditorColorizer {
public:
	virtual void RecolorLine(int line, const char *text, int length, IVDTextEditorColorization *colorization) = 0;
};

class IVDUIMessageFilterW32 {
public:
	virtual bool OnMessage(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam, VDZLRESULT& result) = 0;
};

class IVDTextEditor : public IVDRefCount {
public:
	virtual VDGUIHandle Create(uint32 exStyle, uint32 style, int x, int y, int cx, int cy, VDGUIHandle parent, int id) = 0;

	virtual void SetCallback(IVDTextEditorCallback *pCB) = 0;
	virtual void SetColorizer(IVDTextEditorColorizer *pColorizer) = 0;
	virtual void SetMsgFilter(IVDUIMessageFilterW32 *pFilter) = 0;

	virtual bool IsSelectionPresent() = 0;
	virtual bool IsCutPossible() = 0;
	virtual bool IsCopyPossible() = 0;
	virtual bool IsPastePossible() = 0;
	virtual bool IsUndoPossible() = 0;
	virtual bool IsRedoPossible() = 0;

	virtual int GetLineCount() = 0;
	virtual bool GetLineText(int line, vdfastvector<char>& buf) = 0;

	virtual void SetReadOnly(bool enable) = 0;
	virtual void SetWordWrap(bool enable) = 0;

	virtual int  GetCursorLine() = 0;
	virtual void SetCursorPos(int line, int offset) = 0;
	virtual void SetCursorPixelPos(int x, int y) = 0;

	virtual void RecolorLine(int line) = 0;
	virtual void RecolorAll() = 0;

	virtual bool Find(const char *text, int len, bool caseSensitive, bool wholeWord, bool searchUp) = 0;

	virtual int	GetVisibleHeight() = 0;
	virtual	int	GetParagraphForYPos(int y) = 0;
	virtual int GetVisibleLineCount() = 0;
	virtual void MakeLineVisible(int line) = 0;
	virtual void CenterViewOnLine(int line) = 0;

	virtual void SetUpdateEnabled(bool updateEnabled) = 0;

	virtual void Undo() = 0;
	virtual void Redo() = 0;
	virtual void Clear() = 0;
	virtual void Cut() = 0;
	virtual void Copy() = 0;
	virtual void Paste() = 0;
	virtual void Delete() = 0;
	virtual void DeleteSelection() = 0;
	virtual void SelectAll() = 0;

	virtual void Append(const char *s) = 0;

	virtual void Load(IVDStream& stream) = 0;
	virtual void Save(IVDTextEditorStreamOut& streamout) = 0;
};

bool VDCreateTextEditor(IVDTextEditor **ppTextEditor);

#endif
