//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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
#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_vectorview.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/strutil.h>
#include <vd2/system/VDString.h>
#include <at/atcore/address.h>
#include <at/atcore/vfs.h>
#include <algorithm>
#include <ctype.h>
#include <map>
#include "symbols.h"
#include "ksyms.h"

class ATSymbolFileParsingException : public MyError {
public:
	ATSymbolFileParsingException(int line) : MyError("Symbol file parsing failed at line %d.", line) {}
};

class ATSymbolStore : public vdrefcounted<IATCustomSymbolStore> {
public:
	struct SymbolInfo {
		uint32 mOffset;
		const char *mpName;
		uint32 mSize;
	};

	ATSymbolStore();
	~ATSymbolStore();

	void Load(const wchar_t *path);
	void Load(const wchar_t *filename, IVDRandomAccessStream& stream);
	void Save(const wchar_t *path);

	void Init(uint32 moduleBase, uint32 moduleSize);
	void RemoveSymbol(uint32 offset);
	void AddSymbol(uint32 offset, const char *name, uint32 size = 1, uint32 flags = kATSymbol_Read | kATSymbol_Write | kATSymbol_Execute, uint16 fileid = 0, uint16 lineno = 0);
	void AddSymbols(vdvector_view<const SymbolInfo> symbols);
	void AddReadWriteRegisterSymbol(uint32 offset, const char *writename, const char *readname = NULL);
	uint16 AddFileName(const wchar_t *filename);
	void AddSourceLine(uint16 fileId, uint16 line, uint32 moduleOffset, uint32 len = 0);

public:
	uint32	GetDefaultBase() const { return mModuleBase; }
	uint32	GetDefaultSize() const { return mModuleSize; }
	bool	LookupSymbol(uint32 moduleOffset, uint32 flags, ATSymbol& symbol);
	sint32	LookupSymbol(const char *s);
	const wchar_t *GetFileName(uint16 fileid);
	uint16	GetFileId(const wchar_t *fileName, int *matchQuality);
	void	GetLines(uint16 fileId, vdfastvector<ATSourceLineInfo>& lines);
	bool	GetLineForOffset(uint32 moduleOffset, bool searchUp, ATSourceLineInfo& lineInfo);
	bool	GetOffsetForLine(const ATSourceLineInfo& lineInfo, uint32& moduleOffset);
	uint32	GetSymbolCount() const;
	void	GetSymbol(uint32 index, ATSymbolInfo& symbol);
	uint32	GetDirectiveCount() const;
	void	GetDirective(uint32 index, ATSymbolDirectiveInfo& dirInfo);

protected:
	void LoadSymbols(VDTextStream& ifile);
	void LoadCC65Labels(VDTextStream& ifile);
	void LoadCC65DbgFile(VDTextStream& ifile);
	void LoadLabels(VDTextStream& ifile);
	void LoadMADSListing(VDTextStream& ifile);
	void LoadKernelListing(VDTextStream& ifile);

	void CanonicalizeFileName(VDStringW& s);

	struct Symbol {
		uint32	mNameOffset;
		uint32	mOffset;
		uint8	mFlags;
		uint8	m_pad0;
		uint16	mSize;
		uint16	mFileId;
		uint16	mLine;
	};

	struct SymEqPred {
		bool operator()(const Symbol& sym, uint32 offset) const {
			return sym.mOffset == offset;
		}

		bool operator()(uint32 offset, const Symbol& sym) const {
			return offset == sym.mOffset;
		}

		bool operator()(const Symbol& sym1, const Symbol& sym2) const {
			return sym1.mOffset == sym2.mOffset;
		}
	};

	struct SymSort {
		bool operator()(const Symbol& sym, uint32 offset) const {
			return sym.mOffset < offset;
		}

		bool operator()(uint32 offset, const Symbol& sym) const {
			return offset < sym.mOffset;
		}

		bool operator()(const Symbol& sym1, const Symbol& sym2) const {
			return sym1.mOffset < sym2.mOffset;
		}
	};

	struct Directive {
		ATSymbolDirectiveType mType;
		uint32	mOffset;
		size_t	mArgOffset;
	};

	uint32	mModuleBase;
	uint32	mModuleSize;
	bool	mbSymbolsNeedSorting;
	bool	mbGlobalBank0;

	typedef vdfastvector<Symbol> Symbols;
	Symbols					mSymbols;
	vdfastvector<char>		mNameBytes;
	vdfastvector<wchar_t>	mWideNameBytes;
	vdfastvector<uint32>	mFileNameOffsets;

	typedef vdfastvector<Directive> Directives;
	Directives	mDirectives;

	typedef std::map<uint32, std::pair<uint32, uint32> > OffsetToLine;
	typedef std::map<uint32, uint32> LineToOffset;

	OffsetToLine	mOffsetToLine;
	LineToOffset	mLineToOffset;
};

ATSymbolStore::ATSymbolStore()
	: mModuleBase(0)
	, mbSymbolsNeedSorting(false)
{
}

ATSymbolStore::~ATSymbolStore() {
}

void ATSymbolStore::Load(const wchar_t *path) {
	vdrefptr<ATVFSFileView> view;
	ATVFSOpenFileView(path, false, ~view);
	auto& fs = view->GetStream();

	return Load(path, fs);
}

void ATSymbolStore::Load(const wchar_t *filename, IVDRandomAccessStream& stream) {
	{
		VDTextStream ts(&stream);

		const char *line = ts.GetNextLine();

		if (line) {
			if (!strncmp(line, "mads ", 5) || !strncmp(line, "xasm ", 5)) {
				LoadMADSListing(ts);
				return;
			}

			if (!strncmp(line, "Altirra symbol file", 19)) {
				LoadSymbols(ts);
				return;
			}

			if (!strncmp(line, "ca65 ", 5))
				throw MyError("CA65 listings are not supported.");

			if (!strncmp(line, "version\tmajor=2,minor=", 22)) {
				LoadCC65DbgFile(ts);
				return;
			}
		}
	}

	stream.Seek(0);

	VDTextStream ts2(&stream);

	const wchar_t *ext = VDFileSplitExt(filename);
	if (!vdwcsicmp(ext, L".lbl")) {
		LoadCC65Labels(ts2);
		return;
	}

	if (!vdwcsicmp(ext, L".lab")) {
		LoadLabels(ts2);
		return;
	}

	LoadKernelListing(ts2);
}

void ATSymbolStore::Init(uint32 moduleBase, uint32 moduleSize) {
	mModuleBase = moduleBase;
	mModuleSize = moduleSize;
}

void ATSymbolStore::RemoveSymbol(uint32 offset) {
	if (mbSymbolsNeedSorting) {
		std::sort(mSymbols.begin(), mSymbols.end(), SymSort());
		mbSymbolsNeedSorting = false;
	}

	Symbols::iterator it(std::lower_bound(mSymbols.begin(), mSymbols.end(), offset, SymSort()));

	if (it != mSymbols.end() && it->mOffset == offset)
		mSymbols.erase(it);
}

void ATSymbolStore::AddSymbol(uint32 offset, const char *name, uint32 size, uint32 flags, uint16 fileid, uint16 lineno) {
	Symbol sym;

	sym.mNameOffset = (uint32)mNameBytes.size();
	sym.mOffset		= offset - mModuleBase;
	sym.mFlags		= (uint8)flags;
	sym.mSize		= size > 0xFFFF ? 0 : size;
	sym.mFileId		= fileid;
	sym.mLine		= lineno;

	mSymbols.push_back(sym);
	mNameBytes.insert(mNameBytes.end(), name, name + strlen(name) + 1);

	mbSymbolsNeedSorting = true;
}

void ATSymbolStore::AddSymbols(vdvector_view<const SymbolInfo> symbols) {
	for(const SymbolInfo& si : symbols)
		AddSymbol(si.mOffset, si.mpName, si.mSize);
}

void ATSymbolStore::AddReadWriteRegisterSymbol(uint32 offset, const char *writename, const char *readname) {
	if (readname)
		AddSymbol(offset, readname, 1, kATSymbol_Read);

	if (writename)
		AddSymbol(offset, writename, 1, kATSymbol_Write);
}

