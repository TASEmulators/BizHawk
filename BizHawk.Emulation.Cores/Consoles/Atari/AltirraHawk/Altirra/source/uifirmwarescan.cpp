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
#include <vd2/system/thread.h>
#include <vd2/Dita/services.h>
#include <at/atcore/progress.h>
#include <at/atnativeui/dialog.h>
#include "firmwaremanager.h"
#include "firmwaredetect.h"

void ATUIScanForFirmware(VDGUIHandle hParent, ATFirmwareManager& fwmgr) {
	const VDStringW& path = VDGetDirectory('FMWR', hParent, L"Select folder to scan for firmware");

	if (path.empty())
		return;

	ATProgress progress;
	progress.InitF(100, NULL, L"Scanning for firmware");

	VDDirectoryIterator it(VDMakePath(path.c_str(), L"*.*").c_str());
	vdvector<VDStringW> pathsToScan;

	ATFirmwareInfo info;
	while(it.Next()) {
		progress.Update(0);

		if (it.GetAttributes() & (kVDFileAttr_System | kVDFileAttr_Hidden))
			continue;

		if (it.IsDirectory())
			continue;

		sint64 size = it.GetSize();

		if (!ATFirmwareAutodetectCheckSize(size))
			continue;

		pathsToScan.push_back(it.GetFullPath());
	}

	progress.Update(10);

	size_t n = pathsToScan.size();
	vdvector<ATFirmwareInfo> detectedFirmwares;

	for(size_t i=0; i<n; ++i) {
		progress.Update((uint32)(10 + (i*90)/n));
		const VDStringW& fullPath = pathsToScan[i];
		try {
			VDFile f(fullPath.c_str());
			sint64 size = f.size();

			if (!ATFirmwareAutodetectCheckSize(size))
				continue;

			uint32 size32 = (uint32)size;

			vdblock<char> buf(size32);
			f.read(buf.data(), (long)buf.size());

			ATSpecificFirmwareType specificType;
			if (ATFirmwareAutodetect(buf.data(), (uint32)buf.size(), info, specificType)) {
				ATFirmwareInfo& info2 = detectedFirmwares.push_back();

				vdmove(info2, info);
				info2.mId = ATGetFirmwareIdFromPath(fullPath.c_str());
				info2.mPath = fullPath;

				if (specificType != kATSpecificFirmwareType_None && !fwmgr.GetSpecificFirmware(specificType))
					fwmgr.SetSpecificFirmware(specificType, info2.mId);
			}
		} catch(const MyError&) {
		}
	}

	progress.Shutdown();

	size_t n2 = detectedFirmwares.size();
	size_t existing = 0;
	for(size_t i=0; i<n2; ++i) {
		const ATFirmwareInfo& info = detectedFirmwares[i];

		ATFirmwareInfo info2;
		if (fwmgr.GetFirmwareInfo(info.mId, info2)) {
			++existing;
			continue;
		}

		fwmgr.AddFirmware(info);
	}

	VDStringW message;
	message.sprintf(L"Firmware images recognized: %u (%u already present)", (uint32)n2, (uint32)existing);
	VDDialogFrameW32::ShowInfo(hParent, message.c_str(), L"Altirra Info");
}
