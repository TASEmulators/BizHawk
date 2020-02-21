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

#ifndef f_AT_CONSTANTS_H
#define f_AT_CONSTANTS_H

#include <at/atcore/enumparse.h>

enum ATMemoryMode : uint32 {
	kATMemoryMode_48K,
	kATMemoryMode_52K,
	kATMemoryMode_64K,
	kATMemoryMode_128K,
	kATMemoryMode_320K,
	kATMemoryMode_576K,
	kATMemoryMode_1088K,
	kATMemoryMode_16K,
	kATMemoryMode_8K,
	kATMemoryMode_24K,
	kATMemoryMode_32K,
	kATMemoryMode_40K,
	kATMemoryMode_320K_Compy,
	kATMemoryMode_576K_Compy,
	kATMemoryMode_256K,
	kATMemoryModeCount
};

enum ATHardwareMode : uint32 {
	kATHardwareMode_800,
	kATHardwareMode_800XL,
	kATHardwareMode_5200,
	kATHardwareMode_XEGS,
	kATHardwareMode_1200XL,
	kATHardwareMode_130XE,
	kATHardwareModeCount
};

enum ATROMImage {
	kATROMImage_OSA,
	kATROMImage_OSB,
	kATROMImage_XL,
	kATROMImage_XEGS,
	kATROMImage_Other,
	kATROMImage_5200,
	kATROMImage_Basic,
	kATROMImage_Game,
	kATROMImage_KMKJZIDE,
	kATROMImage_KMKJZIDEV2,
	kATROMImage_KMKJZIDEV2_SDX,
	kATROMImage_SIDE_SDX,
	kATROMImage_1200XL,
	kATROMImage_MyIDEII,
	kATROMImage_Ultimate1MB,
	kATROMImage_SIDE2_SDX,
	kATROMImageCount
};

enum ATKernelMode {
	kATKernelMode_Default,
	kATKernelMode_800,				// $D800-FFFF
	kATKernelMode_800Extended,		// $C000-CFFF, D800-FFFF
	kATKernelMode_XL,				// $C000-CFFF, D800-FFFF + self-test
	kATKernelMode_5200,				// $F000-FFFF (2K mirrored)
	kATKernelModeCount
};

enum ATStorageId {
	kATStorageId_None,
	kATStorageId_UnitMask = 0x00FF,
	kATStorageId_Disk = 0x0100,
	kATStorageId_Cartridge = 0x0200,
	kATStorageId_Tape = 0x0300,
	kATStorageId_Firmware = 0x0400,
	kATStorageId_TypeMask = 0xFF00,
	kATStorageId_All
};

static const int kATStorageIdTypeShift = 8;

enum ATStorageTypeMask : uint32 {
	kATStorageTypeMask_Disk			= 0x1,
	kATStorageTypeMask_Cartridge	= 0x2,
	kATStorageTypeMask_Tape			= 0x4,
	kATStorageTypeMask_All			= 0x7
};

enum ATVideoStandard : uint32 {
	kATVideoStandard_NTSC,
	kATVideoStandard_PAL,
	kATVideoStandard_SECAM,
	kATVideoStandard_PAL60,
	kATVideoStandard_NTSC50,
	kATVideoStandardCount
};

enum ATMemoryClearMode : uint8 {
	kATMemoryClearMode_Zero,
	kATMemoryClearMode_Random,
	kATMemoryClearMode_DRAM1,
	kATMemoryClearMode_DRAM2,
	kATMemoryClearMode_DRAM3,
	kATMemoryClearModeCount
};

enum ATHLEProgramLoadMode {
	kATHLEProgramLoadMode_Default,
	kATHLEProgramLoadMode_Type3Poll,
	kATHLEProgramLoadMode_Deferred,
	kATHLEProgramLoadMode_DiskBoot,
};

AT_DECLARE_ENUM_TABLE(ATHLEProgramLoadMode);

#endif
