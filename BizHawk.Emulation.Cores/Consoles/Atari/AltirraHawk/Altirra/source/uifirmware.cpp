//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/linearalloc.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/system/zip.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "oshelper.h"
#include "firmwaremanager.h"
#include "firmwaredetect.h"
#include "uimenu.h"
#include <at/atui/uimenulist.h>

void ATUIScanForFirmware(VDGUIHandle hParent, ATFirmwareManager& fwmgr);

///////////////////////////////////////////////////////////////////////////

namespace {
	const wchar_t *GetSpecificFirmwareLabel(ATSpecificFirmwareType ft) {
		switch(ft) {
			case kATSpecificFirmwareType_BASICRevA:	return L"BASIC rev. A";
			case kATSpecificFirmwareType_BASICRevB:	return L"BASIC rev. B";
			case kATSpecificFirmwareType_BASICRevC:	return L"BASIC rev. C";
			case kATSpecificFirmwareType_OSA:		return L"OS-A";
			case kATSpecificFirmwareType_OSB:		return L"OS-B";
			case kATSpecificFirmwareType_XLOSr2:	return L"XL/XE OS ver. 2";
			case kATSpecificFirmwareType_XLOSr4:	return L"XL/XE/XEGS OS ver. 4";
			default:
				return nullptr;
		}
	}

	struct FirmwareItem : public vdrefcounted<IVDUITreeViewVirtualItem> {
		FirmwareItem(uint64 id, ATFirmwareType type, bool category, const wchar_t *text, const wchar_t *path)
			: mId(id)
			, mType(type)
			, mbCategory(category)
			, mText(text)
			, mPath(path)
			, mFlags(0)
			, mbDefault(false)
		{
		}

		void *AsInterface(uint32 iid) {
			return NULL;
		}

		void GetText(VDStringW& s) const;

		const uint64 mId;
		ATFirmwareType mType;
		VDUIProxyTreeViewControl::NodeRef mNode;
		VDStringW mText;
		VDStringW mPath;
		uint32 mFlags;
		bool mbCategory;
		bool mbDefault;

		vdhashmap<ATSpecificFirmwareType, VDUIProxyTreeViewControl::NodeRef, vdhash<uint32>> mSpecificNodes;
	};

	void FirmwareItem::GetText(VDStringW& s) const {
		if (mbCategory)
			s = mText;
		else
			s.sprintf(L"%ls%ls (%ls)", mbDefault ? L"*" : L"", mText.c_str(), mId < kATFirmwareId_Custom ? L"internal" : VDFileSplitPath(mPath.c_str()));
	}

	struct FirmwareItemComparer : public IVDUITreeViewVirtualItemComparer {
		virtual int Compare(IVDUITreeViewVirtualItem& x, IVDUITreeViewVirtualItem& y) const {
			return static_cast<const FirmwareItem&>(x).mText.comparei(static_cast<const FirmwareItem&>(y).mText);
		}
	};

