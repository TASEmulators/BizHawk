#include <stdafx.h>

#include <windows.h>
#include <commctrl.h>
#include <vd2/system/filesys.h>
#include <vd2/system/strutil.h>
#include <vd2/Dita/accel.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include <at/atnativeui/hotkeyexcontrol.h>
#include "resource.h"


extern const wchar_t g_wszWarning[];

///////////////////////////////////////////////////////////////////////////

class VDDialogEditAccelerators : public VDResizableDialogFrameW32 {
public:
	VDDialogEditAccelerators(const VDAccelToCommandEntry *commands,
		uint32 commandCount,
		VDAccelTableDefinition *tables,
		const VDAccelTableDefinition *defaultTables,
		uint32 tableCount,
		const wchar_t *const *contextNames);
	~VDDialogEditAccelerators();

protected:
	void OnDataExchange(bool write);
	bool OnLoaded();
	bool OnCommand(uint32 id, uint32 extcode);
	void OnSize();
	void OnDestroy();
	bool OnErase(VDZHDC hdc);
	void LoadTables(const VDAccelTableDefinition *tables);
	void RefilterCommands(const char *pattern);
	void RefreshBoundList();
	void DestroyBoundCommands();

	void OnColumnClicked(VDUIProxyListView *source, int column);
	void OnItemSelectionChanged(VDUIProxyListView *source, int index);
	void OnHotKeyChanged(IVDUIHotKeyExControl *source, const VDUIAccelerator& accel);

	typedef vdfastvector<const VDAccelToCommandEntry *> Commands;

	uint32		mTableCount;
	const wchar_t *const *mpContextNames;

	Commands	mAllCommands;
	Commands	mFilteredCommands;

	struct BoundCommand : public vdrefcounted<IVDUIListViewVirtualItem>, public VDAccelTableEntry {
		void GetText(int subItem, VDStringW& s) const;

		uint32 mContext;
		const wchar_t *mpContextName;
	};

	struct BoundCommandSort {
		bool operator()(const BoundCommand *x, const BoundCommand *y) const;

		int		mSortAxes[3];
	};

	BoundCommandSort	mBoundCommandSort;

	typedef vdfastvector<BoundCommand *> BoundCommands;
	BoundCommands	mBoundCommands;
	VDAccelTableDefinition *mpBoundCommandsResults;
	const VDAccelTableDefinition *mpBoundCommandsDefaults;

	VDUIProxyComboBoxControl	mComboContext;
	VDUIProxyListView		mListViewBoundCommands;
	vdrefptr<IVDUIHotKeyExControl>	mpHotKeyControl;

	VDDelegate	mDelegateColumnClicked;
	VDDelegate	mDelegateItemSelectionChanged;
	VDDelegate	mDelegateHotKeyChanged;

	bool	mbBlockCommandUpdate;
};

namespace {
	struct CommandSort {
		bool operator()(const VDAccelToCommandEntry *x, const VDAccelToCommandEntry *y) const {
			return _stricmp(x->mpName, y->mpName) < 0;
		}
	};
}

bool VDDialogEditAccelerators::BoundCommandSort::operator()(const BoundCommand *x, const BoundCommand *y) const {
	for(int i=0; i<3; ++i) {
		int axis = mSortAxes[i];
		int r;

		switch(axis) {
			case 0:
				if (x->mContext != y->mContext)
					return x->mContext < y->mContext;
				break;

			case 1:
				if (x->mContext != y->mContext)
					return x->mContext > y->mContext;
				break;

			case 2:
				r = vdstricmp(x->mpCommand, y->mpCommand);
				if (r)
					return r < 0;
				break;

			case 3:
				r = vdstricmp(x->mpCommand, y->mpCommand);
				if (r)
					return r > 0;
				break;

			case 4:
				if (x->mAccel.mModifiers != y->mAccel.mModifiers)
					return x->mAccel.mModifiers < y->mAccel.mModifiers;

				if (x->mAccel.mVirtKey != y->mAccel.mVirtKey)
					return x->mAccel.mVirtKey < y->mAccel.mVirtKey;
				break;

			case 5:
				if (x->mAccel.mModifiers != y->mAccel.mModifiers)
					return x->mAccel.mModifiers > y->mAccel.mModifiers;

				if (x->mAccel.mVirtKey != y->mAccel.mVirtKey)
					return x->mAccel.mVirtKey > y->mAccel.mVirtKey;
				break;
		}
	}

	return false;
}