uint16 ATSymbolStore::AddFileName(const wchar_t *filename) {
	VDStringW tempName(filename);

	CanonicalizeFileName(tempName);

	const wchar_t *fnbase = mWideNameBytes.data();
	size_t n = mFileNameOffsets.size();
	for(size_t i=0; i<n; ++i)
		if (!vdwcsicmp(fnbase + mFileNameOffsets[i], tempName.c_str()))
			return (uint16)(i+1);

	mFileNameOffsets.push_back((uint32)mWideNameBytes.size());

	const wchar_t *pTempName = tempName.c_str();
	mWideNameBytes.insert(mWideNameBytes.end(), pTempName, pTempName + wcslen(pTempName) + 1);
	return (uint16)mFileNameOffsets.size();
}

void ATSymbolStore::AddSourceLine(uint16 fileId, uint16 line, uint32 moduleOffset, uint32 len) {
	uint32 key = (fileId << 16) + line;
	mLineToOffset.insert(LineToOffset::value_type(key, moduleOffset));
	mOffsetToLine.insert(OffsetToLine::value_type(moduleOffset, std::make_pair(key, len)));
}

bool ATSymbolStore::LookupSymbol(uint32 moduleOffset, uint32 flags, ATSymbol& symout) {
	if (mbSymbolsNeedSorting) {
		std::sort(mSymbols.begin(), mSymbols.end(), SymSort());
		mbSymbolsNeedSorting = false;
	}

	Symbols::const_iterator itBegin(mSymbols.begin());
	Symbols::const_iterator it(std::upper_bound(mSymbols.begin(), mSymbols.end(), moduleOffset, SymSort()));

	uint32 moduleOffset2 = moduleOffset;

	if (mbGlobalBank0) {
		moduleOffset2 = UINT32_C(0) - (moduleOffset & 0xFFFF0000);
	}

	while(it != itBegin) {
		--it;
		const Symbol& sym = *it;

		if (sym.mFlags & flags) {
			if (sym.mSize && (moduleOffset - sym.mOffset) >= sym.mSize && (moduleOffset2 - sym.mOffset) >= sym.mSize)
				return false;

			symout.mpName	= mNameBytes.data() + sym.mNameOffset;
			symout.mFlags	= sym.mFlags;
			symout.mOffset	= sym.mOffset;
			symout.mFileId	= sym.mFileId;
			symout.mLine	= sym.mLine;
			return true;
		}
	}

	return false;
}

sint32 ATSymbolStore::LookupSymbol(const char *s) {
	Symbols::const_iterator it(mSymbols.begin()), itEnd(mSymbols.end());
	for(; it != itEnd; ++it) {
		const Symbol& sym = *it;

		if (!_stricmp(s, mNameBytes.data() + sym.mNameOffset))
			return sym.mOffset;
	}

	return -1;
}

const wchar_t *ATSymbolStore::GetFileName(uint16 fileid) {
	if (!fileid)
		return NULL;

	--fileid;
	if (fileid >= mFileNameOffsets.size())
		return NULL;

	return mWideNameBytes.data() + mFileNameOffsets[fileid];
}

uint16 ATSymbolStore::GetFileId(const wchar_t *fileName, int *matchQuality) {
	VDStringW tempName(fileName);

	CanonicalizeFileName(tempName);

	const wchar_t *fullPath = tempName.c_str();
	size_t l1 = wcslen(fullPath);

	size_t n = mFileNameOffsets.size();
	int bestq = 0;
	uint16 bestidx = 0;

	for(size_t i=0; i<n; ++i) {
		const wchar_t *fn = mWideNameBytes.data() + mFileNameOffsets[i];

		size_t l2 = wcslen(fn);
		size_t lm = l1 > l2 ? l2 : l1;

		// check for partial match length
		for(size_t j=1; j<=lm; ++j) {
			if (towlower(fullPath[l1 - j]) != towlower(fn[l2 - j]))
				break;

			if ((j == l1 || fullPath[l1 - j - 1] == L'\\') &&
				(j == l2 || fn[l2 - j - 1] == L'\\'))
			{
				// We factor two things into the quality score, in priority order:
				//
				// 1) How long of a suffix was matched.
				// 2) How short the original path was.
				//
				// #2 is a hack, but makes the debugger prefer foo.s over f1/foo.s
				// and f2/foo.s.

				int q = (int)(j * 10000 - l2);

				if (q > bestq) {
					bestq = q;
					bestidx = (uint16)(i + 1);
				}
			}
		}
	}

	if (matchQuality)
		*matchQuality = bestq;

	return bestidx;
}

void ATSymbolStore::GetLines(uint16 matchFileId, vdfastvector<ATSourceLineInfo>& lines) {
	OffsetToLine::const_iterator it(mOffsetToLine.begin()), itEnd(mOffsetToLine.end());
	for(; it!=itEnd; ++it) {
		uint32 offset = it->first;
		uint32 key = it->second.first;
		uint16 fileId = key >> 16;

		if (fileId == matchFileId) {
			ATSourceLineInfo& linfo = lines.push_back();
			linfo.mOffset = offset;
			linfo.mFileId = matchFileId;
			linfo.mLine = key & 0xffff;
		}
	}
}

bool ATSymbolStore::GetLineForOffset(uint32 moduleOffset, bool searchUp, ATSourceLineInfo& lineInfo) {
	OffsetToLine::const_iterator it(mOffsetToLine.upper_bound(moduleOffset));
	
	if (searchUp) {
		if (it == mOffsetToLine.end())
			return false;
	} else {
		if (it == mOffsetToLine.begin())
			return false;

		--it;
	}

	if (it->second.second && moduleOffset - it->first >= it->second.second)
		return false;

	uint32 key = it->second.first;
	lineInfo.mOffset = it->first;
	lineInfo.mFileId = key >> 16;
	lineInfo.mLine = key & 0xffff;
	return true;
}

bool ATSymbolStore::GetOffsetForLine(const ATSourceLineInfo& lineInfo, uint32& moduleOffset) {
	uint32 key = ((uint32)lineInfo.mFileId << 16) + lineInfo.mLine;

	LineToOffset::const_iterator it(mLineToOffset.find(key));

	if (it == mLineToOffset.end())
		return false;

	moduleOffset = it->second;
	return true;
}

uint32 ATSymbolStore::GetSymbolCount() const {
	return (uint32)mSymbols.size();
}

void ATSymbolStore::GetSymbol(uint32 index, ATSymbolInfo& symbol) {
	const Symbol& sym = mSymbols[index];

	symbol.mpName	= mNameBytes.data() + sym.mNameOffset;
	symbol.mFlags	= sym.mFlags;
	symbol.mOffset	= sym.mOffset;
	symbol.mLength	= sym.mSize;
}

uint32 ATSymbolStore::GetDirectiveCount() const {
	return (uint32)mDirectives.size();
}

void ATSymbolStore::GetDirective(uint32 index, ATSymbolDirectiveInfo& dirInfo) {
	const Directive& dir = mDirectives[index];

	dirInfo.mType = dir.mType;
	dirInfo.mpArguments = mNameBytes.data() + dir.mArgOffset;
	dirInfo.mOffset = dir.mOffset;
}

