//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include <at/atcore/cio.h>
#include <at/atcore/devicecio.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/propertyset.h>
#include "hostdevice.h"
#include "hostdeviceutils.h"
#include "oshelper.h"
#include "cio.h"
#include "uirender.h"

using namespace ATCIOSymbols;

uint8 ATTranslateWin32ErrorToSIOError(uint32 err);

///////////////////////////////////////////////////////////////////////////

void ATHostDeviceMergeWildPath(VDStringW& dst, const wchar_t *s, const wchar_t *pat) {
	int charsLeft = 8;

	dst.clear();
	while(wchar_t patchar = *pat++) {
		wchar_t d = *s;

		if (d == L'.')
			d = 0;
		else if (d)
			++s;

		if (patchar == L'?') {
			if (d && charsLeft) {
				--charsLeft;
				dst += d;
			}

			continue;
		}

		if (patchar == L'*') {
			if (d) {
				if (charsLeft) {
					--charsLeft;
					dst += d;
				}

				--pat;
			}

			continue;
		}

		if (patchar == L'.') {
			if (d) {
				--s;
				continue;
			}

			if (*s == L'.')
				++s;

			dst.push_back(L'.');
			charsLeft = 3;
			continue;
		}

		if (charsLeft) {
			--charsLeft;
			dst.push_back(patchar);
		}
	}

	if (!dst.empty() && dst.back() == L'.')
		dst.pop_back();
}

#ifdef _DEBUG
namespace {
	struct ATTest_HostDeviceMergeWildPath {
		ATTest_HostDeviceMergeWildPath() {
			VDStringW resultPath;

			ATHostDeviceMergeWildPath(resultPath, L"foo.txt", L"bar.txt"); VDASSERT(resultPath == L"bar.txt");
			ATHostDeviceMergeWildPath(resultPath, L"foo.txt", L"*.*"); VDASSERT(resultPath == L"foo.txt");
			ATHostDeviceMergeWildPath(resultPath, L"foo.txt", L"*.bin"); VDASSERT(resultPath == L"foo.bin");
			ATHostDeviceMergeWildPath(resultPath, L"foo.txt", L"bar.*"); VDASSERT(resultPath == L"bar.txt");
			ATHostDeviceMergeWildPath(resultPath, L"foo.txt", L"f?x.txt"); VDASSERT(resultPath == L"fox.txt");
			ATHostDeviceMergeWildPath(resultPath, L"foo", L"b*.*"); VDASSERT(resultPath == L"boo");

		}
	} g_ATTest_HostDeviceMergeWildPath;
}
#endif

bool ATHostDeviceParseFilename(const char *s, bool allowDir, bool allowWild, bool allowPath, bool lowercase, VDStringW& nativeRelPath) {
	bool inext = false;
	bool wild = false;
	int fnchars = 0;
	int extchars = 0;
	uint32 componentStart = nativeRelPath.size();

	while(uint8 c = *s++) {
		if (c == '>' || c == '\\') {
			if (wild)
				return false;

			if (!allowPath)
				return false;

			if (inext && !extchars)
				return false;

			if (ATHostDeviceIsDevice(nativeRelPath.c_str() + componentStart))
				nativeRelPath.insert(nativeRelPath.begin() + componentStart, L'!');

			inext = false;
			fnchars = 0;
			extchars = 0;
			continue;
		}

		if (c == '.') {
			if (!fnchars) {
				if (s[0] == '.') {
					if (s[1] == 0 || s[1] == '>' || s[1] == '\\') {
						if (!allowPath)
							return false;

						// remove a component
						if (!nativeRelPath.empty() && nativeRelPath.back() == '\\')
							nativeRelPath.pop_back();

						while(!nativeRelPath.empty()) {
							wchar_t c = nativeRelPath.back();

							nativeRelPath.pop_back();

							if (c == '\\')
								break;
						}

						++s;

						if (!*s)
							break;

						++s;
						continue;
					}
				} else if (s[0] == '>' || s[0] == '\\' || s[0] == 0) {
					if (!allowPath)
						return false;

					continue;
				}
			}

			if (inext)
				return false;
		}

		if (!fnchars) {
			if (!nativeRelPath.empty())
				nativeRelPath += '\\';

			componentStart = nativeRelPath.size();
		}

		if (c == '.') {
			nativeRelPath += '.';
			inext = true;
			continue;
		}

		if ((uint8)(c - 'a') < 26)
			c &= ~0x20;

		if (c == '*' || c == '?')
			wild = true;
		else if (!ATHostDeviceIsValidPathChar(c))
			return false;

		if (inext) {
			if (++extchars > 3)
				return false;
		} else {
			if (++fnchars > 8)
				return false;
		}

		if (lowercase && (uint8)(c - 'A') < 26)
			c |= 0x20;

		nativeRelPath += c;
	}

	if (!allowDir) {
		if (fnchars + extchars == 0)
			return false;
	}

	if (wild && !allowWild)
		return false;

	if (!wild && ATHostDeviceIsDevice(nativeRelPath.c_str() + componentStart))
		nativeRelPath.insert(nativeRelPath.begin() + componentStart, L'!');

	// strip off trailing separator if present
	if (!nativeRelPath.empty() && nativeRelPath.back() == '\\')
		nativeRelPath.pop_back();

	return true;
}