VDDialogEditAccelerators::VDDialogEditAccelerators(
	const VDAccelToCommandEntry *commands,
	uint32 commandCount,
	VDAccelTableDefinition *tables,
	const VDAccelTableDefinition *defaultTables,
	uint32 tableCount,
	const wchar_t *const *contextNames)
	: VDResizableDialogFrameW32(IDD_CONFIGURE_ACCEL)
	, mTableCount(tableCount)
	, mpContextNames(contextNames)
	, mAllCommands(commandCount)
	, mpBoundCommandsResults(tables)
	, mpBoundCommandsDefaults(defaultTables)
	, mbBlockCommandUpdate(false)
{
	mBoundCommandSort.mSortAxes[0] = 0;
	mBoundCommandSort.mSortAxes[1] = 4;
	mBoundCommandSort.mSortAxes[2] = 2;

	for(uint32 i=0; i<commandCount; ++i)
		mAllCommands[i] = &commands[i];

	std::sort(mAllCommands.begin(), mAllCommands.end(), CommandSort());

	mListViewBoundCommands.OnColumnClicked() += mDelegateColumnClicked.Bind(this, &VDDialogEditAccelerators::OnColumnClicked);
	mListViewBoundCommands.OnItemSelectionChanged() += mDelegateItemSelectionChanged.Bind(this, &VDDialogEditAccelerators::OnItemSelectionChanged);
}

VDDialogEditAccelerators::~VDDialogEditAccelerators() {
	DestroyBoundCommands();
}

void VDDialogEditAccelerators::OnDataExchange(bool write) {
	if (write) {
		size_t n = mBoundCommands.size();
	
		for(uint32 context=0; context<mTableCount; ++context) {
			VDAccelTableDefinition newTable;

			for(size_t i=0; i<n; ++i) {
				const BoundCommand& ent = *mBoundCommands[i];

				if (ent.mContext == context)
					newTable.Add(ent);
			}

			mpBoundCommandsResults[context].Swap(newTable);
		}
	} else {
		LoadTables(mpBoundCommandsResults);
	}
}

bool VDDialogEditAccelerators::OnLoaded() {
	//VDSetDialogDefaultIcons(mhdlg);

	mpHotKeyControl = VDGetUIHotKeyExControl((VDGUIHandle)GetControl(IDC_HOTKEY));
	if (mpHotKeyControl)
		mpHotKeyControl->OnChange() += mDelegateHotKeyChanged(this, &VDDialogEditAccelerators::OnHotKeyChanged);

	mResizer.Add(IDOK, VDDialogResizerW32::kBR);
	mResizer.Add(IDCANCEL, VDDialogResizerW32::kBR);
	mResizer.Add(IDC_ADD, VDDialogResizerW32::kBR);
	mResizer.Add(IDC_REMOVE, VDDialogResizerW32::kBR);
	mResizer.Add(IDC_RESET, VDDialogResizerW32::kBL);
	mResizer.Add(IDC_HOTKEY, VDDialogResizerW32::kBC);
	mResizer.Add(IDC_STATIC_QUICKSEARCH, VDDialogResizerW32::kBL);
	mResizer.Add(IDC_STATIC_SHORTCUT, VDDialogResizerW32::kBL);
	mResizer.Add(IDC_STATIC_AVAILABLECOMMANDS, VDDialogResizerW32::kAnchorX2_C);
	mResizer.Add(IDC_STATIC_BOUNDCOMMANDS, VDDialogResizerW32::kAnchorX1_C | VDDialogResizerW32::kAnchorX2_R);
	mResizer.Add(IDC_AVAILCOMMANDS, VDDialogResizerW32::kAnchorX2_C | VDDialogResizerW32::kAnchorY2_B | VDDialogResizerW32::kAvoidFlicker);
	mResizer.Add(IDC_BOUNDCOMMANDS, VDDialogResizerW32::kAnchorX1_C
		| VDDialogResizerW32::kAnchorX2_R
		| VDDialogResizerW32::kAnchorY2_B
		| VDDialogResizerW32::kAvoidFlicker
		);
	mResizer.Add(IDC_FILTER, VDDialogResizerW32::kAnchorY1_B | VDDialogResizerW32::kAnchorX2_C | VDDialogResizerW32::kAnchorY2_B);
	mResizer.Add(IDC_HOTKEY, VDDialogResizerW32::kBC | VDDialogResizerW32::kAvoidFlicker);
	mResizer.Add(IDC_CONTEXT, VDDialogResizerW32::kBL | VDDialogResizerW32::kAvoidFlicker);
	mResizer.Add(IDC_KEYUP, VDDialogResizerW32::kBL);

	AddProxy(&mListViewBoundCommands, IDC_BOUNDCOMMANDS);
	AddProxy(&mComboContext, IDC_CONTEXT);

	for(uint32 i=0; i<mTableCount; ++i)
		mComboContext.AddItem(mpContextNames[i]);

	mComboContext.SetSelection(0);

	mListViewBoundCommands.SetFullRowSelectEnabled(true);
	mListViewBoundCommands.InsertColumn(0, L"Context", 50);
	mListViewBoundCommands.InsertColumn(1, L"Command", 50);
	mListViewBoundCommands.InsertColumn(2, L"Shortcut", 50);
	mListViewBoundCommands.AutoSizeColumns();

	RefilterCommands("*");

	VDDialogFrameW32::OnLoaded();

	SetFocusToControl(IDC_FILTER);
	return true;
}

