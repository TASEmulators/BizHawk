//	VDCompiler - Custom shader video filter for VirtualDub
//	Copyright (C) 2007 Avery Lee
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
#include <math.h>
#include <windows.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/VDString.h>
#include <vd2/system/w32assist.h>
#include "filecreator.h"

HMODULE LoadPECOFF(const wchar_t *s) {
	VDFile f(s);
	uint32 hdrpage[1024];

	f.read(hdrpage, 4096);

	char *pBase = (char *)hdrpage;

	// The PEheader offset is at hmod+0x3c.  Add the size of the optional header
	// to step to the section headers.

	const uint32 peoffset = ((const long *)pBase)[15];
	const uint32 signature = *(uint32 *)(pBase + peoffset);

	if (signature != IMAGE_NT_SIGNATURE)
		return NULL;

	const IMAGE_FILE_HEADER *pHeader = (const IMAGE_FILE_HEADER *)(pBase + peoffset + 4);
	DWORD imageSize;

	switch(*(short *)((char *)pHeader + IMAGE_SIZEOF_FILE_HEADER)) {
	case IMAGE_NT_OPTIONAL_HDR64_MAGIC:
		{
			const IMAGE_OPTIONAL_HEADER64 *pOpt = (IMAGE_OPTIONAL_HEADER64 *)((const char *)pHeader + sizeof(IMAGE_FILE_HEADER));

			imageSize = pOpt->SizeOfImage;
		}
		break;
	case IMAGE_NT_OPTIONAL_HDR32_MAGIC:
		{
			const IMAGE_OPTIONAL_HEADER32 *pOpt = (IMAGE_OPTIONAL_HEADER32 *)((const char *)pHeader + sizeof(IMAGE_FILE_HEADER));

			imageSize = pOpt->SizeOfImage;
		}
		break;
	default:		// reject PE32+
		return NULL;
	}

	if (imageSize < sizeof hdrpage)
		return NULL;

	HMODULE hmod = (HMODULE)VirtualAlloc(NULL, imageSize, MEM_COMMIT, PAGE_READWRITE);
	if (!hmod)
		throw MyMemoryError();

	memcpy(hmod, hdrpage, sizeof hdrpage);
	pHeader = (IMAGE_FILE_HEADER *)((char *)pBase + peoffset + 4);

	IMAGE_SECTION_HEADER *pSection = (IMAGE_SECTION_HEADER *)((char *)(pHeader + 1) + pHeader->SizeOfOptionalHeader);
	for(uint32 i = 0; i < pHeader->NumberOfSections; ++i, ++pSection) {
		if (pSection->VirtualAddress > imageSize || imageSize - pSection->VirtualAddress < pSection->SizeOfRawData)
			throw MyError("Invalid section in PE/COFF file.");

		f.seek(pSection->PointerToRawData);
		f.read((char *)hmod + pSection->VirtualAddress, pSection->SizeOfRawData);
	}

	return hmod;
}

void FreePECOFF(HMODULE hmod) {
	VirtualFree(hmod, 0, MEM_FREE);
}

