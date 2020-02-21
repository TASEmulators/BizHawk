//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2014 Avery Lee
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
#include <vd2/system/registry.h>
#include <vd2/system/thread.h>
#include <vd2/system/date.h>
#include "firmwaremanager.h"
#include "oshelper.h"
#include "resource.h"

bool g_ATFirmwarePathPortabilityEnabled;

const char *ATGetFirmwareTypeName(ATFirmwareType type) {
	static const char *kTypeNames[]={
		"",
		"kernel800_osa",
		"kernel800_osb",
		"kernelxl",
		"kernelxegs",
		"kernel5200",
		"kernel1200xl",
		"basic",
		"5200cart",
		"u1mb",
		"myide",
		"myide2",
		"side",
		"side2",
		"kmkjzide",
		"kmkjzide2",
		"kmkjzide2_sdx",
		"blackbox",
		"game",
		"mio",
		"850handler",
		"850relocator",
		"1030firmware",
		"810",
		"happy810",
		"810archiver",
		"1050",
		"usdoubler",
		"speedy1050",
		"happy1050",
		"superarchiver",
		"toms1050",
		"tygrys1050",
		"1050duplicator",
		"indusgt",
		"1050turbo",
		"1050turboii",
		"xf551",
		"atr8000",
		"percom",
		"rapidus_flash",
		"rapidus_corepbi",
		"isplate",
		"warpos",
	};

	VDASSERTCT(vdcountof(kTypeNames) == kATFirmwareTypeCount);

	return kTypeNames[type];
}

ATFirmwareType ATGetFirmwareTypeFromName(const char *type) {
	     if (!strcmp(type, "kernel800_osa")) return kATFirmwareType_Kernel800_OSA;
	else if (!strcmp(type, "kernel800_osb")) return kATFirmwareType_Kernel800_OSB;
	else if (!strcmp(type, "kernelxl")) return kATFirmwareType_KernelXL;
	else if (!strcmp(type, "kernelxegs")) return kATFirmwareType_KernelXEGS;
	else if (!strcmp(type, "game")) return kATFirmwareType_Game;
	else if (!strcmp(type, "kernel1200xl")) return kATFirmwareType_Kernel1200XL;
	else if (!strcmp(type, "kernel5200")) return kATFirmwareType_Kernel5200;
	else if (!strcmp(type, "basic")) return kATFirmwareType_Basic;
	else if (!strcmp(type, "5200cart")) return kATFirmwareType_5200Cartridge;
	else if (!strcmp(type, "u1mb")) return kATFirmwareType_U1MB;
	else if (!strcmp(type, "myide")) return kATFirmwareType_MyIDE;
	else if (!strcmp(type, "myide2")) return kATFirmwareType_MyIDE2;
	else if (!strcmp(type, "side")) return kATFirmwareType_SIDE;
	else if (!strcmp(type, "side2")) return kATFirmwareType_SIDE2;
	else if (!strcmp(type, "kmkjzide")) return kATFirmwareType_KMKJZIDE;
	else if (!strcmp(type, "kmkjzide2")) return kATFirmwareType_KMKJZIDE2;
	else if (!strcmp(type, "kmkjzide2_sdx")) return kATFirmwareType_KMKJZIDE2_SDX;
	else if (!strcmp(type, "blackbox")) return kATFirmwareType_BlackBox;
	else if (!strcmp(type, "mio")) return kATFirmwareType_MIO;
	else if (!strcmp(type, "850handler")) return kATFirmwareType_850Handler;
	else if (!strcmp(type, "850relocator")) return kATFirmwareType_850Relocator;
	else if (!strcmp(type, "1030firmware")) return kATFirmwareType_1030Firmware;
	else if (!strcmp(type, "810")) return kATFirmwareType_810;
	else if (!strcmp(type, "happy810")) return kATFirmwareType_Happy810;
	else if (!strcmp(type, "810archiver")) return kATFirmwareType_810Archiver;
	else if (!strcmp(type, "1050")) return kATFirmwareType_1050;
	else if (!strcmp(type, "usdoubler")) return kATFirmwareType_USDoubler;
	else if (!strcmp(type, "speedy1050")) return kATFirmwareType_Speedy1050;
	else if (!strcmp(type, "happy1050")) return kATFirmwareType_Happy1050;
	else if (!strcmp(type, "superarchiver")) return kATFirmwareType_SuperArchiver;
	else if (!strcmp(type, "toms1050")) return kATFirmwareType_TOMS1050;
	else if (!strcmp(type, "tygrys1050")) return kATFirmwareType_Tygrys1050;
	else if (!strcmp(type, "1050duplicator")) return kATFirmwareType_1050Duplicator;
	else if (!strcmp(type, "indusgt")) return kATFirmwareType_IndusGT;
	else if (!strcmp(type, "1050turbo")) return kATFirmwareType_1050Turbo;
	else if (!strcmp(type, "1050turboii")) return kATFirmwareType_1050TurboII;
	else if (!strcmp(type, "xf551")) return kATFirmwareType_XF551;
	else if (!strcmp(type, "atr8000")) return kATFirmwareType_ATR8000;
	else if (!strcmp(type, "percom")) return kATFirmwareType_Percom;
	else if (!strcmp(type, "rapidus_flash")) return kATFirmwareType_RapidusFlash;
	else if (!strcmp(type, "rapidus_corepbi")) return kATFirmwareType_RapidusCorePBI;
	else if (!strcmp(type, "isplate")) return kATFirmwareType_ISPlate;
	else if (!strcmp(type, "warpos")) return kATFirmwareType_WarpOS;
	else return kATFirmwareType_Unknown;
}

