//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2009 Avery Lee
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
#include <vd2/system/binary.h>
#include <vd2/system/text.h>
#include <vd2/system/VDString.h>
#include "savestate.h"

///////////////////////////////////////////////////////////////////////////

ATInvalidSaveStateException::ATInvalidSaveStateException()
	: MyError("The save state data is invalid.")
{
}

ATUnsupportedSaveStateException::ATUnsupportedSaveStateException()
	: MyError("The saved state uses features unsupported by this version of Altirra and cannot be loaded.")
{
}

///////////////////////////////////////////////////////////////////////////

ATSaveStateReader::ATSaveStateReader(const uint8 *src, uint32 len)
	: mpSrc(src)
	, mSize(len)
	, mPosition(0)
{
}

ATSaveStateReader::~ATSaveStateReader() {
}

void ATSaveStateReader::RegisterHandler(ATSaveStateSection section, uint32 fcc, const ATSaveStateReadHandler& handler) {
	ATSaveStateReadHandler *p = (ATSaveStateReadHandler *)mLinearAlloc.Allocate(handler.mSize);
	memcpy(p, &handler, handler.mSize);

	HandlerEntry *he = (HandlerEntry *)mLinearAlloc.Allocate(sizeof(HandlerEntry));
	he->mpNext = NULL;
	he->mpHandler = p;

	HandlerMap::insert_return_type r = mHandlers[section].insert(fcc);

	if (r.second) {
		// no previous entries... link in directly
		r.first->second = he;
	} else {
		// previous entries... link in at *end* of chain
		HandlerEntry *next = r.first->second;

		while(next->mpNext)
			next = next->mpNext;

		next->mpNext = he;
	}
}

bool ATSaveStateReader::CheckAvailable(uint32 size) const {
	return (mSize - mPosition) >= size;
}

uint32 ATSaveStateReader::GetAvailable() const {
	return mSize - mPosition;
}

void ATSaveStateReader::OpenChunk(uint32 length) {
	if (mSize - mPosition < length)
		throw ATInvalidSaveStateException();

	mChunkStack.push_back(mSize);
	mSize = mPosition + length;
}

void ATSaveStateReader::CloseChunk() {
	mPosition = mSize;
	mSize = mChunkStack.back();
	mChunkStack.pop_back();
}

void ATSaveStateReader::DispatchChunk(ATSaveStateSection section, uint32 fcc) {
	HandlerMap::iterator it = mHandlers[section].find(fcc);

	if (it == mHandlers[section].end())
		return;

	HandlerEntry *he = it->second;

	do {
		const ATSaveStateReadHandler *h = he->mpHandler;

		h->mpDispatchFn(*this, h);

		he = he->mpNext;

		// Zero is a special broadcast case.
	} while(he && !fcc);

	// remove handlers that we processed
	it->second = he;
}

bool ATSaveStateReader::ReadBool() {
	uint8 v = 0;
	ReadData(&v, 1);
	return v != 0;
}

sint8 ATSaveStateReader::ReadSint8() {
	sint8 v = 0;
	ReadData(&v, 1);
	return v;
}

sint16 ATSaveStateReader::ReadSint16() {
	sint16 v = 0;
	ReadData(&v, 2);
	return v;
}

sint32 ATSaveStateReader::ReadSint32() {
	sint32 v = 0;
	ReadData(&v, 4);
	return v;
}

uint8 ATSaveStateReader::ReadUint8() {
	uint8 v = 0;
	ReadData(&v, 1);
	return v;
}

uint16 ATSaveStateReader::ReadUint16() {
	uint16 v = 0;
	ReadData(&v, 2);
	return v;
}

uint32 ATSaveStateReader::ReadUint32() {
	uint32 v = 0;
	ReadData(&v, 4);
	return v;
}

uint64 ATSaveStateReader::ReadUint64() {
	uint64 v = 0;
	ReadData(&v, 8);
	return v;
}

void ATSaveStateReader::ReadString(VDStringW& str) {
	uint32 len = 0;
	int shift = 0;

	for(;;) {
		uint8 v;
		ReadData(&v, 1);

		len += (v & 0x7f) << shift;
		shift += 7;

		if (!(v & 0x80))
			break;
	}

	if (mSize - mPosition < len)
		throw ATInvalidSaveStateException();

	str = VDTextU8ToW((const char *)(mpSrc + mPosition), len);
	mPosition += len;
}

void ATSaveStateReader::ReadData(void *dst, uint32 count) {
	if (mSize - mPosition < count)
		throw ATInvalidSaveStateException();

	memcpy(dst, mpSrc + mPosition, count);
	mPosition += count;
}

///////////////////////////////////////////////////////////////////////////

ATSaveStateWriter::ATSaveStateWriter(Storage& dst)
	: mDst(dst)
{
}

ATSaveStateWriter::~ATSaveStateWriter() {
}

void ATSaveStateWriter::RegisterHandler(ATSaveStateSection section, const ATSaveStateWriteHandler& handler) {
	void *p = mLinearAlloc.Allocate(handler.mSize);
	memcpy(p, &handler, handler.mSize);

	mHandlers[section].push_back((const ATSaveStateWriteHandler *)p);
}

void ATSaveStateWriter::WriteSection(ATSaveStateSection section) {
	HandlerList::const_iterator it(mHandlers[section].begin()), itEnd(mHandlers[section].end());
	for(; it != itEnd; ++it) {
		const ATSaveStateWriteHandler *h = *it;

		h->mpDispatchFn(*this, h);
	}
}

void ATSaveStateWriter::BeginChunk(uint32 id) {
	WriteUint32(id);
	mChunkStack.push_back(mDst.size());
	WriteUint32(0);
}

void ATSaveStateWriter::EndChunk() {
	size_t prevPos = mChunkStack.back();
	mChunkStack.pop_back();

	VDWriteUnalignedLEU32(&mDst[prevPos], (uint32)(mDst.size() - (prevPos + 4)));
}

void ATSaveStateWriter::WriteBool(bool b) {
	uint8 v = b ? 1 : 0;
	WriteData(&v, 1);
}

void ATSaveStateWriter::WriteSint8(sint8 v) {
	WriteData(&v, 1);
}

void ATSaveStateWriter::WriteSint16(sint16 v) {
	WriteData(&v, 2);
}

void ATSaveStateWriter::WriteSint32(sint32 v) {
	WriteData(&v, 4);
}

void ATSaveStateWriter::WriteUint8(uint8 v) {
	WriteData(&v, 1);
}

void ATSaveStateWriter::WriteUint16(uint16 v) {
	WriteData(&v, 2);
}

void ATSaveStateWriter::WriteUint32(uint32 v) {
	WriteData(&v, 4);
}

void ATSaveStateWriter::WriteUint64(uint64 v) {
	WriteData(&v, 8);
}

void ATSaveStateWriter::WriteString(const wchar_t *s) {
	const VDStringA& s8 = VDTextWToU8(s, (uint32)wcslen(s));
	size_t len = s8.size();

	do {
		uint8 val = (uint8)len;

		len >>= 7;
		if (len)
			val |= 0x80;

		WriteData(&val, 1);
	} while(len);

	WriteData(s8.data(), s8.size());
}

void ATSaveStateWriter::WriteData(const void *src, uint32 count) {
	const uint8 *p = (const uint8 *)src;

	mDst.insert(mDst.end(), p, p+count);
}