static bool ExtractExports(HMODULE hmod, vdvector<VDStringA>& exports) {
	char *pBase = (char *)((uintptr)hmod & ~(uintptr)0xffff);

	// The PEheader offset is at hmod+0x3c.  Add the size of the optional header
	// to step to the section headers.

	const uint32 peoffset = ((const long *)pBase)[15];
	const uint32 signature = *(uint32 *)(pBase + peoffset);

	if (signature != IMAGE_NT_SIGNATURE)
		return false;

	const IMAGE_FILE_HEADER *pHeader = (const IMAGE_FILE_HEADER *)(pBase + peoffset + 4);

	// Verify the PE optional structure.

	if (pHeader->SizeOfOptionalHeader < 104)
		return false;

	// Find export directory.

	const IMAGE_EXPORT_DIRECTORY *pExportDir;

	switch(*(short *)((char *)pHeader + IMAGE_SIZEOF_FILE_HEADER)) {
	case IMAGE_NT_OPTIONAL_HDR64_MAGIC:
		{
			const IMAGE_OPTIONAL_HEADER64 *pOpt = (IMAGE_OPTIONAL_HEADER64 *)((const char *)pHeader + sizeof(IMAGE_FILE_HEADER));

			if (pOpt->NumberOfRvaAndSizes < 1)
				return false;

			DWORD exportDirRVA = pOpt->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;

			if (!exportDirRVA)
				return false;

			pExportDir = (const IMAGE_EXPORT_DIRECTORY *)(pBase + exportDirRVA);
		}
		break;
	case IMAGE_NT_OPTIONAL_HDR32_MAGIC:
		{
			const IMAGE_OPTIONAL_HEADER32 *pOpt = (IMAGE_OPTIONAL_HEADER32 *)((const char *)pHeader + sizeof(IMAGE_FILE_HEADER));

			if (pOpt->NumberOfRvaAndSizes < 1)
				return false;

			DWORD exportDirRVA = pOpt->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;

			if (!exportDirRVA)
				return false;

			pExportDir = (const IMAGE_EXPORT_DIRECTORY *)(pBase + exportDirRVA);
		}
		break;
	default:		// reject PE32+
		return false;
	}

	// Scan for the export name.
	DWORD nameCount = pExportDir->NumberOfNames;
	const DWORD *nameRVAs = (const DWORD *)(pBase + pExportDir->AddressOfNames);

	for(DWORD i=0; i<nameCount; ++i) {
		DWORD nameRVA = nameRVAs[i];
		const char *pName = (const char *)(pBase + nameRVA);

		exports.push_back() = pName;
	}

	return true;
}

static bool ExtractImports(HMODULE hmod, vdvector<VDStringA>& imports) {
	char *pBase = (char *)((uintptr)hmod & ~(uintptr)0xffff);

	// The PEheader offset is at hmod+0x3c.  Add the size of the optional header
	// to step to the section headers.

	const uint32 peoffset = ((const long *)pBase)[15];
	const uint32 signature = *(uint32 *)(pBase + peoffset);

	if (signature != IMAGE_NT_SIGNATURE)
		return false;

	const IMAGE_FILE_HEADER *pHeader = (const IMAGE_FILE_HEADER *)(pBase + peoffset + 4);

	// Verify the PE optional structure.

	if (pHeader->SizeOfOptionalHeader < 104)
		return false;

	// Find export directory.

	const IMAGE_IMPORT_DESCRIPTOR *pImportDir;
	DWORD importDirRVA;
	bool is64bit = false;

	switch(*(short *)((char *)pHeader + IMAGE_SIZEOF_FILE_HEADER)) {
	case IMAGE_NT_OPTIONAL_HDR64_MAGIC:
		{
			const IMAGE_OPTIONAL_HEADER64 *pOpt = (IMAGE_OPTIONAL_HEADER64 *)((const char *)pHeader + sizeof(IMAGE_FILE_HEADER));

			if (pOpt->NumberOfRvaAndSizes < 1)
				return false;

			importDirRVA = pOpt->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress;
			is64bit = true;
		}
		break;
	case IMAGE_NT_OPTIONAL_HDR32_MAGIC:
		{
			const IMAGE_OPTIONAL_HEADER32 *pOpt = (IMAGE_OPTIONAL_HEADER32 *)((const char *)pHeader + sizeof(IMAGE_FILE_HEADER));

			if (pOpt->NumberOfRvaAndSizes < 1)
				return false;

			importDirRVA = pOpt->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress;
		}
		break;
	default:		// reject PE32+
		return false;
	}

	if (!importDirRVA)
		return false;

	pImportDir = (const IMAGE_IMPORT_DESCRIPTOR *)(pBase + importDirRVA);

	for(; pImportDir->Name; ++pImportDir) {
		const char *dllName = (const char *)(pBase + pImportDir->Name);

		VDStringA normalizedDllName(dllName);

		for(VDStringA::iterator it(normalizedDllName.begin()), itEnd(normalizedDllName.end());
			it != itEnd;
			++it)
		{
			*it = tolower((unsigned char)*it);
		}

		// find the import lookup table
		DWORD iltRVA = pImportDir->Characteristics;
		const char *ilt = pBase + iltRVA;

		for(;;) {
			const char *name;

			if (is64bit) {
				uint64 v = *(const uint64 *)ilt;
				ilt += 8;

				if (!v)
					break;

				// skip ordinal imports
				if (v & 0x8000000000000000ull)
					continue;

				name = pBase + v + 2;
			} else {
				uint32 v = *(const uint32 *)ilt;
				ilt += 4;

				if (!v)
					break;

				// skip ordinal imports
				if (v & 0x80000000ul)
					continue;

				name = pBase + v + 2;
			}

			VDStringA& s = imports.push_back();
			s = normalizedDllName;
			s += ':';
			s += name;
		}
	}

	return true;
}