bool VDDialogEditAccelerators::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_FILTER) {
		if (extcode == EN_CHANGE) {
			VDStringA s("*");
			s += VDTextWToA(GetControlValueString(id)).c_str();
			s += '*';

			RefilterCommands(s.c_str());
			return true;
		}
	} else if (id == IDC_ADD) {
		VDUIAccelerator accel;

		int selIdx = LBGetSelectedIndex(IDC_AVAILCOMMANDS);

		if ((size_t)selIdx < mFilteredCommands.size()) {
			const VDAccelToCommandEntry *ace = mFilteredCommands[selIdx];

			if (mpHotKeyControl) {
				mpHotKeyControl->GetAccelerator(accel);

				// Look for a conflicting command.
				for(BoundCommands::iterator it(mBoundCommands.begin()), itEnd(mBoundCommands.end()); it != itEnd; ++it) {
					BoundCommand *obc = *it;

					if (obc->mAccel == accel) {
						VDStringW keyName;
						VDUIGetAcceleratorString(accel, keyName);

						VDStringW msg;
						msg.sprintf(L"The key %ls is already bound to %hs. Rebind it to %hs?", keyName.c_str(), obc->mpCommand, ace->mpName);

						if (IDOK != MessageBoxW(mhdlg, msg.c_str(), g_wszWarning, MB_OKCANCEL | MB_ICONEXCLAMATION))
							return true;

						mBoundCommands.erase(it);
						obc->Release();
					}
				}

				vdrefptr<BoundCommand> bc(new_nothrow BoundCommand);
				
				if (bc) {
					bc->mpCommand = ace->mpName;
					bc->mCommandId = ace->mId;
					bc->mAccel = accel;

					int context = mComboContext.GetSelection();
					if (context < 0 || context >= (int)mTableCount)
						context = 0;

					bc->mContext = context;
					bc->mpContextName = mpContextNames[context];

					mBoundCommands.push_back(bc.release());
					RefreshBoundList();
				}
			}
		}

		return true;
	} else if (id == IDC_REMOVE) {
		int selIdx = mListViewBoundCommands.GetSelectedIndex();

		if ((unsigned)selIdx < mBoundCommands.size()) {
			BoundCommand *bc = mBoundCommands[selIdx];

			mBoundCommands.erase(mBoundCommands.begin() + selIdx);

			bc->Release();

			RefreshBoundList();
		}

		return true;
	} else if (id == IDC_KEYUP) {
		if (mpHotKeyControl) {
			const bool up = IsButtonChecked(id);

			VDUIAccelerator accel;
			mpHotKeyControl->GetAccelerator(accel);

			const bool wasUp = (accel.mModifiers & VDUIAccelerator::kModUp) != 0;

			if (up != wasUp) {
				accel.mModifiers ^= VDUIAccelerator::kModUp;
				mpHotKeyControl->SetAccelerator(accel);
			}
		}
	} else if (id == IDC_RESET) {
		if (IDOK == MessageBoxW(mhdlg, L"Really reset?", g_wszWarning, MB_OKCANCEL | MB_ICONEXCLAMATION))
			LoadTables(mpBoundCommandsDefaults);

		return true;
	}

	return false;
}

void VDDialogEditAccelerators::OnSize() {
	mResizer.Relayout();
}

void VDDialogEditAccelerators::OnDestroy() {
	mListViewBoundCommands.Clear();
}

bool VDDialogEditAccelerators::OnErase(VDZHDC hdc) {
	mResizer.Erase(&hdc);
	return true;
}

void VDDialogEditAccelerators::RefilterCommands(const char *pattern) {
	mFilteredCommands.clear();

	LBClear(IDC_AVAILCOMMANDS);

	Commands::const_iterator it(mAllCommands.begin()), itEnd(mAllCommands.end());
	for(; it != itEnd; ++it) {
		const VDAccelToCommandEntry& ent = **it;

		if (VDFileWildMatch(pattern, ent.mpName)) {
			const VDStringW s(VDTextAToW(ent.mpName));

			mFilteredCommands.push_back(&ent);
			LBAddString(IDC_AVAILCOMMANDS, s.c_str());
		}
	}

	LBSetSelectedIndex(IDC_AVAILCOMMANDS, 0);
}