	VDStringW BrowseForFirmware(VDDialogFrameW32 *parent) {
		return VDGetLoadFileName('ROMI', (VDGUIHandle)parent->GetWindowHandle(), L"Browse for ROM image", L"ROM image\0*.rom;*.bin;*.epr;*.epm\0All files\0*.*\0", NULL);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogEditFirmwareSettings : public VDDialogFrameW32 {
public:
	ATUIDialogEditFirmwareSettings(FirmwareItem& item);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
	void OnTypeChanged(VDUIProxyComboBoxControl *sender, int index);
	void RedoOptions(ATFirmwareType type);

	FirmwareItem& mItem;
	VDUIProxyListView mOptionsView;
	VDUIProxyComboBoxControl mTypeList;
	uint32 mFlagCount;

	VDDelegate mDelTypeChanged;

	struct TypeEntry {
		ATFirmwareType mType;
		const wchar_t *mpName;
	};

	const TypeEntry *mpSortedTypes[39];
	
	static const TypeEntry kTypeNames[];
};

const ATUIDialogEditFirmwareSettings::TypeEntry ATUIDialogEditFirmwareSettings::kTypeNames[]={
	{ kATFirmwareType_Kernel800_OSA, L"400/800 Kernel (OS-A compatible)" },
	{ kATFirmwareType_Kernel800_OSB, L"400/800 Kernel (OS-B compatible)" },
	{ kATFirmwareType_KernelXL, L"XL/XE Kernel" },
	{ kATFirmwareType_KernelXEGS, L"XEGS Kernel" },
	{ kATFirmwareType_Game, L"XEGS Game" },
	{ kATFirmwareType_Kernel1200XL, L"1200XL Kernel" },
	{ kATFirmwareType_Kernel5200, L"5200 Kernel" },
	{ kATFirmwareType_Basic, L"Internal BASIC (XL/XE/XEGS)" },
	{ kATFirmwareType_U1MB, L"Ultimate1MB" },
	{ kATFirmwareType_MyIDE2, L"MyIDE-II" },
	{ kATFirmwareType_SIDE, L"SIDE" },
	{ kATFirmwareType_SIDE2, L"SIDE 2" },
	{ kATFirmwareType_KMKJZIDE, L"KMK/JZ IDE" },
	{ kATFirmwareType_KMKJZIDE2, L"KMK/JZ IDE 2 (IDEPlus) main" },
	{ kATFirmwareType_KMKJZIDE2_SDX, L"KMK/JZ IDE 2 (IDEPlus) SDX" },
	{ kATFirmwareType_BlackBox, L"BlackBox" },
	{ kATFirmwareType_MIO, L"MIO" },
	{ kATFirmwareType_1030Firmware, L"1030 Modem Firmware" },
	{ kATFirmwareType_810, L"810 Disk Drive Firmware" },
	{ kATFirmwareType_Happy810, L"Happy 810 Disk Drive Firmware" },
	{ kATFirmwareType_810Archiver, L"810 Archiver Disk Drive Firmware" },
	{ kATFirmwareType_1050, L"1050 Disk Drive Firmware" },
	{ kATFirmwareType_1050Duplicator, L"1050 Duplicator Disk Drive Firmware" },
	{ kATFirmwareType_USDoubler, L"US Doubler Disk Drive Firmware" },
	{ kATFirmwareType_Speedy1050, L"Speedy 1050 Disk Drive Firmware" },
	{ kATFirmwareType_Happy1050, L"Happy 1050 Disk Drive Firmware" },
	{ kATFirmwareType_SuperArchiver, L"Super Archiver Disk Drive Firmware" },
	{ kATFirmwareType_TOMS1050, L"TOMS 1050 Disk Drive Firmware" },
	{ kATFirmwareType_Tygrys1050, L"Tygrys 1050 Disk Drive Firmware" },
	{ kATFirmwareType_IndusGT, L"Indus GT Disk Drive Firmware" },
	{ kATFirmwareType_1050Turbo, L"1050 Turbo Disk Drive Firmware" },
	{ kATFirmwareType_1050TurboII, L"1050 Turbo II Disk Drive Firmware" },
	{ kATFirmwareType_ISPlate, L"I.S. Plate Disk Drive Firmware" },
	{ kATFirmwareType_XF551, L"XF551 Disk Drive Firmware" },
	{ kATFirmwareType_ATR8000, L"ATR8000 Disk Drive Firmware" },
	{ kATFirmwareType_Percom, L"PERCOM Disk Drive Firmware" },
	{ kATFirmwareType_RapidusFlash, L"Rapidus Flash Firmware" },
	{ kATFirmwareType_RapidusCorePBI, L"Rapidus Core PBI Firmware" },
	{ kATFirmwareType_WarpOS, L"APE Warp+ OS 32-in-1 Firmware" },
};

ATUIDialogEditFirmwareSettings::ATUIDialogEditFirmwareSettings(FirmwareItem& item)
	: VDDialogFrameW32(IDD_FIRMWARE_EDIT)
	, mItem(item)
	, mFlagCount(0)
{
	static_assert(vdcountof(kTypeNames) == vdcountof(mpSortedTypes), "array mismatch");

	mTypeList.OnSelectionChanged() += mDelTypeChanged.Bind(this, &ATUIDialogEditFirmwareSettings::OnTypeChanged);
}

bool ATUIDialogEditFirmwareSettings::OnLoaded() {
	AddProxy(&mOptionsView, IDC_OPTIONS);
	AddProxy(&mTypeList, IDC_TYPE);

	for(size_t i=0; i<vdcountof(kTypeNames); ++i)
		mpSortedTypes[i] = &kTypeNames[i];

	mTypeList.AddItem(L"(Type not set yet)");
	for(size_t i=0; i<vdcountof(kTypeNames); ++i)
		mTypeList.AddItem(mpSortedTypes[i]->mpName);

	mOptionsView.SetFullRowSelectEnabled(true);
	mOptionsView.SetItemCheckboxesEnabled(true);

	try {
		VDFile f(mItem.mPath.c_str());

		sint64 size64 = f.size();

		if (size64 <= 4*1024*1024) {
			uint32 size32 = (uint32)size64;

			vdblock<uint8> buf(size32);
			f.read(buf.data(), size32);

			const uint32 crc32 = VDCRCTable::CRC32.CRC(buf.data(), size32);

			SetControlTextF(IDC_CRC32, L"%08X", crc32);
		}
	} catch(const MyError&) {
	}

	VDDialogFrameW32::OnLoaded();
	SetFocusToControl(IDC_NAME);
	return false;
}

void ATUIDialogEditFirmwareSettings::OnDataExchange(bool write) {
	ExchangeControlValueString(write, IDC_NAME, mItem.mText);

	if (write) {
		int typeIndex = mTypeList.GetSelection();
		if ((unsigned)typeIndex - 1 < (vdcountof(mpSortedTypes)))
			mItem.mType = mpSortedTypes[typeIndex - 1]->mType;
		else {
			FailValidation(IDC_TYPE, L"The firmware type has not been set.");
			return;
		}

		if (mFlagCount)
			mItem.mFlags = mOptionsView.IsItemChecked(0) ? 1 : 0;
		else
			mItem.mFlags = 0;
	} else {
		SetControlText(IDC_PATH, mItem.mPath.c_str());

		int typeIndex = 0;

		for(size_t i=0; i<vdcountof(mpSortedTypes); ++i) {
			if (mItem.mType == mpSortedTypes[i]->mType) {
				typeIndex = (int)i + 1;
				break;
			}
		}

		mTypeList.SetSelection(typeIndex);

		RedoOptions(mItem.mType);

		if (mFlagCount)
			mOptionsView.SetItemChecked(0, (mItem.mFlags & 1) != 0);
	}
}

void ATUIDialogEditFirmwareSettings::OnTypeChanged(VDUIProxyComboBoxControl *sender, int index) {
	if ((unsigned)index - 1 < vdcountof(mpSortedTypes))
		RedoOptions(mpSortedTypes[index - 1]->mType);
}

void ATUIDialogEditFirmwareSettings::RedoOptions(ATFirmwareType type) {
	mOptionsView.Clear();
	mFlagCount = 0;

	switch(type) {
		case kATFirmwareType_KernelXL:
		case kATFirmwareType_KernelXEGS:
			mFlagCount = 1;
			mOptionsView.InsertItem(-1, L"OPTION key inverted (hold to enable BASIC)");
			mOptionsView.SetEnabled(true);
			break;

		default:
			mOptionsView.SetEnabled(false);
			break;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogFirmware final : public VDResizableDialogFrameW32 {
public:
	ATUIDialogFirmware(ATFirmwareManager& sim);

	bool AnyChanges() const { return mbAnyChanges; }

protected:
	bool OnLoaded() override;
	void OnDestroy() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 extcode) override;
	void OnDropFiles(IVDUIDropFileList *dropFileList) override;

	void Add();
	void Add(const wchar_t *);
	void Remove();
	void EditSettings();
	void SetAsDefault();
	void SetAsSpecific();
	void UpdateFirmwareItem(const FirmwareItem& item);
	void UpdateSpecificNodes(FirmwareItem *item);

	void OnSelChanged(VDUIProxyTreeViewControl *sender, int idx);
	void OnItemDoubleClicked(VDUIProxyTreeViewControl *sender, bool *handled);
	void OnItemGetDisplayAttributes(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::GetDispAttrEvent *event);
	void OnBeginEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::BeginEditEvent *event);
	void OnEndEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::EndEditEvent *event);

	ATFirmwareManager& mFwManager;
	bool mbAnyChanges;

	vdrefptr<FirmwareItem> mpCategories[kATFirmwareTypeCount];
	uint64 mDefaultIds[kATFirmwareTypeCount];

	VDUIProxyTreeViewControl mTreeView;

	VDDelegate mDelSelChanged;
	VDDelegate mDelDblClk;
	VDDelegate mDelItemGetDispAttr;
	VDDelegate mDelBeginEdit;
	VDDelegate mDelEndEdit;
};

ATUIDialogFirmware::ATUIDialogFirmware(ATFirmwareManager& fw)
	: VDResizableDialogFrameW32(IDD_FIRMWARE)
	, mFwManager(fw)
	, mbAnyChanges(false)
{
}

bool ATUIDialogFirmware::OnLoaded() {
	AddProxy(&mTreeView, IDC_TREE);

	typedef VDDialogResizerW32 RS;

	mResizer.Add(IDC_TREE, RS::kMC);
	mResizer.Add(IDC_ADD, RS::kBL);
	mResizer.Add(IDC_REMOVE, RS::kBL);
	mResizer.Add(IDC_SETTINGS, RS::kBL);
	mResizer.Add(IDC_SCAN, RS::kBL);
	mResizer.Add(IDC_SETASDEFAULT, RS::kBL);
	mResizer.Add(IDC_SETASSPECIFIC, RS::kBL);
	mResizer.Add(IDC_CLEAR, RS::kBL);
	mResizer.Add(IDOK, RS::kBR);

	ATUIRestoreWindowPlacement(mhdlg, "Firmware dialog");

	OnDataExchange(false);

	mTreeView.OnItemSelectionChanged() += mDelSelChanged.Bind(this, &ATUIDialogFirmware::OnSelChanged);
	mTreeView.OnItemDoubleClicked() += mDelDblClk.Bind(this, &ATUIDialogFirmware::OnItemDoubleClicked);
	mTreeView.OnItemGetDisplayAttributes() += mDelItemGetDispAttr.Bind(this, &ATUIDialogFirmware::OnItemGetDisplayAttributes);
	mTreeView.OnItemBeginEdit() += mDelBeginEdit.Bind(this, &ATUIDialogFirmware::OnBeginEdit);
	mTreeView.OnItemEndEdit() += mDelEndEdit.Bind(this, &ATUIDialogFirmware::OnEndEdit);

	SetFocusToControl(IDC_LIST);
	return true;
}

void ATUIDialogFirmware::OnDestroy() {
	mTreeView.Clear();

	ATUISaveWindowPlacement(mhdlg, "Firmware dialog");

	VDDialogFrameW32::OnDestroy();
}

void ATUIDialogFirmware::OnDataExchange(bool write) {
	if (write) {
	} else {
		mTreeView.SetRedraw(false);
		mTreeView.Clear();

		static constexpr struct Category {
			ATFirmwareType mType;
			const wchar_t *mpName;
		} kCategories[]={
			{ kATFirmwareType_Kernel800_OSA, L"400/800 Kernel ROMs (OS-A compatible)" },
			{ kATFirmwareType_Kernel800_OSB, L"400/800 Kernel ROMs (OS-B compatible)" },
			{ kATFirmwareType_KernelXL, L"XL/XE Kernel ROMs" },
			{ kATFirmwareType_KernelXEGS, L"XEGS Kernel ROMs" },
			{ kATFirmwareType_Game, L"XEGS Game ROMs" },
			{ kATFirmwareType_Kernel1200XL, L"1200XL Kernel ROMs" },
			{ kATFirmwareType_Kernel5200, L"5200 Kernel ROMs" },
			{ kATFirmwareType_Basic, L"Internal BASIC ROMs (XL/XE/XEGS)" },
			{ kATFirmwareType_U1MB, L"Ultimate1MB ROMs" },
			{ kATFirmwareType_MyIDE2, L"MyIDE-II ROMs" },
			{ kATFirmwareType_SIDE, L"SIDE ROMs" },
			{ kATFirmwareType_SIDE2, L"SIDE 2 ROMs" },
			{ kATFirmwareType_KMKJZIDE, L"KMK/JZ IDE ROMs" },
			{ kATFirmwareType_KMKJZIDE2, L"KMK/JZ IDE 2 (IDEPlus) main ROMs" },
			{ kATFirmwareType_KMKJZIDE2_SDX, L"KMK/JZ IDE 2 (IDEPlus) SDX ROMs" },
			{ kATFirmwareType_BlackBox, L"BlackBox ROMs" },
			{ kATFirmwareType_MIO, L"MIO ROMs" },
			{ kATFirmwareType_1030Firmware, L"1030 Modem Firmware" },
			{ kATFirmwareType_810, L"810 Disk Drive Firmware" },
			{ kATFirmwareType_Happy810, L"Happy 810 Disk Drive Firmware" },
			{ kATFirmwareType_810Archiver, L"810 Archiver Disk Drive Firmware" },
			{ kATFirmwareType_1050, L"1050 Disk Drive Firmware" },
			{ kATFirmwareType_1050Duplicator, L"1050 Duplicator Disk Drive Firmware" },
			{ kATFirmwareType_USDoubler, L"US Doubler Disk Drive Firmware" },
			{ kATFirmwareType_Speedy1050, L"Speedy 1050 Disk Drive Firmware" },
			{ kATFirmwareType_Happy1050, L"Happy 1050 Disk Drive Firmware" },
			{ kATFirmwareType_SuperArchiver, L"Super Archiver Disk Drive Firmware" },
			{ kATFirmwareType_TOMS1050, L"TOMS 1050 Disk Drive Firmware" },
			{ kATFirmwareType_Tygrys1050, L"Tygrys 1050 Disk Drive Firmware" },
			{ kATFirmwareType_1050Turbo, L"1050 Turbo Disk Drive Firmware" },
			{ kATFirmwareType_1050TurboII, L"1050 Turbo II Disk Drive Firmware" },
			{ kATFirmwareType_ISPlate, L"I.S. Plate Disk Drive Firmware" },
			{ kATFirmwareType_IndusGT, L"Indus GT Disk Drive Firmware" },
			{ kATFirmwareType_XF551, L"XF551 Disk Drive Firmware" },
			{ kATFirmwareType_ATR8000, L"ATR8000 Disk Drive Firmware" },
			{ kATFirmwareType_Percom, L"PERCOM Disk Drive Firmware" },
			{ kATFirmwareType_RapidusFlash, L"Rapidus Flash Firmware" },
			{ kATFirmwareType_RapidusCorePBI, L"Rapidus Core PBI Firmware" },
			{ kATFirmwareType_WarpOS, L"APE Warp+ OS 32-in-1 Firmware" },
		};

		std::fill(mDefaultIds, mDefaultIds + vdcountof(mDefaultIds), 0);

		for(size_t i=0; i<vdcountof(kCategories); ++i) {
			const ATFirmwareType type = kCategories[i].mType;

			mpCategories[type] = new FirmwareItem(0, type, true, kCategories[i].mpName, L"");
			mpCategories[type]->mNode = mTreeView.AddVirtualItem(mTreeView.kNodeRoot, mTreeView.kNodeLast, mpCategories[type]);

			mDefaultIds[type] = mFwManager.GetDefaultFirmware(type);
		}

		typedef vdvector<ATFirmwareInfo> Firmwares;
		Firmwares firmwares;
		mFwManager.GetFirmwareList(firmwares);

		for(Firmwares::const_iterator it(firmwares.begin()), itEnd(firmwares.end());
			it != itEnd;
			++it)
		{
			if (!it->mbVisible)
				continue;

			if (mpCategories[it->mType]) {
				vdrefptr<FirmwareItem> item(new FirmwareItem(it->mId, it->mType, false, it->mName.c_str(), it->mPath.c_str()));
				item->mFlags = it->mFlags;
				item->mNode = mTreeView.AddVirtualItem(mpCategories[it->mType]->mNode, mTreeView.kNodeLast, item);
				item->mbDefault = (mDefaultIds[it->mType] == it->mId);

				UpdateSpecificNodes(item);
			}
		}

		for(size_t i=0; i<vdcountof(mpCategories); ++i) {
			if (mpCategories[i]) {
				FirmwareItemComparer comparer;
				mTreeView.SortChildren(mpCategories[i]->mNode, comparer);
			}
		}

		mTreeView.SetRedraw(true);
	}
}

bool ATUIDialogFirmware::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_ADD:
			Add();
			return true;

