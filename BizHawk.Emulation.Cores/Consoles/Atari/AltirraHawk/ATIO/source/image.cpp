//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - image common definitions
//	Copyright (C) 2008-2016 Avery Lee
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
#include <vd2/system/strutil.h>
#include <vd2/system/zip.h>
#include <at/atcore/vfs.h>
#include <at/atio/image.h>
#include <at/atio/savestate.h>
#include <at/atio/cassetteimage.h>
#include <at/atio/cartridgeimage.h>
#include <at/atio/blobimage.h>
#include <at/atio/diskimage.h>

ATImageType ATGetImageTypeForFileExtension(const wchar_t *ext) {
	static const struct ExtEntry {
		const wchar_t *ext;
		ATImageType type;
	} kExtEntries[]={
		{ L".bin", kATImageType_Cartridge },
		{ L".rom", kATImageType_Cartridge },
		{ L".car", kATImageType_Cartridge },
		{ L".a52", kATImageType_Cartridge },
		{ L".xfd", kATImageType_Disk },
		{ L".atr", kATImageType_Disk },
		{ L".atx", kATImageType_Disk },
		{ L".atz", kATImageType_Disk },
		{ L".dcm", kATImageType_Disk },
		{ L".cas", kATImageType_Tape },
		{ L".wav", kATImageType_Tape },
		{ L".xex", kATImageType_Program },
		{ L".exe", kATImageType_Program },
		{ L".obx", kATImageType_Program },
		{ L".com", kATImageType_Program },
		{ L".bas", kATImageType_BasicProgram },
		{ L".sap", kATImageType_SAP },
	};

	for(const auto& entry : kExtEntries) {
		if (!vdwcsicmp(ext, entry.ext))
			return entry.type;
	}

	return kATImageType_None;
}

ATImageType ATDetectImageType(const wchar_t *imagePath, IVDRandomAccessStream& stream) {
	const wchar_t *ext = imagePath ? VDFileSplitExt(imagePath) : L"";

	// Try to detect by filename.
	if (!vdwcsicmp(ext, L".zip"))
		return kATImageType_Zip;

	if (!vdwcsicmp(ext, L".gz") || !vdwcsicmp(ext, L".atz"))
		return kATImageType_GZip;

	if (!vdwcsicmp(ext, L".xfd") || !vdwcsicmp(ext, L".arc"))
		return kATImageType_Disk;

	if (!vdwcsicmp(ext, L".bin") || !vdwcsicmp(ext, L".rom") || !vdwcsicmp(ext, L".a52"))
		return kATImageType_Cartridge;

	// If we still haven't determined the load type, try to autodetect by content.
	uint8 header[16] = {};
	long actual = stream.ReadData(header, 16);
	stream.Seek(0);

	if (actual < 6)
		return kATImageType_None;

	auto size = stream.Length();

	// Detect archive types.
	if (header[0] == 0x1f && header[1] == 0x8b)
		return kATImageType_GZip;

	else if (header[0] == 'P' && header[1] == 'K' && header[2] == 0x03 && header[3] == 0x04)
		return kATImageType_Zip;

	if (header[0] == 0xFF && header[1] == 0xFF)
		return kATImageType_Program;

	if ((header[0] == 'A' && header[1] == 'T' && header[2] == '8' && header[3] == 'X') ||
		(header[2] == 'P' && header[3] == '3') ||
		(header[2] == 'P' && header[3] == '2') ||
		(header[0] == 0x96 && header[1] == 0x02) ||
		(!(size & 127) && size <= 65535*128 && !_wcsicmp(ext, L".xfd")))
	{
		return kATImageType_Disk;
	}
	
	if (actual >= 12
		&& header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
		&& header[8] == 'W' && header[9] == 'A' && header[10] == 'V' && header[11] == 'E'
		)
	{
		return kATImageType_Tape;
	}
	
	if (header[0] == 'F' && header[1] == 'U' && header[2] == 'J' && header[3] == 'I')
		return kATImageType_Tape;

	if (header[0] == 'C' && header[1] == 'A' && header[2] == 'R' && header[3] == 'T')
		return kATImageType_Cartridge;

	if (!memcmp(header, kATSaveStateHeader, sizeof kATSaveStateHeader))
		return kATImageType_SaveState;

	if (!memcmp(header, "SAP\r\n", 5) && (!vdwcsicmp(ext, L".sap") || !memcmp(header + 5, "AUTHOR ", 7)))
		return kATImageType_SAP;

	if (!vdwcsicmp(ext, L".xex") || !vdwcsicmp(ext, L".obx") || !vdwcsicmp(ext, L".exe") || !vdwcsicmp(ext, L".com"))
		return kATImageType_Program;

	if (!vdwcsicmp(ext, L".bas"))
		return kATImageType_BasicProgram;

	if (!vdwcsicmp(ext, L".atr") || !vdwcsicmp(ext, L".dcm"))
		return kATImageType_Disk;

	if (!vdwcsicmp(ext, L".cas") || !vdwcsicmp(ext, L".wav"))
		return kATImageType_Tape;

	if (!vdwcsicmp(ext, L".rom") || !vdwcsicmp(ext, L".car") || !vdwcsicmp(ext, L".a52"))
		return kATImageType_Cartridge;

	return kATImageType_None;
}