void ATSymbolStore::LoadSymbols(VDTextStream& ifile) {
	enum {
		kStateNone,
		kStateSymbols
	} state = kStateNone;

	mModuleBase = 0;
	mModuleSize = 0x10000;

	int lineno = 0;
	while(const char *line = ifile.GetNextLine()) {
		++lineno;

		while(*line == ' ' || *line == '\t')
			++line;

		// skip comments
		if (*line == ';')
			continue;

		// skip blank lines
		if (!*line)
			continue;

		// check for group
		if (*line == '[') {
			const char *groupStart = ++line;

			while(*line != ']') {
				if (!*line)
					throw ATSymbolFileParsingException(lineno);
				++line;
			}

			VDStringSpanA groupName(groupStart, line);

			if (groupName == "symbols")
				state = kStateSymbols;
			else
				state = kStateNone;

			continue;
		}

		if (state == kStateSymbols) {
			// rwx address,length name
			uint32 rwxflags = 0;
			for(;;) {
				char c = *line++;

				if (!c)
					throw ATSymbolFileParsingException(lineno);

				if (c == ' ' || c == '\t')
					break;

				if (c == 'r')
					rwxflags |= kATSymbol_Read;
				else if (c == 'w')
					rwxflags |= kATSymbol_Write;
				else if (c == 'x')
					rwxflags |= kATSymbol_Execute;
			}

			if (!rwxflags)
				throw ATSymbolFileParsingException(lineno);

			while(*line == ' ' || *line == '\t')
				++line;

			char *end;
			unsigned long address = strtoul(line, &end, 16);

			if (line == end)
				throw ATSymbolFileParsingException(lineno);

			line = end;

			if (*line++ != ',')
				throw ATSymbolFileParsingException(lineno);

			unsigned long length = strtoul(line, &end, 16);
			if (line == end)
				throw ATSymbolFileParsingException(lineno);

			line = end;

			while(*line == ' ' || *line == '\t')
				++line;

			const char *nameStart = line;

			while(*line != ' ' && *line != '\t' && *line != ';' && *line)
				++line;

			if (line == nameStart)
				throw ATSymbolFileParsingException(lineno);

			const char *nameEnd = line;

			while(*line == ' ' || *line == '\t')
				++line;

			if (*line && *line != ';')
				throw ATSymbolFileParsingException(lineno);

			AddSymbol(address, VDStringA(nameStart, nameEnd).c_str(), length);
		}
	}
}

void ATSymbolStore::LoadCC65Labels(VDTextStream& ifile) {
	VDStringA label;

	while(const char *line = ifile.GetNextLine()) {
		unsigned long addr;
		int nameoffset;
		char namecheck;

		if (2 != sscanf(line, "al %6lx %n%c", &addr, &nameoffset, &namecheck))
			continue;

		if (namecheck == '.')
			++nameoffset;

		const char *labelStart = line + nameoffset;
		const char *labelEnd = labelStart;

		for(;;) {
			char c = *labelEnd;

			if (!c || c == ' ' || c == '\t' || c == '\n' || c== '\r')
				break;

			++labelEnd;
		}

		label.assign(labelStart, labelEnd);
		AddSymbol(addr, label.c_str());
	}

	mModuleBase = 0;
	mModuleSize = 0x10000;
}

namespace {
	struct CC65Span {
		uint32 mSeg;
		uint32 mStart;
		uint32 mSize;
	};

	struct CC65Line {
		uint32 mFile;
		uint32 mLine;
		uint32 mSpan;
	};
}

void ATSymbolStore::LoadCC65DbgFile(VDTextStream& ifile) {
	enum {
		kAttrib_Id,
		kAttrib_Name,
		kAttrib_Size,
		kAttrib_Mtime,
		kAttrib_Mod,
		kAttrib_Start,
		kAttrib_Addrsize,
		kAttrib_Type,
		kAttrib_Oname,
		kAttrib_Ooffs,
		kAttrib_Seg,
		kAttrib_Scope,
		kAttrib_Def,
		kAttrib_Ref,
		kAttrib_Val,
		kAttrib_File,
		kAttrib_Line,
		kAttrib_Span,

		kAttribCount
	};

	static const char *const kAttribNames[]={
		"id",
		"name",
		"size",
		"mtime",
		"mod",
		"start",
		"addrsize",
		"type",
		"oname",
		"ooffs",
		"seg",
		"scope",
		"def",
		"ref",
		"val",
		"file",
		"line",
		"span",
	};

	VDASSERTCT(vdcountof(kAttribNames) == kAttribCount);

	typedef vdhashmap<uint32, uint32> Segs;
	Segs segs;

	typedef vdhashmap<uint32, uint16> Files;
	Files files;

	typedef vdhashmap<uint32, CC65Span> CC65Spans;
	CC65Spans cc65spans;

	typedef vdfastvector<CC65Line> CC65Lines;
	CC65Lines cc65lines;

	VDStringSpanA attribs[kAttribCount];

	mModuleBase = 0;
	mModuleSize = 0x10000;

	while(const char *line = ifile.GetNextLine()) {
		VDStringRefA r(line);
		VDStringRefA token;

		if (!r.split('\t', token))
			continue;

		// parse out attributes
		uint32 attribMask = 0;

		while(!r.empty()) {
			VDStringRefA attrToken;
			if (!r.split('=', attrToken))
				break;

			int attr = -1;

			for(int i=0; i<(int)sizeof(kAttribNames)/sizeof(kAttribNames[0]); ++i) {
				if (attrToken == kAttribNames[i]) {
					attr = i;
					break;
				}
			}

			if (!r.empty() && r.front() == '"') {
				r.split('"', attrToken);
				r.split('"', attrToken);
				if (!r.empty() && r.front() == ',') {
					VDStringRefA dummyToken;
					r.split(',', dummyToken);
				}
			} else {
				if (!r.split(',', attrToken))
					attrToken = r;
			}

			if (attr >= 0) {
				attribs[attr] = attrToken;
				attribMask |= (1 << attr);
			}
		}

		if (token == "file") {
			// file id=0,name="hello.s",size=1682,mtime=0x532BC30D,mod=0
			if (~attribMask & ((1 << kAttrib_Id) | (1 << kAttrib_Name)))
				continue;

			unsigned id;
			char dummy;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Id]).c_str(), "%u%c", &id, &dummy))
				continue;

			Files::insert_return_type result = files.insert(id);

			if (result.second)
				result.first->second = AddFileName(VDTextAToW(attribs[kAttrib_Name]).c_str());
		} else if (token == "line") {
			// line id=26,file=0,line=59,span=15
			if (~attribMask & ((1 << kAttrib_Id) | (1 << kAttrib_File) | (1 << kAttrib_Line) | (1 << kAttrib_Span) | (1 << kAttrib_Type)))
				continue;

			unsigned id;
			char dummy;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Id]).c_str(), "%u%c", &id, &dummy))
				continue;

			unsigned file;
			if (1 != sscanf(VDStringA(attribs[kAttrib_File]).c_str(), "%u%c", &file, &dummy))
				continue;

			unsigned lineno;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Line]).c_str(), "%u%c", &lineno, &dummy))
				continue;

			unsigned type;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Type]).c_str(), "%u%c", &type, &dummy))
				continue;

			if (type != 1)
				continue;

			// span can have a + delimited list, which we must parse out; we produce one entry per span
			VDStringRefA spanList(attribs[kAttrib_Span]);

			while(!spanList.empty()) {
				VDStringRefA spanToken;

				if (!spanList.split('+', spanToken)) {
					spanToken = spanList;
					spanList.clear();
				}

				unsigned span;
				if (1 == sscanf(VDStringA(spanToken).c_str(), "%u%c", &span, &dummy)) {
					CC65Line cc65line;
					cc65line.mFile = file;
					cc65line.mLine = lineno;
					cc65line.mSpan = span;

					cc65lines.push_back(cc65line);
				}
			}
		} else if (token == "seg") {
			// seg id=0,name="CODE",start=0x002092,size=0x0863,addrsize=absolute,type=ro,oname="hello.xex",ooffs=407
			if (~attribMask & ((1 << kAttrib_Id) | (1 << kAttrib_Start) | (1 << kAttrib_Addrsize)))
				continue;

			if (attribs[kAttrib_Addrsize] != "absolute")
				continue;

			unsigned id;
			char dummy;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Id]).c_str(), "%u%c", &id, &dummy))
				continue;

			unsigned start;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Start]).c_str(), "%i%c", &start, &dummy))
				continue;

			segs[id] = start;
		} else if (token == "span") {
			// span id=0,seg=3,start=0,size=2,type=1
			if (~attribMask & ((1 << kAttrib_Id) | (1 << kAttrib_Seg) | (1 << kAttrib_Start) | (1 << kAttrib_Size)))
				continue;

			unsigned id;
			char dummy;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Id]).c_str(), "%u%c", &id, &dummy))
				continue;

			unsigned seg;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Seg]).c_str(), "%u%c", &seg, &dummy))
				continue;

			unsigned start;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Start]).c_str(), "%u%c", &start, &dummy))
				continue;

			unsigned size;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Size]).c_str(), "%u%c", &size, &dummy))
				continue;

			CC65Span span;
			span.mSeg = seg;
			span.mStart = start;
			span.mSize = size;

			cc65spans[id] = span;
		} else if (token == "sym") {
			// sym id=0,name="L0002",addrsize=absolute,size=1,scope=1,def=52+50,ref=16,val=0x20DD,seg=0,type=lab
			if (~attribMask & ((1 << kAttrib_Id) | (1 << kAttrib_Name) | (1 << kAttrib_Addrsize) | (1 << kAttrib_Val)))
				continue;

			if (attribs[kAttrib_Addrsize] != "absolute")
				continue;

			unsigned val;
			char dummy;
			if (1 != sscanf(VDStringA(attribs[kAttrib_Val]).c_str(), "%i%c", &val, &dummy))
				continue;

			unsigned size = 1;
			if (attribMask & (1 << kAttrib_Size)) {
				if (1 != sscanf(VDStringA(attribs[kAttrib_Size]).c_str(), "%u%c", &size, &dummy))
					continue;
			}

			AddSymbol(val, VDStringA(attribs[kAttrib_Name]).c_str(), size);
		}
	}

	// process line number information
	for(CC65Lines::const_iterator it(cc65lines.begin()), itEnd(cc65lines.end());
		it != itEnd;
		++it)
	{
		const CC65Line& cline = *it;

		// Okay, we need to do the following lookups:
		//	line -> file
		//	     -> span -> seg
		Files::const_iterator itFile = files.find(cline.mFile);
		if (itFile == files.end())
			continue;

		CC65Spans::const_iterator itSpan = cc65spans.find(cline.mSpan);
		if (itSpan == cc65spans.end())
			continue;

		Segs::const_iterator itSeg = segs.find(itSpan->second.mSeg);
		if (itSeg == segs.end())
			continue;

		const uint32 addr = itSeg->second + itSpan->second.mStart;
		const uint16 fileId = itFile->second;

		AddSourceLine(fileId, cline.mLine, addr, itSpan->second.mSize);
	}
}