		case IDC_REMOVE:
			Remove();
			return true;

		case IDC_CLEAR:
			if (Confirm(L"This will remove all non-built-in firmware entries. Are you sure?")) {

				vdvector<ATFirmwareInfo> fws;
				mFwManager.GetFirmwareList(fws);

				for(auto it = fws.begin(), itEnd = fws.end(); it != itEnd; ++it) {
					mFwManager.RemoveFirmware(it->mId);
				}

				OnDataExchange(false);
			}
			return true;

		case IDC_SETTINGS:
			EditSettings();
			return true;

		case IDC_SCAN:
			ATUIScanForFirmware((VDGUIHandle)mhdlg, mFwManager);
			OnDataExchange(false);
			return true;

		case IDC_SETASDEFAULT:
			SetAsDefault();
			return true;

		case IDC_SETASSPECIFIC:
			SetAsSpecific();
			return true;
	}

	return false;
}

void ATUIDialogFirmware::OnDropFiles(IVDUIDropFileList *dropFileList) {
	VDStringW fn;

	try {
		if (dropFileList->GetFileName(0, fn))
			Add(fn.c_str());
	} catch(const MyError& e) {
		ShowError(e);
	}
}

void ATUIDialogFirmware::Add() {
	const VDStringW& path = BrowseForFirmware(this);

	if (path.empty())
		return;

	Add(path.c_str());
}