const char *ATGetSpecificFirmwareTypeKey(ATSpecificFirmwareType ft) {
	switch(ft) {
	case kATSpecificFirmwareType_BASICRevA:		return "BASIC rev A";
	case kATSpecificFirmwareType_BASICRevB:		return "BASIC rev B";
	case kATSpecificFirmwareType_BASICRevC:		return "BASIC rev C";
	case kATSpecificFirmwareType_OSA:			return "OS-A";
	case kATSpecificFirmwareType_OSB:			return "OS-B";
	case kATSpecificFirmwareType_XLOSr2:		return "XLOS rev 2";
	case kATSpecificFirmwareType_XLOSr4:		return "XLOS rev 4";
	default:
		return nullptr;
	}
}

bool ATIsSpecificFirmwareTypeCompatible(ATFirmwareType type, ATSpecificFirmwareType specificType) {
	switch(specificType) {
		case kATSpecificFirmwareType_BASICRevA:
		case kATSpecificFirmwareType_BASICRevB:
		case kATSpecificFirmwareType_BASICRevC:
			return type == kATFirmwareType_Basic;

		case kATSpecificFirmwareType_OSA:
		case kATSpecificFirmwareType_OSB:
			return type == kATFirmwareType_Kernel800_OSA || type == kATFirmwareType_Kernel800_OSB;

		case kATSpecificFirmwareType_XLOSr2:
		case kATSpecificFirmwareType_XLOSr4:
			return type == kATFirmwareType_KernelXL || type == kATFirmwareType_KernelXEGS || type == kATFirmwareType_Kernel1200XL;
	}

	return false;
}

void ATSetFirmwarePathPortabilityMode(bool portable) {
	g_ATFirmwarePathPortabilityEnabled = portable;
}

uint64 ATGetFirmwareIdFromPath(const wchar_t *path) {
	VDStringW relPath;
	if (!VDFileIsRelativePath(path)) {
		relPath = VDFileGetRelativePath(VDGetProgramPath().c_str(), path, g_ATFirmwarePathPortabilityEnabled);

		if (!relPath.empty())
			path = relPath.c_str();
	}

	uint64 hash = 14695981039346656037ull;

	while(const wchar_t c = towlower(*path++))
		hash = (hash ^ (unsigned)c) * 1099511628211ull;

	return hash | 0x8000000000000000ull;
}