void ATSymbolStore::LoadLabels(VDTextStream& ifile) {
	VDStringA label;

	while(const char *line = ifile.GetNextLine()) {
		unsigned long addr;
		int nameoffset;
		char namecheck;

		if (2 != sscanf(line, "%6lx %n%c", &addr, &nameoffset, &namecheck))
			continue;

		const char *labelStart = line + nameoffset;
		const char *labelEnd = labelStart;

		for(;;) {
			char c = *labelEnd;

			if (!c || c == ' ' || c == '\t' || c == '\n' || c== '\r')
				break;

			++labelEnd;
		}

		label.assign(labelStart, labelEnd);
		AddSymbol(addr, label.c_str());
	}

	mModuleBase = 0;
	mModuleSize = 0x10000;	
}

namespace {
	struct FileEntry {
		uint16 mFileId;
		int mNextLine;
		int mForcedLine;
	};
}

void ATSymbolStore::LoadMADSListing(VDTextStream& ifile) {
	uint16 fileid = 0;

	enum {
		kModeNone,
		kModeSource,
		kModeLabels
	} mode = kModeNone;

	VDStringA label;

	typedef vdfastvector<FileEntry> FileStack;
	FileStack fileStack;
	int nextline = 1;
	bool macroMode = false;
	int forcedLine = -1;
	bool directivePending = false;

	uint32 bankOffsetMap[256] {};
	bool extBanksUsed = false;

	while(const char *line = ifile.GetNextLine()) {
		char space0;
		int origline;
		int address;
		int address2;
		unsigned bank;
		char dummy;
		char space1;
		char space2;
		char space3;
		char space4;
		int op;

		if (!strncmp(line, "Macro: ", 7)) {
			macroMode = true;
		} else if (!strncmp(line, "Source: ", 8)) {
			if (macroMode)
				macroMode = false;
			else {
				if (fileid) {
					FileEntry& fe = fileStack.push_back();
					fe.mNextLine = nextline;
					fe.mFileId = fileid;
					fe.mForcedLine = forcedLine;
				}

				fileid = AddFileName(VDTextAToW(line+8).c_str());
				forcedLine = -1;
				mode = kModeSource;
				nextline = 1;
			}

			continue;
		} else if (!strncmp(line, "Label table:", 12)) {
			fileid = 0;
			mode = kModeLabels;
		}

		if (mode == kModeSource) {
			if (macroMode)
				continue;

			bool valid = false;
			int afterline = 0;

			if (2 == sscanf(line, "%c%5d%n", &space0, &origline, &afterline)
				&& space0 == ' ')
			{
				if (fileid && origline > 0) {
					// check for discontinuous line (mads doesn't re-emit the parent line)
					if (origline != nextline) {
						FileStack::const_reverse_iterator it(fileStack.rbegin()), itEnd(fileStack.rend());

						for(; it != itEnd; ++it) {
							const FileEntry& fe = *it;
							if (fe.mNextLine == origline && fe.mFileId != fileid) {
								fileid = fe.mFileId;
								forcedLine = fe.mForcedLine;
								break;
							}
						}
					}

					nextline = origline + 1;
				}

				// 105 1088 8D ...
				//  12 02,ACBB A2 FF...
				// 131 2000-201F> 00 ...
				//   4 FFFF> 2000-2006> EA              nop
				if (7 == sscanf(line, "%c%5d%c%4x%c%2x%c", &space0, &origline, &space1, &address, &space2, &op, &space3)
					&& space0 == ' '
					&& space1 == ' '
					&& space2 == ' '
					&& (space3 == ' ' || space3 == '\t'))
				{
					valid = true;
				} else if (8 == sscanf(line, "%c%5d%c%2x,%4x%c%2x%c", &space0, &origline, &space1, &bank, &address, &space2, &op, &space3)
					&& space0 == ' '
					&& space1 == ' '
					&& space2 == ' '
					&& (space3 == ' ' || space3 == '\t'))
				{
					valid = true;
					address += bank << 16;
				} else if (8 == sscanf(line, "%c%5d%c%4x-%4x>%c%2x%c", &space0, &origline, &space1, &address, &address2, &space2, &op, &space3)
					&& space0 == ' '
					&& space1 == ' '
					&& space2 == ' '
					&& (space3 == ' ' || space3 == '\t'))
				{
					valid = true;
				} else if (9 == sscanf(line, "%c%5d%cFFFF>%c%4x-%4x>%c%2x%c", &space0, &origline, &space1, &space2, &address, &address2, &space3, &op, &space4)
					&& space0 == ' '
					&& space1 == ' '
					&& space2 == ' '
					&& space3 == ' '
					&& (space4 == ' ' || space4 == '\t'))
				{
					valid = true;
				} else {
					// Look for a comment line.
					const char *s = line + afterline;

					while(*s == ' ' || *s == '\t')
						++s;

					if (*s == ';') {
						// We have a comment. Check if it has one of these special syntaxes:
						// ; ### file(num)    [line number directive]
						++s;

						while(*s == ' ' || *s == '\t')
							++s;

						if (s[0] == '#' && s[1] == '#') {
							s += 2;

							if (*s == '#') {
								// ;### file(line) ...     [line number directive]
								++s;
								while(*s == ' ' || *s == '\t')
									++s;

								const char *fnstart = s;
								const char *fnend;
								if (*s == '"') {
									++s;
									fnstart = s;

									while(*s && *s != '"')
										++s;

									fnend = s;

									if (*s)
										++s;
								} else {
									while(*s && *s != ' ' && *s != '(')
										++s;

									fnend = s;
								}

								while(*s == ' ' || *s == '\t')
									++s;

								char term = 0;
								unsigned newlineno;
								if (2 == sscanf(s, "(%u %c", &newlineno, &term) && term == ')') {
									fileid = AddFileName(VDTextAToW(fnstart, (int)(fnend - fnstart)).c_str());
									forcedLine = newlineno;
								}
							} else if (!strncmp(s, "ASSERT", 6) && (s[6] == ' ' || s[6] == '\t')) {
								// ;##ASSERT <address> <condition>
								s += 7;

								while(*s == ' ' || *s == '\t')
									++s;

								VDStringSpanA arg(s);
								arg.trim(" \t");

								if (!arg.empty()) {
									Directive& dir = mDirectives.push_back();
									dir.mOffset = 0;
									dir.mType = kATSymbolDirType_Assert;
									dir.mArgOffset = mNameBytes.size();

									mNameBytes.insert(mNameBytes.end(), arg.begin(), arg.end());
									mNameBytes.push_back(0);

									directivePending = true;
								}
							} else if (!strncmp(s, "TRACE", 5) && (s[5] == ' ' || s[5] == '\t')) {
								// ;##TRACE <printf arguments>
								s += 6;

								while(*s == ' ' || *s == '\t')
									++s;

								VDStringSpanA arg(s);
								arg.trim(" \t");

								if (!arg.empty()) {
									Directive& dir = mDirectives.push_back();
									dir.mOffset = 0;
									dir.mType = kATSymbolDirType_Trace;
									dir.mArgOffset = mNameBytes.size();

									mNameBytes.insert(mNameBytes.end(), arg.begin(), arg.end());
									mNameBytes.push_back(0);

									directivePending = true;
								}
							} else if (!strncmp(s, "BANK", 4) && (s[4] == ' ' || s[4] == '\t')) {
								s += 5;

								while(*s == ' ' || *s == '\t')
									++s;

								const char *bankStart = s;
								unsigned bank = 0;
								if (*s == '$') {
									char *t = const_cast<char *>(s);
									bank = strtoul(s + 1, &t, 16);
									s = t;
								} else {
									char *t = const_cast<char *>(s);
									bank = strtoul(s + 1, &t, 10);
									s = t;
								}

								if (bank < 256) {
									while(*s == ' ' || *s == '\t')
										++s;

									const char *typeNameStart = s;
									while(*s && *s != ' ' && *s != '\t')
										++s;

									const VDStringSpanA arg(typeNameStart, s);

									while(*s == ' ' || *s == '\t')
										++s;

									if (arg.comparei("default") == 0) {
										bankOffsetMap[bank] = 0 - (bank << 16);
										extBanksUsed = true;
									} else if (arg.comparei("ram") == 0) {
										bankOffsetMap[bank] = 0 - (bank << 16) + kATAddressSpace_RAM;
										extBanksUsed = true;
									} else if (arg.comparei("cart") == 0) {
										unsigned cartBank = 0;
										bool cartBankValid = false;
										char dummy;

										if (*s == '$') {
											cartBankValid = (1 == sscanf(s + 1, "%x%c", &cartBank, &dummy));
										} else {
											cartBankValid = (1 == sscanf(s, "%u%c", &cartBank, &dummy));
										}

										if (cartBankValid && cartBank < 256) {
											bankOffsetMap[bank] = 0 - (bank << 16) + (cartBank << 16) + kATAddressSpace_CB;
											extBanksUsed = true;
										}
									}
								}
							}
						}
					}
				}
			} else if (8 == sscanf(line, "%6x%c%c%c%c%c%2x%c", &address, &space0, &space1, &dummy, &space2, &space3, &op, &space4)
				&& space0 == ' '
				&& space1 == ' '
				&& space2 == ' '
				&& space3 == ' '
				&& space4 == ' '
				&& isdigit((unsigned char)dummy))
			{
				valid = true;
			} else if (6 == sscanf(line, "%6d%c%4x%c%2x%c", &origline, &space0, &address, &space1, &op, &space2)
				&& space0 == ' '
				&& space1 == ' '
				&& (space2 == ' ' || space2 == '\t'))
			{
				valid = true;
			}

			if (valid) {
				if (directivePending) {
					for(Directives::reverse_iterator it(mDirectives.rbegin()), itEnd(mDirectives.rend()); it != itEnd; ++it) {
						Directive& d = *it;

						if (d.mOffset)
							break;

						d.mOffset = address;
					}

					directivePending = false;
				}

				if (forcedLine >= 0) {
					AddSourceLine(fileid, forcedLine, address);
					forcedLine = -2;
				} else if (forcedLine == -1)
					AddSourceLine(fileid, origline, address);
			}
		} else if (mode == kModeLabels) {
			// MADS:
			// 00      11A3    DLI
			//
			// xasm:
			//         2000 MAIN

			if (isxdigit((unsigned char)line[0])) {
				int pos1 = -1;
				int pos2 = -1;
				unsigned bank;
				if (2 == sscanf(line, "%2x %4x %n%*s%n", &bank, &address, &pos1, &pos2) && pos1 >= 0 && pos2 > pos1) {
					label.assign(line + pos1, line + pos2);

					char end;
					char dummy;
					unsigned srcBank;
					if (2 == sscanf(label.c_str(), "__ATBANK_%02X_RA%c%c", &srcBank, &end, &dummy) && end == 'M' && srcBank < 256 && address < 256) {
						extBanksUsed = true;
						bankOffsetMap[srcBank] = kATAddressSpace_RAM - (srcBank << 16);
					} else if (2 == sscanf(label.c_str(), "__ATBANK_%02X_CAR%c%c", &srcBank, &end, &dummy) && end == 'T' && srcBank < 256 && address < 256) {
						extBanksUsed = true;
						bankOffsetMap[srcBank] = kATAddressSpace_CB - (srcBank << 16) + (address << 16);
					} else if (2 == sscanf(label.c_str(), "__ATBANK_%02X_SHARE%c%c", &srcBank, &end, &dummy) && end == 'D' && srcBank < 256 && address < 256) {
						extBanksUsed = true;
						bankOffsetMap[srcBank] = kATAddressSpace_CB - (srcBank << 16) + 0x1000000;
					} else if (label == "__ATBANK_00_GLOBAL") {
						mbGlobalBank0 = true;
					} else {
						AddSymbol(((uint32)bank << 16) + address, label.c_str());
					}
				}
			} else {
				int pos1 = -1;
				int pos2 = -1;
				if (1 == sscanf(line, "%4x %n%*s%n", &address, &pos1, &pos2) && pos1 >= 0 && pos2 >= pos1) {
					label.assign(line + pos1, line + pos2);

					AddSymbol(address, label.c_str());
				}
			}
		}
	}

	mModuleBase = 0;
	mModuleSize = 0x1000000;

	if (extBanksUsed) {
		mModuleSize = 0xFFFFFFFF;

		for(Symbol& sym : mSymbols) {
			uint32 symBankOffset = bankOffsetMap[(sym.mOffset >> 16) & 0xFF];

			if (symBankOffset) {
				sym.mOffset += symBankOffset;

				if ((sym.mOffset & kATAddressSpaceMask) == kATAddressSpace_CB && (uint16)(sym.mOffset - 0xA000) >= 0x2000)
					sym.mOffset &= 0xFFFF;
			}
		}

		for(Directive& directive : mDirectives) {
			directive.mOffset += bankOffsetMap[(directive.mOffset >> 16) & 0xFF];
		}

		mbSymbolsNeedSorting = true;
	}

	// remove useless directives
	while(!mDirectives.empty() && mDirectives.back().mOffset == 0)
		mDirectives.pop_back();
}