void ATUIDialogFirmware::Add(const wchar_t *path) {
	const uint64 id = ATGetFirmwareIdFromPath(path);

	vdrefptr<FirmwareItem> newItem(new FirmwareItem(id, kATFirmwareType_Unknown, false, VDFileSplitExtLeft(VDStringW(VDFileSplitPath(path))).c_str(), path));

	// try to autodetect it
	ATSpecificFirmwareType specificType = kATSpecificFirmwareType_None;
	try {
		VDFile f(path);

		sint64 size = f.size();

		if (ATFirmwareAutodetectCheckSize(size)) {
			uint32 size32 = (uint32)size;
			vdblock<char> buf(size32);

			f.read(buf.data(), size32);

			ATFirmwareInfo info;
			if (ATFirmwareAutodetect(buf.data(), size32, info, specificType)) {
				newItem->mText = info.mName;
				newItem->mFlags = info.mFlags;
				newItem->mType = info.mType;
			}
		}
	} catch(const MyError&) {
	}

	ATUIDialogEditFirmwareSettings dlg2(*newItem);
	if (dlg2.ShowDialog(this)) {
		newItem->mNode = mTreeView.AddVirtualItem(mpCategories[newItem->mType]->mNode, mTreeView.kNodeLast, newItem);
		if (newItem->mNode) {
			UpdateFirmwareItem(*newItem);

			if (specificType && !mFwManager.GetSpecificFirmware(specificType))
				mFwManager.SetSpecificFirmware(specificType, newItem->mId);

			UpdateSpecificNodes(newItem);

			mTreeView.RefreshNode(newItem->mNode);

			FirmwareItemComparer comparer;
			mTreeView.SortChildren(mpCategories[newItem->mType]->mNode, comparer);
			mTreeView.MakeNodeVisible(newItem->mNode);
			mTreeView.SelectNode(newItem->mNode);

			mbAnyChanges = true;
		}
	}
}