bool ATLoadInternalFirmware(uint64 id, void *dst, uint32 offset, uint32 len, bool *changed, uint32 *actualLen, vdfastvector<uint8> *dstbuf, bool *isUsable) {
	static const uint32 kResourceIds[]={
		IDR_NOKERNEL,
		IDR_KERNEL,
		IDR_KERNELXL,
		0,
		IDR_BASIC,
		IDR_5200KERNEL,
		IDR_NOCARTRIDGE,
		IDR_U1MBBIOS,
		IDR_NOHDBIOS,
		IDR_850RELOCATOR,
		IDR_850HANDLER,
		IDR_1030HANDLER,
		IDR_NOMIO,
		IDR_NOBLACKBOX,
		IDR_NOGAME,
		IDR_RAPIDUSFLASH,
		IDR_RAPIDUSPBI16,
		IDR_KERNEL816
	};

	VDASSERTCT(vdcountof(kResourceIds) == kATFirmwareId_PredefCount);

	if (id >= kATFirmwareId_PredefCount1 || id == kATFirmwareId_Kernel_HLE)
		return false;

	if (isUsable) {
		bool usable = true;

		switch(id) {
			case kATFirmwareId_Invalid:
			case kATFirmwareId_NoKernel:
			case kATFirmwareId_5200_NoCartridge:
			case kATFirmwareId_NoMIO:
			case kATFirmwareId_NoBlackBox:
			case kATFirmwareId_NoGame:
				usable = false;
				break;
		}

		*isUsable = usable;
	}

	uint32 resId = kResourceIds[id - 1];

	switch(resId) {
		default:
			// uncompressed
			if (dstbuf)
				return ATLoadKernelResource(resId, *dstbuf);
			else
				return ATLoadKernelResource(resId, dst, offset, len, true);

		case IDR_U1MBBIOS:
		case IDR_NOMIO:
		case IDR_NOBLACKBOX:
		case IDR_RAPIDUSFLASH:
			// compressed
			break;
	}

	vdfastvector<uint8> buffer;
	ATLoadKernelResourceLZPacked(resId, buffer);

	if (dstbuf) {
		if (changed && *dstbuf != buffer)
			*changed = true;

		*dstbuf = buffer;
	} else {
		if (offset >= buffer.size())
			return false;

		size_t avail = buffer.size() - offset;
		if (len > avail)
			len = (uint32)avail;

		if (changed && memcmp(dst, buffer.data() + offset, len))
			*changed = true;

		memcpy(dst, buffer.data() + offset, len);
	}

	if (actualLen)
		*actualLen = len;

	return true;
}

ATFirmwareManager::ATFirmwareManager() {
	// mark specific firmware cache uninitialized
	for(uint64& id : mSpecificFirmwares) {
		id = 0xFFFF;
	}
}

ATFirmwareManager::~ATFirmwareManager() {
}

