//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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
#include <windows.h>
#include <ole2.h>
#include <shellapi.h>

#pragma warning(push)
#pragma warning(disable: 4768)		// ShlObj.h(1065): warning C4768: __declspec attributes before linkage specification are ignored
#include <shlobj.h>
#pragma warning(pop)

#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/w32assist.h>
#include <vd2/system/vdstl_vectorview.h>
#include <vd2/Dita/services.h>
#include <at/atcore/media.h>
#include <at/atcore/vfs.h>
#include <at/atio/diskfs.h>
#include <at/atio/diskfsutil.h>
#include <at/atio/diskimage.h>
#include <at/atio/image.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/dragdrop.h>
#include <at/atnativeui/messageloop.h>
#include "resource.h"
#include "disk.h"
#include "simulator.h"
#include "uiaccessors.h"
#include "uifilefilters.h"
#include "options.h"
#include "oshelper.h"

extern ATSimulator g_sim;

void ATUIShowDialogDiskExplorer(VDGUIHandle h, IATDiskImage *image, const wchar_t *imageName, bool writeEnabled, bool autoFlush);

enum ATDiskFormatFileSystem {
	kATDiskFFS_None,
	kATDiskFFS_DOS2,
	kATDiskFFS_DOS1,
	kATDiskFFS_DOS3,
	kATDiskFFS_MyDOS,
	kATDiskFFS_SDFS,
};

class ATNewDiskDialog : public VDDialogFrameW32 {
public:
	ATNewDiskDialog();
	~ATNewDiskDialog();

	uint32 GetSectorCount() const { return mSectorCount; }
	uint32 GetBootSectorCount() const { return mBootSectorCount; }
	uint32 GetSectorSize() const { return mSectorSize; }
	ATDiskFormatFileSystem GetFormatFFS() const { return mDiskFFS; }

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
	bool OnCommand(uint32 id, uint32 extcode);
	void UpdateEnables();

	int	mFormatTypeIndex;
	ATDiskFormatFileSystem mDiskFFS;
	uint32	mSectorCount;
	uint32	mBootSectorCount;
	uint32	mSectorSize;

	struct FormatType {
		uint32 mSectorSize;
		uint32 mSectorCount;
		const wchar_t *mpTag;
	};

	static const FormatType kFormatTypes[];
};

const ATNewDiskDialog::FormatType ATNewDiskDialog::kFormatTypes[] = {
	{ 0, 0, L"Custom" },
	{ 128,  720, L"Single density (720 sectors, 128 bytes/sector)" },
	{ 128, 1040, L"Medium density (1040 sectors, 128 bytes/sector)" },
	{ 256,  720, L"Double density (720 sectors, 256 bytes/sector)" },
	{ 256, 1440, L"Double-sided DD (1440 sectors, 256 bytes/sector)" },
	{ 256, 2880, L"DSDD 80 tracks (2880 sectors, 256 bytes/sector)" },
};

ATNewDiskDialog::ATNewDiskDialog()
	: VDDialogFrameW32(IDD_CREATE_DISK)
	, mFormatTypeIndex(1)
	, mDiskFFS(kATDiskFFS_None)
	, mSectorCount(720)
	, mBootSectorCount(3)
	, mSectorSize(128)
{
}

ATNewDiskDialog::~ATNewDiskDialog() {
}

bool ATNewDiskDialog::OnLoaded() {
	for(const auto& formatType : kFormatTypes)
		CBAddString(IDC_FORMAT, formatType.mpTag);

	CBAddString(IDC_FILESYSTEM, L"None (unformatted)");
	CBAddString(IDC_FILESYSTEM, L"DOS 2.0/2.5");
	CBAddString(IDC_FILESYSTEM, L"DOS 1");
	CBAddString(IDC_FILESYSTEM, L"DOS 3");
	CBAddString(IDC_FILESYSTEM, L"MyDOS");
	CBAddString(IDC_FILESYSTEM, L"SpartaDOS File System (SDFS)");

	return VDDialogFrameW32::OnLoaded();
}

void ATNewDiskDialog::OnDataExchange(bool write) {
	ExchangeControlValueUint32(write, IDC_BOOT_SECTOR_COUNT, mBootSectorCount, 0, 255);
	ExchangeControlValueUint32(write, IDC_SECTOR_COUNT, mSectorCount, mBootSectorCount, 65535);

	if (write) {
		mSectorSize = 128;
		if (IsButtonChecked(IDC_SECTOR_SIZE_256))
			mSectorSize = 256;
		else if (IsButtonChecked(IDC_SECTOR_SIZE_512)) {
			mSectorSize = 512;
			mBootSectorCount = 0;
		}

		bool supported = false;
		switch(CBGetSelectedIndex(IDC_FILESYSTEM)) {
			case 0:
			default:
				mDiskFFS = kATDiskFFS_None;
				supported = true;
				break;

			case 1:
				// DOS 2.0S: 720 sectors, SD or DD (yes, DOS 2.0S supports DD!)
				// DOS 2.5: adds 1040 sectors @ 128bps (ED)
				if (mBootSectorCount != 3)
					break;

				if (mSectorSize != 128 && mSectorSize != 256)
					break;

				if (mSectorCount == 1040) {
					if (mSectorSize != 128)
						break;
				} else if (mSectorCount != 720)
					break;

				mDiskFFS = kATDiskFFS_DOS2;
				supported = true;
				break;

			case 2:
				if (mSectorCount != 720 || mSectorSize != 128)
					break;

				mDiskFFS = kATDiskFFS_DOS1;
				supported = true;
				break;

			case 3:
				if ((mSectorCount != 720 && mSectorCount != 1040) || mSectorSize != 128)
					break;

				mDiskFFS = kATDiskFFS_DOS3;
				supported = true;
				break;

			case 4:
				if (mSectorCount < 720 || (mSectorSize != 128 && mSectorSize != 256))
					break;

				mDiskFFS = kATDiskFFS_MyDOS;
				supported = true;
				break;

			case 5:
				if (mSectorCount < 16)
					break;

				mDiskFFS = kATDiskFFS_SDFS;
				supported = true;
				break;
		}

		if (!supported) {
			ShowError(L"The specified disk geometry is not supported for the selected filesystem.", L"Altirra Error");
			FailValidation(IDC_FILESYSTEM);
			return;
		}
	} else {
		CheckButton(IDC_SECTOR_SIZE_128, mSectorSize == 128);
		CheckButton(IDC_SECTOR_SIZE_256, mSectorSize == 256);
		CheckButton(IDC_SECTOR_SIZE_512, mSectorSize == 512);
		CBSetSelectedIndex(IDC_FORMAT, mFormatTypeIndex);
		UpdateEnables();

		CBSetSelectedIndex(IDC_FILESYSTEM, mDiskFFS);
	}
}

bool ATNewDiskDialog::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_FORMAT && extcode == CBN_SELCHANGE) {
		int formatTypeIndex = CBGetSelectedIndex(IDC_FORMAT);

		if (mFormatTypeIndex != formatTypeIndex) {
			mFormatTypeIndex = formatTypeIndex;
			UpdateEnables();

			if (formatTypeIndex > 0 && formatTypeIndex < (int)vdcountof(kFormatTypes)) {
				const auto& formatType = kFormatTypes[formatTypeIndex];

				mSectorCount = formatType.mSectorCount;
				mSectorSize = formatType.mSectorSize;
				mBootSectorCount = 3;
				OnDataExchange(false);
			}
		}
	} else if ((id == IDC_SECTOR_SIZE_128 || id == IDC_SECTOR_SIZE_256 || id == IDC_SECTOR_SIZE_512)
		&& extcode == BN_CLICKED) {
		UpdateEnables();
	}

	return false;
}

