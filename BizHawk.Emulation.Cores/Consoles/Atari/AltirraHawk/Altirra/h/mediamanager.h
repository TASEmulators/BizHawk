//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_MEDIAMANAGER_H
#define f_AT_MEDIAMANAGER_H

#include <vd2/system/refcount.h>
#include <vd2/system/VDString.h>
#include <at/atcore/media.h>
#include <at/atio/image.h>

struct ATMediaLoadContext {
	// Original path to media file; empty if N/A. If present, this is used for persisting
	// references to the source. If empty, no persistent location is available and the source
	// medium is ephemeral.
	VDStringW mOriginalPath;

	// Name of media file; may include path, empty if N/A. If present, this is used for
	// pulling metadata, particularly display name and extension for type guessing.
	VDStringW mImageName;

	// Optional stream. If present, used in lieu of opening the source path.
	IVDRandomAccessStream *mpStream = nullptr;

	// Cached image. Used in preference to stream if available. Certain recoverable failures
	// will return with this populated in place of the stream.s
	vdrefptr<IATImage> mpImage;

	ATMediaWriteMode mWriteMode = kATMediaWriteMode_RO;

	ATImageLoadContext *mpImageLoadContext = nullptr;

	bool mbStopAfterImageLoaded = false;

	bool mbStopOnModeIncompatibility = false;
	bool mbStopOnMemoryConflictBasic = false;
	bool mbModeIncompatible = false;
	bool mbMemoryConflictBasic = false;
	bool mbModeComputerRequired = false;
	bool mbMode5200Required = false;
};

#endif