void VDDialogEditAccelerators::LoadTables(const VDAccelTableDefinition *tables) {
	DestroyBoundCommands();

	for(uint32 context=0; context<mTableCount; ++context) {
		const VDAccelTableDefinition& table = tables[context];
		uint32 n = table.GetSize();
		mBoundCommands.reserve(mBoundCommands.size() + n);

		for(uint32 i=0; i<n; ++i) {
			vdrefptr<BoundCommand> bc(new_nothrow BoundCommand);
			if (!bc)
				break;

			const VDAccelTableEntry& ent = table[i];

			static_cast<VDAccelTableEntry&>(*bc) = ent;
			bc->mContext = context;
			bc->mpContextName = mpContextNames[context];

			mBoundCommands.push_back(bc.release());
		}
	}

	RefreshBoundList();
}

void VDDialogEditAccelerators::RefreshBoundList() {
	int visIdx = mListViewBoundCommands.GetVisibleTopIndex();

	std::sort(mBoundCommands.begin(), mBoundCommands.end(), mBoundCommandSort);

	mListViewBoundCommands.SetRedraw(false);
	mListViewBoundCommands.Clear();

	BoundCommands::const_iterator it(mBoundCommands.begin()), itEnd(mBoundCommands.end());
	int index = 0;

	for(; it != itEnd; ++it) {
		BoundCommand *bc = *it;

		mListViewBoundCommands.InsertVirtualItem(index++, bc);
	}

	mListViewBoundCommands.AutoSizeColumns();
	mListViewBoundCommands.SetVisibleTopIndex(visIdx);
	mListViewBoundCommands.SetRedraw(true);
}

void VDDialogEditAccelerators::DestroyBoundCommands() {
	mListViewBoundCommands.Clear();

	while(!mBoundCommands.empty()) {
		BoundCommand *bc = mBoundCommands.back();
		mBoundCommands.pop_back();

		bc->Release();
	}
}

void VDDialogEditAccelerators::OnColumnClicked(VDUIProxyListView *source, int column) {
	for(int i=0; i<3; ++i) {
		if ((mBoundCommandSort.mSortAxes[i] >> 1) == column) {
			if (i == 0)
				mBoundCommandSort.mSortAxes[i] ^= 1;
			else
				std::rotate(mBoundCommandSort.mSortAxes, mBoundCommandSort.mSortAxes + i, mBoundCommandSort.mSortAxes + i + 1);

			break;
		}
	}

	RefreshBoundList();
}

void VDDialogEditAccelerators::OnItemSelectionChanged(VDUIProxyListView *source, int index) {
	if (index < 0 || mbBlockCommandUpdate)
		return;

	const BoundCommand& bcmd = *mBoundCommands[index];

	if (mpHotKeyControl)
		mpHotKeyControl->SetAccelerator(bcmd.mAccel);

	CheckButton(IDC_KEYUP, (bcmd.mAccel.mModifiers & VDUIAccelerator::kModUp) != 0);

	uint32 n = (uint32)mFilteredCommands.size();
	int cmdSelIndex = -1;

	for(uint32 i=0; i<n; ++i) {
		const VDAccelToCommandEntry& cent = *mFilteredCommands[i];

		if (!_stricmp(cent.mpName, bcmd.mpCommand)) {
			cmdSelIndex = i;
			break;
		}
	}

	LBSetSelectedIndex(IDC_AVAILCOMMANDS, cmdSelIndex);
}

void VDDialogEditAccelerators::OnHotKeyChanged(IVDUIHotKeyExControl *source, const VDUIAccelerator& accel) {
	BoundCommands::const_iterator it(mBoundCommands.begin()), itEnd(mBoundCommands.end());
	int index = 0;

	for(; it != itEnd; ++it, ++index) {
		BoundCommand *bc = *it;

		if (bc->mAccel == accel) {
			mbBlockCommandUpdate = true;
			mListViewBoundCommands.SetSelectedIndex(index);
			mbBlockCommandUpdate = false;
			mListViewBoundCommands.EnsureItemVisible(index);
			break;
		}
	}
}

///////////////////////////////////////////////////////////////////////////

void VDDialogEditAccelerators::BoundCommand::GetText(int subItem, VDStringW& s) const {
	switch(subItem) {
		case 0:
			s = mpContextName;
			break;

		case 1:
			s = VDTextAToW(mpCommand);
			break;

		case 2:
			VDUIGetAcceleratorString(mAccel, s);
			break;
	}
}

///////////////////////////////////////////////////////////////////////////

bool ATUIShowDialogEditAccelerators(VDGUIHandle hParent,
	const VDAccelToCommandEntry *commands,
	uint32 commandCount,
	VDAccelTableDefinition *accelTables,
	const VDAccelTableDefinition *defaultTables,
	uint32 tableCount,
	const wchar_t *const *contextNames)
{
	VDDialogEditAccelerators dlg(commands, commandCount, accelTables, defaultTables, tableCount, contextNames);

	return dlg.ShowDialog(hParent) != 0;
}