void ATNewDiskDialog::UpdateEnables() {
	bool custom = (CBGetSelectedIndex(IDC_FORMAT) == 0);

	EnableControl(IDC_SECTOR_SIZE_128, custom);
	EnableControl(IDC_SECTOR_SIZE_256, custom);
	EnableControl(IDC_SECTOR_SIZE_512, custom);
	EnableControl(IDC_SECTOR_COUNT, custom);
	EnableControl(IDC_BOOT_SECTOR_COUNT, custom && !IsButtonChecked(IDC_SECTOR_SIZE_512));
}

///////////////////////////////////////////////////////////////////////////////

class IATDiskDriveDropTargetNotify {
public:
	virtual int FindDropTarget(sint32 x, sint32 y) const = 0;
	virtual void SetDropTargetHighlight(int index) = 0;
	virtual void OnDrop(int index, const wchar_t *path, const wchar_t *imageName, IVDRandomAccessStream& stream) = 0;
};

class ATDiskDriveDropTargetW32 final : public ATUIDropTargetBaseW32 {
public:
	ATDiskDriveDropTargetW32(HWND hwnd, IATDiskDriveDropTargetNotify *notify);

	void Detach() { mpNotify = nullptr; }

	HRESULT STDMETHODCALLTYPE DragEnter(IDataObject *pDataObj, DWORD grfKeyState, POINTL pt, DWORD *pdwEffect) override;
	HRESULT STDMETHODCALLTYPE Drop(IDataObject *pDataObj, DWORD grfKeyState, POINTL pt, DWORD *pdwEffect) override;

protected:
	void OnDragOver(sint32 x, sint32 y) override;
	void OnDragLeave() override;

	void ProcessFileDescriptors(int index, IDataObject *pDataObj, const vdvector_view<const FILEDESCRIPTORW>& files);
	void WriteFromStorageMedium(int index, STGMEDIUM *medium, const wchar_t *filename, uint32 len);

	HWND mhwnd = nullptr;

	IATDiskDriveDropTargetNotify *mpNotify = nullptr;
};

ATDiskDriveDropTargetW32::ATDiskDriveDropTargetW32(HWND hwnd, IATDiskDriveDropTargetNotify *notify)
	: mpNotify(notify)
{
}