#ifdef _DEBUG
namespace {
	struct ATTest_HostDeviceParseFilename {
		ATTest_HostDeviceParseFilename() {
			VDStringW nativeRelPath;

			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"TEST.TXT");
			nativeRelPath = L""; VDASSERT(!ATHostDeviceParseFilename("*.TXT", false, false, true, false, nativeRelPath));
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("*.TXT", false, true, true, false, nativeRelPath) && nativeRelPath == L"*.TXT");
			nativeRelPath = L""; VDASSERT(!ATHostDeviceParseFilename("*>*.TXT", false, false, true, false, nativeRelPath));
			nativeRelPath = L""; VDASSERT(!ATHostDeviceParseFilename("*>*.TXT", false, true, true, false, nativeRelPath));
			nativeRelPath = L""; VDASSERT(!ATHostDeviceParseFilename("*>FOO.TXT", false, true, true, false, nativeRelPath));
			nativeRelPath = L""; VDASSERT(!ATHostDeviceParseFilename("", false, false, true, false, nativeRelPath));
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("", true, false, true, false, nativeRelPath) && nativeRelPath == L"");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("FOO>", true, false, true, false, nativeRelPath) && nativeRelPath == L"FOO");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("FOO>BAR", true, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAR");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("FOO>BAR>", true, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAR");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("FOO>BAR>.", true, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAR");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("FOO>BAR>..", true, false, true, false, nativeRelPath) && nativeRelPath == L"FOO");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("CON", false, false, true, false, nativeRelPath) && nativeRelPath == L"!CON");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("CON.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"!CON.TXT");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("CONX.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"CONX.TXT");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"TEST.TXT");
			nativeRelPath = L""; VDASSERT( ATHostDeviceParseFilename("TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"TEST.TXT");
			nativeRelPath = L"FOO"; VDASSERT(ATHostDeviceParseFilename("TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\TEST.TXT");
			nativeRelPath = L"FOO\\BAR"; VDASSERT(ATHostDeviceParseFilename("TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAR\\TEST.TXT");
			nativeRelPath = L"FOO\\BAR"; VDASSERT(ATHostDeviceParseFilename("BAZ>TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAR\\BAZ\\TEST.TXT");
			nativeRelPath = L"FOO\\BAR"; VDASSERT(ATHostDeviceParseFilename("..\\BAZ>TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAZ\\TEST.TXT");
			nativeRelPath = L"FOO\\BAR\\BLAH"; VDASSERT(ATHostDeviceParseFilename("..\\..\\BAZ>TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"FOO\\BAZ\\TEST.TXT");
			nativeRelPath = L"FOO\\BAR\\BLAH"; VDASSERT(ATHostDeviceParseFilename("..\\..\\..\\..\\BAZ>TEST.TXT", false, false, true, false, nativeRelPath) && nativeRelPath == L"BAZ\\TEST.TXT");
		}
	} g_ATTest_HostDeviceParseFilename;
}
#endif

///////////////////////////////////////////////////////////////////////////
class ATHostDeviceChannel {
public:
	ATHostDeviceChannel();

	void Close();

	bool GetLength(uint32& len);
	uint8 Seek(uint32 pos);
	uint8 Read(void *dst, uint32 len, uint32& actual);
	uint8 Write(const void *src, uint32 len);

public:
	VDFile	mFile;
	vdfastvector<uint8>	mData;
	uint32	mOffset;
	uint32	mLength;
	bool	mbWriteBackData;
	bool	mbUsingRawData;
	bool	mbTranslateEOL;
	bool	mbOpen;
	bool	mbReadEnabled;
	bool	mbWriteEnabled;
};

ATHostDeviceChannel::ATHostDeviceChannel()
	: mOffset(0)
	, mLength(0)
	, mbWriteBackData(false)
	, mbUsingRawData(false)
	, mbTranslateEOL(false)
	, mbOpen(false)
	, mbReadEnabled(false)
	, mbWriteEnabled(false)
{
}

void ATHostDeviceChannel::Close() {
	if (!mbOpen)
		return;

	if (mbWriteBackData && mFile.isOpen()) {
		vdfastvector<uint8> tmp;
		tmp.reserve(mData.size());

		vdfastvector<uint8>::const_iterator it(mData.begin()), itEnd(mData.end());
		for(; it != itEnd; ++it) {
			uint8 c = *it;

			if (c == 0x9B) {
				tmp.push_back(0x0D);
				c = 0x0A;
			}

			tmp.push_back(c);
		}

		if (mFile.seekNT(0)) {
			mFile.writeData(tmp.data(), (uint32)tmp.size());
			mFile.truncateNT();
			mFile.closeNT();
		}
	}

	mbOpen = false;
	mbWriteBackData = false;
	mbUsingRawData = false;
	mbReadEnabled = false;
	mbWriteEnabled = false;
	mFile.closeNT();

	vdfastvector<uint8> tmp;
	tmp.swap(mData);
}

bool ATHostDeviceChannel::GetLength(uint32& len) {
	try {
		if (mbUsingRawData)
			len = (uint32)mData.size();
		else
			len = (uint32)mFile.size();
	} catch(const MyError&) {
		return false;
	}

	return true;
}

uint8 ATHostDeviceChannel::Seek(uint32 pos) {
	if (!mbWriteEnabled) {
		if (pos > mLength)
			return CIOStatInvPoint;
	}

	mOffset = pos;

	return CIOStatSuccess;
}

uint8 ATHostDeviceChannel::Read(void *dst, uint32 len, uint32& actual) {
	actual = 0;

	uint8 status = CIOStatSuccess;
	try {
		if (mbUsingRawData) {
			uint32 fileSize = (uint32)mData.size();

			if (mOffset < fileSize) {
				uint32 tc = fileSize - mOffset;

				if (tc > len)
					tc = len;

				memcpy(dst, mData.data() + mOffset, tc);
				actual = tc;
			}
		} else {
			mFile.seek(mOffset);
			actual = mFile.readData(dst, len);
		}

		mOffset += actual;

		if (!actual)
			status = CIOStatEndOfFile;
		else if (mOffset >= mLength)
			status = CIOStatSuccessEOF;

	} catch(const MyError&) {
		return CIOStatFatalDiskIO;
	}

	return status;
}

uint8 ATHostDeviceChannel::Write(const void *buf, uint32 tc) {
	if (0xFFFFFF - mOffset < tc)
		return CIOStatDiskFull;

	if (mbUsingRawData) {
		uint32 end = mOffset + tc;

		if (end > mLength)
			mData.resize(end, 0);

		memcpy(mData.data() + mOffset, buf, tc);
	} else {
		try {
			mFile.seek(mOffset);
			mFile.write(buf, tc);
		} catch(const MyError&) {
			return CIOStatFatalDiskIO;
		}
	}

	mOffset += tc;

	if (mLength < mOffset)
		mLength = mOffset;

	return CIOStatSuccess;
}

///////////////////////////////////////////////////////////////////////////
class ATHostDeviceEmulator final : public ATDevice, public IATHostDeviceEmulator, public IATDeviceCIO, public IATDeviceIndicators {
	ATHostDeviceEmulator(const ATHostDeviceEmulator&) = delete;
	ATHostDeviceEmulator& operator=(const ATHostDeviceEmulator&) = delete;
public:
	ATHostDeviceEmulator();
	~ATHostDeviceEmulator();

	void *AsInterface(uint32 id);

	void SetUIRenderer(IATUIRenderer *uir);

	bool IsReadOnly() const;
	void SetReadOnly(bool enabled);

	bool IsLongNameEncodingEnabled() const { return mbLongNameEncoding; }
	void SetLongNameEncodingEnabled(bool enabled) { mbLongNameEncoding = enabled; }

	bool IsLowercaseNamingEnabled() const { return mbLowercaseNaming; }
	void SetLowercaseNamingEnabled(bool enabled) { mbLowercaseNaming = enabled; }

	const wchar_t *GetBasePath(int index) const;
	void SetBasePath(int index, const wchar_t *s);

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void WarmReset() override;
	void ColdReset() override;
	void Shutdown();

public:
	void InitCIO(IATDeviceCIOManager *mgr) override;
	void GetCIODevices(char *buf, size_t len) const override;
	sint32 OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) override;
	sint32 OnCIOClose(int channel, uint8 deviceNo) override;
	sint32 OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) override;
	sint32 OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) override;
	void OnCIOAbortAsync() override;

public:
	void InitIndicators(IATDeviceIndicatorManager *r) override;

protected:
	uint8 ReadFilename(uint16 bufadr, bool allowDir, bool allowWild);
	uint8 ReadFilename(const uint8 *rawfn, bool allowDir, bool allowWild);
	bool GetNextMatch(VDDirectoryIterator& it, bool allowDirs = false, VDStringA *encodedName = 0);

	typedef ATHostDeviceChannel Channel;

	Channel		mChannels[8];
	VDStringW	mNativeBasePath[4];
	VDStringW	mNativeCurDir[4];
	VDStringW	mNativeSearchPath;
	VDStringA	mFilePattern;
	VDStringW	mNativeRelPath;
	VDStringW	mNativeDirPath;
	int			mPathIndex;
	bool		mbPathTranslate;
	bool		mbReadOnly;
	bool		mbLongNameEncoding;
	bool		mbLowercaseNaming;
	bool		mbFakeDisk;

	IATDeviceCIOManager *mpCIOMgr;
	IATDeviceIndicatorManager	*mpUIRenderer;

	char		mFilename[128];
	uint32		mFilenameEnd;
};

void ATCreateDeviceHostFS(const ATPropertySet& pset, IATDevice **dev) {
	ATHostDeviceEmulator *p = new ATHostDeviceEmulator;
	p->AddRef();
	*dev = p;
}

extern const ATDeviceDefinition g_ATDeviceDefHostDevice = { "hostfs", "hostfs", L"Host device (H:)", ATCreateDeviceHostFS };

ATHostDeviceEmulator::ATHostDeviceEmulator()
	: mPathIndex(0)
	, mbPathTranslate(false)
	, mbReadOnly(false)
	, mbLongNameEncoding(false)
	, mbLowercaseNaming(false)
	, mbFakeDisk(false)
	, mpCIOMgr(nullptr)
	, mpUIRenderer(nullptr)
	, mFilenameEnd(0)
{
	ColdReset();
}

ATHostDeviceEmulator::~ATHostDeviceEmulator() {
	ColdReset();
}

void *ATHostDeviceEmulator::AsInterface(uint32 id) {
	if (id == IATHostDeviceEmulator::kTypeID)
		return static_cast<IATHostDeviceEmulator *>(this);
	else if (id == IATDeviceCIO::kTypeID)
		return static_cast<IATDeviceCIO *>(this);
	else if (id == IATDeviceIndicators::kTypeID)
		return static_cast<IATDeviceIndicators *>(this);

	return ATDevice::AsInterface(id);
}

void ATHostDeviceEmulator::SetUIRenderer(IATUIRenderer *uir) {
	mpUIRenderer = uir;
}

bool ATHostDeviceEmulator::IsReadOnly() const {
	return mbReadOnly;
}

void ATHostDeviceEmulator::SetReadOnly(bool enabled) {
	mbReadOnly = enabled;
}

const wchar_t *ATHostDeviceEmulator::GetBasePath(int index) const {
	return (unsigned)index < 4 ? mNativeBasePath[index].c_str() : L"";
}

void ATHostDeviceEmulator::SetBasePath(int index, const wchar_t *basePath) {
	if ((unsigned)index >= 4)
		return;

	VDStringW& nbpath = mNativeBasePath[index];
	nbpath = basePath;

	if (!nbpath.empty()) {
		if (!VDIsPathSeparator(nbpath.back()))
			nbpath += L'\\';
	}
}

void ATHostDeviceEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefHostDevice;
}

void ATHostDeviceEmulator::GetSettings(ATPropertySet& settings) {
	settings.Clear();

	if (!mbReadOnly)
		settings.SetBool("readonly", false);

	if (!mbLongNameEncoding)
		settings.SetBool("encodelfn", false);

	if (!mbLowercaseNaming)
		settings.SetBool("lowercase", false);

	if (mbFakeDisk)
		settings.SetBool("fakedisk", true);

	for(int i=0; i<4; ++i) {
		const wchar_t *s = GetBasePath(i);
		if (*s)
			settings.SetString(VDStringA().sprintf("path%d", i+1).c_str(), s);
	}
}

bool ATHostDeviceEmulator::SetSettings(const ATPropertySet& settings) {
	mbReadOnly = settings.GetBool("readonly", true);
	mbLongNameEncoding = settings.GetBool("encodelfn", true);
	mbLowercaseNaming = settings.GetBool("lowercase", true);

	bool fakeDisk = settings.GetBool("fakedisk", false);
	if (mbFakeDisk != fakeDisk) {
		mbFakeDisk = fakeDisk;

		if (mpCIOMgr)
			mpCIOMgr->NotifyCIODevicesChanged(this);
	}

	for(int i=0; i<4; ++i) 
		SetBasePath(i, settings.GetString(VDStringA().sprintf("path%d", i+1).c_str(), L""));

	return true;
}

void ATHostDeviceEmulator::WarmReset() {
	ColdReset();
}

void ATHostDeviceEmulator::ColdReset() {
	for(int i=0; i<8; ++i) {
		Channel& ch = mChannels[i];

		ch.Close();
	}
}

void ATHostDeviceEmulator::Shutdown() {
	if (mpCIOMgr) {
		mpCIOMgr->RemoveCIODevice(this);
		mpCIOMgr = nullptr;
	}

	mpUIRenderer = nullptr;
}

void ATHostDeviceEmulator::InitCIO(IATDeviceCIOManager *mgr) {
	mpCIOMgr = mgr;
	mpCIOMgr->AddCIODevice(this);
}

void ATHostDeviceEmulator::GetCIODevices(char *buf, size_t len) const {
	vdstrlcpy(buf, "DH" + (mbFakeDisk ? 0 : 1), len);
}

sint32 ATHostDeviceEmulator::OnCIOOpen(int channel, uint8 deviceNo, uint8 mode, uint8 aux2, const uint8 *filename) {
	Channel& ch = mChannels[channel];
	if (ch.mbOpen)
		return kATCIOStat_IOCBInUse;

	bool append = false;
	bool create = false;
	bool write = false;
	uint32 flags;

	switch(mode) {
		case 0x04:
			flags = nsVDFile::kRead | nsVDFile::kDenyWrite | nsVDFile::kOpenExisting;
			break;

		case 0x08:
			flags = nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways;
			create = true;
			write = true;
			break;

		case 0x09:
			flags = nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kOpenAlways;
			create = true;
			append = true;
			write = true;
			break;

		case 0x0C:
			flags = nsVDFile::kReadWrite | nsVDFile::kDenyAll | nsVDFile::kOpenExisting;
			write = true;
			break;

		case 0x06:	// open directory
		case 0x07:	// open directory, showing extended (Atari DOS 2.5)
			break;

		default:
			return kATCIOStat_InvalidCmd;
	}

	if (write && mbReadOnly) {
		return kATCIOStat_ReadOnly;
	}

	ch.mbReadEnabled = (mode & 0x04) != 0;
	ch.mbWriteEnabled = (mode & 0x08) != 0;

	if (uint8 fnfail = ReadFilename(filename, true, true))
		return fnfail;

	if (mpUIRenderer)
		mpUIRenderer->SetHActivity(false);

	if (mode == 0x06 || mode == 0x07) {
		ch.mbOpen = true;
		ch.mbUsingRawData = true;
		ch.mData.clear();
		ch.mOffset = 0;

		const bool useSpartaDOSFormat = false;
		VDStringA line;

		try {
			VDDirectoryIterator it(mNativeSearchPath.c_str());

			// <---------------> 17 bytes
			//   DOS     SYS 037   (Normal)
			// * DOS     SYS 037   (Locked)
			//  <DOS     SYS>037   (DOS 2.5 extended)
			//  :BLAH    X   0008  (MyDOS 4.53 subdirectory)
			//   ZHAND    COM    857 11-01-85 10:51a  (SpartaDOS 3.2g file)
			//   X        BIN  1386k 25-01-06 20:16   (SpartaDOS X large file)
			//   FOO          <DIR>   6-06-94  3:48p  (SpartaDOS 3.2g dir)

			if (useSpartaDOSFormat) {
				const char kHeader1[]="Volume: ";
				const char kHeader2[]="Directory: ";

				ch.mData.push_back(0x9B);
				memcpy(ch.mData.alloc(sizeof(kHeader1)-1), kHeader1, sizeof(kHeader1)-1);
				ch.mData.push_back(0x9B);
				memcpy(ch.mData.alloc(sizeof(kHeader2)-1), kHeader2, sizeof(kHeader2)-1);

				VDStringW::const_iterator it(mNativeCurDir[mPathIndex].begin()), itEnd(mNativeCurDir[mPathIndex].end());

				if (it == itEnd) {
					const uint8 kMain[]={'M', 'A', 'I', 'N'};

					memcpy(ch.mData.alloc(4), kMain, 4);
				} else {
					ch.mData.push_back('>');

					for(; it != itEnd; ++it) {
						wchar_t c = *it;

						if (c == L'$' || c == L'!')
							continue;

						if (c == '\\')
							c = L'>';

						ch.mData.push_back((uint8)c);
					}
				}

				ch.mData.push_back(0x9B);
				ch.mData.push_back(0x9B);
			}

			VDStringA translatedName;
			while(GetNextMatch(it, true, &translatedName)) {
				const char *fn = translatedName.c_str();
				const char *ext = VDFileSplitExt(fn);

				int flen = (int)(ext - fn);
				if (flen > 8)
					flen = 8;

				if (*ext == '.')
					++ext;

				int elen = (int)strlen(ext);
				if (elen > 3)
					elen = 3;

				if (useSpartaDOSFormat) {
					line.clear();

					for(int i=0; i<flen; ++i)
						line.push_back(toupper((unsigned char)fn[i]));

					for(int i=flen; i<9; ++i)
						line.push_back(' ');

					for(int i=0; i<elen; ++i)
						line.push_back(toupper((unsigned char)ext[i]));

					for(int i=elen; i<4; ++i)
						line.push_back(' ');

					sint64 len = it.GetSize();

					if (len < 1000000)
						line.append_sprintf("%6u", (unsigned)len);
					else if (len < 1000000ull * 1024)
						line.append_sprintf("%5uk", (unsigned)(len >> 10));
					else if (len < 1000000ull * 1024 * 1024)
						line.append_sprintf("%5um", (unsigned)(len >> 20));
					else if (len < 1000000ull * 1024 * 1024 * 1024)
						line.append_sprintf("%5ug", (unsigned)(len >> 30));
					else
						line.append_sprintf("%5ut", (unsigned)(len >> 40));

					VDDate date = it.GetLastWriteDate();
					const VDExpandedDate& xdate = VDGetLocalDate(date);

					line.append_sprintf(" %02u-%02u-%02u %02u:%02u"
						, xdate.mDay
						, xdate.mMonth
						, xdate.mYear % 100
						, xdate.mHour
						, xdate.mSecond
						);

					size_t n = line.size();
					memcpy(ch.mData.alloc(n + 1), line.data(), n);
					ch.mData.back() = 0x9B;
				} else {
					uint8 *s = ch.mData.alloc(18);

					memset(s, ' ', 18);

					if (it.IsDirectory())
						s[1] = ':';
					else if (it.GetAttributes() & kVDFileAttr_ReadOnly)
						s[0] = '*';

					for(int i=0; i<flen; ++i)
						s[i+2] = toupper((unsigned char)fn[i]);

					for(int i=0; i<elen; ++i)
						s[i+10] = toupper((unsigned char)ext[i]);

					sint64 byteSize = it.GetSize();

					if (byteSize > 999 * 125)
						byteSize = 999 * 125;

					int sectors = ((int)byteSize + 124) / 125;

					s[14] = '0' + (sectors / 100);
					s[15] = '0' + ((sectors / 10) % 10);
					s[16] = '0' + (sectors % 10);
					s[17] = 0x9B;
				}
			}
		} catch(const MyError&) {
		}

		if (useSpartaDOSFormat) {
			static const char kSizeHeader[]=" 65521 FREE SECTORS";

			memcpy(ch.mData.alloc(sizeof(kSizeHeader)), kSizeHeader, sizeof(kSizeHeader)-1);
			ch.mData.back() = 0x9B;
		} else {
			uint8 *t = ch.mData.alloc(17);
			t[ 0] = '9';
			t[ 1] = '9';
			t[ 2] = '9';
			t[ 3] = ' ';
			t[ 4] = 'F';
			t[ 5] = 'R';
			t[ 6] = 'E';
			t[ 7] = 'E';
			t[ 8] = ' ';
			t[ 9] = 'S';
			t[10] = 'E';
			t[11] = 'C';
			t[12] = 'T';
			t[13] = 'O';
			t[14] = 'R';
			t[15] = 'S';
			t[16] = 0x9B;
		}

		ch.mLength = (uint32)ch.mData.size();
	} else {
		// attempt to open file
		ch.mbUsingRawData = false;

		try {
			VDDirectoryIterator it(mNativeSearchPath.c_str());

			if (!GetNextMatch(it)) {
				if (create)
					ch.mFile.open(VDMakePath(mNativeBasePath[mPathIndex].c_str(), mNativeRelPath.c_str()).c_str(), flags);
				else
					return kATCIOStat_FileNotFound;
			} else {
				ch.mFile.open(it.GetFullPath().c_str(), flags);
			}

			ch.mbTranslateEOL = mbPathTranslate;

			if (mbPathTranslate) {
				ch.mbUsingRawData = true;
				ch.mbWriteBackData = ch.mbWriteEnabled;

				if (ch.mbReadEnabled) {
					sint64 len = ch.mFile.size();

					if (len > 0xFFFFFF)
						throw MyError("file too large");

					uint32 len32 = (uint32)len;
					vdfastvector<uint8> tmp(len32);

					ch.mFile.read(tmp.data(), len32);

					ch.mData.reserve(len32);

					uint8 skipNext = 0;
					for(vdfastvector<uint8>::const_iterator it(tmp.begin()), itEnd(tmp.end());
						it != itEnd;
						++it)
					{
						uint8 c = *it;

						if (skipNext) {
							uint8 d = skipNext;
							skipNext = 0;

							if (c == d)
								continue;
						}

						if (c == '\r' || c == '\n') {
							skipNext = c ^ ('\r' ^ '\n');
							c = 0x9B;
						}

						ch.mData.push_back(c);
					}
				}

				ch.mOffset = 0;
				ch.mLength = (uint32)ch.mData.size();

				if (append)
					ch.mOffset = ch.mLength;
			} else {
				sint64 size64 = ch.mFile.size();

				ch.mLength = size64 > 0xFFFFFF ? 0xFFFFFF : (uint32)size64;

				if (append) {
					ch.mFile.seek(ch.mLength);
					ch.mOffset = ch.mLength;
				} else
					ch.mOffset = 0;
			}
		} catch(const MyWin32Error& e) {
			ch.mFile.closeNT();
			return ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
		} catch(const MyError&) {
			ch.mFile.closeNT();
			return kATCIOStat_FileNotFound;
		}

		// all good
		ch.mbOpen = true;
	}

	return kATCIOStat_Success;
}

sint32 ATHostDeviceEmulator::OnCIOClose(int channel, uint8 deviceNo) {
	Channel& ch = mChannels[channel];

	ch.Close();

	return kATCIOStat_Success;
}

sint32 ATHostDeviceEmulator::OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) {
	Channel& ch = mChannels[channel];

	if (!ch.mbOpen)
		return kATCIOStat_NotOpen;

	if (!ch.mbReadEnabled)
		return kATCIOStat_WriteOnly;

	if (mpUIRenderer)
		mpUIRenderer->SetHActivity(false);

	// check if we can do a burst read
	return ch.Read(buf, len, actual);
}

sint32 ATHostDeviceEmulator::OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) {
	Channel& ch = mChannels[channel];

	if (!ch.mbOpen)
		return kATCIOStat_NotOpen;

	if (!ch.mbWriteEnabled)
		return kATCIOStat_WriteOnly;

	if (mpUIRenderer)
		mpUIRenderer->SetHActivity(true);

	uint8 status = ch.Write(buf, len);
	actual = len;

	return status;
}

