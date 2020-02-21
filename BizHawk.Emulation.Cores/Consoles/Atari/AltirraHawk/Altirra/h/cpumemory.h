//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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

#ifndef f_AT_CPUMEMORY_H
#define f_AT_CPUMEMORY_H

#ifndef VDFORCEINLINE
#define VDFORCEINLINE __forceinline
#endif

#define ATCPUMEMISSPECIAL(addr) (((addr) & 1) != 0)

class VDINTERFACE ATCPUEmulatorMemory {
public:
	using PageTable = uintptr[256];
	using PageTablePtr = PageTable *;
	using BankTable = PageTablePtr[256];

	virtual uint8 CPUReadByte(uint32 address) = 0;
	virtual uint8 CPUExtReadByte(uint16 address, uint8 bank) = 0;
	virtual sint32 CPUExtReadByteAccel(uint16 address, uint8 bank, bool chipOK) = 0;
	virtual uint8 CPUDebugReadByte(uint16 address) const = 0;
	virtual uint8 CPUDebugExtReadByte(uint16 address, uint8 bank) const = 0;
	virtual void CPUWriteByte(uint16 address, uint8 value) = 0;
	virtual void CPUExtWriteByte(uint16 address, uint8 bank, uint8 value) = 0;
	virtual sint32 CPUExtWriteByteAccel(uint16 address, uint8 bank, uint8 value, bool chipOK) = 0;

	uint8	mBusValue;
	PageTablePtr mpCPUReadPageMap;
	PageTablePtr mpCPUWritePageMap;
	const uint32 *mpCPUReadAddressPageMap;
	const BankTable *mpCPUReadBankMap;
	const BankTable *mpCPUWriteBankMap;

	uint8 DebugReadByte(uint16 address) const {
		uintptr readPage = (*mpCPUReadPageMap)[address >> 8];
		return ATCPUMEMISSPECIAL(readPage) ? CPUDebugReadByte(address) : *(const uint8 *)(readPage + address);
	}

	VDFORCEINLINE uint8 ReadByte(uint32 address) {
		uintptr readPage = (*mpCPUReadPageMap)[(uint8)(address >> 8)];
		return ATCPUMEMISSPECIAL(readPage) ? CPUReadByte(address) : *(const uint8 *)(readPage + (address & 0xffff));
	}

	VDFORCEINLINE uint8 ReadByteAddr16(uint32 address) {
		uintptr readPage = (*mpCPUReadPageMap)[address >> 8];
		return ATCPUMEMISSPECIAL(readPage) ? CPUReadByte(address) : *(const uint8 *)(readPage + (address & 0xffff));
	}

	VDFORCEINLINE void DummyReadByte(uint32 address) {
		uintptr readPage = (*mpCPUReadPageMap)[(uint8)(address >> 8)];

		if (ATCPUMEMISSPECIAL(readPage))
			CPUReadByte(address);
	}

	uint8 DebugExtReadByte(uint16 address, uint8 bank) const {
		uintptr readPage = (*(*mpCPUReadBankMap)[bank])[address >> 8];
		return ATCPUMEMISSPECIAL(readPage) ? CPUDebugExtReadByte(address, bank) : *(const uint8 *)(readPage + address);
	}

	void DummyExtReadByte(uint16 address, uint8 bank) {
		uintptr readPage = (*(*mpCPUReadBankMap)[bank])[address >> 8];
		if (ATCPUMEMISSPECIAL(readPage))
			CPUExtReadByte(address, bank);
	}

	uint8 ExtReadByte(uint16 address, uint8 bank) {
		uintptr readPage = (*(*mpCPUReadBankMap)[bank])[address >> 8];
		return ATCPUMEMISSPECIAL(readPage) ? CPUExtReadByte(address, bank) : *(const uint8 *)(readPage + address);
	}
	
	sint32 ExtReadByteAccel(uint16 address, uint8 bank, bool chipOK) {
		uintptr readPage = (*(*mpCPUReadBankMap)[bank])[address >> 8];
		return ATCPUMEMISSPECIAL(readPage) ? CPUExtReadByteAccel(address, bank, chipOK) : *(const uint8 *)(readPage + address);
	}

	void WriteByte(uint16 address, uint8 value) {
		uintptr writePage = (*mpCPUWritePageMap)[address >> 8];
		if (!ATCPUMEMISSPECIAL(writePage))
			*(uint8 *)(writePage + address) = value;
		else
			CPUWriteByte(address, value);
	}

	void ExtWriteByte(uint16 address, uint8 bank, uint8 value) {
		uintptr writePage = (*(*mpCPUWriteBankMap)[bank])[address >> 8];

		if (!ATCPUMEMISSPECIAL(writePage))
			*(uint8 *)(writePage + address) = value;
		else
			CPUExtWriteByte(address, bank, value);
	}

	sint32 ExtWriteByteAccel(uint16 address, uint8 bank, uint8 value, bool chipOK) {
		uintptr writePage = (*(*mpCPUWriteBankMap)[bank])[address >> 8];

		if (!ATCPUMEMISSPECIAL(writePage)) {
			*(uint8 *)(writePage + address) = value;
			return 0;
		} else
			return CPUExtWriteByteAccel(address, bank, value, chipOK);
	}
};

#endif	// f_AT_CPUMEMORY_H