HRESULT STDMETHODCALLTYPE ATDiskDriveDropTargetW32::DragEnter(IDataObject *pDataObj, DWORD grfKeyState, POINTL pt, DWORD *pdwEffect) {
	mDropEffect = DROPEFFECT_NONE;

	if ((GetWindowLong(mhwnd, GWL_STYLE) & WS_DISABLED) || !mpNotify) {
		*pdwEffect = mDropEffect;
		return S_OK;
	}

	const auto& formats = ATUIInitDragDropFormatsW32();

	FORMATETC etc {};
	etc.cfFormat = CF_HDROP;
	etc.dwAspect = DVASPECT_CONTENT;
	etc.lindex = -1;
	etc.ptd = NULL;
	etc.tymed = TYMED_HGLOBAL;

	ATAutoStgMediumW32 medium;
	medium.tymed = TYMED_HGLOBAL;
	medium.hGlobal = nullptr;

	HRESULT hr = pDataObj->GetData(&etc, &medium);

	if (hr == S_OK) {
		// Check if more than one file is present.
		HDROP hdrop = (HDROP)medium.hGlobal;
		UINT count = DragQueryFileW(hdrop, 0xFFFFFFFF, NULL, 0);

		if (count != 1)
			hr = E_FAIL;
	} else {
		medium.Clear();
		etc.cfFormat = formats.mDescriptorW;
		hr = pDataObj->GetData(&etc, &medium);

		if (hr == S_OK) {
			FILEGROUPDESCRIPTORW *descriptors = (FILEGROUPDESCRIPTORW *)GlobalLock(medium.hGlobal);

			if (descriptors) {
				if (descriptors->cItems != 1)
					hr = E_FAIL;

				GlobalUnlock(medium.hGlobal);
			}
		} else {
			medium.Clear();
			etc.cfFormat = formats.mDescriptorA;
			hr = pDataObj->GetData(&etc, &medium);

			if (hr == S_OK) {
				FILEGROUPDESCRIPTORA *descriptors = (FILEGROUPDESCRIPTORA *)GlobalLock(medium.hGlobal);

				if (descriptors) {
					if (descriptors->cItems != 1)
						hr = E_FAIL;

					GlobalUnlock(medium.hGlobal);
				}
			}
		}
	}

	if (hr == S_OK)
		mDropEffect = DROPEFFECT_COPY;

	*pdwEffect = mDropEffect;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE ATDiskDriveDropTargetW32::Drop(IDataObject *pDataObj, DWORD grfKeyState, POINTL pt, DWORD *pdwEffect) {
	if (mpNotify)
		mpNotify->SetDropTargetHighlight(-1);

	if ((GetWindowLong(mhwnd, GWL_STYLE) & WS_DISABLED) || !mpNotify)
		return S_OK;

	int index = mpNotify->FindDropTarget(pt.x, pt.y);
	if (index < 0)
		return S_OK;

	const auto& formats = ATUIInitDragDropFormatsW32();

	// pull filenames
	FORMATETC etc {};
	etc.cfFormat = formats.mShellIdList;
	etc.dwAspect = DVASPECT_CONTENT;
	etc.lindex = -1;
	etc.ptd = NULL;
	etc.tymed = TYMED_HGLOBAL;

	ATAutoStgMediumW32 medium;
	medium.tymed = TYMED_HGLOBAL;
	medium.hGlobal = NULL;
	HRESULT hr = pDataObj->GetData(&etc, &medium);

	if (SUCCEEDED(hr)) {
		VDStringW vfsPath;
		if (ATUIGetVFSPathFromShellIDListW32(medium.hGlobal, vfsPath)) {
			try {
				// try to open the .zip file via VFS -- if it fails, we bail silently and fall
				// through to file/stream based path
				vdrefptr<ATVFSFileView> view;
				ATVFSOpenFileView(vfsPath.c_str(), false, ~view);

				try {
					mpNotify->OnDrop(index, vfsPath.c_str(), view->GetFileName(), view->GetStream());
				} catch(const MyError& e) {
					e.post(mhwnd, "Altirra Error");
				}

				return S_OK;
			} catch(const MyError&) {
				// Eat VFS errors. We assume that it's something like a .zip we can't handle,
				// and fall through.
			}
		}
	}

	// try HDROP (drag/drop file list)
	etc.cfFormat = CF_HDROP;

	medium.Clear();
	medium.tymed = TYMED_HGLOBAL;
	medium.hGlobal = NULL;
	hr = pDataObj->GetData(&etc, &medium);

	if (SUCCEEDED(hr)) {
		HDROP hdrop = (HDROP)medium.hGlobal;

		UINT count = DragQueryFileW(hdrop, 0xFFFFFFFF, NULL, 0);

		vdfastvector<wchar_t> nameBuf;
		try {
			for(UINT i = 0; i < count; ++i) {
				UINT len = DragQueryFileW(hdrop, i, NULL, 0);

				nameBuf.clear();
				nameBuf.resize(len+1, 0);

				if (DragQueryFileW(hdrop, i, nameBuf.data(), len+1)) {
					VDFileStream fs(nameBuf.data());

					mpNotify->OnDrop(index, nameBuf.data(), nameBuf.data(), fs);
				}
			}
		} catch(const MyError& e) {
			e.post(mhwnd, "Altirra Error");
		}

		return S_OK;
	}

	// try wide-char descriptor
	medium.Clear();
	etc.cfFormat = formats.mDescriptorW;
	hr = pDataObj->GetData(&etc, &medium);

	if (SUCCEEDED(hr)) {
		FILEGROUPDESCRIPTORW *descriptors = (FILEGROUPDESCRIPTORW *)GlobalLock(medium.hGlobal);

		if (descriptors) {
			vdfastvector<FILEDESCRIPTORW> files;
			ATReadDragDropFileDescriptorsW32(files, descriptors);

			ProcessFileDescriptors(index, pDataObj, vdvector_view<const FILEDESCRIPTORW>(files.data(), files.size()));

			GlobalUnlock(medium.hGlobal);
		}
	}

	// try ANSI descriptor
	medium.Clear();
	etc.cfFormat = formats.mDescriptorA;
	hr = pDataObj->GetData(&etc, &medium);

	if (SUCCEEDED(hr)) {
		FILEGROUPDESCRIPTORA *descriptors = (FILEGROUPDESCRIPTORA *)GlobalLock(medium.hGlobal);

		if (descriptors) {
			vdfastvector<FILEDESCRIPTORW> files;
			ATReadDragDropFileDescriptorsW32(files, descriptors);

			ProcessFileDescriptors(index, pDataObj, vdvector_view<const FILEDESCRIPTORW>(files.data(), files.size()));

			GlobalUnlock(medium.hGlobal);
		}

		return S_OK;
	}
	return S_OK;
}

void ATDiskDriveDropTargetW32::OnDragOver(sint32 x, sint32 y) {
	if (mpNotify) {
		int index = mpNotify->FindDropTarget(x, y);

		mpNotify->SetDropTargetHighlight(index);
	}
}

void ATDiskDriveDropTargetW32::OnDragLeave() {
	if (mpNotify)
		mpNotify->SetDropTargetHighlight(-1);
}

void ATDiskDriveDropTargetW32::ProcessFileDescriptors(int index, IDataObject *pDataObj, const vdvector_view<const FILEDESCRIPTORW>& files) {
	const auto& formats = ATUIInitDragDropFormatsW32();

	// read out the files, one at a time
	try {
		LONG fileIndex = 0;
		for(const auto& file : files) {
			uint64 len64 = file.nFileSizeLow + ((uint64)file.nFileSizeHigh << 32);

			if (len64 > 0x4000000)
				continue;

			FORMATETC etc {};
			etc.cfFormat = formats.mContents;
			etc.dwAspect = DVASPECT_CONTENT;
			etc.lindex = fileIndex++;
			etc.ptd = NULL;
			etc.tymed = TYMED_HGLOBAL | TYMED_ISTREAM;

			ATAutoStgMediumW32 medium2;
			medium2.tymed = TYMED_HGLOBAL;
			medium2.hGlobal = NULL;
			HRESULT hr = pDataObj->GetData(&etc, &medium2);

			if (SUCCEEDED(hr))
				WriteFromStorageMedium(index, &medium2, file.cFileName, (uint32)len64);
		}
	} catch(const MyError& e) {
		e.post(mhwnd, "Altirra Error");
	}
}

void ATDiskDriveDropTargetW32::WriteFromStorageMedium(int index, STGMEDIUM *medium, const wchar_t *filename, uint32 len32) {
	vdrefptr<IStream> stream;
	if (medium->tymed == TYMED_HGLOBAL) {
		CreateStreamOnHGlobal(medium->hGlobal, FALSE, ~stream);
	} else {
		stream = medium->pstm;
	}

	if (!stream)
		return;

	vdfastvector<uint8> buf;

	LARGE_INTEGER dist;
	dist.QuadPart = 0;
	HRESULT hr = stream->Seek(dist, STREAM_SEEK_SET, NULL);

	if (SUCCEEDED(hr)) {
		buf.resize(len32);

		ULONG actual;
		hr = stream->Read(buf.data(), len32, &actual);

		if (SUCCEEDED(hr)) {
			VDMemoryStream ms(buf.data(), len32);

			mpNotify->OnDrop(index, nullptr, filename, ms);
		}
	}
}

///////////////////////////////////////////////////////////////////////////////

class ATDiskDriveDialog final : public VDResizableDialogFrameW32, public IATDiskDriveDropTargetNotify {
public:
	ATDiskDriveDialog();
	~ATDiskDriveDialog();

	bool OnLoaded() override;
	void OnDestroy() override;
	void OnEnable(bool enable) override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 extcode) override;

public:
	int FindDropTarget(sint32 x, sint32 y) const override;
	void SetDropTargetHighlight(int index) override;
	void OnDrop(int index, const wchar_t *path, const wchar_t *imageName, IVDRandomAccessStream& stream) override;

protected:
	VDZINT_PTR DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) override;
	void OnDpiChanged() override;
	bool PreNCDestroy() override;

	void AttachToInterfaces();
	void DetachFromInterfaces();
	void UpdateFonts();
	void UpdateActionButtons();
	void RefreshDrive(int driveIndex, bool forceViewUpdate = false);
	void RefreshDriveColor(int driveIndex, bool forceViewUpdate = false);
	void Eject(int driveIndex);
	bool ConfirmEject(int driveIndex);
	void Reinterleave(ATDiskInterface& diskIf, ATDiskInterleave interleave);
	void Convert(ATDiskInterface& diskIf, ATDiskFormatFileSystem ffs, uint32 sectorSize);

	int GetVisibleDriveOffset(int driveIndex) const;
	
	enum DriveColor {
		kDriveColor_Default,
		kDriveColor_Dirty,
		kDriveColor_Virtual,
		kDriveColor_VirtualFolder,
		kDriveColor_Highlighted,
		kDriveColorCount
	};

	struct DriveEntry {
		VDStringW mPath;
		DriveColor mColor = kDriveColor_Default;
		ATMediaWriteMode mWriteMode = {};
		vdfunction<void()> mUpdateFn;
	};

	bool mbHighDrives = false;
	int mSelectedDrive = -1;
	int mHighlightedDrive = -1;
	bool mbSwapMode = false;
	uint32 mInDriveUpdateCount = 0;
	HBRUSH mDriveColorBrushes[kDriveColorCount] {};
	HICON mhEjectIcon = nullptr;
	HFONT mhFontMarlett = nullptr;
	COLORREF mDriveColors[kDriveColorCount] {};

	DriveEntry mDriveEntries[15];

	vdrefptr<ATDiskDriveDropTargetW32> mpDropTarget;

	int mRowBounds[8][2] = {};

	static const struct EmulationModeEntry {
		ATDiskEmulationMode mMode;
		const wchar_t *mpLabel;
	} kEmuModes[];
};

ATDiskDriveDialog *g_pATDiskDriveDialog;

const ATDiskDriveDialog::EmulationModeEntry ATDiskDriveDialog::kEmuModes[] = {
	{ kATDiskEmulationMode_Generic, L"Generic emulation (288 RPM)" },
	{ kATDiskEmulationMode_Generic57600, L"Generic emulation + 57600 baud (288 RPM)" },
	{ kATDiskEmulationMode_FastestPossible, L"Fastest possible (288 RPM, 128Kbps high speed)" },
	{ kATDiskEmulationMode_810, L"810 (288 RPM)" },
	{ kATDiskEmulationMode_1050, L"1050 (288 RPM)" },
	{ kATDiskEmulationMode_XF551, L"XF551 (300 RPM, 39Kbps high speed)" },
	{ kATDiskEmulationMode_USDoubler, L"US-Doubler (288 RPM, 52Kbps high speed)" },
	{ kATDiskEmulationMode_Speedy1050, L"Speedy 1050 (288 RPM, 56Kbps high speed)" },
	{ kATDiskEmulationMode_IndusGT, L"Indus GT (288 RPM, 68Kbps high speed)" },
	{ kATDiskEmulationMode_Happy810, L"Happy 810 (288 RPM)" },
	{ kATDiskEmulationMode_Happy1050, L"Happy 1050 (288 RPM, 52Kbps high speed)" },
	{ kATDiskEmulationMode_1050Turbo, L"1050 Turbo (288 RPM, 68Kbps high speed)" },
};

