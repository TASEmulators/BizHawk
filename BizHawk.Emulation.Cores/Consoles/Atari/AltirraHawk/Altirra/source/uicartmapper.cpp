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
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "cartridge.h"

uint32 ATCartridgeAutodetectMode(const void *data, uint32 size, vdfastvector<int>& cartModes);

class ATUIDialogCartridgeMapper : public VDDialogFrameW32 {
public:
	ATUIDialogCartridgeMapper(uint32 cartSize, const void *cartData);

	int GetMapper() const { return mMapper; }

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	static const wchar_t *GetModeName(int mode);
	static const wchar_t *GetModeDesc(int mode);

	int mMapper = 0;
	uint32 mCartSize = 0;
	const uint8 *mpCartData = nullptr;
	uint32 mRecommendedMapperIndex = 0;
	bool mbShow2600Warning = false;
	typedef vdfastvector<int> Mappers;
	Mappers mOriginalMappers;
	Mappers mMappers;

	VDUIProxyListView mModeList;
	VDUIProxyButtonControl mShowAllButton;
	VDUIProxyButtonControl mShowDetailsButton;

	struct MapSorter {
		bool operator()(int x, int y) const {
			// The terminology is a bit screwed here -- need to clean up mapper vs. mode.
			int xm = ATGetCartridgeMapperForMode((ATCartridgeMode)x, 0);
			int ym = ATGetCartridgeMapperForMode((ATCartridgeMode)y, 0);

			if (!xm)
				xm = 1000;

			if (!ym)
				ym = 1000;

			if (xm < ym)
				return true;

			if (xm == ym && xm == 1000 && wcscmp(GetModeName(x), GetModeName(y)) < 0)
				return true;

			return false;
		}
	};

	struct ModeItem final : public vdrefcounted<IVDUIListViewVirtualItem> {
		void GetText(int subItem, VDStringW& s) const override;

		ATCartridgeMode mMode = {};
		uint32 mSize = 0;
		bool mSuggested = false;
		bool mRecommended = false;
	};
};

void ATUIDialogCartridgeMapper::ModeItem::GetText(int subItem, VDStringW& s) const {
	switch(subItem) {
		case 0:
			{
				const int mapper = ATGetCartridgeMapperForMode(mMode, mSize);

				if (mapper)
					s.sprintf(L"%u", mapper);
			}
			break;

		case 1:
			if (mSuggested)
				s = L"*";
			s += GetModeName(mMode);
			if (mRecommended)
				s += L" (recommended)";
			break;

		case 2:
			s = GetModeDesc(mMode);
			break;
	}
}

ATUIDialogCartridgeMapper::ATUIDialogCartridgeMapper(uint32 cartSize, const void *cartData)
	: VDDialogFrameW32(IDD_CARTRIDGE_MAPPER)
	, mMapper(0)
	, mCartSize(cartSize)
	, mpCartData((const uint8 *)cartData)
{
	mbShow2600Warning = false;

	// Check if we see what looks like NMI, RESET, and IRQ handler addresses
	// in the Fxxx range. That highly likely indicates a 2600 cartridge.
	if ((cartSize == 2048 || cartSize == 4096) && cartData) {
		const uint8 *tail = (const uint8 *)cartData + cartSize - 6;

		if (tail[1] >= 0xF0 && tail[3] >= 0xF0 && tail[5] >= 0xF0)
			mbShow2600Warning = true;
	}

	mShowAllButton.SetOnClicked([this]() {
			OnDataExchange(false);
			OnDataExchange(true);
		}
	);

	mShowDetailsButton.SetOnClicked([this]() {
			OnDataExchange(false);
			OnDataExchange(true);
		}
	);

	mModeList.SetOnItemDoubleClicked(
		[this](int) {
			if (!OnOK())
				End(true);
		}
	);
}

