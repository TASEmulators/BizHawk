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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/function.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicesio.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include "pclink.h"
#include "console.h"
#include "cio.h"
#include "cpu.h"
#include "kerneldb.h"
#include "uirender.h"
#include "debuggerlog.h"

ATDebuggerLogChannel g_ATLCPCLink(false, false, "PCLINK", "PCLink activity");

uint8 ATTranslateWin32ErrorToSIOError(uint32 err);

namespace {
	// 2011 and we still have to put up with this crap
	static const char *const kReservedDeviceNames[]={
		"CON",
		"PRN",
		"AUX",
		"NUL",
		"COM1",
		"COM2",
		"COM3",
		"COM4",
		"COM5",
		"COM6",
		"COM7",
		"COM8",
		"COM9",
		"LPT1",
		"LPT2",
		"LPT3",
		"LPT4",
		"LPT5",
		"LPT6",
		"LPT7",
		"LPT8",
		"LPT9",
		NULL
	};
}

///////////////////////////////////////////////////////////////////////////

struct ATPCLinkDiskInfo {
	uint8	mInfoVersion;
	uint8	mRootDirLo;
	uint8	mRootDirHi;
	uint8	mSectorCountLo;
	uint8	mSectorCountHi;
	uint8	mSectorsFreeLo;
	uint8	mSectorsFreeHi;
	uint8	mVTOCSectorCount;
	uint8	mVTOCSectorStartLo;
	uint8	mVTOCSectorStartHi;
	uint8	mNextFileSectorLo;
	uint8	mNextFileSectorHi;
	uint8	mNextDirSectorLo;
	uint8	mNextDirSectorHi;
	uint8	mVolumeLabel[8];
	uint8	mTrackCount;
	uint8	mBytesPerSectorCode;
	uint8	mVersion;
	uint8	mBytesPerSectorLo;
	uint8	mBytesPerSectorHi;
	uint8	mMapSectorCountLo;
	uint8	mMapSectorCountHi;
	uint8	mSectorsPerCluster;
	uint8	mNoSeq;
	uint8	mNoRnd;
	uint8	mBootLo;
	uint8	mBootHi;
	uint8	mWriteProtectFlag;
	uint8	mPad[29];
};

struct ATPCLinkDirEnt {
	enum : uint8 {
		kFlag_OpenForWrite	= 0x80,
		kFlag_Directory		= 0x20,
		kFlag_Deleted		= 0x10,
		kFlag_InUse			= 0x08,
		kFlag_Archive		= 0x04,
		kFlag_Hidden		= 0x02,
		kFlag_Locked		= 0x01
	};

	enum : uint8 {
		kAttrMask_NoSubDir		= 0x80,
		kAttrMask_NoArchived	= 0x40,
		kAttrMask_NoHidden		= 0x20,
		kAttrMask_NoLocked		= 0x10,
		kAttrMask_OnlySubDir	= 0x08,
		kAttrMask_OnlyArchived	= 0x04,
		kAttrMask_OnlyHidden	= 0x02,
		kAttrMask_OnlyLocked	= 0x01,
	};

	uint8	mFlags;
	uint8	mSectorMapLo;
	uint8	mSectorMapHi;
	uint8	mLengthLo;
	uint8	mLengthMid;
	uint8	mLengthHi;
	uint8	mName[11];
	uint8	mDay;
	uint8	mMonth;
	uint8	mYear;
	uint8	mHour;
	uint8	mMin;
	uint8	mSec;

	void SetFlagsFromAttributes(uint32 attr);
	bool TestAttrFilter(uint8 attrFilter) const;
	void SetDate(const VDDate& date);

	static VDDate DecodeDate(const uint8 tsdata[6]);
};

void ATPCLinkDirEnt::SetFlagsFromAttributes(uint32 attr) {
	mFlags = ATPCLinkDirEnt::kFlag_InUse;
	if (attr & kVDFileAttr_Hidden)
		mFlags |= ATPCLinkDirEnt::kFlag_Hidden;

	if (attr & kVDFileAttr_Archive)
		mFlags |= ATPCLinkDirEnt::kFlag_Archive;

	if (attr & kVDFileAttr_ReadOnly)
		mFlags |= ATPCLinkDirEnt::kFlag_Locked;

	if (attr & kVDFileAttr_Directory)
		mFlags |= ATPCLinkDirEnt::kFlag_Directory;

}

bool ATPCLinkDirEnt::TestAttrFilter(uint8 attrFilter) const {
	if (!attrFilter)
		return true;

	if (attrFilter & kAttrMask_NoArchived) {
		if (mFlags & ATPCLinkDirEnt::kFlag_Archive)
			return false;
	}

	if (attrFilter & kAttrMask_NoHidden) {
		if (mFlags & ATPCLinkDirEnt::kFlag_Hidden)
			return false;
	}

	if (attrFilter & kAttrMask_NoLocked) {
		if (mFlags & ATPCLinkDirEnt::kFlag_Locked)
			return false;
	}

	if (attrFilter & kAttrMask_NoSubDir) {
		if (mFlags & ATPCLinkDirEnt::kFlag_Directory)
			return false;
	}

	if (attrFilter & kAttrMask_OnlyArchived) {
		if (!(mFlags & ATPCLinkDirEnt::kFlag_Archive))
			return false;
	}

	if (attrFilter & kAttrMask_OnlyHidden) {
		if (!(mFlags & ATPCLinkDirEnt::kFlag_Hidden))
			return false;
	}

	if (attrFilter & kAttrMask_OnlyLocked) {
		if (!(mFlags & ATPCLinkDirEnt::kFlag_Locked))
			return false;
	}

	if (attrFilter & kAttrMask_OnlySubDir) {
		if (!(mFlags & ATPCLinkDirEnt::kFlag_Directory))
			return false;
	}

	return true;
}

void ATPCLinkDirEnt::SetDate(const VDDate& date) {
	VDExpandedDate date2(VDGetLocalDate(date));
	mDay = date2.mDay;
	mMonth = date2.mMonth;
	mYear = date2.mYear % 100;
	mHour = date2.mHour;
	mMin = date2.mMinute;
	mSec = date2.mSecond;
}

VDDate ATPCLinkDirEnt::DecodeDate(const uint8 tsdata[6]) {
	VDExpandedDate xdate {};

	xdate.mDay = tsdata[0];
	xdate.mMonth = tsdata[1];
	xdate.mYear = tsdata[2] < 50 ? 2000 + tsdata[2] : 1900 + tsdata[2];
	xdate.mHour = tsdata[3];
	xdate.mMinute = tsdata[4];
	xdate.mSecond = tsdata[5];
	xdate.mMilliseconds = 0;

	return VDDateFromLocalDate(xdate);
}

struct ATPCLinkDirEntSort {
	bool operator()(const ATPCLinkDirEnt& x, const ATPCLinkDirEnt& y) {
		// Note that we are sorting in reverse order here because we also
		// pop them off in reverse order.
		if ((x.mFlags ^ y.mFlags) & ATPCLinkDirEnt::kFlag_Directory)
			return (x.mFlags & ATPCLinkDirEnt::kFlag_Directory) == 0;

		return memcmp(x.mName, y.mName, 11) > 0;
	}
};

struct ATPCLinkFileName {
	uint8	mName[11];

	bool operator==(const ATPCLinkFileName& x) const {
		return memcmp(mName, x.mName, 11) == 0;
	}

	bool ParseFromNet(const uint8 fn[11]);
	bool ParseFromNative(const wchar_t *fn);
	void AppendNative(VDStringA& s) const;
	void AppendNative(VDStringW& s) const;

	bool IsWild() const;
	bool IsReservedDeviceName() const;
	bool WildMatch(const ATPCLinkFileName& fn) const;
	void WildMerge(const ATPCLinkFileName& fn);
};

