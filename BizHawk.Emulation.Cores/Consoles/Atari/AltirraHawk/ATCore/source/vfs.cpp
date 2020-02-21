//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - virtualized file system support
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
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/strutil.h>
#include <vd2/system/zip.h>
#include <at/atcore/vfs.h>

vdfunction<void(ATVFSFileView *, const wchar_t *, ATVFSFileView **)> g_ATVFSAtfsHandler;

ATInvalidVFSPathException::ATInvalidVFSPathException(const wchar_t *badPath)
	: MyError("Invalid VFS path: %ls.", badPath)
{
}

ATUnsupportedVFSPathException::ATUnsupportedVFSPathException(const wchar_t *badPath)
	: MyError("Unsupported VFS path: %ls.", badPath)
{
}

bool ATDecodeVFSPath(VDStringW& dst, const VDStringSpanW& src) {
	auto it = src.begin();
	const auto itEnd = src.end();

	if (it == itEnd)
		return false;

	uint32 unicode = 0;
	uint8 extsleft = 0;

	while(it != itEnd) {
		wchar_t c = *it++;

		if (c == '%') {
			if (itEnd - it < 2)
				return false;

			uint8 code = 0;
			for(int i=0; i<2; ++i) {
				c = *it++;

				code <<= 4;
				if (c >= '0' && c <= '9')
					code += (uint8)(c - '0');
				else if (c >= 'a' && c <= 'f')
					code += (uint8)((c - 'a') + 10);
				else if (c >= 'A' && c <= 'F')
					code += (uint8)((c - 'A') + 10);
				else
					return false;
			}

			if (extsleft) {
				// next byte must be an extension byte
				if (code < 0x80 || code >= 0xC0)
					return false;

				unicode = (unicode << 6) + (uint32)(code - 0x80);

				if (--extsleft)
					continue;

				// direct encoding of surrogate code points is invalid
				if ((uint32)(unicode - 0xD800) < 0x800)
					return false;

				// check if we need to emit a surrogate
				if (c >= 0x10000) {
					dst += (wchar_t)(0xD800 + ((c - 0x10000) >> 10));

					c = (wchar_t)(0xDC00 + (c & 0x3FF));
				}
			} else if (code < 0x80)
				c = code;
			else if (c < 0xC0)
				return false;
			else if (c < 0xE0) {
				unicode = c & 0x1F;
				extsleft = 1;
				continue;
			} else if (c < 0xF0) {
				unicode = c & 0x0F;
				extsleft = 2;
				continue;
			} else if (c < 0xF8) {
				unicode = c & 0x07;
				extsleft = 3;
				continue;
			} else {
				// U+200000 or above is invalid
				return false;
			}
		} else {
			// check for unterminated UTF-8 sequence
			if (extsleft)
				return false;
		}

		dst += c;
	}

	// check for unterminated UTF-8 sequence
	if (extsleft)
		return false;

	return true;
}

void ATEncodeVFSPath(VDStringW& dst, const VDStringSpanW& src, bool filepath) {
	uint32 surrogateOffset = 0;

	for(wchar_t cw : src) {
		uint32 c = cw;

		if (sizeof(wchar_t) == 2)
			c &= 0xFFFF;

		if ((uint32)(c - 0xD800) < 0x800) {
			if (c < 0xDC00) {
				surrogateOffset = ((c - 0xD800) << 10) + 0x10000;
				continue;
			}

			c = (c - 0xDC00) + surrogateOffset;
			surrogateOffset = 0;
		}

		if (filepath && c == L'\\')
			c = L'/';

		bool escapingNeeded = false;
		switch(c) {
			case 0x21:
			case 0x23:
			case 0x24:
			case 0x26:
			case 0x27:
			case 0x28:
			case 0x29:
			case 0x2A:
			case 0x2B:
			case 0x2C:
			case 0x3B:
			case 0x3D:
			case 0x3F:
			case 0x40:
			case 0x5B:
			case 0x5D:
				escapingNeeded = true;
				break;

			default:
				if (c < 0x20 || c > 0x7E)
					escapingNeeded = true;
				break;
		}

		if (escapingNeeded) {
			if (c >= 0x10000) {
				dst.append_sprintf(L"%%%02X%%%02X%%%02X%%%02X", 0xC0 + ((c >> 18) & 0x07), 0x80 + ((c >> 12) & 0x3F), 0x80 + ((c >> 6) & 0x3F), 0x80 + (c & 0x3F));
			} else if (c >= 0x800) {
				dst.append_sprintf(L"%%%02X%%%02X%%%02X", 0xC0 + (c >> 12), 0x80 + ((c >> 6) & 0x3F), 0x80 + (c & 0x3F));
			} else if (c >= 0x80) {
				dst.append_sprintf(L"%%%02X%%%02X", 0xC0 + (c >> 6), 0x80 + (c & 0x3F));
			} else {
				dst.append_sprintf(L"%%%02X", c);
			}
		} else {
			dst += (wchar_t)c;
		}
	}
}

