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

#ifndef AT_SYMBOLS_H
#define AT_SYMBOLS_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/refcount.h>

class IVDRandomAccessStream;

enum {
	kATSymbol_Read		= 0x01,
	kATSymbol_Write		= 0x02,
	kATSymbol_Execute	= 0x04,
	kATSymbol_Any		= 0x07
};

struct ATSymbol {
	const char *mpName;
	uint32	mOffset;
	uint16	mFlags;
	uint16	mLine;
	uint16	mFileId;
};

struct ATSymbolInfo {
	const char *mpName;
	uint16	mFlags;
	uint32	mOffset;
	uint16	mLength;
};

struct ATSourceLineInfo {
	uint32	mOffset;
	uint16	mLine;
	uint16	mFileId;
};

enum ATSymbolDirectiveType {
	kATSymbolDirType_None,
	kATSymbolDirType_Assert,
	kATSymbolDirType_Trace
};

struct ATSymbolDirectiveInfo {
	ATSymbolDirectiveType mType;
	uint32 mOffset;
	const char *mpArguments;
};

class IATSymbolStore : public IVDRefCount {
public:
	virtual uint32	GetDefaultBase() const = 0;
	virtual uint32	GetDefaultSize() const = 0; 
	virtual bool	LookupSymbol(uint32 moduleOffset, uint32 flags, ATSymbol& symbol) = 0;
	virtual sint32	LookupSymbol(const char *name) = 0;
	virtual const wchar_t *GetFileName(uint16 fileid) = 0;
	virtual uint16	GetFileId(const wchar_t *fileName, int *matchQuality) = 0;

	virtual void	GetLines(uint16 fileId, vdfastvector<ATSourceLineInfo>& lines) = 0;
	virtual bool	GetLineForOffset(uint32 moduleOffset, bool searchUp, ATSourceLineInfo& lineInfo) = 0;
	virtual bool	GetOffsetForLine(const ATSourceLineInfo& lineInfo, uint32& moduleOffset) = 0;

	virtual uint32	GetSymbolCount() const = 0;
	virtual void	GetSymbol(uint32 index, ATSymbolInfo& symbol) = 0;

	virtual uint32	GetDirectiveCount() const = 0;
	virtual void	GetDirective(uint32 index, ATSymbolDirectiveInfo& dirInfo) = 0;
};

class IATCustomSymbolStore : public IATSymbolStore {
public:
	virtual void Load(const wchar_t *path) = 0;
	virtual void Load(const wchar_t *filename, IVDRandomAccessStream& stream) = 0;
	virtual void Init(uint32 moduleBase, uint32 moduleSize) = 0;
	virtual void RemoveSymbol(uint32 offset) = 0;
	virtual void AddSymbol(uint32 offset, const char *name, uint32 size = 1, uint32 flags = kATSymbol_Read | kATSymbol_Write | kATSymbol_Execute, uint16 file = 0, uint16 line = 0) = 0;
	virtual void AddReadWriteRegisterSymbol(uint32 offset, const char *writename, const char *readname = NULL) = 0;
	virtual uint16 AddFileName(const wchar_t *fileName) = 0;
	virtual void AddSourceLine(uint16 fileId, uint16 line, uint32 moduleOffset, uint32 len = 0) = 0;
};

bool ATCreateDefaultVariableSymbolStore(IATSymbolStore **ppStore);
bool ATCreateDefaultVariableSymbolStore5200(IATSymbolStore **ppStore);
bool ATCreateDefaultKernelSymbolStore(IATSymbolStore **ppStore);
bool ATCreateDefaultHardwareSymbolStore(IATSymbolStore **ppStore);
bool ATCreateDefault5200HardwareSymbolStore(IATSymbolStore **ppStore);
void ATCreateCustomSymbolStore(IATCustomSymbolStore **ppStore);
void ATLoadSymbols(const wchar_t *path, IATSymbolStore **ppStore);
void ATLoadSymbols(const wchar_t *filename, IVDRandomAccessStream& stream, IATSymbolStore **ppStore);
void ATSaveSymbols(const wchar_t *path, IATSymbolStore *ppStore);

#endif