bool ATPCLinkFileName::ParseFromNet(const uint8 fn[11]) {
	uint8 fill = 0;

	for(int i=0; i<11; ++i) {
		if (i == 8)
			fill = 0;

		if (fill)
			mName[i] = fill;
		else {
			uint8 c = fn[i];

			if (c == '*') {
				c = '?';
				fill = c;
			}

			if (c == ' ')
				fill = c;
			else if (c >= L'a' && c <= L'z')
				c &= ~0x20;
			else if ((c < L'A' || c > L'Z') && (c < L'0' || c > L'9') && c != L'_' && c != '?')
				return false;

			mName[i] = c;
		}
	}

	return true;
}

bool ATPCLinkFileName::ParseFromNative(const wchar_t *fn) {
	int i = 0;
	bool inext = false;
	bool isescaped = false;

	if (*fn == '!' || *fn == '$') {
		++fn;
		isescaped = true;
	}

	while(wchar_t c = *fn++) {
		if (c == L'.') {
			if (i > 8)
				return false;

			while(i < 8)
				mName[i++] = L' ';

			inext = true;
			continue;
		}

		if (inext) {
			if (i >= 11)
				return false;
		} else {
			if (i >= 8)
				return false;
		}

		if (c >= L'a' && c <= L'z')
			mName[i++] = (uint8)(c - 0x20);
		else if ((c >= L'A' && c <= L'Z') || (c >= L'0' && c <= L'9') || c == L'_')
			mName[i++] = (uint8)c;
		else
			return false;
	}

	while(i < 11)
		mName[i++] = L' ';

	if (isescaped != IsReservedDeviceName())
		return false;

	return true;
}

void ATPCLinkFileName::AppendNative(VDStringA& s) const {
	if (IsReservedDeviceName())
		s += '!';

	for(int i=0; i<11; ++i) {
		const char c = (char)mName[i];

		if (c == ' ') {
			if (i == 8)
				break;
			continue;
		}

		if (i == 8)
			s += '.';

		s += c;
	}
}

void ATPCLinkFileName::AppendNative(VDStringW& s) const {
	if (IsReservedDeviceName())
		s += L'!';

	for(int i=0; i<11; ++i) {
		const wchar_t c = (wchar_t)mName[i];

		if (c == ' ') {
			if (i == 8)
				break;
			continue;
		}

		if (i == 8)
			s += L'.';

		s += c;
	}
}

bool ATPCLinkFileName::IsWild() const {
	for(int i=0; i<11; ++i) {
		if (mName[i] == '?')
			return true;
	}

	return false;
}

bool ATPCLinkFileName::IsReservedDeviceName() const {
	const char *const *p = kReservedDeviceNames;

	while(const char *s = *p++) {
		const uint8 *t = mName;

		while(const char c = *s++) {
			if (*t++ != c)
				goto fail;
		}

		// no reserved name is >7 chars
		if (*t == ' ')
			return true;

fail:
		;
	}

	return false;
}

bool ATPCLinkFileName::WildMatch(const ATPCLinkFileName& fn) const {
	for(int i=0; i<11; ++i) {
		const uint8 c = mName[i];
		const uint8 d = fn.mName[i];

		if (c != d && c != '?')
			return false;
	}

	return true;
}