void ATUIDialogFirmware::Remove() {
	vdrefptr<FirmwareItem> item(static_cast<FirmwareItem *>(mTreeView.GetSelectedVirtualItem()));
	if (!item)
		return;

	if (item->mId < kATFirmwareId_Custom)
		return;

	item->mSpecificNodes.clear();
	mTreeView.DeleteItem(item->mNode);
	mFwManager.RemoveFirmware(item->mId);
	mbAnyChanges = true;
}

void ATUIDialogFirmware::EditSettings() {
	vdrefptr<FirmwareItem> item(static_cast<FirmwareItem *>(mTreeView.GetSelectedVirtualItem()));
	if (!item)
		return;

	if (item->mId < kATFirmwareId_Custom)
		return;

	const ATFirmwareType oldType = item->mType;
	ATUIDialogEditFirmwareSettings dlg2(*item);
	VDStringW name(item->mText);
	if (dlg2.ShowDialog(this)) {
		mTreeView.RefreshNode(item->mNode);

		UpdateFirmwareItem(*item);

		if (oldType != item->mType) {
			OnDataExchange(false);
		} else if (name.comparei(item->mText)) {
			FirmwareItemComparer comparer;
			mTreeView.SortChildren(mpCategories[item->mType]->mNode, comparer);
			mTreeView.MakeNodeVisible(item->mNode);
			mTreeView.SelectNode(item->mNode);
		}

		mbAnyChanges = true;
	}
}

