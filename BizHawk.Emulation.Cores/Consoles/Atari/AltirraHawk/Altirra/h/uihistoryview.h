//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2017 Avery Lee
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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#ifndef f_AT_UIHISTORYVIEW_H
#define f_AT_UIHISTORYVIEW_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/refcount.h>
#include <at/atcpu/history.h>

class ATUINativeWindow;

class IATUIHistoryModel {
public:
	virtual double DecodeTapeSample(uint32 cycle) = 0;
	virtual double DecodeTapeSeconds(uint32 cycle) = 0;
	virtual uint32 ConvertRawTimestamp(uint32 rawCycle) = 0;
	virtual float ConvertRawTimestampDeltaF(sint32 rawCycleDelta) = 0;
	virtual ATCPUBeamPosition DecodeBeamPosition(uint32 cycle) = 0;
	virtual bool IsInterruptPositionVBI(uint32 cycle) = 0;
	virtual bool UpdatePreviewNode(ATCPUHistoryEntry& he) = 0;
	virtual uint32 ReadInsns(const ATCPUHistoryEntry **ppInsns, uint32 startIndex, uint32 n) = 0;
	virtual void OnEsc() = 0;
	virtual void OnInsnSelected(uint32 index) = 0;
	virtual void JumpToInsn(uint32 pc) = 0;
	virtual void JumpToSource(uint32 pc) = 0;
};

class IATUIHistoryView : public IVDRefCount {
public:
	virtual ATUINativeWindow *AsNativeWindow() = 0;
	virtual void SetHistoryModel(IATUIHistoryModel *model) = 0;
	virtual void SetDisasmMode(ATDebugDisasmMode disasmMode, uint32 subCycles, bool decodeAnticNMI) = 0;
	virtual void SetFonts(HFONT hfontProp, sint32 fontPropHeight, HFONT hfontMono, sint32 fontMonoHeight) = 0;

	virtual void SetTimestampOrigin(uint32 cycles, uint32 unhaltedCycles) = 0;

	virtual void SelectInsn(uint32 index) = 0;
	virtual void ClearInsns() = 0;
	virtual void UpdateInsns(uint32 historyStart, uint32 historyEnd) = 0;
	virtual void RefreshAll() = 0;
};

bool ATUICreateHistoryView(VDGUIHandle parent, IATUIHistoryView **ppview);

#endif
