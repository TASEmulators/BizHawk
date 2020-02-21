//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI library - drag and drop support
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

#ifndef f_AT_ATNATIVE_DRAGDROP_H
#define f_AT_ATNATIVE_DRAGDROP_H

#include <vd2/system/atomic.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_vectorview.h>

#pragma warning(push)
#pragma warning(disable: 4091)		// ShlObj.h(1151): warning C4091: 'typedef ': ignored on left of 'tagGPFIDL_FLAGS' when no variable is declared
#pragma warning(disable: 4768)		// ShlObj.h(1065): warning C4768: __declspec attributes before linkage specification are ignored
#include <windows.h>
#include <ShlObj.h>
#pragma warning(pop)

struct ATUIDragDropFormatsW32 {
	UINT mContents;
	UINT mDescriptorA;
	UINT mDescriptorW;
	UINT mShellIdList;
	UINT mDropDescription;
	UINT mDragWindow;
};

const ATUIDragDropFormatsW32& ATUIInitDragDropFormatsW32();

struct ATAutoStgMediumW32 : public STGMEDIUM {
	ATAutoStgMediumW32() {
		tymed = TYMED_NULL;
		pUnkForRelease = nullptr;
	}

	ATAutoStgMediumW32(const ATAutoStgMediumW32&) = delete;
	ATAutoStgMediumW32& operator=(const ATAutoStgMediumW32&) = delete;
	~ATAutoStgMediumW32() {
		Clear();
	}

	void Clear();
};

void ATReadDragDropFileDescriptorsW32(vdfastvector<FILEDESCRIPTORW>& dst, const FILEGROUPDESCRIPTORW *src);
void ATReadDragDropFileDescriptorsW32(vdfastvector<FILEDESCRIPTORW>& dst, const FILEGROUPDESCRIPTORA *src);

void ATUIClearDropDescriptionW32(IDataObject *obj);
void ATUISetDropDescriptionW32(IDataObject *obj, DROPIMAGETYPE dropImageType, const wchar_t *templateStr, const wchar_t *insertStr);

bool ATUIGetVFSPathFromShellIDListW32(HGLOBAL hGlobal, VDStringW& vfsPath);

class ATUIDropTargetBaseW32 : public IDropTarget {
	ATUIDropTargetBaseW32(const ATUIDropTargetBaseW32&) = delete;
	ATUIDropTargetBaseW32& operator=(const ATUIDropTargetBaseW32&) = delete;
public:
	ATUIDropTargetBaseW32() = default;
	virtual ~ATUIDropTargetBaseW32() = default;

	ULONG STDMETHODCALLTYPE AddRef() override;
	ULONG STDMETHODCALLTYPE Release() override;
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObj) override;

	// HRESULT STDMETHODCALLTYPE DragEnter(IDataObject *pDataObj, DWORD grfKeyState, POINTL pt, DWORD *pdwEffect);
	HRESULT STDMETHODCALLTYPE DragOver(DWORD grfKeyState, POINTL pt, DWORD *pdwEffect) override;
	HRESULT STDMETHODCALLTYPE DragLeave() override;
	// HRESULT STDMETHODCALLTYPE Drop(IDataObject *pDataObj, DWORD grfKeyState, POINTL pt, DWORD *pdwEffect);

protected:
	virtual void OnDragOver(sint32 x, sint32 y);
	virtual void OnDragLeave();

	VDAtomicInt mRefCount = 0;
	DWORD mDropEffect = DROPEFFECT_NONE;
};

#endif