bool ATUIDialogCartridgeMapper::OnLoaded() {
	mResizer.Add(IDC_SHOW_DETAILS, mResizer.kBL);
	mResizer.Add(IDC_SHOW_ALL, mResizer.kBC);
	mResizer.Add(IDC_STATIC_2600WARNING, mResizer.kBC);
	mResizer.Add(IDOK, mResizer.kBR);
	mResizer.Add(IDCANCEL, mResizer.kBR);
	mResizer.Add(IDC_LIST, mResizer.kMC | mResizer.kAvoidFlicker);

	AddProxy(&mShowAllButton, IDC_SHOW_ALL);
	AddProxy(&mShowDetailsButton, IDC_SHOW_DETAILS);
	AddProxy(&mModeList, IDC_LIST);

	mModeList.SetFullRowSelectEnabled(true);
	mModeList.InsertColumn(0, L"#", 0);

	mRecommendedMapperIndex = ATCartridgeAutodetectMode(mpCartData, mCartSize, mOriginalMappers);
	
	ShowControl(IDC_STATIC_2600WARNING, mbShow2600Warning);

	OnDataExchange(false);

	if (!mMappers.empty())
		mModeList.Focus();
	return true;
}

void ATUIDialogCartridgeMapper::OnDataExchange(bool write) {
	if (write) {
		int idx = mModeList.GetSelectedIndex();

		if (idx < 0 || (uint32)idx >= mMappers.size()) {
			FailValidation(IDC_LIST);
			return;
		}

		mMapper = mMappers[idx];
	} else {
		mMappers = mOriginalMappers;

		if (mShowAllButton.GetChecked()) {
			vdfastvector<bool> mappersDetected(kATCartridgeModeCount, false);

			// skip these mappers as they are not meant to be visible
			mappersDetected[kATCartridgeMode_None] = true;
			mappersDetected[kATCartridgeMode_SuperCharger3D] = true;

			for(int mapper : mMappers) {
				if (mapper >= 0 && mapper < kATCartridgeModeCount)
					mappersDetected[mapper] = true;
			}

			// list all unmatched mappers that have a CAR mapping, in that order
			for(int i=0; i<=kATCartridgeMapper_Max; ++i) {
				ATCartridgeMode mode = ATGetCartridgeModeForMapper(i);

				if (!mappersDetected[mode]) {
					mappersDetected[mode] = true;
					mMappers.push_back(mode);
				}
			}

			// list anything else selectable that we haven't already listed
			for(int i=1; i<kATCartridgeModeCount; ++i) {
				if (!mappersDetected[i])
					mMappers.push_back(i);
			}
		}
				
		if (mMappers.empty()) {
			mModeList.Hide();
			mModeList.SetEnabled(false);
			ShowControl(IDC_STATIC_NONEFOUND, true);
			EnableControl(IDOK, false);
		} else {
			ShowControl(IDC_STATIC_NONEFOUND, false);
			mModeList.SetRedraw(false);
			mModeList.Clear();
			mModeList.SetEnabled(true);
			EnableControl(IDOK, true);

			mModeList.ClearExtraColumns();
			mModeList.InsertColumn(1, L"Name", 0, false);

			if (mShowDetailsButton.GetChecked())
				mModeList.InsertColumn(2, L"Details", 0, false);

			auto it0 = mMappers.begin();
			auto it1 = it0 + mRecommendedMapperIndex;
			auto it2 = it1;
			auto it3 = mMappers.end();

			// sort recommended mappers first
			std::sort(it0, it1, MapSorter());

			// if we had recommended mappers and all are the same system type, bubble the
			// non-recommended ones of the same system type to the top

			if (mRecommendedMapperIndex) {
				const bool firstIs5200 = ATIsCartridge5200Mode((ATCartridgeMode)*it0);

				if (std::find_if(it0+1, it1, [=](uint32 key) { return (firstIs5200 != ATIsCartridge5200Mode((ATCartridgeMode)key)); }) == it1) {
					it2 = std::partition(it1, it3, [=](uint32 key) { return (firstIs5200 == ATIsCartridge5200Mode((ATCartridgeMode)key)); });
				}
			}

			std::sort(it1, it2, MapSorter());
			std::sort(it2, it3, MapSorter());

			uint32 i = 0;
			for(Mappers::const_iterator it(mMappers.begin()), itEnd(mMappers.end()); it != itEnd; ++it, ++i) {
				vdrefptr<ModeItem> modeItem(new ModeItem);

				modeItem->mMode = (ATCartridgeMode)*it;

				if (i < mRecommendedMapperIndex && mRecommendedMapperIndex > 1)
					modeItem->mSuggested = true;

				if (i < mRecommendedMapperIndex && mRecommendedMapperIndex == 1)
					modeItem->mRecommended = true;

				mModeList.InsertVirtualItem(-1, modeItem);
			}

			mModeList.SetSelectedIndex(0);
			mModeList.AutoSizeColumns(true);
			mModeList.SetRedraw(true);
			mModeList.Show();
		}
	}
}