void ATSymbolStore::LoadKernelListing(VDTextStream& ifile) {
	// hardcoded for now for the kernel
	Init(0xD800, 0x2800);

	while(const char *line = ifile.GetNextLine()) {
		int len = (int)strlen(line);
		if (len < 33)
			continue;

		// What we're looking for:
		//    3587  F138  A9 00            ZERORM

		const char *s = line;
		const char *t;

		if (*s++ != ' ') continue;
		if (*s++ != ' ') continue;
		if (*s++ != ' ') continue;

		// skip line number
		while(*s == ' ')
			++s;
		if (!isdigit((unsigned char)*s++)) continue;
		while(isdigit((unsigned char)*s))
			++s;

		if (*s++ != ' ') continue;
		if (*s++ != ' ') continue;

		// read address
		uint32 address = 0;
		for(int i=0; i<4; ++i) {
			char c = *s;
			if (!isxdigit((unsigned char)c))
				goto fail;

			++s;
			c = toupper(c);
			if (c >= 'A')
				c -= 7;

			address = (address << 4) + (c - '0');
		}

		// skip two more spaces
		if (*s++ != ' ') continue;
		if (*s++ != ' ') continue;

		// check for first opcode byte
		if (!isxdigit((unsigned char)*s++)) continue;
		if (!isxdigit((unsigned char)*s++)) continue;

		// skip all the way to label
		s = line + 33;
		t = s;
		while(isalpha((unsigned char)*t))
			++t;

		if (t != s) {
			AddSymbol(address, VDStringA(s, (uint32)(t-s)).c_str());
		}

fail:
		;
	}
}