void ATPCLinkFileName::WildMerge(const ATPCLinkFileName& fn) {
	for(int i=0; i<11; ++i) {
		const uint8 d = fn.mName[i];

		if (d != '?')
			mName[i] = d;

		if (mName[i] == ' ') {
			if (i < 8) {
				while(i < 7)
					mName[++i] = ' ';
			} else {
				while(i < 10)
					mName[++i] = ' ';
			}
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATPCLinkFileHandle {
	ATPCLinkFileHandle(const ATPCLinkFileHandle&) = delete;
	ATPCLinkFileHandle& operator=(const ATPCLinkFileHandle&) = delete;

public:
	ATPCLinkFileHandle() = default;
	~ATPCLinkFileHandle();

	bool IsOpen() const;
	bool IsDir() const;
	bool IsReadable() const;
	bool IsWritable() const;
	bool WasCreated() const { return mbWasCreated; }

	uint32 GetLength() const;
	uint32 GetPosition() const;
	const ATPCLinkDirEnt& GetDirEnt() const;
	void SetDirEnt(const ATPCLinkDirEnt& dirEnt);

	void AddDirEnt(const ATPCLinkDirEnt& dirEnt);
	uint8 OpenFile(const wchar_t *nativePath, uint32 openFlags, bool allowRead, bool allowWrite, bool append);
	void OpenAsDirectory(const ATPCLinkFileName& dirName, const ATPCLinkFileName& fnextFilter, uint8 attrFilter);
	void Close();
	uint8 Seek(uint32 pos);
	uint8 Read(void *dst, uint32 len, uint32& actual);
	uint8 Write(const void *dst, uint32 len);

	void SetTimestamp(const uint8 tsdata[6]);
 
	bool GetNextDirEnt(ATPCLinkDirEnt& dirEnt);

protected:
	bool	mbOpen = false;
	bool	mbAllowRead = false;
	bool	mbAllowWrite = false;
	bool	mbIsDirectory = false;
	bool	mbWasCreated = false;
	uint32	mPos = 0;
	uint32	mLength = 0;

	vdfastvector<ATPCLinkDirEnt> mDirEnts;

	ATPCLinkDirEnt	mDirEnt = {};
	ATPCLinkFileName mFnextPattern = {};
	uint8	mFnextAttrFilter = 0;

	VDFile	mFile;
};

ATPCLinkFileHandle::~ATPCLinkFileHandle() {
}

bool ATPCLinkFileHandle::IsOpen() const {
	return mbOpen;
}

bool ATPCLinkFileHandle::IsReadable() const {
	return mbAllowRead;
}

bool ATPCLinkFileHandle::IsWritable() const {
	return mbAllowWrite;
}

bool ATPCLinkFileHandle::IsDir() const {
	return mbIsDirectory;
}

uint32 ATPCLinkFileHandle::GetPosition() const {
	return mPos;
}

uint32 ATPCLinkFileHandle::GetLength() const {
	return mLength;
}

const ATPCLinkDirEnt& ATPCLinkFileHandle::GetDirEnt() const {
	return mDirEnt;
}

void ATPCLinkFileHandle::SetDirEnt(const ATPCLinkDirEnt& dirEnt) {
	mDirEnt = dirEnt;
}

void ATPCLinkFileHandle::AddDirEnt(const ATPCLinkDirEnt& dirEnt) {
	mDirEnts.push_back(dirEnt);
}

uint8 ATPCLinkFileHandle::OpenFile(const wchar_t *nativePath, uint32 openFlags, bool allowRead, bool allowWrite, bool append) {
	try {
		const uint32 creationMode = openFlags & nsVDFile::kCreationMask;

		mbWasCreated = false;

		switch(creationMode) {
			case nsVDFile::kOpenExisting:
			case nsVDFile::kOpenAlways:
			case nsVDFile::kTruncateExisting:
				break;

			case nsVDFile::kCreateAlways:
			case nsVDFile::kCreateNew:
				mbWasCreated = true;
				break;
		}

		if (creationMode == nsVDFile::kOpenAlways) {
			if (!mFile.openAlways(nativePath, openFlags))
				mbWasCreated = true;
		} else {
			mFile.open(nativePath, openFlags);
		}
	} catch(const MyWin32Error& e) {
		return ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
	} catch(const MyError&) {
		return ATCIOSymbols::CIOStatSystemError;
	}

	mbOpen = true;
	mbIsDirectory = false;
	mbAllowRead = allowRead;
	mbAllowWrite = allowWrite;

	sint64 len = mFile.size();
	if (len > 0xffffff)
		mLength = 0xffffff;
	else
		mLength = (uint32)len;

	mPos = 0;

	if (append)
		mFile.seekNT(mLength);

	return ATCIOSymbols::CIOStatSuccess;
}

void ATPCLinkFileHandle::OpenAsDirectory(const ATPCLinkFileName& dirName, const ATPCLinkFileName& pattern, uint8 attrFilter) {
	std::sort(mDirEnts.begin(), mDirEnts.end(), ATPCLinkDirEntSort());
	mbOpen = true;
	mbIsDirectory = true;
	mLength = 23 * ((uint32)mDirEnts.size() + 1);
	mPos = 23;
	mbAllowRead = true;
	mbAllowWrite = false;

	memset(&mDirEnt, 0, sizeof mDirEnt);
	mDirEnt.mFlags = ATPCLinkDirEnt::kFlag_InUse | ATPCLinkDirEnt::kFlag_Directory;
	mDirEnt.mLengthLo = (uint8)mLength;
	mDirEnt.mLengthMid = (uint8)(mLength >> 8);
	mDirEnt.mLengthHi = (uint8)(mLength >> 16);
	memcpy(mDirEnt.mName, dirName.mName, 11);

	mDirEnts.insert(mDirEnts.begin(), mDirEnt);

	mFnextPattern = pattern;
	mFnextAttrFilter = attrFilter;
}

void ATPCLinkFileHandle::Close() {
	mFile.closeNT();
	mbOpen = false;
	mbAllowRead = false;
	mbAllowWrite = false;
	vdfastvector<ATPCLinkDirEnt> tmp;
	mDirEnts.swap(tmp);
}

uint8 ATPCLinkFileHandle::Seek(uint32 pos) {
	if (pos > mLength && !mbAllowRead)
		return ATCIOSymbols::CIOStatPointDLen;

	if (!mbIsDirectory) {
		try {
			mFile.seek(pos);
		} catch(const MyWin32Error& e) {
			mFile.seekNT(mPos);
			return ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
		} catch(const MyError&) {
			mFile.seekNT(mPos);
			return ATCIOSymbols::CIOStatSystemError;
		}
	}

	mPos = pos;
	return ATCIOSymbols::CIOStatSuccess;
}

uint8 ATPCLinkFileHandle::Read(void *dst, uint32 len, uint32& actual) {
	actual = 0;

	if (!mbOpen)
		return ATCIOSymbols::CIOStatNotOpen;

	if (!mbAllowRead)
		return ATCIOSymbols::CIOStatWriteOnly;

	long act = 0;

	if (mPos < mLength) {
		if (mbIsDirectory) {
			uint32 dirIndex = mPos / 23;
			uint32 offset = mPos % 23;
			uint32 left = mLength - mPos;

			if (left > len)
				left = len;
		
			while(left > 0) {
				const uint8 *src = (const uint8 *)&mDirEnts[dirIndex];
				uint32 tc = 23 - offset;
				if (tc > left)
					tc = left;

				memcpy((char *)dst + act, src + offset, tc);
				left -= tc;
				act += tc;
				offset = 0;
				++dirIndex;
			}
		} else {
			uint32 tc = len;

			if (mLength - mPos < tc)
				tc = mLength - mPos;

			act = mFile.readData(dst, tc);
			if (act < 0)
				return ATCIOSymbols::CIOStatFatalDiskIO;
		}
	}

	actual = (uint32)act;

	g_ATLCPCLink("Read at pos %d/%d, len %d, actual %d\n", mPos, mLength, len, actual);

	mPos += actual;

	if (actual < len) {
		memset((char *)dst + actual, 0, len - actual);
		return ATCIOSymbols::CIOStatTruncRecord;
	}

	return ATCIOSymbols::CIOStatSuccess;
}

uint8 ATPCLinkFileHandle::Write(const void *dst, uint32 len) {
	if (!mbOpen)
		return ATCIOSymbols::CIOStatNotOpen;

	if (!mbAllowWrite)
		return ATCIOSymbols::CIOStatReadOnly;

	uint32 tc = len;
	uint32 actual = 0;

	if (mPos < 0xffffff) {
		if (0xffffff - mPos < tc)
			tc = 0xffffff - mPos;

		actual = mFile.writeData(dst, tc);
		if (actual != tc) {
			mFile.seekNT(mPos);
			return ATCIOSymbols::CIOStatFatalDiskIO;
		}
	}

	g_ATLCPCLink("Write at pos %d/%d, len %d, actual %d\n", mPos, mLength, len, actual);

	mPos += actual;
	if (mPos > mLength)
		mLength = mPos;

	return actual != len ? ATCIOSymbols::CIOStatDiskFull : ATCIOSymbols::CIOStatSuccess;
}

void ATPCLinkFileHandle::SetTimestamp(const uint8 tsdata[6]) {
	VDDate date = ATPCLinkDirEnt::DecodeDate(tsdata);

	if (date != VDDate {})
		mFile.setCreationTime(date);
}

bool ATPCLinkFileHandle::GetNextDirEnt(ATPCLinkDirEnt& dirEnt) {
	uint32 actual;

	while(Read(&dirEnt, 23, actual) == ATCIOSymbols::CIOStatSuccess && actual >= 23) {
		ATPCLinkFileName name;
		name.ParseFromNet(dirEnt.mName);

		if (mFnextPattern.WildMatch(name) && dirEnt.TestAttrFilter(mFnextAttrFilter))
			return true;
	}

	return false;
}

///////////////////////////////////////////////////////////////////////////

class ATPCLinkDevice final : public IATPCLinkDevice
			, public ATDevice
			, public IATDeviceSIO
			, public IATDeviceIndicators
			, public IATDeviceDiagnostics
{
	ATPCLinkDevice(const ATPCLinkDevice&) = delete;
	ATPCLinkDevice& operator=(const ATPCLinkDevice&) = delete;
public:
	ATPCLinkDevice();
	~ATPCLinkDevice();

	void *AsInterface(uint32 id) override;

	bool IsReadOnly() { return mbReadOnly; }
	void SetReadOnly(bool readOnly);

	const wchar_t *GetBasePath() { return mBasePathNative.c_str(); }
	void SetBasePath(const wchar_t *basePath);

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Shutdown() override;
	void ColdReset() override;

public:
	void InitIndicators(IATDeviceIndicatorManager *uir) override;

public:
	void InitSIO(IATDeviceSIOManager *mgr) override;
	CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	void OnSerialAbortCommand() override;
	void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) override;
	void OnSerialFence(uint32 id) override;
	CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

public:
	void DumpStatus(ATConsoleOutput& output) override;

protected:
	enum Command {
		kCommandNone,
		kCommandGetHiSpeedIndex,
		kCommandStatus,
		kCommandPut,
		kCommandRead
	};

	void AbortCommand();
	void BeginCommand(Command cmd);
	void AdvanceCommand();
	void FinishCommand();

	bool OnPut();
	bool OnRead();

	bool CheckValidFileHandle(bool setError);
	bool IsDirEntIncluded(const ATPCLinkDirEnt& dirEnt) const;
	bool ResolvePath(bool allowDir, VDStringA& resultPath);
	bool ResolveNativePath(bool allowDir, VDStringW& resultPath);
	bool ResolveNativePath(VDStringW& resultPath, const VDStringA& netPath);
	void OnReadActivity();
	void OnWriteActivity();

	IATDeviceSIOManager *mpSIOMgr = nullptr;
	IATDeviceIndicatorManager *mpUIRenderer = nullptr;

	VDStringW	mBasePathNative;
	bool	mbReadOnly = false;
	bool	mbSetTimestamps = false;

	vdfunction<void(const void *, uint32)> mpReceiveFn;
	vdfunction<void()> mpFenceFn;

	uint8	mStatusFlags = 0;
	uint8	mStatusError = 0;
	uint8	mStatusLengthLo = 0;
	uint8	mStatusLengthHi = 0;

	Command	mCommand = kCommandNone;
	uint32	mCommandPhase = 0;
	uint8	mCommandAux1 = 0;
	uint8	mCommandAux2 = 0;

	VDStringA	mCurDir;

	struct ParameterBuffer {
		uint8	mFunction;	// function number
		uint8	mHandle;	// file handle
		uint8	mF[6];
		uint8	mMode;		// file open mode
		uint8	mAttr1;
		uint8	mAttr2;
		uint8	mName1[12];
		uint8	mName2[12];
		uint8	mPath[65];
	};

	ParameterBuffer mParBuf;

	ATPCLinkFileHandle mFileHandles[15];

	uint8	mTransferBuffer[65536];
};

void ATCreateDevicePCLink(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATPCLinkDevice> p(new ATPCLinkDevice);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefPCLink = { "pclink", "pclink", L"PCLink", ATCreateDevicePCLink };

ATPCLinkDevice::ATPCLinkDevice() {
}

ATPCLinkDevice::~ATPCLinkDevice() {
}

void *ATPCLinkDevice::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceSIO::kTypeID:
			return static_cast<IATDeviceSIO *>(this);

		case IATDeviceIndicators::kTypeID:
			return static_cast<IATDeviceIndicators *>(this);

		case IATDeviceDiagnostics::kTypeID:
			return static_cast<IATDeviceDiagnostics *>(this);
	}

	return ATDevice::AsInterface(id);
}

void ATPCLinkDevice::SetReadOnly(bool readOnly) {
	mbReadOnly = readOnly;
}

void ATPCLinkDevice::SetBasePath(const wchar_t *basePath) {
	if (VDFileIsRelativePath(basePath))
		mBasePathNative = VDMakePath(VDGetProgramPath().c_str(), basePath);
	else
		mBasePathNative = basePath;

	if (!mBasePathNative.empty() && !VDIsPathSeparator(mBasePathNative.back()))
		mBasePathNative += '\\';
}

void ATPCLinkDevice::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefPCLink;
}