ATDiskDriveDialog::ATDiskDriveDialog()
	: VDResizableDialogFrameW32(IDD_DISK_DRIVES)
{
	mpDropTarget = new ATDiskDriveDropTargetW32(mhdlg, this);

	g_pATDiskDriveDialog = this;
}

ATDiskDriveDialog::~ATDiskDriveDialog() {
	g_pATDiskDriveDialog = nullptr;

	if (mhEjectIcon)
		DeleteObject(mhEjectIcon);

	if (mhFontMarlett)
		DeleteObject(mhFontMarlett);
}

namespace {
	const uint32 kDriveLabelID[]={
		IDC_STATIC_D1,
		IDC_STATIC_D2,
		IDC_STATIC_D3,
		IDC_STATIC_D4,
		IDC_STATIC_D5,
		IDC_STATIC_D6,
		IDC_STATIC_D7,
		IDC_STATIC_D8,
	};

	const uint32 kDiskPathID[]={
		IDC_DISKPATH1,
		IDC_DISKPATH2,
		IDC_DISKPATH3,
		IDC_DISKPATH4,
		IDC_DISKPATH5,
		IDC_DISKPATH6,
		IDC_DISKPATH7,
		IDC_DISKPATH8
	};

	const uint32 kWriteModeID[]={
		IDC_WRITEMODE1,
		IDC_WRITEMODE2,
		IDC_WRITEMODE3,
		IDC_WRITEMODE4,
		IDC_WRITEMODE5,
		IDC_WRITEMODE6,
		IDC_WRITEMODE7,
		IDC_WRITEMODE8
	};

	const uint32 kEjectID[]={
		IDC_EJECT1,
		IDC_EJECT2,
		IDC_EJECT3,
		IDC_EJECT4,
		IDC_EJECT5,
		IDC_EJECT6,
		IDC_EJECT7,
		IDC_EJECT8,
	};

	const uint32 kMoreIds[]={
		IDC_MORE1,
		IDC_MORE2,
		IDC_MORE3,
		IDC_MORE4,
		IDC_MORE5,
		IDC_MORE6,
		IDC_MORE7,
		IDC_MORE8,
	};

	const uint32 kBrowseIds[]={
		IDC_BROWSE1,
		IDC_BROWSE2,
		IDC_BROWSE3,
		IDC_BROWSE4,
		IDC_BROWSE5,
		IDC_BROWSE6,
		IDC_BROWSE7,
		IDC_BROWSE8,
	};
}

bool ATDiskDriveDialog::OnLoaded() {
	if (!mbIsModal)
		ATUIRegisterModelessDialog(mhwnd);

	for(uint32 id : kMoreIds)
		mResizer.Add(id, mResizer.kTR | mResizer.kSuppressFontChange);

	for(uint32 id : kEjectID)
		mResizer.Add(id, mResizer.kTR);

	for(uint32 id : kWriteModeID)
		mResizer.Add(id, mResizer.kTR);

	for(uint32 id : kBrowseIds)
		mResizer.Add(id, mResizer.kTR);

	for(uint32 id : kDiskPathID)
		mResizer.Add(id, mResizer.kTC);

	mResizer.Add(IDOK, mResizer.kTR);

	SetCurrentSizeAsMaxSize(false, true);

	ATUIRestoreWindowPlacement(mhdlg, "Disk drives", -1, true);
	Activate();

	if (!mhEjectIcon) {
		mhEjectIcon = (HICON)LoadImage(VDGetLocalModuleHandleW32(), MAKEINTRESOURCE(IDI_EJECT), IMAGE_ICON, 16, 16, 0);
	}

	if (mhEjectIcon) {
		for(size_t i=0; i<vdcountof(kEjectID); ++i) {
			HWND hwndControl = GetControl(kEjectID[i]);

			if (hwndControl)
				SendMessage(hwndControl, BM_SETIMAGE, IMAGE_ICON, (LPARAM)mhEjectIcon);
		}
	}

	UpdateFonts();

	if (!mDriveColors[kDriveColor_Dirty]) {
		DWORD c = GetSysColor(COLOR_3DFACE);

		// redden the color
		uint32 d = RGB(255, 128, 64);

		c = (c|d) - (((c^d) & 0xfefefe)>>1);

		mDriveColorBrushes[kDriveColor_Dirty] = CreateSolidBrush(c);
		mDriveColors[kDriveColor_Dirty] = c;
	}

	if (!mDriveColors[kDriveColor_Virtual]) {
		DWORD c = GetSysColor(COLOR_3DFACE);

		// bluify the color
		uint32 d = RGB(64, 128, 255);

		c = (c|d) - (((c^d) & 0xfefefe)>>1);

		mDriveColorBrushes[kDriveColor_Virtual] = CreateSolidBrush(c);
		mDriveColors[kDriveColor_Virtual] = c;
	}

	if (!mDriveColors[kDriveColor_VirtualFolder]) {
		DWORD c = GetSysColor(COLOR_3DFACE);

		// yellowify the color
		uint32 d = RGB(255, 224, 128);

		c = (c|d) - (((c^d) & 0xfefefe)>>1);

		mDriveColorBrushes[kDriveColor_VirtualFolder] = CreateSolidBrush(c);
		mDriveColors[kDriveColor_VirtualFolder] = c;
	}

	if (!mDriveColors[kDriveColor_Highlighted]) {
		mDriveColorBrushes[kDriveColor_Highlighted] = (HBRUSH)GetStockObject(WHITE_BRUSH);
		mDriveColors[kDriveColor_Highlighted] = RGB(255, 255, 255);
	}

	for(int i=0; i<8; ++i) {
		uint32 id = kWriteModeID[i];
		CBAddString(id, L"Off");
		CBAddString(id, L"R/O");
		CBAddString(id, L"VRWSafe");
		CBAddString(id, L"VRW");
		CBAddString(id, L"R/W");

		const auto& r = GetControlPos(kDriveLabelID[i]);

		mRowBounds[i][0] = r.top;
		mRowBounds[i][1] = r.bottom;
	}

	int index = 0;
	int selIndex = 0;
	const auto mode = g_sim.GetDiskDrive(0).GetEmulationMode();
	for(const auto& entry : kEmuModes) {
		if (entry.mMode == mode)
			selIndex = index;

		CBAddString(IDC_EMULATION_LEVEL, entry.mpLabel);
		++index;
	}

	CBSetSelectedIndex(IDC_EMULATION_LEVEL, selIndex);

	RegisterDragDrop(mhdlg, mpDropTarget);

	AttachToInterfaces();

	return VDDialogFrameW32::OnLoaded();
}

void ATDiskDriveDialog::OnDestroy() {
	DetachFromInterfaces();

	ATUISaveWindowPlacement(mhdlg, "Disk drives");

	if (mpDropTarget) {
		mpDropTarget->Detach();
		mpDropTarget.clear();
	}

	RevokeDragDrop(mhdlg);

	for(HBRUSH& hbr : mDriveColorBrushes) {
		if (hbr) {
			DeleteObject(hbr);
			hbr = nullptr;
		}
	}

	if (!mbIsModal)
		ATUIRegisterModelessDialog(mhwnd);
}

void ATDiskDriveDialog::OnEnable(bool enable) {
	if (!mbIsModal)
		ATUISetGlobalEnableState(enable);
}

