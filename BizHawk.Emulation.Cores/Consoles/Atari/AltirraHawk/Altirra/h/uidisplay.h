//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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

#ifndef f_AT_UIDISPLAY_H
#define f_AT_UIDISPLAY_H

class IATDisplayPane {
public:
	enum { kTypeID = 'atdp' };

	virtual void ReleaseMouse() = 0;
	virtual void ToggleCaptureMouse() = 0;
	virtual void OnSize() = 0;
	virtual void ResetDisplay() = 0;
	virtual bool IsTextSelected() const = 0;
	virtual void Copy(bool enableEscaping) = 0;
	virtual void CopyFrame(bool trueAspect) = 0;
	virtual void SaveFrame(bool trueAspect, const wchar_t *path = nullptr) = 0;
	virtual void Paste(const wchar_t *s, size_t len) = 0;
	virtual void UpdateTextDisplay(bool enabled) = 0;
	virtual void UpdateTextModeFont() = 0;
	virtual void UpdateFilterMode() = 0;
};

void ATUIRegisterDisplayPane();

#endif	// f_AT_UIDISPLAY_H