void ATPCLinkDevice::GetSettings(ATPropertySet& settings) {
	if (mbReadOnly)
		settings.Unset("write");
	else
		settings.SetBool("write", true);

	if (mbSetTimestamps)
		settings.SetBool("set_timestamps", true);
	else
		settings.Unset("set_timestamps");

	settings.SetString("path", mBasePathNative.c_str());
}

bool ATPCLinkDevice::SetSettings(const ATPropertySet& settings) {
	mbSetTimestamps = settings.GetBool("set_timestamps");
	SetReadOnly(!settings.GetBool("write"));
	SetBasePath(settings.GetString("path", L""));
	return true;
}

void ATPCLinkDevice::Shutdown() {
	AbortCommand();

	mpUIRenderer = nullptr;

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATPCLinkDevice::ColdReset() {
	for(auto& fh : mFileHandles)
		fh.Close();
}

void ATPCLinkDevice::InitIndicators(IATDeviceIndicatorManager *uir) {
	mpUIRenderer = uir;
}

void ATPCLinkDevice::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATPCLinkDevice::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (cmd.mDevice != 0x6F)
		return kCmdResponse_NotHandled;

	if (mBasePathNative.empty())
		return kCmdResponse_NotHandled;

	if (!cmd.mbStandardRate) {
		if (cmd.mCyclesPerBit < 30 || cmd.mCyclesPerBit > 34)
			return kCmdResponse_NotHandled;
	}

	const uint8 commandId = cmd.mCommand & 0x7f;

	mCommandAux1 = cmd.mAUX[0];
	mCommandAux2 = cmd.mAUX[1];

	Command command = kCommandNone;

	if (commandId == 0x53)			// status
		command = kCommandStatus;
	else if (commandId == 0x50)		// put
		command = kCommandPut;
	else if (commandId == 0x52)		// read
		command = kCommandRead;
	else if (commandId == 0x3F)
		command = kCommandGetHiSpeedIndex;
	else {
		g_ATLCPCLink("Unsupported command $%02x\n", cmd);
		return kCmdResponse_Fail_NAK;
	}

	mpSIOMgr->BeginCommand();

	// High-speed via bit 7 uses 38400 baud.
	// High-speed via HS command frame uses 52Kbaud. Currently we use US Doubler timings.
	if (cmd.mCommand & 0x80)
		mpSIOMgr->SetTransferRate(45, 450);
	else if (!cmd.mbStandardRate)
		mpSIOMgr->SetTransferRate(34, 394);

	mpSIOMgr->SendACK();

	BeginCommand(command);
	return kCmdResponse_Start;
}

void ATPCLinkDevice::OnSerialAbortCommand() {
	AbortCommand();
}

void ATPCLinkDevice::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) {
	mpReceiveFn(data, len);
}

void ATPCLinkDevice::OnSerialFence(uint32 id) {
	mpFenceFn();
}

IATDeviceSIO::CmdResponse ATPCLinkDevice::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}

void ATPCLinkDevice::DumpStatus(ATConsoleOutput& output) {
	output("Native base path: %ls", mBasePathNative.c_str());
	output("Current directory: %s", mCurDir.c_str());

	output("");

	VDStringA s;
	for(int i=0; i<15; ++i) {
		ATPCLinkFileHandle& fh = mFileHandles[i];

		s.sprintf("Handle $%02x: ", i + 1);

		if (!fh.IsOpen()) {
			s += "Not open";
		} else {
			if (fh.IsDir())
				s += "Directory";
			else if (fh.IsReadable()) {
				if (fh.IsWritable())
					s += "Read/Write";
				else
					s += "Read";
			} else
				s += "Write";

			s += " [";

			ATPCLinkFileName fn;

			fn.ParseFromNet(fh.GetDirEnt().mName);
			fn.AppendNative(s);
			s += ']';
		}

		output <<= s.c_str();
	}
}

void ATPCLinkDevice::BeginCommand(Command cmd) {
	mCommand = cmd;
	mCommandPhase = 0;

	AdvanceCommand();
}

void ATPCLinkDevice::AbortCommand() {
	if (mCommand) {
		mCommand = kCommandNone;
		mCommandPhase = 0;

		mpSIOMgr->EndCommand();
	}
}

void ATPCLinkDevice::AdvanceCommand() {
	switch(mCommand) {
		case kCommandGetHiSpeedIndex:
			g_ATLCPCLink("Sending high-speed index\n");
			mpSIOMgr->SendComplete();
			{
				uint8 hsindex = 9;
				mpSIOMgr->SendData(&hsindex, 1, true);
			}
			mpSIOMgr->EndCommand();
			break;

		case kCommandStatus:
			g_ATLCPCLink("Sending status: Flags=$%02x, Error=%3d, Length=%02x%02x\n", mStatusFlags, mStatusError, mStatusLengthHi, mStatusLengthLo);
			mpSIOMgr->SendComplete();
			{
				const uint8 data[4] = {
					mStatusFlags,
					mStatusError,
					mStatusLengthLo,
					mStatusLengthHi
				};

				mpSIOMgr->SendData(data, 4, true);
			}
			mpSIOMgr->EndCommand();
			break;

		case kCommandPut:
			mpReceiveFn = [this](const void *src, uint32 len) {
				memcpy(&mParBuf, src, std::min<uint32>(len, sizeof(mParBuf)));
			};
			mpSIOMgr->ReceiveData(0, mCommandAux1 ? mCommandAux1 : 256, true);
			mpFenceFn = [this]() {
				if (OnPut())
					mpSIOMgr->SendComplete();
				else
					mpSIOMgr->SendError();
				mpSIOMgr->EndCommand();
			};
			mpSIOMgr->InsertFence(0);
			break;

		case kCommandRead:
			// fwrite ($01) is special
			if (mParBuf.mFunction == 0x01) {
				mpReceiveFn = [this](const void *src, uint32 len) {
					memcpy(mTransferBuffer, src, len);
				};
				mpSIOMgr->ReceiveData(0, mParBuf.mF[0] + ((uint32)mParBuf.mF[1] << 8), true);
				mpSIOMgr->InsertFence(0);
				mpFenceFn = [this]() {
					OnRead();
					mpSIOMgr->EndCommand();
				};
				mpSIOMgr->SendComplete();
			} else {
				mpSIOMgr->SendComplete();
				mpFenceFn = [this]() {
					OnRead();
					mpSIOMgr->EndCommand();
				};
				mpSIOMgr->InsertFence(0);
			}

			break;
	}
}

