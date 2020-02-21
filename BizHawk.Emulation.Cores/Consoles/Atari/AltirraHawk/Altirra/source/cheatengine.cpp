//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include "cheatengine.h"

struct ATCheatEngine::CheatPred {
	bool operator()(const ATCheatEngine::Cheat& x, const ATCheatEngine::Cheat& y) const {
		return x.mAddress < y.mAddress;
	}
};

ATCheatEngine::ATCheatEngine()
	: mpMemory(NULL)
	, mMemorySize(0)
{
}

ATCheatEngine::~ATCheatEngine() {
}

void ATCheatEngine::Init(const void *src, uint32 len) {
	mpMemory = (uint8 *)src;
	mMemorySize = len;

	mLastData.resize(len, 0);
	mValidFlags.resize(len, 0);
}

void ATCheatEngine::Clear() {
	std::fill(mValidFlags.begin(), mValidFlags.end(), 0);
	mCheats.clear();
}

namespace {
	static const char kATCheatFileSignature[] = ";Altirra cheat file";
	enum { kATCheatFileSignatureLen = sizeof(kATCheatFileSignature) - 1 };

	class ATCheatFileParsingException : public MyError {
	public:
		ATCheatFileParsingException(int line) : MyError("Cheat file parsing failed at line %d.", line) {}
	};
}

void ATCheatEngine::Load(const wchar_t *filename) {
	Clear();

	VDFileStream fs(filename);
	VDBufferedStream bs(&fs, 4096);

	static const uint8 kSignatureA8T[4]={ 'A', '8', 'T', 1 };
	uint8 buf[kATCheatFileSignatureLen];

	int headerAvail = bs.ReadData(buf, sizeof(kATCheatFileSignature) - 1);

	if (headerAvail >= 4 && !memcmp(buf, kSignatureA8T, 4)) {
		try {
			bs.Seek(20);

			uint8 count;
			bs.Read(&count, 1);

			while(count--) {
				uint16 v[2];

				bs.Read(v, 4);

				const uint16 address = VDFromLE16(v[0]);

				if (address < mMemorySize)
					mValidFlags[address] = 1;
			}
		} catch(const MyError& e) {
			throw MyError("Unable to read .A8T format file: %s", e.gets());
		}
	} else if (headerAvail >= kATCheatFileSignatureLen && !memcmp(buf, kATCheatFileSignature, kATCheatFileSignatureLen)) {
		bs.Seek(0);

		enum {
			kStateNone,
			kStateCheats
		} state = kStateNone;

		VDTextStream ts(&bs);
		int lineno = 0;

		while(const char *line = ts.GetNextLine()) {
			++lineno;

			while(*line == ' ' || *line == '\t')
				++line;

			// skip blank lines
			if (!*line)
				continue;

			// skip comments
			if (*line == ';')
				continue;

			// check for group
			if (*line == '[') {
				const char *groupStart = ++line;

				while(*line != ']') {
					if (!*line)
						throw ATCheatFileParsingException(lineno);
					++line;
				}

				VDStringSpanA groupName(groupStart, line);

				if (groupName == "cheats")
					state = kStateCheats;
				else
					state = kStateNone;

				continue;
			}

			if (state == kStateCheats) {
				// lock = address, length, size, enabled
				int labelStart;
				int labelEnd;
				char eq;
				int address;
				int value;
				int size;
				int enabled;
				char sentinel;
				int scanned = sscanf(line, " %n%*s%n %c $%x, $%x, %d, %d%c", &labelStart, &labelEnd, &eq, &address, &value, &size, &enabled, &sentinel);

				if (scanned < 2)
					continue;

				if (eq != '=')
					throw ATCheatFileParsingException(lineno);

				VDStringSpanA tag(line + labelStart, line + labelEnd);

				if (tag != "lock")
					continue;

				if (scanned != 5)
					throw ATCheatFileParsingException(lineno);

				if (size != 8 && size != 16)
					continue;

				Cheat cheat = {};
				cheat.mAddress = address;
				cheat.mValue = value;
				cheat.mb16Bit = (size == 16);
				cheat.mbEnabled = (enabled != 0);
				AddCheat(cheat);
			}
		}
	} else {
		throw MyError("File %ls is not a supported cheat file.", VDFileSplitPath(filename));
	}
}

void ATCheatEngine::Save(const wchar_t *filename) {
	VDFileStream fs(filename, nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways);
	VDTextOutputStream tos(&fs);

	tos.PutLine(kATCheatFileSignature);
	tos.PutLine();
	tos.PutLine("[cheats]");

	for(Cheats::const_iterator it(mCheats.begin()), itEnd(mCheats.end()); it != itEnd; ++it) {
		const Cheat& cheat = *it;

		tos.FormatLine("lock = $%04X, $%0*X, %d, %d", cheat.mAddress, cheat.mb16Bit ? 4 : 2, cheat.mValue, cheat.mb16Bit ? 16 : 8, cheat.mbEnabled);
	}
}