void tool_checkimports(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() != 3 || !switches.empty()) {
		puts("usage: asuka checkimports extract dll_dir out_importsfile");
		puts("       asuka checkimports verify dll_exe importsfile");
		exit(5);
	}

	const char *cmd = args[0];
	const char *src = args[1];
	const char *dst = args[2];

	if (!strcmp(cmd, "extract")) {
		VDFileStream f(dst, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
		VDTextOutputStream out(&f);

		VDDirectoryIterator it(VDMakePath(VDTextAToW(src).c_str(), L"*.dll").c_str());

		while(it.Next()) {
			HMODULE hmod = LoadPECOFF(it.GetFullPath().c_str());

			if (!hmod)
				throw MyError("Unable to open: %s", VDTextWToA(it.GetName()).c_str());

			vdvector<VDStringA> exports;
			bool success = ExtractExports(hmod, exports);

			FreePECOFF(hmod);

			if (!success)
				throw MyError("Failed to extract exports from: %s", VDTextWToA(it.GetFullPath()).c_str());

			VDStringW dllnamew(it.GetName());
			std::transform(dllnamew.begin(), dllnamew.end(), dllnamew.begin(), towlower);

			VDStringA dllname(VDTextWToA(dllnamew));

			for(vdvector<VDStringA>::const_iterator it(exports.begin()), itEnd(exports.end());
				it != itEnd;
				++it)
			{
				out.FormatLine("%s:%s", dllname.c_str(), it->c_str());
			}
		}
	} else if (!strcmp(cmd, "verify")) {
		vdvector<VDStringA> allowedImports;
		{
			VDTextInputFile ifile(VDTextAToW(dst).c_str());

			while(const char *s = ifile.GetNextLine()) {
				while(*s == ' ' || *s == '\t')
					++s;

				if (*s == ';' || !*s)
					continue;

				allowedImports.push_back_as(s);
			}
		}

		HMODULE hmod = LoadPECOFF(VDTextAToW(src).c_str());

		if (!hmod)
			throw MyError("Unable to open %s", src);

		vdvector<VDStringA> imports;
		bool success = ExtractImports(hmod, imports);

		FreePECOFF(hmod);

		if (!success)
			throw MyError("Unable to extract imports from: %s", src);

		std::sort(allowedImports.begin(), allowedImports.end());
		std::sort(imports.begin(), imports.end());

		vdvector<VDStringA> extraImports(imports.size());
		extraImports.erase(std::set_difference(imports.begin(), imports.end(), allowedImports.begin(), allowedImports.end(), extraImports.begin()), extraImports.end());

		if (!extraImports.empty()) {
			int n = 0;
			while(!extraImports.empty()) {
				printf("Found disallowed import: %s\n", extraImports.back().c_str());
				extraImports.pop_back();
				++n;
			}

			throw MyError("%d disallowed import(s) found.", n);
		}
	} else
		throw MyError("Invalid subcommand: %s", cmd);
}