bool ATFirmwareManager::GetFirmwareInfo(uint64 id, ATFirmwareInfo& fwinfo) const {
	if (!id)
		return false;

	if (id < kATFirmwareId_Custom) {
		if (id >= kATFirmwareId_PredefCount1)
			return false;

		if (id == kATFirmwareId_Kernel_HLE)
			return false;

		static const struct {
			bool mbAutoselect;
			bool mbVisible;
			ATFirmwareType mType;
			const wchar_t *mpName;
		} kPredefFirmwares[]={
			{ false, false, kATFirmwareType_KernelXL, L"NoKernel" },
			{ true, true, kATFirmwareType_Kernel800_OSB, L"AltirraOS for 400/800" },
			{ true, true, kATFirmwareType_KernelXL,  L"AltirraOS for XL/XE/XEGS" },
			{ false, false, kATFirmwareType_KernelXL, L"AltirraOS HLE" },
			{ true, true, kATFirmwareType_Basic, L"Altirra BASIC" },
			{ true, true, kATFirmwareType_Kernel5200, L"AltirraOS for 5200" },
			{ true, false, kATFirmwareType_5200Cartridge, L"No5200" },
			{ true, true, kATFirmwareType_U1MB, L"Altirra U1MB Recovery OS" },
			{ true, false, kATFirmwareType_KMKJZIDE, L"Altirra NoBIOS for KMK/JZ IDE" },
			{ true, false, kATFirmwareType_850Relocator, L"Altirra 850 Relocator Firmware" },
			{ true, false, kATFirmwareType_850Handler, L"Altirra 850 Handler Firmware" },
			{ true, true, kATFirmwareType_1030Firmware, L"Altirra 1030 Modem Firmware" },
			{ true, false, kATFirmwareType_MIO, L"Altirra NoFirmware for MIO" },
			{ true, false, kATFirmwareType_BlackBox, L"Altirra NoFirmware for BlackBox" },
			{ true, false, kATFirmwareType_Game, L"Altirra placeholder NoGame" },
			{ true, true, kATFirmwareType_RapidusFlash, L"Altirra Rapidus Bootstrap Flash" },
			{ true, true, kATFirmwareType_RapidusCorePBI, L"Altirra Rapidus Bootstrap 65816 PBI Firmware" },
			{ false, true, kATFirmwareType_KernelXL, L"AltirraOS for 65C816" },
		};

		VDASSERTCT(vdcountof(kPredefFirmwares) == kATFirmwareId_PredefCount);

		fwinfo.mId = id;
		fwinfo.mbAutoselect = kPredefFirmwares[id - 1].mbAutoselect;
		fwinfo.mbVisible = kPredefFirmwares[id - 1].mbVisible;
		fwinfo.mType = kPredefFirmwares[id - 1].mType;
		fwinfo.mName = kPredefFirmwares[id - 1].mpName;
		fwinfo.mPath.clear();
		fwinfo.mFlags = 0;

		VDStringW s;
		if (id == kATFirmwareId_Kernel_LLEXL || id ==kATFirmwareId_Kernel_816) {
			if (mKernelXLVersion.empty()) {
				vdfastvector<uint8> buf(16384, 0);
				ATLoadInternalFirmware(kATFirmwareId_Kernel_LLEXL, buf.data(), 0, buf.size());

				// AltirraOS XL/XE/XEGS places the version string at the top of the
				// self-test area ($57F8).
				mKernelXLVersion = L"AltirraOS ";

				for(uint32 i = 0xD7F8 - 0xC000; i < 0xD800 - 0xC000; ++i) {
					uint8 c = buf[i];

					if (c >= 0x21 && c < 0x7F)
						mKernelXLVersion += (char)c;
					else
						break;
				}

				mKernel816Version = mKernelXLVersion;
				mKernelXLVersion += L" for XL/XE/XEGS";
				mKernel816Version += L" for 65C816";
			}

			fwinfo.mName = (id == kATFirmwareId_Kernel_LLEXL) ? mKernelXLVersion : mKernel816Version;
		} else if (id == kATFirmwareId_Kernel_LLE) {
			if (mKernelVersion.empty()) {
				vdfastvector<uint8> buf(10240, 0);
				ATLoadInternalFirmware(id, buf.data(), 0, buf.size());

				// Standard AltirraOS currently places the memo pad version string
				// at $E4A6, right below KnownRTS. The first 10 chars are 'AltirraOS '.
				mKernelVersion = L"AltirraOS ";

				for(uint32 i = 0xE4B0 - 0xD800; i < 0xE4C0 - 0xD800; ++i) {
					uint8 c = buf[i];

					if (c >= 0x21 && c < 0x7F)
						mKernelVersion += (char)c;
					else
						break;
				}

				mKernelVersion += L" for 400/800";
			}

			fwinfo.mName = mKernelVersion;
		} else if (id == kATFirmwareId_Basic_ATBasic) {
			if (mBASICVersion.empty()) {
				vdfastvector<uint8> buf(8192, 0);
				ATLoadInternalFirmware(id, buf.data(), 0, buf.size());

				// Search for the string within the first page: 'Altirra 8K BASIC '.
				mBASICVersion = L"Altirra BASIC";

				for(uint32 i = 0; i < 256; ++i) {
					if (!memcmp(&buf[i], "Altirra 8K BASIC ", 17)) {
						mBASICVersion += ' ';

						for(uint32 j=0; j<16; ++j) {
							uint8 c = buf[i+j+17];

							if (c >= 0x21 && c < 0x7F)
								mBASICVersion += (char)c;
							else
								break;
						}
						break;
					}
				}
			}

			fwinfo.mName = mBASICVersion;
		}

		return true;
	} else {
		VDStringA name;
		name.sprintf("Firmware\\Available\\%016llX", id);

		VDRegistryAppKey key(name.c_str(), false, false);
		if (!key.isReady())
			return false;

		fwinfo.mId = id;
		fwinfo.mbAutoselect = true;
		fwinfo.mbVisible = true;
		fwinfo.mType = kATFirmwareType_Unknown;
		
		if (!key.getString("Path", fwinfo.mPath))
			return false;

		if (VDFileIsRelativePath(fwinfo.mPath.c_str()))
			fwinfo.mPath = VDMakePath(VDGetProgramPath().c_str(), fwinfo.mPath.c_str());

		if (!key.getString("Name", fwinfo.mName))
			return false;

		VDStringA type;
		if (!key.getString("Type", type))
			return false;

		fwinfo.mType = ATGetFirmwareTypeFromName(type.c_str());

		fwinfo.mFlags = key.getInt("Flags");
		return true;
	}
}

