//	Altirra - Atari 800/800XL/5200 emulator
//	Runtime compatibility database module
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

#ifndef f_COMPATDB_H
#define f_COMPATDB_H

#include <vd2/system/vdtypes.h>

template<typename T>
struct ATDBVector {
	sint32 mOffset;
	uint32 mSize;

	typedef T value_type;
	typedef T* pointer;
	typedef T& reference;
	typedef const T* const_pointer;
	typedef const T& const_reference;
	typedef pointer iterator;
	typedef const_pointer const_iterator;
	typedef size_t size_type;
	typedef ptrdiff_t difference_type;

	bool empty() const { return !mSize; }
	size_type size() const { return mSize; }
	pointer data() { return (pointer)((char *)&mOffset + (ptrdiff_t)mOffset); }
	const_pointer data() const { return (const_pointer)((const char *)&mOffset + (ptrdiff_t)mOffset); }
	iterator begin() { return data(); }
	const_iterator begin() const { return data(); }
	iterator end() { return data() + mSize; }
	const_iterator end() const { return data() + mSize; }
	const_iterator cbegin() const { return data(); }
	const_iterator cend() const { return data() + mSize; }
	reference operator[](size_type pos) { return data()[pos]; }
	const_reference operator[](size_type pos) const { return data()[pos]; }

	reference front() { return *data(); }
	const_reference front() const { return *data(); }
	reference back() { return data()[mSize - 1]; }
	const_reference back() const { return data()[mSize - 1]; }

	void retarget(const void *target) {
		mOffset = (const char *)target - (const char *)&mOffset;
	}

	bool validate(const void *base, size_t len) const {
		uintptr p = (uintptr)&mOffset + (uintptr)mOffset;
		uintptr offset = p - (uintptr)base;

		return offset <= len && len - offset >= mSize * sizeof(T) && (offset % alignof(T)) == 0;
	}

	bool validate(const ATDBVector& parent) const {
		uintptr p = (uintptr)&mOffset + (uintptr)mOffset;
		uintptr offset = p - (uintptr)parent.data();
		size_t parentSize = parent.size();

		return (offset % sizeof(T)) == 0 && mSize <= parentSize && (offset / sizeof(T)) <= parentSize - mSize;
	}
};

struct ATDBString {
	sint32 mOffset;

	const char *c_str() const { return (const char *)&mOffset + (ptrdiff_t)mOffset; }

	void retarget(const void *target) {
		mOffset = (sint32)((const char *)target - (const char *)&mOffset);
	}

	bool validate(const void *base, size_t len) const {
		uintptr p = (uintptr)&mOffset + (uintptr)mOffset;
		uintptr offset = p - (uintptr)base;

		return offset < len;
	}

	bool validate(const ATDBVector<char>& storage) const {
		return validate(storage.data(), storage.size());
	}
};

struct ATCompatDBTag {
	ATDBString mKey;
};

struct ATCompatDBTitle {
	ATDBString mName;
	ATDBVector<uint32> mTagIds;
};

enum ATCompatRuleType {
	kATCompatRuleType_CartChecksum,
	kATCompatRuleType_DiskChecksum,
	kATCompatRuleType_DOSBootChecksum,
	kATCompatRuleType_ExeChecksum,
};

struct ATCompatDBRule {
	uint32 mValueLo;
	uint32 mValueHi;
	uint32 mAliasId;
	uint32 mNextRuleId;

	uint64 GetValue() const { return ((uint64)mValueHi << 32) + mValueLo; }
};

struct ATCompatDBAlias {
	uint32 mRuleCount;
	uint32 mTitleId;
};

struct ATCompatDBRuleSet {
	uint32 mRuleType;
	ATDBVector<ATCompatDBRule> mRules;
};

struct ATCompatDBHeader {
	char mSignature[16];
	uint32 mVersion;
	char m_Unused1[12];

	ATDBVector<ATCompatDBRuleSet> mRuleSetTable;
	ATDBVector<ATCompatDBRule> mRuleTable;
	ATDBVector<ATCompatDBAlias> mAliasTable;
	ATDBVector<ATCompatDBTitle> mTitleTable;
	ATDBVector<ATCompatDBTag> mTagTable;
	ATDBVector<uint32> mTagIdTable;
	ATDBVector<char> mCharTable;

	bool Validate(size_t len) const;

	static const char kSignature[16];
};

enum ATCompatKnownTag {
	kATCompatKnownTag_None,
	kATCompatKnownTag_BASIC,
	kATCompatKnownTag_BASICRevA,
	kATCompatKnownTag_BASICRevB,
	kATCompatKnownTag_BASICRevC,
	kATCompatKnownTag_NoBASIC,
	kATCompatKnownTag_OSA,
	kATCompatKnownTag_OSB,
	kATCompatKnownTag_XLOS,
	kATCompatKnownTag_AccurateDiskTiming,
	kATCompatKnownTag_NoCIODevices,
	kATCompatKnownTag_NoExpandedMem,
	kATCompatKnownTag_CTIA,
	kATCompatKnownTag_NoU1MB,
	kATCompatKnownTag_Undocumented6502,
	kATCompatKnownTag_No65C816HighAddressing,
	kATCompatKnownTag_WritableDisk,
	kATCompatKnownTag_NoFloatingDataBus,
	kATCompatKnownTag_Cart52008K,
	kATCompatKnownTag_Cart520016KTwoChip,
	kATCompatKnownTag_Cart520016KOneChip,
	kATCompatKnownTag_Cart520032K,
	kATCompatKnownTagCount,	
};

ATCompatKnownTag ATCompatGetKnownTagByKey(const char *s);
const char *ATCompatGetKeyForKnownTag(ATCompatKnownTag knownTag);

class ATCompatDBView {
public:
	ATCompatDBView() = default;
	ATCompatDBView(const ATCompatDBHeader *hdr);

	bool IsValid() const { return mpHeader != nullptr; }

	const ATCompatDBRule *FindMatchingRule(ATCompatRuleType type, uint64 value) const;
	bool HasRelatedRuleOfType(const ATCompatDBRule *baseRule, ATCompatRuleType type) const;

	const ATCompatDBTitle *FindMatchingTitle(const ATCompatDBRule *const *rules, size_t numRules) const;
	ATCompatKnownTag GetKnownTag(uint32 tagId) const;

private:
	const ATCompatDBHeader *mpHeader = nullptr;
};

#endif