void ATPCLinkDevice::FinishCommand() {
	AbortCommand();
}

bool ATPCLinkDevice::OnPut() {
	switch(mParBuf.mFunction) {
		case 0:		// fread
			{
				uint32 bufLen = mParBuf.mF[0] + 256*mParBuf.mF[1];
				g_ATLCPCLink("Received fread($%02x,%d) command.\n", mParBuf.mHandle, bufLen);

				if (CheckValidFileHandle(true)) {
					ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

					if (!fh.IsOpen()) {
						mStatusError = ATCIOSymbols::CIOStatNotOpen;
						return true;
					}

					if (!fh.IsReadable()) {
						mStatusError = ATCIOSymbols::CIOStatWriteOnly;
						return true;
					}

					const uint32 pos = fh.GetPosition();
					const uint32 len = fh.GetLength();


					if (pos >= len)
						bufLen = 0;
					else if (len - pos < bufLen)
						bufLen = len - pos;

					mStatusLengthLo = (uint8)bufLen;
					mStatusLengthHi = (uint8)(bufLen >> 8);
					mStatusError = bufLen ? ATCIOSymbols::CIOStatSuccess : ATCIOSymbols::CIOStatEndOfFile;
				}
			}
			return true;

		case 1:		// fwrite
			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			{
				uint32 bufLen = mParBuf.mF[0] + 256*mParBuf.mF[1];
				g_ATLCPCLink("Received fwrite($%02x,%d) command.\n", mParBuf.mHandle, bufLen);

				if (CheckValidFileHandle(true)) {
					ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

					if (!fh.IsOpen()) {
						mStatusError = ATCIOSymbols::CIOStatNotOpen;
						return true;
					}

					if (!fh.IsWritable()) {
						mStatusError = ATCIOSymbols::CIOStatReadOnly;
						return true;
					}

					const uint32 pos = fh.GetPosition();

					if (pos >= 0xffffff)
						bufLen = 0;
					else if (0xffffff - pos < bufLen)
						bufLen = 0xffffff - pos;

					mStatusLengthLo = (uint8)bufLen;
					mStatusLengthHi = (uint8)(bufLen >> 8);
					mStatusError = bufLen ? ATCIOSymbols::CIOStatSuccess : ATCIOSymbols::CIOStatDiskFull;
				}
			}
			return true;

		case 2:		// fseek
			{
				const uint32 pos = mParBuf.mF[0] + ((uint32)mParBuf.mF[1] << 8) + ((uint32)mParBuf.mF[2] << 16);
				g_ATLCPCLink("Received fseek($%02x,%d) command.\n", mParBuf.mHandle, pos);

				if (CheckValidFileHandle(true)) {
					ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

					if (!fh.IsOpen()) {
						mStatusError = ATCIOSymbols::CIOStatNotOpen;
						return true;
					}

					mStatusError = fh.Seek(pos);
				}
			}
			return true;

		case 3:		// ftell
			g_ATLCPCLink("Received ftell($%02x) command.\n", mParBuf.mHandle);

			if (!CheckValidFileHandle(true))
				return true;

			{
				ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

				if (!fh.IsOpen()) {
					mStatusError = ATCIOSymbols::CIOStatNotOpen;
					return true;
				}

				mStatusError = ATCIOSymbols::CIOStatSuccess;
			}
			return true;

		case 4:		// flen
			if (!CheckValidFileHandle(true))
				return true;

			mStatusError = ATCIOSymbols::CIOStatSuccess;
			return true;

		case 5:		// reserved
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;

		case 6:		// fnext
			g_ATLCPCLink("Received fnext($%02x) command.\n", mParBuf.mHandle);
			if (!CheckValidFileHandle(true))
				return true;

			mStatusError = ATCIOSymbols::CIOStatSuccess;
			return true;

		case 7:		// fclose
			g_ATLCPCLink("Received close($%02x) command.\n", mParBuf.mHandle);
			if (CheckValidFileHandle(false))
				mFileHandles[mParBuf.mHandle - 1].Close();

			mStatusError = ATCIOSymbols::CIOStatSuccess;
			return true;

		case 8:		// init
			g_ATLCPCLink("Received init command.\n");
			for(size_t i = 0; i < sizeof(mFileHandles)/sizeof(mFileHandles[0]); ++i)
				mFileHandles[i].Close();

			mStatusFlags = 0;
			mStatusError = ATCIOSymbols::CIOStatSuccess;
			mStatusLengthLo = 0;
			mStatusLengthHi = 0x6F;

			mCurDir.clear();
			return true;

		case 9:		// fopen
		case 10:	// ffirst
			if (mParBuf.mFunction == 9)
				g_ATLCPCLink("Received fopen() command.\n");
			else
				g_ATLCPCLink("Received ffirst() command.\n");

			OnReadActivity();
			{
				mParBuf.mHandle = 1;

				for(;;) {
					if (mParBuf.mHandle > sizeof(mFileHandles)/sizeof(mFileHandles[0])) {
						mStatusError = ATCIOSymbols::CIOStatTooManyFiles;
						return true;
					}

					if (!mFileHandles[mParBuf.mHandle - 1].IsOpen())
						break;

					++mParBuf.mHandle;
				}

				ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];
				VDStringA netPath;
				VDStringW nativePath;

				if (!ResolvePath(mParBuf.mFunction == 10, netPath) || !ResolveNativePath(nativePath, netPath))
					return true;

				ATPCLinkFileName pattern;

				if (!pattern.ParseFromNet(mParBuf.mName1))
					return true;

				const bool openDir = mParBuf.mFunction == 10 || (mParBuf.mMode & 0x10) != 0;

				VDDirectoryIterator it(VDMakePath(nativePath.c_str(), L"*").c_str());
				ATPCLinkDirEnt dirEnt;
				VDStringW nativeFilePath;

				bool matched = false;
				while(it.Next()) {
					ATPCLinkFileName fn;

					if (!fn.ParseFromNative(it.GetName()))
						continue;

					// We can't filter at this point for a directory, because the byte stream
					// needs to reflect all files while the FNEXT output shouldn't. Therefore,
					// we need to cache the pattern with the file handle instead.
					if (!openDir && !pattern.WildMatch(fn))
						continue;

					sint64 len = it.GetSize();

					if (len > 0xFFFFFF)
						len = 0xFFFFFF;

					dirEnt.SetFlagsFromAttributes(it.GetAttributes());

					if (it.IsDirectory()) {
						// skip blasted . and .. dirs
						if (it.IsDotDirectory())
							continue;
					}

					if (!IsDirEntIncluded(dirEnt))
						continue;

					dirEnt.mSectorMapLo = 0;
					dirEnt.mSectorMapHi = 0;
					dirEnt.mLengthLo = (uint8)len;
					dirEnt.mLengthMid = (uint8)((uint32)len >> 8);
					dirEnt.mLengthHi = (uint8)((uint32)len >> 16);
					memcpy(dirEnt.mName, fn.mName, sizeof dirEnt.mName);
					dirEnt.SetDate(it.GetCreationDate());

					if (!openDir) {
						matched = true;
						nativeFilePath = it.GetFullPath();
						break;
					}

					fh.AddDirEnt(dirEnt);
				}

				if (openDir) {
					// extract name from net path
					const char *s = netPath.c_str();
					const char *t = strrchr(s, '\\');
					if (t)
						s = t + 1;

					const char *end = s + strlen(s);
					const char *ext = strchr(s, '.');
					const char *fnend = ext ? ext : end;

					size_t fnlen = fnend - s;
					size_t extlen = end - ext;

					if (fnlen > 8)
						fnlen = 8;

					if (extlen > 3)
						extlen = 8;

					ATPCLinkFileName dirName;
					memset(dirName.mName, ' ', 11);

					if (fnlen == 0)
						memcpy(dirName.mName, "MAIN", 4);
					else
						memcpy(dirName.mName, s, fnlen);

					if (ext)
						memcpy(dirName.mName + 8, ext, extlen);

					fh.OpenAsDirectory(dirName, pattern, mParBuf.mAttr1);

					mStatusError = ATCIOSymbols::CIOStatSuccess;
				} else {
					if (!matched) {
						// cannot create file with a wildcard
						if (!(mParBuf.mMode & 4) && pattern.IsWild()) {
							mStatusError = ATCIOSymbols::CIOStatIllegalWild;
							return true;
						}

						nativeFilePath = nativePath;
						pattern.AppendNative(nativeFilePath);
					}

					if ((mParBuf.mMode & 8) && mbReadOnly) {
						mStatusError = ATCIOSymbols::CIOStatReadOnly;
					} else {
						bool setTimestamp = false;

						switch(mParBuf.mMode & 15) {
							case 4:		// read
								if (!matched)
									mStatusError = ATCIOSymbols::CIOStatFileNotFound;
								else {
									mStatusError = fh.OpenFile(nativeFilePath.c_str(),
										nsVDFile::kRead | nsVDFile::kDenyWrite | nsVDFile::kOpenExisting,
										true, false, false);
								}
								break;

							case 8:		// write
								mStatusError = fh.OpenFile(nativeFilePath.c_str(),
									nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways,
									false, true, false);
								setTimestamp = true;
								break;

							case 9:		// append
								mStatusError = fh.OpenFile(nativeFilePath.c_str(),
									nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kOpenAlways,
									false, true, true);

								if (fh.WasCreated())
									setTimestamp = true;
								break;

							case 12:	// update
								if (!matched)
									mStatusError = ATCIOSymbols::CIOStatFileNotFound;
								else {
									mStatusError = fh.OpenFile(nativeFilePath.c_str(),
										nsVDFile::kReadWrite | nsVDFile::kDenyAll | nsVDFile::kOpenExisting,
										true, true, false);
								}
								break;

							default:
								mStatusError = ATCIOSymbols::CIOStatInvalidCmd;
								break;
						}

						if (mbSetTimestamps && setTimestamp && fh.IsOpen()) {
							fh.SetTimestamp(mParBuf.mF);
						}
					}

					fh.SetDirEnt(dirEnt);
				}
			}
			return true;

		case 11:	// rename
			g_ATLCPCLink("Received rename() command.\n");

			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			OnWriteActivity();
			{
				VDStringW path;

				if (!ResolveNativePath(false, path))
					return true;

				ATPCLinkFileName srcpat;
				ATPCLinkFileName dstpat;

				if (!srcpat.ParseFromNet(mParBuf.mName1)
					|| !dstpat.ParseFromNet(mParBuf.mName2))
				{
					mStatusError = ATCIOSymbols::CIOStatFileNameErr;
					return true;
				}

				VDStringW srcNativePath;
				VDStringW dstNativePath;

				try {
					VDDirectoryIterator it(VDMakePath(path.c_str(), L"*").c_str());
					bool matched = false;

					while(it.Next()) {
						if (it.IsDotDirectory())
							continue;

						ATPCLinkDirEnt dirEnt;
						dirEnt.SetFlagsFromAttributes(it.GetAttributes());

						if (!IsDirEntIncluded(dirEnt))
							continue;

						ATPCLinkFileName fn;
						if (!fn.ParseFromNative(it.GetName()))
							continue;

						if (!srcpat.WildMatch(fn))
							continue;

						ATPCLinkFileName fn2(fn);
						fn2.WildMerge(dstpat);

						if (fn == fn2)
							continue;

						srcNativePath = path;
						fn.AppendNative(srcNativePath);

						dstNativePath = path;
						fn2.AppendNative(dstNativePath);

						VDMoveFile(srcNativePath.c_str(), dstNativePath.c_str());
						matched = true;
					}

					if (matched)
						mStatusError = ATCIOSymbols::CIOStatSuccess;
					else
						mStatusError = ATCIOSymbols::CIOStatFileNotFound;
				} catch(const MyWin32Error& e) {
					mStatusError = ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
				} catch(const MyError&) {
					mStatusError = ATCIOSymbols::CIOStatSystemError;
				}
			}
			return true;

		case 12:	// remove
			g_ATLCPCLink("Received remove() command.\n");

			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			OnWriteActivity();
			{
				VDStringW resultPath;

				if (!ResolveNativePath(false, resultPath))
					return true;

				ATPCLinkFileName fname;
				if (!fname.ParseFromNet(mParBuf.mName1)) {
					mStatusError = ATCIOSymbols::CIOStatFileNameErr;
					return true;
				}

				if (fname.IsWild()) {
					VDDirectoryIterator it(VDMakePath(resultPath.c_str(), L"*").c_str());
					bool matched = false;

					while(it.Next()) {
						if (it.IsDirectory())
							continue;

						ATPCLinkDirEnt dirEnt;
						dirEnt.SetFlagsFromAttributes(it.GetAttributes());

						if (!IsDirEntIncluded(dirEnt))
							continue;

						ATPCLinkFileName entryName;
						if (!entryName.ParseFromNative(it.GetName()))
							continue;

						if (!fname.WildMatch(entryName))
							continue;

						try {
							VDRemoveFile(it.GetFullPath().c_str());
							matched = true;
						} catch(const MyWin32Error& e) {
							mStatusError = ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
							return true;
						} catch(const MyError&) {
							mStatusError = ATCIOSymbols::CIOStatSystemError;
							return true;
						}
					}

					if (!matched) {
						mStatusError = ATCIOSymbols::CIOStatFileNotFound;
						return true;
					}
				} else {
					fname.AppendNative(resultPath);

					try {
						uint32 attrs = VDFileGetAttributes(resultPath.c_str());

						if (attrs == kVDFileAttr_Invalid) {
							mStatusError = ATCIOSymbols::CIOStatFileNotFound;
							return true;
						}

						ATPCLinkDirEnt dirEnt;
						dirEnt.SetFlagsFromAttributes(attrs);

						if (IsDirEntIncluded(dirEnt))
							VDRemoveFile(resultPath.c_str());
						else {
							mStatusError = ATCIOSymbols::CIOStatFileNotFound;
							return true;
						}
					} catch(const MyWin32Error& e) {
						mStatusError = ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
						return true;
					} catch(const MyError&) {
						mStatusError = ATCIOSymbols::CIOStatSystemError;
						return true;
					}
				}

				mStatusError = ATCIOSymbols::CIOStatSuccess;
			}
			return true;

		case 13:	// chmod
			g_ATLCPCLink("Received chmod() command.\n");

			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			OnWriteActivity();
			{
				VDStringW path;

				if (!ResolveNativePath(false, path))
					return true;

				ATPCLinkFileName srcpat;

				if (!srcpat.ParseFromNet(mParBuf.mName1)) {
					mStatusError = ATCIOSymbols::CIOStatFileNameErr;
					return true;
				}

				VDStringW srcNativePath;

				try {
					VDDirectoryIterator it(VDMakePath(path.c_str(), L"*").c_str());
					bool matched = false;

					while(it.Next()) {
						if (it.IsDotDirectory())
							continue;

						ATPCLinkDirEnt dirEnt;
						dirEnt.SetFlagsFromAttributes(it.GetAttributes());

						if (!IsDirEntIncluded(dirEnt))
							continue;

						ATPCLinkFileName fn;
						if (!fn.ParseFromNative(it.GetName()))
							continue;

						if (!srcpat.WildMatch(fn))
							continue;

						srcNativePath = path;
						fn.AppendNative(srcNativePath);

						uint32 attrMask = 0;
						uint32 attrVals = 0;

						if (mParBuf.mAttr2 & 0x11)
							attrMask |= kVDFileAttr_ReadOnly;

						if (mParBuf.mAttr2 & 0x22)
							attrMask |= kVDFileAttr_Hidden;

						if (mParBuf.mAttr2 & 0x44)
							attrMask |= kVDFileAttr_Archive;

						if (mParBuf.mAttr2 & 0x01)
							attrVals |= kVDFileAttr_ReadOnly;

						if (mParBuf.mAttr2 & 0x02)
							attrVals |= kVDFileAttr_Hidden;

						if (mParBuf.mAttr2 & 0x04)
							attrVals |= kVDFileAttr_Archive;

						VDFileSetAttributes(srcNativePath.c_str(), attrMask, attrVals);

						matched = true;
					}

					if (matched)
						mStatusError = ATCIOSymbols::CIOStatSuccess;
					else
						mStatusError = ATCIOSymbols::CIOStatFileNotFound;
				} catch(const MyWin32Error& e) {
					mStatusError = ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
				} catch(const MyError&) {
					mStatusError = ATCIOSymbols::CIOStatSystemError;
				}
			}
			return true;

		case 14:	// mkdir
			g_ATLCPCLink("Received mkdir() command.\n");

			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			OnWriteActivity();
			{
				VDStringW resultPath;

				if (!ResolveNativePath(false, resultPath))
					return true;

				ATPCLinkFileName fname;
				if (!fname.ParseFromNet(mParBuf.mName1)) {
					mStatusError = ATCIOSymbols::CIOStatFileNameErr;
					return true;
				}

				if (fname.IsWild()) {
					mStatusError = ATCIOSymbols::CIOStatIllegalWild;
					return true;
				}

				fname.AppendNative(resultPath);

				try {
					VDCreateDirectory(resultPath.c_str());

					if (mbSetTimestamps) {
						VDDate date = ATPCLinkDirEnt::DecodeDate(mParBuf.mF);

						if (date != VDDate {})
							VDSetDirectoryCreationTime(resultPath.c_str(), date);
					}

				} catch(const MyWin32Error& e) {
					mStatusError = ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
					return true;
				} catch(const MyError&) {
					mStatusError = ATCIOSymbols::CIOStatSystemError;
					return true;
				}

				mStatusError = ATCIOSymbols::CIOStatSuccess;
			}
			return true;

		case 15:	// rmdir
			g_ATLCPCLink("Received rmdir() command.\n");

			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			OnWriteActivity();
			{
				VDStringW resultPath;

				if (!ResolveNativePath(false, resultPath))
					return true;

				ATPCLinkFileName fname;
				if (!fname.ParseFromNet(mParBuf.mName1)) {
					mStatusError = ATCIOSymbols::CIOStatFileNameErr;
					return true;
				}

				if (fname.IsWild()) {
					mStatusError = ATCIOSymbols::CIOStatIllegalWild;
					return true;
				}

				fname.AppendNative(resultPath);

				try {
					VDRemoveDirectory(resultPath.c_str());
				} catch(const MyWin32Error& e) {
					mStatusError = ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
					return true;
				} catch(const MyError&) {	
					mStatusError = ATCIOSymbols::CIOStatSystemError;
					return true;
				}

				mStatusError = ATCIOSymbols::CIOStatSuccess;
			}
			return true;

		case 16:	// chdir
			g_ATLCPCLink("Received chdir() command.\n");
			{
				VDStringA resultPath;

				if (!ResolvePath(true, resultPath))
					return true;

				if (resultPath.size() > 64) {
					mStatusError = ATCIOSymbols::CIOStatPathTooLong;
					return true;
				}

				VDStringW nativePath;
				if (!ResolveNativePath(nativePath, resultPath))
					return true;

				uint32 attr = VDFileGetAttributes(nativePath.c_str());

				if (attr == kVDFileAttr_Invalid || !(attr & kVDFileAttr_Directory)) {
					mStatusError = ATCIOSymbols::CIOStatPathNotFound;
					return true;
				}

				mCurDir = resultPath;

				mStatusError = ATCIOSymbols::CIOStatSuccess;
			}
			return true;

		case 17:	// getcwd
			mStatusError = ATCIOSymbols::CIOStatSuccess;
			return true;

		case 18:	// setboot
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;

		case 19:	// getdfree
			mStatusError = ATCIOSymbols::CIOStatSuccess;
			return true;
	}

	g_ATLCPCLink("Unsupported put for function $%02x\n", mParBuf.mFunction);
	mStatusError = ATCIOSymbols::CIOStatNotSupported;
	return true;
}