void ATFirmwareManager::GetFirmwareList(vdvector<ATFirmwareInfo>& firmwares) const {
	// add predefined
	firmwares.resize(kATFirmwareId_PredefCount);
	for(uint32 i=0; i<kATFirmwareId_PredefCount; ++i) {
		if (i+1 == kATFirmwareId_Kernel_HLE)
			continue;

		VDVERIFY(GetFirmwareInfo(i+1, firmwares[i]));
	}

	// add custom
	VDRegistryAppKey key("Firmware\\Available", false, false);

	VDRegistryKeyIterator it(key);
	while(const char *name = it.Next()) {
		unsigned long long id;
		char dummy;
		if (1 != sscanf(name, "%llx%c", &id, &dummy))
			continue;

		if (id < (1ull << 63))
			continue;

		ATFirmwareInfo fw;
		if (GetFirmwareInfo(id, fw))
			firmwares.push_back(fw);
	}

	std::sort(firmwares.begin(), firmwares.end(),
		[](const ATFirmwareInfo& x, const ATFirmwareInfo& y) {
			return x.mId < y.mId;
		}
	);
}

uint64 ATFirmwareManager::GetCompatibleFirmware(ATFirmwareType type) const {

	uint64 id = GetFirmwareOfType(type, true);
	if (id)
		return id;

	switch(type) {
		case kATFirmwareType_Kernel800_OSA:
			id = GetCompatibleFirmware(kATFirmwareType_Kernel800_OSB);
			break;

		case kATFirmwareType_Kernel1200XL:
		case kATFirmwareType_KernelXEGS:
			id = GetCompatibleFirmware(kATFirmwareType_KernelXL);
			break;
	}

	return id;
}

uint64 ATFirmwareManager::GetFirmwareOfType(ATFirmwareType type, bool allowInternal) const {
	typedef vdvector<ATFirmwareInfo> Firmwares;
	Firmwares firmwares;
	GetFirmwareList(firmwares);

	uint64 bestId = GetDefaultFirmware(type);

	if (bestId)
		return bestId;

	for(Firmwares::const_iterator it(firmwares.begin()), itEnd(firmwares.end());
		it != itEnd;
		++it)
	{
		if (it->mType != type)
			continue;

		if (it->mId >= kATFirmwareId_Custom) {
			bestId = it->mId;
			break;
		}

		if (allowInternal && !bestId && it->mbAutoselect)
			bestId = it->mId;
	}

	return bestId;
}

VDStringW ATFirmwareManager::GetFirmwareRefString(uint64 id) const {
	if (!id)
		return VDStringW();

	if (id < kATFirmwareId_Custom) {
		VDStringW path;
		path.sprintf(L"internal:%08x", (unsigned)id);
		return path;
	}

	ATFirmwareInfo info;
	GetFirmwareInfo(id, info);

	return info.mPath;
}

uint64 ATFirmwareManager::GetFirmwareByRefString(const wchar_t *refstr) const {
	if (!*refstr)
		return 0;

	if (!wcsncmp(refstr, L"internal:", 9))
		return wcstoul(refstr + 9, NULL, 16);

	uint64 id = ATGetFirmwareIdFromPath(refstr);
	ATFirmwareInfo info;
	return GetFirmwareInfo(id, info) ? id : 0;
}

uint64 ATFirmwareManager::GetDefaultFirmware(ATFirmwareType type) const {
	VDRegistryAppKey key("Firmware\\Default", false);
	VDStringW refStr;
	key.getString(ATGetFirmwareTypeName(type), refStr);

	return GetFirmwareByRefString(refStr.c_str());
}

void ATFirmwareManager::SetDefaultFirmware(ATFirmwareType type, uint64 id) {
	VDRegistryAppKey key("Firmware\\Default", true);
	key.setString(ATGetFirmwareTypeName(type), GetFirmwareRefString(id).c_str());
}