ATVFSProtocol ATParseVFSPath(const wchar_t *s, VDStringW& basePath, VDStringW& subPath) {
	// check for a protocol
	const wchar_t *colon = wcschr(s, ':');
	if (!colon || colon - s < 2 || colon[1] != '/' || colon[2] != '/') {
		// no protocol -- assume it's a straight path
		basePath = s;
		return kATVFSProtocol_File;
	}

	const VDStringSpanW protocol(s, colon);

	if (protocol == L"file") {
		if (colon[3] != L'/')
			return kATVFSProtocol_None;

		if (!ATDecodeVFSPath(basePath, VDStringSpanW(colon + 4)))
			return kATVFSProtocol_None;

		// convert forward slashes to back slashes
		for(auto& c : basePath) {
			if (c == L'/')
				c = L'\\';
		}

		return kATVFSProtocol_File;
	} else if (protocol == L"zip") {
		const wchar_t *zipPathStart = colon + 3;
		const wchar_t *zipPathEnd = wcsrchr(zipPathStart, L'!');

		if (!zipPathEnd)
			return kATVFSProtocol_None;

		if (!ATDecodeVFSPath(basePath, VDStringSpanW(zipPathStart, zipPathEnd)))
			return kATVFSProtocol_None;

		if (!ATDecodeVFSPath(subPath, VDStringSpanW(zipPathEnd + 1)))
			return kATVFSProtocol_None;

		return kATVFSProtocol_Zip;
	} else if (protocol == L"gz") {
		if (!ATDecodeVFSPath(basePath, VDStringSpanW(colon + 3)))
			return kATVFSProtocol_None;

		return kATVFSProtocol_GZip;
	} else if (protocol == L"atfs") {
		const wchar_t *fsPathStart = colon + 3;
		const wchar_t *fsPathEnd = wcsrchr(fsPathStart, L'!');

		if (!fsPathEnd)
			return kATVFSProtocol_None;

		if (!ATDecodeVFSPath(basePath, VDStringSpanW(fsPathStart, fsPathEnd)))
			return kATVFSProtocol_None;

		if (!ATDecodeVFSPath(subPath, VDStringSpanW(fsPathEnd + 1)))
			return kATVFSProtocol_None;

		return kATVFSProtocol_Atfs;
	}

	return kATVFSProtocol_None;
}

bool ATVFSIsFilePath(const wchar_t *s) {
	VDStringW basePath, subPath;

	return kATVFSProtocol_File == ATParseVFSPath(s, basePath, subPath);
}

///////////////////////////////////////////////////////////////////////////

VDStringW ATMakeVFSPathForGZipFile(const wchar_t *path) {
	VDStringW s(L"gz://");

	ATEncodeVFSPath(s, VDStringSpanW(path), true);

	return s;
}

VDStringW ATMakeVFSPathForZipFile(const wchar_t *path, const wchar_t *fileName) {
	VDStringW s(L"zip://");

	ATEncodeVFSPath(s, VDStringSpanW(path), true);
	s += L'!';

	ATEncodeVFSPath(s, VDStringSpanW(fileName), true);

	return s;
}

///////////////////////////////////////////////////////////////////////////

class ATVFSFileViewDirect : public ATVFSFileView {
public:
	ATVFSFileViewDirect(const wchar_t *path, bool write)
		: mFileStream(path, write ? nsVDFile::kCreateAlways | nsVDFile::kWrite | nsVDFile::kDenyAll : nsVDFile::kOpenExisting | nsVDFile::kRead | nsVDFile::kDenyNone)
	{
		mpStream = &mFileStream;
		mFileName = VDFileSplitPath(path);
		mbReadOnly = !write;
	}

private:
	VDFileStream mFileStream;
};