sint32 ATHostDeviceEmulator::OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) {
	return kATCIOStat_Success;
}

sint32 ATHostDeviceEmulator::OnCIOSpecial(int channel, uint8 deviceNo, uint8 command, uint16 bufadr, uint16 buflen, uint8 aux[6]) {
	try {
		// The Atari OS manual has incorrect command IDs for the NOTE and
		// POINT commands: it says that NOTE is $25 and POINT is $26, but
		// it's the other way around.

		if (command == 0x26) {			// note
			Channel& ch = mChannels[channel];

			if (!ch.mbOpen)
				return kATCIOStat_NotOpen;

			int offset = ch.mOffset;
			int sector = offset / 125;

			// Note that we must write directly to the originating IOCB as
			// AUX3-5 are not copied into zero page.

			aux[2] = (uint8)sector;
			aux[3] = (uint8)(sector >> 8);
			aux[4] = (uint8)(offset % 125);

			return kATCIOStat_Success;
		} else if (command == 0x25) {	// point
			Channel& ch = mChannels[channel];

			if (!ch.mbOpen)
				return kATCIOStat_NotOpen;

			uint8 rawpos[3];
			for(int i=0; i<3; ++i)
				rawpos[i] = aux[2+i];

			if (rawpos[2] >= 125)
				return kATCIOStat_InvPoint;

			uint32 pos = 125*(rawpos[0] + 256*(int)rawpos[1]) + rawpos[2];

			return ch.Seek(pos);

		} else if (command == 0x23) {	// lock
			// DOS 2.0S lock behavior:
			// - A file can be locked while open for write.
			// - A file can't be locked on creation until it has been closed (file not found).
			// - If no file is found, file not found is returned, even with wildcards.

			if (mbReadOnly)
				return kATCIOStat_ReadOnly;

			if (uint8 fnfail = ReadFilename(bufadr, true, true))
				return fnfail;

			VDDirectoryIterator it(mNativeSearchPath.c_str());
			bool found = false;

			while(GetNextMatch(it)) {
				ATFileSetReadOnlyAttribute(it.GetFullPath().c_str(), true);
				found = true;
			}

			if (!found)
				return kATCIOStat_FileNotFound;
			else
				return kATCIOStat_Success;
		} else if (command == 0x24) {	// unlock
			// DOS 2.0S unlock behavior:
			// - A file can be unlocked while open for write.
			// - A file can't be unlocked on creation until it has been closed (file not found).
			// - If no file is found, file not found is returned, even with wildcards.

			if (mbReadOnly)
				return kATCIOStat_ReadOnly;

			if (uint8 fnfail = ReadFilename(bufadr, true, true))
				return fnfail;

			VDDirectoryIterator it(mNativeSearchPath.c_str());
			bool found = false;

			while(GetNextMatch(it)) {
				ATFileSetReadOnlyAttribute(it.GetFullPath().c_str(), false);
				found = true;
			}

			if (!found)
				return kATCIOStat_FileNotFound;
			else
				return kATCIOStat_Success;
		} else if (command == 0x21) {	// delete
			if (mbReadOnly)
				return kATCIOStat_ReadOnly;

			if (uint8 fnfail = ReadFilename(bufadr, true, true))
				return fnfail;

			VDDirectoryIterator it(mNativeSearchPath.c_str());
			bool found = false;

			while(GetNextMatch(it)) {
				VDRemoveFileEx(it.GetFullPath().c_str());
				found = true;
			}

			if (!found)
				return kATCIOStat_FileNotFound;
			else
				return kATCIOStat_Success;
		} else if (command == 0x20) {	// rename
			if (uint8 fnfail = ReadFilename(bufadr, false, true))
				return fnfail;

			// look for second filename
			uint8 c;
			mpCIOMgr->ReadMemory(&c, bufadr + mFilenameEnd, 1);

			if (c != ',' && c != ' ')
				return kATCIOStat_FileNameErr;

			uint32 idx2 = mFilenameEnd + 1;
			for(;;) {
				mpCIOMgr->ReadMemory(&c, bufadr + idx2, 1);
				if (c != ' ')
					break;

				++idx2;

				if (idx2 >= 256)
					return kATCIOStat_FileNameErr;
			}

			// parse out second filename
			VDStringA fn2;
			for(;;) {
				mpCIOMgr->ReadMemory(&c, bufadr + (idx2++), 1);

				if (c == 0x9B || c == 0x20 || c == ',' || c == 0)
					break;

				// check for excessively long or unterminated filename
				if (idx2 == 256) 
					return kATCIOStat_FileNameErr;

				// reject non-ASCII characters
				if (c < 0x20 || c > 0x7f)
					return kATCIOStat_FileNameErr;

				// convert to lowercase
				if (c >= 0x61 && c <= 0x7A)
					c -= 0x20;

				fn2.push_back((char)c);
			}

			VDStringW nativePath2;
			if (!ATHostDeviceParseFilename(fn2.c_str(), false, true, false, mbLowercaseNaming, nativePath2))
				return kATCIOStat_FileNameErr;

			VDDirectoryIterator it(mNativeSearchPath.c_str());
			const wchar_t *const destName = nativePath2.c_str();
			const bool wildDest = ATHostDeviceIsPathWild(destName);

			VDStringW destFileBuf;
			bool matched = false;

			while(GetNextMatch(it, true)) {
				if (VDFileGetAttributes(it.GetFullPath().c_str()) & kVDFileAttr_ReadOnly)
					return kATCIOStat_FileLocked;

				if (wildDest) {
					const wchar_t *srcName = it.GetName();
					if (*srcName == L'$' || *srcName == L'!')
						++srcName;

					destFileBuf.clear();
					ATHostDeviceMergeWildPath(destFileBuf, srcName, destName);

					if (ATHostDeviceIsDevice(destFileBuf.c_str()))
						destFileBuf.insert(destFileBuf.begin(), L'!');

					const VDStringW& destFile = VDMakePath(mNativeDirPath.c_str(), destFileBuf.c_str());
					VDMoveFile(it.GetFullPath().c_str(), destFile.c_str());
				} else {
					const VDStringW& destFile = VDMakePath(mNativeDirPath.c_str(), destName);
					VDMoveFile(it.GetFullPath().c_str(), destFile.c_str());
				}

				matched = true;
			}

			if (matched)
				return kATCIOStat_Success;
			else
				return kATCIOStat_FileNotFound;
		} else if (command == 0x27) {	// SDX: Get File Length
			Channel& ch = mChannels[channel];

			if (!ch.mbOpen)
				return kATCIOStat_NotOpen;

			uint32 len;
			if (!ch.GetLength(len))
				return kATCIOStat_FatalDiskIO;

			aux[2] = (uint8)len;
			aux[3] = (uint8)(len >> 8);
			aux[4] = (uint8)(len >> 16);
			return kATCIOStat_Success;
		} else if (command == 0x2C || command == 0x29) {	// SDX: Set Current Directory / MyDOS: Change Directory
			if (uint8 fnfail = ReadFilename(bufadr, true, true))
				return fnfail;

			if (mFilePattern.empty()) {
				VDDirectoryIterator it(mNativeSearchPath.c_str());

				if (!GetNextMatch(it))
					return kATCIOStat_PathNotFound;

				const VDStringW& newPath = VDMakePath(mNativeBasePath[mPathIndex].c_str(), VDFileSplitPathLeft(mNativeRelPath).c_str());

				mNativeCurDir[mPathIndex] = VDMakePath(newPath.c_str(), it.GetName());
				return kATCIOStat_Success;
			} else {
				const VDStringW& newPath = VDMakePath(mNativeBasePath[mPathIndex].c_str(), mNativeRelPath.c_str());

				if (!VDDoesPathExist(newPath.c_str()))
					return kATCIOStat_PathNotFound;

				mNativeCurDir[mPathIndex] = mNativeRelPath;
				return kATCIOStat_Success;
			}
		} else if (command == 0x30) {	// SDX: Get Current Directory
			VDStringW::const_iterator it(mNativeCurDir[mPathIndex].begin()), itEnd(mNativeCurDir[mPathIndex].end());

			vdfastvector<uint8> path;
			if (it != itEnd) {
				path.push_back((uint8)'>');

				for(; it != itEnd; ++it) {
					wchar_t c = *it;

					if (c == L'$' || c == L'!')
						continue;

					if (c == '\\')
						c = L'>';

					path.push_back((uint8)c);
				}
			}

			path.push_back(0);

			// Yes, buflen is correct below... bufadr holds the source path
			// that selects the drive, while buflen holds the dest buffer.
			mpCIOMgr->WriteMemory(buflen, path.data(), (uint16)path.size());

			return kATCIOStat_Success;
		} else if (command == 0x2A) {	// SDX: Create Directory
			if (mbReadOnly)
				return kATCIOStat_ReadOnly;

			if (uint8 fnfail = ReadFilename(bufadr, false, false))
				return fnfail;

			const VDStringW& newPath = VDMakePath(mNativeBasePath[mPathIndex].c_str(), mNativeRelPath.c_str());
			uint8 status = CIOStatSuccess;

			VDCreateDirectory(newPath.c_str());

			return status;
		} else if (command == 0x2B) {	// SDX: Remove Directory
			if (mbReadOnly)
				return kATCIOStat_ReadOnly;

			if (uint8 fnfail = ReadFilename(bufadr, false, false))
				return fnfail;

			uint8 status = CIOStatSuccess;

			if (mFilePattern.empty()) {
				VDDirectoryIterator it(mNativeSearchPath.c_str());

				if (GetNextMatch(it)) {
					const VDStringW& newPath = VDMakePath(mNativeDirPath.c_str(), it.GetName());

					VDRemoveDirectory(newPath.c_str());
				} else {
					status = CIOStatPathNotFound;
				}
			} else {
				const VDStringW& newPath = VDMakePath(mNativeBasePath[mPathIndex].c_str(), mNativeRelPath.c_str());

				VDRemoveDirectory(newPath.c_str());
			}

			return status;
		}
	} catch(const MyWin32Error& e) {
		return ATTranslateWin32ErrorToSIOError(e.GetWin32Error());
	} catch(const MyError&) {
		return kATCIOStat_FatalDiskIO;
	}

	return kATCIOStat_NotSupported;
}