void ATSymbolStore::CanonicalizeFileName(VDStringW& s) {
	VDStringW::size_type pos = 0;
	while((pos = s.find(L'/', pos)) != VDStringW::npos) {
		s[pos] = L'\\';
		++pos;
	}

	// strip duplicate backslashes, except for front (may be UNC path)
	pos = 0;
	while(pos < s.size() && s[pos] == L'\\')
		++pos;

	++pos;
	while(pos < s.size()) {
		if (s[pos - 1] == L'\\' && s[pos] == L'\\')
			s.erase(pos);
		else
			++pos;
	}
}

///////////////////////////////////////////////////////////////////////////////

bool ATCreateDefaultVariableSymbolStore(IATSymbolStore **ppStore) {
	vdrefptr<ATSymbolStore> symstore(new ATSymbolStore);

	symstore->Init(0x0000, 0x0400);

	using namespace ATKernelSymbols;

	static constexpr ATSymbolStore::SymbolInfo kSymbols[] = {
		{ CASINI, "CASINI", 2 },
		{ RAMLO , "RAMLO" , 2 },
		{ TRAMSZ, "TRAMSZ", 1 },
		{ WARMST, "WARMST", 1 },
		{ DOSVEC, "DOSVEC", 2 },
		{ DOSINI, "DOSINI", 2 },
		{ APPMHI, "APPMHI", 2 },
		{ POKMSK, "POKMSK", 1 },
		{ BRKKEY, "BRKKEY", 1 },
		{ RTCLOK, "RTCLOK", 3 },
		{ BUFADR, "BUFADR", 2 },
		{ ICHIDZ, "ICHIDZ", 1 },
		{ ICDNOZ, "ICDNOZ", 1 },
		{ ICCOMZ, "ICCOMZ", 1 },
		{ ICSTAZ, "ICSTAZ", 1 },
		{ ICBALZ, "ICBALZ", 1 },
		{ ICBAHZ, "ICBAHZ", 1 },
		{ ICBLLZ, "ICBLLZ", 1 },
		{ ICBLHZ, "ICBLHZ", 1 },
		{ ICAX1Z, "ICAX1Z", 1 },
		{ ICAX2Z, "ICAX2Z", 1 },
		{ ICAX3Z, "ICAX3Z", 1 },
		{ ICAX4Z, "ICAX4Z", 1 },
		{ ICAX5Z, "ICAX5Z", 1 },
		{ STATUS, "STATUS", 1 },
		{ CHKSUM, "CHKSUM", 1 },
		{ BUFRLO, "BUFRLO", 1 },
		{ BUFRHI, "BUFRHI", 1 },
		{ BFENLO, "BFENLO", 1 },
		{ BFENHI, "BFENHI", 1 },
		{ BUFRFL, "BUFRFL", 1 },
		{ RECVDN, "RECVDN", 1 },
		{ CHKSNT, "CHKSNT", 1 },
		{ SOUNDR, "SOUNDR", 1 },
		{ CRITIC, "CRITIC", 1 },
		{ CKEY,   "CKEY"  , 1 },
		{ CASSBT, "CASSBT", 1 },
		{ ATRACT, "ATRACT", 1 },
		{ DRKMSK, "DRKMSK", 1 },
		{ COLRSH, "COLRSH", 1 },
		{ HOLD1 , "HOLD1" , 1 },
		{ LMARGN, "LMARGN", 1 },
		{ RMARGN, "RMARGN", 1 },
		{ ROWCRS, "ROWCRS", 1 },
		{ COLCRS, "COLCRS", 2 },
		{ OLDROW, "OLDROW", 1 },
		{ OLDCOL, "OLDCOL", 2 },
		{ OLDCHR, "OLDCHR", 1 },
		{ DINDEX, "DINDEX", 1 },
		{ SAVMSC, "SAVMSC", 2 },
		{ OLDADR, "OLDADR", 2 },
		{ PALNTS, "PALNTS", 1 },
		{ LOGCOL, "LOGCOL", 1 },
		{ ADRESS, "ADRESS", 2 },
		{ TOADR , "TOADR" , 2 },
		{ RAMTOP, "RAMTOP", 1 },
		{ BUFCNT, "BUFCNT", 1 },
		{ BUFSTR, "BUFSTR", 2 },
		{ BITMSK, "BITMSK", 1 },
		{ DELTAR, "DELTAR", 1 },
		{ DELTAC, "DELTAC", 2 },
		{ ROWINC, "ROWINC", 1 },
		{ COLINC, "COLINC", 1 },
		{ KEYDEF, "KEYDEF", 2 },	// XL/XE
		{ SWPFLG, "SWPFLG", 1 },
		{ COUNTR, "COUNTR", 2 },

		{ FR0, "FR0", 1 },
		{ FR1, "FR1", 1 },
		{ CIX, "CIX", 1 },

		{ INBUFF, "INBUFF", 1 },
		{ FLPTR, "FLPTR", 1 },

		{ VDSLST, "VDSLST", 2 },
		{ VPRCED, "VPRCED", 2 },
		{ VINTER, "VINTER", 2 },
		{ VBREAK, "VBREAK", 2 },
		{ VKEYBD, "VKEYBD", 2 },
		{ VSERIN, "VSERIN", 2 },
		{ VSEROR, "VSEROR", 2 },
		{ VSEROC, "VSEROC", 2 },
		{ VTIMR1, "VTIMR1", 2 },
		{ VTIMR2, "VTIMR2", 2 },
		{ VTIMR4, "VTIMR4", 2 },
		{ VIMIRQ, "VIMIRQ", 2 },
		{ CDTMV1, "CDTMV1", 2 },
		{ CDTMV2, "CDTMV2", 2 },
		{ CDTMV3, "CDTMV3", 2 },
		{ CDTMV4, "CDTMV4", 2 },
		{ CDTMV5, "CDTMV5", 2 },
		{ VVBLKI, "VVBLKI", 2 },
		{ VVBLKD, "VVBLKD", 2 },
		{ CDTMA1, "CDTMA1", 1 },
		{ CDTMA2, "CDTMA2", 1 },
		{ CDTMF3, "CDTMF3", 1 },
		{ CDTMF4, "CDTMF4", 1 },
		{ CDTMF5, "CDTMF5", 1 },
		{ SDMCTL, "SDMCTL", 1 },
		{ SDLSTL, "SDLSTL", 1 },
		{ SDLSTH, "SDLSTH", 1 },
		{ SSKCTL, "SSKCTL", 1 },
		{ LPENH , "LPENH" , 1 },
		{ LPENV , "LPENV" , 1 },
		{ BRKKY , "BRKKY" , 2 },
		{ VPIRQ , "VPIRQ" , 2 },	// XL/XE
		{ COLDST, "COLDST", 1 },
		{ PDVMSK, "PDVMSK", 1 },
		{ SHPDVS, "SHPDVS", 1 },
		{ PDMSK , "PDMSK" , 1 },	// XL/XE
		{ CHSALT, "CHSALT", 1 },	// XL/XE
		{ GPRIOR, "GPRIOR", 1 },
		{ PADDL0, "PADDL0", 1 },
		{ PADDL1, "PADDL1", 1 },
		{ PADDL2, "PADDL2", 1 },
		{ PADDL3, "PADDL3", 1 },
		{ PADDL4, "PADDL4", 1 },
		{ PADDL5, "PADDL5", 1 },
		{ PADDL6, "PADDL6", 1 },
		{ PADDL7, "PADDL7", 1 },
		{ STICK0, "STICK0", 1 },
		{ STICK1, "STICK1", 1 },
		{ STICK2, "STICK2", 1 },
		{ STICK3, "STICK3", 1 },
		{ PTRIG0, "PTRIG0", 1 },
		{ PTRIG1, "PTRIG1", 1 },
		{ PTRIG2, "PTRIG2", 1 },
		{ PTRIG3, "PTRIG3", 1 },
		{ PTRIG4, "PTRIG4", 1 },
		{ PTRIG5, "PTRIG5", 1 },
		{ PTRIG6, "PTRIG6", 1 },
		{ PTRIG7, "PTRIG7", 1 },
		{ STRIG0, "STRIG0", 1 },
		{ STRIG1, "STRIG1", 1 },
		{ STRIG2, "STRIG2", 1 },
		{ STRIG3, "STRIG3", 1 },
		{ JVECK , "JVECK" , 1 },
		{ TXTROW, "TXTROW", 1 },
		{ TXTCOL, "TXTCOL", 2 },
		{ TINDEX, "TINDEX", 1 },
		{ TXTMSC, "TXTMSC", 2 },
		{ TXTOLD, "TXTOLD", 2 },
		{ HOLD2 , "HOLD2" , 1 },
		{ DMASK , "DMASK" , 1 },
		{ ESCFLG, "ESCFLG", 1 },
		{ TABMAP, "TABMAP", 15 },
		{ LOGMAP, "LOGMAP", 4 },
		{ SHFLOK, "SHFLOK", 1 },
		{ BOTSCR, "BOTSCR", 1 },
		{ PCOLR0, "PCOLR0", 1 },
		{ PCOLR1, "PCOLR1", 1 },
		{ PCOLR2, "PCOLR2", 1 },
		{ PCOLR3, "PCOLR3", 1 },
		{ COLOR0, "COLOR0", 1 },
		{ COLOR1, "COLOR1", 1 },
		{ COLOR2, "COLOR2", 1 },
		{ COLOR3, "COLOR3", 1 },
		{ COLOR4, "COLOR4", 1 },
		{ DSCTLN, "DSCTLN", 1 },	// XL/XE
		{ KRPDEL, "KRPDEL", 1 },	// XL/XE
		{ KEYREP, "KEYREP", 1 },	// XL/XE
		{ NOCLIK, "NOCLIK", 1 },	// XL/XE
		{ HELPFG, "HELPFG", 1 },	// XL/XE
		{ DMASAV, "DMASAV", 1 },	// XL/XE
		{ RUNAD , "RUNAD" , 2 },
		{ INITAD, "INITAD", 2 },
		{ MEMTOP, "MEMTOP", 2 },
		{ MEMLO , "MEMLO" , 2 },
		{ DVSTAT, "DVSTAT", 4 },
		{ CRSINH, "CRSINH", 1 },
		{ KEYDEL, "KEYDEL", 1 },
		{ CH1   , "CH1"   , 1 },
		{ CHACT , "CHACT" , 1 },
		{ CHBAS , "CHBAS" , 1 },
		{ ATACHR, "ATACHR", 1 },
		{ CH    , "CH"    , 1 },
		{ FILDAT, "FILDAT", 1 },
		{ DSPFLG, "DSPFLG", 1 },
		{ DDEVIC, "DDEVIC", 1 },
		{ DUNIT , "DUNIT" , 1 },
		{ DCOMND, "DCOMND", 1 },
		{ DSTATS, "DSTATS", 1 },
		{ DBUFLO, "DBUFLO", 1 },
		{ DBUFHI, "DBUFHI", 1 },
		{ DTIMLO, "DTIMLO", 1 },
		{ DBYTLO, "DBYTLO", 1 },
		{ DBYTHI, "DBYTHI", 1 },
		{ DAUX1 , "DAUX1" , 1 },
		{ DAUX2 , "DAUX2" , 1 },
		{ TIMER1, "TIMER1", 2 },
		{ TIMER2, "TIMER2", 2 },
		{ TIMFLG, "TIMFLG", 1 },
		{ STACKP, "STACKP", 1 },
		{ HATABS, "HATABS", 38 },
		{ ICHID , "ICHID" , 1 },
		{ ICDNO , "ICDNO" , 1 },
		{ ICCMD , "ICCMD" , 1 },
		{ ICSTA , "ICSTA" , 1 },
		{ ICBAL , "ICBAL" , 1 },
		{ ICBAH , "ICBAH" , 1 },
		{ ICPTL , "ICPTL" , 1 },
		{ ICPTH , "ICPTH" , 1 },
		{ ICBLL , "ICBLL" , 1 },
		{ ICBLH , "ICBLH" , 1 },
		{ ICAX1 , "ICAX1" , 1 },
		{ ICAX2 , "ICAX2" , 1 },
		{ ICAX3 , "ICAX3" , 1 },
		{ ICAX4 , "ICAX4" , 1 },
		{ ICAX5 , "ICAX5" , 1 },
		{ ICAX6 , "ICAX6" , 1 },
		{ BASICF, "BASICF", 1 },
		{ GINTLK, "GINTLK", 1 },
		{ CASBUF, "CASBUF", 131 },
		{ LBUFF , "LBUFF" , 128 },
	};

	symstore->AddSymbols(kSymbols);

	*ppStore = symstore.release();
	return true;
}

