//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - notification list
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

#ifndef f_AT_ATCORE_NOTIFYLIST_H
#define f_AT_ATCORE_NOTIFYLIST_H

#include <vd2/system/function.h>
#include <vd2/system/vdstl.h>

class ATNotifyListBase {
protected:
	struct Iterator {
		Iterator *mpNext;
		size_t mIndex;
		size_t mLength;
	};

	void ResetIterators();
	void AdjustIteratorsForAdd(size_t pos);
	void AdjustIteratorsForRemove(size_t pos);
	void AdjustIteratorsForRemove(size_t start, size_t end);

	Iterator *mpIteratorList = nullptr;
	size_t mFirstValid = 0;
};

/// Reentrancy-safe notification list.
///
/// ATNotifyList solves the common problem of allowing adds and removes to
/// a list while it is being iterated. It offers the following guarantees:
///
/// - Any new elements are added on the end, and are not seen by any
///   notifications in progress.
///
/// - After an element is removed, notifications are no longer dispatched
///   for that element until it is subsequently re-added. This applies
///   even to any notifications currently in progress that have not reached
///   that element yet.
///
/// - Adds and removes preserve the ordering of existing items. Notifications
///   occur in insertion order.
///
/// - Iterating over the list (Notify) does not require additional space.
///
/// However, the notification list is not thread-safe.
///
template<class T>
class ATNotifyList : public ATNotifyListBase {
public:
	bool IsEmpty() const;

	void Clear();

	/// Adds a new item to the list. No check is made for a duplicate.
	/// Complexity: Amortized O(1)
	void Add(T v);

	/// Removes the first item from the list that matches the supplied value,
	/// if any. Silently exits if not found.
	/// Complexity: O(N)
	void Remove(T v);

	void NotifyAll(const vdfunction<void(T)>& fn);

	/// Process each element through a callback. If the callback returns
	/// true, stop and return true; otherwise, return false once all
	/// elements have been processed.
	bool Notify(const vdfunction<bool(T)>& fn);

	/// Process each element through a callback. If the callback returns
	/// true, stop and return true; otherwise, return false once all
	/// elements have been processed. All elements processed by the callback
	/// are removed.
	bool NotifyAndClear(const vdfunction<bool(T)>& fn);

private:
	typedef typename std::conditional<std::is_trivial<T>::value, vdfastvector<T>, vdvector<T>>::type List;

	List mList;
};

template<class T>
bool ATNotifyList<T>::IsEmpty() const {
	return mList.empty();
}

template<class T>
void ATNotifyList<T>::Clear() {
	mList.clear();
	
	ResetIterators();
}

template<class T>
void ATNotifyList<T>::Add(T v) {
	mList.push_back(v);
}

template<class T>
void ATNotifyList<T>::Remove(T v) {
	auto it = std::find(mList.begin() + mFirstValid, mList.end(), v);

	if (it != mList.end()) {
		size_t pos = (size_t)(it - mList.begin());

		AdjustIteratorsForRemove(pos);

		mList.erase(it);
	}
}

template<class T>
void ATNotifyList<T>::NotifyAll(const vdfunction<void(T)>& fn) {
	if (mList.empty())
		return;

	bool interrupted = false;

	Iterator it = { mpIteratorList, mFirstValid, mList.size() };
	mpIteratorList = &it;

	try {
		while(it.mIndex < it.mLength) {
			auto v = mList[it.mIndex++];

			fn(v);
		}
	} catch(...) {
		VDASSERT(mpIteratorList == &it);
		mpIteratorList = it.mpNext;
		throw;
	}

	VDASSERT(mpIteratorList == &it);
	mpIteratorList = it.mpNext;
}

template<class T>
bool ATNotifyList<T>::Notify(const vdfunction<bool(T)>& fn) {
	if (mList.empty())
		return false;

	bool interrupted = false;

	Iterator it = { mpIteratorList, mFirstValid, mList.size() };
	mpIteratorList = &it;

	try {
		while(it.mIndex < it.mLength) {
			auto v = mList[it.mIndex++];

			if (fn(v)) {
				interrupted = true;
				break;
			}
		}
	} catch(...) {
		VDASSERT(mpIteratorList == &it);
		mpIteratorList = it.mpNext;
		throw;
	}

	VDASSERT(mpIteratorList == &it);
	mpIteratorList = it.mpNext;

	return interrupted;
}

template<class T>
bool ATNotifyList<T>::NotifyAndClear(const vdfunction<bool(T)>& fn) {
	if (mList.empty())
		return false;

	bool interrupted = false;

	Iterator it = { mpIteratorList, 0, mList.size() };
	mpIteratorList = &it;

	try {
		while(it.mIndex < it.mLength) {
			auto v = mList[it.mIndex++];

			if (mFirstValid < it.mIndex)
				mFirstValid = it.mIndex;

			if (fn(v)) {
				interrupted = true;
				break;
			}
		}
	} catch(...) {
		VDASSERT(mpIteratorList == &it);
		mpIteratorList = it.mpNext;
		throw;
	}

	VDASSERT(mpIteratorList == &it);
	mpIteratorList = it.mpNext;

	if (mFirstValid > 0) {
		mList.erase(mList.begin(), mList.begin() + mFirstValid);

		AdjustIteratorsForRemove(0, mFirstValid);

		mFirstValid = 0;
	}

	return interrupted;
}

#endif