void ATUIDialogFirmware::SetAsDefault() {
	vdrefptr<FirmwareItem> item(static_cast<FirmwareItem *>(mTreeView.GetSelectedVirtualItem()));
	if (!item)
		return;

	FirmwareItem *catItem = mpCategories[item->mType];

	if (catItem) {
		auto f = [&,this](IVDUITreeViewVirtualItem *curItem) {
			auto& fwitem = *static_cast<FirmwareItem *>(curItem);

			bool isDefault = (&fwitem == &*item);
			if (fwitem.mbDefault != isDefault) {
				fwitem.mbDefault = isDefault;

				mTreeView.RefreshNode(fwitem.mNode);
			}
		};

		mTreeView.EnumChildren(catItem->mNode, std::cref(f));
	}

	mFwManager.SetDefaultFirmware(item->mType, item->mId);
	mbAnyChanges = true;
}

void ATUIDialogFirmware::SetAsSpecific() {
	vdrefptr<FirmwareItem> item(static_cast<FirmwareItem *>(mTreeView.GetSelectedVirtualItem()));
	if (!item || item->mbCategory || !item->mId)
		return;

	VDLinearAllocator alloc;
	vdfastvector<const wchar_t *> items(1, L"Clear compatibility flags");
	vdfastvector<ATSpecificFirmwareType> menuLookup(1, kATSpecificFirmwareType_None);

	VDStringW label;
	for(uint32 i = 1; i < kATSpecificFirmwareTypeCount; ++i) {
		const auto ft = (ATSpecificFirmwareType)i;

		if (ATIsSpecificFirmwareTypeCompatible(item->mType, ft)) {
			label.sprintf(L"Use for software requiring: %ls", GetSpecificFirmwareLabel(ft));

			size_t len = sizeof(wchar_t) * (label.size() + 1);
			wchar_t *buf = (wchar_t *)alloc.Allocate(len);
			memcpy(buf, label.c_str(), len);
			items.push_back(buf);
			menuLookup.push_back(ft);
		}
	}

	items.push_back(nullptr);

	int index = ActivateMenuButton(IDC_SETASSPECIFIC, items.data());
	if (index == 0) {
		for(uint32 i = 1; i < kATSpecificFirmwareTypeCount; ++i) {
			ATSpecificFirmwareType ft = (ATSpecificFirmwareType)i;

			if (mFwManager.GetSpecificFirmware(ft) == item->mId)
				mFwManager.SetSpecificFirmware(ft, 0);
		}

		UpdateSpecificNodes(item);
		mTreeView.RefreshNode(item->mNode);
	} else if (index > 0 && index < (int)menuLookup.size()) {
		const ATSpecificFirmwareType ft = menuLookup[index];

		uint64 prevId = mFwManager.GetSpecificFirmware(ft);
		if (prevId == item->mId)
			mFwManager.SetSpecificFirmware(ft, 0);
		else {
			if (prevId) {
				FirmwareItem *itemToRefresh = nullptr;

				mTreeView.EnumChildrenRecursive(mTreeView.kNodeRoot,
					[prevId, &itemToRefresh](auto *item) {
						FirmwareItem *curItem = static_cast<FirmwareItem *>(item);

						if (curItem->mId == prevId)
							itemToRefresh = curItem;
					}
				);

				if (itemToRefresh)
					UpdateSpecificNodes(itemToRefresh);
			}

			mFwManager.SetSpecificFirmware(ft, item->mId);
		}

		UpdateSpecificNodes(item);
		mTreeView.RefreshNode(item->mNode);
	}
}