void ATDiskDriveDialog::OnDataExchange(bool write) {
	if (!write) {
		CheckButton(IDC_DRIVES1_8, !mbHighDrives);
		CheckButton(IDC_DRIVES9_15, mbHighDrives);

		ShowControl(IDC_STATIC_D8, !mbHighDrives);
		ShowControl(IDC_DISKPATH8, !mbHighDrives);
		ShowControl(IDC_WRITEMODE8, !mbHighDrives);
		ShowControl(IDC_BROWSE8, !mbHighDrives);
		ShowControl(IDC_EJECT8, !mbHighDrives);
		ShowControl(IDC_MORE8, !mbHighDrives);

		for(int i=0; i<8; ++i) {
			int driveIdx = i;

			if (mbHighDrives) {
				if (i == 7)
					break;

				driveIdx += 8;
			}

			if (driveIdx < 9)
				SetControlTextF(kDriveLabelID[i], L"D&%c:", '1' + driveIdx);
			else
				SetControlTextF(kDriveLabelID[i], L"D1&%c:", '0' + (driveIdx - 9));

			RefreshDrive(driveIdx, true);
		}

		UpdateActionButtons();
	} else {
		int selIndex = CBGetSelectedIndex(IDC_EMULATION_LEVEL);

		if (selIndex >= 0 && selIndex < (int)vdcountof(kEmuModes)) {
			const ATDiskEmulationMode mode = kEmuModes[selIndex].mMode;
			for(int i=0; i<15; ++i)
				g_sim.GetDiskDrive(i).SetEmulationMode(mode);
		}
	}
}