class ATVFSFileViewGZip : public ATVFSFileView {
public:
	ATVFSFileViewGZip(ATVFSFileView *view)
		: mMemoryStream(nullptr, 0)
	{
		mpStream = &mMemoryStream;
		mbReadOnly = false;

		// check if the filename ends in .gz, and if so, strip it off
		mFileName = view->GetFileName();
		auto len = mFileName.size();
		if (len > 3 && !vdwcsicmp(mFileName.c_str() + len - 3, L".gz"))
			mFileName.resize(len - 3);

		auto& srcStream = view->GetStream();
		vdautoptr<VDGUnzipStream> gzs(new VDGUnzipStream(&srcStream, srcStream.Length()));

		uint32 size = 0;
		for(;;) {
			// Don't gunzip beyond 64MB.
			if (size >= 0x10000000)
				throw MyError("Gzip stream is too large (exceeds 256MB in size).");

			uint32 inc = size < 1024 ? 1024 : ((size >> 1) & ~(uint32)15);

			mBuffer.resize(size + inc);

			sint32 actual = gzs->ReadData(mBuffer.data() + size, inc);
			if (actual <= 0) {
				mBuffer.resize(size);
				break;
			}

			size += actual;
		}

		mMemoryStream = VDMemoryStream(mBuffer.data(), size);
	}

private:
	vdfastvector<uint8> mBuffer;
	VDMemoryStream mMemoryStream;
};

class ATVFSFileViewZip : public ATVFSFileView {
public:
	ATVFSFileViewZip(ATVFSFileView *view, const wchar_t *subfile)
		: mMemoryStream(nullptr, 0)
	{
		mpStream = &mMemoryStream;
		mFileName = subfile;
		mbReadOnly = false;

		auto& srcStream = view->GetStream();
		bool found = false;
		VDStringA subfile8(VDTextWToU8(VDStringW(subfile)));

		VDZipArchive ziparch;
		ziparch.Init(&srcStream);
		sint32 n = ziparch.GetFileCount();
		for(sint32 i=0; i<n; ++i) {
			const VDZipArchive::FileInfo& info = ziparch.GetFileInfo(i);
			const VDStringA& name = info.mFileName;

			if (name == subfile8) {
				IVDStream& innerStream = *ziparch.OpenRawStream(i);

				vdautoptr<VDZipStream> zs(new VDZipStream(&innerStream, info.mCompressedSize, !info.mbPacked));

				mBuffer.resize(info.mUncompressedSize);
				zs->Read(mBuffer.data(), info.mUncompressedSize);

				mMemoryStream = VDMemoryStream(mBuffer.data(), info.mUncompressedSize);
				found = true;
				break;
			}
		}

		if (!found)
			throw MyError("Cannot find within zip file: %ls", subfile);
	}

private:
	vdfastvector<uint8> mBuffer;
	VDMemoryStream mMemoryStream;
};

void ATVFSOpenFileView(const wchar_t *vfsPath, bool write, ATVFSFileView **viewOut) {
	VDStringW basePath;
	VDStringW subPath;
	ATVFSProtocol protocol = ATParseVFSPath(vfsPath, basePath, subPath);

	if (!protocol)
		throw ATInvalidVFSPathException(vfsPath);

	vdrefptr<ATVFSFileView> view;

	switch(protocol) {
		case kATVFSProtocol_File:
			view = new ATVFSFileViewDirect(basePath.c_str(), write);
			break;

		case kATVFSProtocol_Zip:
			if (write)
				throw MyError("Cannot open .zip file for write access: %ls", basePath.c_str());

			ATVFSOpenFileView(basePath.c_str(), false, ~view);
			view = new ATVFSFileViewZip(view, subPath.c_str());
			break;

		case kATVFSProtocol_GZip:
			if (write)
				throw MyError("Cannot open .gz file for write access: %ls", basePath.c_str());

			ATVFSOpenFileView(basePath.c_str(), false, ~view);
			view = new ATVFSFileViewGZip(view);
			break;

		case kATVFSProtocol_Atfs:
			if (!g_ATVFSAtfsHandler)
				throw MyError("Inner filesystems are not supported.");

			if (write)
				throw MyError("Cannot open inner filesystem for write access: %ls", basePath.c_str());

			ATVFSOpenFileView(basePath.c_str(), false, ~view);
			{
				vdrefptr<ATVFSFileView> view2;
				g_ATVFSAtfsHandler(view, subPath.c_str(), ~view2);

				view = std::move(view2);
			}
			break;

		default:
			throw ATUnsupportedVFSPathException(vfsPath);
	}

	*viewOut = view.release();
}

void ATVFSSetAtfsProtocolHandler(vdfunction<void(ATVFSFileView *, const wchar_t *, ATVFSFileView **)> handler) {
	g_ATVFSAtfsHandler = std::move(handler);
}