bool ATCreateDefaultVariableSymbolStore5200(IATSymbolStore **ppStore) {
	vdrefptr<ATSymbolStore> symstore(new ATSymbolStore);

	symstore->Init(0x0000, 0x0400);

	using namespace ATKernelSymbols5200;

	static constexpr ATSymbolStore::SymbolInfo kSymbols[] = {
		{ POKMSK, "POKMSK", 1 },
		{ RTCLOK, "RTCLOK", 1 },
		{ CRITIC, "CRITIC", 1 },
		{ ATRACT, "ATRACT", 1 },
		{ SDMCTL, "SDMCTL", 1 },
		{ SDLSTL, "SDLSTL", 1 },
		{ SDLSTH, "SDLSTH", 1 },
		{ PCOLR0, "PCOLR0", 1 },
		{ PCOLR1, "PCOLR1", 1 },
		{ PCOLR2, "PCOLR2", 1 },
		{ PCOLR3, "PCOLR3", 1 },
		{ COLOR0, "COLOR0", 1 },
		{ COLOR1, "COLOR1", 1 },
		{ COLOR2, "COLOR2", 1 },
		{ COLOR3, "COLOR3", 1 },
		{ COLOR4, "COLOR4", 1 },

		{ VIMIRQ, "VIMIRQ", 2 },
		{ VVBLKI, "VVBLKI", 2 },
		{ VVBLKD, "VVBLKD", 2 },
		{ VDSLST, "VDSLST", 2 },
		{ VTRIGR, "VTRIGR", 2 },
		{ VBRKOP, "VBRKOP", 2 },
		{ VKYBDI, "VKYBDI", 2 },
		{ VKYBDF, "VKYBDF", 2 },
		{ VSERIN, "VSERIN", 2 },
		{ VSEROR, "VSEROR", 2 },
		{ VSEROC, "VSEROC", 2 },
		{ VTIMR1, "VTIMR1", 2 },
		{ VTIMR2, "VTIMR2", 2 },
		{ VTIMR4, "VTIMR4", 2 },
	};

	symstore->AddSymbols(kSymbols);

	*ppStore = symstore.release();
	return true;
}

bool ATCreateDefaultKernelSymbolStore(IATSymbolStore **ppStore) {
	using namespace ATKernelSymbols;

	vdrefptr<ATSymbolStore> symstore(new ATSymbolStore);

	symstore->Init(0xD800, 0x0D00);
	static constexpr ATSymbolStore::SymbolInfo kSymbols[] = {
		{ AFP, "AFP", 1 },
		{ FASC, "FASC", 1 },
		{ IPF, "IPF", 1 },
		{ FPI, "FPI", 1 },
		{ ZFR0, "ZFR0", 1 },
		{ ZF1, "ZF1", 1 },
		{ FADD, "FADD", 1 },
		{ FSUB, "FSUB", 1 },
		{ FMUL, "FMUL", 1 },
		{ FDIV, "FDIV", 1 },
		{ PLYEVL, "PLYEVL", 1 },
		{ FLD0R, "FLD0R", 1 },
		{ FLD0P, "FLD0P", 1 },
		{ FLD1R, "FLD1R", 1 },
		{ FLD1P, "FLD1P", 1 },
		{ FST0R, "FST0R", 1 },
		{ FST0P, "FST0P", 1 },
		{ FMOVE, "FMOVE", 1 },
		{ EXP, "EXP", 1 },
		{ EXP10, "EXP10", 1 },
		{ LOG, "LOG", 1 },
		{ LOG10, "LOG10", 1 },
		{ 0xE400, "EDITRV", 3 },
		{ 0xE410, "SCRENV", 3 },
		{ 0xE420, "KEYBDV", 3 },
		{ 0xE430, "PRINTV", 3 },
		{ 0xE440, "CASETV", 3 },
		{ 0xE450, "DISKIV", 3 },
		{ 0xE453, "DSKINV", 3 },
		{ 0xE456, "CIOV", 3 },
		{ 0xE459, "SIOV", 3 },
		{ 0xE45C, "SETVBV", 3 },
		{ 0xE45F, "SYSVBV", 3 },
		{ 0xE462, "XITVBV", 3 },
		{ 0xE465, "SIOINV", 3 },
		{ 0xE468, "SENDEV", 3 },
		{ 0xE46B, "INTINV", 3 },
		{ 0xE46E, "CIOINV", 3 },
		{ 0xE471, "BLKBDV", 3 },
		{ 0xE474, "WARMSV", 3 },
		{ 0xE477, "COLDSV", 3 },
		{ 0xE47A, "RBLOKV", 3 },
		{ 0xE47D, "CSOPIV", 3 },
		{ 0xE480, "VCTABL", 3 },
	};

	symstore->AddSymbols(kSymbols);

	*ppStore = symstore.release();
	return true;
}