bool ATDiskDriveDialog::OnCommand(uint32 id, uint32 extcode) {
	int index = 0;

	switch(id) {
		case IDC_EJECT8:	++index;
		case IDC_EJECT7:	++index;
		case IDC_EJECT6:	++index;
		case IDC_EJECT5:	++index;
		case IDC_EJECT4:	++index;
		case IDC_EJECT3:	++index;
		case IDC_EJECT2:	++index;
		case IDC_EJECT1:
			{
				int driveIndex = index;

				if (mbHighDrives)
					driveIndex += 8;

				Eject(driveIndex);
			}
			return true;

		case IDC_BROWSE8:	++index;
		case IDC_BROWSE7:	++index;
		case IDC_BROWSE6:	++index;
		case IDC_BROWSE5:	++index;
		case IDC_BROWSE4:	++index;
		case IDC_BROWSE3:	++index;
		case IDC_BROWSE2:	++index;
		case IDC_BROWSE1:
			{
				int driveIndex = index;
				if (mbHighDrives)
					driveIndex += 8;

				if (!ConfirmEject(driveIndex))
					return true;

				VDStringW s(VDGetLoadFileName('disk', (VDGUIHandle)mhdlg, L"Load disk image",
					g_ATUIFileFilter_DiskWithArchives,
					L"atr"));

				if (!s.empty()) {
					ATDiskEmulator& disk = g_sim.GetDiskDrive(index);
					ATDiskInterface& diskIf = g_sim.GetDiskInterface(index);

					try {
						ATImageLoadContext ctx;
						ctx.mLoadType = kATImageType_Disk;
						ctx.mLoadIndex = driveIndex;

						g_sim.Load(s.c_str(), disk.IsEnabled() || diskIf.GetClientCount() > 1 ? diskIf.GetWriteMode() : g_ATOptions.mDefaultWriteMode, &ctx);
						OnDataExchange(false);
					} catch(const MyError& e) {
						e.post(mhdlg, "Disk load error");
					}
				}
			}
			return true;

		case IDC_MORE8:	++index;
		case IDC_MORE7:	++index;
		case IDC_MORE6:	++index;
		case IDC_MORE5:	++index;
		case IDC_MORE4:	++index;
		case IDC_MORE3:	++index;
		case IDC_MORE2:	++index;
		case IDC_MORE1:
			if (mSelectedDrive >= 0) {
				int driveIndex = index;
				if (mbHighDrives)
					driveIndex += 8;

				int oldDrive = mSelectedDrive;
				mSelectedDrive = -1;

				if (driveIndex != oldDrive) {
					if (mbSwapMode) {
						g_sim.SwapDrives(driveIndex, oldDrive);
					} else {
						int direction = driveIndex < oldDrive ? -1 : +1;

						for(int i = oldDrive; i != driveIndex; i += direction) {
							g_sim.SwapDrives(i, i + direction);
						}
					}

					OnDataExchange(false);
				} else {
					UpdateActionButtons();
				}
			} else {
				int driveIndex = index;
				if (mbHighDrives)
					driveIndex += 8;
				
				ATDiskEmulator& disk = g_sim.GetDiskDrive(driveIndex);
				ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveIndex);

				UINT selectedId = 0;

				HMENU hmenu = LoadMenu(VDGetLocalModuleHandleW32(), MAKEINTRESOURCE(IDR_DISK_CONTEXT_MENU));
				if (hmenu) {
					HMENU hsubmenu = GetSubMenu(hmenu, 0);

					if (hsubmenu) {
						RECT r = {0};
						if (HWND hwndItem = GetDlgItem(mhdlg, id))
							GetWindowRect(hwndItem, &r);

						const bool haveDisk = diskIf.IsDiskLoaded();
						VDEnableMenuItemByCommandW32(hsubmenu, ID_CONTEXT_SAVEDISK, haveDisk);
						VDEnableMenuItemByCommandW32(hsubmenu, ID_CONTEXT_SAVEDISKAS, haveDisk);
						VDEnableMenuItemByCommandW32(hsubmenu, ID_CONTEXT_EXPLOREDISK, haveDisk);
						VDEnableMenuItemByCommandW32(hsubmenu, ID_CONTEXT_REVERTDISK, haveDisk && diskIf.CanRevert());

						TPMPARAMS params = {sizeof(TPMPARAMS)};
						params.rcExclude = r;
						selectedId = (UINT)TrackPopupMenuEx(hsubmenu, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_HORIZONTAL | TPM_NONOTIFY | TPM_RETURNCMD, r.right, r.top, mhdlg, &params);
					}

					DestroyMenu(hmenu);
				}

				switch(selectedId) {
					case ID_CONTEXT_NEWDISK:
						if (ConfirmEject(driveIndex)) {
							ATNewDiskDialog dlg;
							if (dlg.ShowDialog(ATUIGetNewPopupOwner())) {
								diskIf.UnloadDisk();
								diskIf.CreateDisk(dlg.GetSectorCount(), dlg.GetBootSectorCount(), dlg.GetSectorSize());

								if (diskIf.GetClientCount() < 2)
									disk.SetEnabled(true);

								diskIf.SetWriteMode(kATMediaWriteMode_VRW);

								vdautoptr<IATDiskFS> fs;

								try {
									IATDiskImage *image = diskIf.GetDiskImage();

									switch(dlg.GetFormatFFS()) {
										case kATDiskFFS_DOS1:
											fs = ATDiskFormatImageDOS1(image);
											break;
										case kATDiskFFS_DOS2:
											fs = ATDiskFormatImageDOS2(image);
											break;
										case kATDiskFFS_DOS3:
											fs = ATDiskFormatImageDOS3(image);
											break;
										case kATDiskFFS_MyDOS:
											fs = ATDiskFormatImageMyDOS(image);
											break;
										case kATDiskFFS_SDFS:
											fs = ATDiskFormatImageSDX2(image);
											break;
									}

									if (fs)
										fs->Flush();
								} catch(const MyError& e) {
									e.post(mhdlg, "Format error");
								}

								SetControlText(kDiskPathID[index], diskIf.GetPath());
								CBSetSelectedIndex(kWriteModeID[index], 3);
							}
						}
						break;

					case ID_CONTEXT_EXPLOREDISK:
						if (IATDiskImage *image = diskIf.GetDiskImage()) {
							VDStringW imageName;

							imageName.sprintf(L"Mounted disk on D%u:", driveIndex + 1);

							const auto writeMode = diskIf.GetWriteMode();
							ATUIShowDialogDiskExplorer(ATUIGetNewPopupOwner(), image, imageName.c_str(),
								(writeMode & kATMediaWriteMode_AllowWrite) != 0,
								(writeMode & kATMediaWriteMode_AutoFlush) != 0);

							// invalidate the path widget in case the disk has been dirtied
							RefreshDriveColor(driveIndex);
						}
						break;

					case ID_CONTEXT_MOUNTFOLDERDOS2:
					case ID_CONTEXT_MOUNTFOLDERSDFS:
						if (ConfirmEject(driveIndex)) {
							const VDStringW& path = VDGetDirectory('vfol', (VDGUIHandle)mhdlg, L"Select folder for virtual disk image");

							if (!path.empty()) {
								try {
									diskIf.MountFolder(path.c_str(), selectedId == ID_CONTEXT_MOUNTFOLDERSDFS);

									if (diskIf.GetClientCount() < 2)
										disk.SetEnabled(true);

									OnDataExchange(false);
								} catch(const MyError& e) {
									e.post(mhdlg, "Mount error");
								}
							}
						}
						break;

					case ID_CONTEXT_EXTRACTBOOTSECTORS:
						if (IATDiskImage *image = diskIf.GetDiskImage()) {
							try {
								if (image->GetBootSectorCount() != 3) {
									throw MyError("The currently mounted disk image does not have standard DOS boot sectors.");
								} else {
									VDSetLastLoadSaveFileName('bsec', L"$dosboot.bin");
									VDStringW s(VDGetSaveFileName(
											'bsec',
											(VDGUIHandle)mhdlg,
											L"Save boot sectors",
											L"Virtual disk boot sectors file\0$dosboot.bin\0All files\0*.*\0",
											L"bin"));

									if (!s.empty()) {
										uint8 sec[384] = {0};

										for(int i=0; i<3; ++i)
											image->ReadPhysicalSector(i, &sec[i*128], 128);

										VDFile f(s.c_str(), nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);

										f.write(sec, sizeof sec);
									}
								}
							} catch(const MyError& e) {
								e.post(mhdlg, "Extract error");
							}
						}
						break;

					case ID_CONTEXT_SAVEDISK:
						if (diskIf.IsDirty() && diskIf.GetDiskImage()->IsUpdatable()) {
							try {
								// if the disk is in VirtR/W mode, switch to R/W mode
								const auto writeMode = diskIf.GetWriteMode();
								if ((writeMode & kATMediaWriteMode_AllowWrite) && !(writeMode & kATMediaWriteMode_AutoFlush)) {
									diskIf.SetWriteMode(kATMediaWriteMode_RW);
									OnDataExchange(false);
								}

								diskIf.SaveDisk();
							} catch(const MyError& e) {
								ShowError(e);
							}
						}
						break;

					case ID_CONTEXT_SAVEDISKAS:
						if (diskIf.IsDiskLoaded() && !diskIf.GetDiskImage()->IsDynamic()) {
							VDFileDialogOption opts[]={
								{ VDFileDialogOption::kSelectedFilter, 0 },
								{ VDFileDialogOption::kEnd }
							};

							int optVals[1] = { 0 };

							VDStringW s(VDGetSaveFileName(
									'disk',
									(VDGUIHandle)mhdlg,
									L"Save disk image",
									L"Atari disk image (*.atr)\0*.atr\0"
										L"VAPI protected disk image (*.atx)\0*.atx\0"
										L"APE protected disk image v2 (*.pro)\0*.pro\0"
										L"APE protected disk image v3 (*.pro)\0*.pro\0"
										L"DiskComm compressed image (*.dcm)\0*.dcm\0"
										L"XFormer disk image (*.xfd)\0*.xfd\0"
										L"All files\0*.*\0",
									L"atr",
									opts,
									optVals));

							if (!s.empty()) {
								try {
									ATDiskImageFormat format = kATDiskImageFormat_ATR;

									switch(optVals[0]) {
										case 1:
											// default is ATR
											break;
										case 2:
											format = kATDiskImageFormat_ATX;
											break;
										case 3:
											format = kATDiskImageFormat_P2;
											break;
										case 4:
											format = kATDiskImageFormat_P3;
											break;
										case 5:
											format = kATDiskImageFormat_DCM;
											break;
										case 6:
											format = kATDiskImageFormat_XFD;
											break;
									}

									diskIf.SaveDiskAs(s.c_str(), format);

									// if the disk is in VirtR/W mode, switch to R/W mode
									const auto writeMode = diskIf.GetWriteMode();
									if ((writeMode & kATMediaWriteMode_AllowWrite) && !(writeMode & kATMediaWriteMode_AutoFlush)) {
										diskIf.SetWriteMode(kATMediaWriteMode_RW);
										OnDataExchange(false);
									}

									SetControlText(kDiskPathID[index], s.c_str());
								} catch(const MyError& e) {
									ShowError(e);
								}
							}
						}
						break;

					case ID_CONTEXT_REVERTDISK:
						if (Confirm2("RevertDisk", L"All unsaved changes will be lost.", L"Reverting modified disk"))
							diskIf.RevertDisk();
						break;

					case ID_CONTEXT_SWAPWITHANOTHERDRIVE:
						mSelectedDrive = driveIndex;
						mbSwapMode = true;
						UpdateActionButtons();
						break;

					case ID_CONTEXT_ROTATETOANOTHERDRIVE:
						mSelectedDrive = driveIndex;
						mbSwapMode = false;
						UpdateActionButtons();
						break;

					case ID_CONTEXT_EXPANDARCS:
						if (diskIf.IsDiskLoaded() && !diskIf.GetDiskImage()->IsDynamic()) {
							try {
								// If the disk drive is read-only, make the image VRWSafe.
								if (!(diskIf.GetWriteMode() & kATMediaWriteMode_AllowWrite))
									diskIf.SetWriteMode(kATMediaWriteMode_VRWSafe);

								vdautoptr<IATDiskFS> fs(ATDiskMountImage(diskIf.GetDiskImage(), false));
								if (!fs)
									throw MyError("The disk image does not have a recognized filesystem.");

								fs->SetAllowExtend(true);
								uint32 expanded;
								
								try {
									expanded = ATDiskRecursivelyExpandARCs(*fs);
								} catch(...) {
									// If we failed, try to put the filesystem in a consistent state
									// before we bail.
									try {
										fs->Flush();
									} catch(...) {
									}

									throw;
								}

								fs->Flush();

								ShowInfo(VDStringW().sprintf(L"Archives expanded: %u", expanded).c_str());
							} catch(const MyError& e) {
								ShowError(e);
							}

							diskIf.OnDiskChanged(true);
							OnDataExchange(false);
						}
						break;

					case ID_CHANGEINTERLEAVE_DEFAULT:		Reinterleave(diskIf, kATDiskInterleave_Default); break;
					case ID_CHANGEINTERLEAVE_1_1:			Reinterleave(diskIf, kATDiskInterleave_1_1); break;
					case ID_CHANGEINTERLEAVE_SD_12_1:		Reinterleave(diskIf, kATDiskInterleave_SD_12_1); break;
					case ID_CHANGEINTERLEAVE_SD_9_1:		Reinterleave(diskIf, kATDiskInterleave_SD_9_1); break;
					case ID_CHANGEINTERLEAVE_SD_9_1_REV:	Reinterleave(diskIf, kATDiskInterleave_SD_9_1_REV); break;
					case ID_CHANGEINTERLEAVE_SD_5_1:		Reinterleave(diskIf, kATDiskInterleave_SD_5_1); break;
					case ID_CHANGEINTERLEAVE_ED_13_1:		Reinterleave(diskIf, kATDiskInterleave_ED_13_1); break;
					case ID_CHANGEINTERLEAVE_ED_12_1:		Reinterleave(diskIf, kATDiskInterleave_ED_12_1); break;
					case ID_CHANGEINTERLEAVE_DD_15_1:		Reinterleave(diskIf, kATDiskInterleave_DD_15_1); break;
					case ID_CHANGEINTERLEAVE_DD_9_1:		Reinterleave(diskIf, kATDiskInterleave_DD_9_1); break;
					case ID_CHANGEINTERLEAVE_DD_7_1:		Reinterleave(diskIf, kATDiskInterleave_DD_7_1); break;

					case ID_CONVERTTO_DOS1_SD:
						Convert(diskIf, kATDiskFFS_DOS1, 128);
						break;

					case ID_CONVERTTO_DOS2_SDED:
						Convert(diskIf, kATDiskFFS_DOS2, 128);
						break;

					case ID_CONVERTTO_DOS2_DD:
						Convert(diskIf, kATDiskFFS_DOS2, 256);
						break;

					case ID_CONVERTTO_MYDOS_SDED:
						Convert(diskIf, kATDiskFFS_MyDOS, 128);
						break;

					case ID_CONVERTTO_MYDOS_DD:
						Convert(diskIf, kATDiskFFS_MyDOS, 256);
						break;

					case ID_CONVERTTO_SPARTADOS_SDED:
						Convert(diskIf, kATDiskFFS_SDFS, 128);
						break;

					case ID_CONVERTTO_SPARTADOS_DD:
						Convert(diskIf, kATDiskFFS_SDFS, 256);
						break;

					case ID_CONVERTTO_SPARTADOS_512:
						Convert(diskIf, kATDiskFFS_SDFS, 512);
						break;
				}
			}
			return true;

		case IDC_WRITEMODE8:	++index;
		case IDC_WRITEMODE7:	++index;
		case IDC_WRITEMODE6:	++index;
		case IDC_WRITEMODE5:	++index;
		case IDC_WRITEMODE4:	++index;
		case IDC_WRITEMODE3:	++index;
		case IDC_WRITEMODE2:	++index;
		case IDC_WRITEMODE1:
			if (!mInDriveUpdateCount) {
				int driveIndex = index;
				if (mbHighDrives)
					driveIndex += 8;

				int mode = CBGetSelectedIndex(id);
				ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveIndex);
				ATDiskEmulator& disk = g_sim.GetDiskDrive(driveIndex);

				if (mode == 0) {
					// We may get more than one message -- protect against reentrancy
					// while the confirmation is up.
					++mInDriveUpdateCount;
					bool confirmed = ConfirmEject(driveIndex);
					--mInDriveUpdateCount;

					if (confirmed) {
						diskIf.UnloadDisk();
						disk.SetEnabled(false);
					} else {
						RefreshDrive(driveIndex, true);
					}
				} else {
					if (diskIf.GetClientCount() < 2)
						disk.SetEnabled(true);

					switch(mode) {
						case 1:
							diskIf.SetWriteMode(kATMediaWriteMode_RO);
							break;

						case 2:
							diskIf.SetWriteMode(kATMediaWriteMode_VRWSafe);
							break;

						case 3:
							diskIf.SetWriteMode(kATMediaWriteMode_VRW);
							break;

						case 4:
							diskIf.SetWriteMode(kATMediaWriteMode_RW);
							break;
					}
				}
			}
			return true;

		case IDC_DRIVES1_8:
			if (mbHighDrives && IsButtonChecked(id)) {
				mbHighDrives = false;

				OnDataExchange(false);
			}
			return true;

		case IDC_DRIVES9_15:
			if (!mbHighDrives && IsButtonChecked(id)) {
				mbHighDrives = true;

				OnDataExchange(false);
			}
			return true;
	}

	return false;
}