bool ATImageLoadAuto(const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream, ATImageLoadContext *loadCtx, VDStringW *resultPath, bool *canUpdate, IATImage **ppImage) {
	sint64 size = 0;

	const wchar_t *ext = imagePath ? VDFileSplitExt(imagePath) : L"";

	ATImageType loadType = kATImageType_None;

	if (loadCtx)
		loadType = loadCtx->mLoadType;

	// Try to detect load type by content and image name.
	ATImageType intermediateLoadType = ATDetectImageType(imagePath, stream);

	// Don't change the load type if we have an archive in the way.
	switch(intermediateLoadType) {
		case kATImageType_Zip:
		case kATImageType_GZip:
			break;

		default:
			if (loadType == kATImageType_None)
				loadType = intermediateLoadType;
			break;
	}

	// Handle archive types first. Note that we do NOT write zip/gzip back into the load
	// type in the load context, in case there is an override for the inner resource that
	// we need to preserve.
	if (intermediateLoadType == kATImageType_GZip) {
		// This is really big, so don't put it on the stack.
		vdautoptr<VDGUnzipStream> gzs(new VDGUnzipStream(&stream, stream.Length()));

		vdfastvector<uint8> buffer;

		uint32 size = 0;
		for(;;) {
			// Don't gunzip beyond 64MB.
			if (size >= 0x4000000)
				throw MyError("Gzip stream is too large (exceeds 64MB in size).");

			buffer.resize(size + 1024);

			sint32 actual = gzs->ReadData(buffer.data() + size, 1024);
			if (actual <= 0) {
				buffer.resize(size);
				break;
			}

			size += actual;
		}

		VDMemoryStream ms(buffer.data(), (uint32)buffer.size());

		// Okay, now we have to figure out the filename. If there was one in the gzip
		// stream use that. If the name ended in .gz, then strip that off and use the
		// base name. If it was .atz, replace it with .atr. Otherwise, just replace it
		// with .dat and hope for the best.
		VDStringW newname;
		const wchar_t *newPath = NULL;

		const char *fn = gzs->GetFilename();
		if (fn && *fn) {
			newname = VDTextAToW(fn);
			newPath = newname.c_str();
		} else if (imagePath) {
			newname.assign(imagePath, ext);

			if (!vdwcsicmp(ext, L".atz"))
				newname += L".atr";
			else if (vdwcsicmp(ext, L".gz") != 0)
				newname += L".dat";

			newPath = newname.c_str();
		}

		if (loadCtx)
			loadCtx->mLoadType = kATImageType_None;
		
		VDStringW vfsPath = ATMakeVFSPathForGZipFile(origPath);

		if (canUpdate)
			*canUpdate = false;

		return ATImageLoadAuto(vfsPath.c_str(), newPath, ms, loadCtx, resultPath, false, ppImage);
	} else if (intermediateLoadType == kATImageType_Zip) {
		VDZipArchive ziparch;

		ziparch.Init(&stream);

		sint32 n = ziparch.GetFileCount();

		VDStringW extBuf;

		for(sint32 i=0; i<n; ++i) {
			const VDZipArchive::FileInfo& info = ziparch.GetFileInfo(i);
			const VDStringA& name = info.mFileName;
			const char *ext = VDFileSplitExt(name.c_str());

			// Just translate A->W directly by code point. If it has loc chars, it isn't
			// going to match anyway.
			extBuf.clear();

			for(const char *s = ext; *s; ++s) {
				extBuf += (wchar_t)(unsigned char)*s;
			}

			auto detectedType = ATGetImageTypeForFileExtension(extBuf.c_str());
			if (detectedType != kATImageType_None && (!loadType || loadType == detectedType)) {
				IVDStream& innerStream = *ziparch.OpenRawStream(i);
				vdfastvector<uint8> data;

				vdautoptr<VDZipStream> zs(new VDZipStream(&innerStream, info.mCompressedSize, !info.mbPacked));

				data.resize(info.mUncompressedSize);
				zs->Read(data.data(), info.mUncompressedSize);

				VDMemoryStream ms(data.data(), (uint32)data.size());

				VDStringW vfsPath;
				
				if (origPath)
					vfsPath = ATMakeVFSPathForZipFile(origPath, VDTextU8ToW(name).c_str());

				if (canUpdate)
					*canUpdate = false;

				return ATImageLoadAuto(origPath ? vfsPath.c_str() : nullptr, VDTextU8ToW(name).c_str(), ms, loadCtx, resultPath, nullptr, ppImage);
			}
		}

		if (origPath)
			throw MyError("The zip file \"%ls\" does not contain a recognized file type.", origPath);
		else
			throw MyError("The zip file does not contain a recognized file type.");
	}

	// Stash off the detected type (or None if we failed).
	if (loadCtx)
		loadCtx->mLoadType = loadType;

	// Load the resource.
	vdrefptr<IATImage> image;
	if (loadType == kATImageType_Program) {
		vdrefptr<IATBlobImage> programImage;
		ATLoadBlobImage(kATImageType_Program, stream, ~programImage);

		*ppImage = programImage.release();
	} else if (loadType == kATImageType_BasicProgram) {
		vdrefptr<IATBlobImage> basicProgramImage;
		ATLoadBlobImage(kATImageType_BasicProgram, stream, ~basicProgramImage);

		*ppImage = basicProgramImage.release();
	} else if (loadType == kATImageType_Cartridge) {
		vdrefptr<IATCartridgeImage> cartImage;
		if (!ATLoadCartridgeImage(origPath, stream, loadCtx ? loadCtx->mpCartLoadContext : nullptr, ~cartImage))
			return false;

		*ppImage = cartImage.release();
	} else if (loadType == kATImageType_Tape) {
		vdrefptr<IATCassetteImage> tapeImage;
		ATLoadCassetteImage(stream, nullptr, ~tapeImage);

		*ppImage = tapeImage.release();
	} else if (loadType == kATImageType_Disk) {
		vdrefptr<IATDiskImage> diskImage;
		ATLoadDiskImage(origPath, imagePath, stream, ~diskImage);

		*ppImage = diskImage.release();
	} else if (loadType == kATImageType_SaveState) {
		vdrefptr<IATBlobImage> saveStateImage;
		ATLoadBlobImage(kATImageType_SaveState, stream, ~saveStateImage);

		*ppImage = saveStateImage.release();
	} else if (loadType == kATImageType_SAP) {
		vdrefptr<IATBlobImage> sapImage;
		ATLoadBlobImage(kATImageType_SAP, stream, ~sapImage);

		*ppImage = sapImage.release();
	} else {
		if (origPath)
			throw MyError("Unable to identify type of file: %ls.", origPath);
		else
			throw MyError("Unable to identify type of file.");
	}

	if (resultPath) {
		if (origPath)
			*resultPath = origPath;
		else
			resultPath->clear();
	}

	if (canUpdate)
		*canUpdate = true;

	return true;
}