const wchar_t *ATUIDialogCartridgeMapper::GetModeName(int mode) {
	switch(mode) {
		case kATCartridgeMode_8K:					return L"8K";
		case kATCartridgeMode_16K:					return L"16K";
		case kATCartridgeMode_OSS_034M:				return L"OSS '034M'";
		case kATCartridgeMode_5200_32K:				return L"5200 32K";
		case kATCartridgeMode_DB_32K:				return L"DB 32K";
		case kATCartridgeMode_5200_16K_TwoChip:		return L"5200 16K (two chip)";
		case kATCartridgeMode_BountyBob5200:		return L"Bounty Bob (5200)";
		case kATCartridgeMode_Williams_64K:			return L"Williams 64K";
		case kATCartridgeMode_Express_64K:			return L"Express 64K";
		case kATCartridgeMode_Diamond_64K:			return L"Diamond 64K";
		case kATCartridgeMode_SpartaDosX_64K:		return L"SpartaDOS X 64K";
		case kATCartridgeMode_XEGS_32K:				return L"32K XEGS";
		case kATCartridgeMode_XEGS_64K:				return L"64K XEGS";
		case kATCartridgeMode_XEGS_128K:			return L"128K XEGS";
		case kATCartridgeMode_OSS_M091:				return L"OSS 'M091'";
		case kATCartridgeMode_5200_16K_OneChip:		return L"5200 16K (one chip)";
		case kATCartridgeMode_Atrax_128K:			return L"Atrax 128K (decoded order)";
		case kATCartridgeMode_BountyBob800:			return L"Bounty Bob (800)";
		case kATCartridgeMode_5200_8K:				return L"5200 8K";
		case kATCartridgeMode_5200_4K:				return L"5200 4K";
		case kATCartridgeMode_RightSlot_8K:			return L"Right slot 8K";
		case kATCartridgeMode_Williams_32K:			return L"Williams 32K";
		case kATCartridgeMode_XEGS_256K:			return L"256K XEGS";
		case kATCartridgeMode_XEGS_512K:			return L"512K XEGS";
		case kATCartridgeMode_XEGS_1M:				return L"1M XEGS";
		case kATCartridgeMode_MegaCart_16K:			return L"16K MegaCart";
		case kATCartridgeMode_MegaCart_32K:			return L"32K MegaCart";
		case kATCartridgeMode_MegaCart_64K:			return L"64K MegaCart";
		case kATCartridgeMode_MegaCart_128K:		return L"128K MegaCart";
		case kATCartridgeMode_MegaCart_256K:		return L"256K MegaCart";
		case kATCartridgeMode_MegaCart_512K:		return L"512K MegaCart";
		case kATCartridgeMode_MegaCart_1M:			return L"1M MegaCart";
		case kATCartridgeMode_MegaCart_2M:			return L"2M MegaCart";
		case kATCartridgeMode_Switchable_XEGS_32K:	return L"32K Switchable XEGS";
		case kATCartridgeMode_Switchable_XEGS_64K:	return L"64K Switchable XEGS";
		case kATCartridgeMode_Switchable_XEGS_128K:	return L"128K Switchable XEGS";
		case kATCartridgeMode_Switchable_XEGS_256K:	return L"256K Switchable XEGS";
		case kATCartridgeMode_Switchable_XEGS_512K:	return L"512K Switchable XEGS";
		case kATCartridgeMode_Switchable_XEGS_1M:	return L"1M Switchable XEGS";
		case kATCartridgeMode_Phoenix_8K:			return L"Phoenix 8K";
		case kATCartridgeMode_Blizzard_16K:			return L"Blizzard 16K";
		case kATCartridgeMode_MaxFlash_128K:		return L"MaxFlash 128K / 1Mbit";
		case kATCartridgeMode_MaxFlash_1024K:		return L"MaxFlash 1M / 8Mbit - older (bank 127)";
		case kATCartridgeMode_SpartaDosX_128K:		return L"SpartaDOS X 128K";
		case kATCartridgeMode_OSS_8K:				return L"OSS 8K";
		case kATCartridgeMode_OSS_043M:				return L"OSS '043M'";
		case kATCartridgeMode_Blizzard_4K:			return L"Blizzard 4K";
		case kATCartridgeMode_AST_32K:				return L"AST 32K";
		case kATCartridgeMode_Atrax_SDX_64K:		return L"Atrax SDX 64K";
		case kATCartridgeMode_Atrax_SDX_128K:		return L"Atrax SDX 128K";
		case kATCartridgeMode_Turbosoft_64K:		return L"Turbosoft 64K";
		case kATCartridgeMode_Turbosoft_128K:		return L"Turbosoft 128K";
		case kATCartridgeMode_MaxFlash_128K_MyIDE:	return L"MaxFlash 128K + MyIDE";
		case kATCartridgeMode_Corina_1M_EEPROM:		return L"Corina 1M + 8K EEPROM";
		case kATCartridgeMode_Corina_512K_SRAM_EEPROM:	return L"Corina 512K + 512K SRAM + 8K EEPROM";
		case kATCartridgeMode_TelelinkII:			return L"8K Telelink II";
		case kATCartridgeMode_SIC:					return L"SIC!";
		case kATCartridgeMode_MaxFlash_1024K_Bank0:	return L"MaxFlash 1M / 8Mbit - newer (bank 0)";
		case kATCartridgeMode_MegaCart_1M_2:		return L"Megacart 1M (2)";
		case kATCartridgeMode_5200_64K_32KBanks:	return L"5200 64K cartridge (32K banks)";
		case kATCartridgeMode_5200_512K_32KBanks:	return L"5200 512K cartridge (32K banks)";
		case kATCartridgeMode_MicroCalc:			return L"MicroCalc 32K";
		case kATCartridgeMode_2K:					return L"2K";
		case kATCartridgeMode_4K:					return L"4K";
		case kATCartridgeMode_RightSlot_4K:			return L"Right slot 4K";
		case kATCartridgeMode_Blizzard_32K:			return L"Blizzard 32K";
		case kATCartridgeMode_MegaCart_512K_3:		return L"MegaCart 512K (3)";
		case kATCartridgeMode_MegaMax_2M:			return L"MegaMax 2M";
		case kATCartridgeMode_TheCart_128M:			return L"The!Cart 128M";
		case kATCartridgeMode_MegaCart_4M_3:		return L"MegaCart 4M (3)";
		case kATCartridgeMode_TheCart_32M:			return L"The!Cart 32M";
		case kATCartridgeMode_TheCart_64M:			return L"The!Cart 64M";
		case kATCartridgeMode_BountyBob5200Alt:		return L"Bounty Bob (5200) - Alternate layout";
		case kATCartridgeMode_XEGS_64K_Alt:			return L"XEGS 64K (alternate)";
		case kATCartridgeMode_Atrax_128K_Raw:		return L"Atrax 128K (raw order)";
		case kATCartridgeMode_aDawliah_32K:			return L"aDawliah 32K";
		case kATCartridgeMode_aDawliah_64K:			return L"aDawliah 64K";

		// These modes should not be hit
		case kATCartridgeMode_SuperCharger3D:
		default:
			VDASSERT(false);
			return L"";
	}
}