int ATDiskDriveDialog::FindDropTarget(sint32 x, sint32 y) const {
	POINT pt = {x, y};
	ScreenToClient(mhdlg, &pt);

	for(int i=0; i<8; ++i) {
		if (pt.y >= mRowBounds[i][0] && pt.y < mRowBounds[i][1]) {
			int index = i;

			if (mbHighDrives)
				index += 8;

			if (index < 15)
				return index;
		}
	}

	return -1;
}

void ATDiskDriveDialog::SetDropTargetHighlight(int index) {
	if (mHighlightedDrive != index) {
		int oldHighlight = mHighlightedDrive;
		mHighlightedDrive = index;

		if (oldHighlight >= 0)
			RefreshDriveColor(oldHighlight);

		if (index >= 0)
			RefreshDriveColor(index);
	}
}

void ATDiskDriveDialog::OnDrop(int index, const wchar_t *path, const wchar_t *imageName, IVDRandomAccessStream& stream) {
	if (index < 0 || index > 15)
		return;

	ATDiskEmulator& disk = g_sim.GetDiskDrive(index);
	ATDiskInterface& diskIf = g_sim.GetDiskInterface(index);

	ATImageLoadContext ctx;
	ctx.mLoadType = kATImageType_Disk;
	ctx.mLoadIndex = index;

	g_sim.Load(path, imageName, stream, disk.IsEnabled() || diskIf.GetClientCount() > 1 ? diskIf.GetWriteMode() : g_ATOptions.mDefaultWriteMode, &ctx);
	OnDataExchange(false);
}

VDZINT_PTR ATDiskDriveDialog::DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	int index;

	switch(msg) {
		case WM_CTLCOLORSTATIC:
		case WM_CTLCOLOREDIT:
			index = 0;

			switch(GetWindowLong((HWND)lParam, GWL_ID)) {
				case IDC_DISKPATH8:	++index;
				case IDC_DISKPATH7:	++index;
				case IDC_DISKPATH6:	++index;
				case IDC_DISKPATH5:	++index;
				case IDC_DISKPATH4:	++index;
				case IDC_DISKPATH3:	++index;
				case IDC_DISKPATH2:	++index;
				case IDC_DISKPATH1:
					{
						int driveIndex = index;
						if (mbHighDrives)
							driveIndex += 8;

						const HDC hdc = (HDC)wParam;

						DriveColor dcl = mDriveEntries[driveIndex].mColor;
						SetBkColor(hdc, mDriveColors[dcl]);
						return (VDZINT_PTR)mDriveColorBrushes[dcl];
					}
					break;
			}

	}

	return VDDialogFrameW32::DlgProc(msg, wParam, lParam);
}

void ATDiskDriveDialog::OnDpiChanged() {
	VDResizableDialogFrameW32::OnDpiChanged();

	UpdateFonts();
}

bool ATDiskDriveDialog::PreNCDestroy() {
	return !mbIsModal;
}

void ATDiskDriveDialog::AttachToInterfaces() {
	for(int i=0; i<15; ++i) {
		ATDiskInterface& diskIf = g_sim.GetDiskInterface(i);

		mDriveEntries[i].mUpdateFn = [i,this] { RefreshDrive(i); };
		
		diskIf.AddStateChangeHandler(&mDriveEntries[i].mUpdateFn);

		RefreshDrive(i);
	}
}

void ATDiskDriveDialog::DetachFromInterfaces() {
	for(int i=0; i<15; ++i) {
		ATDiskInterface& diskIf = g_sim.GetDiskInterface(i);

		diskIf.RemoveStateChangeHandler(&mDriveEntries[i].mUpdateFn);
	}
}

void ATDiskDriveDialog::UpdateFonts() {
	HFONT hNewFont = nullptr;

	HFONT hfontDlg = (HFONT)SendMessage(mhdlg, WM_GETFONT, 0, 0);

	if (hfontDlg) {
		LOGFONT lf = {0};
		if (GetObject(hfontDlg, sizeof lf, &lf)) {
			hNewFont = CreateFont(lf.lfHeight, 0, 0, 0, FW_DONTCARE, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, _T("Marlett"));
		}
	}

	if (hNewFont) {
		for(size_t i=0; i<vdcountof(kMoreIds); ++i) {
			HWND hwndControl = GetControl(kMoreIds[i]);

			if (hwndControl) {
				SendMessage(hwndControl, WM_SETFONT, (WPARAM)hNewFont, MAKELONG(TRUE, 0));
			}
		}

		if (mhFontMarlett)
			DeleteObject(mhFontMarlett);

		mhFontMarlett = hNewFont;
	}
}