void ATCheatEngine::Snapshot(ATCheatSnapshotMode mode, uint32 value, bool bit16) {
	const uint32 n8 = mMemorySize;
	const uint32 n16 = mMemorySize - 1;

	VDASSERT(mMemorySize == mLastData.size());
	VDASSERT(mMemorySize == mValidFlags.size());

	if (bit16)
		mValidFlags[n16] = 0;

	uint8 *prev = mLastData.data();
	uint8 *cur = mpMemory;
	uint8 *valid = mValidFlags.data();

	switch(mode) {
		case kATCheatSnapMode_Replace:
			std::fill(mValidFlags.begin(), mValidFlags.end(), 1);
			break;

		case kATCheatSnapMode_Equal:
			if (bit16) {
				for(uint32 i=0; i<n16; ++i) {
					if (valid[i] && (cur[i] != prev[i] || cur[i+1] != prev[i+1]))
						valid[i] = 0;
				}
			} else {
				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] != prev[i])
						valid[i] = 0;
				}
			}
			break;

		case kATCheatSnapMode_NotEqual:
			if (bit16) {
				for(uint32 i=0; i<n16; ++i) {
					if (valid[i] && cur[i] == prev[i] && cur[i+1] == prev[i+1])
						valid[i] = 0;
				}
			} else {
				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] == prev[i])
						valid[i] = 0;
				}
			}
			break;

		case kATCheatSnapMode_Less:
			if (bit16) {
				for(uint32 i=0; i<n16; ++i) {
					if (valid[i]) {
						uint32 cur16 = cur[i] + ((uint32)cur[i+1] << 8);
						uint32 prev16 = prev[i] + ((uint32)prev[i+1] << 8);

						if (cur16 >= prev16)
							valid[i] = 0;
					}
				}
			} else {
				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] >= prev[i])
						valid[i] = 0;
				}
			}
			break;

		case kATCheatSnapMode_LessEqual:
			if (bit16) {
				for(uint32 i=0; i<n16; ++i) {
					if (valid[i]) {
						uint32 cur16 = cur[i] + ((uint32)cur[i+1] << 8);
						uint32 prev16 = prev[i] + ((uint32)prev[i+1] << 8);

						if (cur16 > prev16)
							valid[i] = 0;
					}
				}
			} else {
				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] > prev[i])
						valid[i] = 0;
				}
			}
			break;

		case kATCheatSnapMode_Greater:
			if (bit16) {
				for(uint32 i=0; i<n16; ++i) {
					if (valid[i]) {
						uint32 cur16 = cur[i] + ((uint32)cur[i+1] << 8);
						uint32 prev16 = prev[i] + ((uint32)prev[i+1] << 8);

						if (cur16 <= prev16)
							valid[i] = 0;
					}
				}
			} else {
				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] <= prev[i])
						valid[i] = 0;
				}
			}
			break;

		case kATCheatSnapMode_GreaterEqual:
			if (bit16) {
				for(uint32 i=0; i<n16; ++i) {
					if (valid[i]) {
						uint32 cur16 = cur[i] + ((uint32)cur[i+1] << 8);
						uint32 prev16 = prev[i] + ((uint32)prev[i+1] << 8);

						if (cur16 < prev16)
							valid[i] = 0;
					}
				}
			} else {
				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] < prev[i])
						valid[i] = 0;
				}
			}
			break;

		case kATCheatSnapMode_EqualRef:
			if (bit16) {
				uint8 vlo = (uint8)value;
				uint8 vhi = (uint8)(value >> 8);

				for(uint32 i=0; i<n16; ++i) {
					if (valid[i] && (cur[i] != vlo || cur[i+1] != vhi))
						valid[i] = 0;
				}
			} else {
				uint8 value8 = (uint8)value;

				for(uint32 i=0; i<n8; ++i) {
					if (valid[i] && cur[i] != value8)
						valid[i] = 0;
				}
			}
			break;
	}

	memcpy(mLastData.data(), mpMemory, mMemorySize);
}

uint32 ATCheatEngine::GetValidOffsets(uint32 *dst, uint32 maxResults) const {
	uint32 n = 0;

	if (dst) {
		for(uint32 i=0; i<mMemorySize; ++i) {
			if (mValidFlags[i]) {
				if (n < maxResults)
					*dst++ = i;

				++n;
			}
		}
	} else {
		for(uint32 i=0; i<mMemorySize; ++i)
			n += mValidFlags[i];
	}

	return n;
}

uint32 ATCheatEngine::GetOffsetCurrentValue(uint32 offset, bool bit16) const {
	if (bit16) {
		return offset < mMemorySize - 1 ? mpMemory[offset] + ((uint32)mpMemory[offset + 1] << 8) : 0;
	} else {
		return offset < mMemorySize ? mpMemory[offset] : 0;
	}
}

uint32 ATCheatEngine::GetCheatCount() const {
	return (uint32)mCheats.size();
}

const ATCheatEngine::Cheat& ATCheatEngine::GetCheatByIndex(uint32 index) const {
	return mCheats[index];
}

void ATCheatEngine::AddCheat(uint32 offset, bool bit16) {
	if (bit16) {
		if (offset >= mMemorySize - 1)
			return;
	} else {
		if (offset >= mMemorySize)
			return;
	}

	Cheat cheat = { offset, (uint16)GetOffsetCurrentValue(offset, bit16), bit16, true };
	mCheats.push_back(cheat);
}

void ATCheatEngine::AddCheat(const Cheat& cheat) {
	mCheats.push_back(cheat);
}

void ATCheatEngine::RemoveCheatByIndex(uint32 index) {
	if (index < mCheats.size())
		mCheats.erase(mCheats.begin() + index);
}

void ATCheatEngine::UpdateCheat(uint32 index, const Cheat& cheat) {
	if (index < mCheats.size())
		mCheats[index] = cheat;
}

void ATCheatEngine::ApplyCheats() {
	for(Cheats::const_iterator it(mCheats.begin()), itEnd(mCheats.end()); it != itEnd; ++it) {
		uint32 address = it->mAddress;

		if (!it->mbEnabled)
			continue;

		if (it->mb16Bit) {
			if (address >= mMemorySize - 1)
				continue;

			mpMemory[address] = (uint8)it->mValue;
			mpMemory[address+1] = (uint8)(it->mValue >> 8);
		} else {
			if (address >= mMemorySize)
				continue;

			mpMemory[address] = (uint8)it->mValue;
		}
	}
}