void ATHostDeviceEmulator::OnCIOAbortAsync() {
}

void ATHostDeviceEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

uint8 ATHostDeviceEmulator::ReadFilename(uint16 bufadr, bool allowDir, bool allowWild) {
	uint8 fn[256];
	mpCIOMgr->ReadFilename(fn, vdcountof(fn), bufadr);

	return ReadFilename(fn, allowDir, allowWild);
}

uint8 ATHostDeviceEmulator::ReadFilename(const uint8 *rawfn, bool allowDir, bool allowWild) {
	int i = 0;

	while(uint8 c = *rawfn++) {
		if (c == 0x9B || c == 0x20 || c == ',' || c == 0)
			break;

		// check for excessively long or unterminated filename
		if (i == 127)
			return kATCIOStat_FileNameErr;

		// reject non-ASCII characters
		if (c < 0x20 || c > 0x7f)
			return kATCIOStat_FileNameErr;

		if (mbLowercaseNaming) {
			// convert to lowercase
			if (c >= 0x41 && c <= 0x5A)
				c += 0x20;
		} else {
			// convert to uppercase
			if (c >= 0x61 && c <= 0x7A)
				c -= 0x20;
		}

		mFilename[i++] = (char)c;
	}

	mFilenameEnd = i;
	mFilename[i] = 0;

	// parse path prefix
	int index = 1;

	// check for H device specifier
	const char *s = mFilename;
	char c = *s++;
	if (c != 'H' && c != 'h' && ((c != 'D' && c != 'd') || !mbFakeDisk))
		return kATCIOStat_FileNameErr;

	// check for drive number
	c = *s++;

	index = 1;
	if (c != ':') {
		if (c < '1' || c > '9' || c == '5')
			return kATCIOStat_FileNameErr;

		index = c - '0';

		c = *s++;
		if (c != ':')
			return kATCIOStat_FileNameErr;
	}

	VDStringW parsedPath;

	// check for parent specifiers
	if (*s == '>' || *s == '\\') {
		++s;
	} else
		parsedPath = mNativeCurDir[mPathIndex];

	// check for back-up specifiers
	while(*s == L'<') {
		++s;

		while(!parsedPath.empty()) {
			wchar_t c = parsedPath.back();

			parsedPath.pop_back();

			if (c == L'\\')
				break;
		}
	}

	if (index >= 6) {
		mPathIndex = index - 6;
		mbPathTranslate = true;
	} else {
		mPathIndex = index - 1;
		mbPathTranslate = false;
	}

	if (mNativeBasePath[mPathIndex].empty())
		return kATCIOStat_FileNameErr;

	// validate filename format
	if (!ATHostDeviceParseFilename(s, allowDir, allowWild, true, mbLowercaseNaming, parsedPath))
		return kATCIOStat_FileNameErr;

	const wchar_t *nativeRelPath = parsedPath.c_str();
	const wchar_t *nativeFile = VDFileSplitPath(nativeRelPath);

	mNativeRelPath = nativeRelPath;
	mNativeDirPath = mNativeBasePath[mPathIndex];
	mNativeDirPath.append(nativeRelPath, nativeFile);
	mNativeSearchPath = mNativeDirPath;
	mNativeSearchPath += L"*.*";
	mFilePattern = VDTextWToA(*nativeFile == L'!' ? nativeFile + 1 : nativeFile);

	if (mFilePattern.find('.') == VDStringW::npos)
		mFilePattern += '.';

	return 0;
}

bool ATHostDeviceEmulator::GetNextMatch(VDDirectoryIterator& it, bool allowDirs, VDStringA *encodedName) {
	char xlName[13];

	for(;;) {
		if (!it.Next())
			return false;

		if (it.IsDotDirectory())
			continue;

		if (!allowDirs && it.IsDirectory())
			continue;

		ATHostDeviceEncodeName(xlName, it.GetName(), mbLongNameEncoding);

		if (VDFileWildMatch(mFilePattern.c_str(), xlName)) {
			if (encodedName)
				encodedName->assign(xlName);

			return true;
		}
	}
}
