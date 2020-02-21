//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include "verifier.h"
#include "resource.h"
#include "simulator.h"

class ATUIVerifierDialog : public VDDialogFrameW32 {
public:
	ATUIVerifierDialog(ATSimulator& sim);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATSimulator& mSim;
	VDUIProxyListView mOptionsView;
	VDDelegate mDelOnDoubleClick;

	static const struct FlagInfo {
		uint32 mFlag;
		const wchar_t *mpLabel;
	} kVerifierFlags[];
};

const ATUIVerifierDialog::FlagInfo ATUIVerifierDialog::kVerifierFlags[] = {
	{ kATVerifierFlag_UndocumentedKernelEntry, L"Undocumented OS entry" },
	{ kATVerifierFlag_RecursiveNMI, L"Recursive NMI execution" },
	{ kATVerifierFlag_InterruptRegs, L"Interrupt handler register corruption" },
	{ kATVerifierFlag_64KWrap, L"Address indexing across 64K boundary" },
	{ kATVerifierFlag_AbnormalDMA, L"Abnormal playfield DMA" },
	{ kATVerifierFlag_CallingConventionViolations, L"OS calling convention violations" },
	{ kATVerifierFlag_LoadingOverDisplayList, L"Loading over active display list" },
	{ kATVerifierFlag_AddressZero, L"Loading absolute address zero" },
};

ATUIVerifierDialog::ATUIVerifierDialog(ATSimulator& sim)
	: VDDialogFrameW32(IDD_VERIFIER)
	, mSim(sim)
{
	mOptionsView.SetOnItemDoubleClicked([this](int index) {
		mOptionsView.SetItemChecked(index, !mOptionsView.IsItemChecked(index));
	});
}

bool ATUIVerifierDialog::OnLoaded() {
	AddProxy(&mOptionsView, IDC_OPTIONS);
	mResizer.Add(IDC_OPTIONS, mResizer.kMC | mResizer.kAvoidFlicker);

	mOptionsView.SetFullRowSelectEnabled(true);
	mOptionsView.SetItemCheckboxesEnabled(true);
	mOptionsView.InsertColumn(0, L"", 0);

	for(const auto& entry : kVerifierFlags) {
		mOptionsView.InsertItem(-1, entry.mpLabel);
	}

	mOptionsView.AutoSizeColumns(true);

	OnDataExchange(false);
	SetFocusToControl(IDC_OPTIONS);
	return true;
}

void ATUIVerifierDialog::OnDataExchange(bool write) {
	if (write) {
		uint32 flags = 0;

		for(uint32 i=0; i<vdcountof(kVerifierFlags); ++i) {
			if (mOptionsView.IsItemChecked(i))
				flags |= kVerifierFlags[i].mFlag;
		}

		if (!flags)
			mSim.SetVerifierEnabled(false);
		else {
			mSim.SetVerifierEnabled(true);

			ATCPUVerifier *ver = mSim.GetVerifier();
			if (ver)
				ver->SetFlags(flags);
		}
	} else {
		ATCPUVerifier *ver = mSim.GetVerifier();

		if (ver) {
			uint32 flags = ver->GetFlags();

			for(uint32 i=0; i<vdcountof(kVerifierFlags); ++i)
				mOptionsView.SetItemChecked(i, (flags & kVerifierFlags[i].mFlag) != 0);
		}
	}
}

void ATUIShowDialogVerifier(VDGUIHandle h, ATSimulator& sim) {
	ATUIVerifierDialog dlg(sim);

	dlg.ShowDialog(h);
}