uint64 ATFirmwareManager::GetSpecificFirmware(ATSpecificFirmwareType ft) const {
	if (mSpecificFirmwares[ft] != 0xFFFF)
		return mSpecificFirmwares[ft];

	VDRegistryAppKey key("Firmware\\Specific", false);

	VDStringA s;
	if (!key.getString(ATGetSpecificFirmwareTypeKey(ft), s))
		return 0;

	unsigned long long id;
	char c;
	if (1 != sscanf(s.c_str(), "%llx%c", &id, &c))
		id = 0;

	mSpecificFirmwares[ft] = id;
	return id;
}

void ATFirmwareManager::SetSpecificFirmware(ATSpecificFirmwareType ft, uint64 id) {
	VDRegistryAppKey key("Firmware\\Specific", true);

	VDStringA s;
	s.sprintf("%016llx", id);

	key.setString(ATGetSpecificFirmwareTypeKey(ft), s.c_str());

	mSpecificFirmwares[ft] = id;
}

bool ATFirmwareManager::LoadFirmware(uint64 id, void *dst, uint32 offset, uint32 len, bool *changed, uint32 *actualLen, vdfastvector<uint8> *dstbuf, const uint8 *fill, bool *isUsable) {
	if (id < kATFirmwareId_Custom)
		return ATLoadInternalFirmware(id, dst, offset, len, changed, actualLen, dstbuf, isUsable);

	if (isUsable)
		*isUsable = false;

	ATFirmwareInfo fwinfo;

	if (!GetFirmwareInfo(id, fwinfo))
		return false;

	long actual;

	try {
		VDFile f;
		f.open(fwinfo.mPath.c_str());
		f.seek(offset);

		if (changed) {
			vdblock<char> tmpData(len);
			actual = f.readData(tmpData.data(), len);

			if (actual >= 0) {
				*changed = memcmp(dst, tmpData.data(), actual) != 0;

				memcpy(dst, tmpData.data(), actual);
			}
		} else {
			actual = f.readData(dst, len);
		}

		if (fill) {
			uint32 uactual = actual < 0 ? 0 : (uint32)actual;
			const uint8 fillc = *fill;

			if (uactual < len) {
				uint32 extralen = len - uactual;
				uint8 *extra = (uint8 *)dst + uactual;

				if (changed && !*changed) {
					for(uint32 i=0; i<extralen; ++i) {
						if (extra[i] != fillc) {
							*changed = true;
							break;
						}
					}
				}

				memset(extra, fillc, extralen);
			}
		}
	} catch(const MyError&) {
		return false;
	}

	if (isUsable) {
		bool usable = false;

		const uint8 *dst8 = (const uint8 *)dst;
		for(long i=0; i<actual; ++i) {
			if (dst8[i] != 0 && dst8[i] != 0xFF) {
				usable = true;
				break;
			}
		}

		*isUsable = usable;
	}

	if (actualLen)
		*actualLen = (actual < 0) ? 0 : (uint32)actual;

	return true;
}

void ATFirmwareManager::AddFirmware(const ATFirmwareInfo& info) {
	VDASSERT(info.mType != kATFirmwareType_Unknown);

	VDStringA name;
	name.sprintf("Firmware\\Available\\%016llX", ATGetFirmwareIdFromPath(info.mPath.c_str()));
	VDRegistryAppKey key(name.c_str());
	key.setString("Name", info.mName.c_str());

	const VDStringW& programPath = VDGetProgramPath();
	const VDStringW& relPath = VDFileGetRelativePath(programPath.c_str(), info.mPath.c_str(), g_ATFirmwarePathPortabilityEnabled);
	const VDStringW *path = relPath.empty() ? &info.mPath : &relPath;

	key.setString("Path", path->c_str());

	key.setString("Type", ATGetFirmwareTypeName(info.mType));
	key.setInt("Flags", info.mFlags);
}

void ATFirmwareManager::RemoveFirmware(uint64 id) {
	if (id < kATFirmwareId_Custom)
		return;

	VDStringA name;
	name.sprintf("%016llX", id);

	VDRegistryAppKey key("Firmware\\Available");

	{
		VDRegistryKey key2(key, name.c_str());

		VDRegistryValueIterator it(key);
		while(const char *name = it.Next())
			key.removeValue(name);
	}

	key.removeKey(name.c_str());
}