void ATUIDialogFirmware::UpdateFirmwareItem(const FirmwareItem& item) {
	ATFirmwareInfo info;
	info.mId = item.mId;
	info.mName = item.mText;
	info.mPath = item.mPath;
	info.mType = item.mType;
	info.mFlags = item.mFlags;
	mFwManager.AddFirmware(info);
}

void ATUIDialogFirmware::UpdateSpecificNodes(FirmwareItem *item) {
	if (!item)
		return;

	const auto id = item->mId;
	for(uint32 i = 1; i < kATSpecificFirmwareTypeCount; ++i) {
		auto type = (ATSpecificFirmwareType)i;

		if (id && mFwManager.GetSpecificFirmware(type) == id) {
			auto newNode = item->mSpecificNodes.insert_as(type);

			if (newNode.second) {
				VDStringW label;
				label.sprintf(L"(Use for software requiring: %ls)", GetSpecificFirmwareLabel(type));
				newNode.first->second = mTreeView.AddItem(item->mNode, mTreeView.kNodeLast, label.c_str());
				mTreeView.ExpandNode(item->mNode, true);
			}
		} else {
			auto existingNode = item->mSpecificNodes.find(type);

			if (existingNode != item->mSpecificNodes.end()) {
				mTreeView.DeleteItem(existingNode->second);
				item->mSpecificNodes.erase(existingNode);
			}
		}
	}
}

