//	Altirra - Atari 800/800XL/5200 emulator
//	Editable compatibility database module
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

#ifndef f_COMPATEDB_H
#define f_COMPATEDB_H

#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_hashmap.h>
#include "compatdb.h"

template<class T>
class ATCompatEDBTable {
	ATCompatEDBTable(const ATCompatEDBTable&) = delete;
	ATCompatEDBTable& operator=(const ATCompatEDBTable&) = delete;
public:
	ATCompatEDBTable() = default;

	ATCompatEDBTable(ATCompatEDBTable&& src)
		: mRows(std::move(src.mRows))
	{
		src.mRows.clear();
	}

	~ATCompatEDBTable();

	ATCompatEDBTable& operator=(ATCompatEDBTable&& src) {
		Clear();

		mRows.swap(src.mRows);
		return *this;
	}

	T *const *begin() const { return mRows.begin(); }
	T *const *end() const { return mRows.end(); }

	size_t Size() const { return mRows.size(); }
	T *GetByIndex(size_t idx) const { return mRows[idx]; }

	void Clear();
	T *Get(uint32 id) const;
	T *Create();
	T *Create(uint32 id);
	void Destroy(uint32 id);

private:
	size_t Find(uint32 id) const;

	vdvector<T *> mRows;
};

template<class T>
ATCompatEDBTable<T>::~ATCompatEDBTable() {
	Clear();
}

template<class T>
T *ATCompatEDBTable<T>::Create() {
	vdautoptr<T> p(new T());
	uint32 id = 0;

	if (!mRows.empty()) {
		uint32 idStart = mRows.front()->mId;
		uint32 idEnd = mRows.back()->mId;

		if (idEnd - idStart == mRows.size() - 1) {
			id = mRows.back()->mId;

			if (id == UINT32_MAX) {
				id = (mRows.front()->mId) - 1;
				mRows.insert(mRows.begin(), p);
			} else {
				++id;
				mRows.push_back(p);
			}
		} else {
			size_t start = 1;
			size_t end = mRows.size() - 1;

			while(end > start) {
				const size_t mid = start + ((end - start) >> 1);
				const uint32 idMid = mRows[mid]->mId;

				const size_t gapsLeft = (idMid - idStart) - (mid - start);
				const size_t gapsRight = (idEnd - idMid) - (end - (mid + 1));

				if (gapsRight >= gapsLeft) {
					start = mid + 1;
					idStart = idMid + 1;
				} else {
					end = mid;
					idEnd = idMid;
				}
			}

			mRows.insert(mRows.begin() + start, p);
		}
	} else {
		mRows.push_back(p);
	}

	p->mId = id;
	return p.release();
}

template<class T>
void ATCompatEDBTable<T>::Clear() {
	while(!mRows.empty()) {
		delete mRows.back();
		mRows.pop_back();
	}
}

template<class T>
T *ATCompatEDBTable<T>::Create(uint32 id) {

	auto it = mRows.begin() + Find(id);
	if (it != mRows.end() && (*it)->mId == id)
		return nullptr;

	vdautoptr<T> p(new T());
	p->mId = id;

	mRows.insert(it, p);

	return p.release();
}

template<class T>
T *ATCompatEDBTable<T>::Get(uint32 id) const {
	if (mRows.empty())
		return nullptr;

	auto it = mRows.begin() + Find(id);

	if (it == mRows.end() || (*it)->mId != id)
		return nullptr;

	return *it;
}

template<class T>
void ATCompatEDBTable<T>::Destroy(uint32 id) {
	auto it = mRows.begin() + Find(id);

	if (it != mRows.end() && (*it)->mId == id) {
		T *p = *it;

		mRows.erase(it);
		delete p;
	}
}

template<class T>
size_t ATCompatEDBTable<T>::Find(uint32 id) const {
	size_t n = mRows.size();
	size_t pos = 0;
	while(n > 0) {
		size_t step = n >> 1;

		if (mRows[pos + step]->mId < id) {
			pos += step + 1;
			n -= step + 1;
		} else {
			n = step;
		}
	}

	return pos;
}

///////////////////////////////////////////////////////////////////////////

struct ATCompatEDBAliasRule {
	ATCompatRuleType mRuleType;
	uint64 mChecksum;

	bool operator==(const ATCompatEDBAliasRule& other) const {
		return mRuleType == other.mRuleType && mChecksum == other.mChecksum;
	}

	bool operator!=(const ATCompatEDBAliasRule& other) const {
		return !operator==(other);
	}

	VDStringW ToDisplayString() const;
};

struct ATCompatEDBSourcedAliasRule {
	ATCompatEDBAliasRule mRule;
	VDStringW mSource;

	VDStringW ToDisplayString() const;
};

struct ATCompatEDBAlias {
	vdfastvector<ATCompatEDBAliasRule> mRules;
};

struct ATCompatEDBTitle {
	uint32 mId;
	VDStringW mName;
	vdvector<ATCompatEDBAlias> mAliases;
	vdvector<VDStringA> mTags;
};

struct ATCompatEDBTag {
	VDStringA mKey;
	VDStringW mDisplayName;
};

struct ATCompatEDB {
	ATCompatEDBTable<ATCompatEDBTitle> mTitleTable;
	vdhashmap<VDStringA, ATCompatEDBTag, vdhash<VDStringA>, vdstringpred> mTagTable;
};


void ATLoadCompatEDB(const wchar_t *path, ATCompatEDB& edb);
void ATSaveCompatEDB(const wchar_t *path, const ATCompatEDB& edb);
void ATCompileCompatEDB(vdblock<char>& dst, const ATCompatEDB& edb);

#endif
