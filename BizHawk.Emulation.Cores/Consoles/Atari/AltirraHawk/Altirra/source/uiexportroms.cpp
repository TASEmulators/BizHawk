//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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
#include <vd2/Dita/services.h>
#include "firmwaremanager.h"
#include "oshelper.h"
#include "resource.h"
#include "uiaccessors.h"
#include "uicommondialogs.h"

void OnCommandToolsExportROMSet() {
	static const struct OutputInfo {
		ATFirmwareId mId;
		const wchar_t *mpFilename;
	} kOutputs[]={
		{ kATFirmwareId_Invalid, L"readme.html" },
		{ kATFirmwareId_Basic_ATBasic, L"atbasic.rom" },
		{ kATFirmwareId_Kernel_LLE, L"altirraos-800.rom" },
		{ kATFirmwareId_Kernel_LLEXL, L"altirraos-xl.rom" },
		{ kATFirmwareId_Kernel_816, L"altirraos-816.rom" },
		{ kATFirmwareId_5200_LLE, L"altirraos-5200.rom" },
	};

	const auto path = VDGetDirectory('erom', ATUIGetMainWindow(), L"Choose target folder for ROM export");
	if (path.empty())
		return;

	// check if any files already exist
	for (auto&& outputInfo : kOutputs) {
		if (VDDoesPathExist(VDMakePath(path, VDStringSpanW(outputInfo.mpFilename)).c_str())) {
			if (!ATUIShowWarningConfirm(ATUIGetMainWindow(), L"There are existing files with the same names that will be overwritten. Are you sure?"))
				return;
			break;
		}
	}

	// write out the files
	vdfastvector<uint8> buf;

	try {
		for (auto&& outputInfo : kOutputs) {
			if (outputInfo.mId == kATFirmwareId_Invalid)
				ATLoadMiscResource(IDR_ROMSETREADME, buf);
			else
				ATLoadInternalFirmware(outputInfo.mId, nullptr, 0, 0, nullptr, nullptr, &buf);

			VDFile f(VDMakePath(path, VDStringSpanW(outputInfo.mpFilename)).c_str(),
				nsVDFile::kWrite | nsVDFile::kCreateAlways | nsVDFile::kSequential);

			f.write(buf.data(), (long)buf.size());
		}

		ATUIShowInfo(ATUIGetMainWindow(), L"ROM set successfully exported.");
	} catch(const MyError& e) {
		ATUIShowError(ATUIGetMainWindow(), e);
	}
}