void ATUIDialogFirmware::OnSelChanged(VDUIProxyTreeViewControl *sender, int idx) {
	FirmwareItem *pItem = static_cast<FirmwareItem *>(sender->GetSelectedVirtualItem());

	if (pItem) {
		EnableControl(IDC_REMOVE, !pItem->mbCategory && pItem->mId >= kATFirmwareId_Custom);
		EnableControl(IDC_SETTINGS, !pItem->mbCategory && pItem->mId >= kATFirmwareId_Custom);
		EnableControl(IDC_SETASDEFAULT, !pItem->mbCategory);
		EnableControl(IDC_SETASSPECIFIC, !pItem->mbCategory);
	} else {
		EnableControl(IDC_REMOVE, false);
		EnableControl(IDC_SETTINGS, false);
		EnableControl(IDC_SETASDEFAULT, false);
		EnableControl(IDC_SETASSPECIFIC, false);
	}
}

void ATUIDialogFirmware::OnItemDoubleClicked(VDUIProxyTreeViewControl *sender, bool *handled) {
	FirmwareItem *pItem = static_cast<FirmwareItem *>(sender->GetSelectedVirtualItem());

	if (pItem && pItem->mId >= kATFirmwareId_Custom) {
		EditSettings();
		*handled = true;
	}
}

void ATUIDialogFirmware::OnItemGetDisplayAttributes(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::GetDispAttrEvent *event) {
	FirmwareItem *pItem = static_cast<FirmwareItem *>(event->mpItem);

	if (pItem)
		event->mbIsBold = pItem->mbCategory;
	else
		event->mbIsMuted = true;
}

void ATUIDialogFirmware::OnBeginEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::BeginEditEvent *event) {
	FirmwareItem *pItem = static_cast<FirmwareItem *>(event->mpItem);

	if (!pItem || pItem->mId < kATFirmwareId_Custom)
		event->mbAllowEdit = false;
	else {
		event->mbOverrideText = true;
		event->mOverrideText = pItem->mText;
	}
}

void ATUIDialogFirmware::OnEndEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::EndEditEvent *event) {
	FirmwareItem *pItem = static_cast<FirmwareItem *>(event->mpItem);

	if (pItem && pItem->mId >= kATFirmwareId_Custom && event->mpNewText)
		pItem->mText = event->mpNewText;
}

void ATUIShowDialogFirmware(VDGUIHandle hParent, ATFirmwareManager& fw, bool *anyChanges) {
	ATUIDialogFirmware dlg(fw);

	dlg.ShowDialog(hParent);

	*anyChanges = dlg.AnyChanges();
}