void ATDiskDriveDialog::UpdateActionButtons() {
	int driveIndex = (mbHighDrives ? 8 : 0);

	// update to:
	// - right arrow if no swap in progress
	// - X if swap/rotate in progress and this is the originating drive
	// - <> if swap in progress and other drive
	// - ^v if rotate in progress and another drive
	for(int i=0; i<8; ++i) {
		const wchar_t *text = L"4";		// right arrow

		if (mSelectedDrive >= 0) {
			if (mSelectedDrive == driveIndex + i)
				text = L"r";		// X
			else if (mbSwapMode)
				text = L"v";		// <>
			else if (mSelectedDrive > driveIndex + i)
				text = L"5";
			else
				text = L"6";
		}

		SetControlText(kMoreIds[i], text);
	}
}

void ATDiskDriveDialog::RefreshDrive(int driveIndex, bool forceViewUpdate) {
	ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveIndex);
	DriveEntry& de = mDriveEntries[driveIndex];
	const wchar_t *path = diskIf.GetPath();

	if (!path)
		path = L"";

	const int visIndex = GetVisibleDriveOffset(driveIndex);
	bool pathChanged = forceViewUpdate;

	if (de.mPath != path) {
		de.mPath = path;
		pathChanged = true;
	}

	if (visIndex >= 0 && pathChanged)
		SetControlText(kDiskPathID[visIndex], path);

	RefreshDriveColor(driveIndex, forceViewUpdate);

	const auto writeMode = diskIf.GetWriteMode();
	bool writeModeChanged = forceViewUpdate;
	if (de.mWriteMode != writeMode) {
		de.mWriteMode = writeMode;

		writeModeChanged = true;
	}

	if (visIndex >= 0 && writeModeChanged) {
		ATDiskEmulator& disk = g_sim.GetDiskDrive(driveIndex);
		int selIndex = 0;

		if (!disk.IsEnabled() && diskIf.GetClientCount() < 2)
			selIndex = 0;
		else if (!(writeMode & kATMediaWriteMode_AllowWrite))
			selIndex = 1;
		else if (writeMode & kATMediaWriteMode_AutoFlush)
			selIndex = 4;
		else if (writeMode & kATMediaWriteMode_AllowFormat)
			selIndex = 3;
		else
			selIndex = 2;

		++mInDriveUpdateCount;
		CBSetSelectedIndex(kWriteModeID[visIndex], selIndex);
		--mInDriveUpdateCount;
	}
}

void ATDiskDriveDialog::RefreshDriveColor(int driveIndex, bool forceViewUpdate) {
	ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveIndex);
	DriveEntry& de = mDriveEntries[driveIndex];
	DriveColor dcl = kDriveColor_Default;

	if (mHighlightedDrive == driveIndex)
		dcl = kDriveColor_Highlighted;
	else if (diskIf.IsDirty())
		dcl = kDriveColor_Dirty;
	else if (diskIf.IsDiskLoaded()) {
		if (de.mPath.find(L'*') != VDStringW::npos)
			dcl = kDriveColor_VirtualFolder;
		else if (!diskIf.IsDiskBacked())
			dcl = kDriveColor_Virtual;
	}

	bool colorChanged = forceViewUpdate;
	if (de.mColor != dcl) {
		de.mColor = dcl;
		colorChanged = true;
	}

	if (colorChanged) {
		int visIndex = GetVisibleDriveOffset(driveIndex);
		if (visIndex >= 0) {
			HWND hwndPathControl = GetControl(kDiskPathID[visIndex]);

			if (hwndPathControl)
				InvalidateRect(hwndPathControl, NULL, TRUE);
		}
	}
}

void ATDiskDriveDialog::Eject(int driveIndex) {
	ATDiskInterface& diskIf = g_sim.GetDiskInterface(driveIndex);
	ATDiskEmulator& disk = g_sim.GetDiskDrive(driveIndex);
	int visIndex = GetVisibleDriveOffset(driveIndex);

	if (diskIf.IsDiskLoaded()) {
		if (ConfirmEject(driveIndex))
			diskIf.UnloadDisk();
	} else {
		disk.SetEnabled(false);

		if (visIndex >= 0)
			CBSetSelectedIndex(kWriteModeID[visIndex], 0);
	}
}

bool ATDiskDriveDialog::ConfirmEject(int driveIndex) {
	auto& diskIf = g_sim.GetDiskInterface(driveIndex);

	if (!diskIf.IsDirty())
		return true;

	VDStringW message;
	message.sprintf(L"The modified disk image in D%u: has not been saved and will be discarded. Are you sure?", (unsigned)driveIndex + 1);
	return Confirm(message.c_str());
}

void ATDiskDriveDialog::Reinterleave(ATDiskInterface& diskIf, ATDiskInterleave interleave) {
	IATDiskImage *img = diskIf.GetDiskImage();
	if (!img)
		return;

	if (!img->IsSafeToReinterleave()) {
		if (!Confirm2("ReinterleaveProtectedDisk", L"This disk image may not work correctly with its sectors reordered. Are you sure?", L"Reinterleaving disk"))
			return;
	}

	img->Reinterleave(interleave);
}

void ATDiskDriveDialog::Convert(ATDiskInterface& diskIf, ATDiskFormatFileSystem ffs, uint32 sectorSize) {
	IATDiskImage *img = diskIf.GetDiskImage();
	if (!img)
		return;

	try {
		vdautoptr<IATDiskFS> fs(ATDiskMountImage(img, true));
		vdrefptr<IATDiskImage> newImage;
		vdautoptr<IATDiskFS> newfs;
		uint32 diskSize;

		switch(ffs) {
			case kATDiskFFS_DOS1:
				diskSize = 720;
				ATCreateDiskImage(diskSize, 3, sectorSize, ~newImage);
				newfs = ATDiskFormatImageDOS1(newImage);
				break;

			case kATDiskFFS_DOS2:
				diskSize = ATDiskFSEstimateDOS2SectorsNeeded(*fs, sectorSize);
				ATCreateDiskImage(diskSize, 3, sectorSize, ~newImage);
				newfs = ATDiskFormatImageDOS2(newImage);
				break;

			case kATDiskFFS_MyDOS:
				diskSize = ATDiskFSEstimateMyDOSSectorsNeeded(*fs, sectorSize);
				ATCreateDiskImage(diskSize, 3, sectorSize, ~newImage);
				newfs = ATDiskFormatImageMyDOS(newImage);
				break;

			case kATDiskFFS_SDFS:
				diskSize = ATDiskFSEstimateSDX2SectorsNeeded(*fs, sectorSize);

				// DD 512 disks do have a boot sector, but it is not a 128-byte special kind
				// of boot sector.
				ATCreateDiskImage(diskSize, sectorSize >= 512 ? 0 : 3, sectorSize, ~newImage);
				newfs = ATDiskFormatImageSDX2(newImage);
				break;

			default:
				return;
		}

		// copy everything over
		ATDiskFSCopyTree(*newfs, ATDiskFSKey::None, *fs, ATDiskFSKey::None, true);

		// unmount filesystems
		fs = nullptr;

		newfs->Flush();
		newfs = nullptr;

		// mount new image
		VDStringW origName { VDFileSplitPath(diskIf.GetPath()) };
		diskIf.LoadDisk(nullptr, origName.c_str(), newImage);
	} catch(const MyError& e) {
		ShowError(e);
	}
}

int ATDiskDriveDialog::GetVisibleDriveOffset(int driveIndex) const {
	if ((unsigned)driveIndex >= 15)
		return -1;

	if (mbHighDrives)
		driveIndex -= 8;

	if ((unsigned)driveIndex >= 8)
		return -1;

	return driveIndex;
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDiskDriveDialog(VDGUIHandle hParent) {
	if (!g_pATDiskDriveDialog) {
		g_pATDiskDriveDialog = new ATDiskDriveDialog;
		g_pATDiskDriveDialog->Create(ATUIGetMainWindow());
	} else {
		g_pATDiskDriveDialog->Activate();
	}
}
