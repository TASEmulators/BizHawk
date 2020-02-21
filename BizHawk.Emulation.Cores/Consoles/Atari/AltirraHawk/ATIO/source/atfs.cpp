//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - VFS nested filesystem integration
//	Copyright (C) 2008-2018 Avery Lee
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

#include <stdafx.h>
#include <vd2/system/file.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/vfs.h>
#include <at/atio/atfs.h>
#include <at/atio/diskfs.h>
#include <at/atio/diskimage.h>
#include <at/atio/image.h>

class ATVFSAtfsFileView final : public ATVFSFileView {
public:
	ATVFSAtfsFileView(const wchar_t *fileName)
		: mMemoryStream(nullptr, 0)
	{
		mFileName = fileName;
		mpStream = &mMemoryStream;
	}

	vdfastvector<uint8> mBuffer;
	VDMemoryStream mMemoryStream;
};

void ATVFSOpenAtfsFileView(ATVFSFileView *srcView, const wchar_t *subPath, ATVFSFileView **newView) {
	// check that the subPath doesn't end in a separator and isn't empty
	VDStringSpanW spSpan(subPath);
	if (spSpan.empty() || spSpan.end()[-1] == L'/')
		throw MyError("Invalid subpath for inner filesystem: %ls", subPath);

	// attempt to mount the source as an image
	vdrefptr<IATImage> image;
	ATImageLoadContext ctx {};
	ctx.mLoadType = kATImageType_Disk;
	ATImageLoadAuto(srcView->GetFileName(), srcView->GetFileName(), srcView->GetStream(), &ctx, nullptr, nullptr, ~image);

	IATDiskImage *diskImage = vdpoly_cast<IATDiskImage *>(image);
	if (!diskImage)
		throw MyError("Unable to mount as disk image: %ls", srcView->GetFileName());

	// attempt to mount a recognized filesystem
	vdautoptr<IATDiskFS> fs(ATDiskMountImage(diskImage, true));

	if (!fs)
		throw MyError("Unrecognized filesystem in image: %ls", srcView->GetFileName());

	// traverse the filesystem using the subpath
	const wchar_t *s = subPath;
	ATDiskFSKey currentObject = ATDiskFSKey::None;
	VDStringA component;
	for(;;) {
		// find next separator
		const wchar_t *separator = wcschr(s, L'/');

		// skip empty components
		if (separator == s) {
			s = separator + 1;
			continue;
		}

		// convert component to ATASCII
		const wchar_t *t = separator ? separator : s + wcslen(s);
		const size_t componentLen = (size_t)(t - s);

		component.resize(componentLen);

		for(size_t i = 0; i < componentLen; ++i) {
			const wchar_t c = s[i];

			// 8-bit filesystems don't do Unicode.
			if (c < 0x20 || c >= 0x7F)
				throw MyError("Invalid nested filesystem path: %ls", subPath);

			component[i] = (char)c;
		}

		// look up in current directory
		currentObject = fs->LookupFile(currentObject, component.c_str());
		if (currentObject == ATDiskFSKey::None)
			throw MyError("File not found in inner filesystem: %ls", subPath);

		if (!separator) {
			// reached the end -- done with traversal
			break;
		}

		// we have more components -- keep going
		s = separator + 1;
	}

	// create a new view to hold the file
	vdrefptr<ATVFSAtfsFileView> view2(new ATVFSAtfsFileView(subPath));

	// read file into the view's buffer
	fs->ReadFile(currentObject, view2->mBuffer);

	// mount the view stream on the buffer
	view2->mMemoryStream = VDMemoryStream(view2->mBuffer.data(), (uint32)view2->mBuffer.size());
	
	// all done
	*newView = view2.release();
}

void ATVFSInstallAtfsHandler() {
	ATVFSSetAtfsProtocolHandler(ATVFSOpenAtfsFileView);
}