const wchar_t *ATUIDialogCartridgeMapper::GetModeDesc(int mode) {
	switch(mode) {
		case kATCartridgeMode_8K:					return L"8K fixed";
		case kATCartridgeMode_16K:					return L"16K fixed";
		case kATCartridgeMode_OSS_034M:				return L"4K banked by CCTL data + 4K fixed";
		case kATCartridgeMode_5200_32K:				return L"32K fixed";
		case kATCartridgeMode_DB_32K:				return L"8K banked by CCTL address + 8K fixed";
		case kATCartridgeMode_5200_16K_TwoChip:		return L"16K fixed";
		case kATCartridgeMode_BountyBob800:
		case kATCartridgeMode_BountyBob5200:
		case kATCartridgeMode_BountyBob5200Alt:		return L"4K+4K banked by $4/5FF6-9 + 8K fixed";
		case kATCartridgeMode_Williams_64K:			return L"8K banked by CCTL address (switchable)";
		case kATCartridgeMode_Express_64K:			return L"8K banked by CCTL $D57x (switchable)";
		case kATCartridgeMode_Diamond_64K:			return L"8K banked by CCTL $D5Dx (switchable)";
		case kATCartridgeMode_Atrax_SDX_64K:
		case kATCartridgeMode_SpartaDosX_64K:		return L"8K banked by CCTL $D5Ex (switchable)";

		case kATCartridgeMode_XEGS_32K:
		case kATCartridgeMode_XEGS_64K:
		case kATCartridgeMode_XEGS_64K_Alt:
		case kATCartridgeMode_XEGS_128K:
		case kATCartridgeMode_XEGS_256K:
		case kATCartridgeMode_XEGS_512K:
		case kATCartridgeMode_XEGS_1M:				return L"8K banked by CCTL data + 8K fixed (switchable)";

		case kATCartridgeMode_OSS_M091:				return L"4K banked by CCTL data + 4K fixed";
		case kATCartridgeMode_5200_16K_OneChip:		return L"16K fixed";
		case kATCartridgeMode_Atrax_128K:
		case kATCartridgeMode_Atrax_128K_Raw:		return L"8K banked by CCTL data (switchable)";
		case kATCartridgeMode_5200_8K:				return L"8K fixed";
		case kATCartridgeMode_5200_4K:				return L"4K fixed";
		case kATCartridgeMode_RightSlot_8K:			return L"8K right slot fixed";
		case kATCartridgeMode_Williams_32K:			return L"8K banked by CCTL address (switchable)";

		case kATCartridgeMode_MegaCart_16K:
		case kATCartridgeMode_MegaCart_32K:
		case kATCartridgeMode_MegaCart_64K:
		case kATCartridgeMode_MegaCart_128K:
		case kATCartridgeMode_MegaCart_256K:
		case kATCartridgeMode_MegaCart_512K:
		case kATCartridgeMode_MegaCart_1M:
		case kATCartridgeMode_MegaCart_2M:			return L"16K banked by CCTL data (switchable)";

		case kATCartridgeMode_Switchable_XEGS_32K:
		case kATCartridgeMode_Switchable_XEGS_64K:
		case kATCartridgeMode_Switchable_XEGS_128K:
		case kATCartridgeMode_Switchable_XEGS_256K:
		case kATCartridgeMode_Switchable_XEGS_512K:
		case kATCartridgeMode_Switchable_XEGS_1M:	return L"8K banked by CCTL data + 8K fixed (switchable)";

		case kATCartridgeMode_Phoenix_8K:			return L"8K fixed (one-time disable)";
		case kATCartridgeMode_Blizzard_4K:			return L"8K fixed (one-time disable)";
		case kATCartridgeMode_Blizzard_16K:			return L"16K fixed (one-time disable)";
		case kATCartridgeMode_Blizzard_32K:			return L"8K banked (autoincrement + disable)";

		case kATCartridgeMode_MaxFlash_128K:		return L"8K banked by CCTL address (switchable)";
		case kATCartridgeMode_MaxFlash_1024K:		return L"8K banked by CCTL address (switchable)";
		case kATCartridgeMode_MaxFlash_1024K_Bank0:	return L"8K banked by CCTL address (switchable)";
		case kATCartridgeMode_MaxFlash_128K_MyIDE:	return L"8K banked + CCTL keyhole (switchable)";

		case kATCartridgeMode_Atrax_SDX_128K:
		case kATCartridgeMode_SpartaDosX_128K:		return L"8K banked by CCTL $D5E0-D5FF address (switchable)";

		case kATCartridgeMode_OSS_8K:				return L"4K banked by CCTL data + 4K fixed";
		case kATCartridgeMode_OSS_043M:				return L"4K banked by CCTL data + 4K fixed";
		case kATCartridgeMode_AST_32K:				return L"8K disableable + CCTL autoincrement by write";

		case kATCartridgeMode_Turbosoft_64K:		
		case kATCartridgeMode_Turbosoft_128K:		return L"8K banked by CCTL address (switchable)";

		case kATCartridgeMode_Corina_1M_EEPROM:
		case kATCartridgeMode_Corina_512K_SRAM_EEPROM:	return L"8K+8K banked (complex)";

		case kATCartridgeMode_TelelinkII:			return L"8K fixed + EEPROM";
		case kATCartridgeMode_SIC:					return L"16K banked by CCTL $D500-D51F access (8K+8K switchable)";
		case kATCartridgeMode_MegaCart_1M_2:		return L"8K banked by CCTL data (switchable)";
		case kATCartridgeMode_5200_64K_32KBanks:	return L"32K banked by $BFD0-BFFF access";
		case kATCartridgeMode_5200_512K_32KBanks:	return L"32K banked by $BFC0-BFFF access";
		case kATCartridgeMode_MicroCalc:			return L"8K banked by CCTL access (autoincrement, switchable)";
		case kATCartridgeMode_2K:					return L"2K fixed";
		case kATCartridgeMode_4K:					return L"4K fixed";
		case kATCartridgeMode_RightSlot_4K:			return L"4K fixed right slot";
		case kATCartridgeMode_MegaCart_512K_3:		return L"16K banked by CCTL data (switchable)";
		case kATCartridgeMode_MegaMax_2M:			return L"16K banked by CCTL address (switchable)";
		case kATCartridgeMode_MegaCart_4M_3:		return L"16K banked by CCTL data (switchable)";

		case kATCartridgeMode_TheCart_32M:
		case kATCartridgeMode_TheCart_64M:
		case kATCartridgeMode_TheCart_128M:			return L"8K+8K banked (complex)";

		case kATCartridgeMode_aDawliah_32K:			return L"8K banked by CCTL access (autoincrement)";
		case kATCartridgeMode_aDawliah_64K:			return L"8K banked by CCTL access (autoincrement)";

		// These modes should not be hit
		case kATCartridgeMode_SuperCharger3D:
		default:
			VDASSERT(false);
			return L"";
	}
}

int ATUIShowDialogCartridgeMapper(VDGUIHandle h, uint32 cartSize, const void *data) {
	ATUIDialogCartridgeMapper mapperdlg(cartSize, data);

	return mapperdlg.ShowDialog(h) ? mapperdlg.GetMapper() : -1;
}
