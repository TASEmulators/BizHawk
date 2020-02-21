//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - filesystem handler defines
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_AT_ATIO_DISKFS_H
#define f_AT_ATIO_DISKFS_H

#include <vd2/system/date.h>
#include <vd2/system/error.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>

class IATDiskImage;
class IVDRandomAccessStream;

enum class ATDiskFSFindHandle : uintptr { Invalid = 0 };
enum class ATDiskFSKey : uint32 { None = 0 };

struct ATDiskFSInfo {
	VDStringA mFSType;
	uint32	mFreeBlocks;
	uint32	mBlockSize;
};

struct ATDiskFSEntryInfo {
	VDStringA mFileName;
	uint32	mSectors;
	uint32	mBytes;
	ATDiskFSKey	mKey;
	bool	mbIsDirectory;
	bool	mbDateValid;
	VDExpandedDate	mDate;
};

struct ATDiskFSValidationReport {
	bool mbBitmapIncorrect = false;
	bool mbBitmapIncorrectLostSectorsOnly = false;
	bool mbBrokenFiles = false;
	bool mbOpenWriteFiles = false;
	bool mbMetadataCorruption = false;

	bool IsSerious() const {
		return mbBitmapIncorrect || mbBrokenFiles || mbOpenWriteFiles || mbMetadataCorruption;
	}
};

enum ATDiskFSError {
	kATDiskFSError_InvalidFileName,
	kATDiskFSError_DiskFull,
	kATDiskFSError_DiskFullFragmented,
	kATDiskFSError_DirectoryFull,
	kATDiskFSError_CorruptedFileSystem,
	kATDiskFSError_FileExists,
	kATDiskFSError_ReadOnly,
	kATDiskFSError_FileTooLarge,
	kATDiskFSError_ReadError,
	kATDiskFSError_WriteError,
	kATDiskFSError_CannotReadSparseFile,
	kATDiskFSError_DirectoryNotEmpty,
	kATDiskFSError_UnsupportedCompressionMode,
	kATDiskFSError_DecompressionError,
	kATDiskFSError_CRCError,
	kATDiskFSError_NotSupported,
	kATDiskFSError_MediaNotSupported,
};

class ATDiskFSException : public MyError {
public:
	ATDiskFSException(ATDiskFSError error);

	ATDiskFSError GetErrorCode() const { return mErrorCode; }

protected:
	const ATDiskFSError mErrorCode;
};

class VDINTERFACE IATDiskFS {
public:
	virtual ~IATDiskFS() {}

	virtual void GetInfo(ATDiskFSInfo& info) = 0;

	virtual bool IsReadOnly() = 0;
	virtual void SetReadOnly(bool readOnly) = 0;
	virtual void SetAllowExtend(bool allow) = 0;
	virtual void SetStrictNameChecking(bool strict) = 0;

	virtual bool Validate(ATDiskFSValidationReport& report) = 0;
	virtual void Flush() = 0;

	// Begin iterating over directory entries in the given directory. Returns
	// a valid search handle if there is at least one entry, Invalid otherwise.
	virtual ATDiskFSFindHandle FindFirst(ATDiskFSKey key, ATDiskFSEntryInfo& info) = 0;

	// Retrieve the next directory entry. Returns true if another entry was found,
	// false if the end has been reached.
	virtual bool FindNext(ATDiskFSFindHandle searchKey, ATDiskFSEntryInfo& info) = 0;

	// Finish iterating over a directory.
	virtual void FindEnd(ATDiskFSFindHandle searchKey) = 0;

	virtual void GetFileInfo(ATDiskFSKey key, ATDiskFSEntryInfo& info) = 0;

	// Retrieve the key of the parent directory for a given entry. If the root is
	// passed, None is returned.
	virtual ATDiskFSKey GetParentDirectory(ATDiskFSKey dirKey) = 0;

	// Look for a file with a particular name within the given directory.
	virtual ATDiskFSKey LookupFile(ATDiskFSKey parentKey, const char *filename) = 0;

	// Delete a file or directory.
	virtual void DeleteFile(ATDiskFSKey key) = 0;

	// Read the contents of a file.
	virtual void ReadFile(ATDiskFSKey key, vdfastvector<uint8>& dst) = 0;

	// Write a file with the given contents. Fails if the file already exists.
	virtual ATDiskFSKey WriteFile(ATDiskFSKey parentKey, const char *filename, const void *src, uint32 len) = 0;

	// Rename a file to another name.
	virtual void RenameFile(ATDiskFSKey key, const char *newFileName) = 0;

	// Sets the timestamp on a file or directory. Silently ignored if timestamps are not
	// supported.
	virtual void SetFileTimestamp(ATDiskFSKey key, const VDExpandedDate& date) = 0;

	// Create a subdirectory. Throws if not supported.
	virtual ATDiskFSKey CreateDir(ATDiskFSKey parentKey, const char *filename) = 0;
};

IATDiskFS *ATDiskFormatImageDOS1(IATDiskImage *image);
IATDiskFS *ATDiskFormatImageDOS2(IATDiskImage *image);
IATDiskFS *ATDiskFormatImageDOS3(IATDiskImage *image);
IATDiskFS *ATDiskFormatImageMyDOS(IATDiskImage *image);
IATDiskFS *ATDiskFormatImageSDX2(IATDiskImage *image, const char *volNameHint = 0);

IATDiskFS *ATDiskMountImage(IATDiskImage *image, bool readOnly);
IATDiskFS *ATDiskMountImageARC(const wchar_t *path);
IATDiskFS *ATDiskMountImageARC(IVDRandomAccessStream& stream, const wchar_t *path);
IATDiskFS *ATDiskMountImageSDX2(IATDiskImage *image, bool readOnly);

#endif	// f_AT_DISKFS_H