bool ATPCLinkDevice::OnRead() {
	switch(mParBuf.mFunction) {
		case 0:		// fread
			OnReadActivity();
			{
				uint32 blocklen = mStatusLengthLo + ((uint32)mStatusLengthHi << 8);
				if (CheckValidFileHandle(true)) {
					ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];
					uint32 actual = 0;

					mStatusError = fh.Read(mTransferBuffer, blocklen, actual);

					mStatusLengthLo = (uint8)actual;
					mStatusLengthHi = (uint8)(actual >> 8);
				}

				mpSIOMgr->SendData(mTransferBuffer, blocklen, true);
			}
			return true;

		case 1:		// fwrite
			if (mbReadOnly) {
				mStatusError = ATCIOSymbols::CIOStatReadOnly;
				return true;
			}

			OnWriteActivity();
			{
				uint32 blocklen = mStatusLengthLo + ((uint32)mStatusLengthHi << 8);
				if (CheckValidFileHandle(true)) {
					ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

					mStatusError = fh.Write(mTransferBuffer, blocklen);
				}
			}
			return true;

		case 3:		// ftell
			if (CheckValidFileHandle(true)) {
				ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

				if (!fh.IsOpen()) {
					mStatusError = ATCIOSymbols::CIOStatNotOpen;
					return true;
				}

				const uint32 len = fh.GetPosition();
				mTransferBuffer[0] = (uint8)len;
				mTransferBuffer[1] = (uint8)(len >> 8);
				mTransferBuffer[2] = (uint8)(len >> 16);
				mStatusError = ATCIOSymbols::CIOStatSuccess;
			}
			mpSIOMgr->SendData(mTransferBuffer, 3, true);
			return true;

		case 4:		// flen
			memset(mTransferBuffer, 0, 3);
			if (CheckValidFileHandle(true)) {
				ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

				if (!fh.IsOpen())
					mStatusError = ATCIOSymbols::CIOStatNotOpen;
				else {
					uint32 len = fh.GetLength();

					mTransferBuffer[0] = (uint8)len;
					mTransferBuffer[1] = (uint8)(len >> 8);
					mTransferBuffer[2] = (uint8)(len >> 16);
				}
			}
			mpSIOMgr->SendData(mTransferBuffer, 3, true);
			return true;

		case 5:		// reserved
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;

		case 6:		// fnext
			OnReadActivity();
			memset(mTransferBuffer, 0, sizeof(ATPCLinkDirEnt) + 1);

			if (CheckValidFileHandle(true)) {
				ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

				if (!fh.IsDir()) {
					mStatusError = ATCIOSymbols::CIOStatBadParameter;
				} else {
					ATPCLinkDirEnt dirEnt = {0};

					if (!fh.GetNextDirEnt(dirEnt))
						mStatusError = ATCIOSymbols::CIOStatEndOfFile;
					else
						mStatusError = ATCIOSymbols::CIOStatSuccess;

					memcpy(mTransferBuffer + 1, &dirEnt, sizeof(ATPCLinkDirEnt));
				}
			}

			mTransferBuffer[0] = mStatusError;
			mpSIOMgr->SendData(mTransferBuffer, sizeof(ATPCLinkDirEnt) + 1, true);
			return true;

		case 7:		// fclose
		case 8:		// init
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;

		case 9:		// open
		case 10:	// ffirst
			mTransferBuffer[0] = mParBuf.mHandle;
			memset(mTransferBuffer + 1, 0, sizeof(ATPCLinkDirEnt));

			if (CheckValidFileHandle(true)) {
				ATPCLinkFileHandle& fh = mFileHandles[mParBuf.mHandle - 1];

				const ATPCLinkDirEnt dirEnt = fh.GetDirEnt();

				mStatusError = ATCIOSymbols::CIOStatSuccess;

				memcpy(mTransferBuffer + 1, &dirEnt, sizeof(ATPCLinkDirEnt));
			}
			mpSIOMgr->SendData(mTransferBuffer, sizeof(ATPCLinkDirEnt) + 1, true);
			return true;

		case 11:	// rename
		case 12:	// remove
		case 13:	// chmod
		case 14:	// mkdir
		case 15:	// rmdir
		case 16:	// chdir
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;

		case 17:	// getcwd
			{
				memset(mTransferBuffer, 0, 65);
				strncpy((char *)mTransferBuffer, mCurDir.c_str(), 64);
				mpSIOMgr->SendData(mTransferBuffer, 64, true);
			}
			return true;

		case 18:	// setboot
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;

		case 19:	// getdfree
			{
				ATPCLinkDiskInfo diskInfo = {};

				VDASSERTCT(sizeof(diskInfo) == 64);

				diskInfo.mInfoVersion = 0x21;
				diskInfo.mSectorCountLo = 0xff;
				diskInfo.mSectorCountHi = 0xff;
				diskInfo.mSectorsFreeLo = 0xff;
				diskInfo.mSectorsFreeHi = 0xff;
				diskInfo.mBytesPerSectorCode = 1;
				diskInfo.mVersion = 0x80;
				diskInfo.mBytesPerSectorHi = 2;
				diskInfo.mSectorsPerCluster = 1;
				memcpy(diskInfo.mVolumeLabel, "PCLink  ", 8);

				memcpy(mTransferBuffer, &diskInfo, 64);
				mpSIOMgr->SendData(mTransferBuffer, 64, true);
			}
			return true;

		case 20:	// chvol
			mStatusError = ATCIOSymbols::CIOStatNotSupported;
			return true;
	}

	return false;
}