namespace {
	struct HardwareSymbol {
		uint32 mOffset;
		const char *mpWriteName;
		const char *mpReadName;
	};

	static const HardwareSymbol kGTIASymbols[]={
		{ 0x00, "HPOSP0", "M0PF" },
		{ 0x01, "HPOSP1", "M1PF" },
		{ 0x02, "HPOSP2", "M2PF" },
		{ 0x03, "HPOSP3", "M3PF" },
		{ 0x04, "HPOSM0", "P0PF" },
		{ 0x05, "HPOSM1", "P1PF" },
		{ 0x06, "HPOSM2", "P2PF" },
		{ 0x07, "HPOSM3", "P3PF" },
		{ 0x08, "SIZEP0", "M0PL" },
		{ 0x09, "SIZEP1", "M1PL" },
		{ 0x0A, "SIZEP2", "M2PL" },
		{ 0x0B, "SIZEP3", "M3PL" },
		{ 0x0C, "SIZEM", "P0PL" },
		{ 0x0D, "GRAFP0", "P1PL" },
		{ 0x0E, "GRAFP1", "P2PL" },
		{ 0x0F, "GRAFP2", "P3PL" },
		{ 0x10, "GRAFP3", "TRIG0" },
		{ 0x11, "GRAFM", "TRIG1" },
		{ 0x12, "COLPM0", "TRIG2" },
		{ 0x13, "COLPM1", "TRIG3" },
		{ 0x14, "COLPM2", "PAL" },
		{ 0x15, "COLPM3", NULL },
		{ 0x16, "COLPF0" },
		{ 0x17, "COLPF1" },
		{ 0x18, "COLPF2" },
		{ 0x19, "COLPF3" },
		{ 0x1A, "COLBK" },
		{ 0x1B, "PRIOR" },
		{ 0x1C, "VDELAY" },
		{ 0x1D, "GRACTL" },
		{ 0x1E, "HITCLR" },
		{ 0x1F, "CONSOL", "CONSOL" },
	};

	static const HardwareSymbol kPOKEYSymbols[]={
		{ 0x00, "AUDF1", "POT0" },
		{ 0x01, "AUDC1", "POT1" },
		{ 0x02, "AUDF2", "POT2" },
		{ 0x03, "AUDC2", "POT3" },
		{ 0x04, "AUDF3", "POT4" },
		{ 0x05, "AUDC3", "POT5" },
		{ 0x06, "AUDF4", "POT6" },
		{ 0x07, "AUDC4", "POT7" },
		{ 0x08, "AUDCTL", "ALLPOT" },
		{ 0x09, "STIMER", "KBCODE" },
		{ 0x0A, "SKRES", "RANDOM" },
		{ 0x0B, "POTGO" },
		{ 0x0D, "SEROUT", "SERIN" },
		{ 0x0E, "IRQEN", "IRQST" },
		{ 0x0F, "SKCTL", "SKSTAT" },
	};

	static const HardwareSymbol kPIASymbols[]={
		{ 0x00, "PORTA", "PORTA" },
		{ 0x01, "PORTB", "PORTB" },
		{ 0x02, "PACTL", "PACTL" },
		{ 0x03, "PBCTL", "PBCTL" },
	};

	static const HardwareSymbol kANTICSymbols[]={
		{ 0x00, "DMACTL" },
		{ 0x01, "CHACTL" },
		{ 0x02, "DLISTL" },
		{ 0x03, "DLISTH" },
		{ 0x04, "HSCROL" },
		{ 0x05, "VSCROL" },
		{ 0x07, "PMBASE" },
		{ 0x09, "CHBASE" },
		{ 0x0A, "WSYNC" },
		{ 0x0B, NULL, "VCOUNT" },
		{ 0x0C, NULL, "PENH" },
		{ 0x0D, NULL, "PENV" },
		{ 0x0E, "NMIEN" },
		{ 0x0F, "NMIRES", "NMIST" },
	};

	void AddHardwareSymbols(ATSymbolStore *store, uint32 base, const HardwareSymbol *sym, uint32 n) {
		while(n--) {
			store->AddReadWriteRegisterSymbol(base + sym->mOffset, sym->mpWriteName, sym->mpReadName);
			++sym;
		}
	}

	template<size_t N>
	inline void AddHardwareSymbols(ATSymbolStore *store, uint32 base, const HardwareSymbol (&syms)[N]) {
		AddHardwareSymbols(store, base, syms, N);
	}
}

bool ATCreateDefaultHardwareSymbolStore(IATSymbolStore **ppStore) {
	vdrefptr<ATSymbolStore> symstore(new ATSymbolStore);

	symstore->Init(0xD000, 0x0500);
	AddHardwareSymbols(symstore, 0xD000, kGTIASymbols);
	AddHardwareSymbols(symstore, 0xD200, kPOKEYSymbols);
	AddHardwareSymbols(symstore, 0xD300, kPIASymbols);
	AddHardwareSymbols(symstore, 0xD400, kANTICSymbols);

	*ppStore = symstore.release();
	return true;
}

bool ATCreateDefault5200HardwareSymbolStore(IATSymbolStore **ppStore) {
	vdrefptr<ATSymbolStore> symstore(new ATSymbolStore);

	symstore->Init(0xC000, 0x3000);
	AddHardwareSymbols(symstore, 0xC000, kGTIASymbols);
	AddHardwareSymbols(symstore, 0xE800, kPOKEYSymbols);
	AddHardwareSymbols(symstore, 0xD400, kANTICSymbols);

	*ppStore = symstore.release();
	return true;
}

void ATCreateCustomSymbolStore(IATCustomSymbolStore **ppStore) {
	vdrefptr<ATSymbolStore> symstore(new ATSymbolStore);
	*ppStore = symstore.release();
}

void ATLoadSymbols(const wchar_t *path, IATSymbolStore **outsymbols) {
	vdrefptr<IATCustomSymbolStore> symbols;
	ATCreateCustomSymbolStore(~symbols);

	symbols->Load(path);

	*outsymbols = symbols.release();
}

void ATLoadSymbols(const wchar_t *filename, IVDRandomAccessStream& stream, IATSymbolStore **outsymbols) {
	vdrefptr<IATCustomSymbolStore> symbols;
	ATCreateCustomSymbolStore(~symbols);

	symbols->Load(filename, stream);

	*outsymbols = symbols.release();
}

void ATSaveSymbols(const wchar_t *path, IATSymbolStore *syms) {
	VDFileStream fs(path, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
	VDTextOutputStream tos(&fs);

	// write header
	tos.PutLine("Altirra symbol file");

	// write out symbols
	tos.PutLine();
	tos.PutLine("[symbols]");

	uint32 base = syms->GetDefaultBase();

	const uint32 n = syms->GetSymbolCount();
	for(uint32 i=0; i<n; ++i) {
		ATSymbolInfo sym;

		syms->GetSymbol(i, sym);

		tos.FormatLine("%s%s%s %04x,%x %s"
			, sym.mFlags & kATSymbol_Read ? "r" : ""
			, sym.mFlags & kATSymbol_Write ? "w" : ""
			, sym.mFlags & kATSymbol_Execute ? "x" : ""
			, base + sym.mOffset
			, sym.mLength
			, sym.mpName);
	}
}