bool ATPCLinkDevice::CheckValidFileHandle(bool setError) {
	if (mParBuf.mHandle == 0 || mParBuf.mHandle >= 16) {
		if (setError)
			mStatusError = ATCIOSymbols::CIOStatInvalidIOCB;

		return false;
	}

	return true;
}

bool ATPCLinkDevice::IsDirEntIncluded(const ATPCLinkDirEnt& dirEnt) const {
	return dirEnt.TestAttrFilter(mParBuf.mAttr1);
}

bool ATPCLinkDevice::ResolvePath(bool allowDir, VDStringA& resultPath) {
	const uint8 *s = mParBuf.mPath;

	// check for absolute path
	if (*s == '>' || *s == '\\') {
		resultPath.clear();
		++s;
	} else
		resultPath = mCurDir;

	// check for back-up specifiers
	while(*s == '<') {
		++s;

		while(!resultPath.empty()) {
			uint8 c = resultPath.back();

			resultPath.pop_back();

			if (c == '\\')
				break;
		}
	}

	// parse out remaining components

	bool inext = false;
	int fnchars = 0;
	int extchars = 0;

	while(uint8 c = *s++) {
		if (c == '>' || c == '\\') {
			if (inext && !extchars) {
				mStatusError = ATCIOSymbols::CIOStatFileNameErr;
				return false;
			}

			if (fnchars)
				resultPath += '\\';

			inext = false;
			fnchars = 0;
			extchars = 0;
			continue;
		}

		if (c == '.') {
			if (!fnchars) {
				if (s[0] == '.' && (s[1] == 0 || s[1] == '>' || s[1] == '\\')) {
					// remove a component
					if (!resultPath.empty() && resultPath.back() == '\\')
						resultPath.pop_back();

					while(!resultPath.empty()) {
						uint8 c = resultPath.back();

						resultPath.pop_back();

						if (c == '\\')
							break;
					}

					++s;

					if (!*s)
						break;

					++s;
					continue;
				}
			}

			if (inext) {
				mStatusError = ATCIOSymbols::CIOStatFileNameErr;
				return false;
			}

			resultPath += '.';
			inext = true;
			continue;
		}

		if (!fnchars)
			resultPath += '\\';

		if ((uint8)(c - 'a') < 26)
			c &= ~0x20;

		if (c != '_' && (uint8)(c - '0') >= 10 && (uint8)(c - 'A') >= 26) {
			mStatusError = ATCIOSymbols::CIOStatFileNameErr;
			return false;
		}

		if (inext) {
			if (++extchars > 3) {
				mStatusError = ATCIOSymbols::CIOStatFileNameErr;
				return false;
			}
		} else {
			if (++fnchars > 8) {
				mStatusError = ATCIOSymbols::CIOStatFileNameErr;
				return false;
			}
		}

		resultPath += c;
	}

	if (inext && !extchars && !allowDir) {
		mStatusError = ATCIOSymbols::CIOStatFileNameErr;
		return false;
	}

	// strip off trailing separator if present
	if (!resultPath.empty() && resultPath.back() == '\\')
		resultPath.pop_back();

	return true;
}

bool ATPCLinkDevice::ResolveNativePath(bool allowDir, VDStringW& resultPath) {
	VDStringA netPath;

	if (!ResolvePath(allowDir, netPath))
		return false;

	return ResolveNativePath(resultPath, netPath);
}

bool ATPCLinkDevice::ResolveNativePath(VDStringW& resultPath, const VDStringA& netPath) {

	// translate path
	resultPath = mBasePathNative;

	for(VDStringA::const_iterator it(netPath.begin()), itEnd(netPath.end());
		it != itEnd;
		++it)
	{
		char c = *it;

		if (c >= 'A' && c <= 'Z')
			c &= ~0x20;

		resultPath += (wchar_t)c;	
	}

	// ensure trailing separator
	if (!resultPath.empty() && resultPath.back() != L'\\')
		resultPath += L'\\';

	return true;
}

void ATPCLinkDevice::OnReadActivity() {
	if (mpUIRenderer)
		mpUIRenderer->SetPCLinkActivity(false);
}

void ATPCLinkDevice::OnWriteActivity() {
	if (mpUIRenderer)
		mpUIRenderer->SetPCLinkActivity(true);
}
